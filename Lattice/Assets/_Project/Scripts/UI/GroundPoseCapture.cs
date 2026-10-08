using System;
using System.Globalization;
using System.IO;
using Lattice.Combat;
using UnityEngine;

namespace Lattice.UI
{
    // Opt-in evaluated geometry, after GroundFeet's LateUpdate. This records
    // the real Animator mixture for both heroes; it never resamples a clip.
    public sealed class GroundPoseCapture:IDisposable
    {
        static readonly HumanBodyBones[] Joints={HumanBodyBones.Hips,HumanBodyBones.Chest,
            HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot};
        public const string VectorColumns="root,forward,velocity,hips,chest,leftHip,leftKnee,leftAnkle,rightHip,rightKnee,rightAnkle";
        readonly string folder;
        readonly StreamWriter writer;
        int samples,rows;
        bool finished;
        public string Failure {get;private set;}
        public GroundPoseCapture(string output)
        {
            folder=output;
            writer=new StreamWriter(Path.Combine(folder,"ground-pose.csv"),false,new System.Text.UTF8Encoding(false),65536);
            writer.Write("sample,frame,elapsed,step,role,hero,form,state,clip,phase,transitioning,delta,grade,requestedDrop,available");
            foreach(string name in VectorColumns.Split(','))foreach(char axis in "XYZ")writer.Write(","+name+axis);
            writer.WriteLine();
        }
        public void Record(int sample,int frame,double elapsed,int step,CombatActor active,CombatActor partner)
        {
            if(finished)throw new InvalidOperationException("Ground pose capture already finished");
            if(sample!=samples)Failure??="Ground pose sample sequence mismatch";
            RecordActor(sample,frame,elapsed,step,"active",active);
            RecordActor(sample,frame,elapsed,step,"partner",partner);
            samples++;
        }
        void RecordActor(int sample,int frame,double elapsed,int step,string role,CombatActor actor)
        {
            var rig=actor!=null?actor.GetComponentInChildren<Animator>():null;
            var driver=actor!=null?actor.GetComponentInChildren<GeneratedAnimator>():null;
            var feet=driver!=null?driver.GetComponent<GroundFeet>():null;
            var form=actor!=null?actor.GetComponent<FormController>():null;
            bool available=actor!=null&&!actor.flight&&actor.motor!=null&&rig!=null&&rig.isHuman&&driver!=null&&feet!=null&&form!=null;
            var missing=new Vector3(float.NaN,float.NaN,float.NaN);
            var points=new Vector3[11];
            points[0]=actor!=null?actor.transform.position:missing;
            points[1]=actor!=null?actor.transform.forward:missing;
            points[2]=actor?.motor!=null?actor.motor.Velocity:missing;
            for(int i=0;i<Joints.Length;i++)
            {
                var joint=rig!=null&&rig.isHuman?rig.GetBoneTransform(Joints[i]):null;
                points[i+3]=joint!=null?joint.position:missing;
                available&=joint!=null;
            }
            if(!available)Failure??="Ground pose unavailable for "+role+" at sample "+sample;
            foreach(var point in points)if(!float.IsFinite(point.x)||!float.IsFinite(point.y)||!float.IsFinite(point.z))
                Failure??="Nonfinite ground pose at sample "+sample;
            writer.Write(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2:R},{3},{4},{5},{6},{7},{8},{9:R},{10},{11:R},{12:R},{13:R},{14}",
                sample,frame,elapsed,step,role,actor!=null?actor.character:"",form!=null?form.Current.ToString():"",
                actor!=null?actor.State.ToString():"",driver!=null?driver.LocomotionAnimation:"",
                rig!=null?rig.GetCurrentAnimatorStateInfo(0).normalizedTime:float.NaN,rig!=null&&rig.IsInTransition(0),
                Time.unscaledDeltaTime,feet!=null?feet.TerrainGrade:float.NaN,feet!=null?feet.RequestedSupportDrop:float.NaN,available));
            foreach(var point in points)writer.Write(string.Format(CultureInfo.InvariantCulture,",{0:R},{1:R},{2:R}",point.x,point.y,point.z));
            writer.WriteLine();rows++;
        }
        [Serializable] sealed class Report
        {
            public int schema=1,samples,rows;
            public bool complete;
            public string failure;
            public string scope="Evaluated world-space bones after GroundFeet; both party roles, all Animator transitions retained. Diagnostic only; no clean timing or visual acceptance.";
        }
        public void Finish()
        {
            if(finished)return;
            writer.Dispose();finished=true;
            if(samples==0)Failure??="Empty ground pose capture";
            File.WriteAllText(Path.Combine(folder,"ground-pose.json"),JsonUtility.ToJson(new Report{
                samples=samples,rows=rows,complete=Failure==null,failure=Failure},true));
        }
        public void Dispose(){writer.Dispose();}
    }
}
