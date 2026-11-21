using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// What the shared controller asks about one boss, in one place: each boss's own file registers a <see cref="Kit"/> (name, phases, chooser, every move
    /// by name, what a shift promises and calls in, its body, how it ends), and nothing else has to name it. Before this each of those was an arm of a
    /// switch on the id somewhere in BossController, ActorFactory, EnemyBrain or the objective panel, and a boss left out of one fought with the Burrower's
    /// charge without a word. A boss with no kit now says so: an exception in the editor, an error in the player's log, and no specials at all.
    /// </summary>
    public sealed partial class BossController
    {
        /// <summary>How the fight ends, for the body's last moment (EnemyBrain.OnDeath): the boss explosion, a machine switched off, an animal let go, a person who gives up, or a
        /// creature driven off that leaves under its own power (BossDeparture walks it out; a BossExit in the scene says where to). Append only.</summary>
        public enum Ending{Destroyed,ShutDown,Released,Yields,Leaves}
        public sealed class Kit
        {
            /// <summary>The EnemyDef's id.</summary>
            public string id;
            /// <summary>The bar's name, in capitals ("THE BURROWER"), and the line under it on the title card.</summary>
            public string title,epithet="";
            /// <summary>Integrity fractions at which the next phase begins, highest first.</summary>
            public float[] thresholds={.5f};
            /// <summary>Picks the next special for this hero: its name and its routine.</summary>
            public Func<BossController,CombatActor,(string,IEnumerator)> choose;
            /// <summary>Every special by name. Tests and review captures start them through Perform, and a shift's promise is kept through it.</summary>
            public Dictionary<string,Func<BossController,IEnumerator>> moves=new();
            /// <summary>The move a shift into this phase promises next, or null.</summary>
            public Func<int,string> signature;
            /// <summary>What a shift brings in (its summons), after the shove.</summary>
            public Action<BossController> shift;
            public string shiftText="SHIFTING  ·  BRACE";
            /// <summary>The fight's state beyond integrity, for the bar; and what the bar says while it can't be hurt, if not the shared words.</summary>
            public Func<BossController,string> status,guarded;
            /// <summary>The objective panel's line with the bar up, given the line the zone had. None: that line stays.</summary>
            public Func<BossController,string,string> fightGoal;
            /// <summary>The body's capsule (3 m tall): a rig's is 1.4, a person's .8.</summary>
            public float capsuleRadius=1.4f;
            /// <summary>Stands tall enough to hide the party from the fixed camera: gets BossOcclusion.</summary>
            public bool occlusion;
            public Ending ending=Ending.Destroyed;
            /// <summary>The party's line when it falls: a bark situation.</summary>
            public string downBark="boss";
            /// <summary>For a boss with parts of its own: after the shared Start, every frame it is alive and unpaused, and as it falls.</summary>
            public Action<BossController> start,update,down;
            /// <summary>Where its weak spots sit, in its own space, if not where <see cref="WeakSpotAt"/> puts them (its flanks, and its front when it is wider than 1.4 m). Each must stand
            /// proud of the body's capsule where a hero can stand, or no blade reaches it (QaPlanCombatTests checks every boss).</summary>
            public Vector3[] weakSpots;
            /// <summary>For a boss whose weak spot is there only at a moment of its fight (Dessik Varn's cockpit, while his suit kneels): what brings that moment about now (true) or ends
            /// it (false), as <see cref="Perform"/> starts a move, so the shared tests can find the spot where a player would and check it there. Unset: its spots stand from the start.</summary>
            public Action<BossController,bool> showWeakSpot;
            /// <summary>For a fight wider than the bar, the panel's line and the framing look within (34 m of a boss's root: the Tollkeeper's lane is 116 m across): the point its fight is
            /// measured from, and how much farther than an asker's range it reaches (<see cref="Focus"/>, <see cref="AnyFightUnderway"/>). Unset: its root, and no farther.</summary>
            public Func<BossController,Vector3> centre;
            public Func<BossController,float> reach;
        }
        // No initializer: each boss's file registers from a static field of its own, and the order between files isn't fixed.
        static Dictionary<string,Kit> kits;
        static List<string> twice;
        static readonly HashSet<string> reported=new();
        /// <summary>Called from a static field in the boss's own file: <c>static readonly Kit HessaKit=Register(new Kit{id="HessaKorl",...});</c></summary>
        static Kit Register(Kit kit)
        {
            kits??=new();
            if(kits.ContainsKey(kit.id))(twice??=new()).Add(kit.id);
            kits[kit.id]=kit;return kit;
        }
        static Kit Find(string id)=>id!=null&&kits!=null&&kits.TryGetValue(id,out var kit)?kit:null;
        public static bool Registered(string id)=>Find(id)!=null;
        /// <summary>Every registered boss, by EnemyDef id.</summary>
        public static string[] RegisteredIds{get{var ids=new string[kits?.Count??0];kits?.Keys.CopyTo(ids,0);Array.Sort(ids,string.CompareOrdinal);return ids;}}
        /// <summary>A registered boss's specials by name, its thresholds, and the move each phase from 2 on promises (null where it promises none).</summary>
        public static string[] MovesOf(string id){var kit=Find(id);if(kit==null)return Array.Empty<string>();var names=new string[kit.moves.Count];kit.moves.Keys.CopyTo(names,0);return names;}
        public static float[] ThresholdsOf(string id)=>Find(id)?.thresholds??Array.Empty<float>();
        public static string PromisedMove(string id,int phase)=>Find(id)?.signature?.Invoke(phase);
        public static float CapsuleRadius(string id)=>Find(id)?.capsuleRadius??1.4f;
        public static bool Occludes(string id)=>Find(id)?.occlusion??false;
        public static Ending EndingOf(string id)=>Find(id)?.ending??Ending.Destroyed;
        /// <summary>Whether this boss's weak spot is there only at a moment of its fight (<see cref="Kit.showWeakSpot"/>).</summary>
        public static bool WeakSpotGated(string id)=>Find(id)?.showWeakSpot!=null;
        /// <summary>Brings about (or ends) the moment this boss's weak spot is there. False for a boss whose spots stand from the start, which needs nothing brought about.</summary>
        public bool ShowWeakSpot(bool open){var show=health!=null&&health.Alive?Own.showWeakSpot:null;if(show==null)return false;show(this,open);return true;}
        /// <summary>A boss nobody registered has no moves, and used to borrow the Burrower's. In the editor that stops whoever is making it; in a player it's an error in the log, once.</summary>
        public static void Require(string id)
        {
            string fault=null;
            if(twice!=null&&twice.Contains(id))fault=$"BOSS_REGISTERED_TWICE {id}: two BossController files register this id, and the later one won";
            else if(!Registered(id))fault=$"BOSS_UNREGISTERED {id}: its EnemyDef says boss, and no BossController.<Name>.cs registers a Kit with this id, so it has no specials, phases or name of its own";
            if(fault==null)return;
#if UNITY_EDITOR
            throw new InvalidOperationException(fault);
#else
            if(reported.Add(id))Debug.LogError(fault);
#endif
        }
        Kit kit;
        /// <summary>This boss's registration; for one that has none, a kit with no moves, so it can still be fought to the end and its door still opens.</summary>
        Kit Own=>kit??=Find(health.id)??new Kit{id=health.id,title=health.id.ToUpperInvariant()};
        /// <summary>What the objective panel says with this boss's bar up.</summary>
        public string FightGoal(string label)=>health!=null&&Own.fightGoal!=null?Own.fightGoal(this,label):label;
        /// <summary>The track its fight plays: its definition's, or the shared one.</summary>
        string Track=>brain!=null&&brain.definition!=null&&!string.IsNullOrWhiteSpace(brain.definition.bossMusicId)?brain.definition.bossMusicId:Lattice.Core.MusicDirector.BossTrack;
        /// <summary>A special by name: this boss's own, or (as before there were kits) any boss's.</summary>
        IEnumerator Routine(string move)
        {
            if(move=="PhaseShift")return PhaseShift();
            if(Own.moves.TryGetValue(move,out var own))return own(this);
            if(kits!=null)foreach(var other in kits.Values)if(other.moves.TryGetValue(move,out var borrowed))return borrowed(this);
            return null;
        }
    }
}
