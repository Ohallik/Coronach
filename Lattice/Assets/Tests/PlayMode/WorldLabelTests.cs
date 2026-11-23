using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The words drawn in the world (places on the floor, gates, people's names) read as a player reads them: from the play camera, as rendered. The size, the level and the
    /// material were checked for a year (EverySignReadsLevelToTheCamera, WorldLabelsReadFromTheGamesCameraAtTheTypeFloor, FloorSignsReadFromTheFixedCamera...) and the November 20
    /// capture still showed sixty words cut: 'HE PRESS' behind a lamp post, 'F… ARTER' behind a well, DACE lying mirrored on the floor, TERN'S HALL white on white snow. Nothing had
    /// looked at what is drawn in front of a word, at what is behind it, or at a name whose person had been turned. These do, one case per zone.</summary>
    // A zone's case is seconds to a minute; the stills set their own.
    [Timeout(420000)]
    public sealed class WorldLabelTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members[1-PartyController.Current.index];
        static CameraProfile View=>ZoneController.Current.definition.cameraProfile;
        static bool Flight=>ZoneController.Current.Flight;
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            // Nothing a measurement does starts a fight: an encounter's box is what wakes one.
            foreach(var fight in All<EncounterVolume>())foreach(var box in fight.GetComponents<Collider>())box.enabled=false;
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members)
            {
                actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;
                actor.Health.maximum=actor.Health.integrity=90000;
            }
            yield return new WaitForSecondsRealtime(.6f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var cc=hero.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;hero.transform.position=at;if(cc!=null)cc.enabled=true;Physics.SyncTransforms();
        }

        // ---------------------------------------------------------------- what a label is
        /// <summary>Every word a builder laid in the world (WorldBuilder.Label's "Sign_", which a person's name is too) that is drawn as the zone loads.</summary>
        static List<TextMeshPro> Labels()=>All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&!string.IsNullOrEmpty(t.text)&&t.gameObject.activeInHierarchy&&t.enabled
            &&t.TryGetComponent<Renderer>(out var view)&&view.enabled).ToList();
        static Npc Owner(TextMeshPro label)
        {
            var npc=label.GetComponentInParent<Npc>(true);if(npc!=null)return npc;
            foreach(var person in All<Npc>())if(label.name=="Sign_"+DisplayNames.Speaker(person.speaker))return person;
            return null;
        }
        static string Named(string zone,TextMeshPro label)=>$"{zone} {(Owner(label)!=null?"name":"sign")} \"{Words(label)}\"";
        /// <summary>A label's words as the player reads them: no tags, and not the hidden copy a name's backing is drawn over (SignStyle.Backed).</summary>
        static string Words(TextMeshPro label)=>System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(label.text,"<color=#00000000>.*?</color>|​",""),"<[^>]+>","").Trim();
        static string From(Read read)=>read.from==null?"":$" (seen with the party by \"{read.from}\")";
        static Quaternion Turn=>Quaternion.Euler(View.pitch,View.yaw,0);
        static Vector3 TowardCamera{get{var back=-(Turn*Vector3.forward);back.y=0;return back.normalized;}}
        static float FloorUnder(Vector3 at)
        {
            foreach(var hit in Physics.RaycastAll(at+Vector3.up*.5f,Vector3.down,12,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(!(hit.collider is CharacterController)&&hit.normal.y>.6f)return hit.point.y;
            return at.y;
        }
        /// <summary>Where the camera looks when a player sees this word: the party three metres short of it on the camera's side, on the floor, or on the plane in flight.</summary>
        static Vector3 Focus(TextMeshPro label)
        {
            var person=Owner(label);var at=person!=null&&label.transform.IsChildOf(person.transform)?person.transform.position:label.transform.position;
            if(Flight)at.y=1;else if(person==null||!label.transform.IsChildOf(person.transform))at.y=FloorUnder(at);
            return at+TowardCamera*3;
        }

        // ---------------------------------------------------------------- the camera's three looks at a view
        const int W=960,H=540;
        sealed class Read{public string name,from;public bool person;public int core,seen;public float ratio,ink,behind;public string over,why;}
        sealed class ZoneReads{public readonly List<Read> reads=new();public readonly List<string> unmeasured=new();}
        static int probeLayer=-1;
        static int ProbeLayer(Camera camera)
        {
            if(probeLayer>=0)return probeLayer;
            for(int l=31;l>=8;l--)if(string.IsNullOrEmpty(LayerMask.LayerToName(l))&&(camera.cullingMask&(1<<l))==0){probeLayer=l;return l;}
            return probeLayer=31;
        }
        static Color32[] Shoot(Camera camera,RenderTexture target,Texture2D into)
        {
            var before=RenderTexture.active;camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;into.ReadPixels(new Rect(0,0,target.width,target.height),0,0,false);RenderTexture.active=before;
            return into.GetPixels32();
        }
        static RectInt OnScreen(Camera camera,TextMeshPro label,float pad)
        {
            label.ForceMeshUpdate();var b=label.textBounds;float xMin=1e9f,xMax=-1e9f,yMin=1e9f,yMax=-1e9f;
            foreach(var x in new[]{b.min.x,b.max.x})foreach(var y in new[]{b.min.y,b.max.y})
            {
                var p=camera.WorldToScreenPoint(label.transform.TransformPoint(new Vector3(x,y,0)));if(p.z<=0)return new RectInt(-1,-1,0,0);
                xMin=Mathf.Min(xMin,p.x);xMax=Mathf.Max(xMax,p.x);yMin=Mathf.Min(yMin,p.y);yMax=Mathf.Max(yMax,p.y);
            }
            return new RectInt(Mathf.FloorToInt(xMin-pad),Mathf.FloorToInt(yMin-pad),Mathf.CeilToInt(xMax-xMin+2*pad),Mathf.CeilToInt(yMax-yMin+2*pad));
        }
        static bool Inside(RectInt r)=>r.width>0&&r.xMin>=W*.04f&&r.xMax<=W*.96f&&r.yMin>=H*.04f&&r.yMax<=H*.96f;
        static float Linear(byte c){float v=c/255f;return v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);}
        static float Luminance(Color32 c)=>.2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b);
        static float Percentile(List<float> values,float share){if(values.Count==0)return 0;values.Sort();return values[Mathf.Clamp(Mathf.RoundToInt(share*(values.Count-1)),0,values.Count-1)];}

        /// <summary>Reads every drawn label in a zone from the play camera, a view a word: centred on it, the party three metres short of it; and again from every other word's view that
        /// holds it whole, apart from the words round it (a word at the top of the screen as the party stands by another is seen there too). Each view is rendered three ways by the game's own camera (post-processing and all): the words alone on black (where their
        /// letters are), the world with the words' faces turned magenta (which of those letters reach the screen), and the world as drawn (what the letters stand against).</summary>
        static IEnumerator ReadZone(string zone,ZoneReads found)
        {
            yield return Zone(zone);yield return new WaitForSecondsRealtime(.8f);
            var camera=Camera.main;Assert.IsNotNull(camera,zone+": no game camera");
            int layer=ProbeLayer(camera);
            var labels=Labels();
            var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(W,H,TextureFormat.RGBA32,false);
            var bodies=PartyController.Current.members.SelectMany(m=>m.GetComponentsInChildren<Renderer>()).Where(r=>!r.forceRenderingOff).ToArray();
            Vector3 cameraAt=camera.transform.position;var cameraTurn=camera.transform.rotation;float fov=camera.fieldOfView;var mask=camera.cullingMask;var clear=camera.clearFlags;var back=camera.backgroundColor;var was=camera.targetTexture;
            try
            {
                foreach(var first in labels)
                {
                    camera.transform.SetPositionAndRotation(Focus(first)-Turn*Vector3.forward*View.distance,Turn);camera.fieldOfView=View.fov;camera.targetTexture=target;
                    // The word this view is centred on, and every other word wholly inside it that crosses no other word there (each one's letters are told apart by where they are).
                    // Every word gets its own view: read only as a second word near another's frame, which view a word was read from went by the order the scene lists them, and a
                    // re-save of DecksLeaving moved ARRIVALS / DOCK OFFICE from its own view to the corner of GATE SIX's (November 21).
                    var own=OnScreen(camera,first,4);
                    if(!Inside(own)){found.unmeasured.Add(Named(zone,first)+" (its view doesn't hold it)");continue;}
                    var inside=labels.Where(l=>l!=first).Select(l=>(label:l,rect:OnScreen(camera,l,4))).Where(h=>Inside(h.rect)).ToList();
                    var here=new List<(TextMeshPro label,RectInt rect)>{(first,own)};
                    here.AddRange(inside.Where(h=>!h.rect.Overlaps(own)&&!inside.Any(o=>o.label!=h.label&&o.rect.Overlaps(h.rect))));
                    // Shown at full strength while read: when a word steps aside is SignFade's business, what it looks like when shown is this test's.
                    var kept=new List<(TextMeshPro label,SignFade fade,bool on,float alpha,Color color,int layer,Renderer plate,MaterialPropertyBlock block)>();
                    foreach(var (label,_) in here)
                    {
                        label.TryGetComponent<SignFade>(out var fade);var plateAt=label.transform.Find(SignStyle.PlateName);var plate=plateAt!=null?plateAt.GetComponent<Renderer>():null;
                        MaterialPropertyBlock block=null;if(plate!=null){block=new MaterialPropertyBlock();plate.GetPropertyBlock(block);plate.SetPropertyBlock(null);}
                        kept.Add((label,fade,fade!=null&&fade.enabled,label.alpha,label.color,label.gameObject.layer,plate,block));
                        if(fade!=null)fade.enabled=false;label.alpha=1;
                    }
                    foreach(var r in bodies)r.forceRenderingOff=true;
                    // Alone, on black, in white: where its letters are, whatever its ink.
                    foreach(var k in kept){k.label.gameObject.layer=layer;k.label.color=new Color(1,1,1,1);k.label.ForceMeshUpdate();}
                    camera.cullingMask=1<<layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                    var alone=Shoot(camera,target,image);
                    foreach(var k in kept)k.label.gameObject.layer=k.layer;
                    camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;
                    // Faces in magenta.
                    foreach(var k in kept){k.label.color=new Color(1,0,1,1);k.label.ForceMeshUpdate();}
                    var marked=Shoot(camera,target,image);
                    foreach(var k in kept){k.label.color=k.color;k.label.alpha=1;k.label.ForceMeshUpdate();}
                    // As drawn.
                    var drawn=Shoot(camera,target,image);
                    var measured=here.Select(h=>Measure(zone,camera,h.label,h.rect,alone,marked,drawn)).ToList();
                    for(int i=1;i<measured.Count;i++)measured[i].from=Words(first);
                    // A word that doesn't stand out says why, while the view is still posed as it was read: what its plate is, and what is behind it as drawn, with every transparent
                    // thing over it taken away, and with its plate taken away.
                    for(int i=0;i<here.Count;i++)if(measured[i].seen>=10&&measured[i].ratio<LeastContrast)measured[i].why=WhyFaint(camera,here[i].label,here[i].rect,alone,drawn,target,image,measured[i].ink);
                    // A cut word's view is kept as the camera had it, as drawn and with the word's faces in magenta, for the zone's engineer to see what stands over it.
                    for(int i=0;i<here.Count;i++)if(measured[i].core>=10&&1-measured[i].seen/(float)measured[i].core>MostHidden)
                    {
                        string stem=Letters(here[i].label)+"-cut-from-"+Letters(first);
                        Keep(stem+".png",drawn);Keep(stem+"-magenta.png",marked);
                    }
                    foreach(var r in bodies)r.forceRenderingOff=false;
                    foreach(var k in kept)
                    {
                        k.label.color=k.color;k.label.alpha=k.alpha;if(k.fade!=null)k.fade.enabled=k.on;if(k.plate!=null)k.plate.SetPropertyBlock(k.block);
                    }
                    found.reads.AddRange(measured);
                    camera.targetTexture=was;
                }
            }
            finally
            {
                camera.targetTexture=was;camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;camera.fieldOfView=fov;camera.transform.SetPositionAndRotation(cameraAt,cameraTurn);
                foreach(var r in bodies)if(r!=null)r.forceRenderingOff=false;
                target.Release();Object.Destroy(target);Object.Destroy(image);
            }
        }
        static Read Measure(string zone,Camera camera,TextMeshPro label,RectInt rect,Color32[] alone,Color32[] marked,Color32[] drawn)
        {
            var read=new Read{name=Named(zone,label),person=Owner(label)!=null};
            int x0=Mathf.Max(0,rect.xMin),x1=Mathf.Min(W-1,rect.xMax),y0=Mathf.Max(0,rect.yMin),y1=Mathf.Min(H-1,rect.yMax);
            bool Letter(int x,int y)=>x>=0&&y>=0&&x<W&&y<H&&Mathf.Max(alone[y*W+x].r,Mathf.Max(alone[y*W+x].g,alone[y*W+x].b))>115;
            var ink=new List<float>();var hidden=new List<Vector2Int>();
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                // A letter's inside, a pixel in from its edge: the edge is blended with whatever is behind it either way.
                if(!Letter(x,y)||!Letter(x-1,y)||!Letter(x+1,y)||!Letter(x,y-1)||!Letter(x,y+1))continue;
                read.core++;int i=y*W+x;
                // Where the word reaches the screen the two renders differ: magenta against its own ink, light or dark, in one channel or another. Where something stands in front they're the same.
                var d=drawn[i];var m=marked[i];
                if(Mathf.Max(Mathf.Abs(d.r-m.r),Mathf.Max(Mathf.Abs(d.g-m.g),Mathf.Abs(d.b-m.b)))>64){read.seen++;ink.Add(Luminance(d));}else if(hidden.Count<400)hidden.Add(new Vector2Int(x,y));
            }
            // What the letters stand against: round them and between them, clear of their dark edge.
            int pad=Mathf.Max(4,Mathf.RoundToInt(rect.height*.3f));var around=new List<float>();
            for(int y=Mathf.Max(0,rect.yMin-pad);y<=Mathf.Min(H-1,rect.yMax+pad);y++)for(int x=Mathf.Max(0,rect.xMin-pad);x<=Mathf.Min(W-1,rect.xMax+pad);x++)
            {
                bool near=false;for(int dy=-3;dy<=3&&!near;dy++)for(int dx=-3;dx<=3&&!near;dx++)near=Letter(x+dx,y+dy);
                if(!near)around.Add(Luminance(drawn[y*W+x]));
            }
            // Against the side of what is behind it nearest its own ink: the brighter fifth behind light ink, the darker fifth behind dark ink (a dark word lying half on a dark patch is
            // read against the patch).
            read.ink=Percentile(ink,.5f);bool darkInk=read.ink<Percentile(new List<float>(around),.5f);read.behind=Percentile(around,darkInk?.2f:.8f);
            read.ratio=(Mathf.Max(read.ink,read.behind)+.05f)/(Mathf.Min(read.ink,read.behind)+.05f);
            if(hidden.Count>0)read.over=WhatCovers(camera,label,hidden);
            return read;
        }
        /// <summary>The brighter or darker fifth (by the ink's side) of what is round a word's letters in a render.</summary>
        static float Surround(RectInt rect,Color32[] alone,Color32[] shot,bool darkInk)
        {
            bool Letter(int x,int y)=>x>=0&&y>=0&&x<W&&y<H&&Mathf.Max(alone[y*W+x].r,Mathf.Max(alone[y*W+x].g,alone[y*W+x].b))>115;
            int pad=Mathf.Max(4,Mathf.RoundToInt(rect.height*.3f));var around=new List<float>();
            for(int y=Mathf.Max(0,rect.yMin-pad);y<=Mathf.Min(H-1,rect.yMax+pad);y++)for(int x=Mathf.Max(0,rect.xMin-pad);x<=Mathf.Min(W-1,rect.xMax+pad);x++)
            {
                bool near=false;for(int dy=-3;dy<=3&&!near;dy++)for(int dx=-3;dx<=3&&!near;dx++)near=Letter(x+dx,y+dy);
                if(!near)around.Add(Luminance(shot[y*W+x]));
            }
            return Percentile(around,darkInk?.2f:.8f);
        }
        static string WhyFaint(Camera camera,TextMeshPro label,RectInt rect,Color32[] alone,Color32[] drawn,RenderTexture target,Texture2D image,float ink)
        {
            var plateAt=label.transform.Find(SignStyle.PlateName);var plate=plateAt!=null?plateAt.GetComponent<Renderer>():null;
            string paint=plate!=null&&plate.sharedMaterial!=null?$"{plate.sharedMaterial.name} at {plate.sharedMaterial.renderQueue}, {plate.sharedMaterial.GetColor("_BaseColor").a:0.00}":"no plate";
            var box=new Rect(rect.xMin,rect.yMin,rect.width,rect.height);
            var over=All<Renderer>().Where(r=>r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy&&r!=plate&&r.GetComponentInParent<TMP_Text>()==null&&r.sharedMaterial!=null&&r.sharedMaterial.renderQueue>2500
                &&PanelFade.ScreenBounds(camera,r.bounds,out var on)&&on.Overlaps(box)).ToArray();
            bool dark=ink<.2f;float asDrawn=Surround(rect,alone,drawn,dark);
            foreach(var r in over)r.forceRenderingOff=true;
            float without=Surround(rect,alone,Shoot(camera,target,image),dark);
            foreach(var r in over)r.forceRenderingOff=false;
            float bare=asDrawn;if(plate!=null){plate.forceRenderingOff=true;bare=Surround(rect,alone,Shoot(camera,target,image),dark);plate.forceRenderingOff=false;}
            // And with the camera's post-processing off: what the grade and the bloom do to the plate's dark.
            float flat=float.NaN;var data=System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            var urp=data!=null?camera.GetComponent(data):null;var post=data?.GetProperty("renderPostProcessing");
            if(urp!=null&&post!=null){var was=post.GetValue(urp);post.SetValue(urp,false);flat=Surround(rect,alone,Shoot(camera,target,image),dark);post.SetValue(urp,was);}
            // Kept for the eye: the view round the word as drawn.
            var folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/labels/why"));Directory.CreateDirectory(folder);
            var crop=new Texture2D(W,H,TextureFormat.RGBA32,false);crop.SetPixels32(drawn);crop.Apply();
            File.WriteAllBytes(System.IO.Path.Combine(folder,Letters(label)+".png"),crop.EncodeToPNG());Object.Destroy(crop);
            string names=string.Join(", ",over.Take(6).Select(r=>$"{Path(r.transform)} ({r.sharedMaterial.name} at {r.sharedMaterial.renderQueue})"));
            return $"{(dark?"dark":"light")} ink on {paint}; behind {asDrawn:0.00} as drawn, {without:0.00} without the {over.Length} transparent things over it [{names}], {bare:0.00} without its plate, {flat:0.00} without post-processing";
        }
        static string Letters(TextMeshPro label)=>System.Text.RegularExpressions.Regex.Replace(Words(label),"[^A-Za-z]","");
        /// <summary>A render of a view, kept under Builds/quality/labels/why.</summary>
        static void Keep(string file,Color32[] pixels)
        {
            var folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/labels/why"));Directory.CreateDirectory(folder);
            var shot=new Texture2D(W,H,TextureFormat.RGBA32,false);shot.SetPixels32(pixels);shot.Apply();File.WriteAllBytes(System.IO.Path.Combine(folder,file),shot.EncodeToPNG());Object.Destroy(shot);
        }
        /// <summary>What stands in front of a word's hidden letters, by name, for the zone's engineer: the nearest drawn thing on the camera's line to them.</summary>
        static string WhatCovers(Camera camera,TextMeshPro label,List<Vector2Int> hidden)
        {
            var names=new Dictionary<string,int>();float reach=Vector3.Distance(camera.transform.position,label.transform.position);
            var parts=All<Renderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r is MeshRenderer or SkinnedMeshRenderer&&r.GetComponentInParent<TMP_Text>()==null
                &&Vector3.Distance(r.bounds.center,label.transform.position)<30+r.bounds.extents.magnitude).ToArray();
            for(int i=0;i<hidden.Count;i+=Mathf.Max(1,hidden.Count/12))
            {
                var ray=camera.ScreenPointToRay(new Vector3(hidden[i].x,hidden[i].y,0));string best=null;float nearest=reach-.3f;
                if(Physics.Raycast(ray,out var hit,reach-.3f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.TryGetComponent<Renderer>(out var solid)){best=Path(solid.transform);nearest=hit.distance;}
                foreach(var part in parts)if(part.bounds.IntersectRay(ray,out float along)&&along<nearest&&part.bounds.size.magnitude<14){nearest=along;best=Path(part.transform);}
                if(best!=null)names[best]=names.TryGetValue(best,out int n)?n+1:1;
            }
            if(names.Count>0)return string.Join(", ",names.OrderByDescending(n=>n.Value).Take(2).Select(n=>n.Key));
            // Nothing solid: what is drawn over those letters' place on the screen without a body, nearest the camera first (a transparent thing drawn after the word covers it).
            var box=Rect.MinMaxRect(hidden.Min(h=>h.x),hidden.Min(h=>h.y),hidden.Max(h=>h.x)+1,hidden.Max(h=>h.y)+1);var own=label.GetComponent<Renderer>();var eye=camera.transform.position;
            var drawn=All<Renderer>().Where(r=>r!=own&&r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy&&(camera.cullingMask&(1<<r.gameObject.layer))!=0&&r.sharedMaterial!=null&&r.sharedMaterial.renderQueue>2500
                &&PanelFade.ScreenBounds(camera,r.bounds,out var on)&&on.Overlaps(box)).OrderBy(r=>Vector3.Distance(eye,r.bounds.center)).Take(5)
                .Select(r=>$"{Path(r.transform)} ({r.GetType().Name}, {r.sharedMaterial.name} at {r.sharedMaterial.renderQueue}, {Vector3.Distance(eye,r.bounds.center):0.0} m)");
            return $"something drawn but not solid (a glow, a decal, a mark){(drawn.Any()?": "+string.Join(", ",drawn):"")}; the word's own middle {Vector3.Distance(eye,own!=null?own.bounds.center:label.transform.position):0.0} m";
        }
        static string Path(Transform t){var up=t.parent;return up!=null&&up.parent!=null?up.name+"/"+t.name:t.name;}

        // ---------------------------------------------------------------- per zone
        static string[] SightZones=>ContentRegistry.With(nameof(EveryLabelIsSeenWholeFromThePlayCamera),ContentRegistry.FirstZones);
        static string[] ContrastZones=>ContentRegistry.With(nameof(EveryLabelStandsOutFromWhatIsBehindIt),ContentRegistry.FirstZones);
        static string[] UprightZones=>ContentRegistry.With(nameof(EveryLabelReadsUprightToTheCamera),ContentRegistry.FirstZones);
        /// <summary>A word is cut when more than this share of its letters' insides doesn't reach the screen. A lamp post across a T is about a tenth of 'THE PRESS'.</summary>
        const float MostHidden=.04f;
        /// <summary>Large type wants 3 to 1 between its letters and what they stand on (WCAG's large-text floor); TERN'S HALL on Lastlight's snow measured about 1.4.</summary>
        const float LeastContrast=3;
        static List<string> Cut(ZoneReads found)
        {
            var failures=new List<string>();
            foreach(var read in found.reads)
            {
                if(read.core<10)continue;
                float hidden=1-read.seen/(float)read.core;
                if(hidden>MostHidden)failures.Add($"{read.name} is cut{From(read)}: {hidden:P0} of its letters don't reach the screen, behind {read.over}");
            }
            return failures;
        }
        static List<string> Faint(ZoneReads found)
        {
            var failures=new List<string>();
            foreach(var read in found.reads)
            {
                if(read.seen<10)continue;
                if(read.ratio<LeastContrast)failures.Add($"{read.name} stands {read.ratio:0.0} to 1{From(read)} against what is behind it (letters {read.ink:0.00}, behind {read.behind:0.00}): under {LeastContrast} it doesn't read. {read.why}");
            }
            return failures;
        }
        static void Log(string what,string zone,int read,List<string> failures)
        {
            TestContext.WriteLine($"LABEL_{what} {zone} read={read} wrong={failures.Count}");
            foreach(var f in failures)TestContext.WriteLine($"LABEL_{what}_ITEM {f}");
        }
        /// <summary>A zone's views as a case read them earlier in this run, or nothing (then the asking case reads them itself).</summary>
        static ZoneReads Reads(string check,string zone)
        {
            try{return ZoneWalk.Kept<ZoneReads>(check,zone);}
            catch(System.InvalidOperationException){return null;}
        }
        [UnityTest] public IEnumerator EveryLabelIsSeenWholeFromThePlayCamera([ValueSource(nameof(SightZones))] string zone)
        {
            var found=new ZoneReads();yield return ReadZone(zone,found);
            ZoneWalk.Keep(nameof(EveryLabelIsSeenWholeFromThePlayCamera),zone,found);
            var failures=Cut(found);Log("SIGHT",zone,found.reads.Count,failures);
            foreach(var u in found.unmeasured)TestContext.WriteLine("LABEL_UNMEASURED "+u);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator EveryLabelStandsOutFromWhatIsBehindIt([ValueSource(nameof(ContrastZones))] string zone)
        {
            var found=Reads(nameof(EveryLabelIsSeenWholeFromThePlayCamera),zone);
            if(found==null){found=new ZoneReads();yield return ReadZone(zone,found);}
            var failures=Faint(found);Log("CONTRAST",zone,found.reads.Count,failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        static void Upright(string zone,List<string> failures,out int read)
        {
            read=0;var right=Turn*Vector3.right;var up=Turn*Vector3.up;var look=Turn*Vector3.forward;
            foreach(var label in Labels())
            {
                read++;var t=label.transform;string name=Named(zone,label);
                float slant=Vector3.Angle(Vector3.ProjectOnPlane(t.right,look),right);
                if(Vector3.Dot(t.forward,look)<.25f)failures.Add($"{name} faces away from the camera: it reads mirrored or not at all");
                else if(slant>5)failures.Add($"{name} slants {slant:0} degrees across the frame");
                else if(Vector3.Dot(t.up,up)<.5f)failures.Add($"{name} is tipped {Vector3.Angle(t.up,up):0} degrees off the camera's upright: it lies flat or reads upside down");
            }
        }
        [UnityTest] public IEnumerator EveryLabelReadsUprightToTheCamera([ValueSource(nameof(UprightZones))] string zone)
        {
            yield return Zone(zone);
            // Long enough for whoever is posed, seated or turned by the zone's own staging to be so.
            yield return new WaitForSecondsRealtime(1.5f);
            var failures=new List<string>();Upright(zone,failures,out int read);Log("UPRIGHT",zone,read,failures);
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        // ---------------------------------------------------------------- the moments the per-zone cases can't stand in
        /// <summary>Plays a moment and asks that it doesn't show the fault. Each was played under the rules before November 21 first and showed it (labels-full2: BELLOWS CHAMBER at .88
        /// on the bar's first frame, LOWER CHAMBER at 1.00 through a fight and at .75 under a hero's feet, a word six pixels from the edge at 1.00, a word under the type floor at
        /// 1.00, eight names at the meeting turned).</summary>
        IEnumerator Holds(System.Func<System.Action<float>,IEnumerator> moment,System.Func<float,bool> wrong,string fault)
        {
            float now=float.NaN;yield return moment(v=>now=v);
            Assert.IsFalse(wrong(now),$"{fault} ({now:0.00})");
        }
        /// <summary>s57/055 and 136: the Decks on the day of leaving, the town meeting seated. Jun, Dace and Oda wait their turn with their talk shut, so nothing turned their names to
        /// the camera: each kept the builder's tilt and turned with its body, DACE upside down by the grate, ODA flat on the floor by the benches.</summary>
        [UnityTest] public IEnumerator TheMeetingsNamesReadUprightWhileTheirPeopleWaitTheirTurn()
        {
            var said=new List<string>();
            IEnumerator Moment(System.Action<float> got)
            {
                foreach(var f in new[]{"home.treadRecovered","home.returned","home.meetingGathered"})Flag(f);
                yield return Zone("DecksLeaving","Meeting");yield return new WaitForSecondsRealtime(1.5f);
                var shut=All<Npc>().Where(n=>n.gameObject.activeInHierarchy&&!n.enabled).ToArray();
                Assert.GreaterOrEqual(shut.Length,2,"fixture: people at the meeting waiting with their talk shut");
                var failures=new List<string>();Upright("DecksLeaving",failures,out int read);
                Assert.Greater(read,8,"fixture: the meeting's names");
                foreach(var f in failures)TestContext.WriteLine("LABEL_MEETING "+f);
                said.Clear();said.AddRange(failures);got(failures.Count);
            }
            yield return Holds(Moment,v=>v>0,"names at the meeting lie flat, slant or read mirrored");
            Assert.IsEmpty(said,string.Join("\n",said));
        }
        /// <summary>s15/135: the Hessa Korl fight opens and GREAT LANTERN is still on the floor between the bar and the hint strip. It went in a tenth of a second, which a still catches.
        /// A place's name has no business under a fight's bar for even a frame.</summary>
        [UnityTest] public IEnumerator APlaceLabelIsGoneTheFrameABossBarComesUp()
        {
            IEnumerator Moment(System.Action<float> got)
            {
                yield return Zone("Hushwell");
                var sign=All<TextMeshPro>().First(t=>t.text=="BELLOWS CHAMBER");
                foreach(var hero in PartyController.Current.members)Put(hero,new Vector3(hero==Lead?3:-7,HushwellLayout.Middle+.1f,247));
                yield return new WaitForSecondsRealtime(1f);
                Assert.Greater(sign.alpha,.9f,"fixture: nobody near the sign, so it shows");
                All<EncounterVolume>().Single(e=>e.encounterId=="Hushwell_Bellows").Begin();
                float deadline=Time.unscaledTime+5;while(BossController.Focus(Lead.transform.position)==null&&Time.unscaledTime<deadline)yield return null;
                var boss=BossController.Focus(Lead.transform.position);
                Assert.IsNotNull(boss,"fixture: five seconds after the Bellows fight begins, its boss bar still hasn't come up");
                // The frame the bar came up has had its late update: what the sign did there is what a still of that frame shows. (Not WaitForEndOfFrame: a Unity with no
                // window never reaches it, and a run waited on it, with the Bellows fighting, for three hours.)
                yield return null;
                got(sign.alpha);
                // Woken for this and nothing else: stilled and gone before anything else is read.
                foreach(var brain in boss.GetComponentsInChildren<EnemyBrain>(true))brain.Passive=true;
                Object.Destroy(boss.gameObject);yield return null;
            }
            yield return Holds(Moment,v=>v>=.05f,"BELLOWS CHAMBER is still drawn on the frame the boss bar comes up: a still catches it between the bar and the hints");
        }
        /// <summary>s15/027: CUT CORRIDOR on the floor under a scavenger's telegraph lines. With a fight on round the party a place's name goes, and comes back when it's over.</summary>
        [UnityTest] public IEnumerator APlaceLabelStepsAsideWhileAFightIsOnAroundTheParty()
        {
            float back=0;
            IEnumerator Moment(System.Action<float> got)
            {
                yield return Zone("Hushwell");
                var sign=All<TextMeshPro>().First(t=>t.text=="LOWER CHAMBER");
                Put(Lead,sign.transform.position+TowardCamera*5+Vector3.up*.1f);yield return new WaitForSecondsRealtime(1f);
                Assert.Greater(sign.alpha,.9f,"fixture: the sign shows with nobody on it");
                var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Lead.transform.position+Vector3.right*7);hound.Passive=false;
                yield return new WaitForSecondsRealtime(.6f);
                got(sign.alpha);
                hound.Passive=true;Object.Destroy(hound.gameObject);yield return new WaitForSecondsRealtime(1.2f);
                back=sign.alpha;
            }
            yield return Holds(Moment,v=>v>=.05f,"LOWER CHAMBER stays on the floor through a fight, under its telegraphs");
            Assert.Greater(back,.9f,"the sign doesn't come back once the fight is over");
        }
        /// <summary>s43/044 and s08/112: the party standing on a word left it at a third of its strength through their legs, 'LAST LANTERN' and 'LO R CHAMBER'. A body over a word's
        /// letters takes it down to a trace (AWordUnderAHerosLegsStepsAside asks that it isn't gone outright), not to a ghost.</summary>
        [UnityTest] public IEnumerator AWordUnderThePartysFeetIsATraceNotAGhost()
        {
            float shown=float.NaN;
            IEnumerator Moment(System.Action<float> got)
            {
                yield return Zone("Hushwell");
                var sign=All<TextMeshPro>().First(t=>t.text=="LOWER CHAMBER");var along=sign.transform.right;along.y=0;along.Normalize();var at=sign.transform.position;
                Put(Partner,at+along*12+TowardCamera*8);
                Put(Lead,at+TowardCamera*6);yield return new WaitForSecondsRealtime(1f);
                Assert.Greater(sign.alpha,.9f,"fixture: the sign shows with nobody on it");
                // On its line, a metre along: where Taren stood in s43/044.
                Put(Lead,at+along*1.1f+Vector3.up*.1f);yield return new WaitForSecondsRealtime(1f);
                var camera=Camera.main;var feet=camera.WorldToScreenPoint(Lead.transform.position);
                sign.ForceMeshUpdate();var b=sign.textBounds;var low=camera.WorldToScreenPoint(sign.transform.TransformPoint(new Vector3(b.center.x,b.min.y,0)));var high=camera.WorldToScreenPoint(sign.transform.TransformPoint(new Vector3(b.center.x,b.max.y,0)));
                Assert.IsTrue(feet.y<Mathf.Max(low.y,high.y),"fixture: the hero's body rises over the word's letters on the screen");
                shown=sign.alpha;got(sign.alpha);
            }
            yield return Holds(Moment,v=>v>.1f,"LOWER CHAMBER reads as a ghost through the legs of the hero standing on its letters");
            Assert.Greater(shown,.02f,"the word went altogether, which AWordUnderAHerosLegsStepsAside rules out");
        }
        static TextMeshPro FixtureSign(string words)
        {
            var go=new GameObject("Sign_Fixture",typeof(TextMeshPro));go.transform.rotation=Quaternion.Euler(72,View.yaw,0);
            var text=go.GetComponent<TextMeshPro>();text.font=UiSkin.Display;text.text=words;text.fontSize=UiKit.WorldFontSize(40);text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;
            // A person's name, as the words this stands for were (KEEPER ABRI, PIM): since D356 a hanging sign steps aside for a hero under it, and a name doesn't.
            var fade=go.AddComponent<SignFade>();fade.bossOnly=true;fade.nameTag=true;return text;
        }
        static Vector3 Under(Vector2 screen,float height)
        {
            var ray=Camera.main.ScreenPointToRay(screen);float along=(height-ray.origin.y)/ray.direction.y;return ray.origin+ray.direction*along;
        }
        /// <summary>s28/170 and s27: KEEPER ABRI eight pixels from the screen's left edge, PIM hard against the zone's tag. A word with nothing between it and the edge, or a plate, reads as
        /// cut off by it.</summary>
        [UnityTest] public IEnumerator AWordHardByTheScreensEdgeOrAPlateGoes()
        {
            float byTag=-1;
            IEnumerator Moment(System.Action<float> got)
            {
                yield return Zone("Hub_Decks");yield return new WaitForSecondsRealtime(.8f);
                var camera=Camera.main;float px=camera.pixelHeight/1080f;var sign=FixtureSign("KEEPER ABRI");
                void Lay(Vector2 screen){sign.transform.position=Under(screen,Lead.transform.position.y+.05f);}
                Rect Letters(){sign.ForceMeshUpdate();var b=sign.textBounds;var one=camera.WorldToScreenPoint(sign.transform.TransformPoint(b.min));var two=camera.WorldToScreenPoint(sign.transform.TransformPoint(b.max));
                    return Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));}
                Lay(new Vector2(camera.pixelWidth*.5f,camera.pixelHeight*.62f));yield return new WaitForSecondsRealtime(.4f);
                Assert.Greater(sign.alpha,.9f,"fixture: a word on open floor shows");
                // Its first letter six pixels in from the left edge, wholly on the screen.
                var r=Letters();Lay(new Vector2(r.width*.5f+6*px,camera.pixelHeight*.62f));yield return new WaitForSecondsRealtime(.4f);
                r=Letters();Assert.IsTrue(r.xMin>0,"fixture: the word is wholly on the screen");
                got(sign.alpha);
                // Six pixels short of the zone's tag, under it and to its left.
                var tag=Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).Where(t=>t.name=="ZonePlate").Select(PanelFade.ScreenBox).FirstOrDefault(b=>b.width>0);
                if(tag.width>0)
                {
                    Lay(new Vector2(camera.pixelWidth*.5f,camera.pixelHeight*.62f));yield return new WaitForSecondsRealtime(.4f);
                    r=Letters();Lay(new Vector2(tag.xMin-6*px-r.width*.5f,tag.yMin-6*px-r.height*.5f));yield return new WaitForSecondsRealtime(.4f);
                    r=Letters();Assert.IsFalse(r.Overlaps(tag),"fixture: the word is beside the tag, not under it");
                    byTag=sign.alpha;
                }
                Object.Destroy(sign.gameObject);
            }
            yield return Holds(Moment,v=>v>=.05f,"a word six pixels from the screen's edge stays: it reads as cut off by it");
            if(byTag>=0)Assert.Less(byTag,.05f,"a word hard against the zone's tag stays: PIM read as part of DRIFTREED");
        }
        /// <summary>s38/052 and s63/010: the flight camera drawn far back for a wide view, and the Orrery's gate names and the clamps' names went to specks. Under the type floor a word
        /// isn't drawn at all.</summary>
        [UnityTest] public IEnumerator AWordUnderTheTypeFloorIsNotDrawn()
        {
            float back=0;
            IEnumerator Moment(System.Action<float> got)
            {
                // A word lying under the plane in a flight zone, showing with the camera at rest: it grows only so far, and a wide view draws back past it (s63/010, Sorrel's
                // clamps; their own zone frames them drawn back already). The first such word found, a few zones and a few words at the most.
                TextMeshPro sign=null;Camera camera=null;
                foreach(var zone in new[]{"Gullet_Tunnel","WickReach","XylosNearspace"})
                {
                    yield return Zone(zone);yield return new WaitForSecondsRealtime(.8f);camera=Camera.main;
                    foreach(var candidate in Labels().Where(t=>t.transform.position.y<1&&t.TryGetComponent<SignFade>(out _)&&!t.text.StartsWith("Warp")).Take(3))
                    {
                        Put(Lead,new Vector3(candidate.transform.position.x,1,candidate.transform.position.z-6));yield return new WaitForSecondsRealtime(1.5f);
                        if(candidate.alpha>.9f){sign=candidate;break;}
                    }
                    if(sign!=null)break;
                }
                Assert.IsNotNull(sign,"fixture: no word lying under the plane shows with the camera at rest in the Gullet, the Reach or Xylos");
                var brain=camera.GetComponent("CinemachineBrain") as Behaviour;if(brain!=null)brain.enabled=false;
                var rest=camera.transform.position;
                // Where s63/010 stood: some eleven to fourteen times the flight camera's height over the plane.
                camera.transform.position=Lead.transform.position+(rest-Lead.transform.position)*14;
                yield return new WaitForSecondsRealtime(.5f);
                var low=camera.WorldToScreenPoint(sign.transform.position);var high=camera.WorldToScreenPoint(sign.transform.position+camera.transform.up*sign.fontSize*.1f*sign.transform.lossyScale.y);
                float em=Vector2.Distance(low,high)/camera.pixelHeight*1080;
                got(sign.alpha);
                camera.transform.position=rest;if(brain!=null)brain.enabled=true;
                Assert.Less(em,SignStyle.TypeFloorPixels,"fixture: drawn back this far its em is under the type floor");
                yield return new WaitForSecondsRealtime(.8f);
                back=sign.alpha;
            }
            yield return Holds(Moment,v=>v>=.05f,"a word drawn back under the type floor is still drawn: a speck nobody can read");
            Assert.Greater(back,.9f,"the word doesn't come back with the camera");
        }

        // ---------------------------------------------------------------- the review's stills, rendered again
        sealed class Still{public string id,zone,spawn;public string[] flags;public Vector3 hero;public Vector3? partner;}
        static readonly Still[] Stills=
        {
            new(){id="s18-091",zone="Rootmarket",hero=new(33.25f,.08f,41.01f)},
            new(){id="s50-201",zone="Harrowgate",hero=new(-24.98f,2.48f,40.48f)},
            new(){id="s51-220",zone="Harrowgate",hero=new(-25f,2.48f,43.97f)},
            new(){id="s58-125",zone="SeparationWorks",hero=new(12.53f,-1.13f,82.01f)},
            new(){id="s48-008",zone="ScoriaFlows",hero=new(30.55f,.09f,206.03f)},
            new(){id="s33-066",zone="Lastlight",hero=new(7.89f,.09f,58.35f)},
            new(){id="s37-008",zone="Lastlight",hero=new(-24.37f,.08f,42f)},
            new(){id="s37-012",zone="Lastlight",hero=new(5.85f,.09f,53.7f)},
            new(){id="s67-099",zone="StillwaterChamber",hero=new(0,-15.92f,51.69f)},
            new(){id="s11-117",zone="TallowDrift",hero=new(.02f,.08f,1.62f)},
            new(){id="s27-005",zone="DrownedWaystation",hero=new(-2.5f,-.42f,261.5f)},
            new(){id="s42-061",zone="OrielSkyfields",hero=new(-38f,12.08f,122.62f)},
            new(){id="s43-044",zone="OrielSkyfields",hero=new(12.76f,.07f,13.96f),partner=new(14.9f,.07f,13.7f)},
            new(){id="s08-112",zone="Hushwell",hero=new(12.84f,-15.92f,321.44f)},
            new(){id="s69-031",zone="NeighbourhoodAfloat",hero=new(-12.02f,.08f,68.03f)},
            new(){id="decks-market-galley",zone="DecksLeaving",hero=new(0,.08f,-5f)},
            new(){id="decks-orrin",zone="DecksLeaving",hero=new(-31.6f,.08f,4.9f)},
            new(){id="decks-by-ilo",zone="DecksLeaving",hero=new(-25.4f,.08f,-11.7f)},
            new(){id="decks-by-cabins",zone="DecksLeaving",hero=new(-29f,.08f,11.7f)},
            new(){id="hub-decks-arrival",zone="Hub_Decks",hero=new(-28f,.08f,-11f)},
            new(){id="decks-cabins",zone="DecksLeaving",hero=new(-28f,.08f,13f)},
            new(){id="hands-raft-bay",zone="ManyHands",hero=new(-10.4f,4.5f,-30.6f)},
            new(){id="hull-cabins",zone="ManyHandsHull",hero=new(-62f,1f,41f)},
            new(){id="afloat-low-street",zone="NeighbourhoodAfloat",hero=new(-30.4f,.5f,57f)},
            new(){id="s57-055",zone="DecksLeaving",spawn="Meeting",flags=new[]{"home.treadRecovered","home.returned","home.meetingGathered"},hero=new(-33.16f,.08f,6.55f)},
            // The November 22 capture's (D356), the party where frames.csv had it at the end of each step.
            new(){id="n22-s18-091",zone="Rootmarket",hero=new(33.25f,.08f,41.01f),partner=new(28.54f,.08f,37.83f)},
            new(){id="n22-s20-019",zone="BraidedRiver",hero=new(37.52f,.68f,122.5f),partner=new(35.82f,.68f,123.24f)},
            new(){id="n22-s25-090",zone="Driftreed",hero=new(-4.55f,.08f,40.51f),partner=new(-2.73f,.08f,40.47f)},
            new(){id="n22-s30-118",zone="ManyHands",hero=new(-15.21f,.08f,20.34f),partner=new(-8.02f,.08f,21.62f)},
            new(){id="n22-s16-144",zone="TallowDrift",hero=new(0,.08f,.27f),partner=new(1.41f,.08f,-5.66f)},
            new(){id="n22-s22-014",zone="Rootmarket",hero=new(27.61f,.07f,49.52f),partner=new(31.34f,.07f,45.86f)},
            new(){id="n22-s30-277",zone="ManyHands",hero=new(62.02f,.08f,3.13f),partner=new(60.79f,.08f,4.05f)},
            new(){id="n22-s31-125",zone="ManyHands",hero=new(59.54f,.08f,38.97f),partner=new(61.29f,.08f,38.4f)},
            new(){id="n22-s27-013",zone="Driftreed",hero=new(0,.0f,8.5f),partner=new(2,.0f,8.5f)},
            new(){id="n22-s26-512",zone="DrownedWaystation",hero=new(12.08f,-2.32f,206.74f),partner=new(10.71f,-2.32f,213f)},
            new(){id="n22-s12-026",zone="TallowApproach",hero=new(19.73f,1,-17.22f),partner=new(13.57f,1,-15.76f)},
            new(){id="n22-s38-087",zone="Orrery",hero=new(0,1,258.58f),partner=new(1.84f,1,249f)},
        };
        /// <summary>Renders the review stills' places as the game's camera has them, with the party where the capture stood, into Builds/quality/labels/&lt;LABELS_STILLS&gt;/: the
        /// labels engineer's before and after. No HUD (it is a screen overlay); what the words do in the world is all there.</summary>
        [UnityTest,Timeout(1500000)] public IEnumerator RendersTheReviewStillsForTheLabelsEngineer()
        {
            string run=System.Environment.GetEnvironmentVariable("LABELS_STILLS");
            if(string.IsNullOrEmpty(run))Assert.Ignore("renders only when LABELS_STILLS names a run");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/labels",run));Directory.CreateDirectory(folder);
            var done=new List<string>();
            string only=System.Environment.GetEnvironmentVariable("LABELS_STILLS_ONLY");
            foreach(var still in Stills.Where(s=>string.IsNullOrEmpty(only)||only.Split(',').Contains(s.id)))
            {
                yield return Cleanup();yield return Boot();
                if(still.flags!=null)foreach(var f in still.flags)Flag(f);
                yield return Zone(still.zone,still.spawn??"Arrival");
                Put(Lead,still.hero);Put(Partner,still.partner??still.hero+new Vector3(-1.8f,0,-1.2f));
                yield return new WaitForSecondsRealtime(2.5f);
                if(DialogueSystem.Current!=null&&GameInput.Current!=null&&GameInput.Current.Blocked){DialogueSystem.Current.Cancel();yield return new WaitForSecondsRealtime(1f);}
                var camera=Camera.main;var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);var was=camera.targetTexture;
                try
                {
                    camera.targetTexture=rt;camera.Render();var before=RenderTexture.active;RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();RenderTexture.active=before;
                    File.WriteAllBytes(System.IO.Path.Combine(folder,still.id+".png"),image.EncodeToPNG());done.Add(still.id);
                }
                finally{camera.targetTexture=was;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
            }
            TestContext.WriteLine("LABEL_STILLS "+folder+" "+string.Join(",",done));
            Assert.AreEqual(Stills.Count(s=>string.IsNullOrEmpty(only)||only.Split(',').Contains(s.id)),done.Count,"not every still rendered");
        }
    }
}
