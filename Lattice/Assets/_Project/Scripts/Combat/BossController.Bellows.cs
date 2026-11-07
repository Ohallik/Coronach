using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Bellows Below (D198): Hushwell's breathing guardian. Its inhale now drags the party toward it before the exhale
    /// ring lands (run outward, or dodge through the blast), and one pumping organ blasts its marked half of the chamber.
    /// Its ribs slam out in a marked star. From phase 2 (below 50%) it looses drifting pressure bubbles that hunt the
    /// active hero and burst; in phase 3 (below 30%) it lunges between its exposed ribs and breathes from each.
    /// </summary>
    public sealed partial class BossController
    {
        float BellowsRest=>Phase>=3?3.2f:Phase>=2?3.8f:4.6f;
        /// <summary>The colour of the markings they burst in. In hot orange, against the ribs, they read as a fireball or a weak spot on the boss.</summary>
        static readonly Color BubbleColour=new(.72f,.4f,1f,.95f);
        const float BubbleGathers=3.2f,BubbleSpread=1.2f,BubbleRise=1.9f;

        (string,IEnumerator) ChooseBellows(CombatActor hero)
        {
            // The last phase keeps the fight moving: every other special crosses to a rib.
            if(Phase>=3&&lastMove!="Crossing")return("Crossing",BellowsCrossing());
            string move=Pick(("Breath",2f),("RibStar",1.4f),("Bubbles",Phase>=2?1.3f:0));
            return move switch{"RibStar"=>(move,RibStar()),"Bubbles"=>(move,Bubbles()),_=>(move,BellowsBreath())};
        }

        void Deflate()
        {
            if(inflated!=null)inflated.localScale=inflatedRest;inflated=null;if(body!=null)body.localScale=bodyRest;
            foreach(var organ in PressureOrgan.All)organ.Breathe(0);
        }
        /// <summary>A phase shift's brace on a body with no rig to rear back with: the dome draws down onto the floor and spreads, shaking as the ring fills, and lets go with the shove.</summary>
        IEnumerator DomeBrace(float seconds)
        {
            inflated=body;inflatedRest=bodyRest;
            for(float t=0;t<seconds&&!Interrupted;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds)),shake=.035f*k*k*Mathf.Sin(Time.time*47);
                inflated.localScale=Vector3.Scale(inflatedRest,new Vector3(1+.16f*k+shake,1-.24f*k-shake,1+.16f*k+shake));
                yield return null;
            }
            Deflate();
        }
        IEnumerator BellowsBreath()
        {
            // Inhale: the body and its organs swell and pull the party in. One pumping organ, in turn, marks its half of the chamber, or with none left the dome marks a ring.
            // Exhale: everything inside what's marked is struck and staggered.
            busy=true;float radius=Phase>=2?13:9.5f,inhale=Phase>=2?1.25f:1.05f;
            PressureOrgan blast=null;int pumping=0;foreach(var organ in PressureOrgan.All)if(organ.Pumping)pumping++;
            if(pumping>0){int turn=breaths++%pumping;foreach(var organ in PressureOrgan.All)if(organ.Pumping&&turn--==0){blast=organ;break;}}
            // One thing to read at a time. While an organ pumps the breath goes out through it, across its half and no further: with the ring round the dome on top of that nothing on screen was
            // safe, and breaking an organ made nothing safe either. The ring comes back when no organ is left to blast. The hit reads who's really under it, so a hero on the safe side isn't told to roll.
            Telegraph half=null,ring=null;var marks=new List<Telegraph>(1);
            if(blast!=null)
            {
                var mid=PressureOrgan.Middle;var side=Flat(blast.transform.position-mid);
                half=Mark(Telegraph.Cone(mid,side,24,180,inhale,Telegraph.Stagger));marks.Add(half);
            }
            else{ring=Mark(Telegraph.Circle(transform.position,radius,inhale,Telegraph.Threat));marks.Add(ring);}
            inflated=body;inflatedRest=bodyRest;ThreatFrom(marks);
            float pull=Phase>=2?3f:2.2f;
            for(float t=0;t<inhale;)
            {
                if(GameTime.Paused){yield return null;continue;}
                t+=Time.deltaTime;float k=Mathf.Clamp01(t/inhale);
                if(inflated!=null)inflated.localScale=inflatedRest*(1+.35f*k);
                foreach(var organ in PressureOrgan.All)organ.Breathe(k);
                // The draw of the inhale: strongest close in, gone at the chamber's edge; a dodge is not dragged.
                foreach(var actor in PartyController.Current.members)
                {
                    if(!actor.Health.Alive||actor.State==ActorState.Dodge)continue;
                    var toward=Flat(transform.position-actor.transform.position);float d=toward.magnitude;
                    if(d<2.4f||d>18)continue;
                    var controller=actor.GetComponent<CharacterController>();
                    if(controller!=null&&controller.enabled)controller.Move(toward/d*(pull*(1-d/18f)*Time.deltaTime));
                }
                ThreatFrom(marks);
                yield return null;
            }
            Deflate();
            bool collapsed=health.Alive&&!health.Broken;
            if(health.Alive&&!health.Broken)
            {
                CombatVfx.Burst(transform.position+Vector3.up,new Color(.85f,.72f,.55f),"shape");
                Tremor(.3f,.07f);
                if(ring!=null)ring.Release();if(half!=null&&blast.Pumping)half.Release();else if(half!=null)half.Cancel();
                foreach(var actor in PartyController.Current.members)
                    if(actor.Health.Alive&&(ring!=null&&ring.Contains(actor.transform.position)||half!=null&&blast.Pumping&&half.Contains(actor.transform.position)))
                    {
                        float damage=actor.Health.Receive(Packet(22,DamageType.Kinetic));
                        if(damage>0)actor.Stagger(.5f);
                    }
            }
            // The breath spent, the dome sags: this is the fight's rhythm, and its window.
            if(collapsed)yield return Opening(1.3f,BellowsRest);else Finish(BellowsRest);
        }
        IEnumerator RibStar()
        {
            // The ribs slam outward along marked spokes: stand between them.
            int spokes=Phase>=2?6:4;float tell=1f,offset=Random.Range(0,360f/spokes);
            var marks=new List<Telegraph>();
            for(int i=0;i<spokes;i++)
            {
                var direction=Quaternion.Euler(0,offset+i*360f/spokes,0)*Vector3.forward;
                marks.Add(Mark(Telegraph.Line(transform.position+direction*1.2f,direction,15,2.4f,tell,Telegraph.Heavy)));
            }
            inflated=body;inflatedRest=bodyRest;
            for(float t=0;t<tell;)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(Interrupted)yield break;
                t+=Time.deltaTime;if(inflated!=null)inflated.localScale=inflatedRest*(1-.12f*Mathf.Clamp01(t/tell));
                ThreatFrom(marks);yield return null;
            }
            Deflate();
            var once=new HashSet<Health>();
            foreach(var marking in marks)
            {
                Strike(marking,20,DamageType.Kinetic,.45f,0,once);
                for(int s=1;s<=3;s++)CombatVfx.Burst(marking.Origin+marking.Forward*(s*4.5f)+Vector3.up*.3f,new Color(.8f,.68f,.52f),"shape");
            }
            Tremor(.35f,.09f);AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.2f,AudioBus.SFX,60);
            Finish(BellowsRest);
        }
        IEnumerator Bubbles()
        {
            // Pressure bubbles drift after the active hero and burst where their marking has charged.
            int count=Phase>=3?6:5;float fuse=3.2f,radius=1.9f,speed=2.4f;
            var bubbles=new List<(Transform view,Telegraph mark)>();
            for(int i=0;i<count;i++)
            {
                var around=Quaternion.Euler(0,i*360f/count,0)*Vector3.forward*3.2f;var start=transform.position+around;
                var view=ActorFactory.Visual("Pressure bubble",PrimitiveType.Sphere,null,Vector3.one*1.1f,start+Vector3.up*1.1f,"Threat");view.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(BubbleColour);view.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;temporary.Add(view);
                bubbles.Add((view.transform,Mark(Telegraph.Circle(start,radius,fuse+i*.12f,Telegraph.Stagger))));
            }
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.18f,AudioBus.SFX,50);
            var marks=new List<Telegraph>();foreach(var b in bubbles)marks.Add(b.mark);
            // One volley, one hit: they burst .12 s apart, and the first one's .25 s stagger held a standing hero for the rest, 40% of him with no way to roll.
            var once=new HashSet<Health>();
            var lens=Camera.main;float nearSide=(lens!=null?lens.transform.eulerAngles.y:0)+180;
            while(true)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(!health.Alive)yield break;
                var goal=PartyController.Current.Active.transform.position;bool waiting=false;
                for(int i=0;i<bubbles.Count;i++)
                {
                    var (view,mark)=bubbles[i];if(view==null)continue;
                    if(mark.Charged)
                    {
                        mark.Hide(false);Strike(mark,14,DamageType.Pulse,.25f,0,once,true);CombatVfx.Burst(view.position,new Color(.7f,.45f,1f),"shape");
                        temporary.Remove(view.gameObject);Destroy(view.gameObject);bubbles[i]=(null,mark);continue;
                    }
                    waiting=true;
                    var at=Vector3.MoveTowards(Flat(mark.Origin),Flat(goal),speed*Time.deltaTime)+Vector3.up*mark.Origin.y;
                    mark.MoveTo(at);
                    // The markings close on him as one, and that's the hit. What's drawn rises over him and spreads as it comes, the gap on the camera's side. All at chest height on the one spot,
                    // five of them were a glowing ball with the hero a thin shape inside it, and against the ribs the door showed it through its dots.
                    float close=1-Mathf.Clamp01(Flat(at-goal).magnitude/BubbleGathers);
                    view.position=at+Quaternion.Euler(0,nearSide+(i+.5f)*360f/bubbles.Count,0)*Vector3.forward*(BubbleSpread*close)+Vector3.up*(1.1f+BubbleRise*close+.15f*Mathf.Sin(Time.time*4+i));
                    // Rings on one spot are one ring, however many: five of them were a glaring disc.
                    bool under=false;for(int j=0;j<i&&!under;j++)under=bubbles[j].view!=null&&Flat(bubbles[j].mark.Origin-at).sqrMagnitude<.09f;
                    mark.Hide(under);
                }
                ThreatFrom(marks);
                if(!waiting)break;
                yield return null;
            }
            Finish(BellowsRest);
        }
        IEnumerator BellowsCrossing()
        {
            // Last phase: the dome collapses onto its ribs. It marks a path to the
            // rib nearest the party, lunges along it striking whatever stands in
            // the way, then breathes from the new rib.
            busy=true;var rib=BellowsRib.Choose(transform.position,PartyController.Current.Active.transform.position);
            if(rib==null){yield return BellowsBreath();yield break;}
            var goal=rib.Stance;var direction=goal-transform.position;direction.y=0;float distance=direction.magnitude;direction/=Mathf.Max(.01f,distance);
            transform.rotation=Quaternion.LookRotation(direction);strikeAt=Time.time+.9f;
            var lane=Mark(Telegraph.Line(transform.position,direction,distance,3.6f,.9f,Telegraph.Heavy));
            yield return new WaitForSeconds(.9f);
            if(Interrupted)yield break;
            lane.Release();
            var hit=new HashSet<Health>();var controller=GetComponent<CharacterController>();Collide(false);
            for(float t=0;t<1.8f&&health.Alive&&!health.Broken;)
            {
                if(GameTime.Paused){yield return null;continue;}
                t+=Time.deltaTime;var left=goal-transform.position;left.y=0;if(left.magnitude<.6f)break;
                if(controller.enabled)controller.Move(left.normalized*Mathf.Min(16*Time.deltaTime,left.magnitude));
                foreach(var actor in PartyController.Current.members)
                    if(actor.Health.Alive&&!hit.Contains(actor.Health)&&lane.Contains(actor.transform.position)&&(actor.transform.position-transform.position).sqrMagnitude<9)
                    {hit.Add(actor.Health);float damage=actor.Health.Receive(Packet(24,DamageType.Kinetic));if(damage>0)actor.Stagger(.5f);}
                yield return null;
            }
            strikeAt=0;Collide(true);
            if(health.Alive&&!health.Broken)yield return BellowsBreath();
            else Finish(BellowsRest);
        }
    }
}
