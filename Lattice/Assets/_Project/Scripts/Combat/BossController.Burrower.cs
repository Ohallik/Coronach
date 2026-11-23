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
        static readonly Kit BurrowerKit=Register(new Kit
        {
            id="Burrower",title="THE BURROWER",epithet="Sorrel's drill rig, chasing an evacuation signal",thresholds=new[]{.6f,.3f},
            choose=(boss,hero)=>boss.ChooseBurrower(hero),
            moves={["DrillCharge"]=boss=>boss.DrillCharge(boss.Phase>=2?2:1),["SpoilToss"]=boss=>boss.SpoilToss(),["TailSweep"]=boss=>boss.TailSweep(),["Burrow"]=boss=>boss.Burrow(),["SeismicRings"]=boss=>boss.SeismicRings()},
            signature=phase=>phase==2?"Burrow":phase>=3?"SeismicRings":null,
            shift=boss=>boss.SummonMites(boss.HealthPhase>=3?2:2),
            fightGoal=(boss,label)=>"Stop the Burrower",
            occlusion=true,ending=Ending.ShutDown,
            start=boss=>boss.BurrowerBody(),update=boss=>boss.BurrowerBodyFollows(),down=boss=>boss.BurrowerBodyDown(),
        });
        /// <summary>The rig is drawn five and a half metres long and its capsule is a rig's, 2.8 m across, so a hero walked a metre into its drill and stood in its mouth (nov20-whole s06/025, 030). Its
        /// solids now match what is drawn: a capsule laid along its length, as wide as the drawn body and never wider than the upright one (so its flanks, and the weak spots on them,
        /// are where they were), that stops a hero at its drill and its tail; and a hurtbox the same shape, so a blade or a shot that meets the drawn drill lands, and the replay
        /// driver, which measures its reach to a body's hurtboxes, swings from where it's stopped. Measured from the drawn body as it is made. Its charge passes through the party
        /// (Collide), so the length does too; buried, it has none.</summary>
        CapsuleCollider burrowerSolid,burrowerHurt;readonly List<CharacterController> burrowerThrough=new(),burrowerClearing=new();
        internal const float BurrowerLengthDrawn=.94f,BurrowerWidthDrawn=.9f;
        void BurrowerBody()
        {
            if(visual==null)return;var own=GetComponent<CharacterController>();if(own==null)return;
            // The drawn body's extent in the rig's own space, from each mesh's bounds (readable in a player, unlike its vertices).
            var toLocal=transform.worldToLocalMatrix;Vector3 low=Vector3.one*float.MaxValue,high=-low;
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var b=filter.sharedMesh.bounds;var to=toLocal*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++){var p=to.MultiplyPoint3x4(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));low=Vector3.Min(low,p);high=Vector3.Max(high,p);}
            }
            if(low.x>high.x)return;
            float length=(high.z-low.z)*BurrowerLengthDrawn,radius=Mathf.Min((high.x-low.x)*.5f*BurrowerWidthDrawn,own.radius);
            if(length<=radius*2+.2f)return;
            var centre=new Vector3((low.x+high.x)*.5f,radius,(low.z+high.z)*.5f);
            var body=new GameObject("Burrower length");body.transform.SetParent(transform,false);body.transform.localPosition=Vector3.zero;
            burrowerSolid=body.AddComponent<CapsuleCollider>();burrowerSolid.direction=2;burrowerSolid.center=centre;burrowerSolid.radius=radius;burrowerSolid.height=length;
            Physics.IgnoreCollision(own,burrowerSolid,true);
            var hurt=new GameObject("Burrower length hurtbox",typeof(Hurtbox));hurt.transform.SetParent(transform,false);
            burrowerHurt=hurt.AddComponent<CapsuleCollider>();burrowerHurt.isTrigger=true;burrowerHurt.direction=2;burrowerHurt.center=centre;burrowerHurt.radius=radius;burrowerHurt.height=length;
            hurt.GetComponent<Hurtbox>().owner=health;
            Debug.Log($"BURROWER_LENGTH drawn={high.z-low.z:0.0}x{high.x-low.x:0.0} solid={length:0.0}x{radius*2:0.0}");
        }
        void BurrowerBodyFollows()
        {
            if(burrowerSolid==null)return;
            bool there=!burrowed;
            if(burrowerSolid.enabled!=there)
            {
                // Coming up under someone, it doesn't throw them across the floor: whoever it surfaces on passes out of it, and it is solid to them once they have.
                if(there&&PartyController.Current!=null)foreach(var member in PartyController.Current.members)
                {
                    var body=member!=null?member.GetComponent<CharacterController>():null;if(body==null||burrowerClearing.Contains(body))continue;
                    if(body.bounds.Intersects(burrowerSolid.bounds)){Physics.IgnoreCollision(body,burrowerSolid,true);burrowerClearing.Add(body);}
                }
                burrowerSolid.enabled=there;burrowerHurt.enabled=there;
            }
            for(int i=burrowerClearing.Count-1;i>=0;i--)
            {
                var body=burrowerClearing[i];
                if(body!=null&&body.bounds.Intersects(burrowerSolid.bounds))continue;
                if(body!=null&&!burrowerThrough.Contains(body))Physics.IgnoreCollision(body,burrowerSolid,false);
                burrowerClearing.RemoveAt(i);
            }
            if(PartyController.Current==null)return;
            foreach(var member in PartyController.Current.members)
            {
                var body=member!=null?member.GetComponent<CharacterController>():null;if(body==null)continue;
                bool through=passing.Contains(body);if(through==burrowerThrough.Contains(body))continue;
                if(through){burrowerThrough.Add(body);Physics.IgnoreCollision(body,burrowerSolid,true);}
                else{burrowerThrough.Remove(body);if(!burrowerClearing.Contains(body))Physics.IgnoreCollision(body,burrowerSolid,false);}
            }
        }
        /// <summary>Shut down, the rig is a wreck where it stopped: still solid, and no longer anything to strike.</summary>
        void BurrowerBodyDown(){if(burrowerHurt!=null)burrowerHurt.enabled=false;}
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
                    Contact(hit,transform.position,2.3f,20,.45f,lane);
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
                var clump=Instantiate(Resources.Load<GameObject>("Effects/SpoilClump"),start,Quaternion.Euler(0,Random.Range(0,360f),0));clump.name="Spoil clump";temporary.Add(clump);clumps.Add(clump.transform);
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
            foreach(var marking in marks){Strike(marking,12,DamageType.Kinetic,.3f,0,once);CombatVfx.Burst(marking.Origin+Vector3.up*.3f,new Color(.55f,.45f,.35f),"kill");}
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
            Strike(arc,18,DamageType.Kinetic,.4f,2.6f);
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
            Strike(eruption,20,DamageType.Kinetic,.6f,3f);strikeAt=0;
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
            Strike(slam,16,DamageType.Kinetic,.4f,2.4f);Tremor(.6f,.12f);Pose(0);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.28f,AudioBus.SFX,60);
            const float band=1.5f,speed=8f,reach=17f,spacing=.55f;
            var centre=transform.position;var rings=new List<(Telegraph mark,float born,HashSet<Health> met)>();
            // One cast, one hit: bands .55 s apart with a .3 s stagger chained a hero from 60 to 0 in a second. Only one that lands spends it:
            // a band he rolled through is behind him and the next is still to come, and each band meets him once, so a perfect guard isn't hit a frame later by the band it just stopped.
            var struck=new HashSet<Health>();
            float started=Time.time;int launched=0;
            while(health.Alive&&(launched<3||rings.Count>0))
            {
                if(GameTime.Paused){yield return null;continue;}
                if(launched<3&&Time.time-started>=launched*spacing)
                {
                    // Out of the slam, from its centre: born at the slam's rim, a band struck a hero who had just stepped clear of it in under a fifth of a second. Now nothing outside that rim is struck in under a third.
                    var ring=Mark(Telegraph.Ring(centre,0,.5f,99,Telegraph.Threat));ring.Steady=true;
                    rings.Add((ring,Time.time,new HashSet<Health>()));launched++;
                }
                float soonest=float.PositiveInfinity;var hero=PartyController.Current.Active.transform.position;
                for(int i=rings.Count-1;i>=0;i--)
                {
                    var (mark,born,met)=rings[i];float outer=.5f+(Time.time-born)*speed;
                    if(outer-band>reach){mark.Cancel();rings.RemoveAt(i);continue;}
                    mark.SetRing(outer-band,outer);
                    foreach(var actor in PartyController.Current.members)
                    {
                        if(!actor.Health.Alive||struck.Contains(actor.Health)||met.Contains(actor.Health))continue;
                        float d=Flat(actor.transform.position-centre).magnitude;
                        if(d>=outer-band-Telegraph.BodyAllowance&&d<=outer+Telegraph.BodyAllowance)
                        {met.Add(actor.Health);float dealt=actor.Health.Receive(Packet(13,DamageType.Pulse));if(dealt>0){struck.Add(actor.Health);actor.Stagger(.3f);}}
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
