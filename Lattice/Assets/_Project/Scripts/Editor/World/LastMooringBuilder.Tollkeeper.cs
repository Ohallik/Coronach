using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Tollkeeper (brief, "The fights"): an anchor-fleet carrier whose outriggers form a moving cage across Last Mooring's lane. Chosen braces broken to open gaps, a weave out as the cage contracts, a short dock on an arm to stop its gate motor on foot, the launch before it draws in; its guns fire marked lanes between the chains. When the motor dies the cage hangs open and the first convoy hulls come through behind the party.
    /// <para>Owner: the Tollkeeper engineer. Its moves and every number are <c>S/Combat/BossController.Tollkeeper.cs</c> (<see cref="BossController.TollTune"/>), its cage
    /// <c>S/Combat/TollkeeperCage.cs</c>; its sheet and tests <c>T/PlayMode/LastMooringBossTests.cs</c> (its own region); what it needs of its room is docs/maps/LastMooring.md, "What the
    /// fight needs of the room". How a checkpoint, a defeat and a Continue behave at the arm is docs/chapters/ch10-wiring.md, section 6.3, and D285.</para>
    /// <para>This file: its definition (its Kit registered in the same change, so nothing that spawns it throws <c>BOSS_UNREGISTERED</c>), the one call the Last Mooring zone makes at its
    /// gate (<see cref="Tollkeeper.StandCage"/>), and its stills (<see cref="TollkeeperLook"/>).</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static class Tollkeeper
        {
            /// <summary>Its integrity: in its room its bar is its motor's three stages and no blow lands on the carrier, so it is the bar's scale and the measure of a fight with no
            /// cage (the yards'), sized so that one lasts as long as its three cycles do (LastMooringBossTests pins the literal below to the asset).</summary>
            internal static void Define()
            {
                // It never fights by its brain's own attack: every blow is a marked special. One at its weight that reaches nobody, so its brain stands still.
                var still=ArenaBuilder.Asset<AttackDef>("TollkeeperStill",a=>{a.damage=188;a.range=0;a.telegraph=1;a.cooldown=99;a.type=DamageType.Beam;a.radius=1;a.projectile=false;});
                // The carrier (90 m, generated); its outrigger (40 m) is the cage's bar (TollkeeperCage reads it off this definition). A machine: a hammer's blow carries to it, a beam
                // is turned. A Mine to its brain (it lies where it lies). Its capsule in a yard is 4 m round and 8 tall; in its room it has none. Shut down, it pays the party's Sync.
                ArenaBuilder.Asset<EnemyDef>("Tollkeeper",e=>{e.id="Tollkeeper";e.displayName="THE TOLLKEEPER";e.prefab=ZoneKit.Body("Enemies","Tollkeeper","Enemies","FlakGate");
                    e.bodySegment=ZoneKit.Prefab("Enemies","TollkeeperOutrigger");e.tailSegment=null;
                    // Sized from the shared length test's flat out against it in the flight yard (BOSS_RATE Tollkeeper 1,221 a second at Sync 28 in Choirwrought, November 14):
                    // 68,000 lasts 186 s at the assumed .3 and 223 s at a quarter, inside its sheet's 120 to 300.
                    e.integrity=68000;e.breakThreshold=1400;e.speed=0;e.attacks=new[]{still};
                    e.weakness=DamageType.Kinetic;e.resistance=DamageType.Beam;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.None;e.xp=1600;e.scrip=0;e.scrap=0;e.boss=true;
                    e.bossMusicId="Glitch Boss";e.yieldsOnDefeat=false;e.heavyDeath=false;e.drawnScale=1;e.motion=EnemyMotion.None;
                    e.capsuleRadius=0;e.capsuleHeight=BossController.TollTune.Height;e.lootTable=null;});
            }
            /// <summary>The cage at the gate (docs/maps/LastMooring.md, "What the fight needs of the room"): the Last Mooring zone calls this once in its build, after the gate and the arm
            /// stand, when the boss is defined. It stands a <see cref="TollkeeperCage"/> under <paramref name="parent"/> with the gate's hinges, where the carrier lies, the lanes, the arm's
            /// root and its two places, and the motor's seat; the cage draws its outriggers and braces as the scene wakes, and keeps what the save says.</summary>
            internal static TollkeeperCage StandCage(Transform parent)
            {
                var go=new GameObject("The Tollkeeper's cage");if(parent!=null)go.transform.SetParent(parent,true);go.transform.position=(Mooring.HingeWest+Mooring.HingeEast)*.5f;
                var cage=go.AddComponent<TollkeeperCage>();
                cage.hingeWest=Mooring.HingeWest;cage.hingeEast=Mooring.HingeEast;cage.carrier=Mooring.TollkeeperAt;
                cage.lanesRun=Mooring.HingeWest.z-Mooring.Lines[Mooring.Lines.Length-1].z-1.5f;cage.dividers=Mooring.ArenaChainX;cage.outer=Mooring.OuterX;
                var arm=GameObject.Find(Mooring.ArmRoot);
                if(arm!=null){cage.arm=arm.transform;cage.armOut=Mooring.ArmOut;cage.armIn=Mooring.ArmIn;var seat=arm.transform.Find(Mooring.MotorSeat);cage.seat=seat;}
                else Debug.LogWarning("TOLL_CAGE_NO_ARM: '"+Mooring.ArmRoot+"' isn't in the scene yet: the cage stands without its arm");
                cage.berthId=Mooring.ArmBerth;cage.outriggerPrefab=ZoneKit.Prefab("Enemies","TollkeeperOutrigger");
                Debug.Log("TOLL_CAGE_STOOD hinges="+cage.hingeWest+"/"+cage.hingeEast+" arm="+(cage.arm!=null)+" seat="+(cage.seat!=null));
                return cage;
            }
        }

        /// <summary>Stills of the Tollkeeper as a player sees it (marker <c>TOLL_LOOK_OK</c>; <c>-look-run &lt;name&gt;</c>, written to Builds/quality/look/&lt;name&gt;): in Last Mooring's scene,
        /// the carrier laid where its fight lays it, its cage stood open with its braces and lamps, the arm run out with its motor; seen with the game's own cameras from the lane's mouth,
        /// at a brace, at the arm's pad, drawn back over the whole machine, on the deck at the motor, mid-contraction, and hanging open after its fall. Nothing is saved.</summary>
        public static void TollkeeperLook()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??"toll-look";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            string scenePath="Assets/_Project/Scenes/"+MooringZone+".unity";
            if(!File.Exists(scenePath))throw new System.InvalidOperationException(MooringZone+" isn't built");
            EditorSceneManager.OpenScene(scenePath);
            foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var zone=Find<ZoneDef>(MooringZone);if(zone!=null)RenderSettings.skybox=zone.skybox;
            var def=Find<EnemyDef>("Tollkeeper");if(def==null)throw new System.InvalidOperationException("Tollkeeper isn't defined: run LastMooringBuilder.Definitions first");
            // The zone's own stand-ins for an undefined fight go: the fight draws its own.
            foreach(var name in new[]{"The Tollkeeper (drawn: no fight defined yet)","The cage, closed (drawn: no fight defined yet)","The cage, hanging open","The Tollkeeper, as it stays (drawn after its fall)"})
            {var stale=GameObject.Find(name);if(stale!=null)stale.SetActive(false);}
            foreach(var stale in Object.FindObjectsByType<TollkeeperCage>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(stale.gameObject);
            // The carrier as its fight lays it: its drawn middle on the zone's T.
            var body=Object.Instantiate(def.prefab);body.name="The Tollkeeper (look)";body.transform.rotation=Quaternion.Euler(0,180,0);
            var drawn=ZoneKit.Drawn(body);body.transform.position+=Mooring.TollkeeperAt-drawn.center;drawn=ZoneKit.Drawn(body);
            Debug.Log($"TOLL_BODY_DRAWN min={drawn.min} max={drawn.max} (T {Mooring.TollkeeperAt})");
            var cage=Tollkeeper.StandCage(null);cage.Build();
            if(cage.arm!=null)cage.arm.position=Mooring.ArmOut;cage.ShowMotor(true,true);
            foreach(var rigger in cage.riggers)foreach(var b in rigger.braces)Debug.Log($"TOLL_BRACE_AT {b.index+1} {b.node.position}");
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
            camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var placed=new System.Collections.Generic.List<GameObject>();
            void Hero(string id,Vector3 at,float yaw){var prefab=ZoneKit.Prefab("Characters",id);if(prefab==null)return;var hero=(GameObject)PrefabUtility.InstantiatePrefab(prefab);hero.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                foreach(var b in hero.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;placed.Add(hero);
                foreach(var animator in hero.GetComponentsInChildren<Animator>()){var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    foreach(var clip in clips)if(clip!=null&&clip.name.ToLowerInvariant().Contains("idle")){clip.SampleAnimation(animator.gameObject,.3f);break;}}}
            void Clear(){foreach(var h in placed)if(h!=null)Object.DestroyImmediate(h);placed.Clear();}
            void Shot(string name,CameraProfile profile,Vector3 point,float zoom)
            {
                var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);camera.fieldOfView=profile.fov;
                camera.transform.SetPositionAndRotation(point-rotation*Vector3.forward*profile.distance*zoom,rotation);
                var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".jpg"),image.EncodeToJPG(86));RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
            }
            void Pair(Vector3 at){Hero("TarenFlight",at+Vector3.left*2.1f,0);Hero("IdraFlight",at+Vector3.right*2.1f,0);}
            var mouth=new Vector3(0,1,404);Pair(mouth);Shot("toll-mouth",flight,mouth,1);Clear();
            var brace=cage.riggers[0].braces[1].node.position;brace.y=1;var near=brace+new Vector3(9,0,-6);Pair(near);Shot("toll-brace",flight,near,1);Clear();
            var pad=new Vector3(0,1,446);Pair(pad);Shot("toll-arm",flight,pad,1);Clear();
            Pair(new Vector3(0,1,430));Shot("toll-wide",flight,new Vector3(0,1,436),2.6f);Clear();
            var deck=Mooring.ArmMotor+new Vector3(0,0,-2.2f);Hero("TarenShaped",deck+Vector3.left,180);Hero("IdraShaped",deck+new Vector3(1.6f,0,-1.5f),180);Shot("toll-deck",ground,deck+Vector3.up,1);Clear();
            cage.Pose(55);Pair(new Vector3(-12,1,420));Shot("toll-contract",flight,new Vector3(-6,1,430),1.6f);Clear();
            cage.HangOpen(1);if(cage.arm!=null)cage.arm.position=Mooring.ArmIn;cage.ShowMotor(false);Pair(new Vector3(0,1,440));Shot("toll-open",flight,new Vector3(0,1,446),2.2f);Clear();
            Debug.Log("TOLL_LOOK_OK "+folder);
        });
    }
}
