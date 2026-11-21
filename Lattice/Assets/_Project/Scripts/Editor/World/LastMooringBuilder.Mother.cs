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
    /// Mother of Doors (brief, "The fights"; manifest section 5): not an enemy with a bar to empty but a rescue expressed as a boss fight. The adult Choir coiled round the nursery at Sorrel, pinned by four clamps; her sweeps and pressure-waves huge, readable and given generous lanes, narrowing as the herders hum and each clamp comes off; each clamp's guns flown down, the shelf docked, the release done on foot while the partner covers, the launch; the fourth opened alongside her as she strains. She goes, and the young follow her out. Nobody speaks of killing her, and no string says she was defeated.
    /// <para>Owner: the Mother of Doors engineer. Her moves and every number are <c>S/Combat/BossController.MotherOfDoors.cs</c> (<see cref="BossController.MotherTune"/>), her body
    /// <c>S/Combat/MotherCoil.cs</c>, the fourth shelf's swing <c>S/Combat/MotherStrain.cs</c>; her sheet and tests <c>T/PlayMode/LastMooringBossTests.cs</c>; what she needs of her room is
    /// docs/maps/SorrelSecondSky.md, "Boss arena contract". How a checkpoint, a defeat and a Continue behave at each dock is docs/chapters/ch10-wiring.md, section 6.2 and D285.</para>
    /// <para>This file: her definition (with her Kit registered in the same change, so nothing that spawns her throws <c>BOSS_UNREGISTERED</c>), and the two calls the Sorrel zone makes on its
    /// shelves: <see cref="Mother.StandClamp"/> at each seat and <see cref="Mother.StandStrain"/> on the fourth shelf's root.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static class Mother
        {
            /// <summary>Her integrity: in her own room her bar is her four clamps and no blow lands, so it is her bar's scale and the measure of a fight with no clamps (the yards'),
            /// sized so that one lasts as long as her four cycles do (LastMooringBossTests pins the literal below to the asset).</summary>
            internal static void Define()
            {
                // She never fights by her brain's own attack: every blow is a marked special. One at her weight that reaches nobody, so her brain stands still.
                var still=ArenaBuilder.Asset<AttackDef>("MotherStill",a=>{a.damage=180;a.range=0;a.telegraph=1;a.cooldown=99;a.type=DamageType.Kinetic;a.radius=1;a.projectile=false;});
                // Her head (16 m, generated) with the Cantor's body and tail in her grade (9 m, 11 m), chained by her own code. A pressure creature: a pulse carries to her, a slug
                // doesn't. A Mine to her brain (pinned, she never walks). Her capsule is her head's, 4 m round and 8 tall. Released, a freed creature pays the party's Sync and nothing else.
                ArenaBuilder.Asset<EnemyDef>("MotherOfDoors",e=>{e.id="MotherOfDoors";e.displayName="MOTHER OF DOORS";e.prefab=ZoneKit.Body("Enemies","MotherOfDoors","Enemies","Cantor");
                    e.bodySegment=ZoneKit.Prefab("Enemies","MotherBody");e.tailSegment=ZoneKit.Prefab("Enemies","MotherTail");
                    // Sized from the shared length test's flat out against her in the flight yard (BOSS_RATE MotherOfDoors 1,453 a second at Sync 27, November 14): 80,000 lasts 183 s at
                    // the assumed .3 and 220 s at a quarter, inside her sheet's 120 to 300.
                    e.integrity=80000;e.breakThreshold=1400;e.speed=0;e.attacks=new[]{still};
                    e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.archetype=EnemyArchetype.Mine;e.behaviour=EnemyBehaviourKind.None;e.xp=1500;e.scrip=0;e.scrap=0;e.boss=true;
                    e.bossMusicId="Boss of the Ages";e.yieldsOnDefeat=false;e.heavyDeath=false;e.drawnScale=1;e.motion=EnemyMotion.None;
                    e.capsuleRadius=0;e.capsuleHeight=BossController.MotherTune.Height;e.lootTable=null;});
            }
            /// <summary>The hold at clamp <paramref name="index"/> (1 to 4): on an empty at the seat (the floor a hero stands on to work it, on the shelf, facing her), chapter 7's
            /// <see cref="BridleClamp"/>, hers: offered while the party stands on this shelf and she is fighting, eased in her slack, its flag <c>end.clampN</c> kept and loaded open (D285),
            /// reporting <c>Clamp</c>. The Sorrel engineer calls it for each shelf.</summary>
            internal static BridleClamp StandClamp(Transform seat,int index)
            {
                var clamp=seat.GetComponent<BridleClamp>()??seat.gameObject.AddComponent<BridleClamp>();
                clamp.index=index;clamp.settledBy="MotherOfDoors";clamp.openedFlag="end.clamp"+index;clamp.keepsOpened=true;
                return clamp;
            }
            /// <summary>The fourth shelf swings on her strain: on the root of its berth (everything of the shelf under it), round her middle (<paramref name="pivot"/>; unsaid, the root's
            /// own position, where the Sorrel zone stands it), from the shelf's bearing <paramref name="fromBearing"/> out to <paramref name="toBearing"/> and back, its way marked at
            /// <paramref name="radius"/> (docs/maps/SorrelSecondSky.md, "Boss arena contract").</summary>
            internal static MotherStrain StandStrain(Transform root,Vector3? pivot=null,float fromBearing=120,float toBearing=92,float radius=27)
            {
                var strain=root.GetComponent<MotherStrain>()??root.gameObject.AddComponent<MotherStrain>();
                var middle=pivot??root.position;middle.y=1;
                strain.pivot=middle;strain.fromBearing=fromBearing;strain.toBearing=toBearing;strain.radius=radius;strain.period=16;return strain;
            }
        }

        /// <summary>Stills of her as a player sees her (marker <c>MOTHER_LOOK_OK</c>; <c>-look-run &lt;name&gt;</c>, written to Builds/quality/look/&lt;name&gt;): in Sorrel's scene if it has
        /// been built (else a bare dark sky), her head and coil laid as play lays them at the crater's middle C (0, 1, 330), seen with the game's own cameras: a ship in the south lane at
        /// r 27, the same drawn back to the whole creature, and a hero on the third shelf. Nothing is saved.</summary>
        public static void MotherLook()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??"mother-look";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            var middle=new Vector3(0,1,330);
            if(File.Exists("Assets/_Project/Scenes/"+SorrelZone+".unity"))EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+SorrelZone+".unity");
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var sun=new GameObject("Sun",typeof(Light)).GetComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(50,-30,0);sun.intensity=1.2f;
            }
            foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var zone=Find<ZoneDef>(SorrelZone);if(zone!=null)RenderSettings.skybox=zone.skybox;
            var def=Find<EnemyDef>("MotherOfDoors");if(def==null)throw new System.InvalidOperationException("MotherOfDoors isn't defined: run LastMooringBuilder.Definitions first");
            var root=new GameObject("MotherOfDoors (look)");root.transform.SetPositionAndRotation(middle,Quaternion.Euler(0,180,0));
            Object.Instantiate(def.prefab,root.transform);
            root.AddComponent<MotherCoil>().Lay(def);
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
            camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var placed=new System.Collections.Generic.List<GameObject>();
            void Hero(string id,Vector3 at,float yaw){var prefab=ZoneKit.Prefab("Characters",id);if(prefab==null)return;var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;placed.Add(body);
                // Standing as in play, not in the pose the rig was bound in.
                foreach(var animator in body.GetComponentsInChildren<Animator>()){var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    foreach(var clip in clips)if(clip!=null&&clip.name.ToLowerInvariant().Contains("idle")){clip.SampleAnimation(animator.gameObject,.3f);break;}}}
            void Shot(string name,CameraProfile profile,Vector3 point,float zoom)
            {
                var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);camera.fieldOfView=profile.fov;
                camera.transform.SetPositionAndRotation(point-rotation*Vector3.forward*profile.distance*zoom,rotation);
                var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToJPG(88));RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
            }
            var lane=middle+Vector3.back*27;
            Hero("TarenFlight",lane+Vector3.left*2.1f,0);Hero("IdraFlight",lane+Vector3.right*2.1f,0);
            Shot("mother-flight",flight,lane,1);
            Shot("mother-flight-wide",flight,middle+Vector3.back*10,2.4f);
            foreach(var h in placed)Object.DestroyImmediate(h);placed.Clear();
            // Does her raised head ever hide a ship? One as near under her snout as her body lets a hull come (her capsule's 4 m and Idra's 2.17), one in the first lane south of her.
            var head=root.transform.GetChild(0);var drawn=new Bounds(head.position,Vector3.zero);foreach(var r in head.GetComponentsInChildren<Renderer>())drawn.Encapsulate(r.bounds);
            Debug.Log($"MOTHER_HEAD_DRAWN min={drawn.min} max={drawn.max} (her middle {middle})");
            foreach(var (name,r,bearing) in new[]{("mother-snout",6.3f,180f),("mother-lane21",21f,180f),("mother-near-south",11.8f,180f),("mother-near-north",18.3f,0f),("mother-near-northeast",16.6f,35f),("mother-near-east",11.8f,90f)})
            {
                var at=middle+Quaternion.Euler(0,bearing,0)*Vector3.forward*r;Hero("IdraFlight",at,0);Shot(name,flight,at,1);
                foreach(var h in placed)Object.DestroyImmediate(h);placed.Clear();
            }
            // The third shelf's floor where the zone stands it (a bare scene gets one at the contract's height).
            var shelf=middle+Quaternion.Euler(0,155,0)*Vector3.forward*26;shelf.y=-.8f;
            if(Physics.Raycast(shelf+Vector3.up*3,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))shelf=hit.point;
            else{var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=shelf+Vector3.down*.5f;floor.transform.localScale=new Vector3(14,1,10);placed.Add(floor);}
            Hero("TarenShaped",shelf+Vector3.left,200);Hero("IdraNatural",shelf+Vector3.right,200);
            Shot("mother-shelf",ground,shelf+Vector3.up,1);
            Debug.Log("MOTHER_LOOK_OK "+folder);
        });
    }
}
