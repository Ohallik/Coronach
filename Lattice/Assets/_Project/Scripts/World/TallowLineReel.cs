using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    // The Keeper asked for the port line to be reeled snug. Once the party has seen that its free end holds nothing
    // (`tallow.lineObserved`), the reel winds it in: the line is drawn back along its own path, pulling slightly
    // straighter as it comes, until the fitting rests beside the reel, where the Keeper's return conversation finds it.
    // A save made after the observation restores the wound-in line directly.
    public sealed class TallowLineReel:MonoBehaviour
    {
        public LineRenderer line;
        public Transform fitting;
        public string flag="tallow.lineObserved";
        public float duration=7f,restLength=1.6f,straighten=.35f;
        Vector3[] path;float[] along;float total;Vector3 fittingOffset;float progress=-1;bool initialized;
        public float Progress=>Mathf.Clamp01(progress);
        public bool Reeling=>progress>=0&&progress<1;
        public float SlackLength=>total;

        void Awake()=>Capture();
        void Capture()
        {
            if(initialized||line==null||fitting==null||line.positionCount<2)return;
            path=new Vector3[line.positionCount];line.GetPositions(path);along=new float[path.Length];
            for(int i=1;i<path.Length;i++)along[i]=along[i-1]+Vector3.Distance(path[i-1],path[i]);
            total=along[path.Length-1];fittingOffset=fitting.position-Point(path[path.Length-1]);initialized=true;
        }
        void Start()
        {
            Capture();
            if(initialized&&Observed()){progress=1;Apply(1);}
        }
        void Update()
        {
            if(!initialized||GameTime.Paused)return;
            if(progress<0)
            {
                if(!Observed())return;
                progress=0;
                // A low motor note as the drum starts to wind.
                AudioManager.PlayAt("spaceEngineLow_000",transform,Point(path[0]),.22f,AudioBus.SFX,90,.06f,1.7f);
            }
            if(progress>=1)return;
            progress=Mathf.MoveTowards(progress,1,Time.unscaledDeltaTime/Mathf.Max(.1f,duration));
            Apply(Mathf.SmoothStep(0,1,progress));
            if(progress>=1)AudioManager.PlayAt("impactMetal_000",fitting,fitting.position,.18f,AudioBus.SFX,90);
        }
        bool Observed()=>GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        Vector3 Point(Vector3 local)=>line.useWorldSpace?local:line.transform.TransformPoint(local);

        /// <summary>Line state at reel fraction k (0 slack, 1 wound in): the first part of the original path, as long as
        /// the paid-out line, blended toward the straight chord as it tightens.</summary>
        void Apply(float k)
        {
            float length=Mathf.Lerp(total,Mathf.Min(restLength,total),k);
            int count=1;while(count<path.Length&&along[count]<length)count++;
            var points=new Vector3[count+1];
            for(int i=0;i<count;i++)points[i]=path[i];
            int last=Mathf.Min(count,path.Length-1);float span=along[last]-along[last-1];
            points[count]=span>1e-4f?Vector3.Lerp(path[last-1],path[last],(length-along[last-1])/span):path[last];
            var start=points[0];var end=points[count];
            for(int i=1;i<count;i++)
            {
                float f=along[i]/Mathf.Max(1e-4f,length);
                points[i]=Vector3.Lerp(points[i],Vector3.Lerp(start,end,f),straighten*k);
            }
            line.positionCount=points.Length;line.SetPositions(points);
            fitting.position=Point(end)+fittingOffset*(1-k);
        }
    }
}
