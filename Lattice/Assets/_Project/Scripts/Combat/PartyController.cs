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
        CombatActor reviveTarget;
        CameraRig cameraRig;
        CombatActor framedHero;
        Health framedTarget;
        Renderer[] heroMeshes, targetMeshes;
        readonly System.Collections.Generic.Dictionary<CombatActor,Renderer[]> companionMeshes=new();
        int targetChildCount;
        /// <summary>A ground boss stays in frame. The hero keeps clear of the screen's corners and its bars; the boss's body may run closer to the edge. Past this far back the hero
        /// would be too small to play, and a boss that still can't fit gets the HUD's edge mark instead.</summary>
        static readonly Rect FightBossArea=Rect.MinMaxRect(.03f,.13f,.97f,.93f),FightHeroArea=Rect.MinMaxRect(.14f,.24f,.86f,.84f);
        const float FarthestFight=36,BigMarking=7;
        BossController framedBoss;float framedBossReach,framedBossHeight;
        public float ReviveProgress=>reviveTarget!=null?Mathf.Clamp01(revive/2):0;
        public CombatActor ReviveTarget=>reviveTarget;
        public int SwapCount{get;private set;}
        /// <summary>The tag-in strike is a reward for a timely swap, not free damage: mashing Y out-broke every other source and
        /// stun-locked bosses, and swaps in a paused menu queued strikes that all landed on resume (QA).</summary>
        public const float SwapStrikeCooldown=4;
        float nextSwapStrike,nextSwapBanter;
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
                m.GetComponent<HeroCollision>().SetFlight(zone.Flight);
                if(!zone.Combat)m.TargetLocked=false;
                m.GetComponent<PlayerBrain>().enabled=i==index;m.GetComponent<PartnerBrain>().enabled=i!=index;
                var ground=m.GetComponent<GroundMotor>();var flight=m.GetComponent<FlightMotor>();ground.enabled=!zone.Flight;flight.enabled=zone.Flight;
                flight.plane=zone.definition.flightPlane;m.motor=zone.Flight?(IMotor)flight:ground;
                m.GetComponent<FormController>().Set(zone.Form);
            }
            if(cameraRig==null)cameraRig=FindFirstObjectByType<CameraRig>();if(cameraRig!=null)cameraRig.Apply(Active.transform,zone.definition.cameraProfile);
        }
        /// <param name="forced">The active hero went down; the partner covers instead of joking.</param>
        public bool Swap(bool forced=false)
        {
            int next=(index+1)%members.Length;if(next==index||!members[next].Health.Alive)return false;
            Health target=Active.target;bool locked=Active.TargetLocked;index=next;Active.target=target;Apply();Active.TargetLocked=locked&&target!=null&&target.Alive;
            GameServices.Current.State.activeMember=index;SwapCount++;
            bool strike=ZoneController.Current.Combat&&!GameTime.Paused&&GameTime.Now>=nextSwapStrike;
            if(strike){nextSwapStrike=GameTime.Now+SwapStrikeCooldown;CombatActor.Strike(Active.transform.position+Vector3.up,3,Active.Packet(45,DamageType.Pulse,35)).ThroughWalls=false;CombatAudio.SwapStrike(Active);}
            bool banter=!forced&&Time.unscaledTime>=nextSwapBanter;if(banter)nextSwapBanter=Time.unscaledTime+SwapStrikeCooldown;
            BarkService.Play(Active.character,forced?"cover":"swap",forced||banter);Debug.Log("SWAP_OK");return true;
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
            CombatActor nearbyDown=null;
            if(Active.Health.Alive&&!Active.Recovering&&!GameServices.Current.Input.Blocked)
                foreach(var m in members)
                    if(m!=Active&&!m.Health.Alive)
                    {
                        float reach=Active.flight?HeroCollision.HullRadius(Active.character)+HeroCollision.HullRadius(m.character)+.5f:2;
                        if((m.transform.position-Active.transform.position).sqrMagnitude<reach*reach){nearbyDown=m;break;}
                    }
            if(nearbyDown!=reviveTarget){revive=0;reviveTarget=nearbyDown;}
            if(reviveTarget!=null)
            {
                revive+=Time.deltaTime;
                if(revive>=2){reviveTarget.Health.Heal(reviveTarget.Health.maximum*.35f);revive=0;reviveTarget=null;Debug.Log("REVIVE_OK");}
            }
            if(!Active.Health.Alive)Swap(true);
            FrameFlightEncounter();
            FrameGroundFight();
        }
        void FrameGroundFight()
        {
            if(cameraRig==null||Active.flight)return;
            var at=Active.transform.position;var boss=BossController.Focus(at);
            if(boss==null){framedBoss=null;return;}
            if(framedBoss!=boss)
            {
                // Measured once and held, turning or not: a box that followed every swing of the body would nudge the camera on every swing.
                framedBoss=boss;float reach=1,top=boss.transform.position.y+2;
                foreach(var mesh in boss.GetComponentsInChildren<Renderer>())
                    if(mesh is MeshRenderer||mesh is SkinnedMeshRenderer){var size=mesh.bounds;reach=Mathf.Max(reach,Mathf.Max(size.extents.x,size.extents.z));top=Mathf.Max(top,size.max.y);}
                framedBossReach=reach*.8f;framedBossHeight=top-boss.transform.position.y;
            }
            var subject=new Bounds(boss.transform.position+Vector3.up*(framedBossHeight*.5f),new Vector3(framedBossReach*2,framedBossHeight,framedBossReach*2));
            // Standing in a marking too wide for the usual view, the way out of it has to be on screen as well.
            foreach(var marking in Telegraph.Live)
                if(marking!=null&&!marking.Released&&marking.owner is BossController&&marking.Span>=BigMarking&&marking.Contains(at))subject.Encapsulate(marking.EdgeFrom(at));
            cameraRig.FrameGround(subject,FightBossArea,FightHeroArea,FarthestFight);
        }
        void FrameFlightEncounter()
        {
            if(cameraRig==null)return;
            var hero=Active;var target=hero.target;
            // A collar link remains part of the whole moving animal's envelope.
            if(target!=null&&target.reactionOwner!=null)target=target.reactionOwner;
            bool departure=false,preview=false;
            // Targeting correctly drops resolved health immediately. Preserve
            // only the already-framed Cantor's short release introduction; a
            // newly selected live opponent still takes priority.
            if((target==null||!target.Alive)&&framedTarget!=null&&
                framedTarget.TryGetComponent<CantorRelease>(out var release)&&release.KeepInEncounterFrame)
            {target=framedTarget;departure=true;}
            // A local viewing pocket can frame the real dormant animal without
            // targeting it or extending ordinary combat's 30 m interest range.
            if((target==null||!target.Alive)&&!departure)
            {target=DormantEnemy.Interest(hero.transform.position);preview=target!=null;}
            if(!hero.flight||!ZoneController.Current.Combat||target==null||
                !departure&&!preview&&(!target.Alive||(hero.transform.position-target.transform.position).sqrMagnitude>900))
            {cameraRig.FrameEncounter(null);return;}
            if(framedHero!=hero)
            {
                framedHero=hero;
                heroMeshes=hero.GetComponent<FormController>().flight.GetComponentsInChildren<Renderer>(true);
            }
            // Cantor adds its tail in Start. Refresh once those children exist,
            // without collecting arrays on every flight frame.
            if(framedTarget!=target||targetChildCount!=target.transform.childCount)
            {
                framedTarget=target;targetChildCount=target.transform.childCount;
                targetMeshes=target.GetComponentsInChildren<Renderer>(true);
            }
            var bounds=new Bounds(hero.transform.position+Vector3.up*.8f,Vector3.one*HeroCollision.HullRadius(hero.character)*2);
            IncludeMeshes(ref bounds,heroMeshes);IncludeMeshes(ref bounds,targetMeshes);
            // Nearby disabled craft still need to be seen and recovered. Use
            // the encounter's existing 30 m range so a distant follower cannot
            // pull the camera across the map. Cache each ship for this party.
            foreach(var companion in members)
            {
                if(companion==null||companion==hero||!companion.flight||!companion.gameObject.activeInHierarchy||
                    (companion.transform.position-hero.transform.position).sqrMagnitude>900)continue;
                if(!companionMeshes.TryGetValue(companion,out var meshes))
                {meshes=companion.GetComponent<FormController>().flight.GetComponentsInChildren<Renderer>(true);companionMeshes.Add(companion,meshes);}
                bounds.Encapsulate(new Bounds(companion.transform.position+Vector3.up*.8f,Vector3.one*HeroCollision.HullRadius(companion.character)*2));
                IncludeMeshes(ref bounds,meshes);
            }
            cameraRig.FrameEncounter(bounds);
        }
        static void IncludeMeshes(ref Bounds bounds,Renderer[] meshes)
        {
            if(meshes==null)return;
            foreach(var mesh in meshes)
                if(mesh!=null&&mesh.enabled&&mesh.gameObject.activeInHierarchy&&mesh.name!="Telegraph"&&
                    (mesh is MeshRenderer||mesh is SkinnedMeshRenderer))bounds.Encapsulate(mesh.bounds);
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
                // A level only feeds a living body: lifting a downed hero's integrity here would skip the revive and leave them frozen (QA-001).
                float hp=Levels.MaxIntegrity(member.level);if(hp>actor.Health.maximum&&actor.Health.Alive)actor.Health.integrity+=hp-actor.Health.maximum;actor.Health.maximum=hp;
                actor.GetComponent<GroundMotor>().speed=6.7f+Mathf.Max(0,melee.response-10)*.03f;
                actor.GetComponent<FlightMotor>().maxSpeed=12+Mathf.Max(0,melee.response-10)*.08f;
            }
        }
        void OnDestroy(){CombatActor.ResetTime();if(Current==this)Current=null;}
    }
}
