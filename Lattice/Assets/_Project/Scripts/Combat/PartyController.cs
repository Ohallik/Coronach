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
        static readonly Rect FightBossArea=Rect.MinMaxRect(.03f,.13f,.97f,.93f);
        /// <summary>Where the hero, and what he is fighting (a lock, a boss's feet and its tells), stay: under the party panel and the objective card (whose feet are at .72 and .78 of the
        /// height), over the skill row (.14). The top was .84 and a lock had no place of its own: a locked Pikeguard stood under the party panel (nov20 s46 047), Vessa Korl fought in its corner
        /// (s48 216). .78 and not .72, so a boss 16 m down the screen still shares the view with him (QaFightLookTests' bosses stay in frame). What he fights may stand up to the lock
        /// bar (.9) between the panels, and comes under them at the sides (<see cref="UnderPanels"/>): held to .78 everywhere, a boss's feet 12 m up the screen left no room for a
        /// partner below him (QaFightThirdTests.ThePartnerGoesInIfSheFits).</summary>
        public static readonly Rect FightHeroArea=Rect.MinMaxRect(.14f,.24f,.86f,.78f),FightClearArea=Rect.MinMaxRect(.06f,.15f,.94f,.9f);
        /// <summary>Clear of the bars and the panels: a partner with her legs under the hint strip, or behind the skill icons, isn't on the screen in any way that helps.</summary>
        static readonly Rect FightPartnerArea=Rect.MinMaxRect(.08f,.22f,.92f,.78f);
        /// <summary>A partner this near counts in full, and she fades out of it by this far: past that she can't share a view with the hero, and at a hard line she'd pop the view back as she crossed it.</summary>
        const float PartnerFramedWithin=20,PartnerFramedTo=26;
        /// <summary>How far back a fight with a body of a person's size may stand (the lead 100 px in 1080), and one with a lock (91 px): it was 36 with the hero's top at .84, and a
        /// lock had no frame of its own.</summary>
        const float FarthestFight=38,FarthestLock=42,BigMarking=7;
        /// <summary>The party panel and the objective card hang in the top corners down to .715 of the height; between them the screen is clear up to the lock bar. Something standing in
        /// a side's third has its area's top brought under them, a body's depth below their foot (an area holds a body's middle line, and its far side stands a little higher on the
        /// screen); in the middle it keeps the area's own.</summary>
        const float PanelSide=CameraRig.PanelSide,PanelFoot=CameraRig.PanelFoot;
        static Rect UnderPanels(Rect area,Bounds body)
        {
            var camera=Camera.main;if(camera==null)return area;
            float least=1,most=0;
            for(int i=0;i<4;i++)
            {
                var view=camera.WorldToViewportPoint(body.center+new Vector3((i&1)==0?-body.extents.x:body.extents.x,body.extents.y,(i&2)==0?-body.extents.z:body.extents.z));
                if(view.z<=0)continue;least=Mathf.Min(least,view.x);most=Mathf.Max(most,view.x);
            }
            if(least<PanelSide||most>1-PanelSide)area.yMax=Mathf.Min(area.yMax,PanelFoot);
            return area;
        }
        Rect HeroArea(Vector3 at)=>UnderPanels(FightHeroArea,new Bounds(at+Vector3.up*.95f,new Vector3(.8f,1.9f,.8f)));
        static Rect PartnerArea(Bounds? beside)=>beside.HasValue?UnderPanels(FightPartnerArea,beside.Value):FightPartnerArea;
        /// <summary>How far back a boss fight's view may stand for a body bigger than the usual view: the camera rises for it so its tells and the party both read, and no further, where the
        /// lead would be under 80 pixels in 1080 (1.9 m at 46 m through a 30-degree lens). A body that doesn't fit by then gets what room is left and no more: the Tollkeeper's carrier
        /// held the view 36 m back for its whole fight on foot, with Taren and the gate motor beside him tiny in the middle of it (nov20 s65 028 to 033).</summary>
        public const float FarthestBossFight=46,BigBody=6;
        /// <summary>Ordinary fights draw back less far for a shooter than a boss fight does: they're smaller, and there's more of them to see. A locked target too.</summary>
        const float FarthestShot=28;
        /// <summary>A boss's own markings this near the lead are its tells for him, and stay in frame with him; and its feet, while he is within this of them.</summary>
        const float TellsFramedWithin=6,CoreFramedWithin=20;
        /// <summary>A target nobody locked still counts when it's this near: the swing is going at it.</summary>
        const float TargetFramedWithin=18;
        BossController framedBoss;float framedBossReach,framedBossHeight;
        Health framedBodyOf;Collider framedBody;
        public float ReviveProgress=>reviveTarget!=null?Mathf.Clamp01(revive/2):0;
        public CombatActor ReviveTarget=>reviveTarget;
        public int SwapCount{get;private set;}
        /// <summary>The tag-in strike is a reward for a timely swap, not free damage: mashing Y out-broke every other source and
        /// stun-locked bosses, and swaps in a paused menu queued strikes that all landed on resume (QA).</summary>
        public const float SwapStrikeCooldown=4;
        float nextSwapStrike,nextSwapBanter;
        void Awake(){Current=this;ItemUse.Reset();}
        /// <summary>The partner's share of a hit: 60%, and what the party's gear adds (Twin Thread), never past the lead's own.</summary>
        public const float PartnerDamage=.6f;
        public float PartnerShare{get{float share=PartnerDamage;if(members!=null)foreach(var m in members)if(m!=null)share+=m.mods.partnerDamage;return Mathf.Clamp(share,.1f,1);}}
        void Start(){Apply();ZoneController.Current.Changed+=Apply;}
        public void Apply()
        {
            if(members==null||members.Length==0)return;
            var zone=ZoneController.Current;
            RefreshStats();
            for(int i=0;i<members.Length;i++)
            {
                var m=members[i];m.flight=zone.Flight;m.damageScale=i==index?1:PartnerShare;
                m.GetComponent<HeroCollision>().SetFlight(zone.Flight);
                if(!zone.Combat)m.TargetLocked=false;
                m.GetComponent<PlayerBrain>().enabled=i==index;m.GetComponent<PartnerBrain>().enabled=i!=index;
                var ground=m.GetComponent<GroundMotor>();var flight=m.GetComponent<FlightMotor>();ground.enabled=!zone.Flight;flight.enabled=zone.Flight;
                flight.plane=zone.FlightPlane;m.motor=zone.Flight?(IMotor)flight:ground;
                m.GetComponent<FormController>().Set(zone.Form);
                // Drawn through whatever stands between the camera and them (the fixed camera's first rule).
                if(!m.TryGetComponent<HeroThrough>(out _))m.gameObject.AddComponent<HeroThrough>();
            }
            if(cameraRig==null)cameraRig=FindFirstObjectByType<CameraRig>();if(cameraRig!=null)cameraRig.Apply(Active.transform,zone.CameraProfile);
        }
        /// <param name="forced">The active hero went down; the partner covers instead of joking.</param>
        public bool Swap(bool forced=false)
        {
            int next=(index+1)%members.Length;if(next==index||!members[next].Health.Alive)return false;
            Health target=Active.target;bool locked=Active.TargetLocked;index=next;Active.target=target;Apply();Active.TargetLocked=locked&&target!=null&&target.Alive;
            // The save's lead is a place in the party. With a hero resting that isn't the place on the field: written as it was, a reload led with the rester.
            var saved=GameServices.Current.State;int place=Deployment.PartyIndex(saved,Active.character);saved.activeMember=place>=0?place:index;SwapCount++;
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
            // No hero is a solid to another (D354), whatever moves them this frame: a shove, a pull, a placing switches a collider off and back.
            foreach(var m in members)if(m!=null&&m.TryGetComponent<CharacterController>(out var hull))HeroCollision.ThroughParty(hull);
            var state=GameServices.Current.State;
            PromptService.FindInteraction(Active.transform.position);
            // A draught and a meal run on the fight's clock: a menu or a conversation doesn't spend them.
            if(!GameTime.CombatHeld)PartyBuffs.Tick(state,Time.deltaTime);
            if(Time.unscaledTime>=nextStats){RefreshStats();nextStats=Time.unscaledTime+.5f;}
            for(int i=0;i<members.Length;i++)
            {
                // By who they are. Taken by place, the second hero out wrote their Integrity over the second hero in the party, who with three may be the one resting.
                // A party the field holds whole keeps the old reading for a body the save doesn't know (a fixture's): its place.
                var kept=i<state.party.Count&&state.party[i].id==members[i].character?state.party[i]:null;
                for(int j=0;kept==null&&j<state.party.Count;j++)if(state.party[j].id==members[i].character)kept=state.party[j];
                if(kept==null&&i<state.party.Count&&!Deployment.Choosing(state))kept=state.party[i];
                if(kept!=null){kept.integrity=members[i].Health.integrity;kept.charge=members[i].charge;}
            }
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
            // After the hero, what he's after and the boss, the partner if she fits. Framing only those first left her under the bars or off the screen, with her bolts coming in from nowhere
            // and the longest danger lines on screen aimed at someone nobody could see. Down, she's what the hero has to get to, fight or no fight.
            var beside=PartnerBody(at,out bool down);
            if(boss==null)
            {
                framedBoss=null;
                // Long danger lines came in from figures with only their legs on screen. Only those firing right now, and they're as gentle as a boss's framing.
                var shooters=default(Bounds);bool shooting=EnemyBrain.ShootingAt(at,ref shooters);
                // What he has locked, or what his swing is going at in a fight that is on, stays on screen with him and clear of the panels: a locked Forge Hound off the top of the
                // frame (nov20 s48 080), Thawed Guards under the party panel with Sela under the skill row (s35 298), a Glass Sentinel's shield under the panel (s40 098).
                var lockBody=Fought(at,null);
                if(lockBody.HasValue)cameraRig.FrameGround(shooting?shooters:lockBody.Value,FightBossArea,HeroArea(at),FarthestLock,0,lockBody,beside,PartnerArea(beside),UnderPanels(FightClearArea,lockBody.Value),shooting);
                else if(shooting)cameraRig.FrameGround(shooters,FightBossArea,FightHeroArea,FarthestShot,0,null,beside,FightPartnerArea);
                else if(beside.HasValue&&(down||EnemyBrain.FightNear(at)))cameraRig.FrameGround(new Bounds(at+Vector3.up*.95f,new Vector3(1,1.9f,1)),FightBossArea,FightHeroArea,FarthestShot,0,null,beside,FightPartnerArea);
                return;
            }
            if(framedBoss!=boss)
            {
                // Measured once and held, turning or not: a box that followed every swing of the body would nudge the camera on every swing.
                framedBoss=boss;float reach=1,top=boss.transform.position.y+2;
                foreach(var mesh in boss.GetComponentsInChildren<Renderer>())
                    if(mesh is MeshRenderer||mesh is SkinnedMeshRenderer){var size=mesh.bounds;reach=Mathf.Max(reach,Mathf.Max(size.extents.x,size.extents.z));top=Mathf.Max(top,size.max.y);}
                framedBossReach=reach*.8f;framedBossHeight=top-boss.transform.position.y;
            }
            var subject=new Bounds(boss.transform.position+Vector3.up*(framedBossHeight*.5f),new Vector3(framedBossReach*2,framedBossHeight,framedBossReach*2));
            // Standing in a marking too wide for the usual view, the way out of it has to be on screen, ahead of the boss: it's the next thing he has to do.
            Bounds? first=null;
            foreach(var marking in Telegraph.Live)
            {
                if(marking==null||marking.Released||!(marking.owner is BossController))continue;
                if(marking.Span>=BigMarking&&marking.Contains(at))Include(ref first,new Bounds(marking.EdgeFrom(at),Vector3.zero));
                // A tell coming down on him or beside him is what he has to read next: it stays on screen with him, wherever the body is.
                else if(marking.Span<BigMarking*2&&Vector3.Distance(Flat(marking.EdgeFrom(at)),Flat(at))<TellsFramedWithin)Include(ref first,Marked(marking));
            }
            // The bar sends the hero at an organ and the plate says locked: that and the hero stay in frame, then the boss if it fits. Held on the boss alone, the organ fell off the left edge
            // with its health going down. Crew and mites the same: what he's fighting comes first.
            var aimedBody=Fought(at,boss);
            if(aimedBody.HasValue)Include(ref first,aimedBody.Value);
            // Where the boss stands, too, while he is near it: a person-sized boss is all feet, and Vessa Korl fought for a minute in the corner under the party panel (nov20 s48 216).
            var feet=boss.FightCentre;if(Flat(feet-at).magnitude<CoreFramedWithin+Mathf.Min(4,framedBossReach))Include(ref first,new Bounds(feet+Vector3.up*Mathf.Min(1.2f,framedBossHeight*.5f),new Vector3(Mathf.Min(4,framedBossReach*2),Mathf.Min(2.4f,framedBossHeight),Mathf.Min(4,framedBossReach*2))));
            // A body bigger than the usual view draws it further back, to the farthest the party still reads at; past that it has what room is left, as the partner does.
            bool big=framedBossHeight>BigBody||framedBossReach*2>BigBody;
            cameraRig.FrameGround(subject,FightBossArea,HeroArea(at),big?FarthestBossFight:FarthestFight,0,first,beside,PartnerArea(beside),first.HasValue?UnderPanels(FightClearArea,first.Value):FightClearArea,true);
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        /// <summary>A marking's floor, as a box: the strip of a line, the round of anything else.</summary>
        static Bounds Marked(Telegraph marking)
        {
            var box=new Bounds(marking.Origin,Vector3.up*.2f);
            if(marking.Shape==Telegraph.Kind.Line)box.Encapsulate(marking.Origin+marking.Forward*marking.Length);
            else box.Expand(new Vector3(marking.Radius*2,0,marking.Radius*2));
            return box;
        }
        /// <summary>What the lead is fighting, as it stands on the floor: what he has locked, or what his swing is going at in a fight that is on (an unlocked aim at a body that is
        /// asleep would frame him for anyone he walks past). Not the boss itself, nor a part of it; null when there is nothing.</summary>
        Bounds? Fought(Vector3 at,BossController boss)
        {
            var aimed=Active.target;
            if(aimed==null||!aimed.Alive||!aimed.Targetable||aimed.friendly)return null;
            if(boss!=null&&(aimed==boss.Health||aimed.reactionOwner==boss.Health))return null;
            bool near=(aimed.transform.position-at).sqrMagnitude<TargetFramedWithin*TargetFramedWithin;
            if(!Active.TargetLocked&&!(near&&(boss!=null||Awake(aimed))))return null;
            if(framedBodyOf!=aimed){framedBodyOf=aimed;framedBody=aimed.GetComponent<Collider>();}
            return framedBody!=null&&framedBody.enabled?framedBody.bounds:new Bounds(aimed.transform.position+Vector3.up,Vector3.one*2);
        }
        static bool Awake(Health body)=>body.TryGetComponent<EnemyBrain>(out var brain)&&brain.enabled&&!brain.Passive;
        static void Include(ref Bounds? box,Bounds more)
        {
            if(box.HasValue){var both=box.Value;both.Encapsulate(more);box=both;}else box=more;
        }
        Bounds? PartnerBody(Vector3 at,out bool down)
        {
            down=false;
            foreach(var member in members)
            {
                if(member==Active||!member.gameObject.activeInHierarchy)continue;
                var body=member.transform.position;var offset=body-at;offset.y=0;float apart=offset.magnitude;if(apart>=PartnerFramedTo)continue;
                down=!member.Health.Alive;body=Vector3.Lerp(at,body,Mathf.InverseLerp(PartnerFramedTo,PartnerFramedWithin,apart));
                return new Bounds(body+Vector3.up*.95f,new Vector3(.9f,1.9f,.9f));
            }
            return null;
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
            // What he has locked, and a boss he is fighting, count from further than an ordinary target's 30 m: the director's craft fought round the Continuance was 64 m off,
            // and the fight's frame was flat grey hull with no craft and no relay in it (nov20 s66 154).
            float reach=target!=null&&(hero.TargetLocked||target.GetComponent<BossController>()!=null)?FlightLockFramed:30;
            if(!hero.flight||!ZoneController.Current.Combat||target==null||
                !departure&&!preview&&(!target.Alive||(hero.transform.position-target.transform.position).sqrMagnitude>reach*reach))
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
            IncludeMeshes(ref bounds,heroMeshes);
            // A body bigger than any view the ships still read in is framed where it is nearest him, with the part he is after: the Tollkeeper's carrier drew the view 300 m
            // back over its own introduction, the boss small in its own card and the ship a speck (nov20 s65 010).
            var body=new Bounds(target.transform.position,Vector3.zero);bool any=false;IncludeMeshes(ref body,targetMeshes,ref any);
            // A quiet preview or a departure is a composed view of the whole animal, at whatever distance it takes: only a fight is cut down to what the ships read in.
            bool composed=preview||departure;
            if(!composed&&any&&Mathf.Max(body.size.x,body.size.z)>FlightBodyMost)
            {
                var near=body.ClosestPoint(hero.transform.position);bounds.Encapsulate(new Bounds(near,new Vector3(FlightBodyNear,6,FlightBodyNear)));
                // A part he has locked (a brace, a relay), if it is a part: the body's own collider is the body again.
                var part=hero.target!=null&&hero.target!=target&&hero.target.TryGetComponent<Collider>(out var solid)&&solid.enabled?solid.bounds:(Bounds?)null;
                if(part.HasValue&&Mathf.Max(part.Value.size.x,part.Value.size.z)<FlightBodyNear&&(part.Value.center-hero.transform.position).sqrMagnitude<FlightLockFramed*FlightLockFramed)bounds.Encapsulate(part.Value);
            }
            else if(any)bounds.Encapsulate(body);
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
            cameraRig.FrameEncounter(bounds,composed);
        }
        static void IncludeMeshes(ref Bounds bounds,Renderer[] meshes){bool any=true;IncludeMeshes(ref bounds,meshes,ref any);}
        static void IncludeMeshes(ref Bounds bounds,Renderer[] meshes,ref bool any)
        {
            if(meshes==null)return;
            foreach(var mesh in meshes)
                if(mesh!=null&&mesh.enabled&&mesh.gameObject.activeInHierarchy&&mesh.name!="Telegraph"&&(mesh is MeshRenderer||mesh is SkinnedMeshRenderer))
                {if(any)bounds.Encapsulate(mesh.bounds);else{bounds=mesh.bounds;any=true;}}
        }
        /// <summary>How far off a locked target or a boss in flight is still framed with the ship; how big a body may be before only its nearest part is, and that part's size.</summary>
        public const float FlightLockFramed=80,FlightBodyMost=80,FlightBodyNear=12;
        public void RefreshStats()
        {
            var state=GameServices.Current.State;var lent=PartyBuffs.Current(state);
            foreach(var actor in members)
            {
                var member=state.party.Find(m=>m.id==actor.character);var def=GameCatalog.Find<CharacterDef>(actor.character);
                if(member==null||def==null)continue;
                // What the parts do beyond the stats, and what the party's draught and meal lend on top: one sum, read where each thing happens (CombatActor.mods).
                actor.mods=Gear.Modifiers(state,member,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>)+lent;
                var melee=Gear.Calculate(state,member,def,GearSlot.Edge,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
                var ranged=Gear.Calculate(state,member,def,GearSlot.Emitter,GameCatalog.Find<TechPartDef>,GameCatalog.Find<AffixDef>);
                actor.damage=melee.output;actor.rangedDamage=ranged.output;actor.plating=melee.plating;actor.resonance=melee.resonance;actor.response=melee.response;actor.fortune=melee.fortune;
                actor.edgeType=Gear.WeaponType(state,member,GearSlot.Edge,DamageType.Kinetic);actor.emitterType=Gear.WeaponType(state,member,GearSlot.Emitter,DamageType.Beam);
                // A level only feeds a living body: lifting a downed hero's integrity here would skip the revive and leave them frozen (QA-001).
                float hp=Levels.MaxIntegrity(member.level);if(hp>actor.Health.maximum&&actor.Health.Alive)actor.Health.integrity+=hp-actor.Health.maximum;actor.Health.maximum=hp;
                actor.GetComponent<GroundMotor>().speed=6.7f+Mathf.Max(0,melee.response-10)*.03f;
                actor.GetComponent<FlightMotor>().maxSpeed=12+Mathf.Max(0,melee.response-10)*.08f;
            }
            // A partner keeps the lead's pace (PartnerBrain.KeepUp): her own cruise and stride, where they are below his, are his. Slower than Taren's 13.76 m/s, Sela fell behind
            // the place she was flying to for as long as he flew (nov22-whole s17).
            if(index>=0&&index<members.Length&&members[index]!=null)
            {
                var lead=members[index];float cruise=lead.GetComponent<FlightMotor>().maxSpeed,stride=lead.GetComponent<GroundMotor>().speed;
                foreach(var actor in members)
                {
                    if(actor==null||actor==lead)continue;
                    var ship=actor.GetComponent<FlightMotor>();ship.maxSpeed=Mathf.Max(ship.maxSpeed,cruise);
                    var walk=actor.GetComponent<GroundMotor>();walk.speed=Mathf.Max(walk.speed,stride);
                }
            }
            // A Twin Thread put on or taken off changes the partner's share at once, not at the next swap. Only when it changes: nothing else is written over here.
            float share=PartnerShare;if(Mathf.Approximately(share,appliedShare))return;
            appliedShare=share;for(int i=0;i<members.Length;i++)if(members[i]!=null)members[i].damageScale=i==index?1:share;
        }
        float appliedShare=PartnerDamage;
        void OnDestroy(){CombatActor.ResetTime();if(Current==this)Current=null;}
        /// <summary>The hero out beside the lead, by what they are and not by where they stand in the array (which was read as members[1-index], and is -1 for a lead in
        /// a third place). With two it's the other one. Were there ever more, whoever is down comes first: that's who the lead has to reach. Null for a hero alone.</summary>
        public CombatActor Partner
        {
            get
            {
                if(members==null||index<0||index>=members.Length)return null;
                CombatActor beside=null;
                for(int i=1;i<members.Length;i++)
                {
                    var member=members[(index+i)%members.Length];if(member==null)continue;
                    if(!member.Health.Alive)return member;
                    beside??=member;
                }
                return beside;
            }
        }
    }
}
