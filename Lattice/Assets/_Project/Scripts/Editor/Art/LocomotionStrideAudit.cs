using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Lattice.EditorTools
{
    /// <summary>Raw anatomical foot trajectories, not a claim of planted-sole
    /// acceptance. Compare these with visible contacts before tuning cadence.</summary>
    public static class LocomotionStrideAudit
    {
        public static void Capture()=>BatchTools.Run(()=>
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/sole-trajectories"));
            if(Directory.Exists(folder))throw new System.InvalidOperationException("Preserve earlier sole evidence");
            Directory.CreateDirectory(folder);
            using var writer=new StreamWriter(Path.Combine(folder,"feet.csv"));
            writer.WriteLine("hero,form,clip,phase,seconds,clipLength,leftX,leftY,leftZ,rightX,rightY,rightZ,leftToeY,rightToeY,rootSpeed,leftHeelX,leftHeelY,leftHeelZ,leftSoleToeX,leftSoleToeY,leftSoleToeZ,rightHeelX,rightHeelY,rightHeelZ,rightSoleToeX,rightSoleToeY,rightSoleToeZ");
            foreach(string hero in new[]{"Taren","Sela"})foreach(bool shaped in new[]{false,true})
            {
                var definition=GameCatalog.Find<CharacterDef>(hero);
                var body=Object.Instantiate(shaped?definition.shaped:definition.natural);
                body.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var driver=body.GetComponent<GeneratedAnimator>();if(driver!=null)Object.DestroyImmediate(driver);
                var animator=body.GetComponentInChildren<Animator>();var clips=animator.runtimeAnimatorController.animationClips;
                var leftSole=new SoleMarkers(animator,true);var rightSole=new SoleMarkers(animator,false);
                animator.runtimeAnimatorController=null;
                try
                {
                    foreach(string state in new[]{"Walk","Run","Sprint"})
                    {
                        var clip=clips.Single(c=>c.name==state);
                        var graph=PlayableGraph.Create("anatomical foot trajectory");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        try
                        {
                            var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(true);
                            AnimationPlayableOutput.Create(graph,"pose",animator).SetSourcePlayable(playable);graph.Play();
                            for(int frame=0;frame<240;frame++)
                            {
                                float phase=frame/240f;playable.SetTime(clip.length*phase);graph.Evaluate(0);
                                var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                                var right=animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                                float lt=animator.GetBoneTransform(HumanBodyBones.LeftToes).position.y,rt=animator.GetBoneTransform(HumanBodyBones.RightToes).position.y;
                                var lh=leftSole.Heel;var ltoe=leftSole.Toe;var rh=rightSole.Heel;var rtoe=rightSole.Toe;
                                writer.WriteLine(System.FormattableString.Invariant($"{hero},{(shaped?"Shaped":"Natural")},{state},{phase:F6},{phase*clip.length:F6},{clip.length:F6},{left.x:F6},{left.y:F6},{left.z:F6},{right.x:F6},{right.y:F6},{right.z:F6},{lt:F6},{rt:F6},{clip.averageSpeed.magnitude:F6},{lh.x:F6},{lh.y:F6},{lh.z:F6},{ltoe.x:F6},{ltoe.y:F6},{ltoe.z:F6},{rh.x:F6},{rh.y:F6},{rh.z:F6},{rtoe.x:F6},{rtoe.y:F6},{rtoe.z:F6}"));
                            }
                        }
                        finally{graph.Destroy();}
                    }
                }
                finally{Object.DestroyImmediate(body);}
            }
            Debug.Log("STRIDE_AUDIT_OK "+folder);
        });
    }
}
