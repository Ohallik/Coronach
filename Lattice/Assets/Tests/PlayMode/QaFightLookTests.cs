using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Batch 8, the eye pass: what a person watching the fights saw go wrong. The body of a boss comes apart, a won fight still threatens the party,
    /// a marking floods the floor, the hero can't be found behind a boss, the boss can't be found at all, and nobody can tell who is talking. Each failure message says
    /// what the player sees. The checks that read real pixels need the -Graphics runner the full suite already uses.</summary>
    public sealed class QaFightLookTests
    {
        /// <summary>The top edge of the dialogue box on the screen: DialoguePanel's frame stops 370 units up a 1080 one.</summary>
        public const float BoxTop=370f/1080;
        const int ShotWidth=1920,ShotHeight=1080;
        CombatActor Taren=>PartyController.Current.members.First(m=>m.character=="Taren");
        CombatActor Sela=>PartyController.Current.members.First(m=>m.character=="Sela");
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static IEnumerator Zone(string zone,bool clear=true)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            // The runner's own window isn't 16:9 (the Tallow fixtures say so too): what's read through the viewport has to be the view a player has, and agree with the shots below.
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)){if(clear)Object.Destroy(enemy.gameObject);else enemy.Passive=true;}
            if(clear)foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))volume.enabled=false;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;hero.target=null;hero.TargetLocked=false;Physics.SyncTransforms();
        }
        /// <summary>Open floor far from the arena's walls.</summary>
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static GameObject Wall(Vector3 center,Vector3 size)
        {var wall=new GameObject("Fight fixture wall",typeof(BoxCollider));wall.transform.position=center;wall.GetComponent<BoxCollider>().size=size;Physics.SyncTransforms();return wall;}
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        static void NoSpecials(EnemyBrain enemy)
        {if(enemy.TryGetComponent<BossController>(out var boss))typeof(BossController).GetField("nextSpecial",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(boss,Time.time+900);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 Floor(float x,float z,float level=0)=>CombatCover.Ground(new Vector3(x,level+4,z));
        static IEnumerator Tell(EnemyBrain enemy,float seconds=4)
        {
            for(float end=Time.unscaledTime+seconds;!enemy.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(enemy.Telegraphing,"fixture: "+enemy.definition.id+" never marked an attack");
        }
        static IEnumerator UntilFree(BossController boss,float seconds)
        {for(float end=Time.unscaledTime+seconds;boss.Busy&&Time.unscaledTime<end;)yield return null;}
        static void Fell(EnemyBrain boss)
        {boss.Health.Damageable=true;boss.Health.Receive(new DamagePacket{source=PartyController.Current.Active.Health,amount=boss.Health.maximum*50,type=DamageType.Plasma});}
        static int LiveMarkings=>Telegraph.Live.Count(t=>t!=null);
        /// <summary>A body's drawn bounds: its meshes, not a name label over it.</summary>
        static Bounds BodyOf(Component who)
        {
            bool any=false;var bounds=default(Bounds);
            foreach(var body in who.GetComponentsInChildren<Renderer>())
            {
                if(body is not MeshRenderer&&body is not SkinnedMeshRenderer||!body.enabled||body.GetComponent<TMPro.TMP_Text>()!=null)continue;
                if(any)bounds.Encapsulate(body.bounds);else{bounds=body.bounds;any=true;}
            }
            Assert.IsTrue(any,"fixture: "+who.name+" has a body");return bounds;
        }
        static bool Inside(Vector3 view,float x0,float y0,float x1,float y1)=>view.z>0&&view.x>=x0&&view.x<=x1&&view.y>=y0&&view.y<=y1;
        static void Hide(Component who,bool hidden)
        {foreach(var body in who.GetComponentsInChildren<Renderer>())if(body is MeshRenderer||body is SkinnedMeshRenderer)body.forceRenderingOff=hidden;}
        static void NoShadows(Component who)
        {foreach(var body in who.GetComponentsInChildren<Renderer>())body.shadowCastingMode=ShadowCastingMode.Off;}
        static void NeedsGraphics()=>Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"this reads real pixels: run it with the -Graphics runner");
        static Color32[] Shot(Camera camera)
        {
            var image=new Texture2D(ShotWidth,ShotHeight,TextureFormat.RGB24,false);var rt=new RenderTexture(ShotWidth,ShotHeight,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,ShotWidth,ShotHeight),0,0);image.Apply();return image.GetPixels32();
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);}
        }
        /// <summary>How much two shots differ at one pixel, 0 to 1 per channel on average.</summary>
        static float Change(Color32 a,Color32 b)=>(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(3*255f);
        static float ChangeAt(Color32[] a,Color32[] b,Camera camera,Vector3 world)
        {
            var view=camera.WorldToViewportPoint(world);if(view.z<=0||view.x<0||view.x>=1||view.y<0||view.y>=1)return 0;
            int index=(int)(view.y*ShotHeight)*ShotWidth+(int)(view.x*ShotWidth);return Change(a[index],b[index]);
        }
        static float Percentile(List<float> values,float share){values.Sort();return values[Mathf.Clamp((int)(values.Count*share),0,values.Count-1)];}

        // ------------------------------------------------------------------ the Cantor holds together
        static void MeasureChain(EnemyBrain cantor,IReadOnlyList<Transform> chain,ref float widest,ref float closest)
        {
            var nodes=new List<Vector3>{Flat(cantor.transform.position)};foreach(var part in chain)nodes.Add(Flat(part.position));
            for(int a=0;a<nodes.Count;a++)for(int b=a+1;b<nodes.Count;b++)
            {
                float gap=Vector3.Distance(nodes[a],nodes[b]);
                if(b==a+1)widest=Mathf.Max(widest,gap);else closest=Mathf.Min(closest,gap);
            }
        }
        [UnityTest] public IEnumerator TheCantorHoldsTogetherWhenItLashesAndDoublesBack()
        {
            yield return Zone("Arena_Flight");
            Put(Taren,new Vector3(300,1,0));Put(Sela,new Vector3(310,1,0));
            var cantor=Enemy("Cantor",new Vector3(200,1,-20));yield return Frames(3);
            cantor.GetComponent<CharacterController>().enabled=false;
            var chain=cantor.GetComponent<SerpentSegments>().Parts;
            float widest=0,closest=float.PositiveInfinity;
            // Its lash is 24 m/s. Straight on, back along itself, off to the side and back across itself.
            foreach(var (direction,seconds) in new[]{(Vector3.forward,1f),(new Vector3(.05f,0,-1).normalized,1f),(Vector3.right,.5f),(new Vector3(-1,0,.3f).normalized,.8f)})
                for(float t=0;t<seconds;)
                {
                    yield return null;
                    MeasureChain(cantor,chain,ref widest,ref closest);
                    t+=Time.deltaTime;cantor.transform.position+=direction*(24*Time.deltaTime);
                }
            Assert.LessOrEqual(widest,2.3f,$"at speed a joint hangs {widest:0.0} m from the next on 2.6 m drums: the boss comes apart into a dotted line of barrels");
            Assert.GreaterOrEqual(closest,2.4f,$"doubling back brings two parts of the body within {closest:0.0} m of each other on 2 m wide drums: the Cantor pushes through itself");
        }
        [UnityTest] public IEnumerator TheDrumsCoverEveryBendTheBodyMakes()
        {
            yield return Zone("Arena_Flight");
            Put(Taren,new Vector3(300,1,0));Put(Sela,new Vector3(310,1,0));
            var cantor=Enemy("Cantor",new Vector3(206,1,0));yield return Frames(3);
            cantor.GetComponent<CharacterController>().enabled=false;
            var chain=cantor.GetComponent<SerpentSegments>().Parts;
            float bendiest=0,around=0;
            // The radius the brain itself swims on, at twice the pace, for long enough that the whole body is on the curve.
            while(around<Mathf.PI*3)
            {
                yield return null;
                if(around>Mathf.PI)
                {
                    var ahead=Flat(cantor.transform.position-chain[0].position);
                    for(int i=1;i<chain.Count;i++){var link=Flat(chain[i-1].position-chain[i].position);bendiest=Mathf.Max(bendiest,Vector3.Angle(ahead,link));ahead=link;}
                }
                around+=7*Time.deltaTime/6;
                cantor.transform.position=new Vector3(200+Mathf.Cos(around)*6,1,Mathf.Sin(around)*6);
            }
            Assert.LessOrEqual(bendiest,28,$"a broad swim folds the body {bendiest:0} degrees at a joint: the drums part on the outside of the bend");
            // A wedge opens on the outside of a bend once the far rim of one drum clears the end of the next. At 26 degrees it has to stay shut.
            float bend=26*Mathf.Deg2Rad;
            for(int i=0;i<chain.Count-1;i++)
            {
                float low=float.PositiveInfinity,high=float.NegativeInfinity,side=0;
                foreach(var body in chain[i].GetComponentsInChildren<Renderer>())
                {
                    if(body is not MeshRenderer&&body is not SkinnedMeshRenderer)continue;var box=body.localBounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                        var point=body.transform.TransformPoint(box.center+Vector3.Scale(box.extents,sign))-chain[i].position;
                        float along=Vector3.Dot(point,chain[i].forward);low=Mathf.Min(low,along);high=Mathf.Max(high,along);
                        side=Mathf.Max(side,Mathf.Abs(Vector3.Dot(point,chain[i].right)));
                    }
                }
                float half=(high-low)*.5f,spacing=Vector3.Distance(Flat(chain[i].position),Flat(chain[i+1].position));
                float closedTo=(half*Mathf.Cos(bend)-(spacing-half))/Mathf.Sin(bend);
                Assert.GreaterOrEqual(closedTo,side,$"drum {i} is {high-low:0.00} m long on a {spacing:0.00} m link: at a 26 degree bend the two drums part and the floor shows between them");
            }
        }

        // ------------------------------------------------------------------ nothing threatens after the fight is over
        [UnityTest] public IEnumerator ABossFallsAndItsMarkingsAreGoneOnThatFrame()
        {
            yield return Zone("Arena_Ground");Arena();
            Put(Taren,new Vector3(200,.1f,0));Put(Sela,new Vector3(260,.1f,0));
            foreach(var (id,move) in new[]{("Burrower","DrillCharge"),("BellowsBelow","Breath"),("HessaKorl","Lampbreaker")})
            {
                var boss=Enemy(id,new Vector3(200,.1f,12));yield return Frames(3);var moves=boss.GetComponent<BossController>();
                Assert.IsTrue(moves.Perform(move),"fixture: "+id+" starts its move");yield return Frames(3);
                Assert.Greater(Telegraph.Live.Count(t=>t!=null&&t.owner==moves),0,"fixture: "+id+" has something marked");
                Fell(boss);yield return null;
                Assert.AreEqual(0,LiveMarkings,$"the screen that pays out the win over {id} still shows its attack marked through the party");
                Object.Destroy(boss.gameObject);yield return Frames(2);
            }
            // The filler attack's marking is the brain's own, as it was through Sela when the collar opened.
            yield return Zone("Arena_Flight");
            Put(Taren,new Vector3(200,1,-10));Put(Sela,new Vector3(260,1,0));
            var cantor=Enemy("Cantor",new Vector3(200,1,0));yield return Frames(3);NoSpecials(cantor);
            cantor.Passive=false;yield return Tell(cantor);
            Fell(cantor);yield return null;
            Assert.AreEqual(0,LiveMarkings,"the collar opens, the bar is empty and the rewards are paying out, and an orange lane is still marked through Sela");
        }
        [UnityTest] public IEnumerator CrewDismissedWithTheirCaptainLeaveNothingPainted()
        {
            yield return Zone("Arena_Ground");Arena();
            Put(Taren,new Vector3(200,.1f,10));Put(Sela,new Vector3(260,.1f,0));
            var captain=Enemy("HessaKorl",new Vector3(200,.1f,0));var crew=Enemy("MusterScav",new Vector3(206,.1f,2));yield return Frames(3);
            var summoned=(List<EnemyBrain>)typeof(BossController).GetField("summoned",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(captain.GetComponent<BossController>());summoned.Add(crew);
            crew.Passive=false;yield return Tell(crew);var lane=crew.AttackMark;Assert.IsNotNull(lane,"fixture: the crewman has a lane marked");
            Fell(captain);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(crew==null,"fixture: the crew leaves with the fight");
            Assert.IsTrue(lane==null,"her crew are dismissed mid-tell and their orange lanes stay painted on the floor for good, through the whole surrender talk and after it");
        }
        [UnityTest] public IEnumerator AnEnemyRemovedMidTellTakesItsMarkingWithIt()
        {
            yield return Zone("Arena_Ground");Arena();
            Put(Taren,new Vector3(200,.1f,3));Put(Sela,new Vector3(260,.1f,0));
            var hound=Enemy("Ridgehound",new Vector3(200,.1f,0));yield return Frames(3);
            hound.Passive=false;yield return Tell(hound);var lane=hound.AttackMark;
            Object.Destroy(hound.gameObject);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.IsTrue(lane==null,"an enemy removed in the middle of its tell leaves its marking on the floor forever, charged and striking nobody");
        }
        [UnityTest] public IEnumerator ANetAndAShotDoNotOutliveTheFightOrWaitOutItsTalk()
        {
            yield return Zone("Arena_Ground");Arena();
            Put(Taren,new Vector3(200,.1f,6));Put(Sela,new Vector3(260,.1f,0));
            var boss=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);
            NetSeed.Land(boss.transform.position,Sela.Packet(10,DamageType.Pulse,0));yield return Frames(3);
            Assert.IsNotNull(Object.FindFirstObjectByType<PulseField>(),"fixture: Sela's net is down");
            Projectile.Fire(boss.transform.position+Vector3.up,Vector3.left,new DamagePacket{source=boss.Health,amount=5,type=DamageType.Beam},2);
            Fell(boss);
            // The surrender talk starts soon after, and holds everything's clock.
            GameServices.Current.Input.Blocked=true;
            yield return new WaitForSecondsRealtime(.6f);
            bool net=Object.FindFirstObjectByType<PulseField>()!=null;int shots=Projectile.Live.Count(s=>s.Threatens(true));
            GameServices.Current.Input.Blocked=false;
            Assert.IsFalse(net,"the Static Net's ring circles an empty floor after the boss has gone, and stays through the whole surrender talk, because a conversation stops its clock");
            Assert.AreEqual(0,shots,"shots from a boss who has already fallen are still flying at the party");
        }

        // ------------------------------------------------------------------ the collar's line
        [UnityTest] public IEnumerator TheCollarsLineDoesNotContradictTheBarUnderIt()
        {
            yield return Zone("Arena_Flight");
            // Taren's line is the one the reader saw; each hero has their own.
            var party=PartyController.Current;if(party.Active.character!="Taren")Assert.IsTrue(party.Swap(),"fixture: the swap");
            foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            Put(Taren,new Vector3(200,1,-20));Put(Sela,new Vector3(240,1,-20));
            var cantor=Enemy("Cantor",new Vector3(200,1,0));yield return Frames(3);var boss=cantor.GetComponent<BossController>();
            var said=new List<string>();void Heard(string who,string line){said.Add(line);}BarkService.Spoken+=Heard;
            Assert.IsTrue(boss.Perform("Choir"),"fixture: the song");yield return UntilFree(boss,3);
            BarkService.Spoken-=Heard;
            var line_=said.FirstOrDefault(l=>l.Contains("collar"));Assert.IsNotNull(line_,"fixture: somebody calls the opening: "+string.Join(" / ",said));
            StringAssert.DoesNotContain("gone slack",line_,"the opening lasts 1.2 s and the bark stays up 3.5 s: 'The collar's gone slack' sits over 'THE COLLAR HOLDS' for most of it, with no link cut");
        }

        // ------------------------------------------------------------------ markings
        [UnityTest] public IEnumerator ACircleReadsRoundAtTheCamerasDistance()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            var at=new Vector3(200,0,0);
            foreach(var (radius,light) in new[]{(3f,true),(1.9f,true),(11f,false)})
            {
                var mark=Telegraph.Circle(at,radius,5,Telegraph.Threat,light);
                var rim=mark.GetComponent<MeshFilter>().sharedMesh.vertices.Where(v=>Mathf.Abs(Vector3.Distance(Flat(v),Flat(at))-radius)<.01f).Select(v=>Mathf.Atan2(v.x-at.x,v.z-at.z)).OrderBy(a=>a).ToList();
                float widest=0;for(int i=1;i<rim.Count;i++)widest=Mathf.Max(widest,rim[i]-rim[i-1]);
                float sag=radius*(1-Mathf.Cos(widest*.5f));
                Assert.LessOrEqual(sag,.02f,$"a {radius} m danger ring has straight sides {sag*100:0.0} cm off round, about {sag*103:0} px at the ground camera: it reads as a polygon");
                Object.Destroy(mark.gameObject);yield return null;
            }
        }
        [UnityTest] public IEnumerator AMarkingLiesOnTheFloorNotAcrossWhatStandsOnIt()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            // Sorrel's sealed drill gate is a bar like this: 0.6 m high, across the arena.
            var bar=Wall(new Vector3(200,.3f,0),new Vector3(20,.6f,.6f));
            var mark=Telegraph.Circle(new Vector3(200,0,0),5,5,Telegraph.Heavy);
            float rise=mark.GetComponent<MeshFilter>().sharedMesh.vertices.Max(v=>v.y);
            Assert.Less(rise,.2f,$"a marking climbs {rise:0.00} m up a low bar crossing it and its outline breaks into shards over the top");
            Object.Destroy(mark.gameObject);Object.Destroy(bar);yield return null;Physics.SyncTransforms();
            // A real slope it still follows.
            // Clear of the fixture's floor: a ramp sunk through it would hand the marking a nearer surface at each end of the slope.
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetPositionAndRotation(new Vector3(200,3,0),Quaternion.Euler(-10,0,0));ramp.transform.localScale=new Vector3(30,1,60);Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(new Vector3(200,20,0),Vector3.down,out var ground,60),"fixture: the ramp");
            mark=Telegraph.Circle(ground.point,6,5,Telegraph.Heavy);var heights=mark.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>v.y).ToList();
            Assert.Greater(heights.Max()-heights.Min(),1.2f,"fixture: the marking follows the slope it lies on");
        }
        [UnityTest] public IEnumerator AMarkingsEdgeIsTheStrongestThingOnItsFloor()
        {
            NeedsGraphics();
            foreach(var (zone,spot,level,color) in new[]{("Hushwell",new Vector2(-4,255),HushwellLayout.Middle,Telegraph.Stagger),("Sorrel_Ridges",new Vector2(-4,170),0f,Telegraph.Threat)})
            {
                yield return Zone(zone);
                var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
                Put(hero,Floor(spot.x,spot.y,level));Put(partner,Floor(spot.x-3,spot.y,level));
                yield return new WaitForSecondsRealtime(1.5f);
                var camera=Camera.main;var right=Flat(camera.transform.right).normalized;
                const float Radius=4;var center=Floor(spot.x+right.x*5.2f,spot.y+right.z*5.2f,level);
                var mark=Telegraph.Circle(center,Radius,.01f,color);yield return new WaitForSecondsRealtime(.25f);
                var view=mark.GetComponent<MeshRenderer>();
                var marked=Shot(camera);view.enabled=false;var bare=Shot(camera);view.enabled=true;
                var fill=new List<float>();var rim=new List<float>();
                for(float x=-Radius;x<=Radius;x+=.09f)for(float z=-Radius;z<=Radius;z+=.09f)
                {
                    float r=Mathf.Sqrt(x*x+z*z);if(r<Radius*.3f||r>Radius*.8f)continue;
                    fill.Add(ChangeAt(marked,bare,camera,Floor(center.x+x,center.z+z,level)));
                }
                for(float a=0;a<360;a+=5)
                {
                    var out_=Quaternion.Euler(0,a,0)*Vector3.forward;float best=0;
                    foreach(float d in new[]{.03f,.06f,.09f,.12f})best=Mathf.Max(best,ChangeAt(marked,bare,camera,Floor(center.x+out_.x*(Radius-d),center.z+out_.z*(Radius-d),level)));
                    rim.Add(best);
                }
                float middle=Percentile(fill,.5f),high=Percentile(fill,.92f),edge=Percentile(rim,.5f);
                Object.Destroy(mark.gameObject);
                TestContext.WriteLine($"MARKING {zone} fill={middle:0.000} hatch={high:0.000} rim={edge:0.000}");
                Assert.LessOrEqual(middle,.14f,$"on {zone}'s floor the marked area shifts every pixel by {middle:0.00}: a big ring or breath floods the floor in flat colour and hides what's on it");
                Assert.GreaterOrEqual(edge,middle*3,$"on {zone}'s floor the edge of the danger ({edge:0.00}) barely stands out from its fill ({middle:0.00}): the edge has to be the strongest thing");
                Assert.GreaterOrEqual(high,middle*1.35f,$"on {zone}'s floor the marked area is one flat sheet of colour ({middle:0.00} to {high:0.00}): nothing says marked rather than painted");
            }
        }
        [UnityTest] public IEnumerator AMarkingsOutlineShowsThroughWhatStandsInIt()
        {
            NeedsGraphics();yield return Zone("Sorrel_Ridges");
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            var center=Floor(-4,170);Put(hero,center);Put(partner,Floor(-14,170));
            yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var toward=Flat(camera.transform.position-center).normalized;
            const float Radius=4;var mark=Telegraph.Circle(center,Radius,.01f,Telegraph.Threat);yield return new WaitForSecondsRealtime(.25f);
            // A low prop on the near rim, between it and the camera, as Sorrel's grass stands on a Scrapmite's disc.
            // Read off the floor where the rim really lies: Sorrel isn't level, and a few centimetres put it outside the window read below.
            var rimAt=center+toward*(Radius-.09f);var rimPoint=Floor(rimAt.x,rimAt.z,center.y);
            var prop=ActorFactory.Visual("Fixture prop",PrimitiveType.Cube,null,new Vector3(1.8f,1.2f,.6f),rimPoint+toward*.59f+Vector3.up*.6f,"Rock");prop.transform.rotation=Quaternion.LookRotation(toward);
            yield return Frames(2);
            var view=mark.GetComponent<MeshRenderer>();var marked=Shot(camera);view.enabled=false;var bare=Shot(camera);view.enabled=true;
            var seen=camera.WorldToViewportPoint(rimPoint);int px=(int)(seen.x*ShotWidth),py=(int)(seen.y*ShotHeight);float strongest=0;
            for(int y=py-8;y<=py+8;y++)for(int x=px-8;x<=px+8;x++)strongest=Mathf.Max(strongest,Change(marked[y*ShotWidth+x],bare[y*ShotWidth+x]));
            Assert.GreaterOrEqual(strongest,.05f,"what stands on a marking hides its edge completely: you can't see where the danger ends on that side");
            float chest=ChangeAt(marked,bare,camera,hero.transform.position+Vector3.up*1.1f);
            Assert.Less(chest,.03f,$"the marking draws over the hero standing in it ({chest:0.00}): the body has to stay in front at full strength");
        }

        // ------------------------------------------------------------------ seeing through
        [UnityTest] public IEnumerator TheSeeThroughIsHeroSizedAndKeepsTheBody()
        {
            yield return Zone("Arena_Ground");
            var hero=PartyController.Current.Active;var camera=Camera.main;
            var toward=Flat(camera.transform.position-hero.transform.position).normalized;
            var boss=Enemy("Burrower",hero.transform.position+toward*2.3f);yield return new WaitForSecondsRealtime(.5f);
            var body=boss.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer);
            var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
            Assert.Greater(block.GetFloat("_OccFade"),.9f,"fixture: the boss is opening for the hero");
            var ellipse=block.GetVector("_OccFadeEllipse");
            var chest=hero.transform.position+Vector3.up*.95f;var middle=camera.WorldToViewportPoint(chest);
            float wide=ellipse.z/Mathf.Abs(camera.WorldToViewportPoint(chest+camera.transform.right).x-middle.x),tall=ellipse.w/Mathf.Abs(camera.WorldToViewportPoint(chest+camera.transform.up).y-middle.y);
            Assert.LessOrEqual(wide,.75f,$"the see-through reaches {wide:0.00} m either side of a hero who is about .5 m: it reads as a bite out of the boss");
            Assert.LessOrEqual(tall,1.15f,$"the see-through reaches {tall:0.00} m above and below a hero who stands 1.9 m at this angle: it reads as a bite out of the boss");
            float depth=block.GetFloat("_OccFadeDepth");
            Assert.That(depth,Is.InRange(.5f,.65f),$"the boss's screen-door opens {depth:0.00} of its pixels: most of the body and its weak spot are gone where the hero stands");
            // He shows through in his own colours, and the door shuts when nothing hides him. QaFightSecondTests stands him beside a real body and behind one.
            bool Ghosted()=>hero.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>m!=null&&m.shader!=null&&m.shader.name=="Lattice/HeroSilhouette"));
            yield return Frames(2);
            Assert.IsFalse(Ghosted(),"a flat cyan ghost lies over the hero behind the boss, and through his own limbs");
            var controller=boss.GetComponent<CharacterController>();controller.enabled=false;boss.transform.position=hero.transform.position-toward*15;controller.enabled=true;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.3f);
            body.GetPropertyBlock(block);Assert.Less(block.GetFloat("_OccFade"),.01f,"the door stays open on the boss after it has moved away from the hero");
        }
        [UnityTest] public IEnumerator HessasBodyOpensForTheHeroToo()
        {
            yield return Zone("Arena_Ground");
            var hero=PartyController.Current.Active;var camera=Camera.main;
            var toward=Flat(camera.transform.position-hero.transform.position).normalized;
            var boss=Enemy("HessaKorl",hero.transform.position+toward*1.5f);yield return new WaitForSecondsRealtime(.5f);
            var body=boss.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer);
            var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
            Assert.Greater(block.GetFloat("_OccFade"),.9f,"a hero behind a captain half as tall again is simply gone: she has no see-through at all");
            Assert.IsTrue(body.sharedMaterials.All(m=>m.IsKeywordEnabled("_OCCFADE_ON")),"her material was built without the fade, so a player build would draw nothing for it");
        }
        [UnityTest] public IEnumerator TheWreckOpensForAHeroBehindIt()
        {
            foreach(var flag in new[]{"met.Survivor","bossdown.Burrower","clear.Sorrel_Burrower"})Flag(flag);
            yield return Zone("Sorrel_Ridges");
            var site=Object.FindFirstObjectByType<BossWreck>();Assert.IsNotNull(site,"fixture: the wreck's site");Assert.IsTrue(site.wreck.activeInHierarchy,"fixture: a later visit finds the wreck");
            var bounds=BodyOf(site.rig);var hero=PartyController.Current.Active;var camera=Camera.main;
            // Behind it for the camera: on the line from the lens through the middle of the body itself, down at chest height.
            var floor=Floor(bounds.center.x,bounds.center.z,bounds.min.y).y;var look=camera.transform.forward;
            var points=site.rig.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToList();
            var middle=new Vector3(points.Average(p=>p.x),floor+(points.Max(p=>p.y)-floor)*.6f,points.Average(p=>p.z));
            var behind=middle+look*((middle.y-(floor+.95f))/-look.y);Put(hero,Floor(behind.x,behind.z,bounds.min.y));
            yield return new WaitForSecondsRealtime(1.5f);
            var body=site.rig.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer);
            var block=new MaterialPropertyBlock();body.GetPropertyBlock(block);
            Assert.Greater(block.GetFloat("_OccFade"),.9f,"Taren is a head and shoulders behind the downed rig: nothing opens for the hero, and it has no collider, so anyone can walk there");
        }

        // ------------------------------------------------------------------ ground bosses stay in the frame
        IEnumerator FightStaysInFrame(string zone,string encounterId,string bossId,float level,bool checkReturn,params (Vector2 hero,Vector2 boss,string where)[] places)
        {
            yield return Zone(zone,false);
            var encounter=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId==encounterId);
            encounter.Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id==bossId);
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            var rig=Object.FindFirstObjectByType<CameraRig>();
            foreach(var (heroAt,bossAt,where) in places)
            {
                Put(hero,Floor(heroAt.x,heroAt.y,level));Put(partner,Floor(heroAt.x+1.5f,heroAt.y,level));
                boss.transform.position=Floor(bossAt.x,bossAt.y,level);
                yield return new WaitForSecondsRealtime(4);
                Assert.IsFalse(DialogueSystem.Current.Running,"fixture: nothing else is on the screen");
                Assert.IsNotNull(BossController.Focus(hero.transform.position),"fixture: the boss bar is up");
                var camera=Camera.main;var body=BodyOf(boss);
                Assert.Less(Quaternion.Angle(camera.transform.rotation,Quaternion.Euler(rig.profile.pitch,rig.profile.yaw,0)),.5f,"the fixed camera angle moved");
                foreach(var (point,name) in new[]{(hero.transform.position,"feet"),(hero.transform.position+Vector3.up*1.9f,"head")})
                {
                    var view=camera.WorldToViewportPoint(point);
                    Assert.IsTrue(Inside(view,.08f,.18f,.92f,.92f),$"16 m from the {bossId} ({where}), the hero's {name} is at {view}: under the HUD or out of frame");
                }
                foreach(var (point,name) in new[]{(body.center,"body"),(new Vector3(body.center.x,body.max.y,body.center.z),"top"),(new Vector3(body.center.x,body.min.y,body.center.z),"foot")})
                {
                    var view=camera.WorldToViewportPoint(point);
                    Assert.IsTrue(Inside(view,.01f,.01f,.99f,.99f),$"16 m from the hero ({where}), the {bossId}'s {name} is at {view}: the boss is off screen while its markings come in from outside the frame");
                }
            }
            if(!checkReturn)yield break;
            Fell(boss);yield return new WaitForSecondsRealtime(6);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.75f,"the camera stays drawn back after the fight has ended");
            var settled=Camera.main.WorldToViewportPoint(hero.transform.position);
            Assert.IsTrue(Inside(settled,.42f,.42f,.58f,.58f),$"the view stays slid off the hero after the fight has ended ({settled})");
        }
        [UnityTest] public IEnumerator TheBurrowerStaysInFrame()
        {
            Flag("met.Survivor");
            yield return FightStaysInFrame("Sorrel_Ridges","Sorrel_Burrower","Burrower",0,true,
                (new Vector2(0,166),new Vector2(0,182),"up the screen"),(new Vector2(0,182),new Vector2(0,166),"down the screen"),(new Vector2(-8,168),new Vector2(8,168),"to the right"),(new Vector2(8,168),new Vector2(-8,168),"to the left"));
        }
        [UnityTest] public IEnumerator TheBellowsStaysInFrame()=>FightStaysInFrame("Hushwell","Hushwell_Bellows","BellowsBelow",HushwellLayout.Middle,false,
            (new Vector2(0,256),new Vector2(0,272),"up the screen"),(new Vector2(0,272),new Vector2(0,256),"down the screen"),(new Vector2(-8,262),new Vector2(8,262),"to the right"),(new Vector2(8,262),new Vector2(-8,262),"to the left"));
        [UnityTest] public IEnumerator HessaStaysInFrame()
        {
            Flag("relay.hessaConfronted");
            yield return FightStaysInFrame("SunkenRelay","Relay_Hessa","HessaKorl",0,false,
                (new Vector2(0,130),new Vector2(0,146),"up the screen"),(new Vector2(0,146),new Vector2(0,130),"down the screen"),(new Vector2(-8,138),new Vector2(8,138),"to the right"),(new Vector2(8,138),new Vector2(-8,138),"to the left"));
        }
        [UnityTest] public IEnumerator ABossTooFarToFitGetsAnEdgeMark()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            // West of the hero: the camera's yaw brings that side nearer, where the view is narrow. 33 m due east she still fits at the farthest pull-back.
            Put(hero,new Vector3(213,.1f,0));Put(partner,new Vector3(215,.1f,0));
            var boss=Enemy("HessaKorl",new Vector3(180,.1f,0));yield return Frames(3);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;boss.Passive=false;
            yield return new WaitForSecondsRealtime(4);
            var view=Camera.main.WorldToViewportPoint(boss.transform.position+Vector3.up);
            Assert.IsFalse(Inside(view,0,0,1,1),$"fixture: this far off she can't fit ({view})");
            var mark=Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name=="BossEdgeMark");
            Assert.IsNotNull(mark,"the boss is off screen and nothing says where: its markings just come in from outside the frame");
            Assert.IsTrue(mark.enabled&&mark.gameObject.activeInHierarchy,"the boss is off screen and the edge mark isn't showing");
            Assert.Less(mark.rectTransform.anchorMin.x,.2f,"the edge mark doesn't point the way to the boss");
            boss.transform.position=new Vector3(205,.1f,0);yield return new WaitForSecondsRealtime(4);
            Assert.IsFalse(mark.enabled,"the edge mark stays up with the boss in plain sight");
        }
        [UnityTest] public IEnumerator ACrateOnTheFloorDoesNotDiveTheCameraInAFight()
        {
            Flag("met.Survivor");
            yield return Zone("Sorrel_Ridges",false);
            Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Sorrel_Burrower").Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="Burrower");
            boss.Passive=false;yield return Frames(2);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);var rig=Object.FindFirstObjectByType<CameraRig>();
            Put(hero,Floor(0,182));Put(partner,Floor(1.5f,182));boss.transform.position=Floor(0,166);
            yield return new WaitForSecondsRealtime(4);
            Assert.Greater(rig.GroundCameraDistance,rig.profile.distance+5,"with the boss 16 m down the screen the camera never draws back to show it");
            // The view has slid down the screen, so its own axis starts under the floor and comes up a few metres toward the camera. Whatever stands there hides nobody.
            var camera=Camera.main;var axis=camera.transform.position+camera.transform.forward*rig.GroundCameraDistance+Vector3.up*1.2f;
            float along=(hero.transform.position.y+.5f-axis.y)/-camera.transform.forward.y;
            Assert.Greater(along,1,"fixture: the slid view's axis starts under the floor");
            var crate=GameObject.CreatePrimitive(PrimitiveType.Cube);crate.transform.position=axis-camera.transform.forward*along;crate.transform.localScale=new Vector3(2,1,2);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(rig.GroundCameraDistance,rig.profile.distance+5,$"a crate that hides nobody dives the camera to {rig.GroundCameraDistance:0} m in the middle of a fight");
        }
        [UnityTest] public IEnumerator TheWayOutOfAWideMarkingIsOnScreen()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            Put(hero,new Vector3(200,.1f,-3));Put(partner,new Vector3(202,.1f,-3));
            var boss=Enemy("HessaKorl",new Vector3(200,.1f,0));yield return Frames(3);NoSpecials(boss);boss.GetComponent<CharacterController>().enabled=false;boss.Passive=false;
            var ring=Telegraph.Circle(boss.transform.position,11,30,Telegraph.Stagger);ring.owner=boss.GetComponent<BossController>();
            yield return new WaitForSecondsRealtime(3);
            var edge=Camera.main.WorldToViewportPoint(new Vector3(200,.1f,-11));
            Assert.IsTrue(Inside(edge,.01f,.01f,.99f,.99f),$"a Lampbreaker ring floods the view and the nearest way out of it, 8 m south of the hero, is off screen ({edge})");
            Object.Destroy(ring.gameObject);
        }

        // ------------------------------------------------------------------ conversations show who is talking
        [UnityTest] public IEnumerator AConversationFramesHeroAndSpeakerAboveTheBox()
        {
            yield return Zone("Sorrel_Ridges");
            var npc=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).First(n=>n.speaker=="Survivor"&&n.Available);
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            var camera=Camera.main;var away=Flat(camera.transform.forward).normalized;var rig=Object.FindFirstObjectByType<CameraRig>();
            // Someone standing down-screen of the hero: at the fixed framing they are behind the box.
            var at=npc.transform.position+away*6;Put(hero,Floor(at.x,at.z,npc.transform.position.y));Put(partner,Floor(at.x+2,at.z,npc.transform.position.y));
            yield return new WaitForSecondsRealtime(2);
            npc.Interact();
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: the talk starts");
            yield return new WaitForSecondsRealtime(3);
            var body=BodyOf(npc);
            var foot=camera.WorldToViewportPoint(new Vector3(body.center.x,body.min.y,body.center.z));var top=camera.WorldToViewportPoint(new Vector3(body.center.x,body.max.y,body.center.z));
            Assert.Greater(foot.y,BoxTop,$"the person talking stands behind the dialogue box ({foot})");
            Assert.Less(top.y,1,$"the person talking is off the top of the screen ({top})");
            var heroFeet=camera.WorldToViewportPoint(hero.transform.position);
            Assert.Greater(heroFeet.y,BoxTop-.02f,$"the hero stands behind the dialogue box ({heroFeet})");
            Assert.Less(camera.WorldToViewportPoint(hero.transform.position+Vector3.up*1.9f).y,1,"the hero is off the top of the screen");
            DialogueSystem.Current.Cancel();deadline=Time.unscaledTime+3;while(DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            GameServices.Current.Input.Blocked=false;
            yield return new WaitForSecondsRealtime(6);
            Assert.AreEqual(rig.profile.distance,rig.GroundCameraDistance,.75f,"the camera stays drawn back after the talk");
            Assert.IsTrue(Inside(camera.WorldToViewportPoint(hero.transform.position),.42f,.42f,.58f,.58f),"the view stays slid off the hero after the talk");
            // Standing where the fixed framing already shows them both, nothing swings about for a one-line talk.
            at=npc.transform.position-away*2.5f;Put(hero,Floor(at.x,at.z,npc.transform.position.y));Put(partner,Floor(at.x+2,at.z,npc.transform.position.y));
            yield return new WaitForSecondsRealtime(3);
            var before=camera.transform.position;
            npc.Interact();deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.Less(Vector3.Distance(before,camera.transform.position),.75f,"the camera swings about for a talk with someone already in view");
            DialogueSystem.Current.Cancel();
        }

        // ------------------------------------------------------------------ flight enemies read on a dark sky
        struct Patch{public float warmth,light;public int pixels;}
        static Patch Read(Color32[] with,Color32[] without,bool background)
        {
            float warm=0,light=0;int count=0;
            for(int i=0;i<with.Length;i++)
            {
                if(Change(with[i],without[i])<.04f)continue;
                var c=background?without[i]:with[i];
                warm+=(c.r-c.b)/255f;light+=(.3f*c.r+.59f*c.g+.11f*c.b)/255f;count++;
            }
            return new Patch{warmth=warm/Mathf.Max(1,count),light=light/Mathf.Max(1,count),pixels=count};
        }
        [UnityTest] public IEnumerator FlightEnemiesReadAgainstTheirOwnSky()
        {
            NeedsGraphics();
            foreach(var (zone,ids) in new[]{("WickReach",new[]{"MusterSkiff","Shellmine"}),("Gullet_Tunnel",new[]{"ChoristerDart"})})
            {
                yield return Zone(zone);
                var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
                Put(partner,hero.transform.position+Vector3.back*9+Vector3.right*5);NoShadows(hero);
                yield return new WaitForSecondsRealtime(1);
                var camera=Camera.main;
                foreach(var id in ids)
                {
                    var enemy=Enemy(id,hero.transform.position+Vector3.forward*6);yield return new WaitForSecondsRealtime(.6f);NoShadows(enemy);yield return null;
                    var with=Shot(camera);Hide(enemy,true);var without=Shot(camera);Hide(enemy,false);
                    var body=Read(with,without,false);var sky=Read(with,without,true);
                    Hide(hero,true);var noHero=Shot(camera);Hide(hero,false);var ship=Read(with,noHero,false);
                    TestContext.WriteLine($"FLIGHT_READ {zone} {id} warm={body.warmth:0.00} sky={sky.warmth:0.00} light={body.light:0.00} skylight={sky.light:0.00} ship={ship.light:0.00} px={body.pixels}");
                    Assert.Greater(body.pixels,150,$"fixture: {id} is on screen");
                    Assert.GreaterOrEqual(body.warmth-sky.warmth,.12f,$"{id} in {zone} is the same cold colour as what's behind it (warmth {body.warmth:0.00} against {sky.warmth:0.00}): the enemy can't be told from the sky or the veins");
                    Assert.GreaterOrEqual(body.light-sky.light,.1f,$"{id} in {zone} is a dark shape on a dark sky ({body.light:0.00} against {sky.light:0.00})");
                    Assert.Greater(ship.light,body.light,$"{id} ({body.light:0.00}) outshines the hero's ship ({ship.light:0.00}): the heroes have to stay the brightest clean shapes on screen");
                    float length=Mathf.Max(BodyOf(enemy).size.x,BodyOf(enemy).size.z);
                    float wanted=id=="ChoristerDart"?2.2f:id=="Shellmine"?1.5f:0;
                    Assert.GreaterOrEqual(length,wanted,$"a {id} is {length:0.0} m across, a sliver beside the ship it hunts");
                    Object.Destroy(enemy.gameObject);yield return Frames(2);
                }
            }
        }
        [UnityTest] public IEnumerator EveryFlightEnemyHasAHostileRim()
        {
            yield return Zone("Arena_Flight");
            foreach(var id in new[]{"ChoristerDart","ChoristerDrifter","MusterSkiff","Shellmine"})
            {
                var enemy=Enemy(id,new Vector3(0,-200,0));yield return null;
                var material=enemy.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer).sharedMaterial;
                var rim=material.GetColor("_RimColor");
                Assert.Greater(rim.r,rim.b*2,$"{id}'s rim is the same cold grey as everything else's: nothing on its body says hostile from above");
                Object.Destroy(enemy.gameObject);yield return null;
            }
        }

        [UnityTest] public IEnumerator AGrazerIsNotTheSameBlackAsTheHeroItBites()
        {
            yield return Zone("Arena_Ground");Arena();
            var grazer=Enemy("Gloomgrazer",new Vector3(200,.1f,0));yield return null;
            var material=grazer.GetComponentsInChildren<Renderer>().First(r=>r is MeshRenderer or SkinnedMeshRenderer).sharedMaterial;
            var rim=material.GetColor("_RimColor");
            Assert.Greater(rim.b,rim.g*1.5f,$"a grazer's rim is the same dull grey as everything else's ({rim}): in a clinch Taren's black and gold merges with its black into one dark lump");
            Assert.Greater(rim.b,.6f,$"a grazer's rim is too dim to give it an edge of its own ({rim})");
        }

        // ------------------------------------------------------------------ the party's shots say whose they are
        [UnityTest] public IEnumerator ThePartysShotsSayWhoseTheyAre()
        {
            yield return Zone("Arena_Ground");Arena();
            var sela=Sela;Put(sela,new Vector3(200,.1f,0));Put(Taren,new Vector3(260,.1f,0));
            Projectile.Fire(sela.transform.position+Vector3.up,Vector3.forward,sela.Packet(1,DamageType.Beam,0));
            var shot=Projectile.Live.First(s=>!s.Threatens(true));yield return null;
            Assert.IsTrue(shot.GetComponent<TrailRenderer>().enabled,"Sela's shot is a plain ball with no trail, hanging in the frame: nothing says it's hers or where it's going");
            var glow=shot.GetComponent<MeshRenderer>().sharedMaterial.GetColor("_EmissionColor");
            Assert.Less(glow.maxColorComponent,2.5f,$"Sela's shot burns out to plain white under the grade ({glow}): it has no colour of its own");
            Assert.Less(glow.r,glow.b*.5f,$"Sela's shot isn't the party's cold colour ({glow})");
        }

        // ------------------------------------------------------------------ ground enemies don't pile up
        [UnityTest] public IEnumerator AwakeEnemiesSpreadOutInsteadOfHeapingUp()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            Put(hero,new Vector3(200,.1f,8));Put(partner,new Vector3(260,.1f,0));
            var mites=new List<EnemyBrain>();for(int i=0;i<5;i++)mites.Add(Enemy("Scrapmite",new Vector3(199+i*.5f,.1f,0)));
            yield return Frames(3);foreach(var mite in mites)mite.Passive=false;
            // They arrive in about two seconds and stand through their first tell and its strike before they can spread: this reads the pack that has had time to settle.
            yield return new WaitForSecondsRealtime(5.5f);
            var spacing=new List<float>();
            for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;)
            {
                float least=float.PositiveInfinity;
                for(int a=0;a<mites.Count;a++)for(int b=a+1;b<mites.Count;b++)least=Mathf.Min(least,Vector3.Distance(Flat(mites[a].transform.position),Flat(mites[b].transform.position)));
                spacing.Add(least);yield return null;
            }
            float typical=Percentile(spacing,.5f);float hugging=mites.Min(m=>Vector3.Distance(Flat(m.transform.position),Flat(hero.transform.position)));
            TestContext.WriteLine($"SPACING typical={typical:0.00} hero={hugging:0.00}");
            Assert.GreaterOrEqual(typical,1.35f,$"five Scrapmites 1.5 m across stand {typical:0.00} m apart: one brown heap, and you can't count them");
            Assert.GreaterOrEqual(hugging,1.2f,$"a Scrapmite stands {hugging:0.00} m from the hero: he's half inside it");
            foreach(var mite in mites)Object.Destroy(mite.gameObject);yield return Frames(2);
            // A corridor that fits one abreast still lets them through: the shove never holds anyone off.
            Wall(new Vector3(198.6f,1,10),new Vector3(.4f,3,24));Wall(new Vector3(201.4f,1,10),new Vector3(.4f,3,24));
            Put(hero,new Vector3(200,.1f,18));mites.Clear();for(int i=0;i<3;i++)mites.Add(Enemy("Scrapmite",new Vector3(200,.1f,i*1.4f)));
            yield return Frames(3);foreach(var mite in mites)mite.Passive=false;
            yield return new WaitForSecondsRealtime(6);
            Assert.Less(mites.Min(m=>Vector3.Distance(Flat(m.transform.position),Flat(hero.transform.position))),2.6f,"keeping apart traps a pack in a corridor: none of them reaches the hero");
        }

        // ------------------------------------------------------------------ the Burrower is a machine
        [UnityTest] public IEnumerator TheBurrowerStandsOffItsOwnFloor()
        {
            NeedsGraphics();
            foreach(var flag in new[]{"met.Survivor"})Flag(flag);
            yield return Zone("Sorrel_Ridges");
            var hero=PartyController.Current.Active;var partner=PartyController.Current.members.First(m=>m!=hero);
            Put(hero,Floor(-10,168));Put(partner,Floor(-13,168));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var enemy=Enemy("Burrower",Floor(-3,168));yield return new WaitForSecondsRealtime(.5f);NoShadows(enemy);yield return null;
            var with=Shot(camera);Hide(enemy,true);var without=Shot(camera);Hide(enemy,false);
            var body=Read(with,without,false);var floor=Read(with,without,true);
            TestContext.WriteLine($"BURROWER_READ warm={body.warmth:0.00} floor={floor.warmth:0.00} px={body.pixels}");
            Assert.Greater(body.pixels,2000,"fixture: the rig is on screen");
            Assert.LessOrEqual(body.warmth,floor.warmth-.12f,$"the Burrower is the colour of the floor it stands on (warmth {body.warmth:0.00} against {floor.warmth:0.00}): a tan grub, not a drill rig");
        }

        // ------------------------------------------------------------------ the retune
        [UnityTest] public IEnumerator EveryBossGivesAFightThatLasts()
        {
            yield return null;
            // Integrity per second the replay party took off each boss in the November captures: the Burrower fell in 80 s, the Bellows in about 73, the Cantor in 89, Hessa in about 102.
            foreach(var (id,rate) in new[]{("Burrower",300f),("BellowsBelow",218f),("Cantor",382f),("HessaKorl",430f)})
            {
                float seconds=GameCatalog.Find<EnemyDef>(id).integrity/rate;
                Assert.GreaterOrEqual(seconds,100,$"{id} falls in {seconds:0} s to a driver that dodges perfectly: the bar is 2 to 5 minutes");
            }
            static string Source(string path)=>File.ReadAllText(Path.Combine(Application.dataPath,"_Project/Scripts/Editor/"+path));
            void Written(string id,string path,string pattern)
            {
                var found=Regex.Match(Source(path),pattern,RegexOptions.Singleline);Assert.IsTrue(found.Success,"fixture: "+path+" writes "+id);
                Assert.AreEqual(GameCatalog.Find<EnemyDef>(id).integrity,float.Parse(found.Groups[1].Value),.01f,$"a rebuild would put {id}'s old integrity back");
            }
            Written("Burrower","ArenaBuilder.cs",@"e\.integrity=e\.boss\?\(n==3\?(\d+):\d+\)");Written("Cantor","ArenaBuilder.cs",@"e\.integrity=e\.boss\?\(n==3\?\d+:(\d+)\)");
            Written("BellowsBelow","World/HushwellBuilder.cs",@"e\.id=""BellowsBelow"".*?e\.integrity=(\d+)");Written("HessaKorl","World/LanternRoadBuilder.cs",@"e\.id=""HessaKorl"".*?e\.integrity=(\d+)");
        }

        // ------------------------------------------------------------------ she has given up
        [UnityTest] public IEnumerator HessaDoesNotStandToAttentionAfterSheYields()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay",false);
            var duel=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(e=>e.encounterId=="Relay_Hessa");
            var person=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(n=>n.speaker=="Hessa");
            duel.Begin();yield return null;
            var boss=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Single(e=>e.definition.id=="HessaKorl");boss.Passive=true;
            DialogueSystem.Current.AutoAdvance=true;
            Fell(boss);
            for(float end=Time.unscaledTime+6;!person.gameObject.activeInHierarchy&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(person.gameObject.activeInHierarchy,"fixture: she takes over where she fell");
            yield return new WaitForSecondsRealtime(.3f);
            var rig=person.GetComponentInChildren<Animator>();
            Assert.IsFalse(rig.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"she has just surrendered and stands bolt upright, idling like someone who won");
            Assert.AreEqual(0,rig.speed,.001f,"her beaten pose doesn't hold: she goes on moving through it");
            // Down off her feet, not a flinch held on them: she stood mid-stride with her arms out, and nothing showed she had given up.
            Vector3 Bone(HumanBodyBones bone)=>rig.GetBoneTransform(bone).position;
            float legs=Vector3.Distance(Bone(HumanBodyBones.LeftUpperLeg),Bone(HumanBodyBones.LeftLowerLeg))+Vector3.Distance(Bone(HumanBodyBones.LeftLowerLeg),Bone(HumanBodyBones.LeftFoot));
            Assert.Less(Bone(HumanBodyBones.Hips).y-Bone(HumanBodyBones.LeftFoot).y,legs*.7f,"she yields standing: her hips are still up on straight legs");
        }

        // ------------------------------------------------------------------ the white hairline
        [UnityTest] public IEnumerator SelasBeamEndsAtWhatItStruck()
        {
            yield return Zone("Arena_Ground");Arena();
            var sela=Sela;Put(sela,new Vector3(200,.1f,0));Put(Taren,new Vector3(200,.1f,9));
            var victim=Enemy("Ridgehound",new Vector3(200,.1f,4));victim.Health.maximum=victim.Health.integrity=100000;yield return Frames(3);
            var beam=sela.gameObject.AddComponent<EmitterBeam>();var origin=sela.transform.position+Vector3.up;
            beam.Fire(origin,Vector3.forward,sela.Packet(1,DamageType.Beam,0),13);yield return null;
            var core=sela.GetComponentsInChildren<LineRenderer>(true).First(l=>l.name=="Emitter core");
            float reach=Vector3.Distance(origin,core.GetPosition(1));
            Assert.Less(reach,6.5f,$"a Thread Lance into an enemy 4 m away runs on {reach:0} m: a thin white line through the boss to the hero standing behind her, which looks like an attack on him");
            Assert.GreaterOrEqual(core.startWidth,.05f,$"at {core.startWidth:0.000} m the beam's core is a hairline with no width or colour");
            // Point blank the body is struck at no distance at all, and the beam still ends there.
            var body=victim.GetComponent<CharacterController>();body.enabled=false;victim.transform.position=new Vector3(200,.1f,.2f);body.enabled=true;Physics.SyncTransforms();
            beam.Fire(origin,Vector3.forward,sela.Packet(1,DamageType.Beam,0),13);yield return null;
            Assert.Less(Vector3.Distance(origin,core.GetPosition(1)),2,"a Thread Lance fired point blank still runs its full 13 m through the body to whoever stands behind it");
            Object.Destroy(victim.gameObject);yield return Frames(2);
            beam.Fire(origin,Vector3.right,sela.Packet(1,DamageType.Beam,0),13);yield return null;
            Assert.AreEqual(13,Vector3.Distance(origin,core.GetPosition(1)),.1f,"fixture: with nothing to strike the beam runs its full length");
        }
    }
}
