using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// How an enemy past the six archetypes fights: one of a small set of families (a charger, a lobber, an ambusher, a swarm cloud, a guardian, an anchored body, a tetherer, a
    /// strafer, a stomper, a burster), picked by <see cref="EnemyDef.behaviour"/> and tuned by <see cref="EnemyDef.tune"/> and the definition's attacks, so a later chapter's enemy is data and not a
    /// branch on its id. A behaviour is not a component: <see cref="EnemyBrain"/> makes one in its Start and hands it each frame the body is awake, alive, unbroken and has a hero
    /// to fight (<see cref="Tick"/>). It fights through the brain's own tell (<see cref="EnemyBrain.BeginTell"/>): the marking on the ground, the clock the partner and the replay
    /// driver dodge by, the wind-up a rigid body leans through and a rigged one plays, and the lessons, are the same ones every ordinary attack has always had. The rules hold for
    /// every family: a hit lands only inside a marking that stood at least half a second, an ordinary enemy's hit stops at solid cover (D207), and what is left on the floor is a
    /// boundary unless it damages (D217, D222).
    /// A new family is a class here, a name on <see cref="EnemyBehaviourKind"/> (append only) and an arm of <see cref="For"/>. CHAPTER_GUIDE section 5 lists what each reads.
    /// </summary>
    public abstract class EnemyBehaviour
    {
        protected EnemyBrain brain;protected EnemyDef def;protected Transform body,visual;protected Health health;protected CharacterController controller;
        /// <summary>Its first attack: the tell, the reach, the damage. A family that has a second move reads <see cref="Second"/>.</summary>
        protected AttackDef attack;
        protected AttackDef Second=>def.attacks!=null&&def.attacks.Length>1&&def.attacks[1]!=null?def.attacks[1]:attack;
        protected BehaviourTune tune=>def.tune;
        /// <summary>While true the body's pose is this behaviour's to say (EnemyTellPose eases a rigid body to it): pitch and roll in degrees, a swell as a share of its size.
        /// False, it rears back through a tell and follows through, as every body does.</summary>
        public bool Posing{get;protected set;}
        public float Pitch{get;protected set;}
        public float Roll{get;protected set;}
        public float Swell{get;protected set;}
        /// <summary>Whether a skill may drag the body now (a hook's reel, a Fold's pull).</summary>
        public virtual bool Movable=>true;
        /// <summary>Whether it walks home when the party has left it 35 m behind, as an ordinary body does. One that keeps a place of its own (a hide, a post, a root) doesn't.</summary>
        public virtual bool Roams=>true;
        /// <summary>At nought it isn't killed: it stands aside (a person doing a job, chapter 9's Compact Warden). Its fall is no kill to anyone: no "one less", no burst, no death sound.</summary>
        public virtual bool Yields=>false;

        public static EnemyBehaviour For(EnemyBrain brain)
        {
            if(brain.definition==null)return null;
            EnemyBehaviour made=brain.definition.behaviour switch
            {
                EnemyBehaviourKind.Charger=>new EnemyCharger(),
                EnemyBehaviourKind.Lobber=>new EnemyLobber(),
                EnemyBehaviourKind.Ambusher=>new EnemyAmbusher(),
                EnemyBehaviourKind.SwarmCloud=>new EnemySwarmCloud(),
                EnemyBehaviourKind.Guardian=>new EnemyGuardian(),
                EnemyBehaviourKind.Anchored=>new EnemyAnchored(),
                EnemyBehaviourKind.Tetherer=>new EnemyTetherer(),
                EnemyBehaviourKind.Strafer=>new EnemyStrafer(),
                EnemyBehaviourKind.Stomper=>new EnemyStomper(),
                EnemyBehaviourKind.Burster=>new EnemyBurster(),
                EnemyBehaviourKind.Leaper=>new EnemyLeaper(),
                EnemyBehaviourKind.Rider=>new EnemyRider(),
                EnemyBehaviourKind.Spinner=>new EnemySpinner(),
                EnemyBehaviourKind.Tender=>new EnemyTender(),
                _=>null,
            };
            if(made==null)return null;
            made.brain=brain;made.def=brain.definition;made.body=brain.transform;made.health=brain.Health;made.controller=brain.Controller;
            made.attack=made.def.attacks!=null&&made.def.attacks.Length>0?made.def.attacks[0]:null;
            var presentation=brain.GetComponent<DefeatPresentation>();made.visual=presentation!=null?presentation.visual:null;
            made.Begin();return made;
        }
        /// <summary>Once, as the body wakes: what it carries (a plate, a line, its motes).</summary>
        protected virtual void Begin(){}
        /// <summary>Each frame the body is awake, alive, unbroken, unwarded, out of a safe pocket and has a living hero to fight.</summary>
        public abstract void Tick(CombatActor victim);
        /// <summary>Each frame, late, whatever its state: what is drawn.</summary>
        public virtual void Present(){}
        /// <summary>Its move was taken from it (a break, a hook's reel, a drag off its marking, its death): back to the start, with nothing of the move left behind.</summary>
        public virtual void Cancel(){}
        /// <summary>A hit landed on it and it lives.</summary>
        public virtual void Struck(DamagePacket packet,float amount){}
        /// <summary>It has fallen: what it leaves, frees or sets off.</summary>
        public virtual void Fell(DamagePacket packet){}
        /// <summary>The body is being removed, dead or not.</summary>
        public virtual void Dispose(){}
        /// <summary>The body has been carried this far by the world (<see cref="EnemyBrain.Carry"/>: a current): a place the behaviour keeps of its own goes with it. Nothing, unless a family says.</summary>
        public virtual void Carried(Vector3 delta){}

        // ---------- shared pieces ----------
        protected static Vector3 Flat(Vector3 v){v.y=0;return v;}
        protected static float Or(float value,float fallback)=>value>0?value:fallback;
        protected static bool Flying=>ZoneController.Current!=null&&ZoneController.Current.Flight;
        protected float Tell=>attack!=null?Mathf.Max(.5f,attack.telegraph):.65f;
        protected float Range=>attack!=null?attack.range:2.6f;
        protected float Cooldown=>attack!=null?attack.cooldown:1.6f;
        /// <summary>Where a strike is measured from: the height a bite, a lunge and a shot's muzzle have always used.</summary>
        protected Vector3 Chest=>body.position+Vector3.up*(Flying?0:1);
        /// <summary>Begins the tell with this family's sound, or the definition's own.</summary>
        protected void Mark(Telegraph marking,float seconds,Vector3 toward,CombatActor victim,string sound,float pitch,bool shooter=false)=>
            brain.BeginTell(marking,seconds,toward,victim,string.IsNullOrEmpty(tune.tellSound)?sound:tune.tellSound,tune.tellPitch>0?tune.tellPitch:pitch,shooter);
        /// <summary>The marking lands: every hero inside it with nothing solid between them and where it came from is struck once. It answers how many it hurt.</summary>
        protected int StrikeIn(Telegraph marking,DamagePacket packet,Vector3 from,List<CombatActor> struck=null)
        {
            int landed=0;if(marking==null||PartyController.Current==null)return 0;
            foreach(var hero in PartyController.Current.members)
            {
                if(!hero.Health.Alive||!marking.Contains(hero.transform.position)||!CombatCover.Clear(from,hero.transform.position+Vector3.up*(hero.flight?0:.9f)))continue;
                if(hero.Health.Receive(packet)<=0)continue;
                landed++;struck?.Add(hero);
            }
            return landed;
        }
        /// <summary>A moving body's contact: whoever it touches inside the lane it marked, once each. A released marking is gone from the scene and still knows its shape.</summary>
        protected int Touch(HashSet<Health> met,float radius,DamagePacket packet,Telegraph lane)
        {
            int landed=0;if(PartyController.Current==null)return 0;
            foreach(var hero in PartyController.Current.members)
            {
                if(!hero.Health.Alive||met.Contains(hero.Health)||Flat(hero.transform.position-body.position).sqrMagnitude>=radius*radius)continue;
                if(!ReferenceEquals(lane,null)&&!lane.Contains(hero.transform.position))continue;
                if(!CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f)))continue;
                met.Add(hero.Health);if(hero.Health.Receive(packet)>0)landed++;
            }
            return landed;
        }
        readonly List<CharacterController> passing=new();
        /// <summary>A charge or a pass goes through the party (its contact still strikes) instead of stalling against a hero; solid again when it ends.</summary>
        protected void PassThrough(bool through)
        {
            if(controller==null)return;
            if(through&&PartyController.Current!=null)
                foreach(var member in PartyController.Current.members)
                {var other=member.GetComponent<CharacterController>();if(other!=null&&!passing.Contains(other)){Physics.IgnoreCollision(controller,other,true);passing.Add(other);}}
            if(!through){foreach(var other in passing)if(other!=null)Physics.IgnoreCollision(controller,other,false);passing.Clear();}
        }
        static readonly RaycastHit[] Ahead=new RaycastHit[16];
        /// <summary>Solid scenery (not a body, not the ground underfoot) within a distance ahead of the body: what a charge sticks in. It answers how far off it stands.</summary>
        protected bool Wall(Vector3 direction,float distance,out float gap)
        {
            gap=distance;bool found=false;float radius=Mathf.Max(.25f,controller!=null?controller.radius*.8f:.4f);
            int count=Physics.SphereCastNonAlloc(Chest+Vector3.up*(Flying?0:radius*.5f),radius,direction,Ahead,distance,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=Ahead[i];
                if(hit.collider.transform.IsChildOf(body)||hit.collider is CharacterController||hit.collider.GetComponentInParent<Health>()!=null)continue;
                if(hit.distance<=0&&hit.point==Vector3.zero)continue;
                // Only a face that looks back along the run: the floor it runs on is not a wall.
                if(Vector3.Dot(hit.normal,-direction)>.45f&&hit.distance<gap){gap=hit.distance;found=true;}
            }
            return found;
        }
        /// <summary>Turns the body toward a heading at a rate, as a heavy thing turns: "behind it" is then a place a player can get to.</summary>
        protected void Face(Vector3 toward,float degreesPerSecond)
        {
            toward.y=0;if(toward.sqrMagnitude<.0001f)return;
            body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(toward),degreesPerSecond*Time.deltaTime);
        }
        /// <summary>A sound that is not the tell: from what's drawn, so it places like the body's own and nothing mistakes it for a warning.</summary>
        protected void Sound(string key,float volume,float pitch=1)=>AudioManager.PlayAt(key,visual!=null?visual:null,body.position+Vector3.up,volume,AudioBus.SFX,110,.08f,pitch);
        /// <summary>Stood open for a time, as a wedged boss is: a break's half-again on every hit, without spending its break meter. The brain doesn't tick it meanwhile.</summary>
        protected void Open(float seconds)=>health.Expose(seconds);
        /// <summary>What this body has left on the floor (a scorch, a pour, slag) that is still there: a family that must not leave it behind its body takes it up with <see cref="TakeUp"/>.</summary>
        readonly List<LingeringArea> laid=new();
        protected LingeringArea Leave(LingeringArea area){if(area!=null){laid.RemoveAll(a=>a==null);laid.Add(area);}return area;}
        /// <summary>Everything it left on the floor goes with it: nothing a body does may outlive it into whatever comes next (a fixture's next body, a test's next turn).</summary>
        protected void TakeUp(){foreach(var area in laid)if(area!=null)area.Dismiss();laid.Clear();}
    }
}
