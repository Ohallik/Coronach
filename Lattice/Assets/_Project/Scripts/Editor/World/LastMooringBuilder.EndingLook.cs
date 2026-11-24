using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Stills of the game's ending as the whole-game capture of November 20 saw it (Builds/quality/nov20-whole, segments 66 to 69), each from the camera position that capture recorded for
    /// that still (its frames.csv) with the zone's own profile's angle, so a render can be laid beside the captured still: Meret in the control well in her emergency frame (s67/116) and
    /// standing after her yield (s67/182), the landing at the lift's foot (s67/099), the collar (s67/203) and the release drawn back with the road lit (s67/210); the spine's head (s66/013),
    /// the berth's working deck (s66/154) and her shield relays lit and dark (s66/157, s66/162); the neighbourhood's square at Orrin's board (s69/014) and the market at Jun's stall
    /// (s69/052). Bodies the fights spawn are stood where the stills show them. Marker <c>ENDING_LOOK_OK</c>; <c>-look-run &lt;name&gt;</c>, written to Builds/quality/look/&lt;name&gt;;
    /// <c>-ending-shots a,b</c> takes only those. Nothing is saved.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        public static void EndingLook()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??"ending-look";
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier look run: "+folder);
            Directory.CreateDirectory(folder);
            var only=DevArgs.Value("-ending-shots")?.Split(',');bool Wants(string name)=>only==null||only.Contains(name);
            var ground=Find<CameraProfile>("Ground");var flight=Find<CameraProfile>("Flight");
            Camera camera=null;var placed=new List<GameObject>();
            void Open(string zone)
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                var def=Find<ZoneDef>(zone);if(def!=null)RenderSettings.skybox=def.skybox;
                foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                {
                    if(animator.GetComponentInParent<Posed>()!=null)continue;
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                foreach(var posed in Object.FindObjectsByType<Posed>(FindObjectsSortMode.None))posed.Hold();
                camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);
                camera.nearClipPlane=.3f;camera.farClipPlane=1500;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
            }
            GameObject Stand(string folder,string id,Vector3 at,float yaw)
            {
                var prefab=ZoneKit.Prefab(folder,id);if(prefab==null){Debug.LogWarning("ENDING_LOOK_NO_BODY "+folder+"/"+id);return null;}
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                foreach(var animator in body.GetComponentsInChildren<Animator>())
                {
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                placed.Add(body);return body;
            }
            void Clear(){foreach(var p in placed)if(p!=null)Object.DestroyImmediate(p);placed.Clear();}
            void Shot(string name,CameraProfile profile,Vector3 from)
            {
                camera.fieldOfView=profile.fov;camera.transform.SetPositionAndRotation(from,Quaternion.Euler(profile.pitch,profile.yaw,0));
                var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
                Debug.Log("ENDING_LOOK_SHOT "+name);
            }
            // A closer look at the same angle: the profile's line through a point, a share of its distance back.
            Vector3 Near(CameraProfile profile,Vector3 point,float back)=>point-Quaternion.Euler(profile.pitch,profile.yaw,0)*Vector3.forward*profile.distance*back;
            float Yaw(float fx,float fz)=>Mathf.Atan2(fx,fz)*Mathf.Rad2Deg;

            // ---- the anchor chamber
            if(Wants("well-116")||Wants("well-182")||Wants("landing-099")||Wants("collar-203")||Wants("collar-210")||Wants("well-117"))
            {
                Open(ChamberZone);
                var meretAt=new Vector3(6.4f,Chamber.Well,62.6f);
                // nov22b-whole s67/117 (D373): her duel at its first cover anchor, in her emergency frame, from that still's camera; and closer.
                if(Wants("well-117"))
                {
                    var at=new Vector3(5.2f,Chamber.Well,71.6f);
                    Stand("Characters","TarenShaped",new Vector3(3.57f,Chamber.Well,73.07f),Yaw(.6f,-.8f));Stand("Characters","IdraNatural",new Vector3(7.6f,Chamber.Well,70.4f),-60);
                    var her=Stand("Enemies","MeretWellDirector",at,200);if(her!=null)foreach(var gear in her.GetComponentsInChildren<HessaRegalia>(true))gear.Follow();
                    // Her fight shuts the well's two seals (the plain tiled wall behind her in the still was one).
                    foreach(var seal in Object.FindObjectsByType<Membrane>(FindObjectsSortMode.None))seal.SetOpen(false);
                    Shot("well-117",ground,new Vector3(-2.196f,-2.694f,58.11f));Shot("well-117-close",ground,Near(ground,at+Vector3.up*1.1f,.32f));
                    Clear();
                }
                if(Wants("well-116"))
                {
                    Stand("Characters","TarenShaped",new Vector3(9.09f,Chamber.Well,60.74f),Yaw(-.776f,.63f));Stand("Characters","IdraNatural",new Vector3(3.2f,Chamber.Well,64.8f),110);
                    Stand("Enemies","MeretWell",meretAt,96);
                    Shot("well-116",ground,new Vector3(3.708f,-3.386f,46.996f));
                    Shot("well-116-close",ground,Near(ground,meretAt+Vector3.up*1.1f,.32f));
                    Clear();
                }
                if(Wants("well-182"))
                {
                    Stand("Characters","TarenShaped",new Vector3(7.14f,Chamber.Well,64.55f),Yaw(-.097f,-.995f));Stand("Enemies","MeretWell",Chamber.MeretPost,20);
                    Shot("well-182",ground,new Vector3(1.74f,-3.386f,50.233f));Shot("well-182-close",ground,Near(ground,Chamber.MeretPost+Vector3.up*1.1f,.32f));Clear();
                }
                if(Wants("landing-099"))
                {
                    var cage=GameObject.Find("The lift's cage (moves)");if(cage!=null)cage.transform.position=Chamber.LiftFoot;
                    foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.bounds.Contains(new Vector3(-3.6f,Chamber.Well+.6f,49.2f)))Debug.Log("ENDING_LOOK_ON_THE_SIGN "+r.name+" "+r.bounds);
                    Stand("Characters","TarenShaped",new Vector3(0,Chamber.Well,51.69f),0);Stand("Characters","IdraNatural",new Vector3(1.6f,Chamber.Well,49.4f),200);
                    Shot("landing-099",ground,new Vector3(-5.222f,-3.102f,36.94f));Clear();
                    if(cage!=null)cage.transform.position=Chamber.LiftTop;
                }
                if(Wants("collar-203")){Stand("Characters","TarenShaped",new Vector3(.18f,Chamber.Well,77.27f),Yaw(-.48f,.877f));Shot("collar-203",ground,new Vector3(-4.51f,-3.386f,62.474f));Clear();}
                if(Wants("collar-210"))
                {
                    // The road as the release lights it (AnchorRelease drives its glow by property block).
                    var anchor=Object.FindFirstObjectByType<AnchorRelease>();
                    if(anchor!=null){var block=new MaterialPropertyBlock();foreach(var r in anchor.roadGlow){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor("_EmissionColor",anchor.roadFree);r.SetPropertyBlock(block);}}
                    Stand("Characters","TarenShaped",new Vector3(.01f,Chamber.Well,77.58f),Yaw(-.48f,.877f));Shot("collar-210",ground,new Vector3(-12.15f,18.822f,43.29f));Clear();
                }
                Object.DestroyImmediate(camera.gameObject);
            }

            // ---- the Continuance: the spine's head, the berth, her relays
            if(Wants("spine-013")||Wants("berth-154")||Wants("relay-157")||Wants("relay-162")||Wants("spine-021")||Wants("berth-153"))
            {
                Open(ContinuanceZone);
                // nov22b-whole s66/021 (the spine's head, the party at the hatch) and s66/153 (into her lane over the working deck), from those stills' cameras (D373).
                if(Wants("spine-021")){Stand("Characters","TarenShaped",new Vector3(0,.08f,-8.59f),0);Stand("Characters","IdraNatural",new Vector3(3.6f,.08f,-11.4f),330);Shot("spine-021",ground,new Vector3(-5.109f,12.614f,-23.793f));Clear();}
                if(Wants("berth-153")){Stand("Characters","TarenFlight",new Vector3(-4.3f,19,211.6f),0);Shot("berth-153",flight,new Vector3(-4.091f,39.292f,195.005f));Clear();}
                if(Wants("spine-013")){Stand("Characters","TarenShaped",new Vector3(0,.08f,-14.1f),0);Stand("Characters","IdraNatural",new Vector3(1.8f,.08f,-15.2f),330);Shot("spine-013",ground,new Vector3(-5.156f,12.72f,-28.673f));Clear();}
                if(Wants("berth-154")){Stand("Characters","TarenFlight",new Vector3(-4.07f,19,213.41f),Yaw(.125f,.992f));Shot("berth-154",flight,new Vector3(-3.9f,39.045f,196.752f));Clear();}
                if(Wants("relay-157")||Wants("relay-162"))
                {
                    var her=Stand("Enemies","MeretShip",Continuance.MeretAt,180);
                    var relays=new List<MeretRelay>();var heads=Continuance.Relays;
                    for(int i=0;i<heads.Length;i++)
                    {
                        var go=new GameObject("Look relay "+i);go.transform.position=heads[i];placed.Add(go);
                        var relay=MeretRelay.Draw(go,i,GameObject.Find(MeretRelay.MastName(i))!=null?0:3.2f);relay.beam.sharedMaterial=ActorFactory.Energy(new Color(.45f,1.3f,1.6f,.35f));relays.Add(relay);
                        Debug.Log($"ENDING_LOOK_RELAY {i} head={heads[i]} mast={(GameObject.Find(MeretRelay.MastName(i))!=null)} drawn={relay.head.bounds.size}");
                    }
                    if(Wants("relay-157"))
                    {
                        foreach(var r in relays)r.Light(true,Continuance.MeretAt+Vector3.up*.6f);
                        Stand("Characters","TarenFlight",new Vector3(3.1f,19,232.19f),Yaw(.59f,.807f));Shot("relay-157",flight,new Vector3(4.083f,52.428f,208.19f));
                        Shot("relay-157-close",flight,Near(flight,heads[1],.55f));
                    }
                    if(Wants("relay-162")){foreach(var r in relays)r.Light(false,Continuance.MeretAt);Shot("relay-162",flight,new Vector3(-.821f,55.458f,206.565f));}
                    Clear();
                }
                Object.DestroyImmediate(camera.gameObject);
            }

            // ---- Last Mooring: the carrier as it stays, from where s65/157 stood (Taren's ship over an engine nacelle there)
            if(Wants("toll-157"))
            {
                Open(MooringZone);
                foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(t.name.StartsWith("The Tollkeeper, as it stays")){for(var p=t;p!=null;p=p.parent)p.gameObject.SetActive(true);Debug.Log("ENDING_LOOK_CARRIER "+ZoneKit.Drawn(t.gameObject));}
                foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)){var b=r.bounds;if(b.max.y>-2&&b.min.y<3&&b.min.z<452&&b.max.z>428&&b.min.x<30&&b.max.x>-10&&b.size.magnitude>4)Debug.Log("ENDING_LOOK_AT_THE_PLANE "+r.name+" "+b);}
                Stand("Characters","TarenFlight",new Vector3(10,1,440),Yaw(.998f,-.065f));Shot("toll-157",flight,new Vector3(9.782f,20.308f,422.937f));Clear();
                Object.DestroyImmediate(camera.gameObject);
            }

            // ---- the flight zones' haze as played (D344): each view drawn back wide twice, with the fog as the lens alone has it and as PlaneHaze sets it for that camera. Last Mooring
            // from where the capture's own drawn-back stills stood (s64/071, s64/073, s65/010), the Returning Passage from its wide views. A haze set as a camera begins to draw doesn't
            // reach a still drawn in the editor, so it is set for each camera first.
            void Hazed(string zone,(string name,Vector3 from)[] views)
            {
                Open(zone);var haze=Object.FindFirstObjectByType<PlaneHaze>();Debug.Log("ENDING_LOOK_HAZE "+zone+" planeHaze="+(haze!=null));
                foreach(var (name,from) in views)foreach(bool plane in new[]{false,true})
                {
                    if(haze!=null){haze.enabled=plane;if(plane)haze.ApplyFor(camera);else{RenderSettings.fogStartDistance=haze.start;RenderSettings.fogEndDistance=haze.end;}}
                    camera.fieldOfView=flight.fov;camera.transform.SetPositionAndRotation(from,Quaternion.Euler(flight.pitch,flight.yaw,0));if(haze!=null&&plane)haze.ApplyFor(camera);
                    Shot(name+(plane?"-plane":"-lens"),flight,from);
                    Debug.Log($"ENDING_LOOK_FOG {name} {(plane?"plane":"lens")} {RenderSettings.fogStartDistance:0} to {RenderSettings.fogEndDistance:0}");
                }
                Object.DestroyImmediate(camera.gameObject);
            }
            if(Wants("haze-mooring"))Hazed(MooringZone,new[]{("haze-s64-071",new Vector3(-.013f,122.488f,253.68f)),("haze-s64-073",new Vector3(0,304.592f,107.65f)),("haze-s65-010",new Vector3(-.379f,232.008f,214.115f))});
            if(Wants("haze-passage"))
            {
                var rotation=Quaternion.Euler(flight.pitch,flight.yaw,0);
                Hazed(PassageZone,Passage.Views.Where(v=>v.flight&&v.zoom>=3).Select(v=>("haze-passage-"+v.name,v.point-rotation*Vector3.forward*flight.distance*v.zoom)).ToArray());
            }

            // ---- the neighbourhood: Orrin's board, the market
            if(Wants("afloat-014")||Wants("afloat-052"))
            {
                Open(AfloatZone);
                if(Wants("afloat-014")){Stand("Characters","TarenNatural",new Vector3(-12.02f,.08f,68.03f),Yaw(.971f,.238f));Shot("afloat-014",ground,new Vector3(-17.518f,12.614f,53.891f));Shot("afloat-014-close",ground,Near(ground,new Vector3(-12.4f,1.6f,72.2f),.45f));Clear();}
                if(Wants("afloat-052")){Stand("Characters","TarenNatural",new Vector3(-14.32f,.08f,23.41f),Yaw(.792f,-.611f));Shot("afloat-052",ground,new Vector3(-19.747f,12.614f,9.619f));Shot("afloat-052-close",ground,Near(ground,new Vector3(-17.4f,1,21.5f),.45f));Clear();}
                Object.DestroyImmediate(camera.gameObject);
            }
            Debug.Log("ENDING_LOOK_OK "+folder);
        });
    }
}
