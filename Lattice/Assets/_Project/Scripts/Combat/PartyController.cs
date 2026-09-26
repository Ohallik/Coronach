using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class PartyController:MonoBehaviour
    {
        public static PartyController Current{get;private set;}
        public CombatActor[] members;
        public int index;
        public CombatActor Active=>members[index];
        float revive,nextStats;
        public int SwapCount{get;private set;}
        void Awake(){Current=this;}
        void Start(){Apply();ZoneController.Current.Changed+=Apply;}
        public void Apply()
        {
            if(members==null||members.Length==0)return;
            var zone=ZoneController.Current;
            RefreshStats();
            for(int i=0;i<members.Length;i++)
            {
                var m=members[i];m.flight=zone.Flight;m.damageScale=i==index?1:.6f;
                if(!zone.Combat)m.TargetLocked=false;
                m.GetComponent<PlayerBrain>().enabled=i==index;m.GetComponent<PartnerBrain>().enabled=i!=index;
                var ground=m.GetComponent<GroundMotor>();var flight=m.GetComponent<FlightMotor>();ground.enabled=!zone.Flight;flight.enabled=zone.Flight;
                flight.plane=zone.definition.flightPlane;m.motor=zone.Flight?(IMotor)flight:ground;
                m.GetComponent<FormController>().Set(zone.Form);
            }
            var rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Apply(Active.transform,zone.definition.cameraProfile);
        }
        public bool Swap()
        {
            int next=(index+1)%members.Length;if(next==index||!members[next].Health.Alive)return false;
            Health target=Active.target;bool locked=Active.TargetLocked;index=next;Active.target=target;Apply();Active.TargetLocked=locked&&target!=null&&target.Alive;
            GameServices.Current.State.activeMember=index;SwapCount++;
            if(ZoneController.Current.Combat)CombatActor.Strike(Active.transform.position+Vector3.up,3,Active.Packet(45,DamageType.Pulse,35));
            BarkService.Play(Active.character,"swap",true);Debug.Log("SWAP_OK");return true;
        }
        void Update()
        {
            if(members==null||members.Length==0)return;
            // Loading a save replaces State before the old party is unloaded.
            // Never copy departing actors back into that newly loaded snapshot.
            if(SceneFlow.Current!=null&&SceneFlow.Current.Loading)return;
            var state=GameServices.Current.State;
            PromptService.FindInteraction(Active.transform.position);
            if(Time.unscaledTime>=nextStats){RefreshStats();nextStats=Time.unscaledTime+.5f;}
            for(int i=0;i<members.Length&&i<state.party.Count;i++){state.party[i].integrity=members[i].Health.integrity;state.party[i].charge=members[i].charge;}
            foreach(var m in members)
            {
                if(m==Active||m.Health.Alive)continue;
                if((m.transform.position-Active.transform.position).sqrMagnitude<4){revive+=Time.deltaTime;if(revive>=2){m.Health.Heal(m.Health.maximum*.35f);revive=0;Debug.Log("REVIVE_OK");}}
                else revive=0;
            }
            if(!Active.Health.Alive)Swap();
        }
        public void RefreshStats()
        {
            var state=GameServices.Current.State;
            foreach(var actor in members)
            {
                var member=state.party.Find(m=>m.id==actor.character);var def=GameCatalog.Find<CharacterDef>(actor.character);
                if(member==null||def==null)continue;
                var melee=Gear.Calculate(state,member,def,GearSlot.Edge,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
                var ranged=Gear.Calculate(state,member,def,GearSlot.Emitter,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
                actor.damage=melee.output;actor.rangedDamage=ranged.output;actor.plating=melee.plating;actor.resonance=melee.resonance;actor.response=melee.response;actor.fortune=melee.fortune;
                actor.edgeType=Gear.WeaponType(state,member,GearSlot.Edge,DamageType.Kinetic);actor.emitterType=Gear.WeaponType(state,member,GearSlot.Emitter,DamageType.Beam);
                float hp=Levels.MaxIntegrity(member.level);if(hp>actor.Health.maximum)actor.Health.integrity+=hp-actor.Health.maximum;actor.Health.maximum=hp;
                actor.GetComponent<GroundMotor>().speed=6.7f+Mathf.Max(0,melee.response-10)*.03f;
                actor.GetComponent<FlightMotor>().maxSpeed=12+Mathf.Max(0,melee.response-10)*.08f;
            }
        }
        void OnDestroy(){CombatActor.ResetTime();if(Current==this)Current=null;}
    }
}
