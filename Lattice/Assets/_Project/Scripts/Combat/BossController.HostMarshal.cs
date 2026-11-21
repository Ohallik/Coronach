using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Host Marshal, chapter 6's boss: an Ancelane construct commander twice a guard's height, with a standard-spear and a cage-shield, woken at the bottom of the Muster's dig and
    /// still giving its last order, which is to muster. It is put back to sleep, not destroyed (<see cref="Ending.ShutDown"/>, and it kneels where it falls): the herders want their dead
    /// left in peace, and what the party is fighting is the thaw. The room's contract is in docs/maps/FrozenHost.md.
    /// <list type="bullet">
    /// <item><b>The muster</b> (to half). Three thaw couplings (<see cref="ThawCoupling"/>) feed heat into three thirds of the wall. It teaches its moves here. Spear Lunge, a marked lane it drives down at the hero: the lane ends at the first solid thing in it, and a lunge that strikes nobody
    /// leaves its spear in the floor, or for longer in the ice (the wall, or a soldier the party has put back to sleep). Shield Bash, a short marked fan before it that shoves; swung on
    /// nothing, its shield is wide a moment. Rally, its call: the rank of three waiting in the wall of the running coupling nearest the hero steps out where the wall marks them, once per
    /// third (the fight opens on it, so the couplings are taught first). While a coupling runs the rig keeps it warm and a blow on it lands at <see cref="MarshalTune.Shell"/>, read as resisted
    /// (an opening lands whole, and half again). Break a coupling and its third goes back to sleep: its rank, out or not, freezes where it stands and stays as ice, and a sixth of the
    /// Marshal's bar goes with it; the first two never carry it over half, the third always does, and that is the shift.</item>
    /// <item><b>The cold</b> (under half). The wall sleeps, frost comes in from the rim (<see cref="FrostRing"/>), and it is slower (its own pace at <see cref="MarshalTune.ColdPace"/>, its tells
    /// longer) and heavier (the phase's +18%). Cracking Sweep, a wide marked fan whose blow splits the floor down three marked lines a beat later, after which its heat fails it and it
    /// sinks to a knee, open; the lunge and the bash still, slower.</item>
    /// </list>
    /// It fights without couplings too (the proving yard): its call brings a rank out beside it twice, its bar holds nowhere, and its phase turns on integrity alone.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit HostMarshalKit=Register(new Kit
        {
            id="HostMarshal",title="THE HOST MARSHAL",epithet="An Ancelane commander, still giving its last order",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseMarshal(hero),
            moves={["SpearLunge"]=boss=>boss.SpearLunge(),["ShieldBash"]=boss=>boss.ShieldBash(),["Rally"]=boss=>boss.Rally(),["CrackingSweep"]=boss=>boss.CrackingSweep()},
            signature=phase=>phase>=2?"CrackingSweep":null,
            shift=boss=>boss.TheWallSleeps(),shiftText="THE WALL SLEEPS  ·  BRACE",
            status=boss=>boss.MarshalStatus(),
            fightGoal=(boss,label)=>boss.MarshalGoal(label),
            // It kneels and the frost takes it, which IdraMarshalSleeps follows: the shared "boss" bark ("Done. Count heads.") would land on a thing being put to rest (manifest 8.9).
            capsuleRadius=MarshalTune.Radius,occlusion=true,ending=Ending.ShutDown,downBark="",
            start=boss=>boss.MarshalStart(),update=boss=>boss.MarshalUpdate(),down=boss=>boss.MarshalDown(),
        });
        /// <summary>The Host Marshal's numbers, all of them (its integrity is its EnemyDef's: HoarfellBuilder.Enemies). Damage is before the cold phase's +18%; a boss stays at threat 1.
        /// Every blow is authored at 100 to 102: 12 to 16% of a Sync 17 or 18 hero's bar in the parts chapter 6 sells (HoarfellCombatTests measures it on all three heroes).</summary>
        public static class MarshalTune
        {
            /// <summary>Its capsule's radius; the capsule is twice as tall at least (its EnemyDef's capsuleHeight).</summary>
            public const float Radius=1.5f;
            /// <summary>A coupling's integrity, and the bar each takes with it, as shares of the boss's (three sixths carry it to half); and what of a blow on it gets through while one runs
            /// (.35 until November 14, when the replay driver beat it past half with two couplings of three still running: the couplings are the way through the warm half).</summary>
            public const float CouplingIntegrity=.06f,CouplingBar=1/6f,Shell=.3f;
            /// <summary>Spear Lunge: its tell warm and cold, the lane, the run, the blow; open with its spear in the floor, or longer in the ice.</summary>
            public const float LungeTell=.95f,LungeTellCold=1.2f,LungeLength=14,LungeWidth=2.4f,LungeSpeed=15,LungeDamage=102,Planted=2.2f,Lodged=2.5f;
            /// <summary>Shield Bash: its tell warm and cold, its reach past the body, its fan, the blow and the shove; open with its shield swung wide on nothing.</summary>
            public const float BashTell=.7f,BashTellCold=.9f,BashReach=3.2f,BashAngle=70,BashDamage=100,BashShove=3,Wide=1.2f;
            /// <summary>Rally: its tell, the marked rings where a rank steps out; how many to a rank, how many of them may stand before it calls again, and how often it calls in a room with no wall.</summary>
            public const float RallyTell=1.4f,RankRing=1.1f;
            public const int RankSize=3,StillOut=2,YardRallies=2;
            /// <summary>Cracking Sweep: the fan and its blow; the cracks it leaves, their tell and blow; how long it is down on one knee after.</summary>
            public const float SweepTell=1.15f,SweepReach=7,SweepAngle=150,SweepDamage=102,CrackTell=.85f,CrackLength=10,CrackWidth=1.6f,CrackSpread=40,CrackDamage=100,Falter=2f;
            public const int Cracks=3;
            /// <summary>The cold: its pace, the frost ring's rim and where it stops, how long it takes, and a hero's pace on it.</summary>
            public const float ColdPace=.6f,RingOuter=17,RingInner=9,RingSeconds=70,RingPace=.6f;
            /// <summary>Its rest between specials, warm and cold, and how long it takes to kneel when it is put to sleep.</summary>
            public const float RestWarm=2.6f,RestCold=3.1f,KneelSeconds=2.4f;
            /// <summary>How far it sinks into the frost and bows as it is put to sleep, and the same for the body a later visit finds (HoarfellBuilder.FrozenHost.Kneeling): its head has to
            /// come down a fifth of its height at least to be seen to go down, on a body this tall.</summary>
            public const float KneelSink=1.1f,KneelBow=12;
            /// <summary>Its break threshold while a coupling keeps it warm: nothing breaks it then (see <see cref="MarshalUpdate"/>).</summary>
            public const float HeldTogether=1e9f;
        }
        /// <summary>A rank's place on the floor is no blow: a pale boundary in the ice's own blue (D217, D222). Ice, for a soldier or the Marshal put back to sleep.</summary>
        static readonly Color RankTone=new(.72f,.86f,1f,.42f),IceTone=new(.66f,.82f,1f,1),ThawLight=new(1.4f,.8f,.32f);
        readonly List<(EnemyBrain guard,ThawCoupling from)> rallied=new();
        int yardRallies;bool hearsCouplings,stood;float marshalRadius=MarshalTune.Radius,breaksAt;string openWhy="";Vector3 stoodAt;Quaternion stoodTurned=Quaternion.identity;
        bool Cold=>Phase>=2;
        float MarshalRest=>Cold?MarshalTune.RestCold:MarshalTune.RestWarm;
        /// <summary>Soldiers it has called out of the wall who are still up.</summary>
        public int RalliedStanding{get{rallied.RemoveAll(r=>r.guard==null||!r.guard.Health.Alive);return rallied.Count;}}
        void MarshalStart()
        {
            marshalRadius=GetComponent<CharacterController>().radius;breaksAt=health.breakThreshold;
            // The couplings are a sixth of its bar each, so their integrity is the fight's to say: a share of its own, broken by a blow and shrugging off heat as a heater would.
            float each=health.maximum*MarshalTune.CouplingIntegrity;
            foreach(var coupling in ThawCoupling.All)
            {
                if(!coupling.Running)continue;
                var part=coupling.Health;part.maximum=part.integrity=each;part.weakness=DamageType.Kinetic;part.resistance=DamageType.Plasma;part.breakThreshold=1e7f;
            }
            ThawCoupling.Expose(true);ThawCoupling.Broke+=MarshalCouplingBroke;hearsCouplings=true;
            // While a coupling runs the rig keeps it warm: a blow on it lands at a share and reads as resisted. An opening (a caught spear, a wide shield) is a break, which the shell
            // never touches. A hard floor on its bar was tried first: the bar sat still under every blow, and a blow big enough to end the fight didn't.
            health.Plate=_=>Warm?MarshalTune.Shell:1;
            // The fight opens on its call, where there is a wall to call from: the couplings are taught before anything else.
            if(ThawCoupling.Waiting(transform.position)!=null)queuedMove="Rally";
        }
        /// <summary>Warm: before its shift, with a coupling running. Its blows are the same; a blow on it lands at <see cref="MarshalTune.Shell"/>.</summary>
        public bool Warm=>health!=null&&health.Alive&&Phase<2&&ThawCoupling.RunningCount>0;
        void MarshalCouplingBroke(ThawCoupling coupling)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){ThawCoupling.Broke-=MarshalCouplingBroke;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            // Its third of the wall goes back to sleep, and so does every soldier who came out of it, where they stand.
            for(int i=rallied.Count-1;i>=0;i--)if(rallied[i].from==coupling){Refreeze(rallied[i].guard);rallied.RemoveAt(i);}
            BarkService.Play(PartyController.Current.Active.character,"coupling",true);
            Tremor(.35f,.07f);AudioManager.PlayAt("root_groan",null,coupling.transform.position,.3f,AudioBus.SFX,50,.06f,.6f);
            if(shifting||HealthPhase>=2)return;
            bool last=ThawCoupling.RunningCount==0;float line=health.maximum*(Thresholds.Length>0?Thresholds[0]:.5f),after=health.integrity-health.maximum*MarshalTune.CouplingBar;
            if(health.integrity>line)health.integrity=Mathf.Max(1,last?Mathf.Min(after,line-1):Mathf.Max(after,line+1));
            Debug.Log($"MARSHAL_COLDER broken={ThawCoupling.BrokenCount}");
        }
        void MarshalUpdate()
        {
            // Where its body stood last frame, for the kneel: a death first hands the body back to the pose its fight began in (Interrupt), and a body that stood
            // lower than that was lifted to it and sunk back to where it stood, so its head did not come down at all (HoarfellCombatTests, November 14).
            if(visual!=null){stoodAt=visual.localPosition;stoodTurned=visual.localRotation;stood=true;}
            // Cold, its walk is slower: it claims a share of its own pace, as footing would.
            if(Cold)health.Hinder(MarshalTune.ColdPace);
            // Warm, the rig's heat holds it together: no blow breaks it, and its meter doesn't fill. Breaks were the fight's most generous openings: a break lets a blow past the
            // shell whole and half again (4.3 times a warm blow), and the replay driver broke it seven times in each of its first two fights (Builds/quality/nov14-recon4 and
            // -recon5, r36), so it beat it past half with one coupling of three broken and the fight was over in 94.8 s, under the two minutes. The couplings are how the wall is put
            // back to sleep; its moves still open it (a shield swung wide, a spear caught). Cold, it breaks as anything does.
            if(Warm){if(health.breakThreshold<MarshalTune.HeldTogether)breaksAt=health.breakThreshold;health.breakThreshold=MarshalTune.HeldTogether;health.ClearBreak();}
            else if(health.breakThreshold>=MarshalTune.HeldTogether)health.breakThreshold=breaksAt;
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            Pose(Cold?3+1.5f*Mathf.Sin(Time.time*1.3f):1.2f*Mathf.Sin(Time.time*.9f),Cold?-.12f:0);
            WorkRoundTheCamera();
        }
        string MarshalStatus()
        {
            if(health==null||!health.Alive)return "";
            if(health.Broken&&Time.time<openingUntil)return openWhy;
            return !Cold&&ThawCoupling.RunningCount>0?"THE RANKS ARE WAKING  ·  BREAK A COUPLING":Cold?"ITS HEAT IS FAILING":"";
        }
        string MarshalGoal(string label)
        {
            if(!Cold&&ThawCoupling.All.Count>0)return $"Break a coupling to put the wall back to sleep ({ThawCoupling.BrokenCount}/{ThawCoupling.All.Count})";
            return Cold?"Outlast the Host Marshal as its heat fails":label;
        }
        /// <summary>The last coupling is gone (or, with none, its own heat has run down): the wall is asleep, every soldier still out with it, and the frost comes in from the rim.</summary>
        void TheWallSleeps()
        {
            if(ThawCoupling.RunningCount>0)ThawCoupling.SettleAll();
            foreach(var (guard,_) in rallied)Refreeze(guard);rallied.Clear();
            var middle=ThawCoupling.Centre(out var centre)?centre:brain.Home;
            FrostRing.Lay(CombatCover.Ground(middle+Vector3.up*3),MarshalTune.RingOuter,MarshalTune.RingInner,MarshalTune.RingSeconds,MarshalTune.RingPace);
            AudioManager.PlayAt("root_groan",null,transform.position,.34f,AudioBus.SFX,40,.2f,.55f);Debug.Log("MARSHAL_COLD");
        }
        void MarshalDown()
        {
            if(hearsCouplings){ThawCoupling.Broke-=MarshalCouplingBroke;hearsCouplings=false;}
            ThawCoupling.SettleAll();
            foreach(var (guard,_) in rallied)Refreeze(guard);rallied.Clear();
            if(FrostRing.Current!=null)FrostRing.Current.Dismiss();
            // Put back to sleep, not destroyed: its body stays where it knelt, as ice, for the rest of the visit, instead of being cleared away as a beaten body is.
            var presentation=GetComponent<DefeatPresentation>();if(presentation!=null)Destroy(presentation);
            StartCoroutine(Kneel());
            Debug.Log("MARSHAL_ASLEEP");
        }
        /// <summary>Put to sleep it sinks into the frost and bows, its rig held where it fell (its animators off), and the frost takes it, colour and glow; the body a later visit finds
        /// stands as sunk and bowed (HoarfellBuilder.FrozenHost.Kneeling). It used to tip twelve degrees and sink 45 cm: on "It's kneeling" in the first player run, a body twice a
        /// guard's height had barely moved (its head 3.8 m over the floor, then 3.2). Played into its own Down clip first, the rig set its hips back over its feet whatever the body
        /// was moved by, and the head came down no further.</summary>
        IEnumerator Kneel()
        {
            var drawn=visual;if(drawn==null)yield break;
            foreach(var driver in drawn.GetComponentsInChildren<GeneratedAnimator>())driver.enabled=false;
            foreach(var motion in drawn.GetComponentsInChildren<ProceduralMotion>())motion.enabled=false;
            foreach(var animator in drawn.GetComponentsInChildren<Animator>())animator.enabled=false;
            // And what stands the rig on the floor: it writes the drawn body's place and turn every LateUpdate, and took the sink and the bow back each frame (its head
            // stood 3.79 m over the floor before and after, HoarfellCombatTests, November 14).
            foreach(var support in drawn.GetComponentsInChildren<GroundLifecycleSupport>())support.enabled=false;
            // From where the body stood as it fell, not from where it stood when the fight began (where the death's Interrupt has just put it back): held passive or
            // mid-move, those differ, and a kneel measured from the fight's first pose ended where the body already stood.
            var from=stood?stoodAt:drawn.localPosition;var turned=stood?stoodTurned:drawn.localRotation;
            var block=new MaterialPropertyBlock();var views=drawn.GetComponentsInChildren<Renderer>();
            for(float t=0;t<MarshalTune.KneelSeconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/MarshalTune.KneelSeconds));
                drawn.localRotation=turned*Quaternion.Euler(MarshalTune.KneelBow*k,0,0);drawn.localPosition=from+Vector3.down*(MarshalTune.KneelSink*k);
                foreach(var view in views)
                {
                    if(view==null)continue;view.GetPropertyBlock(block);
                    block.SetColor("_BaseColor",Color.Lerp(Color.white,IceTone,k));block.SetColor("_EmissionColor",Color.Lerp(ThawLight,Color.black,k));view.SetPropertyBlock(block);
                }
                yield return null;
            }
            Debug.Log($"MARSHAL_KNELT {drawn.name} at {drawn.localPosition}");
        }
        /// <summary>How near a hero's feet a frozen rank may stand and still be solid: its own half-width, a hero's, and room to walk between two of them.</summary>
        public const float RankRoom=1.6f;
        /// <summary>Whether a rank freezing here would stand in a hero's way out: any hero out, standing or down, within its box's reach and <see cref="RankRoom"/>.</summary>
        public static bool BesideAHero(Vector3 at,float radius)
        {
            var party=PartyController.Current;if(party==null||party.members==null)return false;
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.gameObject.activeInHierarchy)continue;
                var gap=hero.transform.position-at;gap.y=0;
                if(gap.magnitude<radius*.8f+RankRoom)return true;
            }
            return false;
        }
        /// <summary>A soldier put back to sleep where it stands: what's drawn stays, as ice, and is cover from then on (a solid box with no Health over it, unless a hero stands
        /// within reach of it: <see cref="BesideAHero"/>); the soldier itself goes without a death, so nothing pays for it and nothing it marked lands.</summary>
        public static GameObject Refreeze(EnemyBrain guard)
        {
            if(guard==null||!guard.Health.Alive)return null;
            var shape=guard.Controller;float radius=shape!=null?shape.radius:.55f,height=shape!=null?shape.height:2;
            var statue=new GameObject("Frozen rank");statue.transform.SetPositionAndRotation(guard.transform.position,guard.transform.rotation);
            var presentation=guard.GetComponent<DefeatPresentation>();var drawn=presentation!=null?presentation.visual:null;
            if(drawn!=null)
            {
                drawn.SetParent(statue.transform,true);
                foreach(var animator in drawn.GetComponentsInChildren<Animator>())animator.enabled=false;
                foreach(var motion in drawn.GetComponentsInChildren<ProceduralMotion>())motion.enabled=false;
                var block=new MaterialPropertyBlock();
                foreach(var view in drawn.GetComponentsInChildren<Renderer>()){view.GetPropertyBlock(block);block.SetColor("_BaseColor",IceTone);block.SetColor("_EmissionColor",Color.black);view.SetPropertyBlock(block);}
            }
            // Cover, unless it froze within a hero's reach: then it is ice to look at and nothing to walk into. Ranks fight at arm's length, so the three still out when the wall
            // went back to sleep froze round the lead where he stood by a thaw rig, and their boxes and the rig's made a cell: he spent the last sixty-five seconds of the fight
            // rolling against its walls and never left it afterwards (first player run, November 14).
            if(!BesideAHero(guard.transform.position,radius))
            {var box=statue.AddComponent<BoxCollider>();box.size=new Vector3(radius*1.6f,height,radius*1.6f);box.center=Vector3.up*height*.5f;}
            CombatVfx.Burst(guard.transform.position+Vector3.up*1.2f,IceTone,"crack");
            Telegraph.ClearAll(guard);Debug.Log("RANK_REFROZEN "+guard.definition.id);
            Destroy(guard.gameObject);
            return statue;
        }

        (string,IEnumerator) ChooseMarshal(CombatActor hero)
        {
            var to=Flat(hero.transform.position-transform.position);float distance=to.magnitude;bool close=distance<marshalRadius+MarshalTune.BashReach;
            if(!Cold)
            {
                // It calls while a third of the wall still has a rank to give (or, with no wall, twice), and not while two of the last are still up.
                bool wall=ThawCoupling.All.Count>0?ThawCoupling.Waiting(hero.transform.position)!=null:yardRallies<MarshalTune.YardRallies;
                string warm=Pick(("SpearLunge",distance>5?1.8f:.4f),("ShieldBash",close?2.2f:0),("Rally",wall&&RalliedStanding<MarshalTune.StillOut?1.2f:0));
                return warm switch{"ShieldBash"=>(warm,ShieldBash()),"Rally"=>(warm,Rally()),_=>(warm,SpearLunge())};
            }
            string cold=Pick(("CrackingSweep",distance<marshalRadius+MarshalTune.SweepReach+1?2:.5f),("SpearLunge",distance>6?1.4f:.3f),("ShieldBash",close?1.4f:0));
            return cold switch{"ShieldBash"=>(cold,ShieldBash()),"CrackingSweep"=>(cold,CrackingSweep()),_=>(cold,SpearLunge())};
        }
        /// <summary>Solid scenery ahead of it at a soldier's chest (not a body, not the floor): the ice it would drive its spear into, and how far off.</summary>
        bool IceAhead(Vector3 direction,float distance,out float gap)
        {
            gap=distance;var from=transform.position+Vector3.up*1.4f;
            if(!CombatCover.Sweep(from,from+direction*distance,.45f,out var hit))return false;
            gap=hit.distance;return true;
        }
        /// <summary>The marking lands as Strike lands it, and says how many it struck.</summary>
        int Landed(Telegraph marking,float amount,DamageType type,float stagger,float shove)
        {
            marking.Release();int landed=0;
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!marking.Contains(actor.transform.position))continue;
                if(actor.Health.Receive(Packet(amount,type))<=0)continue;
                landed++;if(stagger>0)actor.Stagger(stagger);
                if(shove>0)StartCoroutine(Shove(actor,Flat(actor.transform.position-marking.Origin),shove));
            }
            return landed;
        }
        IEnumerator Open(string why,float seconds)
        {
            openWhy=why;BarkService.Play(PartyController.Current.Active.character,"bossopening",true);
            yield return Opening(seconds,MarshalRest);
        }
        IEnumerator SpearLunge()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Cold?MarshalTune.LungeTellCold:MarshalTune.LungeTell,reach=MarshalTune.LungeLength;
            // The lane ends at the first solid thing in it (the wall's ice, a rig, a rank frozen where it stood): what's painted is where the spear goes.
            if(IceAhead(direction,reach+marshalRadius,out float gap))reach=Mathf.Max(2,gap-marshalRadius*.5f);
            var lane=Mark(Telegraph.Line(transform.position,direction,reach+marshalRadius,MarshalTune.LungeWidth,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-9,.15f,.03f);
            if(Interrupted)yield break;
            lane.Release();Collide(false);Pose(10);
            AudioManager.PlayAt("thrusterFire_000",transform,transform.position+Vector3.up*2,.24f,AudioBus.SFX,50,.05f,.6f);
            var controller=GetComponent<CharacterController>();var struck=new HashSet<Health>();float travelled=0;bool lodged=false;
            while(travelled<reach)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(Interrupted){Collide(true);yield break;}
                float step=Mathf.Min(MarshalTune.LungeSpeed*Time.deltaTime,reach-travelled);
                if(IceAhead(direction,step+marshalRadius+.3f,out _)){lodged=true;break;}
                var before=transform.position;if(controller.enabled)controller.Move(direction*step);
                float moved=Flat(transform.position-before).magnitude;travelled+=Mathf.Max(moved,step*.25f);
                Contact(struck,transform.position+direction*(marshalRadius*.6f),marshalRadius+.7f,MarshalTune.LungeDamage,.4f,lane);
                if(moved<step*.2f&&travelled>1){lodged=true;break;}
                yield return null;
            }
            Collide(true);
            if(struck.Count>0){yield return Windup(.25f,0);Pose(0);Finish(MarshalRest);yield break;}
            // It struck nobody: its spear is in the floor at the end of its reach, or in the ice it ran into, and it has to wrench it free.
            Pose(16,-.3f);Tremor(.3f,.08f);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+direction*marshalRadius+Vector3.up,.3f,AudioBus.SFX,50,.05f,.6f);
            CombatVfx.Burst(transform.position+direction*(marshalRadius+1)+Vector3.up*.6f,IceTone,"kill");
            Debug.Log("MARSHAL_LODGED "+(lodged?"ice":"floor"));
            yield return Open("ITS SPEAR IS CAUGHT  ·  STRIKE",lodged?MarshalTune.Lodged:MarshalTune.Planted);Pose(0);
        }
        IEnumerator ShieldBash()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Cold?MarshalTune.BashTellCold:MarshalTune.BashTell;
            var fan=Mark(Telegraph.Cone(transform.position,direction,marshalRadius+MarshalTune.BashReach,MarshalTune.BashAngle,tell,Telegraph.Threat));
            strikeAt=Time.time+tell;
            yield return Windup(tell,-6,.1f,.03f);
            if(Interrupted)yield break;
            Pose(9);int took=Landed(fan,MarshalTune.BashDamage,DamageType.Kinetic,.35f,MarshalTune.BashShove);strikeAt=0;
            Tremor(.25f,.06f);AudioManager.PlayAt("impactMetal_000",transform,transform.position+direction*marshalRadius+Vector3.up*1.5f,.28f,AudioBus.SFX,50,.05f,.8f);
            if(took>0){yield return Windup(.25f,0);Pose(0);Finish(MarshalRest);yield break;}
            // Swung on nothing: its shield is wide of it for a moment.
            Pose(-5);Debug.Log("MARSHAL_WIDE");
            yield return Open("ITS SHIELD IS WIDE  ·  STRIKE",MarshalTune.Wide);Pose(0);
        }
        IEnumerator Rally()
        {
            var lead=PartyController.Current.Active.transform.position;var source=ThawCoupling.Waiting(lead);var spots=new List<Vector3>();
            if(source!=null&&source.ranks!=null)foreach(var rank in source.ranks)if(rank!=null)spots.Add(rank.position);
            if(spots.Count==0&&(source!=null||ThawCoupling.All.Count==0))
            {
                // No wall to call from (the proving yard), or no places set for its rank: they step out beside it, three abreast before it.
                var ahead=Toward(lead);var right=Vector3.Cross(Vector3.up,ahead);
                for(int i=0;i<MarshalTune.RankSize;i++)spots.Add(SummonPoint(ahead*4.5f+right*((i-1)*2.4f)));
            }
            if(spots.Count==0){Finish(1);yield break;}
            if(source==null)yardRallies++;
            var rings=new List<Telegraph>();foreach(var spot in spots)rings.Add(Mark(Telegraph.Circle(spot,MarshalTune.RankRing,MarshalTune.RallyTell,RankTone)));
            strikeAt=0;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*3,.3f,AudioBus.SFX,50,.05f,.45f);
            if(source!=null)foreach(var light in source.GetComponentsInChildren<Light>())CombatVfx.Burst(light.transform.position,ThawLight,"steam");
            // "It's calling the wall": the party's own line for this boss's call (bossopening.HostMarshal), which is what the call opens.
            BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
            Debug.Log("MARSHAL_RALLY "+(source!=null?source.index:0));
            yield return Windup(MarshalTune.RallyTell,-12,.35f,.02f);
            if(Interrupted)yield break;
            foreach(var ring in rings)ring.Release();
            var definition=GameCatalog.Find<EnemyDef>("ThawedGuard");
            if(definition!=null)
            {
                if(source!=null)source.rankWoken=true;
                foreach(var spot in spots)
                {
                    var guard=ActorFactory.Enemy(definition,spot+Vector3.up*.05f);var face=Flat(lead-spot);
                    if(face.sqrMagnitude>.01f)guard.transform.rotation=Quaternion.LookRotation(face);
                    rallied.Add((guard,source));CombatVfx.Burst(spot+Vector3.up*1.2f,ThawLight,"kill");
                }
                Debug.Log("BOSS_SUMMON "+health.id+" "+rallied.Count);
            }
            Pose(0);Finish(MarshalRest);
        }
        IEnumerator CrackingSweep()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var fan=Mark(Telegraph.Cone(transform.position,direction,marshalRadius+MarshalTune.SweepReach,MarshalTune.SweepAngle,MarshalTune.SweepTell,Telegraph.Heavy));
            strikeAt=Time.time+MarshalTune.SweepTell;
            yield return Windup(MarshalTune.SweepTell,-11,.25f,.05f);
            if(Interrupted)yield break;
            Pose(12,-.1f);Strike(fan,MarshalTune.SweepDamage,DamageType.Kinetic,.35f);Tremor(.45f,.1f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+direction*3,.3f,AudioBus.SFX,50,.05f,.7f);
            // The floor it struck splits: three lines out through the fan, the middle one down its heading, all erupting a beat later.
            var cracks=new List<Telegraph>();
            for(int i=0;i<MarshalTune.Cracks;i++)
            {
                float turn=(i-(MarshalTune.Cracks-1)*.5f)*MarshalTune.CrackSpread;var way=Quaternion.Euler(0,turn,0)*direction;
                cracks.Add(Mark(Telegraph.Line(transform.position+way*marshalRadius,way,MarshalTune.CrackLength,MarshalTune.CrackWidth,MarshalTune.CrackTell,Telegraph.Threat)));
            }
            for(float t=0;t<MarshalTune.CrackTell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                ThreatFrom(cracks);yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var crack in cracks)
            {
                Strike(crack,MarshalTune.CrackDamage,DamageType.Kinetic,.3f,0,once);
                for(int i=1;i<=3;i++)CombatVfx.Burst(crack.Origin+crack.Forward*(crack.Length*i/4f)+Vector3.up*.3f,IceTone,"kill");
            }
            strikeAt=0;Tremor(.35f,.08f);AudioManager.PlayAt("explosionCrunch_000",null,transform.position+direction*6,.26f,AudioBus.SFX,50,.05f,.55f);
            // Its heat fails with the blow: it sinks to a knee with the frost up its legs, and is open until it rises.
            Pose(18,-.7f);Debug.Log("MARSHAL_FALTERS");
            yield return Open("ITS HEAT FAILS  ·  STRIKE",MarshalTune.Falter);Pose(0);
        }
    }
}
