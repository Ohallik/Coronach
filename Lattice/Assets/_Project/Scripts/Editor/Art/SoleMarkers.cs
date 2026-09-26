using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Contact probes from the visible generated sole, independently of
    /// the ankle's anatomical origin. Construct in bind pose, evaluate in pose.</summary>
    public sealed class SoleMarkers
    {
        sealed class Skin
        {
            public Vector3[] vertices;public BoneWeight[] weights;
            public Matrix4x4[] bind;public Transform[] bones;
            public Skin(SkinnedMeshRenderer renderer)
            {var mesh=renderer.sharedMesh;vertices=mesh.vertices;weights=mesh.boneWeights;bind=mesh.bindposes;bones=renderer.bones;}
        }
        readonly List<(Skin skin,int vertex)> heel=new(),toe=new();
        public SoleMarkers(Animator animator,bool left)
        {
            Transform ankle=animator.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
            Transform ball=animator.GetBoneTransform(left?HumanBodyBones.LeftToes:HumanBodyBones.RightToes);
            var candidates=new List<(Skin skin,int vertex,Vector3 point)>();
            foreach(var renderer in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(renderer.sharedMesh==null)continue;
                var skin=new Skin(renderer);var weights=skin.weights;var bones=skin.bones;
                for(int i=0;i<weights.Length;i++)
                {
                    var w=weights[i];
                    float Total(int index,float weight)=>bones[index]==ankle||bones[index]==ball?weight:0;
                    if(Total(w.boneIndex0,w.weight0)+Total(w.boneIndex1,w.weight1)+Total(w.boneIndex2,w.weight2)+Total(w.boneIndex3,w.weight3)<.7f)continue;
                    candidates.Add((skin,i,Position(skin,i)));
                }
            }
            if(candidates.Count==0)throw new System.InvalidOperationException("No visible weighted sole for "+animator.name);
            float bottom=candidates.Min(c=>c.point.y);
            var sole=candidates.Where(c=>c.point.y<=bottom+.025f).ToArray();
            float back=sole.Min(c=>c.point.z),front=sole.Max(c=>c.point.z),span=front-back;
            if(span<.08f)throw new System.InvalidOperationException("Sole reference has no heel/toe separation");
            foreach(var c in sole)
            {
                if(c.point.z<back+span*.25f)heel.Add((c.skin,c.vertex));
                if(c.point.z>front-span*.25f)toe.Add((c.skin,c.vertex));
            }
        }
        public Vector3 Heel=>Centroid(heel);
        public Vector3 Toe=>Centroid(toe);
        static Vector3 Centroid(List<(Skin skin,int vertex)> vertices)
        {var result=Vector3.zero;foreach(var v in vertices)result+=Position(v.skin,v.vertex);return result/vertices.Count;}
        static Vector3 Position(Skin skin,int index)
        {
            var w=skin.weights[index];var vertex=skin.vertices[index];var poses=skin.bind;var bones=skin.bones;
            Vector3 Weighted(int bone,float weight)=>weight<=0?Vector3.zero:(bones[bone].localToWorldMatrix*poses[bone]).MultiplyPoint3x4(vertex)*weight;
            return Weighted(w.boneIndex0,w.weight0)+Weighted(w.boneIndex1,w.weight1)+Weighted(w.boneIndex2,w.weight2)+Weighted(w.boneIndex3,w.weight3);
        }
    }
}
