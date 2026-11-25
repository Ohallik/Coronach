using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Slag Colossus, chapter 8's first boss: a walking crucible war-engine, seven metres of iron on four legs with two pour-arms, guarding the Bridle Foundry's heart and the caged juvenile
    /// behind it. It is a machine the party takes apart by working the room, and it is destroyed (<see cref="Ending.Destroyed"/>). The heart's contract is in docs/maps/BridleFoundry.md, "Boss
    /// arena contract".
    /// <list type="bullet">
    /// <item><b>Too hot to hurt.</b> It wears three heat plates (its front and both flanks, drawn here), and while any of them still glows its body takes nothing: every blow reads as resisted,
    /// the bar says TOO HOT TO HURT · OPEN A SLUICE, and the first blow says so on screen. Round the rim stand three coolant sluices (<see cref="CoolantSluice"/>). A hero turns one (1.6 s,
    /// hands on the wheel) and its coolant is thrown onto the Colossus: the plate it reaches goes dark for <see cref="ColossusTune.Dark"/> seconds, the engine reels, and a dark plate is a break
    /// target. Broken, a plate is shed for good and takes an eighth of the bar; one left glowing again keeps its wounds. Its stuck and venting beats are the time to get to a wheel.</item>
    /// <item><b>Cooled</b> (its plates shed). Its body can be hurt and its two pour-arms open as break targets (<see cref="BossParts"/>, <c>Pour-arm</c>); its bar holds at half until both are
    /// off (BREAK THE POUR-ARMS): its body is fought from five eighths down to half, and held there, and the arms take nothing off it. The second arm off with its bar at the line tears
    /// loose and carries it over: the shift. (Off before the bar is down to the line, the line just stops holding.)</item>
    /// <item><b>Coming apart</b> (under half). No arms, so no pour. It leaks bright slag where it walks (a danger for a few seconds, then dark crust that only slows: the chapter's rule, dark
    /// crust is safe), its tells are quicker, and it spills its crucible round itself (a marked ring with its own legs inside it safe), after which it stands emptied and open.</item>
    /// </list>
    /// Its moves: <b>Pour</b> (an arm tips its ladle over a marked sector; the slag crusts where it ran), <b>Stomp</b> (a marked circle round it; its feet bite the floor and it is stuck),
    /// <b>Slag Bombs</b> (a fan of marked circles, the time in the air the tell), <b>Furnace Breath</b> (a marked lane that solid cover takes; then it vents) and, coming apart, <b>Spill</b>.
    /// It fights without sluices too (the proving yard): it wears no plates, its arms are open from the start, and its phase turns on integrity alone. It keeps within
    /// <see cref="ColossusTune.Leash"/> of where it was set, and works round rather than stand between the camera and the hero.
    /// </summary>
    public sealed partial class BossController
    {
        static readonly Kit SlagColossusKit=Register(new Kit
        {
            id="SlagColossus",title="THE SLAG COLOSSUS",epithet="A crucible on legs, too hot to hurt",thresholds=new[]{.5f},
            choose=(boss,hero)=>boss.ChooseColossus(hero),
            moves={["Pour"]=boss=>boss.SlagPour(),["Stomp"]=boss=>boss.SlagStomp(),["SlagBombs"]=boss=>boss.SlagBombs(),["FurnaceBreath"]=boss=>boss.FurnaceBreath(),["Spill"]=boss=>boss.SlagSpill()},
            signature=phase=>phase>=2?"Spill":null,
            shift=boss=>boss.ColossusComesApart(),shiftText="IT COMES APART  ·  BRACE",
            status=boss=>boss.ColossusStatus(),
            fightGoal=(boss,label)=>boss.ColossusGoal(label),
            // Destroyed, and TovraRelease follows at once in the heart: the shared "boss" bark would be said over the smiths filing in (manifest 8.5).
            capsuleRadius=ColossusTune.Radius,occlusion=true,ending=Ending.Destroyed,downBark="",
            // Its joints between the plates, behind and at its back flanks, a hand proud of a 3 m body at 1.4 m: what a blade reaches once it has cooled. The front and flanks are the plates'.
            weakSpots=new[]{new Vector3(1.909f,1.4f,-1.909f),new Vector3(0,1.4f,-2.7f),new Vector3(-1.909f,1.4f,-1.909f)},
            start=boss=>boss.ColossusStart(),update=boss=>boss.ColossusUpdate(),down=boss=>boss.ColossusDown(),
        });
        /// <summary>The Slag Colossus's numbers, all of them (its integrity is its EnemyDef's: HarrowgateBuilder.Bosses). Damage is before the second phase's +18%; a boss stays at threat 1.
        /// Every move it has in both halves is authored at 138 to 146 (12.2% of Idra's bar at Sync 24 to 15.5% of Taren's at 23: HarrowgateBuilder.Bosses' sizing), and the Spill, which
        /// it has only coming apart, at 122 (144 with the half's +18%).</summary>
        public static class ColossusTune
        {
            /// <summary>Its capsule's radius (its definition makes it 7 m tall), and how far from where it was set it will walk.</summary>
            public const float Radius=3,Leash=6;
            /// <summary>The heat plates: how many, where they stand (degrees from its front), how far out from its middle and how high, how big a target, each one's integrity and the bar it
            /// takes when shed (shares of the boss's), how long a quenched plate stays dark, and how long the quench makes it reel.</summary>
            public const int Plates=3;
            public static readonly float[] PlateBearings={0,90,270};
            public const float PlateOut=3.25f,PlateHeight=1.8f,PlateTarget=1.05f,PlateIntegrity=.05f,PlateBar=.125f,Dark=9,Quench=1.4f;
            /// <summary>The pour-arms: where they stand (degrees from its front, either side), how far out and how high, how big a target, each one's integrity (a share of the boss's: they
            /// take nothing off its bar, they hold it at half), and how long the second takes to tear loose with the bar at the line before it goes over.</summary>
            public const float ArmBearing=50,ArmOut=3.3f,ArmHeight=2,ArmTarget=1.1f,ArmIntegrity=.15f,TearsLoose=1.4f;
            /// <summary>Pour: its reach past its body, the sector's angle, the tell, the blow; the crust it leaves (how many, how big, how long, the pace across it).</summary>
            public const float PourReach=12,PourAngle=70,PourTell=1.1f,PourDamage=142,CrustRadius=2.4f,CrustSeconds=6,CrustPace=.6f;
            public const int PourCrusts=2;
            /// <summary>Stomp: its reach past its body, the tell whole and coming apart, the blow and the shove, and how long its feet are in the floor.</summary>
            public const float StompReach=4.5f,StompTell=1f,StompTellLater=.85f,StompDamage=144,StompShove=2.6f,Lodged=2.2f;
            /// <summary>Slag Bombs: how many, the fan's angle, the nearest and farthest a bomb lands past its body, the second row's step out, the landing's radius, the time in the air (whole and
            /// coming apart) and the blow.</summary>
            public const int Bombs=5;
            public const float BombSpread=90,BombNear=6,BombFar=15,BombStagger=3.2f,BombRadius=2,BombTell=1.3f,BombTellLater=1.15f,BombDamage=138;
            /// <summary>Furnace Breath: the lane's length past its body and its width, the tell whole and coming apart, the blow, and how long it vents after.</summary>
            public const float BreathLength=18,BreathWidth=3.2f,BreathTell=1.1f,BreathTellLater=.9f,BreathDamage=146,Vented=1.8f;
            /// <summary>Spill: where its safe ring under the crucible's lip ends past its body, how far the slag runs past its body, the tell, the blow, the shove, how long the crust lasts and
            /// how many patches of it; how long it stands emptied.</summary>
            public const float SpillInner=.9f,SpillReach=6,SpillTell=1.1f,SpillDamage=122,SpillShove=1.6f,Emptied=2.4f;
            public const int SpillCrusts=8;
            /// <summary>Its leak coming apart: a pool every few metres it walks, how big, how long bright (a danger) and then crust (a boundary), and its burn a second while bright.</summary>
            public const float LeakEvery=3.5f,LeakRadius=1.8f,LeakBright=2.5f,LeakCrust=3,LeakTick=34;
            /// <summary>Its rest between specials: glowing, cooled, coming apart.</summary>
            public const float RestHot=3,RestCooled=2.6f,RestApart=2.2f;
        }
        /// <summary>Slag that slows as it crusts: a boundary in cooled iron's steel-blue, never a danger's colour (D217), round a body of black crust. Coolant, and the glow of what is still hot.
        /// At (.6, .62, .68) with nothing under it a crust was a grey, see-through disc in its own grey steam, and nobody could tell it from steam or hazard (whole-game capture s49/046).</summary>
        static readonly Color CrustTone=new(.6f,.7f,.86f,.44f),CoolantTone=new(.7f,.94f,1.1f,.85f),SlagTone=new(1.6f,.62f,.22f,.9f);
        /// <summary>The crusts it has laid, each with its body, which sinks into the floor over its last half second.</summary>
        readonly List<(LingeringArea area,Transform body)> colossusCrustBodies=new();
        static Material crustLook;static bool crustLookTried;Light colossusLight;
        /// <summary>Crust where slag ran: the slowing ground (its ring the boundary every slow is) and what it is, drawn under the ring: a lobed patch of black, ropy slag skin, a little
        /// raised and dull, cooled iron's pale ash at its edge. Dark crust is safe ground in this chapter (the Flows' rule); bright slag is the danger, drawn as one. The body is the zone's
        /// own material from Resources (the Foundry's builder makes it), so the player draws it as the editor does.</summary>
        LingeringArea Crust(Vector3 at,float radius,float seconds)
        {
            var area=LingeringArea.Lay(at,radius,seconds,AreaEffect.Slow,ColossusTune.CrustPace,0,health,DamageType.Plasma,CrustTone);
            if(!crustLookTried){crustLookTried=true;crustLook=Resources.Load<Material>("WorldMaterials/foundry-slag-crust")??Resources.Load<Material>("WorldMaterials/scoria-crust");}
            if(crustLook==null)return area;
            var body=new GameObject("Slag crust",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(area.transform,false);body.transform.position=at+Vector3.up*.02f;
            body.GetComponent<MeshFilter>().sharedMesh=CrustMesh(radius,Mathf.RoundToInt(at.x*13+at.z*7));
            var view=body.GetComponent<MeshRenderer>();view.sharedMaterial=crustLook;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            colossusCrustBodies.Add((area,body.transform));return area;
        }
        static Mesh CrustMesh(float radius,int seed)
        {
            const int Around=20;var points=new List<Vector3>{new(0,.035f,0)};var tones=new List<Color>{new(.34f,.33f,.34f)};var tris=new List<int>();
            float s=seed*.37f;
            for(int k=0;k<Around;k++)
            {
                float a=k*Mathf.PI*2/Around,lobe=.84f+.2f*Mathf.PerlinNoise(Mathf.Cos(a)*1.6f+s,Mathf.Sin(a)*1.6f+s*.7f)+.06f*Mathf.Sin(a*5+s);
                var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                points.Add(d*radius*lobe*.62f+Vector3.up*.03f);tones.Add(new Color(.26f,.25f,.26f));
                points.Add(d*radius*lobe+Vector3.up*.012f);tones.Add(new Color(.58f,.58f,.6f));
            }
            for(int k=0;k<Around;k++)
            {
                int i=1+k*2,j=1+((k+1)%Around)*2;
                tris.AddRange(new[]{0,j,i});tris.AddRange(new[]{i,j,j+1});tris.AddRange(new[]{i,j+1,i+1});
            }
            var mesh=new Mesh{name="Slag crust"};mesh.SetVertices(points);mesh.SetColors(tones);mesh.SetUVs(0,points.ConvertAll(p=>new Vector2(p.x,p.z)*.45f));mesh.SetTriangles(tris,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        /// <summary>A crust's body goes with its ring: down into the floor over the ring's last half second, not gone between two frames.</summary>
        void CrustBodies()
        {
            for(int i=colossusCrustBodies.Count-1;i>=0;i--)
            {
                var (area,body)=colossusCrustBodies[i];if(area==null||body==null){colossusCrustBodies.RemoveAt(i);continue;}
                float k=Mathf.Clamp01(area.Remaining/.5f);body.localScale=new Vector3(1,k,1);
            }
        }
        /// <summary>A heat plate on its body: a break target with its own integrity (not shared with the body, so the body's heat doesn't stop it), shut while it glows.</summary>
        sealed class ColossusPlate{public Health health;public Transform node;public Renderer slab;public SphereCollider shape;public bool shed;public float darkUntil=-1;public int index;}
        readonly List<ColossusPlate> colossusPlates=new();
        readonly List<(Vector3 at,float when)> colossusCrusts=new();
        BossParts colossusArms;
        float colossusRadius=ColossusTune.Radius,colossusBreak,colossusStuckUntil,colossusTearAt=-1,colossusLeaked,colossusHissAt;
        int colossusQuenches,colossusArmsBroken;bool colossusHears,colossusHotSaid,colossusApart;string colossusOpenWhy="";Vector3 colossusLastAt;
        Renderer[] colossusBody;Color colossusGlow=new(1,.5f,.27f);MaterialPropertyBlock colossusBlock;Material colossusPlateMaterial;
        /// <summary>A heat plate still glows on it: its body is too hot to hurt. Never coming apart, and never in a room with no sluices (it wears no plates there).</summary>
        public bool Hot{get{if(health==null||!health.Alive||Phase>=2)return false;foreach(var plate in colossusPlates)if(!plate.shed)return true;return false;}}
        /// <summary>A quenched plate is dark now: the thing to strike.</summary>
        public bool PlateDark{get{foreach(var plate in colossusPlates)if(!plate.shed&&Time.time<plate.darkUntil)return true;return false;}}
        public int PlatesShed{get{int n=0;foreach(var plate in colossusPlates)if(plate.shed)n++;return n;}}
        public int PlatesWorn=>colossusPlates.Count;
        /// <summary>Its pour-arms still on it.</summary>
        public int ArmsStanding=>colossusArms==null?0:colossusArms.Standing;
        public BossParts PourArms=>colossusArms;
        /// <summary>The plates as break targets, for a test: their Health, in the order they stand (front, right, left).</summary>
        public List<Health> HeatPlates{get{var list=new List<Health>();foreach(var plate in colossusPlates)list.Add(plate.health);return list;}}
        /// <summary>Cooled, its arms hold its bar at half: a blow brings it to the line and no further until both are off.</summary>
        bool ArmsHold=>Phase<2&&!Hot&&(ArmsStanding>0||colossusTearAt>=0);
        float ColossusLine=>health.maximum*(Thresholds.Length>0?Thresholds[0]:.5f);
        float ColossusRest=>Phase>=2?ColossusTune.RestApart:Hot?ColossusTune.RestHot:ColossusTune.RestCooled;

        void ColossusStart()
        {
            colossusRadius=GetComponent<CharacterController>().radius;colossusBreak=health.breakThreshold;colossusLastAt=transform.position;
            colossusBody=visual!=null?visual.GetComponentsInChildren<Renderer>():GetComponentsInChildren<Renderer>();
            foreach(var body in colossusBody)if(body!=null&&body.sharedMaterial!=null&&body.sharedMaterial.HasProperty("_EmissionColor")){colossusGlow=body.sharedMaterial.GetColor("_EmissionColor");colossusPlateMaterial=body.sharedMaterial;break;}
            // The plates are made as it wakes, and only in a room with sluices to cool them: the yard's Colossus wears none, and nothing can be struck or worked from the door.
            bool sluices=false;foreach(var sluice in CoolantSluice.All)if(sluice!=null&&(string.IsNullOrEmpty(sluice.settledBy)||sluice.settledBy==health.id))sluices=true;
            colossusPlates.Clear();
            if(sluices)for(int i=0;i<ColossusTune.Plates;i++)colossusPlates.Add(MakePlate(i));
            // The pour-arms, a share of its own integrity each, shut while it glows.
            colossusArms=gameObject.AddComponent<BossParts>();
            colossusArms.Build(health,"Pour-arm",2,ColossusTune.ArmOut,ColossusTune.ArmHeight,ColossusTune.ArmTarget,health.maximum*ColossusTune.ArmIntegrity,ColossusTune.ArmBearing,SlagTone);
            if(colossusArms.Parts.Count>1)colossusArms.Parts[1].transform.localPosition=Quaternion.Euler(0,-ColossusTune.ArmBearing,0)*Vector3.forward*ColossusTune.ArmOut+Vector3.up*ColossusTune.ArmHeight;
            colossusArms.Expose(!Hot);colossusArms.Broke+=ColossusArmBroken;
            // Too hot: a blow on its body lands at nothing and reads as resisted, and nothing (a break, an opening) gets through while it glows. Cooled, its arms hold the bar at half.
            health.Plate=_=>Hot?0:1;
            health.Holds=()=>Hot?health.integrity:ArmsHold?ColossusLine+1:0;
            if(Hot)health.breakThreshold=1e9f;
            health.Damaged+=ColossusStruck;CoolantSluice.Opened+=ColossusQuenched;colossusHears=true;
            colossusBlock??=new MaterialPropertyBlock();
            Debug.Log($"COLOSSUS_WAKES plates={colossusPlates.Count} arms={ArmsStanding}");
        }
        ColossusPlate MakePlate(int i)
        {
            float bearing=ColossusTune.PlateBearings[i%ColossusTune.PlateBearings.Length];
            var node=new GameObject("Heat plate "+(i+1));node.transform.SetParent(transform,false);
            node.transform.localPosition=Quaternion.Euler(0,bearing,0)*Vector3.forward*ColossusTune.PlateOut+Vector3.up*ColossusTune.PlateHeight;
            node.transform.localRotation=Quaternion.Euler(0,bearing,0);
            var part=node.AddComponent<Health>();part.id="Heat plate";part.maximum=part.integrity=health.maximum*ColossusTune.PlateIntegrity;
            part.weakness=health.weakness;part.resistance=health.resistance;part.breakThreshold=1e9f;part.Damageable=false;
            var shape=node.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=ColossusTune.PlateTarget;shape.enabled=false;
            node.AddComponent<Hurtbox>().owner=part;
            // The plate drawn: a lobed shell of the slag it is armoured in, its cracks glowing while hot and black glassy skin when quenched, with cooled iron's pale ash round its
            // edge (D336's crust, in the zone's own slag materials from Resources). It was a cube of the engine's iron, and dark it was a hard-edged black box (s49/033, D380).
            // Its look is set every frame by property block on its first material; the ash rim is the second and keeps its colour.
            var slab=new GameObject("Plate",typeof(MeshFilter),typeof(MeshRenderer));slab.transform.SetParent(node.transform,false);
            slab.GetComponent<MeshFilter>().sharedMesh=PlateMesh(i);var view=slab.GetComponent<MeshRenderer>();
            if(!plateLookTried){plateLookTried=true;plateSkin=Resources.Load<Material>("WorldMaterials/foundry-hot-slag");plateAsh=Resources.Load<Material>("WorldMaterials/foundry-slag-crust");}
            Material skin=plateSkin;
            if(skin==null&&colossusPlateMaterial!=null){skin=new Material(colossusPlateMaterial);skin.SetTextureScale("_BaseMap",new Vector2(.3f,.3f));}
            if(skin==null)skin=ActorFactory.Energy(SlagTone);
            view.sharedMaterials=plateAsh!=null?new[]{skin,plateAsh}:new[]{skin};
            view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var plate=new ColossusPlate{health=part,node=node.transform,slab=view,shape=shape,index=i};
            part.Damaged+=(_,packet,amount)=>{CombatActor.PayHit(packet,amount);if(amount>0&&engaged<0)engaged=Time.realtimeSinceStartup;};
            part.Died+=(_,__)=>PlateShed(plate);
            return plate;
        }
        static Material plateSkin,plateAsh;static bool plateLookTried;
        /// <summary>A heat plate's shell, standing on the body's flank and facing out (+z): a dome of slag with a lobed edge (submesh 0), and a band of pale ash round it sloping back
        /// to the body (submesh 1, vertex colour). As wide and as high as the slab it replaced (2.2 by 2 m), so the target it marks is where it was.</summary>
        static Mesh PlateMesh(int index)
        {
            const int Around=22;float s=index*2.3f+.7f;
            var points=new List<Vector3>{new(0,0,.32f)};var tones=new List<Color>{Color.white};var skin=new List<int>();var ash=new List<int>();
            for(int k=0;k<Around;k++)
            {
                float a=k*Mathf.PI*2/Around,lobe=.8f+.24f*Mathf.PerlinNoise(Mathf.Cos(a)*1.4f+s,Mathf.Sin(a)*1.4f+s*.6f)+.07f*Mathf.Sin(a*4+s);
                var d=new Vector3(Mathf.Cos(a)*1.08f,Mathf.Sin(a)*.96f,0);
                points.Add(d*lobe*.55f+new Vector3(0,0,.25f));tones.Add(Color.white);   // 1+k*4: the dome's shoulder
                points.Add(d*lobe+new Vector3(0,0,.04f));tones.Add(Color.white);         // 2+k*4: the skin's edge
                points.Add(d*lobe+new Vector3(0,0,.04f));tones.Add(new Color(.8f,.78f,.75f)); // 3+k*4: the ash's inner edge, where it meets the skin
                points.Add(d*lobe*1.15f+new Vector3(0,0,-.14f));tones.Add(new Color(.97f,.95f,.92f)); // 4+k*4: the ash's outer edge, down on the body
            }
            for(int k=0;k<Around;k++)
            {
                int i=1+k*4,j=1+((k+1)%Around)*4;
                skin.AddRange(new[]{0,i,j});skin.AddRange(new[]{i,i+1,j+1});skin.AddRange(new[]{i,j+1,j});
                ash.AddRange(new[]{i+2,i+3,j+3});ash.AddRange(new[]{i+2,j+3,j+2});
            }
            var mesh=new Mesh{name="Heat plate"};mesh.SetVertices(points);mesh.SetColors(tones);mesh.SetUVs(0,points.ConvertAll(p=>new Vector2(p.x,p.y)*.45f+new Vector2(s,s*.5f)));
            mesh.subMeshCount=2;mesh.SetTriangles(skin,0);mesh.SetTriangles(ash,1);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void ColossusUpdate()
        {
            // It keeps to the heart: whatever its walk toward the hero makes of that, it is drawn back within its leash.
            if(brain.Controller.enabled){var off=Flat(transform.position-brain.Home);float out_=off.magnitude;if(out_>ColossusTune.Leash)brain.Controller.Move(-off/out_*(out_-ColossusTune.Leash));}
            bool hot=Hot;
            // A plate's dark runs out: it glows again, with what it has taken.
            foreach(var plate in colossusPlates)
            {
                if(plate.shed||plate.darkUntil<0||Time.time<plate.darkUntil)continue;
                plate.darkUntil=-1;plate.health.Damageable=false;plate.shape.enabled=false;Debug.Log("PLATE_REHEATS "+(plate.index+1));
            }
            // The sluices can be turned while it glows and no plate is dark: one plate at a time.
            CoolantSluice.Offer(hot&&!PlateDark&&!shifting);
            if(colossusTearAt>=0&&Time.time>=colossusTearAt)
            {
                // The second arm torn loose: its bar goes over the line, and the shift is it coming apart.
                colossusTearAt=-1;if(health.integrity>ColossusLine-1)health.integrity=Mathf.Max(1,ColossusLine-1);
            }
            ColossusLeak();CrustBodies();ColossusLooks(hot);
            if(busy||health.Broken||brain.Passive||brain.Telegraphing)return;
            // It heaves on its four legs, the crucible slopping; coming apart it shudders.
            if(Phase<2)Pose(1.2f*Mathf.Sin(Time.time*1.1f),.05f*Mathf.Sin(Time.time*2.2f));else Pose(2+1.4f*Mathf.Sin(Time.time*2.6f),-.12f+.06f*Mathf.Sin(Time.time*7.3f));
            WorkRoundTheCamera();
        }
        /// <summary>Its glow: the body's own emission, bright while any plate glows, dull once cooled, guttering as it comes apart; each plate glowing, dark, or gone.</summary>
        void ColossusLooks(bool hot)
        {
            colossusBlock??=new MaterialPropertyBlock();
            float t=Time.time,glow=hot?1.6f+.25f*Mathf.Sin(t*3.1f):Phase<2?.55f:.35f+.25f*Mathf.PerlinNoise(t*4,7);
            // Its crucible lights the floor round it, as hot as it is: the heart is lit by what is fought in it (whole-game capture s49/033: a dim brown floor and no heat light).
            if(colossusLight==null){var at=new GameObject("Crucible glow",typeof(Light));at.transform.SetParent(transform,false);at.transform.localPosition=Vector3.up*5.4f;
                colossusLight=at.GetComponent<Light>();colossusLight.type=LightType.Point;colossusLight.range=13;colossusLight.color=new Color(1,.6f,.32f);colossusLight.shadows=LightShadows.None;}
            colossusLight.intensity=Mathf.Clamp(glow,.3f,1.9f)*(health!=null&&health.Alive?1:0);
            if(colossusBody!=null)foreach(var body in colossusBody){if(body==null||body.name=="Plate")continue;body.GetPropertyBlock(colossusBlock);colossusBlock.SetColor("_EmissionColor",colossusGlow*glow);body.SetPropertyBlock(colossusBlock);}
            foreach(var plate in colossusPlates)
            {
                if(plate.slab==null||plate.shed)continue;
                bool dark=Time.time<plate.darkUntil;
                // Dark, it steams off and fades back toward red over its last two seconds, so the end of the opening can be seen coming.
                float heat=dark?Mathf.Clamp01(1-(plate.darkUntil-Time.time)/2f)*.6f:1;
                // On its skin only (material 0): hot, the slag's cracks glow through it; dark, it is black glassy skin inside its ash rim (material 1, untouched).
                plate.slab.GetPropertyBlock(colossusBlock,0);
                colossusBlock.SetColor("_BaseColor",Color.Lerp(new Color(.22f,.2f,.2f),new Color(.5f,.3f,.22f),heat));
                colossusBlock.SetColor("_EmissionColor",new Color(2.6f,.95f,.3f)*(heat*(dark?.4f:1.1f+.2f*Mathf.Sin(t*5+plate.index))));
                plate.slab.SetPropertyBlock(colossusBlock,0);
                if(dark&&Time.frameCount%11==plate.index)CombatVfx.Burst(plate.node.position+Vector3.up*.6f,new Color(.82f,.88f,.92f,.45f),"steam");
            }
        }
        /// <summary>A blow on it. On glowing armour it lands at nothing: it hisses, and the first one says why on screen. Any hero's blow means the fight is on.</summary>
        void ColossusStruck(Health _,DamagePacket packet,float amount)
        {
            if(packet.source==null||!packet.source.friendly)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            if(!Hot||amount>0)return;
            if(Time.time>=colossusHissAt)
            {
                colossusHissAt=Time.time+.35f;var at=packet.source.transform.position;var toward=Flat(at-transform.position);float gap=toward.magnitude;
                var mark=transform.position+(gap>.01f?toward/gap:transform.forward)*colossusRadius+Vector3.up*1.6f;
                CombatVfx.Burst(mark,new Color(1f,.55f,.3f,.6f),"steam");AudioManager.PlayAt("thrusterFire_000",transform,mark,.14f,AudioBus.SFX,40,.08f,1.8f);
            }
            if(colossusHotSaid)return;
            colossusHotSaid=true;Debug.Log("COLOSSUS_HOT");
            Notices.Post("TOO HOT TO HURT","Its armour glows too hot for any blow. Turn a coolant sluice on the rim: the coolant darkens one of its plates, and a dark plate can be broken.");
        }
        /// <summary>A sluice has opened: its coolant is thrown onto the plate that has taken most (or, of those whole, the one nearest the spout), which goes dark, and the engine reels.</summary>
        void ColossusQuenched(CoolantSluice sluice)
        {
            // A static event outlives a boss the scene took away mid-fight.
            if(this==null){CoolantSluice.Opened-=ColossusQuenched;return;}
            if(health==null||!health.Alive||!isActiveAndEnabled||!Hot||shifting)return;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            var spout=sluice.Spout;ColossusPlate chosen=null;float most=-1,nearest=float.PositiveInfinity;
            foreach(var plate in colossusPlates)
            {
                if(plate.shed||Time.time<plate.darkUntil)continue;
                float taken=1-plate.health.integrity/Mathf.Max(1,plate.health.maximum),gap=(plate.node.position-spout).sqrMagnitude;
                if(taken>most+.001f||Mathf.Abs(taken-most)<=.001f&&gap<nearest){most=taken;nearest=gap;chosen=plate;}
            }
            if(chosen==null)return;
            chosen.darkUntil=Time.time+ColossusTune.Dark;chosen.health.Damageable=true;chosen.shape.enabled=true;colossusQuenches++;
            Debug.Log($"PLATE_DARK {chosen.index+1} sluice={sluice.index}");
            // Whatever it was doing, the quench rocks it back: a turned wheel can take a blow off a hero.
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;nextSpecial=Mathf.Max(nextSpecial,Time.time+ColossusTune.Quench+.6f);
            StartCoroutine(CoolantJet(spout,chosen.node));StartCoroutine(ColossusReels(ColossusTune.Quench));
            Tremor(.35f,.08f);AudioManager.PlayAt("water_splash",transform,chosen.node.position,.4f,AudioBus.SFX,50,.08f,.7f);
            // Tovra's third call down the speaking tube at the first dark plate (her first two are the dungeon's two wheels), and after it the party's own word: one line at a time.
            if(colossusQuenches==1)BarkService.PlayAt("Tovra","hail.sluice",2,true);
            else BarkService.Play(PartyController.Current.Active.character,Situation("bossopening"),true);
        }
        IEnumerator ColossusReels(float seconds)
        {
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.3f))*Mathf.SmoothStep(0,1,Mathf.Clamp01((seconds-t)/.4f));
                Pose(-10*k+2*Mathf.Sin(t*19)*k,.15f*k);yield return null;
            }
            Pose(0);
        }
        /// <summary>The coolant thrown from the spout onto the plate: a jet that arcs over and breaks in steam.</summary>
        IEnumerator CoolantJet(Vector3 from,Transform onto)
        {
            var go=new GameObject("Coolant jet");temporary.Add(go);var jet=go.AddComponent<LineRenderer>();jet.positionCount=12;jet.useWorldSpace=true;jet.startWidth=.5f;jet.endWidth=.9f;
            jet.sharedMaterial=ActorFactory.Energy(CoolantTone);jet.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(float t=0;t<.9f&&onto!=null;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;float reach=Mathf.Clamp01(t/.25f),fade=Mathf.Clamp01((.9f-t)/.3f);
                var to=onto.position;float arc=Mathf.Max(1.5f,Flat(to-from).magnitude*.12f);
                for(int i=0;i<12;i++){float k=i/11f*reach;jet.SetPosition(i,Vector3.Lerp(from,to,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*arc));}
                jet.startColor=jet.endColor=new Color(1,1,1,fade);
                if(Time.frameCount%3==0&&reach>=1)CombatVfx.Burst(to+Random.insideUnitSphere*.6f,new Color(.85f,.92f,.96f,.55f),"steam");
                yield return null;
            }
            temporary.Remove(go);Destroy(go);
        }
        /// <summary>A dark plate broken: shed for good, an eighth of its bar with it. The third leaves it cooled: its body can be hurt and its arms open.</summary>
        void PlateShed(ColossusPlate plate)
        {
            if(health==null||!health.Alive||plate.shed)return;
            plate.shed=true;plate.darkUntil=-1;plate.shape.enabled=false;plate.health.Damageable=false;
            if(engaged<0)engaged=Time.realtimeSinceStartup;
            float line=ColossusLine,after=health.integrity-health.maximum*ColossusTune.PlateBar;
            if(health.integrity>line+1)health.integrity=Mathf.Max(line+1,after);
            Debug.Log($"PLATE_SHED {plate.index+1} shed={PlatesShed}");
            Tremor(.45f,.1f);AudioManager.PlayAt("impactMetal_000",transform,plate.node.position,.32f,AudioBus.SFX,50,.05f,.6f);
            CombatVfx.Burst(plate.node.position,new Color(.7f,.72f,.78f),"crack");
            if(plate.slab!=null)StartCoroutine(PlateFalls(plate.slab.transform));
            if(Hot)return;
            // The last plate: it is cooling iron now.
            health.breakThreshold=colossusBreak;colossusArms.Expose(true);CoolantSluice.Offer(false);
            Debug.Log("COLOSSUS_COOLED");
        }
        /// <summary>A shed plate falls off the body, turning, and is gone.</summary>
        IEnumerator PlateFalls(Transform slab)
        {
            slab.SetParent(null,true);temporary.Add(slab.gameObject);var from=slab.position;var outward=Flat(from-transform.position).normalized;var spin=Random.onUnitSphere*240;
            for(float t=0;t<1.1f&&slab!=null;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;
                slab.position=from+outward*(1.2f*t)+Vector3.up*(1.2f*t-4.9f*t*t);slab.Rotate(spin*Time.deltaTime,Space.World);
                if(slab.position.y<transform.position.y+.1f)break;
                yield return null;
            }
            if(slab!=null){CombatVfx.Burst(slab.position,new Color(.5f,.48f,.46f),"kill");temporary.Remove(slab.gameObject);Destroy(slab.gameObject);}
        }
        void ColossusArmBroken(BossParts parts,int index)
        {
            if(health==null||!health.Alive)return;
            colossusArmsBroken++;if(engaged<0)engaged=Time.realtimeSinceStartup;
            Tremor(.45f,.11f);AudioManager.PlayAt("explosionCrunch_000",transform,parts.Parts[index].transform.position,.3f,AudioBus.SFX,50,.05f,.7f);
            BarkService.PlayAt(PartyController.Current.Active.character,"arm",parts.Standing>0?0:1,true);
            // The arms take nothing off its bar: they hold it at half. With one still on, nothing more; with both off and its body not yet fought down to the line, the line simply stops holding.
            if(parts.Standing>0){Debug.Log("COLOSSUS_ARM_OFF 1");return;}
            Debug.Log("COLOSSUS_ARMS_OFF");
            if(health.integrity>ColossusLine+1.5f)return;
            // Its bar held at the line: the second tears loose, it reels a beat, then the bar goes over and its shift is it coming apart.
            colossusTearAt=Time.time+ColossusTune.TearsLoose;
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;nextSpecial=Mathf.Max(nextSpecial,colossusTearAt+.4f);StartCoroutine(ColossusReels(ColossusTune.TearsLoose));
            Debug.Log("COLOSSUS_TEARS_LOOSE");
        }
        /// <summary>What its shift brings: anything still on it comes off (in a room with no sluices its arms are torn loose here), the sluices are done with, and it begins to leak.</summary>
        void ColossusComesApart()
        {
            colossusApart=true;colossusTearAt=-1;
            foreach(var plate in colossusPlates)if(!plate.shed){plate.shed=true;plate.health.integrity=0;plate.health.Damageable=false;plate.shape.enabled=false;if(plate.slab!=null)StartCoroutine(PlateFalls(plate.slab.transform));}
            if(colossusArms!=null)for(int i=0;i<colossusArms.Parts.Count;i++)colossusArms.Settle(i);
            CoolantSluice.Offer(false);health.breakThreshold=colossusBreak;colossusLeaked=0;colossusLastAt=transform.position;
            for(int i=0;i<4;i++)CombatVfx.Burst(transform.position+Quaternion.Euler(0,i*90+45,0)*Vector3.forward*colossusRadius+Vector3.up*(1.4f+i*.4f),SlagTone,"kill");
            Debug.Log("COLOSSUS_LEAKS");
        }
        /// <summary>Coming apart, it leaks where it walks: a bright pool (a danger, it burns) that crusts dark after a few seconds and only slows. Its own walking is measured, not a body put somewhere.</summary>
        void ColossusLeak()
        {
            for(int i=colossusCrusts.Count-1;i>=0;i--)
            {
                if(Time.time<colossusCrusts[i].when)continue;
                Crust(colossusCrusts[i].at,ColossusTune.LeakRadius,ColossusTune.LeakCrust);
                colossusCrusts.RemoveAt(i);
            }
            var now=transform.position;float step=Flat(now-colossusLastAt).magnitude;colossusLastAt=now;
            if(!colossusApart||Phase<2||step>2||GameTime.Paused)return;
            colossusLeaked+=step;if(colossusLeaked<ColossusTune.LeakEvery)return;
            colossusLeaked=0;var at=CombatCover.Ground(now-transform.forward*(colossusRadius*.6f)+Vector3.up*3);
            LingeringArea.Lay(at,ColossusTune.LeakRadius,ColossusTune.LeakBright,AreaEffect.Burn,1,ColossusTune.LeakTick,health,DamageType.Plasma);
            colossusCrusts.Add((at,Time.time+ColossusTune.LeakBright));
            CombatVfx.Burst(at+Vector3.up*.3f,SlagTone,"steam");
        }
        void ColossusDown()
        {
            if(colossusHears){CoolantSluice.Opened-=ColossusQuenched;if(health!=null)health.Damaged-=ColossusStruck;colossusHears=false;}
            CoolantSluice.SettleAll();colossusCrusts.Clear();LingeringArea.DismissAll();if(colossusLight!=null)colossusLight.enabled=false;
            if(colossusArms!=null)colossusArms.Expose(false);
            foreach(var plate in colossusPlates){plate.health.Damageable=false;plate.shape.enabled=false;}
        }
        string ColossusStatus()
        {
            if(health==null||!health.Alive)return "";
            if(Time.time<colossusStuckUntil&&Hot)return colossusOpenWhy+"  ·  TURN A WHEEL";
            if(Hot)return PlateDark?"A PLATE IS DARK  ·  BREAK IT":"TOO HOT TO HURT  ·  OPEN A SLUICE";
            if(health.Broken&&Time.time<openingUntil)return colossusOpenWhy+"  ·  STRIKE";
            if(Phase<2&&ArmsStanding>0)return "ITS ARMS HOLD IT  ·  BREAK THE POUR-ARMS";
            return "";
        }
        string ColossusGoal(string label)
        {
            if(health==null||!health.Alive)return label;
            if(Hot)return PlateDark?"Hit the dark plate while it lasts":"Open a sluice: it is too hot to hurt";
            if(Phase<2&&colossusArms!=null&&colossusArms.Parts.Count>0)return $"Break the pour-arms ({colossusArms.Parts.Count-ArmsStanding}/{colossusArms.Parts.Count})";
            return label;
        }
        /// <summary>What the lead says at a cooled opening: the shared line, never the dark plate's.</summary>
        public const string OpeningLine="bossopening";
        /// <summary>Its stuck and venting beats: while it glows they open nothing (its body is too hot), and they are the time to get to a wheel; cooled they are an opening like any boss's.</summary>
        IEnumerator ColossusOpens(float seconds,string why,string said)
        {
            colossusOpenWhy=why;Debug.Log(said);Pose(12,-.25f);strikeAt=0;
            if(Hot)
            {
                colossusStuckUntil=Time.time+seconds;
                for(float t=0;t<seconds;){if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;yield return null;}
                Pose(0);Finish(ColossusRest);yield break;
            }
            // Cooled, its plates are gone: the opening is its body's, said as any boss's. Its own line is the dark plate's, said at the quench (D387: 'That plate's gone dark. Hit
            // the dark.' came at every stuck and vent after the plates were shed, and nobody could find the plate it meant, nov23-whole s49/043).
            BarkService.Play(PartyController.Current.Active.character,OpeningLine,true);
            yield return Opening(seconds,ColossusRest);Pose(0);
        }

        (string,IEnumerator) ChooseColossus(CombatActor hero)
        {
            float edge=Flat(hero.transform.position-transform.position).magnitude-colossusRadius;string move;
            // Its stomp and its breath are its openings (and while it glows, a hero's time at a wheel): each comes round whatever the range. Its pour needs an arm.
            if(Phase<2)move=Pick(("Stomp",edge<ColossusTune.StompReach+.5f?2f:FullOffer),("Pour",ArmsStanding>0&&edge<ColossusTune.PourReach?1.6f:0),
                ("SlagBombs",edge>ColossusTune.BombNear?1.5f:.4f),("FurnaceBreath",edge>3?1.4f:FullOffer));
            else move=Pick(("Stomp",edge<ColossusTune.StompReach+.5f?1.8f:FullOffer),("SlagBombs",edge>ColossusTune.BombNear?1.4f:.4f),("FurnaceBreath",edge>3?1.3f:FullOffer),
                ("Spill",edge<ColossusTune.SpillReach+2?1.6f:.6f));
            return move switch{"Pour"=>(move,SlagPour()),"SlagBombs"=>(move,SlagBombs()),"FurnaceBreath"=>(move,FurnaceBreath()),"Spill"=>(move,SlagSpill()),_=>(move,SlagStomp())};
        }
        IEnumerator SlagPour()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            var sector=Mark(Telegraph.Cone(transform.position,direction,colossusRadius+ColossusTune.PourReach,ColossusTune.PourAngle,ColossusTune.PourTell,Telegraph.Heavy));
            strikeAt=Time.time+ColossusTune.PourTell;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*3,.26f,AudioBus.SFX,50,.05f,.6f);
            // An arm lifts its ladle over the sector and tips it: the engine leans in after it.
            yield return Windup(ColossusTune.PourTell,-6,.3f,.03f);
            if(Interrupted)yield break;
            Pose(8);Strike(sector,ColossusTune.PourDamage,DamageType.Plasma,.3f);Tremor(.3f,.07f);
            AudioManager.PlayAt("water_rush",null,transform.position+direction*(colossusRadius+5),.3f,AudioBus.SFX,50,.1f,.55f);
            for(int i=1;i<=4;i++)CombatVfx.Burst(transform.position+direction*(colossusRadius+ColossusTune.PourReach*i/5f)+Vector3.up*.3f,SlagTone,"kill");
            // What it poured crusts where it ran: slowing ground along the sector's middle, a boundary and not a danger.
            for(int i=1;i<=ColossusTune.PourCrusts;i++)
            {
                var at=CombatCover.Ground(transform.position+direction*(colossusRadius+ColossusTune.PourReach*i/(ColossusTune.PourCrusts+1f))+Vector3.up*3);
                Crust(at,ColossusTune.CrustRadius,ColossusTune.CrustSeconds);
            }
            strikeAt=0;yield return Windup(.25f,0);Pose(0);Finish(ColossusRest);
        }
        IEnumerator SlagStomp()
        {
            float tell=Phase>=2?ColossusTune.StompTellLater:ColossusTune.StompTell;
            var ring=Mark(Telegraph.Circle(transform.position,colossusRadius+ColossusTune.StompReach,tell,Telegraph.Heavy));
            strikeAt=Time.time+tell;
            // It rears on its back legs, the crucible tipping back, and comes down.
            yield return Windup(tell,-9,.8f,.05f);
            if(Interrupted)yield break;
            Pose(10,-.2f);Strike(ring,ColossusTune.StompDamage,DamageType.Kinetic,.4f,ColossusTune.StompShove);Tremor(.5f,.12f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position,.3f,AudioBus.SFX,60,.05f,.7f);
            for(int i=0;i<6;i++)CombatVfx.Burst(transform.position+Quaternion.Euler(0,i*60,0)*Vector3.forward*(colossusRadius+2)+Vector3.up*.3f,new Color(.55f,.5f,.46f),"kill");
            // Its front feet have bitten into the floor.
            yield return ColossusOpens(ColossusTune.Lodged,"ITS FEET ARE IN THE FLOOR","COLOSSUS_LODGED");
        }
        IEnumerator SlagBombs()
        {
            var lead=PartyController.Current.Active.transform.position;var direction=Toward(lead);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Phase>=2?ColossusTune.BombTellLater:ColossusTune.BombTell;
            // The middle bomb comes down on him; the others across the fan, every other one a row further out, so there are gaps between them to stand in.
            float toHero=Mathf.Clamp(Flat(lead-transform.position).magnitude,colossusRadius+ColossusTune.BombNear,colossusRadius+ColossusTune.BombFar);
            var circles=new List<Telegraph>();var shells=new List<(Transform shell,Vector3 to)>();var mouth=transform.position+Vector3.up*6.4f;
            for(int i=0;i<ColossusTune.Bombs;i++)
            {
                float turn=(i/(ColossusTune.Bombs-1f)-.5f)*ColossusTune.BombSpread,out_=toHero+((i-ColossusTune.Bombs/2)%2!=0?ColossusTune.BombStagger:0);
                var to=CombatCover.Ground(transform.position+Quaternion.Euler(0,turn,0)*direction*out_+Vector3.up*3);
                circles.Add(Mark(Telegraph.Circle(to,ColossusTune.BombRadius,tell,Telegraph.Heavy)));
                var shell=GameObject.CreatePrimitive(PrimitiveType.Sphere);shell.name="Slag bomb";Destroy(shell.GetComponent<Collider>());shell.transform.localScale=Vector3.one*.9f;
                shell.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(SlagTone);shell.transform.position=mouth;temporary.Add(shell);shells.Add((shell.transform,to));
            }
            AudioManager.PlayAt("thrusterFire_000",transform,mouth,.26f,AudioBus.SFX,50,.05f,.55f);
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.Clamp01(t/tell);Pose(k<.25f?-8*k/.25f:4,.2f);ThreatFrom(circles);
                foreach(var (shell,to) in shells)if(shell!=null)shell.position=Vector3.Lerp(mouth,to,k)+Vector3.up*(Mathf.Sin(k*Mathf.PI)*6);
                yield return null;
            }
            var once=new HashSet<Health>();
            foreach(var circle in circles){Strike(circle,ColossusTune.BombDamage,DamageType.Plasma,.25f,0,once);CombatVfx.Burst(circle.Origin+Vector3.up*.3f,SlagTone,"kill");}
            foreach(var (shell,_) in shells)if(shell!=null){temporary.Remove(shell.gameObject);Destroy(shell.gameObject);}
            Tremor(.3f,.07f);AudioManager.PlayAt("explosionCrunch_000",null,lead,.26f,AudioBus.SFX,50,.05f,1.2f);
            strikeAt=0;Pose(0);Finish(ColossusRest);
        }
        IEnumerator FurnaceBreath()
        {
            var direction=Toward(PartyController.Current.Active.transform.position);transform.rotation=Quaternion.LookRotation(direction);
            float tell=Phase>=2?ColossusTune.BreathTellLater:ColossusTune.BreathTell;var origin=transform.position+direction*(colossusRadius*.6f);
            var lane=Mark(Telegraph.Line(origin,direction,ColossusTune.BreathLength+colossusRadius*.4f,ColossusTune.BreathWidth,tell,Telegraph.Threat));
            strikeAt=Time.time+tell;
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*4,.26f,AudioBus.SFX,50,.05f,.5f);
            // Its furnace door swings open and draws: the glow at its mouth builds through the tell.
            for(float t=0;t<tell;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/tell));Pose(-7*k,.2f*k,.03f*k*k);
                if(Time.frameCount%4==0)CombatVfx.Burst(origin+Vector3.up*3.2f,SlagTone,"steam");
                yield return null;
            }
            // Fire is stopped by stone: a stack between it and a hero takes the breath.
            Pose(7);Strike(lane,ColossusTune.BreathDamage,DamageType.Plasma,.3f,0,Sheltered());
            AudioManager.PlayAt("laserSmall_000",transform,origin+Vector3.up*2,.28f,AudioBus.SFX,50,.05f,.45f);
            for(int i=1;i<=5;i++)CombatVfx.Burst(origin+direction*(ColossusTune.BreathLength*i/6f)+Vector3.up*.8f,SlagTone,"kill");
            yield return ColossusOpens(ColossusTune.Vented,"ITS FURNACE IS VENTING","COLOSSUS_VENTS");
        }
        IEnumerator SlagSpill()
        {
            float inner=colossusRadius+ColossusTune.SpillInner,outer=colossusRadius+ColossusTune.SpillReach;
            // Under the crucible's lip, against its legs, is out of the run: the ring is drawn from there out.
            var ring=Mark(Telegraph.Ring(transform.position,inner,outer,ColossusTune.SpillTell,Telegraph.Heavy));
            strikeAt=Time.time+ColossusTune.SpillTell;
            AudioManager.PlayAt("spaceEngineLow_000",transform,transform.position+Vector3.up*4,.28f,AudioBus.SFX,50,.05f,.5f);
            yield return Windup(ColossusTune.SpillTell,8,.2f,.06f);
            if(Interrupted)yield break;
            Pose(-12,.1f);Strike(ring,ColossusTune.SpillDamage,DamageType.Plasma,.3f,ColossusTune.SpillShove);Tremor(.45f,.1f);
            AudioManager.PlayAt("water_rush",null,transform.position,.34f,AudioBus.SFX,50,.1f,.5f);
            for(int i=0;i<ColossusTune.SpillCrusts;i++)
            {
                var at=CombatCover.Ground(transform.position+Quaternion.Euler(0,i*360f/ColossusTune.SpillCrusts,0)*Vector3.forward*((inner+outer)*.5f)+Vector3.up*3);
                CombatVfx.Burst(at+Vector3.up*.3f,SlagTone,"kill");
                Crust(at,ColossusTune.CrustRadius,ColossusTune.CrustSeconds);
            }
            yield return ColossusOpens(ColossusTune.Emptied,"ITS CRUCIBLE IS EMPTY","COLOSSUS_EMPTIED");
        }
    }
}
