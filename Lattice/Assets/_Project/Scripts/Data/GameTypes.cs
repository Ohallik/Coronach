using System;
using UnityEngine;
namespace Lattice.Data
{
    public enum ZoneKind{GroundSafe,GroundCombat,SpaceSafe,SpaceCombat}
    public enum BodyForm{Natural,Shaped,CivilFlight,Flight}
    public enum DamageType{Beam,Plasma,Kinetic,Pulse}
    public enum GearSlot{Emitter,Edge,Frame,Drive,Module}
    public enum Discipline{Emitters,Edges,Frames,Drives,Modules}
    public enum EnemyArchetype{PackHunter,Swarm,Sentinel,Spitter,Mine,Serpent}
    public enum ObjectiveKind{TalkTo,Reach,Kill,Collect,Interact,Flag}
    public enum ActorState{Idle,Move,Attack,Dodge,Guard,Skill,Stagger,Down,Dead}
    [Serializable] public struct Stats
    {
        public float output,plating,response,resonance,fortune;
        public static Stats operator +(Stats a,Stats b)=>new(){output=a.output+b.output,plating=a.plating+b.plating,response=a.response+b.response,resonance=a.resonance+b.resonance,fortune=a.fortune+b.fortune};
        public static Stats operator *(Stats a,float n)=>new(){output=a.output*n,plating=a.plating*n,response=a.response*n,resonance=a.resonance*n,fortune=a.fortune*n};
    }
    [Serializable] public struct Ingredient{public string id; public int count;}
    [Serializable] public struct Objective{public ObjectiveKind kind;public string target;public int count;}
    [Serializable] public struct LootEntry{public string id;public int count;[Range(0,1)] public float chance;}
}
