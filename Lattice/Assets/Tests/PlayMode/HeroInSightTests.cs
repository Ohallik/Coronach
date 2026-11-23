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
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>
    /// The fixed camera's rules after the first whole-game capture (nov20), measured where the capture broke them:
    /// (1) both heroes are always findable: nothing, a boss included, hides the lead without the game drawing him through it (HeroThrough);
    /// (2) the frame keeps a locked target and the lead on screen and clear of the HUD's panels where the room allows;
    /// (3) a boss bigger than the usual view draws the camera back for its fight, never so far that the lead is under <see cref="LeastHeroPixels"/> pixels in 1080;
    /// and the ground camera never comes nearer the party than its play distance (it leans instead).
    /// Each boss case saves a still of its frame (Builds/quality/camera-frames) for a person to look at.
    /// </summary>
    public sealed class HeroInSightTests
    {
        /// <summary>The least the lead stands on the screen in a fight, feet to head, in pixels of 1080.</summary>
        const float LeastHeroPixels=80;
        /// <summary>The least a ship spans on the screen in a fight, in pixels of 1080.</summary>
        const float LeastShipPixels=60;
        /// <summary>Of the pixels the lead's body covers, the least share the screen must show of him (<see cref="Seen"/>).</summary>
        const float LeastSeen=.5f;
        const int ShotWidth=960,ShotHeight=540;

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static T[] All<T>() where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.FirstOrDefault(m=>m!=Lead&&m.gameObject.activeInHierarchy);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(string zone,bool clearEnemies)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+40;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)){if(clearEnemies)Object.Destroy(enemy.gameObject);else enemy.Passive=true;}
            Quiet();yield return new WaitForSecondsRealtime(.8f);
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Until the view hasn't moved for half a second (at most eight).</summary>
        static IEnumerator Still()
        {
            var last=Camera.main.transform.position;float since=Time.unscaledTime,deadline=Time.unscaledTime+8;
            while(Time.unscaledTime-since<.5f&&Time.unscaledTime<deadline)
            {yield return null;var now=Camera.main.transform.position;if(Vector3.Distance(last,now)>.01f){last=now;since=Time.unscaledTime;}}
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();if(hero.TryGetComponent<GroundMotor>(out var feet))feet.Halt();
            hero.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static void NoSpecials(EnemyBrain enemy)
        {if(enemy.TryGetComponent<BossController>(out var boss))typeof(BossController).GetField("nextSpecial",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(boss,Time.time+900);}
        static bool Graphics=>SystemInfo.graphicsDeviceType!=GraphicsDeviceType.Null;

        /// <summary>The lead's box on the screen, in viewport units: a standing body 1.9 m tall, or a ship's hull.</summary>
        static Rect ScreenBox(Camera camera,CombatActor hero)
        {
            float half=hero.flight?HeroCollision.HullRadius(hero.character):.4f,height=hero.flight?1.6f:1.9f;var foot=hero.transform.position+(hero.flight?Vector3.down*.0f:Vector3.zero);
            float x0=float.MaxValue,y0=float.MaxValue,x1=float.MinValue,y1=float.MinValue;
            for(int i=0;i<8;i++)
            {
                var corner=foot+new Vector3((i&1)==0?-half:half,(i&2)==0?0:height,(i&4)==0?-half:half);
                var view=camera.WorldToViewportPoint(corner);if(view.z<=0)return Rect.zero;
                x0=Mathf.Min(x0,view.x);y0=Mathf.Min(y0,view.y);x1=Mathf.Max(x1,view.x);y1=Mathf.Max(y1,view.y);
            }
            return Rect.MinMaxRect(x0,y0,x1,y1);
        }
        static bool Within(Rect box,Rect area)=>box.width>0&&box.xMin>=area.xMin&&box.xMax<=area.xMax&&box.yMin>=area.yMin&&box.yMax<=area.yMax;
        /// <summary>The HUD's plates as they stand at 1920 by 1080 (nov20 stills): the party panel, the objective card with two lines, the lock bar, the skill row, the hint bar, and in a boss
        /// fight the bar's name and words.</summary>
        static readonly Rect[] Plates={Rect.MinMaxRect(0,.715f,.285f,1),Rect.MinMaxRect(.695f,.785f,1,1),Rect.MinMaxRect(.44f,.915f,.8f,1),Rect.MinMaxRect(.655f,0,1,.145f),Rect.MinMaxRect(.06f,0,.59f,.07f)};
        static readonly Rect BossBar=Rect.MinMaxRect(.26f,.08f,.74f,.2f);
        /// <summary>On the screen and under no plate.</summary>
        static bool Clear(Rect box,bool bossBar)=>Within(box,Rect.MinMaxRect(.02f,.02f,.98f,.98f))&&!Plates.Any(p=>p.Overlaps(box))&&!(bossBar&&BossBar.Overlaps(box));
        static Color32[] Shot(Camera camera,int only=-1)
        {
            var image=new Texture2D(ShotWidth,ShotHeight,TextureFormat.RGB24,false);var rt=new RenderTexture(ShotWidth,ShotHeight,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;int mask=camera.cullingMask;var clear=camera.clearFlags;var back=camera.backgroundColor;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                if(only>=0){camera.cullingMask=only;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;}
                camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,ShotWidth,ShotHeight),0,0);image.Apply();return image.GetPixels32();
            }
            finally{camera.cullingMask=mask;camera.clearFlags=clear;camera.backgroundColor=back;camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        static float Change(Color32 a,Color32 b)=>(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(3*255f);
        /// <summary>A layer nothing in the game draws on, for a render of one body alone.</summary>
        static int ProbeLayer{get{for(int l=31;l>=8;l--)if(string.IsNullOrEmpty(LayerMask.LayerToName(l)))return l;return 31;}}
        /// <summary>The hero's body and his drawing-through, out of every render or back.</summary>
        static void Hide(CombatActor hero,bool hidden)
        {
            foreach(var body in hero.GetComponentsInChildren<Renderer>())if(body is MeshRenderer||body is SkinnedMeshRenderer)body.forceRenderingOff=hidden;
            if(hero.TryGetComponent<HeroThrough>(out var through))foreach(var copy in through.Copies)copy.forceRenderingOff=hidden;
        }
        /// <summary>How much of the lead the screen shows: of the pixels his body covers (a render of his body alone on its own layer, against one without it), the share that change
        /// when he is taken out of the real render. In plain view most of them; behind a wall nothing; drawn through it, the drawing. His shadow is kept out of all of them.</summary>
        static float Seen(Camera camera,CombatActor hero)
        {
            var through=hero.GetComponent<HeroThrough>();var copies=through!=null?through.Holder:null;
            var bodies=hero.GetComponentsInChildren<Renderer>().Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff&&(copies==null||!r.transform.IsChildOf(copies))).ToArray();
            if(bodies.Length==0)return 0;
            var casting=bodies.Select(r=>r.shadowCastingMode).ToArray();var layers=bodies.Select(r=>r.gameObject.layer).ToArray();int probe=ProbeLayer;
            foreach(var r in bodies)r.shadowCastingMode=ShadowCastingMode.Off;
            try
            {
                var shown=Shot(camera);Hide(hero,true);var gone=Shot(camera);var empty=Shot(camera,1<<probe);Hide(hero,false);
                foreach(var r in bodies)r.gameObject.layer=probe;
                var alone=Shot(camera,1<<probe);
                for(int i=0;i<bodies.Length;i++)bodies[i].gameObject.layer=layers[i];
                int covered=0,changed=0;
                for(int i=0;i<alone.Length;i++)
                {
                    if(Change(alone[i],empty[i])<.03f)continue;
                    covered++;if(Change(shown[i],gone[i])>.05f)changed++;
                }
                return covered>20?changed/(float)covered:0;
            }
            finally{for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){bodies[i].shadowCastingMode=casting[i];bodies[i].gameObject.layer=layers[i];}}
        }
        /// <summary>A ship's span on the screen in pixels of 1080: what reads of a hero in flight, where a height means nothing.</summary>
        static float ShipPixels(Camera camera,CombatActor hero)
        {
            var hull=hero.GetComponent<FormController>().flight;bool any=false;var shape=new Bounds(hero.transform.position,Vector3.zero);
            foreach(var r in hull.GetComponentsInChildren<Renderer>())if((r is MeshRenderer||r is SkinnedMeshRenderer)&&r.enabled){if(any)shape.Encapsulate(r.bounds);else{shape=r.bounds;any=true;}}
            float x0=1,y0=1,x1=0,y1=0;
            for(int i=0;i<8;i++){var v=camera.WorldToViewportPoint(new Vector3((i&1)==0?shape.min.x:shape.max.x,(i&2)==0?shape.min.y:shape.max.y,(i&4)==0?shape.min.z:shape.max.z));x0=Mathf.Min(x0,v.x);x1=Mathf.Max(x1,v.x);y0=Mathf.Min(y0,v.y);y1=Mathf.Max(y1,v.y);}
            return Mathf.Max((x1-x0)*1920,(y1-y0)*1080);
        }
        static void SetThrough(bool on){foreach(var hero in PartyController.Current.members){var through=hero.GetComponent<HeroThrough>();Assert.IsNotNull(through,"every hero out is drawn through what hides them");through.enabled=on;}}
        /// <summary>A still of the frame as it stands, for a person to look at: the mirror's Builds/quality/camera-frames.</summary>
        static void Keep(string name)
        {
            if(!Graphics)return;
            var camera=Camera.main;var rt=new RenderTexture(1280,720,24,RenderTextureFormat.ARGB32);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var target=camera.targetTexture;var active=RenderTexture.active;
            try
            {
                rt.Create();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
                var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/camera-frames"));Directory.CreateDirectory(folder);
                File.WriteAllBytes(Path.Combine(folder,name+".jpg"),image.EncodeToJPG(82));
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        static Bounds BodyOf(Component who)
        {
            bool any=false;var bounds=new Bounds(who.transform.position,Vector3.zero);
            foreach(var body in who.GetComponentsInChildren<Renderer>())
            {
                if(body is not MeshRenderer&&body is not SkinnedMeshRenderer||!body.enabled||!body.gameObject.activeInHierarchy||body.GetComponent<TMPro.TMP_Text>()!=null)continue;
                if(any)bounds.Encapsulate(body.bounds);else{bounds=body.bounds;any=true;}
            }
            return bounds;
        }
        static bool EdgeMarkShowing()
        {
            var mark=All<UnityEngine.UI.Image>().FirstOrDefault(i=>i.name=="BossEdgeMark");
            return mark!=null&&mark.enabled&&mark.gameObject.activeInHierarchy;
        }

        // ------------------------------------------------------------------ (1) and (3): every boss's fight
        /// <summary>Each boss at its own door, the party standing in front of it, behind it (the far side from the lens, where a tall body hides the floor) and on both flanks. Wherever the
        /// lead stands he is on screen clear of the panels, at least <see cref="LeastHeroPixels"/> tall, and his body shows (in plain view, or drawn through what is in the way); the boss
        /// stands on the screen or the HUD's edge mark says where. The Foundry Saint's lying arms hid both heroes (nov20 s59 022, 024, 041), and its body has no door for anyone behind it.</summary>
        [UnityTest] public IEnumerator EveryBossFightKeepsTheLeadSeenOnScreenAndAtHeight([ValueSource(typeof(ContentRegistry),nameof(ContentRegistry.Bosses))] string id)
        {
            var sheet=ContentRegistry.Boss(id);
            foreach(string waited in sheet.doorFlags)Flag(waited);
            yield return Zone(sheet.zone,false);
            var door=All<EncounterVolume>().Where(e=>e.encounterId==sheet.door).ToArray();
            Assert.AreEqual(1,door.Length,$"fixture: {id}'s door {sheet.door} in {sheet.zone}");
            door[0].Begin();yield return Frames(2);
            foreach(var brain in All<EnemyBrain>())brain.Passive=true;
            var boss=All<EnemyBrain>().FirstOrDefault(e=>e.definition!=null&&e.definition.id==id&&e.gameObject.activeInHierarchy);
            Assert.IsNotNull(boss,$"fixture: {id} came out at its door");
            yield return Frames(3);Quiet();
            // A fight in a zone of the other form (the Continuance's berth over its hull, chapter 10) is met from that berth, as a player docks onto it.
            if(sheet.flight!=Lead.flight)yield return Dock(Berth.All.FirstOrDefault(b=>b!=null&&b.Flight==sheet.flight));
            var faults=new List<string>();int stood=0;
            var cc=boss.GetComponent<CharacterController>();
            if(!Lead.flight){boss.Passive=false;yield return Frames(2);NoSpecials(boss);if(cc!=null)cc.enabled=false;}
            yield return Around(id,boss,faults,()=>stood++);
            // The on-foot halves of chapter 10's two berth fights (the Tollkeeper's arm, Mother of Doors' shelves) dock only as their fights stage them: a body too big for any view, which
            // is what they meet there, is ABodyTooBigToFrameDoesNotHoldTheViewAtItsLimit's.
            Assert.Greater(stood,0,$"fixture: nowhere round {id} to stand");
            Assert.IsEmpty(faults,$"{id}'s fight in {sheet.zone}:\n"+string.Join("\n",faults));
        }
        static IEnumerator Dock(Berth berth)
        {
            if(berth==null||Berth.Current==berth)yield break;
            Put(Lead,berth.pad.transform.position+(Lead.flight?Vector3.zero:Vector3.up*.1f));yield return Frames(3);
            berth.pad.Interact();
            for(float end=Time.unscaledTime+4;(Berth.Changing||Berth.Current!=berth)&&Time.unscaledTime<end;)yield return null;
            yield return Frames(3);Quiet();
        }
        /// <summary>The lead in front of the boss, behind it (the far side from the lens, where a tall body hides the floor) and on both flanks.</summary>
        IEnumerator Around(string id,EnemyBrain boss,List<string> faults,System.Action stood)
        {
            var camera=Camera.main;var body=BodyOf(boss);var cc=boss.GetComponent<CharacterController>();
            float reach=Mathf.Max(cc!=null?cc.radius:1,Mathf.Max(body.extents.x,body.extents.z)*.55f);
            var away=Flat(camera.transform.forward).normalized;var right=Vector3.Cross(Vector3.up,away);var centre=boss.transform.position;
            foreach(var (where,wanted) in new[]{("behind",centre+away*(reach+2.5f)),("front",centre-away*(reach+3)),("left",centre-right*(reach+3)),("right",centre+right*(reach+3))})
            {
                Vector3 at;
                if(Lead.flight)at=new Vector3(wanted.x,ZoneController.Current.FlightPlane,wanted.z)+Flat(wanted-centre).normalized*9;
                else if(NavMesh.SamplePosition(wanted,out var hit,3,NavMesh.AllAreas))at=hit.position;
                else{TestContext.WriteLine($"BOSS_FRAME {id} {where}: no floor within 3 m of {wanted}, not stood");continue;}
                yield return Stand(id,where,boss,at,faults,stood);
                if(!boss.gameObject.activeInHierarchy||!boss.Health.Alive)yield break;
            }
        }
        IEnumerator Stand(string id,string where,EnemyBrain boss,Vector3 at,List<string> faults,System.Action stood)
        {
            var camera=Camera.main;var rig=Object.FindFirstObjectByType<CameraRig>();bool flight=Lead.flight;
            var right=Vector3.Cross(Vector3.up,Flat(camera.transform.forward).normalized);
            Put(Lead,at);var partner=Partner;if(partner!=null)Put(partner,at+right*(flight?HeroCollision.Apart(Lead.character,partner.character):1.6f));
            Lead.target=boss.Health;Lead.TargetLocked=true;
            yield return new WaitForSecondsRealtime(flight?3.5f:3.2f);
            // A ship set down somewhere else is a jump no ship makes: the flight view holds back a while after it and comes in at its own pace. Judged once it has come to rest.
            if(flight)yield return Still();
            if(!boss.gameObject.activeInHierarchy||!boss.Health.Alive){TestContext.WriteLine($"BOSS_FRAME {id} {where}: the boss left the field");yield break;}
            stood();
            var body=BodyOf(boss);var box=ScreenBox(camera,Lead);float size=flight?ShipPixels(camera,Lead):box.height*1080;
            float seen=Graphics?Seen(camera,Lead):1;
            // Where the body is nearest him: all of a carrier can't be on the screen, its near side can.
            var near=body.ClosestPoint(Lead.transform.position+Vector3.up);var view=camera.WorldToViewportPoint(near);
            bool on=view.z>0&&view.x>.01f&&view.x<.99f&&view.y>.01f&&view.y<.99f;bool marked=!flight&&EdgeMarkShowing();
            float apart=Vector3.Distance(camera.transform.position,Lead.transform.position);
            TestContext.WriteLine($"BOSS_FRAME {id} {where} form={(flight?"flight":"ground")} lens={apart:0.0}m lean={rig.Lean:0} lead=({box.xMin:0.00},{box.yMin:0.00})-({box.xMax:0.00},{box.yMax:0.00}) " +
                $"{(flight?"span":"height")}={size:0}px seen={seen:0.00} boss=({view.x:0.00},{view.y:0.00}) onScreen={on} edgeMark={marked} body={body.size.x:0}x{body.size.y:0}x{body.size.z:0}");
            // With the controls held (a boss's talk mid-fight: Meret Venn's at the well) the HUD is off and the dialogue box is what stands at the foot.
            bool hudOff=GameServices.Current.Input.Blocked,talking=DialogueSystem.Current!=null&&DialogueSystem.Current.Running;
            if(hudOff)TestContext.WriteLine($"BOSS_FRAME {id} {where}: the controls are held{(talking?" by a talk":"")}, the HUD is off");
            if(hudOff?!Within(box,Rect.MinMaxRect(.02f,talking?.36f:.02f,.98f,.98f)):!Clear(box,true))faults.Add($"{where}: the lead's box is ({box.xMin:0.00},{box.yMin:0.00})-({box.xMax:0.00},{box.yMax:0.00}), under a panel or off the screen");
            if(size<(flight?LeastShipPixels:LeastHeroPixels))faults.Add($"{where}: the lead is {size:0} px {(flight?"across":"tall")} in 1080 (the camera {apart:0} m off)");
            if(seen<LeastSeen)faults.Add($"{where}: {seen:P0} of the lead shows: something hides him and nothing draws him through it");
            if(!on&&!marked)faults.Add($"{where}: the boss is off the screen and nothing says where");
            Keep($"{id}-{where.Replace(' ','-')}");
        }

        /// <summary>Rule (1) shown failing first: with the lead behind a body that hides him and his drawing-through switched off (the game as the capture played it) almost nothing of him
        /// shows; with it on, he does. The Foundry Saint in its own hall, its arms down as a slam leaves them, the lead on the far side of the press (nov20 s59 022, 024).</summary>
        [UnityTest] public IEnumerator BehindTheFoundrySaintTheLeadIsDrawnThroughItsBody()
        {
            if(!Graphics)Assert.Ignore("reads real pixels: run with the -Graphics runner");
            var sheet=ContentRegistry.Boss("FoundrySaint");foreach(string waited in sheet.doorFlags)Flag(waited);
            yield return Zone(sheet.zone,false);
            All<EncounterVolume>().Single(e=>e.encounterId==sheet.door).Begin();yield return Frames(2);
            foreach(var brain in All<EnemyBrain>())brain.Passive=true;
            var saint=All<EnemyBrain>().First(e=>e.definition!=null&&e.definition.id=="FoundrySaint");yield return Frames(3);Quiet();
            foreach(var arm in SaintArm.All)arm.Plant(120);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;
            Assert.IsTrue(NavMesh.SamplePosition(saint.transform.position+away*(BossController.SaintTune.LegHalfDepth+2.2f),out var hit,3,NavMesh.AllAreas),"fixture: floor behind the press");
            Put(Lead,hit.position);if(Partner!=null)Put(Partner,hit.position+Vector3.Cross(Vector3.up,away)*1.6f);
            yield return new WaitForSecondsRealtime(3);
            // The partner: the press's own door opens for the lead alone (BossOcclusion), and she had nothing at all (the capture's 'neither hero is visible').
            var partner=Partner;Assert.IsNotNull(partner,"fixture: a partner out");
            var eye=camera.transform.position;var chest=partner.transform.position+Vector3.up*1.2f;
            SetThrough(false);yield return Frames(2);float before=Seen(camera,partner),leadBefore=Seen(camera,Lead);Keep("FoundrySaint-behind-without");
            SetThrough(true);yield return Frames(2);float after=Seen(camera,partner),leadAfter=Seen(camera,Lead);Keep("FoundrySaint-behind-with");
            TestContext.WriteLine($"SAINT_BEHIND partner without={before:0.00} with={after:0.00} lead without={leadBefore:0.00} with={leadAfter:0.00} lens={Vector3.Distance(eye,chest):0.0}m");
            Assert.Less(before,LeastSeen,$"fixture: behind the press {before:P0} of the partner shows even without being drawn through: this place doesn't hide her");
            Assert.GreaterOrEqual(after,LeastSeen,$"drawn through the press, only {after:P0} of the partner shows");
            Assert.GreaterOrEqual(leadAfter,LeastSeen,$"drawn through the press, only {leadAfter:P0} of the lead shows");
        }

        /// <summary>The copies a hero is drawn through with hang under that hero and nowhere else: at the scene's root they were counted as props floating in the air in every zone
        /// (QaPlanWorldTests.NothingFloatsOrSinksIn, November 21). They are drawn only while their body is.</summary>
        [UnityTest] public IEnumerator TheDrawingThroughHangsUnderItsHero()
        {
            yield return Zone("Arena_Ground",true);yield return new WaitForSecondsRealtime(1.2f);
            foreach(var hero in PartyController.Current.members)
            {
                var through=hero.GetComponent<HeroThrough>();Assert.IsNotNull(through,$"{hero.character} isn't drawn through anything");
                Assert.Greater(through.Copies.Count(),0,$"{hero.character} has nothing drawn through");
                foreach(var copy in through.Copies)Assert.IsTrue(copy.transform.IsChildOf(hero.transform),$"{copy.name} hangs outside {hero.character}");
                foreach(var form in new[]{hero.GetComponent<FormController>().natural,hero.GetComponent<FormController>().shaped,hero.GetComponent<FormController>().flight})
                    if(form!=null)Assert.IsFalse(through.Holder.IsChildOf(form.transform),"the copies hang inside a form, where its own walks meet them");
            }
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())StringAssert.DoesNotContain("(through)",string.Join(",",root.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponentInParent<CombatActor>(true)==null).Select(t=>t.name)),"a hero's copy stands loose in the scene");
            var lead=Lead;Hide(lead,false);
            foreach(var body in lead.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(lead.GetComponent<HeroThrough>().Holder)))body.forceRenderingOff=true;
            yield return Frames(2);
            Assert.AreEqual(0,lead.GetComponent<HeroThrough>().Drawn,"a hidden hero's copies are still drawn");
            foreach(var body in lead.GetComponentsInChildren<Renderer>())body.forceRenderingOff=false;
        }

        /// <summary>The Bridle Foundry's hanging iron (its cranes' crucibles, the hoist and what hangs on it) over the edges of its fights, as rebuilt on November 21: from the play view at every
        /// two metres of each fight's floor, whatever of it stands between the lens and a hero's chest has the hero drawn through it, and what stands over a fight's floor (where a tell
        /// would be marked) is counted and named for the zone's engineer.</summary>
        [UnityTest] public IEnumerator TheFoundrysHangingIronHidesNoHeroItDoesNotDrawThrough()
        {
            yield return Zone("BridleFoundry",true);
            var rig=Object.FindFirstObjectByType<CameraRig>();var profile=rig.profile;var back=-(Quaternion.Euler(profile.pitch,profile.yaw,0)*Vector3.forward);
            // The iron that hangs, solid for this measure only: nothing of it is solid in play.
            var made=new List<GameObject>();var hanging=new List<string>();
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                string n=root.name.ToLowerInvariant();if(!(n.Contains("crane")||n.Contains("hoist")||n.Contains("crucible")||n.Contains("ladle")))continue;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==null||!filter.sharedMesh.isReadable||filter.GetComponent<MeshRenderer>()==null)continue;
                    var solid=new GameObject("Sight fixture: "+filter.name);solid.transform.SetParent(filter.transform,false);solid.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;made.Add(solid);
                }
                hanging.Add(root.name);
            }
            Physics.SyncTransforms();
            Assert.IsNotEmpty(hanging,"fixture: the Foundry has hanging iron to measure");
            var heroes=new List<(string fight,Vector3 at,string by)>();var tells=new Dictionary<string,int>();int points=0;
            foreach(var fight in All<EncounterVolume>())
            {
                var box=fight.GetComponent<Collider>().bounds;
                for(float x=box.min.x;x<=box.max.x;x+=2)for(float z=box.min.z;z<=box.max.z;z+=2)
                {
                    if(!NavMesh.SamplePosition(new Vector3(x,box.center.y,z),out var hit,1.5f,NavMesh.AllAreas)||Mathf.Abs(hit.position.x-x)>1||Mathf.Abs(hit.position.z-z)>1)continue;
                    points++;var floor=hit.position;var chest=floor+Vector3.up*1.2f;
                    RaycastHit seen;
                    if(Physics.Linecast(chest+back*profile.distance,chest,out seen,~0,QueryTriggerInteraction.Ignore)&&made.Contains(seen.collider.gameObject))heroes.Add((fight.encounterId,floor,seen.collider.transform.parent.name));
                    if(Physics.Linecast(floor+Vector3.up*.1f+back*profile.distance,floor+Vector3.up*.1f,out seen,~0,QueryTriggerInteraction.Ignore)&&made.Contains(seen.collider.gameObject))
                    {string key=fight.encounterId+" by "+seen.collider.transform.parent.name;tells[key]=tells.TryGetValue(key,out int c)?c+1:1;}
                }
            }
            foreach(var solid in made)Object.Destroy(solid);yield return null;
            TestContext.WriteLine($"FOUNDRY_IRON hanging={string.Join(", ",hanging)} floorPoints={points} heroesHidden={heroes.Count}");
            foreach(var pair in tells)TestContext.WriteLine($"FOUNDRY_IRON floor under iron (a tell there is hidden from the play view): {pair.Key} at {pair.Value} points of 2 m");
            Assert.Greater(points,20,"fixture: the Foundry's fights have floor");
            var faults=new List<string>();var camera=Camera.main;
            foreach(var (fight,at,by) in heroes.GroupBy(h=>h.fight+h.by).Select(g=>g.First()))
            {
                Put(Lead,at);if(Partner!=null)Put(Partner,at+Vector3.right*1.6f);
                yield return new WaitForSecondsRealtime(2.5f);
                float seen=Graphics?Seen(camera,Lead):1;
                TestContext.WriteLine($"FOUNDRY_IRON {fight}: {by} stands between the lens and a hero at ({at.x:0.0},{at.z:0.0}); {seen:P0} of him shows");
                Keep($"foundry-{fight}-{by.Replace(' ','-').Replace("(","").Replace(")","")}");
                if(seen<LeastSeen)faults.Add($"{fight}: behind {by} at ({at.x:0.0},{at.z:0.0}) only {seen:P0} of the lead shows");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ (2): an ordinary locked fight
        /// <summary>A locked enemy up the screen toward the party panel's corner is framed with the lead, both clear of the panels, and the partner too. Shown failing first: the same fight
        /// with the view held on the lead alone (what the capture had: nothing framed a lock in an ordinary fight) leaves the lock off the top of the frame (nov20 s46 047, s48 080, s35 298).</summary>
        [UnityTest] public IEnumerator ALockedEnemyAndTheLeadShareTheFrameClearOfThePanels()
        {
            yield return Zone("Arena_Ground",true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var hero=Lead;Put(hero,new Vector3(200,.05f,0));Put(Partner,new Vector3(201.6f,.05f,-1));
            yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;var right=Vector3.Cross(Vector3.up,away);var rig=Object.FindFirstObjectByType<CameraRig>();
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MusterScav"),hero.transform.position+away*13-right*9);
            yield return Frames(2);enemy.GetComponent<CharacterController>().enabled=false;enemy.Passive=false;
            hero.target=enemy.Health;hero.TargetLocked=true;
            Rect Enemy()=>Box(camera,enemy.transform.position,.5f,2.2f);
            // The capture's frame: the view on the lead alone.
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;)
            {rig.FrameGround(new Bounds(hero.transform.position+Vector3.up*.95f,new Vector3(1,1.9f,1)),Rect.MinMaxRect(.14f,.24f,.86f,.84f),Rect.MinMaxRect(.14f,.24f,.86f,.84f),28,9);yield return null;}
            var old=Enemy();
            TestContext.WriteLine($"LOCK_FRAME held-on-lead enemy=({old.xMin:0.00},{old.yMin:0.00})-({old.xMax:0.00},{old.yMax:0.00})");
            Assert.IsFalse(Clear(old,false),"fixture: held on the lead alone the lock already stands clear of the panels: this place doesn't test anything");
            yield return new WaitForSecondsRealtime(4);
            var now=Enemy();var lead=ScreenBox(camera,hero);var beside=ScreenBox(camera,Partner);
            TestContext.WriteLine($"LOCK_FRAME framed enemy=({now.xMin:0.00},{now.yMin:0.00})-({now.xMax:0.00},{now.yMax:0.00}) lead=({lead.xMin:0.00},{lead.yMin:0.00})-({lead.xMax:0.00},{lead.yMax:0.00}) partner=({beside.xMin:0.00},{beside.yMin:0.00})-({beside.xMax:0.00},{beside.yMax:0.00}) lens={rig.GroundCameraDistance:0.0}");
            Keep("ordinary-lock");
            Assert.IsTrue(Clear(now,false),$"the locked enemy stands at ({now.xMin:0.00},{now.yMin:0.00})-({now.xMax:0.00},{now.yMax:0.00}): under a panel or off the screen");
            Assert.IsTrue(Clear(lead,false),"the lead is under a panel or off the screen while he fights his lock");
            Assert.IsTrue(Clear(beside,false),"the partner is under a panel or off the screen beside a locked fight");
            Assert.GreaterOrEqual(lead.height*1080,LeastHeroPixels,"the lead is too small to play");
            Object.Destroy(enemy.gameObject);
        }
        static Rect Box(Camera camera,Vector3 foot,float half,float height)
        {
            float x0=float.MaxValue,y0=float.MaxValue,x1=float.MinValue,y1=float.MinValue;
            for(int i=0;i<8;i++){var view=camera.WorldToViewportPoint(foot+new Vector3((i&1)==0?-half:half,(i&2)==0?0:height,(i&4)==0?-half:half));if(view.z<=0)return Rect.zero;x0=Mathf.Min(x0,view.x);y0=Mathf.Min(y0,view.y);x1=Mathf.Max(x1,view.x);y1=Mathf.Max(y1,view.y);}
            return Rect.MinMaxRect(x0,y0,x1,y1);
        }

        // ------------------------------------------------------------------ (3): a body bigger than any view
        /// <summary>A boss whose body can't fit with the lead by the farthest the party reads at doesn't hold the camera at that limit: the view frames the lead and what he is fighting. The
        /// Tollkeeper's carrier held it 36 m back for the fight on its arm (nov20 s65 028 to 033). Shown with the Foundry Saint 30 m off in the open yard and a hound locked at the lead's side.</summary>
        [UnityTest] public IEnumerator ABodyTooBigToFrameDoesNotHoldTheViewAtItsLimit()
        {
            yield return Zone("Arena_Ground",true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(160,1,160);Physics.SyncTransforms();
            var hero=Lead;Put(hero,new Vector3(200,.05f,-20));Put(Partner,new Vector3(201.6f,.05f,-21));
            yield return new WaitForSecondsRealtime(1.2f);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            // Inside the 34 m a boss's bar comes up from, far enough up the screen that the press and the lead can't share a view by 46 m.
            var saint=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("FoundrySaint"),hero.transform.position+away*29);
            yield return Frames(3);NoSpecials(saint);saint.Passive=false;
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("MusterScav"),hero.transform.position+away*3);yield return Frames(2);hound.GetComponent<CharacterController>().enabled=false;hound.Passive=false;
            hero.target=hound.Health;hero.TargetLocked=true;
            yield return new WaitForSecondsRealtime(5);
            Assert.IsNotNull(BossController.Focus(hero.transform.position),"fixture: the Saint's bar is up");
            var lead=ScreenBox(camera,hero);float lens=Vector3.Distance(camera.transform.position,hero.transform.position);
            TestContext.WriteLine($"BIG_BODY lens={lens:0.0}m framed={rig.GroundCameraDistance:0.0} height={lead.height*1080:0}px edgeMark={EdgeMarkShowing()}");
            Keep("big-body-far");
            // At the old 36 m hold the lead was 106 px; framed for himself and his hound he is half as tall again.
            Assert.GreaterOrEqual(lead.height*1080,LeastHeroPixels*1.5f,$"a press 29 m off holds the view {rig.GroundCameraDistance:0} m back, the lead {lead.height*1080:0} px tall, for a body it can't show whole");
            Assert.Less(rig.GroundCameraDistance,30,"the view stands at the boss fight's limit for a body it can't show whole");
            Assert.IsTrue(Clear(lead,true),"the lead is under a panel or off the screen");
            Object.Destroy(hound.gameObject);Object.Destroy(saint.gameObject);
        }

        // ------------------------------------------------------------------ the lens never closes in
        /// <summary>Where a wall on the camera's side hides the party the camera leans over it at its play distance: it never comes nearer, where the Drowned Waystation's lower gallery had
        /// the lens inside its wall, the sky behind the zone in a third of the frame and the heroes from the thighs down (nov20 s26 301, 306: 6.3 m off). The place is the capture's own.</summary>
        [UnityTest] public IEnumerator InTheWaystationsLowerGalleryTheLensStaysBackAndLeansOverTheWall()
        {
            // As route 26 plays it at steps 295 to 306: both pumps run, the water down to its second stage (-4.5).
            foreach(string flag in new[]{"visited.DrownedWaystation","wend.tideTurned","wend.lureLearned","wend.pump1","wend.pump2"})Flag(flag);
            yield return Zone("DrownedWaystation",true);yield return new WaitForSecondsRealtime(3);
            // Where the capture's lead stood (frames.csv, steps 300 to 307), on whatever floor is under that point.
            var at=new Vector3(-24.2f,-3.8f,210.4f);Assert.IsTrue(Physics.Raycast(at+Vector3.up*1.5f,Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore),"fixture: the gallery floor where the capture stood");
            var hero=Lead;Put(hero,hit.point);if(Partner!=null)Put(Partner,hit.point+new Vector3(1.6f,0,-1));
            var rig=Object.FindFirstObjectByType<CameraRig>();var profile=rig.profile;
            // The capture's 6.3 m came in a fight there, from a view slid off the party that today's gallery doesn't reproduce by itself (frames.csv, steps 300 to 307). So the
            // test builds a wall of its own (not the game's): 5 m from him on the lens's side and 6 m over his chest, which hides him from the fixed lens and slid D198's in to its nearest.
            yield return new WaitForSecondsRealtime(1);Keep("waystation-lower-gallery-as-played");
            var chest=hero.transform.position+Vector3.up*1.2f;var head=hero.transform.position+Vector3.up*1.75f;var back=-(Quaternion.Euler(profile.pitch,profile.yaw,0)*Vector3.forward);
            var flat=Flat(back).normalized;var foot=hero.transform.position+flat*5;
            var built=GameObject.CreatePrimitive(PrimitiveType.Cube);built.name="HeroInSightTests' own wall (not in the game)";built.transform.rotation=Quaternion.LookRotation(flat);
            float bottom=hero.transform.position.y-1,height=chest.y+6-bottom;built.transform.position=new Vector3(foot.x,bottom+height*.5f,foot.z);built.transform.localScale=new Vector3(8,height,.5f);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(4);
            var fixedEye=hero.transform.position+back*profile.distance;
            bool blockedFixed=Physics.Linecast(fixedEye,chest,out var wall,~0,QueryTriggerInteraction.Ignore)&&CameraRig.Scenery(wall.collider);
            var camera=Camera.main;var eye=camera.transform.position;float lens=Vector3.Distance(eye,hero.transform.position);
            bool seenNow=!(Physics.Linecast(eye,chest,out var now,~0,QueryTriggerInteraction.Ignore)&&CameraRig.Scenery(now.collider))||!(Physics.Linecast(eye,head,out var top,~0,QueryTriggerInteraction.Ignore)&&CameraRig.Scenery(top.collider));
            bool inside=Physics.CheckSphere(eye,.5f,~0,QueryTriggerInteraction.Ignore);
            TestContext.WriteLine($"GALLERY_LENS fixedBlocked={blockedFixed} by={(blockedFixed?wall.collider.name:"")} lens={lens:0.0}m camDistance={rig.GroundCameraDistance:0.0} lean={rig.Lean:0} seen={seenNow} inside={inside}");
            Keep("waystation-lower-gallery-with-the-tests-own-wall");
            Assert.IsTrue(blockedFixed,"fixture: the test's own wall (not the game's) doesn't hide him from the fixed lens: this place doesn't test the rule");
            Assert.GreaterOrEqual(rig.GroundCameraDistance,profile.distance-.01f,$"the camera comes in to {rig.GroundCameraDistance:0.0} m, nearer than its play distance");
            Assert.GreaterOrEqual(lens,profile.distance-1.5f,$"the lens stands {lens:0.0} m from the lead");
            Assert.IsFalse(inside,"the lens stands inside the zone's solid");
            float seen=Graphics?Seen(camera,hero):1;
            TestContext.WriteLine($"GALLERY_SEEN {seen:0.00}");
            Assert.IsTrue(seenNow||seen>=LeastSeen,$"the lead is hidden by the test's own wall and not drawn through it ({seen:P0} of him shows)");
            Assert.Greater(rig.Lean,0,"the lens stays at the fixed angle with the test's own wall hiding him");
            Object.Destroy(built);
        }

        // ------------------------------------------------------------------ talks
        /// <summary>A boss who talks once its fight is over is found by the end of its id as well as the start: the Last Warden's stand-down ("Warden", whose body is LastWarden) was framed
        /// by nobody, his head off the top of the screen for the whole talk (nov20 s41 103). Shown failing first by the fixed framing before the talk.</summary>
        [UnityTest] public IEnumerator TheLastWardenIsFramedForHisOwnTalk()
        {
            yield return Zone("Arena_Ground",true);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Sight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var hero=Lead;Put(hero,new Vector3(200,.05f,0));Put(Partner,new Vector3(201.6f,.05f,-1));
            yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;
            var warden=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("LastWarden"),hero.transform.position+away*11);warden.Passive=true;
            yield return Frames(3);warden.GetComponent<CharacterController>().enabled=false;
            Bounds Body()=>BodyOf(warden);
            float Top()=>camera.WorldToViewportPoint(new Vector3(Body().center.x,Body().max.y,Body().center.z)).y;
            yield return new WaitForSecondsRealtime(1);
            float before=Top();
            Assert.Greater(before,.94f,"fixture: the fixed framing already shows the Warden's head");
            Assert.IsTrue(DialogueSystem.Current.StartNode("WardenAsks","Warden"),"fixture: the Warden's stand-down talk starts");
            yield return new WaitForSecondsRealtime(3.5f);
            float during=Top();var feet=camera.WorldToViewportPoint(new Vector3(Body().center.x,Body().min.y,Body().center.z)).y;
            TestContext.WriteLine($"WARDEN_TALK headBefore={before:0.00} headDuring={during:0.00} feet={feet:0.00}");
            Keep("warden-talk");
            DialogueSystem.Current.Cancel();
            Assert.Less(during,.96f,$"the Warden's head is off the top of the screen while he speaks ({during:0.00})");
            Assert.Greater(feet,.36f,"the Warden stands behind the dialogue box while he speaks");
        }
        /// <summary>A talk with someone across the screen pulls in from the edges: the lead inside the middle three fifths, the speaker inside the middle four fifths, both over the box. Shown
        /// failing first by the fixed framing before the talk, which has the speaker off the side (nov20 s18 010: Taren at the left edge, Ruva at the far right).</summary>
        [UnityTest] public IEnumerator ATalkAcrossTheScreenFramesBothInFromItsEdges()
        {
            yield return Zone("Sorrel_Ridges",true);
            var npc=All<Npc>().First(n=>n.speaker=="Survivor"&&n.Available);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;var right=Vector3.Cross(Vector3.up,away);
            var at=npc.transform.position-right*13+away*1;
            Assert.IsTrue(NavMesh.SamplePosition(at,out var hit,3,NavMesh.AllAreas),"fixture: floor across the screen from the survivor");
            var hero=Lead;Put(hero,hit.position);if(Partner!=null)Put(Partner,hit.position-right*1.6f);
            yield return new WaitForSecondsRealtime(2.5f);
            Rect Speaker()=>Box(camera,npc.transform.position,.4f,1.9f);
            var before=Speaker();
            Assert.IsFalse(Within(before,Rect.MinMaxRect(.1f,.38f,.9f,.94f)),"fixture: the fixed framing already shows the survivor clear of the edges");
            npc.Interact();float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: the talk starts");
            yield return new WaitForSecondsRealtime(3.5f);
            var speaker=Speaker();var lead=ScreenBox(camera,hero);
            TestContext.WriteLine($"TALK_FRAME speaker=({speaker.xMin:0.00},{speaker.yMin:0.00})-({speaker.xMax:0.00},{speaker.yMax:0.00}) lead=({lead.xMin:0.00},{lead.yMin:0.00})-({lead.xMax:0.00},{lead.yMax:0.00})");
            Keep("talk-across");
            DialogueSystem.Current.Cancel();
            Assert.IsTrue(Within(speaker,Rect.MinMaxRect(.08f,.36f,.92f,.96f)),"the speaker stands at the edge of the screen or behind the box");
            // His middle is held in .18 to .82; his box is a body's width either side of that.
            Assert.IsTrue(Within(lead,Rect.MinMaxRect(.14f,.36f,.86f,.92f)),"the lead stands at the edge of the screen or behind the box");
        }
    }
}
