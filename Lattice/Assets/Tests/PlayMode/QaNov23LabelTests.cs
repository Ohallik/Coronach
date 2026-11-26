using System.Collections;
using System.Collections.Generic;
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
    /// <summary>D365, from the November 22 night capture: words the checks called read or gone were drawn pale in the player. Every check had measured a word's alpha and counted
    /// anything under a tenth as gone; the player blends in linear light, where .08 of white over a dark floor or open space shows a third of the way to the letters. So these
    /// look at what is drawn, as the game's camera draws it (pipeline, grade and bloom), at the capture's own places.</summary>
    [Timeout(600000)]
    public sealed class QaNov23LabelTests
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
            if(hero.TryGetComponent<FlightMotor>(out var motor))motor.Halt();
        }

        // ---------------------------------------------------------------- what the game's camera draws
        const int W=1920,H=1080;
        /// <summary>The frame as the game's camera draws it, at 1080p. The camera's target is set by the caller, so the screen rects measured are this frame's.</summary>
        static Color32[] Draw(Camera camera)
        {
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var before=RenderTexture.active;
            try{camera.Render();RenderTexture.active=camera.targetTexture;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=before;Object.Destroy(image);}
        }
        /// <summary>The share of a rect's pixels that two frames differ in by more than this many levels of 255, in any channel.</summary>
        static float Changed(Color32[] a,Color32[] b,Rect r,int least=6)
        {
            int x0=Mathf.Clamp(Mathf.CeilToInt(r.xMin),0,W-1),x1=Mathf.Clamp(Mathf.FloorToInt(r.xMax),0,W-1),y0=Mathf.Clamp(Mathf.CeilToInt(r.yMin),0,H-1),y1=Mathf.Clamp(Mathf.FloorToInt(r.yMax),0,H-1);
            int n=0,changed=0;
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                var p=a[y*W+x];var q=b[y*W+x];n++;
                if(Mathf.Max(Mathf.Abs(p.r-q.r),Mathf.Max(Mathf.Abs(p.g-q.g),Mathf.Abs(p.b-q.b)))>least)changed++;
            }
            return n==0?0:changed/(float)n;
        }
        static Rect Letters(Camera camera,TextMeshPro sign)
        {
            var size=sign.GetPreferredValues();var scale=sign.transform.lossyScale;
            Vector3 at=sign.transform.position,across=sign.transform.right*(size.x*.5f*scale.x),up=sign.transform.up*(size.y*.5f*scale.y);
            Vector3 one=camera.WorldToScreenPoint(at-across-up),two=camera.WorldToScreenPoint(at+across+up);
            return Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));
        }
        /// <summary>The block's face toward the camera, on the screen (it is turned to the camera, so this is what it covers, less its sides).</summary>
        static Rect OnScreen(Camera camera,Renderer block)
        {
            float left=float.MaxValue,right=float.MinValue,low=float.MaxValue,high=float.MinValue;
            foreach(var dx in new[]{-1,1})foreach(var dy in new[]{-1,1})
            {
                var p=camera.WorldToScreenPoint(block.transform.TransformPoint(new Vector3(dx*.5f,dy*.5f,-.5f)));
                left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);
            }
            return Rect.MinMaxRect(left,low,right,high);
        }
        /// <summary>A small block as bright as a sunlit crate before the grade, this far behind a word along the camera's line through its middle.</summary>
        static Renderer Bright(Camera camera,Component word,float behind,float edge)
        {
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);Object.Destroy(block.GetComponent<Collider>());block.name="Bright block fixture";block.layer=word.gameObject.layer;
            var eye=camera.transform.position;var along=(word.transform.position-eye).normalized;block.transform.position=word.transform.position+along*behind;block.transform.rotation=camera.transform.rotation;block.transform.localScale=Vector3.one*edge;
            var paint=new Material(Shader.Find("Universal Render Pipeline/Unlit"));paint.SetColor("_BaseColor",new Color(3,3,3,1));
            // Drawn with the transparent things, just before the plates (Transparent-15).
            paint.renderQueue=2980;
            var view=block.GetComponent<Renderer>();view.sharedMaterial=paint;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
            return view;
        }
        static string Words(TextMeshPro t)=>System.Text.RegularExpressions.Regex.Replace(t.text,"<[^>]*>","");
        /// <summary>Its middle in the camera's view (a test run draws no game view, so a renderer's own isVisible stays false).</summary>
        static bool InView(Camera camera,Component thing){var v=camera.WorldToViewportPoint(thing.transform.position);return v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1;}

        // ---------------------------------------------------------------- 1. the capture's ghosts, where the capture stood
        sealed class Still{public string id,zone,spawn="Arrival",word;public Vector3 hero,partner;}
        /// <summary>The party where frames.csv had it at the end of each step (Builds/quality/nov22b-whole). The word named is the ghost the still showed.</summary>
        static readonly Still[] Stills=
        {
            new(){id="s26-513",zone="DrownedWaystation",word="LOWER GALLERY",hero=new(12.074f,-2.32f,206.768f),partner=new(10.5f,-2.32f,211.279f)},
            new(){id="s21-327",zone="WalkingCanopy",word="THE BASIN",hero=new(22.004f,-2.72f,21.441f),partner=new(20.23f,-2.72f,25.316f)},
            new(){id="s42-216",zone="Orrery",word="THE WEIGHER",hero=new(0,1,238.127f),partner=new(-2.708f,1,243.64f)},
            new(){id="s22-125",zone="TallowApproach",word="WARP — XYLOS NEARSPACE",hero=new(6,1,-30),partner=new(9.113f,1,-32.367f)},
            new(){id="s60-189",zone="DecksLeaving",word="ARRIVALS / DOCK OFFICE",hero=new(-29.703f,.08f,-14.768f),partner=new(-28.502f,.08f,-10.886f)},
        };
        /// <summary>Each still's place, the party where it stood: every word on the screen is read or gone in each of 45 frames, never between. Where the still's ghost lies under a
        /// body, it is gone for that body, and the old trace (.08) is drawn as the game draws it, to show the check sees what the capture saw.</summary>
        [UnityTest] public IEnumerator TheNightCapturesGhostsAreReadOrGoneWhereTheyStood()
        {
            var faults=new List<string>();var seen=new List<string>();
            foreach(var still in Stills)
            {
                yield return Cleanup();yield return Boot();
                yield return Zone(still.zone,still.spawn);
                Put(Lead,still.hero);Put(Partner,still.partner);
                yield return new WaitForSecondsRealtime(2f);
                var camera=Camera.main;
                var words=All<SignFade>().Where(f=>f.isActiveAndEnabled).Select(f=>(fade:f,text:f.GetComponent<TextMeshPro>())).Where(w=>w.text!=null&&InView(camera,w.text)).ToList();
                var worst=new Dictionary<TextMeshPro,float>();
                for(int frame=0;frame<45;frame++)
                {
                    yield return null;
                    foreach(var (fade,text) in words)
                    {
                        float a=text.alpha;
                        if(a>.01f&&a<.95f){worst[text]=a;faults.Add($"{still.id}: \"{Words(text)}\" drawn at {a:0.00} ({fade.GoneFor})");}
                    }
                }
                seen.Add($"{still.id}: on the screen "+string.Join(", ",words.Select(w=>$"\"{Words(w.text)}\" {w.text.alpha:0.00}{(w.fade.GoneFor!=""?" ("+w.fade.GoneFor+")":"")}")));
                var ghost=words.FirstOrDefault(w=>string.Equals(Words(w.text).Trim(),still.word,System.StringComparison.OrdinalIgnoreCase));
                if(ghost.text==null){faults.Add($"fixture: {still.id}: \"{still.word}\" isn't on the screen (on it: {string.Join(", ",words.Select(w=>Words(w.text)))})");continue;}
                string state=$"{still.id}: \"{still.word}\" alpha {ghost.text.alpha:0.00} gone for '{ghost.fade.GoneFor}'";seen.Add(state);
                if(ghost.fade.GoneFor!="body")continue;
                // The old trace, as the game draws it: the word at .08 (and its plate) against the word gone, in its letters' rect.
                var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var was=camera.targetTexture;camera.targetTexture=target;
                ghost.fade.enabled=false;var plateAt=ghost.text.transform.Find(SignStyle.PlateName);var plate=plateAt!=null?plateAt.GetComponent<Renderer>():null;
                var block=new MaterialPropertyBlock();
                try
                {
                    var box=Letters(camera,ghost.text);
                    var gone=Draw(camera);
                    ghost.text.alpha=.08f;ghost.text.ForceMeshUpdate();if(plate!=null){plate.GetPropertyBlock(block);block.SetFloat("_Fade",.08f);plate.SetPropertyBlock(block);}
                    var trace=Draw(camera);
                    float share=Changed(gone,trace,box);
                    seen.Add($"{still.id}: the old trace changes {share:P0} of its letters' rect {box} by more than 6 of 255");
                    if(share<.05f)faults.Add($"fixture: {still.id}: the old trace of \"{still.word}\" doesn't show in the drawn frame ({share:P1}): the check can't see the capture's ghost");
                }
                finally
                {
                    ghost.text.alpha=0;ghost.text.ForceMeshUpdate();if(plate!=null){plate.GetPropertyBlock(block);block.SetFloat("_Fade",0);plate.SetPropertyBlock(block);}
                    ghost.fade.enabled=true;camera.targetTexture=was;target.Release();Object.Destroy(target);
                }
            }
            foreach(var line in seen){TestContext.WriteLine("LABEL_GHOST "+line);Debug.Log("LABEL_GHOST "+line);}
            Assert.IsEmpty(faults,"words drawn between read and gone:\n"+string.Join("\n",faults.Distinct().Take(30)));
        }

        // ---------------------------------------------------------------- 2. what is behind a full plate or a name's tag
        /// <summary>A first draw with the block in it, then a second for the editor to finish compiling the block's shader (a variant still compiling isn't drawn).</summary>
        static IEnumerator WarmUp(Camera camera,RenderTexture target)
        {
            var was=camera.targetTexture;camera.targetTexture=target;
            try{Draw(camera);}finally{camera.targetTexture=was;}
            yield return new WaitForSecondsRealtime(1.5f);
        }
        /// <summary>Two frames, the bright block hidden and shown, compared over the block's own rect: the share of it that shows through what is in front.</summary>
        static float Through(Camera camera,Renderer block)
        {
            block.enabled=false;var hidden=Draw(camera);block.enabled=true;var shown=Draw(camera);
            var r=OnScreen(camera,block);r=Rect.MinMaxRect(r.xMin+2,r.yMin+2,r.xMax-2,r.yMax-2);
            return Changed(hidden,shown,r);
        }
        /// <summary>s12/026: a crate drifting behind WARP — THE WICK REACH showed through its full plate above THE. At .95 a twentieth of a sunlit thing comes through, which the
        /// grade shows plainly; the full plate is solid in its middle. Measured on the Decks, a sign of theirs hung beside MIRA's name and given the full plate SignStyle gives a warp's. The block stands
        /// a metre behind the word, as the name test's does: the block drew nothing behind or in front of GATE SIX or the Wick Reach's warp sign (runs d365c to j, unexplained;
        /// a shader variant still compiling is the likeliest).</summary>
        [UnityTest] public IEnumerator NothingShowsThroughAFullPlate()
        {
            yield return Zone("Hub_Decks");
            Put(Lead,new Vector3(-.03f,.08f,1.607f));Put(Partner,new Vector3(1.846f,.08f,-.491f));yield return new WaitForSecondsRealtime(2f);
            var camera=Camera.main;
            // A sign of the zone's with its plate, hung beside MIRA's name where the name test's block is drawn, and given the full plate.
            var sign=All<SignFade>().Where(f=>f.isActiveAndEnabled&&!f.nameTag&&f.transform.Find(SignStyle.PlateName)!=null).Select(f=>f.GetComponent<TextMeshPro>()).FirstOrDefault(t=>t!=null);
            var name=All<TextMeshPro>().FirstOrDefault(t=>t.isActiveAndEnabled&&t.text.Contains("<mark")&&InView(camera,t)&&Words(t).IndexOf("Mira",System.StringComparison.OrdinalIgnoreCase)>=0);
            Assert.IsNotNull(sign,"fixture: a sign with a plate in the Decks");Assert.IsNotNull(name,"fixture: MIRA's name on the screen at s01/031's place");
            sign.GetComponent<SignFade>().enabled=false;sign.alpha=1;
            sign.transform.SetPositionAndRotation(name.transform.position+camera.transform.right*2.5f,camera.transform.rotation);
            var plate=sign.transform.Find(SignStyle.PlateName).GetComponent<Renderer>();var made=plate.sharedMaterial;
            var full=(Material)typeof(SignStyle).GetProperty("Strong",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetValue(null);
            plate.sharedMaterial=full;
            float em=sign.fontSize*.1f*sign.transform.lossyScale.y;var block=Bright(camera,sign,1,em*.45f);
            var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var was=camera.targetTexture;
            yield return WarmUp(camera,target);camera.targetTexture=target;
            var paint=new MaterialPropertyBlock();float old,now,bare;
            try
            {
                var letters=Letters(camera,sign);var r=OnScreen(camera,block);
                Assert.IsTrue(letters.Contains(r.min)&&letters.Contains(r.max),$"fixture: the block {r} lies behind the word's letters {letters}, in the plate's solid middle");
                sign.GetComponent<Renderer>().enabled=plate.enabled=false;bare=Through(camera,block);sign.GetComponent<Renderer>().enabled=plate.enabled=true;
                Assert.Greater(bare,.5f,$"fixture: the block is drawn where it stands ({bare:P0} of it with the sign and plate off; \"{Words(sign)}\", block {r} of {block.transform.lossyScale.x:0.000} m, letters {letters}, {Vector3.Distance(camera.transform.position,block.transform.position):0.0} m off, camera at {camera.transform.position})");
                // The fader is off and the case measures the plate, so the plate is put at full: the sign taken is whichever comes first, and one already faded out (a floor word under a body) left its plate drawing nothing.
                plate.GetPropertyBlock(paint);const float fade=1;paint.SetFloat("_Fade",fade);
                // The plate as it was (.95), then as it is.
                paint.SetColor("_BaseColor",new Color(.03f,.05f,.09f,.95f));plate.SetPropertyBlock(paint);old=Through(camera,block);
                paint.Clear();paint.SetFloat("_Fade",fade);plate.SetPropertyBlock(paint);now=Through(camera,block);
            }
            finally{plate.sharedMaterial=made;camera.targetTexture=was;target.Release();Object.Destroy(target);Object.Destroy(block.gameObject);}
            string line=$"PLATE_THROUGH \"{Words(sign)}\" old(.95)={old:P1} now={now:P1} ({full.GetColor("_BaseColor").a:0.00})";TestContext.WriteLine(line);Debug.Log(line);
            Assert.Greater(old,.2f,line+": the check doesn't see a sunlit block through the old .95 plate, as s12/026 showed one");
            Assert.Less(now,.02f,line+": a sunlit block behind the warp sign shows through its full plate");
        }
        /// <summary>s01/031: MIRA's tag showed the Decks' pale floor and a passer-by's legs through it. At DB (.86) a seventh of what is behind came through.</summary>
        [UnityTest] public IEnumerator NothingShowsThroughANamesTag()
        {
            yield return Zone("Hub_Decks");
            Put(Lead,new Vector3(-.03f,.08f,1.607f));Put(Partner,new Vector3(1.846f,.08f,-.491f));yield return new WaitForSecondsRealtime(2f);
            var camera=Camera.main;
            var name=All<TextMeshPro>().Where(t=>t.isActiveAndEnabled&&t.text.Contains("<mark")&&t.alpha>.95f&&InView(camera,t))
                .OrderByDescending(t=>Words(t).IndexOf("Mira",System.StringComparison.OrdinalIgnoreCase)>=0).FirstOrDefault();
            Assert.IsNotNull(name,"fixture: a person's backed name on the screen at s01/031's place");
            string words=name.text;float old,now;
            Assert.AreNotEqual(words,words.Replace(SignStyle.NameBacking,""),"fixture: the name is backed by the tag SignStyle makes");
            float em=name.fontSize*.1f*name.transform.lossyScale.y;var block=Bright(camera,name,1,em*.7f);
            var target=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var was=camera.targetTexture;float bare;
            yield return WarmUp(camera,target);camera.targetTexture=target;
            try
            {
                // Inside the tag, which stands nine tenths of an em past the name's letters.
                var letters=Letters(camera,name);float pad=.8f*Vector2.Distance(camera.WorldToScreenPoint(name.transform.position),camera.WorldToScreenPoint(name.transform.position+camera.transform.up*em));
                var tag=Rect.MinMaxRect(letters.xMin-pad,letters.yMin-pad,letters.xMax+pad,letters.yMax+pad);var r=OnScreen(camera,block);
                Assert.IsTrue(tag.Contains(r.min)&&tag.Contains(r.max),$"fixture: the block {r} lies behind the name's tag {tag}");
                name.GetComponent<Renderer>().enabled=false;bare=Through(camera,block);name.GetComponent<Renderer>().enabled=true;
                Assert.Greater(bare,.5f,$"fixture: the block is drawn where it stands ({bare:P0} of it with the name off)");
                // The tag as it was (DB), then as it is.
                name.text=words.Replace(SignStyle.NameBacking,"<mark=#05080EDB padding=\"90,90,90,90\">");name.ForceMeshUpdate();old=Through(camera,block);
                name.text=words;name.ForceMeshUpdate();now=Through(camera,block);
            }
            finally{name.text=words;camera.targetTexture=was;target.Release();Object.Destroy(target);Object.Destroy(block.gameObject);}
            string line=$"TAG_THROUGH \"{Words(name)}\" old(DB)={old:P1} now={now:P1}";TestContext.WriteLine(line);Debug.Log(line);
            Assert.Greater(old,.2f,line+": the check doesn't see a sunlit block through the old tag, as s01/031 showed the floor");
            Assert.Less(now,.02f,line+": a sunlit block behind a person's name shows through its tag");
        }
    }
}
