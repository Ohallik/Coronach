using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Burrower (D198): Sorrel's drill rig, a machine-animal following evacuation signals. A heavy, territorial
    /// brawler. Phase 1 charges along a marked lane (and sticks fast if it rams rock, open to punishment), lobs spoil
    /// at range and sweeps its tail at anyone behind it. Phase 2 (below 60%) dives and hunts the active hero from below
    /// before erupting, and charges twice. Phase 3 (below 30%) adds seismic rings that must be outrun or dodged through.
    /// Each phase shift calls Scrapmites out of the spoil.
    /// </summary>
    public sealed partial class BossController
    {
        bool burrowed;
        public bool Burrowed=>burrowed;
        float BurrowerRest=>Phase>=3?3f:Phase>=2?3.6f:4.2f;

        (string,IEnumerator) ChooseBurrower(CombatActor hero)
        {
            var offset=Flat(hero.transform.position-transform.position);float distance=offset.magnitude;
            bool behind=distance<6&&Vector3.Angle(transform.forward,offset)>75;
            if(behind&&lastMove!="TailSweep")return("TailSweep",TailSweep());
            string move=Pick(("DrillCharge",distance>4?2.2f:.8f),("SpoilToss",distance>7?1.8f:.5f),("TailSweep",behind?1f:0),
                ("Burrow",Phase>=2?1.5f:0),("SeismicRings",Phase>=3?1.6f:0));
            return move switch
            {
                "SpoilToss"=>(move,SpoilToss()),
                "TailSweep"=>(move,TailSweep()),
                "Burrow"=>(move,Burrow()),
                "SeismicRings"=>(move,SeismicRings()),
                _=>(move,DrillCharge(Phase>=2?2:1))
            };
        }

        IEnumerator DrillCharge(int charges)
        {
            var controller=GetComponent<CharacterController>();
            for(int n=0;n<charges;n++)
            {
                var target=PartyController.Current.Active.transform.position;var direction=Toward(target);
                transform.rotation=Quaternion.LookRotation(direction);
                // The charge carries a short way past where the hero stood, so it wedges in rock only when the hero baited it
                // there; across open floor it overshoots and turns. (A fixed 20 m run hit a wall almost every time.)
                float tell=Phase>=2?.75f:.9f,speed=Phase>=2?19:15;
                float length=Mathf.Clamp(Flat(target-transform.position).magnitude+(Phase>=3?7:6),10,Phase>=3?24:20);
                var lane=Mark(Telegraph.Line(transform.position,direction,length,3.4f,tell,Telegraph.Heavy));
                strikeAt=Time.time+tell;
                yield return Windup(tell,-9,.15f,.05f);
                if(Interrupted)yield break;
                lane.Release();Pose(11);
                var hit=new HashSet<Health>();float travelled=0,elapsed=0;bool stuck=false;
                Collide(false);
                while(travelled<length&&elapsed<length/speed+.4f&&!Interrupted)
                {
                    if(GameTime.Paused){yield return null;continue;}
                    elapsed+=Time.deltaTime;
                    float step=speed*Time.deltaTime;
                    if(Rock(direction,step+.4f)){stuck=true;break;}
                    var before=transform.position;
                    if(controller.enabled)controller.Move(direction*step);
                    travelled+=Flat(transform.position-before).magnitude;
                    Contact(hit,transform.position,2.3f,24,.45f,lane);
                    yield return null;
                }
                strikeAt=0;Collide(true);Pose(0);
                if(stuck){yield return Stuck(direction);yield break;}
                if(n+1<charges)yield return new WaitForSeconds(.35f);
            }
            Finish(BurrowerRest);
        }
        /// <summary>Solid scenery (not a body) directly ahead of the rig's drill.</summary>
        bool Rock(Vector3 direction,float distance)
        {
            var hits=Physics.SphereCastAll(transform.position+Vector3.up*1.5f,1.1f,direction,distance,~0,QueryTriggerInteraction.Ignore);
            foreach(var hit in hits)
            {
                if(hit.collider.transform.IsChildOf(transform)||hit.collider is CharacterController||hit.collider.GetComponentInParent<Health>()!=null)continue;
                if(hit.distance<=0&&hit.point==Vector3.zero)continue;
                // Ground underfoot is not a wall: only surfaces facing back along the charge.
                if(Vector3.Dot(hit.normal,-direction)>.45f)return true;
            }
            return false;
        }
        IEnumerator Stuck(Vector3 direction)
        {
            // The drill has buried itself in rock: a clear opening (Zelda-style bait and punish).
            float seconds=Phase>=3?1.8f:Phase>=2?2.2f:2.6f;
            Tremor(.45f,.12f);AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up*1.4f,.35f,AudioBus.SFX,60);
            for(int i=0;i<3;i++)CombatVfx.Burst(transform.position+direction*1.6f+Vector3.up*(.6f+i*.5f),new Color(.75f,.6f,.45f),"kill");
            Debug.Log("BURROWER_STUCK");
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(seconds,1.4f);
        }

        IEnumerator SpoilToss()
        {
            // Spoil lobbed at the party: each clump lands inside its own marked circle.
            float tell=1.15f,radius=2.3f;var party=PartyController.Current;
            var targets=new List<Vector3>{party.Active.transform.position};
            foreach(var member in party.members)if(member!=party.Active&&member.Health.Alive&&Flat(member.transform.position-transform.position).sqrMagnitude<400)targets.Add(member.transform.position);
            int extra=Phase>=2?3:1;
            for(int i=0;i<extra;i++)targets.Add(party.Active.transform.position+Quaternion.Euler(0,Random.Range(0,360f),0)*Vector3.forward*Random.Range(3.5f,5.5f));
            var marks=new List<Telegraph>();var clumps=new List<Transform>();var start=transform.position+Vector3.up*2.6f+transform.forward*1.4f;
            foreach(var point in targets)
            {
                marks.Add(Mark(Telegraph.Circle(point,radius,tell,Telegraph.Threat)));
                var clump=ActorFactory.Visual("Spoil clump",PrimitiveType.Sphere,null,Vector3.one*.75f,start,"Rock");temporary.Add(clump);clumps.Add(clump.transform);
            }
            AudioManager.PlayAt("explosionCrunch_000",transform,start,.12f,AudioBus.SFX,40);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/tell);
                Pose(k<.35f?-14*Mathf.SmoothStep(0,1,k/.35f):Mathf.Lerp(-14,4,Mathf.SmoothStep(0,1,(k-.35f)/.25f)));
                for(int i=0;i<clumps.Count;i++)
                {
                    var landing=marks[i].Origin;
                    clumps[i].position=Vector3.Lerp(start,landing,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*6f);
                    clumps[i].Rotate(200*Time.deltaTime,90*Time.deltaTime,0);
                }
                ThreatFrom(marks);
                yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var marking in marks){Strike(marking,15,DamageType.Kinetic,.3f,0,once);CombatVfx.Burst(marking.Origin+Vector3.up*.3f,new Color(.55f,.45f,.35f),"kill");}
            Tremor(.25f,.06f);ClearTemporary();Pose(0);
            Finish(BurrowerRest);
        }

        IEnumerator TailSweep()
        {
            // A wide sweep behind and beside the rig: the answer to standing at its back.
            float tell=.65f;var back=-transform.forward;
            var arc=Mark(Telegraph.Cone(transform.position,back,6.2f,230,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            yield return Windup(tell,5,0,.06f);
            Pose(0);
            if(Interrupted)yield break;
            Strike(arc,22,DamageType.Kinetic,.4f,2.6f);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.22f,AudioBus.SFX,50);
            var rest=transform.rotation;
            for(float t=0;t<.3f&&!Interrupted;){if(!GameTime.Paused)t+=Time.deltaTime;transform.rotation=rest*Quaternion.Euler(0,360*Mathf.SmoothStep(0,1,t/.3f),0);yield return null;}
            transform.rotation=rest;
            Finish(BurrowerRest*.8f);
        }

        IEnumerator Burrow()
        {
            // Dive, hunt the active hero from below (a dust ring that follows), lock, erupt; then it is briefly exposed.
            var controller=GetComponent<CharacterController>();
            Tremor(.4f,.07f);CombatVfx.Burst(transform.position+Vector3.up*.4f,new Color(.6f,.5f,.38f),"kill");
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.16f,AudioBus.SFX,50);
            for(float t=0;t<.45f;){if(!GameTime.Paused)t+=Time.deltaTime;if(visual!=null)visual.localPosition=visualRest+Vector3.down*(2.6f*Mathf.SmoothStep(0,1,t/.45f));yield return null;}
            burrowed=true;health.Damageable=false;controller.enabled=false;
            var hero=PartyController.Current.Active;
            var trail=Mark(Telegraph.Circle(hero.transform.position,2.6f,99,Telegraph.Trail));trail.Steady=true;
            float hunt=Phase>=3?1.9f:2.3f;var at=transform.position;
            for(float t=0;t<hunt;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                if(!health.Alive)yield break;
                var goal=PartyController.Current.Active.transform.position;
                at=Vector3.MoveTowards(Flat(at),Flat(goal),7.5f*Time.deltaTime)+Vector3.up*goal.y;
                trail.MoveTo(at);
                // The buried body travels under its marking, so the eruption's tell comes from where it will strike
                // (the companion and the replay driver read tells by the boss's distance).
                transform.position=at;
                if(Time.frameCount%6==0)CombatVfx.Burst(at+Vector3.up*.2f,new Color(.6f,.5f,.38f),"failure");
                yield return null;
            }
            trail.Cancel();
            var eruption=Mark(Telegraph.Circle(at,3.2f,.75f,Telegraph.Heavy));strikeAt=Time.time+.75f;
            yield return new WaitForSeconds(.75f);
            if(!health.Alive)yield break;
            // Surface at the marked point, on the ground beneath it.
            var surface=CombatCover.Ground(at+Vector3.up*4);
            transform.position=surface+Vector3.up*.05f;Physics.SyncTransforms();
            controller.enabled=true;burrowed=false;health.Damageable=true;
            if(visual!=null)visual.localPosition=visualRest;
            Strike(eruption,24,DamageType.Kinetic,.6f,3f);strikeAt=0;
            Tremor(.5f,.13f);CombatVfx.Burst(surface+Vector3.up,new Color(.65f,.52f,.4f),"kill");
            AudioManager.PlayAt("explosionCrunch_000",transform,surface,.3f,AudioBus.SFX,60);
            yield return Opening(1.4f,BurrowerRest);
        }

        IEnumerator SeismicRings()
        {
            // Slam, then three expanding rings: get clear of the slam, then outrun each band or dodge through it.
            float windup=.8f;var slam=Mark(Telegraph.Circle(transform.position,3.2f,windup,Telegraph.Heavy));strikeAt=Time.time+windup;
            yield return Windup(windup,-6,.9f,.04f);
            if(Interrupted)yield break;
            Pose(4);
            Strike(slam,20,DamageType.Kinetic,.4f,2.4f);Tremor(.6f,.12f);Pose(0);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.28f,AudioBus.SFX,60);
            const float band=1.5f,speed=8f,reach=17f,spacing=.55f;
            var centre=transform.position;var rings=new List<(Telegraph mark,float born,HashSet<Health> hit)>();
            // One cast, one hit: bands .55 s apart with a .3 s stagger chained a hero from 60 to 0 in a second.
            var struck=new HashSet<Health>();
            float started=Time.time;int launched=0;
            while(health.Alive&&(launched<3||rings.Count>0))
            {
                if(GameTime.Paused){yield return null;continue;}
                if(launched<3&&Time.time-started>=launched*spacing)
                {
                    var ring=Mark(Telegraph.Ring(centre,.5f,.5f+band,99,Telegraph.Threat));ring.Steady=true;
                    rings.Add((ring,Time.time,struck));launched++;
                }
                float soonest=float.PositiveInfinity;var hero=PartyController.Current.Active.transform.position;
                for(int i=rings.Count-1;i>=0;i--)
                {
                    var (mark,born,hit)=rings[i];float outer=.5f+band+(Time.time-born)*speed;
                    if(outer-band>reach){mark.Cancel();rings.RemoveAt(i);continue;}
                    mark.SetRing(outer-band,outer);
                    foreach(var actor in PartyController.Current.members)
                    {
                        if(!actor.Health.Alive||hit.Contains(actor.Health))continue;
                        float d=Flat(actor.transform.position-centre).magnitude;
                        if(d>=outer-band-Telegraph.BodyAllowance&&d<=outer+Telegraph.BodyAllowance)
                        {hit.Add(actor.Health);float dealt=actor.Health.Receive(Packet(16,DamageType.Pulse));if(dealt>0)actor.Stagger(.3f);}
                    }
                    float heroDistance=Flat(hero-centre).magnitude;
                    if(heroDistance>outer)soonest=Mathf.Min(soonest,(heroDistance-outer)/speed);
                }
                strikeAt=float.IsPositiveInfinity(soonest)?0:Time.time+soonest;
                yield return null;
            }
            Finish(BurrowerRest);
        }

        /// <summary>Open ground for a summoned add: next to a cliff, step back toward the boss rather than land on or under it.</summary>
        Vector3 SummonPoint(Vector3 offset)
        {
            for(float k=1;k>.2f;k-=.3f)
            {
                var ground=CombatCover.Ground(transform.position+offset*k+Vector3.up*6);
                if(Mathf.Abs(ground.y-transform.position.y)<1.2f)return ground;
            }
            return transform.position;
        }
        void SummonMites(int count)
        {
            var definition=GameCatalog.Find<EnemyDef>("Scrapmite");if(definition==null)return;
            summoned.RemoveAll(s=>s==null||!s.Health.Alive);
            for(int i=0;i<count&&summoned.Count<2;i++)
            {
                var offset=Quaternion.Euler(0,transform.eulerAngles.y+(i==0?-70:70),0)*Vector3.forward*4.5f;
                var point=SummonPoint(offset);
                var mite=ActorFactory.Enemy(definition,point+Vector3.up*.05f);summoned.Add(mite);
                CombatVfx.Burst(point+Vector3.up*.4f,new Color(.6f,.5f,.38f),"kill");
            }
            Debug.Log("BOSS_SUMMON "+health.id+" "+summoned.Count);
        }
    }
}
