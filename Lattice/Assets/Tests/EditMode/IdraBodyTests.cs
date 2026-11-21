using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.Tests.EditMode
{
    /// <summary>What Idra's bodies need that the first two heroes' already have (November 12): a stride profile measured from her own
    /// boots, so GroundFeet plants them, and engine sockets in the two wells of her cutter's stern deck.</summary>
    public sealed class IdraBodyTests
    {
        [TestCase("IdraNatural")][TestCase("IdraBraced")]
        public void HerGroundBodyWalksOnAStrideMeasuredFromHerOwnSole(string id)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+".prefab");Assert.IsNotNull(prefab,id);
            var definition=GameCatalog.Find<CharacterDef>("Idra");
            Assert.That(new[]{definition.natural,definition.shaped},Has.Member(prefab),"Idra's definition doesn't stand in "+id);
            var profile=prefab.GetComponent<GeneratedAnimator>().strideProfile;
            Assert.IsNotNull(profile,id+" has no stride profile: GroundFeet never runs on her and her feet skate at every pace");
            Assert.AreEqual(id,profile.name,id+" walks on another body's stride");
            var measured=Measured(prefab);
            // Her speeds must stand to her sole as Taren's and Sela's stand to theirs: the same reading of each of their four bodies
            // gives the convention their profiles were stored in (a walk a little over its toe's mid-stance speed).
            var convention=new Dictionary<string,float>{{"Walk",0},{"Run",0},{"Sprint",0}};
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var reference=GameCatalog.Find<CharacterDef>(hero);var body=shaped?reference.shaped:reference.natural;var stored=body.GetComponent<GeneratedAnimator>().strideProfile;
                var read=Measured(body);convention["Walk"]+=stored.walkSpeed/read["Walk"]/4;convention["Run"]+=stored.runSpeed/read["Run"]/4;convention["Sprint"]+=stored.sprintSpeed/read["Sprint"]/4;
            }
            string Off(GroundStrideProfile p)
            {
                var faults=new List<string>();
                foreach(var (clip,speed) in new[]{("Walk",p.walkSpeed),("Run",p.runSpeed),("Sprint",p.sprintSpeed)})
                    if(Mathf.Abs(speed/(measured[clip]*convention[clip])-1)>.03f)faults.Add($"{clip} stored {speed:0.00} m/s, her planted toe travels {measured[clip]:0.000} (x{convention[clip]:0.000} by the first two heroes' convention)");
                return string.Join("; ",faults);
            }
            // The check can fail: Taren's speeds, which she used to borrow with his body, are rejected for hers.
            Assert.IsNotEmpty(Off(Resources.Load<GroundStrideProfile>("Motion/TarenNatural")),"control: Taren's stride passes for Idra's sole, so the check can't tell");
            Assert.IsEmpty(Off(profile),id);
        }

        /// <summary>Her cutter's engines are two round wells sunk in its stern deck either side of the winch (tools/meshy/c13_idra_batch.py), open
        /// upward. Each exhaust leaves its well over the stern lip: the socket sits on the deck at the well's stern edge, with the well open in front
        /// of it, facing aft as every hull's engines do. Sockets read off by eye sat at the wells' rear outer walls, where the flame ran into the hull.</summary>
        [Test] public void HerEnginesLeaveTheTwoWellsInHerSternDeck()
        {
            var hull=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/IdraFlight.prefab"));
            try
            {
                hull.transform.SetPositionAndRotation(new Vector3(0,50,0),Quaternion.identity);
                var sockets=hull.GetComponent<FlightSockets>();Assert.IsNotNull(sockets);Assert.AreEqual(2,sockets.engines.Length,"the cutter has two engine wells");
                foreach(var filter in hull.GetComponentsInChildren<MeshFilter>())filter.gameObject.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                Physics.SyncTransforms();
                var points=hull.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
                float Deck(Vector3 at)=>Physics.Raycast(new Vector3(at.x,at.y+3,at.z),Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)?hit.point.y:float.NegativeInfinity;
                string Fault(Vector3 at)
                {
                    float under=Deck(at),ahead=Deck(at+hull.transform.forward*.2f);
                    if(at.y-under>.06f)return $"floats {at.y-under:0.000} m over the deck";
                    if(under>at.y+.005f)return "is buried under the hull";
                    if(at.y-ahead<.08f)return $"has no well open in front of it (the deck 0.2 m ahead is only {at.y-ahead:0.000} m lower)";
                    // FlightThrusters draws the flame straight aft from the socket; the first half metre of it must be in the open.
                    for(float back=.05f;back<=.45f;back+=.05f)if(Deck(at-hull.transform.forward*back)>at.y+.005f)return $"its flame runs into the hull {back:0.00} m aft";
                    return null;
                }
                // The check can fail: the two sockets first read off by eye, and a point on the deck beside a well, are rejected.
                foreach(var byEye in new[]{new Vector3(-.78f,.78f,-1.25f),new Vector3(.78f,.78f,-1.25f)})
                    Assert.IsNotNull(Fault(hull.transform.TransformPoint(byEye)),"control: the socket read off by eye at "+byEye+" passes");
                Assert.IsNotNull(Fault(sockets.engines[0].position+hull.transform.right*Mathf.Sign(sockets.engines[0].localPosition.x)*.35f),"control: a point beside the well passes as a well");
                Assert.That(sockets.engines.Select(e=>Mathf.Sign(e.localPosition.x)).Distinct().Count(),Is.EqualTo(2),"both engines on one side of the winch");
                foreach(var engine in sockets.engines)
                {
                    Assert.Greater(Mathf.Abs(engine.localPosition.x),.4f,engine.name+" is on the winch, not in a well");
                    Assert.Less(points.Min(p=>Vector3.Distance(p,engine.position)),.13f,engine.name+" floats beyond the generated hull");
                    Assert.Less(engine.localPosition.z,-.7f);Assert.Less(Vector3.Dot(engine.forward,hull.transform.forward),-.99f);
                    Assert.IsNull(Fault(engine.position),engine.name+" "+Fault(engine.position));
                }
            }
            finally{Object.DestroyImmediate(hull);}
        }

        /// <summary>The take's own speed under her sole: how fast the visible toe of each foot travels back through the central stance, read
        /// from the baked generated mesh (no profile offsets, no GroundFeet).</summary>
        static Dictionary<string,float> Measured(GameObject prefab)
        {
            var body=Object.Instantiate(prefab);var result=new Dictionary<string,float>();
            try
            {
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                Object.DestroyImmediate(body.GetComponent<GeneratedAnimator>());
                var rig=body.GetComponentInChildren<Animator>();var clips=rig.runtimeAnimatorController.animationClips;rig.runtimeAnimatorController=null;
                var toes=new[]{new Toe(rig,true),new Toe(rig,false)};
                foreach(var (clip,from,to,other) in new[]{("Walk",.2f,.4f,.5f),("Run",.08f,.16f,.5f),("Sprint",.095f,.16f,.55f)})
                {
                    var take=clips.Single(c=>c.name==clip);var graph=PlayableGraph.Create("Idra sole speed");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    try
                    {
                        var play=AnimationClipPlayable.Create(graph,take);play.SetApplyFootIK(true);
                        AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(play);graph.Play();
                        float Z(int side,float phase){play.SetTime(Mathf.Repeat(phase+side*other,1)*take.length);graph.Evaluate(0);return toes[side].Point().z;}
                        float travelled=0;for(int side=0;side<2;side++)travelled+=Z(side,from)-Z(side,to);
                        result[clip]=travelled*.5f/((to-from)*take.length);
                    }
                    finally{graph.Destroy();}
                }
                foreach(var toe in toes)toe.Dispose();
                return result;
            }
            finally{Object.DestroyImmediate(body);}
        }
        /// <summary>The front quarter of a foot's lowest vertices, chosen in bind pose and baked again in each pose.</summary>
        sealed class Toe
        {
            sealed class Part {public SkinnedMeshRenderer renderer;public Mesh mesh=new Mesh();public List<Vector3> vertices=new();public int[] indices;}
            readonly List<Part> parts=new();
            public Toe(Animator rig,bool left)
            {
                var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                var other=rig.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};parts.Add(part);renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);
                    for(int i=0;i<part.vertices.Count;i++)
                    {
                        var p=renderer.transform.TransformPoint(part.vertices[i]);
                        if(Vector3.Distance(p,foot.position)<.48f&&Vector3.Dot(p-(foot.position+other.position)*.5f,foot.position-other.position)>0)candidates.Add((part,i,p));
                    }
                }
                Assert.IsNotEmpty(candidates);float bottom=candidates.Min(c=>c.point.y);
                var sole=candidates.Where(c=>c.point.y<bottom+.025f).ToArray();float back=sole.Min(c=>c.point.z),front=sole.Max(c=>c.point.z);Assert.Greater(front-back,.08f);
                foreach(var part in parts)part.indices=sole.Where(c=>c.part==part&&c.point.z>front-(front-back)*.25f).Select(c=>c.index).ToArray();
            }
            public Vector3 Point()
            {
                Vector3 sum=Vector3.zero;int count=0;
                foreach(var part in parts){if(part.indices.Length==0)continue;part.renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);foreach(int i in part.indices){sum+=part.renderer.transform.TransformPoint(part.vertices[i]);count++;}}
                return sum/count;
            }
            public void Dispose(){foreach(var part in parts)Object.DestroyImmediate(part.mesh);}
        }
    }
}
