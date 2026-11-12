using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// <summary>November 10, the open marking findings of the QA pass: a comet with nothing marked ahead of it (E2-hushwell-15), markings that land together as a lattice of rims and
    /// crossing hatch (E2-lanternwick-04), a lane open at the end it starts from (E4-sorrel-02), rings that lose the part on the slope (E4-sorrel-03, the hero's Pulse ring with them) and the
    /// Cantor's song in purple on a purple floor (E4-gullet-06). Each was red on the tree it was written against; the comment on each says why. The pixel checks need the -Graphics runner.</summary>
    public sealed class QaNov10MarkingTests
    {
        const int ShotWidth=1920,ShotHeight=1080;
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
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+20;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None))volume.enabled=false;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;actor.Health.InvulnerableUntil=GameTime.Now+900;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.5f);
        }
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static void Put(CombatActor hero,Vector3 at)
        {
            var body=hero.GetComponent<CharacterController>();body.enabled=false;
            if(hero.TryGetComponent<FlightMotor>(out var ship))ship.Halt();
            hero.transform.position=at;body.enabled=true;hero.target=null;hero.TargetLocked=false;Physics.SyncTransforms();
        }
        static void Arena()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Fight fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        static EnemyBrain Enemy(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        static void NoSpecials(EnemyBrain enemy)
        {if(enemy.TryGetComponent<BossController>(out var boss))typeof(BossController).GetField("nextSpecial",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(boss,Time.time+900);}
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 Floor(float x,float z,float level=0)=>CombatCover.Ground(new Vector3(x,level+4,z));
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
        static float Change(Color32 a,Color32 b)=>(Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b))/(3*255f);
        static float ChangeAt(Color32[] a,Color32[] b,Camera camera,Vector3 world)
        {
            var view=camera.WorldToViewportPoint(world);if(view.z<=0||view.x<0||view.x>=1||view.y<0||view.y>=1)return 0;
            int index=(int)(view.y*ShotHeight)*ShotWidth+(int)(view.x*ShotWidth);return Change(a[index],b[index]);
        }
        /// <summary>Whether a point stands well inside the frame. Off it, ChangeAt reads 0, which is no measurement: the far ends of a lane across the view counted as a rim that wasn't there.</summary>
        static bool Seen(Camera camera,Vector3 world){var view=camera.WorldToViewportPoint(world);return view.z>0&&view.x>.03f&&view.x<.97f&&view.y>.03f&&view.y<.97f;}
        static float Median(List<float> values){values.Sort();return values.Count==0?0:values[values.Count/2];}
        static bool ReadsAsFire(Color c){Color.RGBToHSV(c,out float hue,out float saturation,out _);hue*=360;return saturation>=.4f&&hue<=50;}
        /// <summary>The ground's real surface over a point: a hill, not a prop standing on it.</summary>
        static bool Ground(Vector3 point,out float height)
        {
            height=float.NegativeInfinity;bool any=false;
            foreach(var hit in Physics.RaycastAll(new Vector3(point.x,80,point.z),Vector3.down,120,~0,QueryTriggerInteraction.Ignore))
                if(hit.collider is MeshCollider&&hit.point.y>height){height=hit.point.y;any=true;}
            return any;
        }

        // ------------------------------------------------------------------ E2-hushwell-15: a comet is marked ahead of it all the way
        /// <summary>Red before: the spit's lane was released as the comet left and gone a third of a second later, so for the rest of its 1.2 s flight nothing marked where it was going.</summary>
        [UnityTest] public IEnumerator ACometIsMarkedAheadOfItAllTheWayToWhereItEnds()
        {
            yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,10));Put(Partner,new Vector3(260,.1f,0));
            var bellows=Enemy("BellowsBelow",new Vector3(200,.1f,0));yield return Frames(3);NoSpecials(bellows);
            bellows.Passive=false;
            for(float end=Time.unscaledTime+4;!bellows.Telegraphing&&Time.unscaledTime<end;)yield return null;
            Assert.IsTrue(bellows.Telegraphing,"fixture: the Bellows spits at the hero 10 m off");
            for(float end=Time.unscaledTime+2;bellows.Telegraphing&&Time.unscaledTime<end;)yield return null;
            bellows.Passive=true;
            int flying=0,bare=0;
            for(float end=Time.unscaledTime+2;Time.unscaledTime<end;)
            {
                var shots=Projectile.Live.Where(s=>s!=null&&s.Threatens(true)).ToList();if(shots.Count==0&&flying>0)break;
                foreach(var shot in shots)
                {
                    flying++;
                    bool marked=Telegraph.Live.Any(t=>t!=null&&t.owner==bellows&&!t.Released&&t.Contains(shot.transform.position));
                    if(!marked)bare++;
                }
                yield return null;
            }
            TestContext.WriteLine($"COMET_MARKED frames={flying} bare={bare}");
            Assert.Greater(flying,5,"fixture: the comet flies");
            Assert.AreEqual(0,bare,$"for {bare} of {flying} frames the Bellows' comet flies on with nothing marked on the floor ahead of it: a hot ball heading for the hero that reads as Sela's shot in another colour");
            yield return new WaitForSecondsRealtime(.35f);
            Assert.AreEqual(0,Telegraph.Live.Count(t=>t!=null&&t.owner==bellows),"the comet has landed and its lane is still painted");
        }

        // ------------------------------------------------------------------ E2-lanternwick-04: markings landing together are one shape
        /// <summary>Red before: two crossing lanes stacked their washes and hatch (the overlap nearly twice as strong as either), and the second lane's rims ran straight across the first one's
        /// inside, the lattice with bars over the hero.</summary>
        [UnityTest] public IEnumerator CrossingLanesLayTheirAreaOnceAndShareOneOutline()
        {
            NeedsGraphics();yield return Zone("Arena_Ground");Arena();
            var hero=Hero;Put(hero,new Vector3(200,.1f,0));Put(Partner,new Vector3(260,.1f,0));yield return new WaitForSecondsRealtime(1.5f);
            var camera=Camera.main;var right=Flat(camera.transform.right).normalized;var ahead=Flat(camera.transform.forward).normalized;
            var center=new Vector3(200,0,0)+right*4;
            // Read where the paint is drawn, lifted off the floor (Telegraph's Lift), not on the floor under it: the rim is a few pixels wide.
            var paint=Vector3.up*.06f;
            var dirA=Quaternion.Euler(0,30,0)*ahead;var dirB=Quaternion.Euler(0,-30,0)*ahead;const float Width=2.4f,Length=10;
            var a=Telegraph.Line(center-dirA*Length*.5f,dirA,Length,Width,.01f,Telegraph.Threat);var b=Telegraph.Line(center-dirB*Length*.5f,dirB,Length,Width,.01f,Telegraph.Threat);
            yield return new WaitForSecondsRealtime(.25f);
            var viewA=a.GetComponent<MeshRenderer>();var viewB=b.GetComponent<MeshRenderer>();
            viewA.enabled=viewB.enabled=false;var none=Shot(camera);
            viewA.enabled=true;var onlyA=Shot(camera);
            viewA.enabled=false;viewB.enabled=true;var onlyB=Shot(camera);
            viewA.enabled=true;var both=Shot(camera);
            // Deep inside both: the area once, not twice.
            float sumBoth=0,sumA=0,sumB=0;int n=0;
            for(float x=-.5f;x<=.5f;x+=.05f)for(float z=-.5f;z<=.5f;z+=.05f)
            {var at=center+new Vector3(x,0,z)+paint;if(!Seen(camera,at))continue;sumBoth+=ChangeAt(both,none,camera,at);sumA+=ChangeAt(onlyA,none,camera,at);sumB+=ChangeAt(onlyB,none,camera,at);n++;}
            Assert.Greater(n,100,"fixture: the lanes cross on screen");
            float stacked=sumBoth/n,single=Mathf.Max(sumA,sumB)/n;
            // B's rims: where they cross A's inside, and well clear of A.
            var acrossA=Vector3.Cross(Vector3.up,dirA);var acrossB=Vector3.Cross(Vector3.up,dirB);
            var inside=new List<float>();var outside=new List<float>();
            foreach(float side in new[]{-1f,1f})for(float t=.3f;t<=Length-.3f;t+=.05f)
            {
                var at=center-dirB*Length*.5f+dirB*t+acrossB*(side*(Width*.5f-.03f));
                float offA=Mathf.Abs(Vector3.Dot(at-center,acrossA)),alongA=Vector3.Dot(at-(center-dirA*Length*.5f),dirA);
                at+=paint;if(!Seen(camera,at))continue;
                if(offA<Width*.5f-.5f&&alongA>.5f&&alongA<Length-.5f)inside.Add(ChangeAt(both,onlyA,camera,at));
                else if(offA>Width*.5f+1)outside.Add(ChangeAt(onlyB,none,camera,at));
            }
            Object.Destroy(a.gameObject);Object.Destroy(b.gameObject);
            float across=Median(inside),own=Median(outside);
            TestContext.WriteLine($"LATTICE stacked={stacked:0.000} single={single:0.000} rimInside={across:0.000} rimOutside={own:0.000} samples={inside.Count}/{outside.Count}");
            Assert.Greater(inside.Count,8,"fixture: B's rims cross A's inside, on screen");Assert.Greater(outside.Count,8,"fixture: B's rims run clear of A, on screen");
            Assert.GreaterOrEqual(own,.12f,$"a lane's rim clear of everything else changes the floor by only {own:0.00}: the outline is gone");
            Assert.LessOrEqual(stacked,single*1.2f+.004f,$"where two lanes overlap the floor shifts by {stacked:0.000} against {single:0.000} for one: their areas stack into a brighter lattice");
            Assert.LessOrEqual(across,.08f,$"one lane's rim crosses the other's inside at {across:0.00}: a bar across the shape the two make, the lattice over the hero");
        }

        // ------------------------------------------------------------------ E4-sorrel-02: a lane is closed where it starts
        /// <summary>Red before: a lane had a rim on its sides and its far end only, so a charge's lane from the Burrower ended under it on a straight cut across the hatch.</summary>
        [UnityTest] public IEnumerator ALaneIsClosedAtTheEndItStartsFrom()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            var lane=Telegraph.Line(new Vector3(200,0,0),Vector3.forward,12,3.4f,5,Telegraph.Heavy);yield return null;
            var block=new MaterialPropertyBlock();lane.GetComponent<MeshRenderer>().GetPropertyBlock(block);
            Assert.AreEqual(1,block.GetFloat("_InnerEdge"),.01f,"a lane has no rim across the end it starts from: the Burrower's charge ends under it in a straight cut across the hatch, as if the floor had sliced it");
            Object.Destroy(lane.gameObject);
        }

        // ------------------------------------------------------------------ E4-sorrel-03: where the ground cuts a ring, the ring closes there
        /// <summary>Red before: past a wall's foot a ring's vertices were laid flat under the hill and its rim with them, so nothing closed the shape on the slope side. Now each vertex
        /// knows its distance from where the floor ends (uv1.z, negative past it) and the paint and rim stop there, on floor the player can see.</summary>
        [UnityTest] public IEnumerator ARingByTheWallsFootClosesWhereTheFloorEnds()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var (centre,radius) in new[]{(Floor(16,168),7f),(Floor(13,170),5f)})
            {
                var ring=Telegraph.Circle(centre,radius,5,Telegraph.Threat);yield return null;
                var mesh=ring.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var cut=new List<Vector3>();mesh.GetUVs(1,cut);var triangles=mesh.GetTriangles(0);
                int crossings=0;float buried=0;
                for(int t=0;t<triangles.Length;t+=3)for(int e=0;e<3;e++)
                {
                    int i=triangles[t+e],j=triangles[t+(e+1)%3];float zi=cut[i].z,zj=cut[j].z;
                    if(zi>=0==zj>=0)continue;
                    var at=Vector3.Lerp(vertices[i],vertices[j],zi/(zi-zj));crossings++;
                    Assert.IsTrue(Ground(at,out float top),"fixture: ground under the ring");buried=Mathf.Max(buried,top-at.y);
                }
                Object.Destroy(ring.gameObject);yield return null;
                TestContext.WriteLine($"RING_CUT r={radius} crossings={crossings} buried={buried:0.00}");
                Assert.Greater(crossings,0,$"a {radius} m ring by Sorrel's wall has no edge where the floor ends: its slope side lies flat under the hill, with no rim to close it");
                Assert.LessOrEqual(buried,.45f,$"a {radius} m ring's edge at the wall lies {buried:0.00} m under the hill: its rim can't be seen there");
            }
        }
        /// <summary>Red before: the Pulse ring was one flat plane at the hero's feet, so any rise inside its reach hid it, and by the slope beside the Burrower about half of it was gone.</summary>
        [UnityTest] public IEnumerator TheHerosPulseRingLiesOnTheSlopeItCrosses()
        {
            yield return Zone("Arena_Ground");Arena();yield return null;
            var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.transform.SetPositionAndRotation(new Vector3(200,12,0),Quaternion.Euler(-25,0,0));ramp.transform.localScale=new Vector3(40,1,60);Physics.SyncTransforms();
            Assert.IsTrue(Physics.Raycast(new Vector3(200,40,0),Vector3.down,out var ground,60),"fixture: the ramp");
            float Slope(float x,float z)=>Physics.Raycast(new Vector3(x,60,z),Vector3.down,out var hit,90)?hit.point.y:float.NaN;
            var hero=Hero;Put(hero,new Vector3(240,.1f,0));
            var wave=PulseWave.Release(ground.point,hero.Packet(1,DamageType.Pulse,0));
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(wave!=null,"fixture: the wave is still out");
            var ring=wave.GetComponent<GroundRing>();var surface=ring.GetComponent<MeshFilter>().sharedMesh;float worst=0;int read=0;
            foreach(var local in surface.vertices)
            {
                var at=ring.transform.TransformPoint(local);if(Flat(at-ground.point).magnitude<.5f)continue;
                float y=Slope(at.x,at.z);if(float.IsNaN(y))continue;read++;worst=Mathf.Max(worst,Mathf.Abs(at.y-y-.12f));
            }
            TestContext.WriteLine($"PULSE_SLOPE worst={worst:0.00} read={read}");
            Assert.Greater(read,20,"fixture: the ring has spread");
            Assert.LessOrEqual(worst,.3f,$"a Pulse ring on a 25 degree slope is {worst:0.00} m off the ground: it lies on one flat plane, under the hill on the uphill side");
            Object.Destroy(ramp);
        }

        // ------------------------------------------------------------------ E4-gullet-06: the Cantor's song reads on its purple floor
        /// <summary>Red before: the song's 14 m ring was the Stagger purple, a thin rim and a faint wash on the Gullet's purple floor, the one mark in that stretch you had to hunt for.</summary>
        [UnityTest] public IEnumerator TheCantorsSongIsMarkedInAColourItsFloorDoesntShare()
        {
            yield return Zone("Arena_Flight");
            Put(Hero,new Vector3(300,1,0));Put(Partner,new Vector3(310,1,0));
            var cantor=Enemy("Cantor",new Vector3(200,1,0));yield return Frames(3);NoSpecials(cantor);var boss=cantor.GetComponent<BossController>();
            cantor.Health.integrity=cantor.Health.maximum*.45f;yield return Frames(3);
            Assert.GreaterOrEqual(boss.Phase,2,"fixture: the song comes from phase 2");
            typeof(BossController).GetField("cantorSpecials",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(boss,1);
            Assert.IsTrue(boss.Perform("Choir"),"fixture: the Choir");yield return Frames(2);
            var song=Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&t.Shape==Telegraph.Kind.Circle&&t.Radius>=10);
            Assert.IsNotNull(song,"fixture: the song marks its ring");
            var block=new MaterialPropertyBlock();song.GetComponent<MeshRenderer>().GetPropertyBlock(block);var colour=block.GetColor("_BaseColor");
            Assert.IsTrue(ReadsAsFire(colour),$"the Cantor's song is marked in {colour}, the purple of the Gullet's own floor: the one mark in the stretch you have to hunt for");
        }
    }
}
