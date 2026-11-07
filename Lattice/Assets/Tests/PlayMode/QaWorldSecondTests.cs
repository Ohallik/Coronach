using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The second eye pass on the places themselves. A person looked at a full playthrough again, after two big fixes: names printed on signs, a stack of stone over a fight, a curtain with a
    /// razor edge, a partner standing behind the hero, black crates, a town with nobody in it. These pin what they saw to its cause. Whether it now looks finished is for eyes on the next capture.</summary>
    public sealed class QaWorldSecondTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static bool Has(string key)=>GameServices.Current.Flags.GetBool(key);
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            // The runner's own window isn't 16:9: what's read through the viewport has to be the view a player has.
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            if(PartyController.Current!=null)foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker);
        static void ClearFights(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        static CameraProfile View=>ZoneController.Current.definition.cameraProfile;
        static Bounds BoundsOf(Component root)
        {
            var parts=root.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer or SkinnedMeshRenderer).ToArray();
            if(parts.Length==0)return new Bounds(root.transform.position,Vector3.zero);
            var box=parts[0].bounds;foreach(var part in parts)box.Encapsulate(part.bounds);return box;
        }
        static bool Inside(Vector3 view)=>view.z>0&&view.x>.01f&&view.x<.99f&&view.y>.01f&&view.y<.99f;

        // ---------------------------------------------------------------- what a floor sign is
        static TextMeshPro[] FloorSigns()
        {
            var people=new HashSet<string>(All<Npc>().Select(n=>DisplayNames.Speaker(n.speaker)));var warps=All<WarpBeacon>();
            return All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&t.text.Length>0&&!people.Contains(t.text)&&t.GetComponentInParent<Npc>(true)==null&&t.gameObject.activeInHierarchy
                &&!t.text.StartsWith("Warp")&&!warps.Any(w=>w.prompt.StartsWith(t.text))&&t.transform.parent==null).ToArray();
        }
        /// <summary>A gate's sign lies on the floor too, in the chapter's zones: everything but a warp's, which hangs.</summary>
        static TextMeshPro[] FloorAndGateSigns()
        {
            var people=new HashSet<string>(All<Npc>().Select(n=>DisplayNames.Speaker(n.speaker)));
            return All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&t.text.Length>0&&!people.Contains(t.text)&&t.GetComponentInParent<Npc>(true)==null&&t.gameObject.activeInHierarchy
                &&!t.text.StartsWith("Warp")&&t.transform.parent==null&&t.transform.position.y<6).ToArray();
        }
        static IEnumerable<Vector3> Paint(TextMeshPro sign)
        {
            var size=sign.GetPreferredValues(sign.text);float w=size.x+.3f,h=size.y;
            for(int i=0;i<=8;i++)for(int j=0;j<=2;j++)yield return sign.transform.TransformPoint(new Vector3((i/8f-.5f)*w,(j/2f-.5f)*h,0));
        }
        static Rect OnScreen(IEnumerable<Vector3> points,Vector3 right,Vector3 up)
        {
            float xMin=1e6f,xMax=-1e6f,yMin=1e6f,yMax=-1e6f;
            foreach(var p in points){float x=Vector3.Dot(p,right),y=Vector3.Dot(p,up);xMin=Mathf.Min(xMin,x);xMax=Mathf.Max(xMax,x);yMin=Mathf.Min(yMin,y);yMax=Mathf.Max(yMax,y);}
            return Rect.MinMaxRect(xMin,yMin,xMax,yMax);
        }

        // ---------------------------------------------------------------- item 1: names and signs
        /// <summary>From the fixed camera, at their post, a person's name doesn't land on a floor sign, in any zone with both. A sign's letters are about as tall as a name's, so where the two cross neither reads.
        /// Measured as a marked name, gold with its mark and at its larger size, over a head of either height the kit's bodies have.</summary>
        [UnityTest] public IEnumerator NoNameIsPrintedOnAFloorSign()
        {
            var failures=new List<string>();int read=0;
            foreach(var zone in new[]{"Hub_Decks","Sorrel_Ridges","Hushwell","Lanternwick","LanternGorge","SunkenRelay","TallowDrift"})
            {
                // Everyone who can be there: the captain once she's yielded, Pell once she's aboard.
                if(zone=="SunkenRelay")foreach(var flag in new[]{"relay.hessaConfronted","bossdown.HessaKorl","clear.Relay_Hessa","relay.hessaYielded"})Flag(flag);
                if(zone=="TallowDrift")Flag("lanternwick.chartGiven");
                yield return Zone(zone);yield return null;yield return null;
                var rotation=Quaternion.Euler(View.pitch,View.yaw,0);var right=rotation*Vector3.right;var up=rotation*Vector3.up;
                var signs=FloorAndGateSigns().Select(s=>(sign:s,rect:OnScreen(Paint(s),right,up))).ToArray();
                float marked=UiKit.WorldFontSize(32);
                foreach(var npc in All<Npc>().Where(n=>n.gameObject.activeInHierarchy))
                {
                    var label=npc.GetComponentsInChildren<TextMeshPro>().FirstOrDefault();if(label==null)continue;
                    read++;float wide=label.GetPreferredValues("!  "+label.text).x*marked/label.fontSize,tall=marked*.13f;var at=npc.transform.position;
                    var ends=new List<Vector3>();
                    foreach(float head in new[]{1.75f,2.2f}){var over=new Vector3(at.x,at.y+head+.32f,at.z);foreach(int side in new[]{-1,1})foreach(int high in new[]{-1,1})ends.Add(over+right*side*wide*.5f+up*high*tall*.5f);}
                    var name=OnScreen(ends,right,up);
                    foreach(var (sign,rect) in signs)if(name.Overlaps(rect))failures.Add($"{zone}: \"{label.text}\" lies across \"{sign.text}\" from the fixed camera, so neither can be read");
                }
            }
            Assert.Greater(read,8,"fixture: the zones have their people");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        [UnityTest] public IEnumerator TallowsSignsStandWhereTheirNamesAreTrue()
        {
            yield return Zone("TallowDrift");
            TextMeshPro Sign(string words)=>All<TextMeshPro>().Single(t=>t.text==words);
            float Flat(Vector3 a,Vector3 b){a.y=b.y;return Vector3.Distance(a,b);}
            var keeper=Person("Keeper");
            Assert.Less(Flat(Sign("REFUGE / KEEPER").transform.position,keeper.transform.position),6,"REFUGE / KEEPER lies on empty deck across the room from the Keeper");
            var hatches=All<Transform>().Where(t=>t.name.StartsWith("Private rest hatch")).ToArray();Assert.IsNotEmpty(hatches,"fixture: the rest cabins have doors");
            Assert.Less(hatches.Min(h=>Flat(h.position,Sign("SERVICE / REST CABINS").transform.position)),3,"SERVICE / REST CABINS isn't at a cabin's door");
            var launch=All<DockingPad>().Single(d=>d.scene=="TallowApproach");
            // It stands on the lift's own lintel now, a metre from the launch spot and three over it: what this asks is that nobody standing there thins it out, and that's what it says.
            var lift=Sign("PRESSURE LIFT");
            Assert.Greater(lift.transform.position.y,3,"PRESSURE LIFT lies under the feet of everyone standing on the launch spot: it only ever shows faded to a ghost");
            Assert.IsTrue(lift.GetComponent<SignFade>().bossOnly,"PRESSURE LIFT thins out under a hero standing a storey below it: it only ever shows faded to a ghost");
        }

        // ---------------------------------------------------------------- item 2: what stands over a fight
        /// <summary>The rule: from the fixed camera, nothing that stands more than 3 m over the floor and more than 7 m across the screen crosses the line to a chest in the middle of a fight, on the
        /// walked path, or (in the gorge and the relay) anywhere a fight drifts to. A column or a standing tower is narrower than the space between two fighters, and opens a hole for a hero behind it. A gate with
        /// a way through is the arch the party walks under. A lying tower, a rock or a ledge is a wall across the view, which no hole can help.</summary>
        static readonly Vector2[] GorgePath={new(0,-6),new(2,30),new(8,62),new(2,95),new(-8,120),new(0,150),new(8,180),new(4,205),new(1,221)};
        static readonly Vector2 DenEntry=new(-8,92),Den=new(-26,100);
        static float ToSegment(Vector2 p,Vector2 a,Vector2 b){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));return Vector2.Distance(p,a+d*t);}
        static float GorgeDistance(Vector2 p){float d=float.PositiveInfinity;for(int i=1;i<GorgePath.Length;i++)d=Mathf.Min(d,ToSegment(p,GorgePath[i-1],GorgePath[i]));return Mathf.Min(d,ToSegment(p,DenEntry,Den)+3);}
        static bool Slim(Transform part)
        {
            string name=part.root.name;
            return name.Contains("Column")||name.Contains("relay")&&!name.Contains("Fallen")||name.Contains("Lantern tower")||name.StartsWith("Relay light");
        }
        static List<string> Blocked(IEnumerable<Vector3> floor,string where)
        {
            var problems=new List<string>();var rotation=Quaternion.Euler(View.pitch,View.yaw,0);var right=rotation*Vector3.right;
            var parts=All<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.GetComponentInParent<TMP_Text>()==null&&r.GetComponentInParent<CombatActor>()==null&&r.GetComponentInParent<Npc>()==null&&r.GetComponentInParent<EnemyBrain>()==null
                &&!r.name.EndsWith(" floor")&&!r.name.EndsWith(" cliffs")&&!r.name.StartsWith("Floor detail")&&!r.name.StartsWith("Lit chunk")&&!Slim(r.transform)).ToArray();
            var wide=new List<(Bounds box,string name)>();
            foreach(var part in parts)
            {
                var occlusion=part.GetComponentInParent<SceneryOcclusion>();if(occlusion!=null&&occlusion.opening.size.sqrMagnitude>0)continue;
                var box=part.bounds;if(Mathf.Max(box.size.x,box.size.z)>40)continue;
                float across=Mathf.Abs(right.x)*box.size.x+Mathf.Abs(right.z)*box.size.z;
                if(across>=7)wide.Add((box,part.transform.root.name));
            }
            var named=new HashSet<string>();
            foreach(var at in floor)
            {
                var chest=at+Vector3.up*SightlineAudit.Chest;var eye=chest+SightlineAudit.ToCamera*SightlineAudit.Distance;var ray=new Ray(eye,chest-eye);
                foreach(var (box,name) in wide)
                    if(box.max.y-at.y>3&&box.IntersectRay(ray,out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore&&named.Add(name+box.center))
                        problems.Add($"{where}: {name} at ({box.center.x:0},{box.center.z:0}), {box.size.x:0} by {box.size.z:0} m and {box.max.y-at.y:0.0} m over the floor, stands between the camera and ({at.x:0.0},{at.z:0.0})");
            }
            return problems;
        }
        static bool FloorAt(Vector3 near,float reach,out Vector3 floor)
        {
            floor=near;if(!NavMesh.SamplePosition(near,out var hit,reach,NavMesh.AllAreas))return false;
            floor=hit.position;return true;
        }
        [UnityTest] public IEnumerator NothingWideStandsBetweenTheCameraAndAFight()
        {
            var problems=new List<string>();
            foreach(var zone in new[]{"Sorrel_Ridges","Hushwell","SunkenRelay"})
            {
                foreach(var flag in new[]{"relay.hessaConfronted"})Flag(flag);
                yield return Zone(zone);
                var floor=new List<Vector3>();
                foreach(var fight in All<EncounterVolume>())
                {
                    var middle=fight.GetComponent<Collider>().bounds.center;
                    float reach=zone=="SunkenRelay"?6:0;
                    for(float x=-reach;x<=reach;x+=Mathf.Max(3,reach))for(float z=-reach;z<=reach;z+=Mathf.Max(3,reach))
                        if(FloorAt(middle+new Vector3(x,0,z),3,out var at))floor.Add(at);
                }
                Assert.Greater(floor.Count,0,"fixture: "+zone+" has fights");
                problems.AddRange(Blocked(floor,zone));
            }
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator TheGorgesRocksDoNotHangOverItsFloor()
        {
            yield return Zone("LanternGorge");ClearFights();
            var floor=new List<Vector3>();
            void Add(Vector2 p){if(GorgeDistance(p)<12&&Physics.Raycast(new Vector3(p.x,30,p.y),Vector3.down,out var hit,60,~0,QueryTriggerInteraction.Ignore)&&hit.collider is MeshCollider)floor.Add(hit.point);}
            for(int i=1;i<GorgePath.Length;i++)
            {
                Vector2 a=GorgePath[i-1],d=GorgePath[i]-a;float length=d.magnitude;var across=new Vector2(-d.y,d.x)/length;
                for(float along=0;along<=length;along+=2)foreach(float off in new[]{-4f,-2f,0f,2f,4f})Add(a+d*(along/length)+across*off);
            }
            foreach(var fight in All<EncounterVolume>())
            {
                var middle=fight.GetComponent<Collider>().bounds.center;
                for(float x=-10;x<=10;x+=2)for(float z=-7;z<=7;z+=2)Add(new Vector2(middle.x+x,middle.z+z));
            }
            Assert.Greater(floor.Count,300,"fixture: the gorge's floor");
            var problems=Blocked(floor,"LanternGorge");
            string[] rocks={"RidgeRock1","RidgeRock2","RidgeRock3","DressStrataOutcrop","DressStrataLedge","DressBoulders"};int read=0;
            foreach(var rock in All<Transform>().Where(t=>t.parent==null&&rocks.Contains(t.name)))
            {
                // The verge's waist-high boulders stand on the floor on purpose. Past that height it's the walls' rocks.
                var box=BoundsOf(rock);if(box.size.y<1.5f)continue;
                read++;
                if(Mathf.Max(box.size.x,box.size.z)>8.5f)problems.Add($"{rock.name} at ({box.center.x:0},{box.center.z:0}) is {box.size.x:0} by {box.size.z:0} m: the kit fits it by height, and a wall's foot isn't twenty metres deep");
                if(GorgeDistance(new Vector2(box.min.x,box.min.z))<10.5f||GorgeDistance(new Vector2(box.max.x,box.max.z))<10.5f||GorgeDistance(new Vector2(box.min.x,box.max.z))<10.5f||GorgeDistance(new Vector2(box.max.x,box.min.z))<10.5f)
                    problems.Add($"{rock.name} at ({box.center.x:0},{box.center.z:0}) reaches out over the canyon's floor");
            }
            Assert.Greater(read,20,"fixture: the walls have their rocks");
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }
        [UnityTest] public IEnumerator TheTownsStreetIsInView()
        {
            yield return Zone("Lanternwick");
            var street=new List<Vector3>();
            for(float z=-6;z<=88;z+=4)if(FloorAt(new Vector3(0,0,z),2,out var at))street.Add(at);
            Assert.Greater(street.Count,15,"fixture: the street's floor");
            var problems=Blocked(street,"Lanternwick");
            Assert.IsEmpty(problems,string.Join("\n",problems));
        }

        // ---------------------------------------------------------------- item 3: gates and signs
        static bool Stands(Renderer part,float floor)
        {
            if(part is not MeshRenderer||!part.enabled||!part.gameObject.activeInHierarchy)return false;
            if(part.GetComponentInParent<TMP_Text>()!=null||part.GetComponentInParent<CombatActor>()!=null||part.GetComponentInParent<Npc>()!=null)return false;
            if(part.name.StartsWith("Floor detail")||part.name.EndsWith(" floor")||part.name.EndsWith(" cliffs")||part.name=="Walkway runner"||part.name=="Rug")return false;
            if(part.sharedMaterial==null||part.sharedMaterial.shader.name=="Lattice/SignPlate")return false;
            var box=part.bounds;if(Mathf.Max(box.size.x,box.size.z)>40)return false;
            return box.max.y>floor+.25f;
        }
        /// <summary>A prop's world box is good enough for a solid rock, and all there is: a static piece's mesh filter holds its whole merged batch in play. What isn't static is read by its mesh, as a door frame is seen through its opening.</summary>
        static bool Meets(Renderer part,Ray ray,float reach)
        {
            if(!part.bounds.IntersectRay(ray,out float along)||along>=reach)return false;
            if(part.gameObject.isStatic||!part.TryGetComponent<MeshFilter>(out var shape)||shape.sharedMesh==null)return true;
            var probe=part.gameObject.AddComponent<MeshCollider>();probe.sharedMesh=shape.sharedMesh;Physics.SyncTransforms();
            bool met=probe.Raycast(ray,out _,reach);Object.DestroyImmediate(probe);return met;
        }
        [UnityTest] public IEnumerator EveryFloorSignInChapterTwoStandsClear()
        {
            var failures=new List<string>();int read=0;
            foreach(var zone in new[]{"TallowDrift","Lanternwick","LanternGorge","SunkenRelay"})
            {
                yield return Zone(zone);
                var rotation=Quaternion.Euler(View.pitch,View.yaw,0);var look=rotation*Vector3.forward;var right=rotation*Vector3.right;var up=rotation*Vector3.up;
                var signs=FloorAndGateSigns();var parts=All<Renderer>();var seen=new Dictionary<TextMeshPro,Rect>();
                foreach(var sign in signs)
                {
                    read++;float floor=sign.transform.position.y-.2f;
                    var standing=parts.Where(p=>Stands(p,floor)&&Vector3.Distance(p.bounds.center,sign.transform.position)<40+p.bounds.extents.magnitude).ToArray();
                    var hit=new HashSet<string>();
                    foreach(var point in Paint(sign))
                    {
                        var ray=new Ray(point-look*View.distance,look);
                        foreach(var part in standing)if(Meets(part,ray,View.distance+1.2f))hit.Add(part.transform.root.name+" at "+part.bounds.center);
                    }
                    seen[sign]=OnScreen(Paint(sign),right,up);
                    if(hit.Count>0)failures.Add($"{zone} \"{sign.text}\" runs into {string.Join(", ",hit)}");
                }
                foreach(var a in signs)foreach(var b in signs)if(a.GetInstanceID()<b.GetInstanceID()&&seen[a].Overlaps(seen[b]))failures.Add($"{zone} \"{a.text}\" crosses \"{b.text}\"");
            }
            Assert.Greater(read,15,"fixture: the zones carry floor signs");
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }
        /// <summary>A floor sign, the arch's own sign and the prompt all said it within a few steps, and the floor sign read as something left behind.</summary>
        [UnityTest] public IEnumerator TheRelayDoorIsNamedOnce()
        {
            yield return Zone("LanternGorge");
            var names=All<TextMeshPro>().Where(t=>t.name.StartsWith("Sign_")&&t.text.ToUpperInvariant().Contains("SUNKEN RELAY")).ToArray();
            Assert.AreEqual(1,names.Length,"the relay door's name lies on the floor "+names.Length+" times: "+string.Join(", ",names.Select(t=>t.text)));
        }
        [UnityTest] public IEnumerator TheOrchardGateStandsOnTheFloorNotInTheCliff()
        {
            yield return Zone("Lanternwick");
            var gate=All<WarpBeacon>().Single(w=>w.scene=="LanternGorge");var box=BoundsOf(gate);
            // Its leg's middle, a third of its width from its centre.
            var leg=new Vector3(box.center.x-box.size.x*.33f,box.max.y+10,box.center.z);
            Assert.IsTrue(Physics.Raycast(leg,Vector3.down,out var ground,40,~0,QueryTriggerInteraction.Ignore),"fixture: ground under the gate");
            foreach(var hit in Physics.RaycastAll(leg,Vector3.down,40,~0,QueryTriggerInteraction.Ignore))if(hit.collider is MeshCollider)ground=hit;
            Assert.Less(ground.point.y-box.min.y,1,$"the gate's left leg goes {ground.point.y-box.min.y:0.0} m into the cliff, with only a lamp bracket poking out");
        }
        [UnityTest] public IEnumerator TheArrivalShowsAllOfItsSign()
        {
            yield return Zone("Lanternwick");
            yield return new WaitForSecondsRealtime(.8f);
            var sign=All<TextMeshPro>().Single(t=>t.text=="COURIER DOCK");var arch=All<SceneryOcclusion>().Single(s=>s.name=="Market arch");var box=BoundsOf(arch);var eye=Camera.main.transform.position;
            foreach(var point in Paint(sign))
            {
                Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(point)),"COURIER DOCK is cut by the screen's edge on the first look at the town");
                if(box.IntersectRay(new Ray(eye,point-eye),out float along)&&along<Vector3.Distance(eye,point))
                {
                    // Through the arch's box isn't behind its piers: only far enough out to miss both is sure.
                    Assert.Greater(Mathf.Abs(eye.x+(point.x-eye.x)*(box.center.z-eye.z)/(point.z-eye.z)-box.center.x),box.extents.x-.1f,"the arch's pier hides the sign's first letters: 'RIER DOCK'");
                }
            }
        }
        [UnityTest] public IEnumerator ReceivingsSignIsInTheRoomNotUnderTheHud()
        {
            yield return Zone("Hub_Decks");
            Put(Lead,new Vector3(32.63f,.08f,2.9f));yield return new WaitForSecondsRealtime(1.2f);
            var sign=All<TextMeshPro>().First(t=>t.text=="RECEIVING / REPAIR");
            foreach(var point in Paint(sign))
            {
                var view=Camera.main.WorldToViewportPoint(point);
                Assert.Greater(view.y,.16f,"where the party stands at Hal's anvil, RECEIVING / REPAIR lies along the bottom of the screen, half under the hint bar");
                Assert.Greater(view.x,.01f,"where the party stands at Hal's anvil, RECEIVING / REPAIR is cut by the screen's edge");
            }
        }

        // ---------------------------------------------------------------- item 4: curtains and the sky
        static Color[] Read(Texture texture,out int width,out int height)
        {
            width=Mathf.Min(texture.width,256);height=Mathf.Min(texture.height,256);
            var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var image=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try{Graphics.Blit(texture,rt);RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);return image.GetPixels();}
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Object.Destroy(image);}
        }
        /// <summary>Both ends. At the floor a strand comes up from nothing, or it stops in a straight edge, and a row of those across a slanted curtain is a staircase. The rise is in the texture: a line only takes colour at
        /// its points, so a middle colour key on a two-point strand would change nothing.</summary>
        [UnityTest] public IEnumerator AStrandHasNoEdgeAtEitherEnd()
        {
            foreach(var zone in new[]{"SunkenRelay","WickReach"})
            {
                yield return Zone(zone);
                var strand=All<LineRenderer>().First(l=>l.name=="Curtain strand");var texture=strand.sharedMaterial.GetTexture("_BaseMap");
                Assert.AreEqual(TextureWrapMode.Clamp,texture.wrapModeU,zone+": wrapped, the top of the strand's texture bleeds over its foot and draws a line across it");
                var pixels=Read(texture,out int w,out int h);
                float At(float u){return pixels[h/2*w+Mathf.Clamp(Mathf.FloorToInt(u*w),0,w-1)].a;}
                Assert.Less(At(.01f),.08f,zone+": a strand starts at full strength where it leaves the floor: a hard straight edge, stepping like a staircase");
                Assert.Greater(At(.2f),At(.01f)+.6f,zone+": a strand never comes up to its full strength");
                Assert.AreEqual(LineTextureMode.Stretch,strand.textureMode,zone+": the rise is read along the whole strand");
                Assert.AreEqual(2,strand.positionCount,"fixture: a strand is two points, so a colour key couldn't do this");
            }
        }
        [UnityTest] public IEnumerator TheReachsCurtainHasNoRuledFootAndNoTeeth()
        {
            yield return Zone("WickReach");
            var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();Assert.Greater(strands.Length,20,"fixture: the curtain's strands");
            var feet=strands.Select(s=>s.transform.TransformPoint(s.GetPosition(0))).ToArray();
            Assert.Greater(feet.Max(f=>f.y)-feet.Min(f=>f.y),1,"every strand starts at one height: a razor-sharp line across the sky");
            Assert.Greater(feet.Max(f=>f.z)-feet.Min(f=>f.z),1,"every strand starts in one plane: a razor-sharp line across the sky");
            float Edge(LineRenderer s)=>Mathf.Abs(s.transform.TransformPoint(s.GetPosition(0)).x);
            float Rise(LineRenderer s)=>Vector3.Distance(s.GetPosition(0),s.GetPosition(1));
            var outer=strands.Where(s=>Edge(s)>47).ToArray();var middle=strands.Where(s=>Edge(s)<30).ToArray();
            Assert.IsNotEmpty(outer,"fixture: the curtain's ends");Assert.IsNotEmpty(middle,"fixture: the curtain's middle");
            Assert.Less(outer.Max(s=>s.startColor.a),middle.Min(s=>s.startColor.a)*.5f,"the curtain's ends stay at full strength and stop in a row of teeth");
            Assert.Less(outer.Average(Rise),middle.Average(Rise)*.85f,"the curtain's ends stand as tall as its middle and stop in a row of teeth");
        }
        [UnityTest] public IEnumerator TheGatesSignIsASignAndGoesAsTheGateDoes()
        {
            yield return Zone("WickReach");
            var sign=All<TextMeshPro>().Single(t=>t.text=="LANTERN GATE");var strands=All<LineRenderer>().Where(l=>l.name=="Curtain strand").ToArray();
            Assert.IsNotNull(sign.transform.Find("Sign plate"),"LANTERN GATE has no plate");
            Assert.GreaterOrEqual(sign.fontSize,6.5f,$"LANTERN GATE is mostly read with the view pulled back to 60 m, where at {sign.fontSize:0.0} its letters come to 10 px");
            Assert.Less(sign.transform.position.z,strands.Min(s=>s.transform.TransformPoint(s.GetPosition(0)).z),"LANTERN GATE lies behind the light it names");
            var words=sign.GetComponent<Renderer>();Assert.IsTrue(words.enabled,"fixture: the sign shows while the gate's shut");
            Flag("wick.relay1");Flag("wick.relay2");
            float deadline=Time.unscaledTime+3;while(!Has("wick.gateOpen")&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(Has("wick.gateOpen"),"fixture: two relays open the gate");
            yield return new WaitForSecondsRealtime(.25f);
            Assert.IsTrue(strands.Any(s=>s.gameObject.activeInHierarchy&&s.startColor.a>.05f),"fixture: the curtain's still coming down");
            Assert.IsFalse(words.enabled,"LANTERN GATE stays up over the falling curtain and names a stretch of empty sky");
        }
        [UnityTest] public IEnumerator TheReachsSkyMovesWithTheCamera()
        {
            yield return Zone("WickReach");ClearFights();
            Assert.AreEqual("Lattice/DriftingSky",RenderSettings.skybox.shader.name,"the Reach's sky is fixed to the screen: the same mote at the same pixel 100 m apart");
            Assert.IsTrue(RenderSettings.skybox.shader.isSupported,"the sky's shader didn't compile");
            Put(Lead,new Vector3(0,1,40));yield return new WaitForSecondsRealtime(1.5f);
            var there=Shader.GetGlobalVector("_SkyTurn");
            Put(Lead,new Vector3(0,1,140));yield return new WaitForSecondsRealtime(1.5f);
            var here=Shader.GetGlobalVector("_SkyTurn");
            Assert.Greater(Mathf.Abs(here.x-there.x),2.5f,$"flying 100 m turned the sky by {Mathf.Abs(here.x-there.x):0.0} degrees: nothing passes behind the ships");
            // The tilt follows where the view looks, and nothing else. A clock in it brings the panorama's pinched pole into view within the hour, and a lens's distance in it slides the sky with every zoom.
            float rate=(here.x-there.x)/100;
            Assert.Less(Mathf.Abs(here.x-rate*140),.6f,$"the sky's tilt at z 140 is {here.x:0.0} degrees, not {rate*140:0.0}: something other than the view's travel is in it");
        }
        /// <summary>Swapping the shader mustn't change the picture. Panoramic turns its vertices by _Rotation and looks up with the unturned ones, so a shader looking up from fixed vertices has to
        /// turn the other way, or it shows another part of the sky. A turn the same size on the plain shader says how far apart two parts of this sky read, so this can't pass on a blank one.</summary>
        [UnityTest] public IEnumerator TheDriftingSkyIsThePanoramaAtRest()
        {
            yield return Zone("WickReach");
            var drifting=RenderSettings.skybox;Assert.AreEqual("Lattice/DriftingSky",drifting.shader.name,"fixture: the Reach wears the drifting sky");
            var shader=Shader.Find("Skybox/Panoramic");Assert.IsNotNull(shader,"fixture: Skybox/Panoramic to compare with");
            // Stopping it puts the turn back to nothing. By name, so this file compiles before the patch does.
            GameObject.Find("Sky drift").SetActive(false);
            var view=Camera.main;int seen=view.cullingMask;view.cullingMask=0;
            var plain=new Material(shader);plain.CopyPropertiesFromMaterial(drifting);
            float Apart(Color[] a,Color[] b){float sum=0;for(int i=0;i<a.Length;i++)sum+=Mathf.Abs(a[i].grayscale-b[i].grayscale);return sum/a.Length;}
            try
            {
                const int W=960,H=540;
                var ours=Grab(W,H);
                RenderSettings.skybox=plain;var theirs=Grab(W,H);
                plain.SetFloat("_Rotation",plain.GetFloat("_Rotation")+25);var elsewhere=Grab(W,H);
                float yardstick=Apart(theirs,elsewhere);
                Assert.Greater(yardstick,.002f,"fixture: this sky has too little in it to tell one part from another");
                Assert.Less(Apart(ours,theirs),yardstick*.25f,"the new shader shows a different part of the panorama from the one it replaced");
            }
            finally{RenderSettings.skybox=drifting;view.cullingMask=seen;Object.Destroy(plain);}
        }

        // ---------------------------------------------------------------- item 5: the relay
        static readonly (float west,float south,float north)[] RelayWalls={(-8.1f,-4,18),(-3.1f,24,42),(-14.1f,48,80),(-10.1f,90,112),(-18.1f,120,160)};
        /// <summary>The other side of the sweep in QaPlanEarlyTests: that one says a hero isn't hidden, and goes quiet once there's no wall on the camera's line. This says there isn't.</summary>
        [UnityTest] public IEnumerator TheRelaysWallsStayUnderTheLens()
        {
            Flag("relay.hessaConfronted");
            yield return Zone("SunkenRelay");ClearFights();
            foreach(var seal in All<Membrane>())seal.SetOpen(true);
            var back=-(Quaternion.Euler(View.pitch,View.yaw,0)*Vector3.forward);var problems=new List<string>();
            foreach(var (west,south,north) in RelayWalls)for(float z=south;z<=north;z+=2)
            {
                var chest=new Vector3(west,1.2f,z);
                foreach(var hit in Physics.RaycastAll(chest,back,View.distance,~0,QueryTriggerInteraction.Ignore))
                    if(CameraRig.Scenery(hit.collider))problems.Add($"at the west wall's foot ({west},{z}) the rock stands on the camera's line {hit.distance:0.0} m back, so the camera slides in to a close-up");
            }
            Assert.IsEmpty(problems,string.Join("\n",problems.Take(12)));
            var rig=Object.FindFirstObjectByType<CameraRig>();
            Put(Lead,new Vector3(-18.4f,.1f,126.5f));yield return new WaitForSecondsRealtime(1.5f);
            Assert.Greater(rig.GroundCameraDistance,View.distance-1,"beside the Great Lantern's west wall the camera drops in close, Sela huge at the screen's edge and Hessa off it");
            var floor=All<MeshFilter>().Single(f=>f.name.EndsWith(" floor")).sharedMesh;var points=floor.vertices;float tallest=0;foreach(int i in floor.triangles)tallest=Mathf.Max(tallest,points[i].y);
            Assert.Less(tallest,1.4f,"a low wall's top draws as floor: a second, lit floor up on top of every wall on the camera's side");
        }
        [UnityTest] public IEnumerator BodiesInTheRelayGetSomeFill()
        {
            yield return Zone("SunkenRelay");
            Assert.Greater(RenderSettings.ambientEquatorColor.grayscale,.24f,"between lamps a body's flanks get next to no light: near-black cut-outs on the floor");
            Assert.Less(RenderSettings.ambientSkyColor.grayscale,.25f,"the fill has gone from a body's flanks to the floor's whole face: this is meant to stay dark");
        }

        // ---------------------------------------------------------------- item 6: staging
        static float AcrossTheScreen(Vector3 a,Vector3 b)=>Mathf.Abs(Vector3.Dot(a-b,Quaternion.Euler(0,SightlineAudit.Yaw,0)*Vector3.right));
        IEnumerator Stages(System.Action start,Vector3 from)
        {
            Partner.GetComponent<PartnerBrain>().enabled=true;
            Put(Partner,from);
            yield return new WaitForSecondsRealtime(.3f);
            DialogueSystem.Current.AutoAdvance=false;start();
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,"fixture: the talk starts");
            // She walks to her mark for up to four seconds.
            yield return new WaitForSecondsRealtime(4.6f);
        }
        /// <summary>A mark at the hero's side lies straight behind him when he faces east: from this camera, one figure with two heads.</summary>
        [UnityTest] public IEnumerator ThePartnerStandsClearOfTheHeroOnTheScreen()
        {
            yield return Zone("Lanternwick");
            foreach(var (speaker,stand) in new[]{("Aumi",new Vector3(19.7f,3.2f,52.5f)),("Pell",new Vector3(7.5f,.3f,36)),("Corra",new Vector3(-4,.3f,17)),("Ishen",new Vector3(3,.3f,90))})
            {
                var person=Person(speaker);
                Put(Lead,stand);
                yield return Stages(person.Interact,stand+new Vector3(0,0,-4));
                float apart=AcrossTheScreen(Partner.transform.position,Lead.transform.position);
                Assert.Greater(apart,1.05f,$"with {speaker} the partner stands in line with the hero from the camera, {apart:0.0} m across the screen: one figure with two heads");
                Assert.Greater(Vector3.Distance(Partner.transform.position,Lead.transform.position),1.1f,$"with {speaker} she's pressed into him");
                Assert.Less(Vector3.Distance(Partner.transform.position,Lead.transform.position),3.4f,$"with {speaker} she stays off across the square");
                DialogueSystem.Current.Cancel();
                float deadline=Time.unscaledTime+5;while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
                yield return new WaitForSecondsRealtime(.6f);
            }
        }
        [UnityTest] public IEnumerator ADiscoveryBringsThePartnerOver()
        {
            yield return Zone("SunkenRelay");ClearFights();
            var point=All<DiscoveryPoint>().Single(p=>p.flag=="relay.entered");
            Put(Lead,new Vector3(0,.1f,11.8f));
            yield return Stages(point.Interact,new Vector3(-6,.1f,2));
            Assert.Less(Vector3.Distance(Partner.transform.position,Lead.transform.position),3.4f,"a talk that starts on its own leaves her across the hall, saying her own line about the cut housings");
            Assert.Greater(AcrossTheScreen(Partner.transform.position,Lead.transform.position),1.05f,"she comes over and stands behind him");
            DialogueSystem.Current.Cancel();
        }
        [UnityTest] public IEnumerator ACrossingBringsThePartnerOverAndFramesTheCaptain()
        {
            yield return Zone("SunkenRelay");
            var point=All<DiscoveryPoint>().Single(p=>p.flag=="relay.hessaConfronted");
            Put(Lead,new Vector3(1.5f,.1f,122.4f));
            yield return Stages(point.Interact,new Vector3(-3,.1f,114));
            Assert.Less(Vector3.Distance(Partner.transform.position,Lead.transform.position),3.4f,"through Hessa's opening only the top of the partner's head pokes out under the dialogue box");
            Assert.Greater(AcrossTheScreen(Partner.transform.position,Lead.transform.position),1.05f,"she stands in line with the hero through Hessa's opening");
            var hessa=Health.All.FirstOrDefault(h=>h!=null&&!h.friendly&&h.id!=null&&h.id.StartsWith("Hessa"));Assert.IsNotNull(hessa,"fixture: she's in the room");
            Assert.IsTrue(Inside(Camera.main.WorldToViewportPoint(hessa.transform.position)),"Hessa isn't in frame for her own opening");
            DialogueSystem.Current.Cancel();
        }

        // ---------------------------------------------------------------- item 7: props
        static Color[] Grab(int W,int H)
        {
            var camera=Camera.main;
            var image=new Texture2D(W,H,TextureFormat.RGB24,false);var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                return image.GetPixels();
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();Object.Destroy(rt);Object.Destroy(image);}
        }
        [UnityTest] public IEnumerator TheEddysCacheShowsInItsRoom()
        {
            yield return Zone("Gullet_Tunnel");ClearFights();
            var cache=All<SalvageField>().Single(c=>c.cacheId=="Neve");
            Assert.IsNotNull(cache.GetComponentInChildren<Light>(),"nothing picks the cache out of a dim room");
            var at=cache.transform.position;Put(Lead,new Vector3(at.x-7,1,at.z-9));
            foreach(var member in PartyController.Current.members)if(member!=Lead)Put(member,new Vector3(at.x-10,1,at.z-9));
            yield return new WaitForSecondsRealtime(1.5f);
            const int W=960,H=540;var pixels=Grab(W,H);var box=BoundsOf(cache);var camera=Camera.main;
            var low=camera.WorldToViewportPoint(box.center-box.extents*.4f);var high=camera.WorldToViewportPoint(box.center+box.extents*.4f);
            float sum=0;int count=0;
            for(int y=Mathf.FloorToInt(Mathf.Min(low.y,high.y)*H);y<=Mathf.FloorToInt(Mathf.Max(low.y,high.y)*H);y++)for(int x=Mathf.FloorToInt(Mathf.Min(low.x,high.x)*W);x<=Mathf.FloorToInt(Mathf.Max(low.x,high.x)*W);x++)
                if(x>=0&&x<W&&y>=0&&y<H){sum+=pixels[y*W+x].grayscale;count++;}
            Assert.Greater(count,20,"fixture: the cache is in view");
            Assert.Greater(sum/count,.14f,$"the cache is a black hole in the room ({sum/count:0.00}): only its corner brackets show");
        }
        [UnityTest] public IEnumerator TheRigAndTheCrystalsDoNotClipUnderSorrelsSun()
        {
            yield return Zone("Sorrel_Ridges");
            var rig=All<Transform>().First(t=>t.name=="OutpostDrill").GetComponentInChildren<Renderer>().sharedMaterial;
            Assert.Less(rig.GetColor("_BaseColor").grayscale,.8f,"the rig's cream plates clip to a blown-out white, the brightest thing on the screen");
            int looked=0;
            foreach(var node in All<MineNode>())foreach(var part in node.GetComponentsInChildren<Renderer>(true))
            {
                looked++;var look=part.sharedMaterial;
                Assert.Less(look.GetColor("_BaseColor").grayscale,.65f,look.name+"'s paint is so bright under this sun that every band of the ramp clips to one neon cyan: no facets");
                Assert.Less(look.GetColor("_EmissionColor").maxColorComponent,.3f,look.name+" glows too hard for its facets to show");
            }
            Assert.Greater(looked,0,"fixture: Sorrel has crystals");
        }
        /// <summary>The paint under these glows is itself one flat cyan, so turning the glow down only dims a flat rectangle. What's lit has to fall off.</summary>
        [UnityTest] public IEnumerator GlowingTopsFallOffAcrossThemselves()
        {
            yield return Zone("Sorrel_Ridges");
            foreach(var name in new[]{"LatticeAnvil","HushwellOreCart"})
            {
                var look=All<Transform>().First(t=>t.name==name).GetComponentInChildren<Renderer>().sharedMaterial;
                var map=Read(look.GetTexture("_EmissionMap"),out _,out _);
                int lit=0,full=0;foreach(var pixel in map){if(pixel.grayscale<.1f)continue;lit++;if(pixel.grayscale>.85f)full++;}
                Assert.Greater(lit,50,"fixture: "+name+" has something lit");
                Assert.Less(full/(float)lit,.5f,$"{name}: {full*100/lit}% of what's lit is at full strength: a flat cyan rectangle");
            }
        }
        [UnityTest] public IEnumerator PatchesFeatherToNothingAtTheirRim()
        {
            yield return null;
#if UNITY_EDITOR
            var scars=UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Generated/Dressing/FloorDecals-Gullet_Tunnel.asset").OfType<Mesh>().Where(m=>m.name.Contains("GroundScar")).ToArray();
            Assert.IsNotEmpty(scars,"fixture: the throat has its scars");
            foreach(var mesh in scars)
            {
                var tone=mesh.colors32;Assert.IsNotEmpty(tone,"fixture: "+mesh.name+" carries its feather");
                Assert.AreEqual(0,tone.Min(c=>c.a),mesh.name+" never fades out: Mathf.SmoothStep isn't the shader's, and every patch ends in a faint straight-cut square");
                Assert.Greater(tone.Max(c=>c.a),130,mesh.name+" has faded away altogether");
                foreach(var at in mesh.vertices)Assert.IsFalse(at.z>745&&at.z<860,mesh.name+" lies across the Coil's scour with nothing to explain it");
            }
#endif
        }
        [UnityTest] public IEnumerator PlantsHideTheirDiscs()
        {
            yield return Zone("Sorrel_Ridges");
            var plants=All<Transform>().Where(t=>(t.name=="DressSucculent"||t.name=="DressFanShrub")&&t.parent!=null&&t.parent.name=="Room dressing").ToArray();
            Assert.Greater(plants.Length,100,"fixture: Sorrel's scrub");
            int showing=0;
            foreach(var plant in plants)
            {
                if(!Physics.Raycast(plant.position+Vector3.up*3,Vector3.down,out var ground,8,~0,QueryTriggerInteraction.Ignore))continue;
                var box=BoundsOf(plant);if(ground.point.y-box.min.y<.13f*box.size.y)showing++;
            }
            Assert.Less(showing,plants.Length*.1f,$"{showing} of {plants.Length} plants stand with the jagged disc under them above the ground");
        }
        /// <summary>The graded roads, from SorrelRedesign.</summary>
        static readonly Vector2[][] SorrelRoads=
        {
            new Vector2[]{new(0,-18),new(0,8),new(0,24),new(9,39),new(4,58),new(-8,75),new(-5,94),new(9,111),new(16,129),new(7,145),new(0,159),new(-5,177),new(0,199)},
            new Vector2[]{new(-5,27),new(-24,35),new(-36,49),new(-39,68),new(-28,88),new(-24,107),new(-13,124),new(7,145)},
            new Vector2[]{new(9,39),new(29,46),new(39,64),new(31,83),new(35,103),new(31,123),new(16,129)},
        };
        /// <summary>The reader's strip of bright brick by the upper crystal is a strata ledge at (-1.1, 112.2), from the far slopes' scatter, and the verge lays the same piece along the road. It reads as rock only on a far slope's contour.</summary>
        [UnityTest] public IEnumerator TheRidgeHasNoGardenKerb()
        {
            yield return Zone("Sorrel_Ridges");
            var kerbs=new List<string>();
            foreach(var ledge in All<Transform>().Where(t=>t.name=="DressStrataLedge"&&t.parent!=null&&t.parent.name=="Room dressing"))
            {
                var at=new Vector2(ledge.position.x,ledge.position.z);float near=float.PositiveInfinity;
                foreach(var road in SorrelRoads)for(int i=1;i<road.Length;i++)near=Mathf.Min(near,ToSegment(at,road[i-1],road[i]));
                if(near<14)kerbs.Add($"({at.x:0.0}, {at.y:0.0})");
            }
            Assert.IsEmpty(kerbs,"a curved row of bright orange blocks lies beside the road, somebody's garden kerb on a wild ridge, at "+string.Join(", ",kerbs));
        }
        [UnityTest] public IEnumerator ThePlanetShowsNoHardDisc()
        {
            yield return Zone("Hub_CinderHalo");
            foreach(var name in new[]{"Vorun","Sorrel"})
            {
                var planet=All<Renderer>().First(r=>r.name==name);
                Assert.Greater(planet.sharedMaterial.GetColor("_ShadowTint").grayscale,.85f,name+": the toon ramp's steps lie across a painted planet as a hard-edged paler disc");
            }
        }
        [UnityTest] public IEnumerator TheWreckIsBurnedAndItsSpoilIsNotAStamp()
        {
            Flag("bossdown.Burrower");
            yield return Zone("Sorrel_Ridges");
            var wreck=All<Transform>().First(t=>t.name=="Burrower wreck");var parts=wreck.Find("Thrown parts");
            Assert.IsNotNull(wreck.Find("Rig").GetComponent<PropTint>(),"from the road the wreck's clean grey ribs read as a heap of boulders");
            Assert.IsNotNull(parts.Find("Wreck scorch"),"nothing on the ground says it burned here");
            Assert.IsNull(parts.Find("DressSpoilHeap"),"a neat oval of orange nuggets lies at its nose like a stamped spill");
        }
        [UnityTest] public IEnumerator TheNurserysPoolsHaveAShore()
        {
            yield return Zone("Hushwell");
            Assert.GreaterOrEqual(All<Transform>().Count(t=>t.name=="Pool shore"),3,"the nursery's pools fade into the floor with no edge");
            Assert.GreaterOrEqual(All<Transform>().Count(t=>t.name=="Pool lip"),3,"the nursery's pools have no water's edge");
            foreach(var ring in All<MeshRenderer>().Where(r=>r.name=="Pool shore"||r.name=="Pool lip"))Assert.Less(ring.bounds.size.y,1,"a pool's shore runs up the side of something standing beside it: it belongs on the cave's floor");
        }
        [UnityTest] public IEnumerator TheFirstJobHasItsKitOutUntilItsDone()
        {
            GameServices.Current.NewGame();Flag(OpeningMarketSequence.Requested);
            yield return Zone("Hub_Decks","Office");
            var kit=All<Transform>().FirstOrDefault(t=>t.name=="Service job kit");
            Assert.IsNotNull(kit,"the first job is two boards and a black plate, with nothing to say what they are");
            yield return null;yield return null;
            Assert.IsTrue(kit.gameObject.activeInHierarchy,"fixture: the job's on");
            Assert.IsEmpty(kit.GetComponentsInChildren<Collider>(),"the job's kit is solid and off the bake: it can trap someone");
            Assert.GreaterOrEqual(kit.GetComponentsInChildren<Renderer>().Length,5,"the opening has no edge to it");
            Flag(OpeningMarketSequence.Repaired);yield return null;yield return null;
            Assert.IsFalse(All<Transform>().Where(t=>t.name=="Opening edge").Any(t=>t.gameObject.activeInHierarchy),"the job's painted edge stays round a hole that's been closed");
            // The kit stays a while, as if someone's still packing up: QaWorldThirdTests. It doesn't stay for good.
            Flag("warpkey");yield return null;yield return null;
            Assert.IsFalse(kit.gameObject.activeInHierarchy,"the kit stays out for the rest of the game once the walkway's mended");
        }
        [UnityTest] public IEnumerator ThePassageGoesOnPastItsRim()
        {
            yield return Zone("Gullet_Tunnel");
            var tissue=All<MeshRenderer>().FirstOrDefault(r=>r.name=="Gullet outer tissue");
            Assert.IsNotNull(tissue,"pulled back, the floor ends in a ruled line with stars beyond it");
            Assert.IsEmpty(tissue.GetComponentsInChildren<Collider>(),"nothing a ship could reach may be solid out there");
            Assert.Greater(tissue.bounds.max.x,GulletProfile.RightEdge(400)+30,"the tissue stops within sight");
            Assert.Less(tissue.bounds.min.x,GulletProfile.LeftEdge(400)-30,"the tissue stops within sight");
            Assert.Greater(tissue.bounds.min.z,GulletProfile.Start,"the tissue closes off the mouth: the way in has to stay open to space");
        }
        /// <summary>The relay reader's Major. A blade and a shot both stop at a solid piece, and a scavenger backed in behind this one stayed at 128/210 until the route gave up on Relay_2. Whether a fight can still
        /// wedge anywhere else takes a run of chapter 15: this only says this pocket has gone.</summary>
        [UnityTest] public IEnumerator TheVaultsRigLeavesNoPocketInTheFight()
        {
            yield return Zone("SunkenRelay");
            var rig=All<Transform>().Where(t=>t.name=="MusterCutterRig").OrderBy(t=>Vector3.Distance(t.position,new Vector3(9,0,97.5f))).First();
            Assert.Less(Vector3.Distance(rig.position,new Vector3(9,rig.position.y,97.5f)),2,"fixture: the lower vault's rig");
            Assert.IsEmpty(rig.GetComponentsInChildren<Collider>(),"with the column south of it and the wall east of it, the lower vault's rig makes a pocket in a fight that nothing can hit into");
        }
        [UnityTest] public IEnumerator TheDockHasATownUnderIt()
        {
            yield return Zone("WickReach");
            var below=All<Transform>().Where(t=>t.name=="Lanternwick below the dock").ToArray();
            Assert.GreaterOrEqual(below.Length,3,"Lanternwick's dock is a bare disc alone in the sky");
            foreach(var piece in below)
            {
                Assert.IsEmpty(piece.GetComponentsInChildren<Collider>(),"what's under the dock costs integrity to touch");
                Assert.Less(BoundsOf(piece).max.y,.5f,"what's under the dock rises to the ships' plane");
            }
        }
        [UnityTest] public IEnumerator TheOutpostsCradleLooksLikeOne()
        {
            yield return Zone("Sorrel_Ridges");
            var around=All<Transform>().FirstOrDefault(t=>t.name=="Outpost cradle bay");
            Assert.IsNotNull(around,"the first cradle anyone meets is a shut box beside a bench");
            Assert.IsEmpty(around.GetComponentsInChildren<Light>(),"Sorrel's terrain is one renderer with six lights spent: a seventh puts another pool out across the whole basin");
            Assert.IsEmpty(around.GetComponentsInChildren<Collider>(),"the bay is solid and off the bake");
            var bay=All<RepairBay>().Single(b=>b.prompt.StartsWith("Outpost cradle"));
            Assert.Greater(bay.GetComponentInChildren<Renderer>().sharedMaterial.GetColor("_EmissionColor").maxColorComponent,1.5f,"nothing on the outpost's cradle is lit");
        }

        // ---------------------------------------------------------------- item 8: people in Lanternwick
        /// <summary>The town's two routes, from chapter-13 and chapter-16, as they walk them.</summary>
        static readonly Vector2[][] TownRoutes=
        {
            new Vector2[]{new(0,-14),new(0,2),new(-4,17),new(4,33),new(6,31),new(17.6f,25.1f),new(18.9f,25.5f),new(19.4f,49.5f),new(18.9f,51.3f),new(20,53.5f),new(18.9f,51.3f),new(19.4f,49.5f),new(18.9f,25.5f),new(17.1f,25.5f),new(7,36),new(5,34.5f),new(-4,17),new(-2.5f,27.8f),new(3,90),new(-6,81),new(-15.8f,74.1f),new(-30,60),new(-32.7f,71.1f),new(-32,81.6f),new(-31.5f,84)},
            new Vector2[]{new(3,90),new(5.6f,42.5f),new(6.9f,36.3f),new(17.1f,25.5f),new(18.9f,25.5f),new(19.4f,49.5f),new(18.9f,51.3f),new(20,53.5f),new(18.9f,51.3f),new(19.4f,49.5f),new(18.9f,25.5f),new(17.1f,25.5f),new(7,36),new(-5.5f,49.5f),new(-15.8f,51.9f),new(-21.5f,55.2f),new(-30,60),new(-28.5f,55.2f),new(-27.5f,48.8f),new(-8.3f,44.1f),new(-5.5f,38.4f),new(-2.5f,27.8f),new(-4,17),new(0,-19)},
        };
        [UnityTest] public IEnumerator LanternwickHasPeopleLivingInIt()
        {
            yield return Zone("Lanternwick");
            var folk=All<Transform>().Where(t=>t.name=="Townsperson").ToArray();
            Assert.GreaterOrEqual(folk.Length,6,"past the named few, the town is a bare plaza");
            var block=new MaterialPropertyBlock();var tints=new HashSet<Color>();
            foreach(var person in folk)
            {
                Assert.IsEmpty(person.GetComponentsInChildren<InteractionPrompt>(true),"an unnamed townsperson offers a prompt");
                var body=person.GetComponentInChildren<SkinnedMeshRenderer>();Assert.IsNotNull(body,"fixture: a townsperson has a body");
                body.GetPropertyBlock(block);var tint=block.GetColor("_BaseColor");tints.Add(tint);
                Assert.Less(tint.grayscale,.95f,"a townsperson wears their body's own colours and reads as someone with a name");
                var at=new Vector2(person.position.x,person.position.z);
                foreach(var route in TownRoutes)for(int i=1;i<route.Length;i++)
                    Assert.Greater(ToSegment(at,route[i-1],route[i]),1.6f,$"a townsperson at {at} stands on a route, from {route[i-1]} to {route[i]}");
                foreach(var npc in All<Npc>())
                    foreach(float height in new[]{.4f,.9f,1.5f})
                    {
                        var target=npc.transform.position+Vector3.up*height;var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;
                        var solid=new Bounds(person.position+Vector3.up*.95f,new Vector3(.9f,1.9f,.9f));
                        Assert.IsFalse(solid.IntersectRay(new Ray(eye,target-eye),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore,$"a townsperson at {at} stands between the camera and {npc.speaker}");
                    }
            }
            Assert.GreaterOrEqual(tints.Count,5,"the town's people are one colour");
            var stalls=All<Transform>().Where(t=>t.name=="DressMarketStall"&&t.parent==null).ToArray();
            Assert.GreaterOrEqual(stalls.Length,6,"the market is the same four stalls");
            // The model's counter and shelf of jars are on its forward side, and its back is a plain dark panel.
            foreach(var stall in stalls)Assert.Greater(-Mathf.Sign(stall.position.x)*stall.forward.x,.7f,$"the stall at {stall.position} turns its plain back on the street");
            int lamps=All<Transform>().Count(t=>t.name=="LampPost"&&t.parent==null&&t.position.x>20&&t.position.x<25&&t.position.z>18&&t.position.z<48);
            Assert.GreaterOrEqual(lamps,4,"nothing lit climbs the lantern stair");
            foreach(var pool in All<MeshRenderer>().Where(r=>r.name=="Lamp pool"))Assert.Less(pool.bounds.size.y,2.2f,"a lamp's painted pool runs up the planter or the ledge beside it, a slanted orange sheet: it belongs on the ground");
            var brekk=Person("Brekk");
            Assert.IsTrue(All<Transform>().Any(t=>t.name=="CanyonHouse"&&t.parent==null&&Vector3.Distance(t.position,brekk.transform.position)<8),"the quest says a shed, and all that's there is a black tarp on poles");
        }
    }
}
