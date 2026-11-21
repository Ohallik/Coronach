using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the Rotbloom's four lure-stalks, standing on the plinths at the great hall's rim (chapter 4): a lamp it grew to fish with, and a break target. It is something to burn, not an
    /// enemy: it has integrity and a hurtbox, pays a swing what a swing earns and no kill's worth, and its burning is kept in the save. What burning one does is its boss's to say: it hears
    /// <see cref="Burned"/>. A stalk that names its boss (<see cref="settledBy"/>) is shut from the moment the zone loads, so it takes no blow and is no target until that boss is awake and
    /// opens it (chapter 3's gates could be broken from the door before the fight); once the boss is down every stalk loads burned.
    /// Its light flickers as a lure's does (<see cref="Flicker"/>): shorts that never hold a long, which is how Driftreed's lamps, two short and one long, are told from it. The boss can make
    /// one blaze for a move (<see cref="Flare"/>).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class LureStalk:MonoBehaviour
    {
        public static readonly List<LureStalk> All=new();
        /// <summary>A stalk has just burned, and <see cref="Standing"/> already says so.</summary>
        public static event Action<LureStalk> Burned;
        /// <summary>The stalk standing, and what's left of it: one is shown at a time. Either can be missing. Lights under <see cref="whole"/> are the lure's own and flicker.</summary>
        public GameObject whole,burned;
        /// <summary>Set in the save as it burns. Empty: it stands again on every load.</summary>
        public string defeatedFlag;
        /// <summary>The boss whose fall settles every stalk, and who opens them as it wakes.</summary>
        public string settledBy;
        /// <summary>Which stalk this is, counted round the rim: what a boss's pattern goes by, so it never depends on the order the scene woke them in.</summary>
        public int index;
        public Health Health{get;private set;}
        public bool Standing=>Health!=null&&Health.Alive;
        public static int StandingCount{get{int count=0;foreach(var stalk in All)if(stalk.Standing)count++;return count;}}
        Light[] lights;float[] bright;float flareUntil;
        void Awake()
        {
            Health=GetComponent<Health>();
            Health.Damaged+=(_,packet,amount)=>CombatActor.PayHit(packet,amount);
            Health.Died+=(_,__)=>
            {
                if(GameServices.Current!=null&&!string.IsNullOrEmpty(defeatedFlag))GameServices.Current.Flags.SetBool(defeatedFlag,true);
                CombatVfx.Burst(transform.position+Vector3.up*2.4f,new Color(1.4f,.6f,.25f),"kill");
                AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up*2,.26f,AudioBus.SFX,50,.06f,1.2f);
                Show();Debug.Log("STALK_BURNED "+name);
                Burned?.Invoke(this);
            };
            lights=whole!=null?whole.GetComponentsInChildren<Light>(true):Array.Empty<Light>();bright=new float[lights.Length];for(int i=0;i<lights.Length;i++)bright[i]=lights[i].intensity;
        }
        void Start()
        {
            if(GameServices.Current!=null)
            {
                var flags=GameServices.Current.Flags;
                // A loaded save is a quiet state: no burst, no sound, no event.
                if(!string.IsNullOrEmpty(settledBy)&&flags.GetBool("bossdown."+settledBy)||!string.IsNullOrEmpty(defeatedFlag)&&flags.GetBool(defeatedFlag))Health.integrity=0;
            }
            if(Standing&&!string.IsNullOrEmpty(settledBy))Shut(true);
            Show();
        }
        void Shut(bool shut){Health.Damageable=!shut;foreach(var shape in GetComponents<Collider>())shape.enabled=!shut||!shape.isTrigger;}
        /// <summary>Whether blows land on the stalks still standing: their boss opens them as it wakes.</summary>
        public static void Expose(bool open){foreach(var stalk in All)if(stalk.Standing)stalk.Shut(!open);}
        /// <summary>The standing stalk nearest a point, within a distance on the plane, or none.</summary>
        public static LureStalk Nearest(Vector3 point,float within=float.PositiveInfinity)
        {
            LureStalk best=null;float least=within*within;
            foreach(var stalk in All)
            {
                if(!stalk.Standing)continue;
                var offset=stalk.transform.position-point;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=stalk;
            }
            return best;
        }
        /// <summary>Every stalk down without a blow and without the save's flags: the hall drained, or the boss beaten.</summary>
        public static void SettleAll(){foreach(var stalk in All){if(stalk.Standing)stalk.Health.integrity=0;stalk.Show();}}
        /// <summary>Blazing for a moment: the lure the boss is flashing.</summary>
        public void Flare(float seconds)=>flareUntil=Time.time+seconds;
        void Show(){bool standing=Standing;if(whole!=null)whole.SetActive(standing);if(burned!=null)burned.SetActive(!standing);}
        void Update()
        {
            if(lights.Length==0||!Standing||GameTime.Paused)return;
            float k=Time.time<flareUntil?2.4f:Flicker(Time.time,index*7+3);
            for(int i=0;i<lights.Length;i++)if(lights[i]!=null)lights[i].intensity=bright[i]*k;
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>A lure's light at a moment, as a share of its full light: a run of short flashes and a gap, three or four of them by turns, and never a long one ("nothing hungry
        /// waits"). Driftreed's own lamps keep two short and one long, so a lamp that never holds is a lure. Each lure keeps its own clock by its seed.</summary>
        public static float Flicker(float time,int seed)
        {
            const float On=.11f,Gap=.17f,Period=1.7f;
            float t=time+(seed&255)*.137f;int cycle=Mathf.FloorToInt(t/Period);float into=t-cycle*Period;
            int flashes=3+((cycle+seed)&1);int n=Mathf.FloorToInt(into/(On+Gap));
            return n<flashes&&into-n*(On+Gap)<On?1.6f:.14f;
        }
    }
}
