using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the three thaw couplings round the Frozen Host's bottom gallery (chapter 6): a unit of the Muster's thaw rig feeding heat into one third of the wall, where a rank of the
    /// Ancelane host stands frozen. It is a break target, not an enemy: it has integrity and a hurtbox, pays a swing what a swing earns and no kill's worth, and its breaking is kept in the
    /// save. What breaking one does is its boss's to say (the Host Marshal hears <see cref="Broke"/>); what it does to the room is its own: its third of the wall goes from amber to blue
    /// (<see cref="thawing"/> and <see cref="refrozen"/>), its rig goes dark (<see cref="whole"/> and <see cref="broken"/>), and its rank, out or not, goes back to sleep.
    /// A coupling that names its boss (<see cref="settledBy"/>) is shut from the moment the zone loads, so it takes no blow and is no target until that boss is awake and opens it; once the
    /// boss is down every coupling loads broken. The room it stands in: docs/maps/FrozenHost.md, "Boss arena contract".
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ThawCoupling:MonoBehaviour
    {
        public static readonly List<ThawCoupling> All=new();
        /// <summary>A coupling has just broken (by a blow, not settled), and <see cref="Running"/> already says so.</summary>
        public static event Action<ThawCoupling> Broke;
        /// <summary>The rig running and the rig iced over; its third of the wall thawing and the same third refrozen. One of each pair is shown at a time; any can be missing.</summary>
        public GameObject whole,broken,thawing,refrozen;
        /// <summary>Where its rank of soldiers steps out of the wall, on the floor: the boss's rally calls them out here. None: the boss calls them beside itself.</summary>
        public Transform[] ranks;
        /// <summary>Set in the save as it breaks. Empty: it runs again on every load.</summary>
        public string defeatedFlag;
        /// <summary>The boss whose fall settles every coupling, and who opens them as it wakes.</summary>
        public string settledBy;
        /// <summary>Which coupling this is (1, 2, 3): what its flag and the fight's count go by, never the order the scene woke them in.</summary>
        public int index;
        /// <summary>Its rank has been called out of the wall this visit: a sector answers one rally.</summary>
        [NonSerialized] public bool rankWoken;
        public Health Health{get;private set;}
        public bool Running=>Health!=null&&Health.Alive;
        public static int RunningCount{get{int count=0;foreach(var coupling in All)if(coupling.Running)count++;return count;}}
        public static int BrokenCount{get{int count=0;foreach(var coupling in All)if(!coupling.Running)count++;return count;}}
        void Awake()
        {
            Health=GetComponent<Health>();
            Health.Damaged+=(_,packet,amount)=>CombatActor.PayHit(packet,amount);
            Health.Died+=(_,__)=>
            {
                if(GameServices.Current!=null&&!string.IsNullOrEmpty(defeatedFlag))GameServices.Current.Flags.SetBool(defeatedFlag,true);
                CombatVfx.Burst(transform.position+Vector3.up*1.6f,new Color(.7f,.9f,1.3f),"kill");
                AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up*1.5f,.26f,AudioBus.SFX,50,.06f,.75f);
                Show();Debug.Log($"COUPLING_BROKEN {index} running={RunningCount}");
                Broke?.Invoke(this);
            };
        }
        void Start()
        {
            if(GameServices.Current!=null)
            {
                var flags=GameServices.Current.Flags;
                // A loaded save is a quiet state: no burst, no sound, no event.
                if(!string.IsNullOrEmpty(settledBy)&&flags.GetBool("bossdown."+settledBy)||!string.IsNullOrEmpty(defeatedFlag)&&flags.GetBool(defeatedFlag))Health.integrity=0;
            }
            if(Running&&!string.IsNullOrEmpty(settledBy))Shut(true);
            Show();
        }
        void Shut(bool shut){Health.Damageable=!shut;foreach(var shape in GetComponents<Collider>())shape.enabled=!shut||!shape.isTrigger;}
        /// <summary>Whether blows land on the couplings still running: their boss opens them as it wakes.</summary>
        public static void Expose(bool open){foreach(var coupling in All)if(coupling.Running)coupling.Shut(!open);}
        /// <summary>Every coupling down without a blow and without the save's flags: the boss is asleep.</summary>
        public static void SettleAll(){foreach(var coupling in All){if(coupling.Running)coupling.Health.integrity=0;coupling.Show();}}
        /// <summary>The running coupling nearest a point whose rank is still in the wall, or none.</summary>
        public static ThawCoupling Waiting(Vector3 point)
        {
            ThawCoupling best=null;float least=float.PositiveInfinity;
            foreach(var coupling in All)
            {
                if(!coupling.Running||coupling.rankWoken)continue;
                var offset=coupling.transform.position-point;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=coupling;
            }
            return best;
        }
        /// <summary>The middle of the gallery: the centre of the circle the couplings stand on (they stand at one distance from it, wherever round it they are), or with fewer than three
        /// their average; none with no couplings.</summary>
        public static bool Centre(out Vector3 centre)
        {
            centre=Vector3.zero;if(All.Count==0)return false;
            foreach(var coupling in All)centre+=coupling.transform.position;centre/=All.Count;
            if(All.Count<3)return true;
            Vector2 a=new(All[0].transform.position.x,All[0].transform.position.z),b=new(All[1].transform.position.x,All[1].transform.position.z),c=new(All[2].transform.position.x,All[2].transform.position.z);
            float d=2*(a.x*(b.y-c.y)+b.x*(c.y-a.y)+c.x*(a.y-b.y));if(Mathf.Abs(d)<.001f)return true;
            float x=(a.sqrMagnitude*(b.y-c.y)+b.sqrMagnitude*(c.y-a.y)+c.sqrMagnitude*(a.y-b.y))/d,z=(a.sqrMagnitude*(c.x-b.x)+b.sqrMagnitude*(a.x-c.x)+c.sqrMagnitude*(b.x-a.x))/d;
            centre=new Vector3(x,centre.y,z);return true;
        }
        void Show()
        {
            bool running=Running;
            if(whole!=null)whole.SetActive(running);if(broken!=null)broken.SetActive(!running);
            if(thawing!=null)thawing.SetActive(running);if(refrozen!=null)refrozen.SetActive(!running);
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
    }
}
