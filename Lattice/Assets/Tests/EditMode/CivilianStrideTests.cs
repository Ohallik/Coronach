using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.Tests.EditMode
{
    /// <summary>Mira's walk home on the first morning (D195 left it unmeasured, November 12): her body carries a stride profile read off her own sole, in the convention Taren's
    /// and Sela's are stored in, so GeneratedAnimator plays her walk at the rate her pace asks and the foot she stands on stays put. (TownLifeTests.MiraPlantsHerFeetOnHerWalkHome
    /// watches it on the deck.)</summary>
    public sealed class CivilianStrideTests
    {
        [Test] public void MirasWalkIsMeasuredFromHerOwnSole()
        {
            var prefab=GameCatalog.Find<CharacterDef>("Mira").natural;Assert.AreEqual("Mira",prefab.name,"fixture: Mira stands in her own body");
            var profile=prefab.GetComponent<GeneratedAnimator>().strideProfile;
            Assert.IsNotNull(profile,"Mira has no stride profile: her .9 m/s walk plays at the floor rate .7 and her planted sole slides forward under her");
            Assert.AreEqual("Mira",profile.name,"Mira walks on another body's stride");
            float measured=Walk(prefab),convention=0;
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var definition=GameCatalog.Find<CharacterDef>(hero);var body=shaped?definition.shaped:definition.natural;
                convention+=body.GetComponent<GeneratedAnimator>().strideProfile.walkSpeed/Walk(body)/4;
            }
            string Off(float stored)=>Mathf.Abs(stored/(measured*convention)-1)>.03f?$"her walk is stored at {stored:0.00} m/s and her planted toe travels {measured:0.000} (x{convention:0.000} by the heroes' convention)":null;
            // The check can fail: the pace her walk was played at before, .9 m/s at the floor rate .7 (as though her walk covered 1.29 m/s), is rejected.
            Assert.IsNotNull(Off(.9f/.7f),"control: the floor rate's pace passes for Mira's sole, so the check can't tell");
            Assert.IsNull(Off(profile.walkSpeed),Off(profile.walkSpeed));
        }

        /// <summary>The walk the Walk state plays (a hero's own Walk, a townsperson's shared UAL take), played as that state plays it (a hero's places the feet by IK, Mira's doesn't),
        /// and how fast its planted toe travels back through the central stance.</summary>
        static float Walk(GameObject prefab)
        {
            var body=Object.Instantiate(prefab);
            try
            {
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);Object.DestroyImmediate(body.GetComponent<GeneratedAnimator>());
                var rig=body.GetComponentInChildren<Animator>();var controller=rig.runtimeAnimatorController as AnimatorController;
                var state=controller!=null?controller.layers[0].stateMachine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Walk"):null;
                var take=state?.motion as AnimationClip??rig.runtimeAnimatorController.animationClips.First(c=>c.name=="Walk");
                rig.runtimeAnimatorController=null;var toes=new[]{new Toe(rig,true),new Toe(rig,false)};
                var graph=PlayableGraph.Create("civilian sole speed");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                try
                {
                    var play=AnimationClipPlayable.Create(graph,take);play.SetApplyFootIK(state==null||state.iKOnFeet);AnimationPlayableOutput.Create(graph,"pose",rig).SetSourcePlayable(play);graph.Play();
                    float Z(int side,float phase){play.SetTime(Mathf.Repeat(phase+side*.5f,1)*take.length);graph.Evaluate(0);return toes[side].Point().z;}
                    float travelled=0;for(int side=0;side<2;side++)travelled+=Z(side,.2f)-Z(side,.4f);
                    return travelled*.5f/(.2f*take.length);
                }
                finally{graph.Destroy();foreach(var toe in toes)toe.Dispose();}
            }
            finally{Object.DestroyImmediate(body);}
        }
        /// <summary>The front quarter of a foot's lowest vertices, chosen in bind pose and baked again in each pose (IdraBodyTests' reading).</summary>
        sealed class Toe
        {
            sealed class Part{public SkinnedMeshRenderer renderer;public Mesh mesh=new();public List<Vector3> vertices=new();public int[] indices;}
            readonly List<Part> parts=new();
            public Toe(Animator rig,bool left)
            {
                var foot=rig.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);var other=rig.GetBoneTransform(left?HumanBodyBones.RightFoot:HumanBodyBones.LeftFoot);
                var candidates=new List<(Part part,int index,Vector3 point)>();
                foreach(var renderer in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var part=new Part{renderer=renderer};parts.Add(part);renderer.BakeMesh(part.mesh);part.mesh.GetVertices(part.vertices);
                    for(int i=0;i<part.vertices.Count;i++){var p=renderer.transform.TransformPoint(part.vertices[i]);if(Vector3.Distance(p,foot.position)<.48f&&Vector3.Dot(p-(foot.position+other.position)*.5f,foot.position-other.position)>0)candidates.Add((part,i,p));}
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
