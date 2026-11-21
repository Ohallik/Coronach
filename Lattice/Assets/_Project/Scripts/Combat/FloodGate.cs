using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the gates a flooded arena's water stands behind: the Walking Canopy's basin has three on its rim, and a later chapter's pump housings, coolant locks or ice couplings are the same thing with
    /// another body. A gate is something to break, not an enemy: it has integrity and a hurtbox, pays a swing what a swing earns and no kill's worth, and its breaking is kept in the save.
    /// What the gates hold back is one number, <see cref="Held"/>: the share of the flood still standing. The water's level follows it (Lattice.World.FloodBasin) and a boss's phases can too, so neither
    /// has to know the other. A boss that only lets its gates be hit in its openings shuts and opens them with <see cref="Expose"/>. A gate that names its boss (<see cref="settledBy"/>) is shut from the
    /// moment the zone loads, so it takes no blow and shows no target until that boss is awake; one that names none can always be hit.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class FloodGate:MonoBehaviour
    {
        public static readonly List<FloodGate> All=new();
        /// <summary>A gate has just gone, and <see cref="Held"/> already says so.</summary>
        public static event Action<FloodGate> Broke;
        /// <summary>The gate standing, and what's left of it: one is shown at a time. Either can be missing.</summary>
        public GameObject whole,wrecked;
        /// <summary>Set in the save as it breaks, so a gate broken before a cleared fight stays down. Empty: it stands again on every load.</summary>
        public string defeatedFlag;
        /// <summary>The boss whose fall settles every gate: an arena that has been won loads drained, whichever gates were still up when it fell.</summary>
        public string settledBy;
        /// <summary>Which gate this is, counted round the rim from the first: what a boss's pattern goes by, so it never depends on the order the scene woke them in.</summary>
        public int index;
        public Health Health{get;private set;}
        public bool Holding=>Health!=null&&Health.Alive;
        public static int Standing{get{int count=0;foreach(var gate in All)if(gate.Holding)count++;return count;}}
        /// <summary>The share of the flood still held: 1 with every gate standing, 0 with none (and with no gates at all, which is a dry floor).</summary>
        public static float Held=>All.Count==0?0:(float)Standing/All.Count;
        /// <summary>Whether blows land on the standing gates. A boss braced on them opens them with its own openings.</summary>
        public static void Expose(bool open){foreach(var gate in All)if(gate.Holding)gate.Health.Damageable=open;}
        /// <summary>The standing gate nearest a point, for a boss that goes to defend one or a partner that picks a target.</summary>
        public static FloodGate Nearest(Vector3 point)
        {
            FloodGate best=null;float least=float.PositiveInfinity;
            foreach(var gate in All)
            {
                if(!gate.Holding)continue;
                var offset=gate.transform.position-point;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=gate;
            }
            return best;
        }
        void Awake()
        {
            Health=GetComponent<Health>();
            Health.Damaged+=(_,packet,amount)=>CombatActor.PayHit(packet,amount);
            Health.Died+=(_,__)=>
            {
                if(GameServices.Current!=null&&!string.IsNullOrEmpty(defeatedFlag))GameServices.Current.Flags.SetBool(defeatedFlag,true);
                CombatVfx.Burst(transform.position+Vector3.up*1.2f,new Color(.62f,.82f,.78f),"kill");
                AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,50);
                Show();
                Debug.Log("GATE_DOWN "+name);
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
            // A gate that answers to a boss is shut until that boss is awake and opens it: nobody breaks the basin's gates from its door, or locks onto one, before the fight has begun.
            if(Holding&&!string.IsNullOrEmpty(settledBy))Health.Damageable=false;
            Show();
        }
        void Show(){bool holding=Holding;if(whole!=null)whole.SetActive(holding);if(wrecked!=null)wrecked.SetActive(!holding);}
        /// <summary>Every gate down without a blow: the boss is beaten and the water goes with it.</summary>
        public static void SettleAll(){foreach(var gate in All){gate.Health.integrity=0;gate.Show();}}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
    }
}
