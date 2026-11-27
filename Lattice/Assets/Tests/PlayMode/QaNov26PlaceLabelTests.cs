using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Round seven (November 26), from the November 25 whole-game capture: two places' names cut by what stood in front of them where the party first sees them. CASTING
    /// GALLERY lay at (-7.4, 137.6), behind the masonry east of the Bridle Foundry's stair, and read 'CASTING GALL' from the stair's head (s48 155, s49 152); UNSWORN CAMP lay a metre
    /// behind the west lamp of the Scoria camp's cradle, which stood in its R and N (s46 164, s47 159, 178). Each word is looked at from the capture's own cameras with the party where
    /// frames.csv had it: read by SignFade with the game's camera following the party, and its letters reaching the screen as the game's camera draws them from the still's place
    /// (WorldLabelTests' three looks: alone, in magenta, as drawn). The word is put back at its old place first, and there it has to be cut where the capture showed it cut.</summary>
    [Timeout(600000)]
    public sealed class QaNov26PlaceLabelTests
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
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members[1-PartyController.Current.index];
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
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
        /// <summary>The floor under a point, looked for from a little over the given height (the foundry's floors lie under its walls' tops).</summary>
        static float Floor(Vector2 at,float near)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(at.x,near+3,at.y),Vector3.down,14,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(!(hit.collider is CharacterController)&&hit.normal.y>.6f)return hit.point.y;
            return near;
        }

        // ---------------------------------------------------------------- what the game's camera draws
        const int W=1920,H=1080;
        /// <summary>A word is cut when more than this share of its letters' insides doesn't reach the screen. Stricter than WorldLabelTests' .04: the lamp the capture showed in the
        /// lower halves of N and C at s47 159 hides .03 of UNSWORN CAMP drawn where it lies, and a word placed clear of everything hides none.</summary>
        const float MostHidden=.01f;
        const string MarksPass="SRPDefaultUnlit";
        static int Probe(Camera camera)
        {
            for(int l=31;l>=8;l--)if(string.IsNullOrEmpty(LayerMask.LayerToName(l))&&(camera.cullingMask&(1<<l))==0)return l;
            return 31;
        }
        static Color32[] Shoot(Camera camera,RenderTexture target,Texture2D into)
        {
            var before=RenderTexture.active;camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;into.ReadPixels(new Rect(0,0,W,H),0,0,false);into.Apply(false);RenderTexture.active=before;
            return into.GetPixels32();
        }
        /// <summary>What a measure found: the share of the letters hidden (by the render, or by a solid thing standing nearer the camera than the word lies), each part, and the
        /// letters' box on the 1080p screen (bottom-left origin).</summary>
        struct Found{public float hidden,drawnOver,inFront;public Rect box;}
        /// <summary>The word's letters as the game's camera draws them from where it stands (NaN when none of it is on the screen), and the frame as a player sees it with the word read.
        /// A letter is hidden where the drawn frame doesn't show it, or where it doesn't show with the word drawn where it lies: SignText draws a word 6 m nearer the camera than it
        /// lies (SignStyle.Reach), and at s46 164 the editor drew UNSWORN CAMP over the cradle's west lamp, a metre in front of it, that the player drew over its R and N. The party's
        /// bodies are left out: a body over a word is SignFade's business, and it is asked separately.</summary>
        static Found Measure(Camera camera,TextMeshPro label,RenderTexture target,Texture2D image,out Color32[] seen)
        {
            float drawnOver=Hidden(camera,label,target,image,out seen,out var cores,out var box,float.NaN);
            if(cores.Count==0)return new Found{hidden=float.NaN,drawnOver=float.NaN,inFront=float.NaN,box=box};
            // Again with the word drawn where it lies (SignText's reach at 0): what stands in front of it within the reach, as the player drew the lamp over R and N.
            Hidden(camera,label,target,image,out _,out var bare,out _,0);
            var shownBare=new HashSet<(int,int)>(bare.Where(c=>c.shown).Select(c=>(c.x,c.y)));
            int front=0,either=0;
            foreach(var (x,y,shown) in cores){bool before=!shownBare.Contains((x,y));if(before)front++;if(before||!shown)either++;}
            return new Found{hidden=either/(float)cores.Count,drawnOver=drawnOver,inFront=front/(float)cores.Count,box=box};
        }
        /// <summary>Whether a word's box is cut at 1920 by 1080 by the screen's edge or a HUD plate (SignFade's rule, with its 16 pixels of room). A test's screen is 640 by 480,
        /// where the narrower view put CASTING GALLERY under the status panel that the player's doesn't reach: the plates SignFade holds (PanelFade's, in this screen's pixels) are
        /// laid out again at 1080p as the HUD's canvas lays them (1920 by 1080 expanded to the screen; a plate in the upper half keeps its distance from the top, one in the lower from the bottom).</summary>
        static string Cut1080(Rect box)
        {
            const float Room=16;var r=Rect.MinMaxRect(box.xMin-Room,box.yMin-Room,box.xMax+Room,box.yMax+Room);
            if(r.xMin<0||r.yMin<0||r.xMax>W||r.yMax>H)return "the screen's edge";
            var field=typeof(PanelFade).GetField("plates",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            var count=typeof(PanelFade).GetField("plateCount",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            if(field==null||count==null)return "fixture: PanelFade's plates can't be read";
            var plates=(Rect[])field.GetValue(null);int n=(int)count.GetValue(null);float sw=Screen.width,sh=Screen.height,s=Mathf.Min(sw/1920f,sh/1080f);
            for(int i=0;i<n;i++)
            {
                var p=plates[i];float x0=p.xMin/s,x1=p.xMax/s,tall=p.height/s;
                float y0=p.center.y>sh*.5f?H-(sh-p.yMax)/s-tall:p.yMin/s;
                var at=new Rect(x0,y0,x1-x0,tall);if(at.Overlaps(r))return $"a plate at ({at.xMin:0},{at.yMin:0})-({at.xMax:0},{at.yMax:0}) of 1920x1080";
            }
            return null;
        }
        static float Hidden(Camera camera,TextMeshPro label,RenderTexture target,Texture2D image,out Color32[] seen,out List<(int x,int y,bool shown)> cores,out Rect box,float reach)
        {
            cores=new List<(int,int,bool)>();box=default;
            var own=label.fontSharedMaterial;Material lying=null;
            if(!float.IsNaN(reach)&&own!=null&&own.HasProperty("_Reach")){lying=new Material(own){name=own.name+" (where it lies)"};lying.SetFloat("_Reach",reach);label.fontSharedMaterial=lying;}
            try{return Drawn(camera,label,target,image,out seen,cores,out box);}
            finally{if(lying!=null){label.fontSharedMaterial=own;label.ForceMeshUpdate();Object.Destroy(lying);}}
        }
        static float Drawn(Camera camera,TextMeshPro label,RenderTexture target,Texture2D image,out Color32[] seen,List<(int x,int y,bool shown)> cores,out Rect box)
        {
            box=default;
            var fade=label.GetComponent<SignFade>();bool on=fade!=null&&fade.enabled;float alpha=label.alpha;var color=label.color;int layer=label.gameObject.layer;
            var plateAt=label.transform.Find(SignStyle.PlateName);var plate=plateAt!=null?plateAt.GetComponent<Renderer>():null;MaterialPropertyBlock block=null;
            var bodies=PartyController.Current.members.SelectMany(m=>m.GetComponentsInChildren<Renderer>()).Where(r=>!r.forceRenderingOff).ToArray();
            int probe=Probe(camera);var mask=camera.cullingMask;var clear=camera.clearFlags;var back=camera.backgroundColor;
            var face=label.fontSharedMaterial;bool marks=face!=null&&face.shader!=null&&face.shader.name=="Lattice/SignText"&&face.GetShaderPassEnabled(MarksPass);
            try
            {
                if(fade!=null)fade.enabled=false;label.alpha=1;
                if(plate!=null){block=new MaterialPropertyBlock();plate.GetPropertyBlock(block);plate.SetPropertyBlock(null);}
                foreach(var r in bodies)r.forceRenderingOff=true;
                // Alone, on black, in white: where its letters are.
                label.gameObject.layer=probe;label.color=new Color(1,1,1,1);label.ForceMeshUpdate();
                camera.cullingMask=1<<probe;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                if(marks)face.SetShaderPassEnabled(MarksPass,false);
                Color32[] alone;try{alone=Shoot(camera,target,image);}finally{if(marks)face.SetShaderPassEnabled(MarksPass,true);}
                label.gameObject.layer=layer;camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;
                // Its faces in magenta, then as drawn: where something stands in front of a letter the two are the same.
                label.color=new Color(1,0,1,1);label.ForceMeshUpdate();var marked=Shoot(camera,target,image);
                label.color=color;label.alpha=1;label.ForceMeshUpdate();var drawn=Shoot(camera,target,image);
                foreach(var r in bodies)r.forceRenderingOff=false;
                seen=Shoot(camera,target,image);
                bool Letter(int x,int y)=>x>=0&&y>=0&&x<W&&y<H&&Mathf.Max(alone[y*W+x].r,Mathf.Max(alone[y*W+x].g,alone[y*W+x].b))>115;
                int core=0,reach=0;float x0=W,x1=0,y0=H,y1=0;
                for(int y=1;y<H-1;y++)for(int x=1;x<W-1;x++)
                {
                    if(Letter(x,y)){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x+1);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y+1);}
                    // A letter's inside, a pixel in from its edge: the edge is blended with whatever is behind it either way.
                    if(!Letter(x,y)||!Letter(x-1,y)||!Letter(x+1,y)||!Letter(x,y-1)||!Letter(x,y+1))continue;
                    core++;var d=drawn[y*W+x];var m=marked[y*W+x];
                    bool shown=Mathf.Max(Mathf.Abs(d.r-m.r),Mathf.Max(Mathf.Abs(d.g-m.g),Mathf.Abs(d.b-m.b)))>64;if(shown)reach++;
                    cores.Add((x,y,shown));
                }
                if(core<10){cores.Clear();return float.NaN;}
                box=Rect.MinMaxRect(x0,y0,x1,y1);
                return 1-reach/(float)core;
            }
            finally
            {
                foreach(var r in bodies)if(r!=null)r.forceRenderingOff=false;
                label.gameObject.layer=layer;label.color=color;label.alpha=alpha;label.ForceMeshUpdate();if(fade!=null)fade.enabled=on;if(plate!=null)plate.SetPropertyBlock(block);
                camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;
            }
        }
        /// <summary>A frame kept for the eye, at play size, under Builds/quality/labels/r7c.</summary>
        static void Keep(string file,Color32[] pixels)
        {
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/labels/r7c"));Directory.CreateDirectory(folder);
            var shot=new Texture2D(W,H,TextureFormat.RGB24,false);shot.SetPixels32(pixels);shot.Apply();File.WriteAllBytes(Path.Combine(folder,file),shot.EncodeToJPG(92));Object.Destroy(shot);
        }

        // ---------------------------------------------------------------- the capture's places
        /// <summary>Where the capture stood (frames.csv of nov25-whole, the last frame of the step): the game camera, the lead and the partner. Cut: the still showed the word cut at
        /// its old place, so the check has to find it cut there.</summary>
        sealed class Still{public string id;public Vector3 eye,hero;public Vector2 partner;public bool cut;}
        static IEnumerator Look(string zone,string word,Vector2 old,Still[] stills,List<string> faults,List<string> notes)
        {
            yield return Zone(zone);
            var label=All<TextMeshPro>().FirstOrDefault(t=>t.name=="Sign_"+word&&t.gameObject.activeInHierarchy);
            if(label==null){faults.Add($"fixture: {zone} has no \"{word}\"");yield break;}
            var built=label.transform.position;var builtAt=new Vector2(built.x,built.z);
            var was=new Vector3(old.x,built.y+Floor(old,built.y)-Floor(builtAt,built.y),old.y);
            notes.Add($"{zone} \"{word}\" built at {built}, old place {was}");
            var camera=Camera.main;Assert.IsNotNull(camera,zone+": no game camera");
            var view=ZoneController.Current.definition.cameraProfile;
            var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(W,H,TextureFormat.RGB24,false);
            try
            {
                foreach(var still in stills)
                {
                    var p=new Vector2(still.partner.x,still.partner.y);
                    Put(Lead,new Vector3(still.hero.x,Floor(new Vector2(still.hero.x,still.hero.z),still.hero.y),still.hero.z));Put(Partner,new Vector3(p.x,Floor(p,still.hero.y),p.y));
                    yield return new WaitForSecondsRealtime(2f);
                    // Read where it is built, as SignFade has it with the party here and the game's camera following them.
                    // A word SignFade calls cut here is cut on the test's 640 by 480 screen; whether it is cut on the player's 1080p one is asked below, from the capture's camera.
                    var fade=label.GetComponent<SignFade>();int gone=0;string why="";
                    for(int f=0;f<30;f++){yield return null;if(label.alpha<.95f||fade!=null&&fade.GoneFor!=""){gone++;why=fade!=null?fade.GoneFor:"faded";}}
                    if(gone>0&&why!="cut")faults.Add($"{still.id}: \"{word}\" is gone in {gone} of 30 frames ({why}) with the party where the still had them");
                    // From the capture's own camera: at the old place first, then where it is built.
                    var eyeWas=camera.transform.position;var turnWas=camera.transform.rotation;float fovWas=camera.fieldOfView;var targetWas=camera.targetTexture;
                    try
                    {
                        camera.transform.SetPositionAndRotation(still.eye,Quaternion.Euler(view.pitch,view.yaw,0));camera.fieldOfView=view.fov;
                        string stem=System.Text.RegularExpressions.Regex.Replace(word,"[^A-Za-z]","")+"-"+still.id;
                        label.transform.position=was;
                        var before=Measure(camera,label,target,image,out var drawnBefore);Keep(stem+"-old.jpg",drawnBefore);
                        label.transform.position=built;
                        var after=Measure(camera,label,target,image,out var drawnAfter);Keep(stem+"-new.jpg",drawnAfter);
                        string plate=float.IsNaN(after.hidden)?null:Cut1080(after.box);
                        notes.Add($"{still.id}: \"{word}\" hidden {before.hidden:P0} at its old place ({before.drawnOver:P0} as drawn, {before.inFront:P0} drawn where it lies), {after.hidden:P0} where it is built "
                            +$"({after.drawnOver:P0}, {after.inFront:P0}), its letters ({after.box.xMin:0},{1080-after.box.yMax:0})-({after.box.xMax:0},{1080-after.box.yMin:0}) from the top left, "
                            +$"{(plate==null?"clear of the edge and the plates at 1080p":"cut at 1080p by "+plate)}; SignFade on a {Screen.width}x{Screen.height} screen: read in {30-gone} of 30 frames{(gone>0?" ("+why+")":"")}");
                        if(still.cut&&!(before.hidden>MostHidden))faults.Add($"fixture: {still.id}: at its old place \"{word}\" is only {before.hidden:P0} hidden: the check doesn't see the capture's cut");
                        if(float.IsNaN(after.hidden))faults.Add($"{still.id}: \"{word}\" isn't on the screen from the capture's camera");
                        else if(after.hidden>MostHidden)faults.Add($"{still.id}: \"{word}\" is cut: {after.hidden:P0} of its letters don't reach the screen from the capture's camera");
                        if(plate!=null)faults.Add($"{still.id}: \"{word}\" is cut at 1080p by {plate}");
                    }
                    finally{label.transform.position=built;camera.transform.SetPositionAndRotation(eyeWas,turnWas);camera.fieldOfView=fovWas;camera.targetTexture=targetWas;}
                }
            }
            finally{target.Release();Object.Destroy(target);Object.Destroy(image);}
        }
        static void Report(List<string> faults,List<string> notes)
        {
            foreach(var line in notes){TestContext.WriteLine("R7C_LABEL "+line);Debug.Log("R7C_LABEL "+line);}
            Assert.IsEmpty(faults,"place names cut from the capture's places:\n"+string.Join("\n",faults));
        }

        /// <summary>CASTING GALLERY from the stair's head on the way down (s48 155) and back (s49 152), and from the stair's foot coming back (s49 151).</summary>
        [UnityTest] public IEnumerator TheCastingGalleryReadsWholeFromTheStair()
        {
            var faults=new List<string>();var notes=new List<string>();
            yield return Look("BridleFoundry","CASTING GALLERY",new Vector2(-7.4f,137.6f),new[]
            {
                new Still{id="s48-155",eye=new(-18.089f,12.584f,105.263f),hero=new(-12.994f,.082f,120.473f),partner=new(-10.954f,116.061f),cut=true},
                new Still{id="s49-152",eye=new(-18.09f,12.04f,108.62f),hero=new(-12.99f,-.08f,121.51f),partner=new(-14.377f,125.918f),cut=true},
                new Still{id="s49-151",eye=new(-17.01f,7.7f,121.22f),hero=new(-12.68f,-4.67f,134.37f),partner=new(-11.616f,138.641f)},
            },faults,notes);
            Report(faults,notes);
        }
        /// <summary>UNSWORN CAMP from the way in (s46 164, s47 159) and from the cradle (s47 178).</summary>
        [UnityTest] public IEnumerator TheUnswornCampReadsWholeFromTheWayInAndTheCradle()
        {
            var faults=new List<string>();var notes=new List<string>();
            yield return Look("ScoriaFlows","UNSWORN CAMP",new Vector2(33.5f,203),new[]
            {
                new Still{id="s46-164",eye=new(28.64f,12.62f,191.01f),hero=new(34.45f,.09f,204.79f),partner=new(30.24f,204.226f),cut=true},
                new Still{id="s47-159",eye=new(23.99f,12.59f,192.87f),hero=new(29.54f,.07f,206.56f),partner=new(25.297f,206.571f),cut=true},
                new Still{id="s47-178",eye=new(32.97f,12.63f,186.83f),hero=new(37.73f,.1f,200.67f),partner=new(39.702f,203.987f)},
            },faults,notes);
            Report(faults,notes);
        }
    }
}
