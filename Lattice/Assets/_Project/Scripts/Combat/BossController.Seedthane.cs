using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Seedthane, Seedfall's mini-boss: a tall many-legged seed-bearer that has made a nest of the beached carrier and won't be moved off it. It is driven off, not killed:
    /// at nothing it turns and goes upriver (<see cref="Ending.Leaves"/>, <see cref="BossDeparture"/>, a <see cref="BossExit"/> if the bank has one).
    /// <list type="bullet">
    /// <item><b>Planted</b> (to half): it stands rooted and turns where it stands. Thorn Fan, a wide marked sweep; Fan Volley, three of them one after another, after which it has
    /// overreached and is open; Pod Shake, which rains loose husks on marked circles at each hero and brings the nearest hanging pods (<see cref="CoverPod"/>) down where they
    /// hang. A fallen pod is cover, and a fan stops at cover. Its four planted legs are break targets (<see cref="BossParts"/>): each one broken takes a share of its bar and
    /// leaves it listing and open, and the fourth topples it for longer.</item>
    /// <item><b>Mobile</b> (under half): it pulls its legs up, so there is nothing left to break. Circle Fan, a quarter-turn round the hero and a fan from where it stops, twice;
    /// Stamping Advance, three marked stamps that walk it onto him, the last of which leaves it spent and open; Pod Shake still.</item>
    /// </list>
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit SeedthaneKit=Register(new Kit
        {
            id="Seedthane",title="THE SEEDTHANE",epithet="Nesting on Marrow-3 since the storm",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseSeedthane(hero),
            moves={["ThornFan"]=boss=>boss.ThornFan(1),["FanVolley"]=boss=>boss.ThornFan(SeedthaneTune.VolleyFans),["PodShake"]=boss=>boss.PodShake(),["CircleFan"]=boss=>boss.CircleFan(),["StampAdvance"]=boss=>boss.StampAdvance()},
            signature=phase=>phase>=2?"StampAdvance":null,
            shift=boss=>boss.SeedthaneUproots(),shiftText="UPROOTING  ·  BRACE",
            status=boss=>boss.Phase<2&&boss.legs!=null&&boss.legs.Standing>0?"PLANTED  ·  BREAK ITS LEGS":"",
            capsuleRadius=1.6f,occlusion=true,ending=Ending.Leaves,
            start=boss=>boss.SeedthaneStart(),update=boss=>boss.SeedthaneUpdate(),down=boss=>boss.SeedthaneDown(),
        });
        /// <summary>The Seedthane's numbers, all of them (its integrity is its EnemyDef's: XylosBuilder.Enemies). Damage is before the phase's +18%: each move takes 12.3 to 15% of a Sync 11
        /// bar in Refined parts (bar 300, a hit landing at .735) in whichever phase it is used, more than anything on the river (the Thornback's charge, 11.8%; XylosCombatTests).</summary>
        public static class SeedthaneTune
        {
            /// <summary>Legs: how many, each one's integrity and the bar it takes with it as shares of the boss's, and how long it lists for one and lies toppled for the last.</summary>
            public const int Legs=4;
            public const float LegIntegrity=.035f,LegBar=.06f,Lists=3,Topples=5;
            public const float FanReach=7.5f,FanAngle=100,FanTell=.8f,FanTellMobile=.65f,FanNext=.55f,FanDamage=52,Overreached=1.5f;
            public const int VolleyFans=3;
            /// <summary>Pod Shake: the time everything is in the air, each landing's radius and damage, how many hanging pods one shake brings down, and from how far.</summary>
            public const float PodTell=1,PodRadius=2.2f,PodDamage=50,PodReach=26,PodClears=1.9f;
            public const int PodsAShake=2;
            public const float StampRadius=3.2f,StampStride=3.6f,StampTell=.6f,StampDamage=48,StampSpent=1.6f;
            public const int Stamps=3;
            public const float CircleAt=7,CircleSpeed=8,CircleSeconds=1.1f;
            public const float RestPlanted=3,RestMobile=2.2f;
        }
        BossParts legs;
        float SeedthaneRest=>Phase>=2?SeedthaneTune.RestMobile:SeedthaneTune.RestPlanted;
        void SeedthaneStart()
        {
            float girth=GetComponent<CharacterController>().radius;
            legs=gameObject.AddComponent<BossParts>();
            legs.Build(health,"Seedthane - leg",SeedthaneTune.Legs,girth+.9f,.9f,.8f,brain.definition.integrity*SeedthaneTune.LegIntegrity,45,new Color(1.3f,.85f,.35f,.9f));
            legs.Broke+=SeedthaneLegBroke;
            var leaving=gameObject.AddComponent<BossDeparture>();leaving.sound="root_groan";leaving.speed=7;
        }
        void SeedthaneUpdate()
        {
            // Planted, it doesn't walk; its legs can be reached only while they are in the ground.
            brain.Rooted=Phase<2;bool reachable=Phase<2;
            if(legs!=null&&legs.Exposed!=reachable)legs.Expose(reachable);
            // A rigid body's life between moves: a planted thing sways low, one on its legs rides high and treads.
            if(!busy&&!health.Broken&&!brain.Telegraphing)Pose(1.5f*Mathf.Sin(Time.time*1.3f),Phase<2?-.25f:.15f+.07f*Mathf.Sin(Time.time*5));
        }
        void SeedthaneDown(){if(legs!=null)legs.Expose(false);}
        void SeedthaneUproots()
        {
            if(legs!=null)legs.Expose(false);brain.Rooted=false;
            CombatVfx.Burst(transform.position+Vector3.up*.4f,new Color(.7f,.6f,.42f),"kill");Debug.Log("SEEDTHANE_UPROOTS");
        }
        void SeedthaneLegBroke(BossParts parts,int index)
        {
            if(health==null||!health.Alive)return;
            bool last=parts.Standing==0;
            // A leg is a share of what holds it up, and goes off its bar with it. It can't be driven off by its legs alone: the last of its bar is its own.
            health.integrity=Mathf.Max(1,health.integrity-health.maximum*SeedthaneTune.LegBar);
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;
            float seconds=last?SeedthaneTune.Topples:SeedthaneTune.Lists;
            OpenFor(seconds);nextSpecial=Time.time+seconds+.6f;
            Tremor(last?.5f:.3f,last?.12f:.07f);AudioManager.PlayAt("root_groan",null,transform.position,.3f,AudioBus.SFX,50,.1f,last?.7f:1.1f);
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            Debug.Log(last?"SEEDTHANE_TOPPLED":"SEEDTHANE_LISTS");
            StartCoroutine(SeedthaneLists(seconds,last));
        }
        IEnumerator SeedthaneLists(float seconds,bool toppled)
        {
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.35f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-t)/.4f));
                Pose((toppled?26:13)*k,(toppled?-.7f:-.3f)*k);yield return null;
            }
            openingUntil=0;Pose(0);
        }

        (string,IEnumerator) ChooseSeedthane(CombatActor hero)
        {
            bool near=Flat(hero.transform.position-transform.position).magnitude<SeedthaneTune.FanReach+1;
            if(Phase<2)
            {
                // The volley is its one opening that isn't a leg: light at range, where its fans don't reach, and never left out for long.
                string planted=Pick(("ThornFan",near?2f:.4f),("FanVolley",near?1.4f:FullOffer),("PodShake",near?1.3f:2f));
                return planted switch{"FanVolley"=>(planted,ThornFan(SeedthaneTune.VolleyFans)),"PodShake"=>(planted,PodShake()),_=>(planted,ThornFan(1))};
            }
            string mobile=Pick(("CircleFan",2f),("StampAdvance",1.6f),("PodShake",1.1f));
            return mobile switch{"StampAdvance"=>(mobile,StampAdvance()),"PodShake"=>(mobile,PodShake()),_=>(mobile,CircleFan())};
        }
        /// <summary>Who stands behind something solid from here, at the height a fan sweeps: a fallen pod is 1.5 m of cover, and a fan stops at it as a bite stops at a wall.</summary>
        HashSet<Health> BehindCover()
        {
            var from=transform.position+Vector3.up;var covered=new HashSet<Health>();
            foreach(var member in PartyController.Current.members)if(!CombatCover.Clear(from,member.transform.position+Vector3.up*.9f))covered.Add(member.Health);
            return covered;
        }
        /// <summary>One fan: aimed as it is marked, struck where it was marked.</summary>
        IEnumerator Fan(float tell,bool heavy)
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var fan=Mark(Telegraph.Cone(transform.position,direction,SeedthaneTune.FanReach,SeedthaneTune.FanAngle,tell,heavy?Telegraph.Heavy:Telegraph.Threat));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-10,0,.03f);
            if(Interrupted)yield break;
            Pose(9);Strike(fan,SeedthaneTune.FanDamage,DamageType.Kinetic,.3f,1.8f,BehindCover());
            AudioManager.PlayAt("swing_1",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.04f,.7f);
            yield return Windup(.14f,0);
        }
        IEnumerator ThornFan(int fans)
        {
            for(int n=0;n<fans;n++)
            {
                yield return Fan(n>0?SeedthaneTune.FanNext:Phase>=2?SeedthaneTune.FanTellMobile:SeedthaneTune.FanTell,fans>1&&n==fans-1);
                if(Interrupted)yield break;
            }
            Pose(0);
            if(fans<=1){Finish(SeedthaneRest);yield break;}
            // Three in a row and it has thrown itself past its balance.
            Pose(14,-.15f);Debug.Log("SEEDTHANE_OVERREACHED");
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(SeedthaneTune.Overreached,SeedthaneRest);Pose(0);
        }
        IEnumerator CircleFan()
        {
            var controller=GetComponent<CharacterController>();
            for(int n=0;n<2;n++)
            {
                // A quarter-turn round him, one way and then the other, and the fan comes from where it stops.
                float side=(moveCount+n)%2==0?1:-1;
                Collide(false);
                for(float t=0;t<SeedthaneTune.CircleSeconds&&!Interrupted;)
                {
                    if(GameTime.Paused){yield return null;continue;}
                    float dt=Time.deltaTime;t+=dt;
                    var toHero=Flat(PartyController.Current.Active.transform.position-transform.position);float gap=toHero.magnitude;var inward=gap>.01f?toHero/gap:transform.forward;
                    var step=Vector3.Cross(Vector3.up,inward)*side+inward*Mathf.Clamp((gap-SeedthaneTune.CircleAt)*.4f,-1,1);
                    if(controller.enabled)controller.Move(step.normalized*(SeedthaneTune.CircleSpeed*dt));
                    transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(inward),360*dt);
                    Pose(4*Mathf.Sin(t*14),.25f);
                    yield return null;
                }
                Collide(true);
                if(Interrupted)yield break;
                yield return Fan(n==0?SeedthaneTune.FanTellMobile:SeedthaneTune.FanNext,false);
                if(Interrupted)yield break;
            }
            Pose(0);Finish(SeedthaneRest);
        }
        IEnumerator PodShake()
        {
            var party=PartyController.Current;float tell=SeedthaneTune.PodTell;
            var marks=new List<Telegraph>();var falling=new List<CoverPod>();var husks=new List<Transform>();
            // The pods hanging nearest the hero come down where they hang, each on its own marked circle: cover, from then on.
            for(int n=0;n<SeedthaneTune.PodsAShake;n++)
            {
                CoverPod chosen=null;float least=float.PositiveInfinity;
                foreach(var pod in CoverPod.All)
                {
                    if(pod==null||!pod.Hanging||falling.Contains(pod)||Flat(pod.transform.position-transform.position).sqrMagnitude>SeedthaneTune.PodReach*SeedthaneTune.PodReach)continue;
                    float gap=Flat(pod.transform.position-party.Active.transform.position).sqrMagnitude;if(gap<least){least=gap;chosen=pod;}
                }
                if(chosen==null)break;
                falling.Add(chosen);marks.Add(Mark(Telegraph.Circle(PodLanding(chosen),SeedthaneTune.PodRadius,tell,Telegraph.Threat)));
            }
            int hung=marks.Count;
            // And loose husks off its own back: one for the hero it is fighting, one for the other if he is in reach.
            var from=transform.position+Vector3.up*3.2f;
            foreach(var member in party.members)
            {
                if(!member.Health.Alive||member!=party.Active&&Flat(member.transform.position-transform.position).sqrMagnitude>400)continue;
                marks.Add(Mark(Telegraph.Circle(member.transform.position,SeedthaneTune.PodRadius,tell,Telegraph.Threat)));
                var husk=GameObject.CreatePrimitive(PrimitiveType.Sphere);husk.name="Loose husk";Destroy(husk.GetComponent<Collider>());husk.transform.localScale=new Vector3(.6f,.8f,.6f);
                husk.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(new Color(1,.86f,.5f,.9f));husk.transform.position=from;temporary.Add(husk);husks.Add(husk.transform);
            }
            AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.04f,1.3f);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/tell);
                // It shakes itself, hard, for the first half; the rest is everything falling.
                Pose(k<.5f?7*Mathf.Sin(t*38):0,0);
                for(int i=0;i<husks.Count;i++)husks[i].position=Vector3.Lerp(from,marks[hung+i].Origin,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*5);
                ThreatFrom(marks);
                yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var marking in marks){Strike(marking,SeedthaneTune.PodDamage,DamageType.Kinetic,.25f,0,once);CombatVfx.Burst(marking.Origin+Vector3.up*.3f,new Color(.8f,.7f,.42f),"kill");}
            foreach(var pod in falling)
            {
                // Nobody is left standing inside a pod: whoever was under it, hurt or rolled, is set clear in the frame it lands, before it is solid.
                var at=PodLanding(pod);
                foreach(var member in party.members)if(member.Health.Alive)SetClear(member,at);
                Physics.SyncTransforms();pod.Drop();
            }
            Tremor(.25f,.06f);ClearTemporary();Pose(0);strikeAt=0;
            Finish(SeedthaneRest);
        }
        static Vector3 PodLanding(CoverPod pod)=>pod.fallen!=null?pod.fallen.transform.position:pod.transform.position;
        /// <summary>Stands a hero just outside where a pod is about to be solid: straight out from it if that way is open, else the nearest way round that is. Placed, not walked: under
        /// a pod still on its stalk a body is held by the pod's own collider and a step goes nowhere, which left the hero inside the fallen pod for good.</summary>
        static void SetClear(CombatActor member,Vector3 at)
        {
            var away=Flat(member.transform.position-at);float gap=away.magnitude;if(gap>=SeedthaneTune.PodClears)return;
            var first=gap>.05f?away/gap:-member.transform.forward;var chest=member.transform.position+Vector3.up*.9f;
            foreach(float turn in new[]{0f,45,-45,90,-90,135,-135,180})
            {
                var stand=at+Quaternion.Euler(0,turn,0)*first*SeedthaneTune.PodClears;stand.y=member.transform.position.y;
                if(!CombatCover.Clear(chest,stand+Vector3.up*.9f,.3f))continue;
                var feet=member.GetComponent<CharacterController>();bool was=feet!=null&&feet.enabled;
                if(feet!=null)feet.enabled=false;member.transform.position=stand;if(feet!=null)feet.enabled=was;
                return;
            }
        }
        IEnumerator StampAdvance()
        {
            var controller=GetComponent<CharacterController>();
            for(int n=0;n<SeedthaneTune.Stamps;n++)
            {
                var hero=PartyController.Current.Active.transform.position;var direction=Toward(hero);transform.rotation=Quaternion.LookRotation(direction);
                // Each stamp is marked where it will come down, a stride on, and it gets there as the mark fills.
                float stride=Mathf.Min(SeedthaneTune.StampStride,Mathf.Max(0,Flat(hero-transform.position).magnitude-1));
                var ring=Mark(Telegraph.Circle(transform.position+direction*stride,SeedthaneTune.StampRadius,SeedthaneTune.StampTell,n==SeedthaneTune.Stamps-1?Telegraph.Heavy:Telegraph.Threat));
                strikeAt=Time.time+SeedthaneTune.StampTell;
                Collide(false);
                for(float t=0;t<SeedthaneTune.StampTell;)
                {
                    if(GameTime.Paused){yield return null;continue;}
                    float dt=Time.deltaTime;t+=dt;if(Interrupted)yield break;
                    float k=Mathf.Clamp01(t/SeedthaneTune.StampTell);Pose(-6*k,.6f*Mathf.Sin(k*Mathf.PI));
                    if(k>.5f&&controller.enabled)controller.Move(direction*(stride/(SeedthaneTune.StampTell*.5f)*dt));
                    yield return null;
                }
                Collide(true);
                Pose(6);Strike(ring,SeedthaneTune.StampDamage,DamageType.Kinetic,.3f,2);Tremor(.25f,.07f);
                AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.22f,AudioBus.SFX,50,.04f,1.1f);
                yield return Windup(.16f,0);
                if(Interrupted)yield break;
            }
            // The last stamp drives its legs in to the knee.
            strikeAt=0;Pose(10,-.2f);Debug.Log("SEEDTHANE_SPENT");
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            yield return Opening(SeedthaneTune.StampSpent,SeedthaneRest);Pose(0);
        }
    }
}
