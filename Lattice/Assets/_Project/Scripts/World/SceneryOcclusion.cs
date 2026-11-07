using System.Collections.Generic;
using Lattice.Combat;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>The ground camera flies high behind the hero over a town of tall things. Anything tall that comes between it and
    /// the hero opens a hero-sized hole; anything passing right under the lens fades to a ghost as a whole, or a street arch
    /// fills the screen with bricks. The shader's occluder fade does the drawing; the builder enables it on these materials.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class SceneryOcclusion:MonoBehaviour
    {
        const float UnderLens=8,FadeSpeed=6;
        /// <summary>Only what really comes up to the lens fades whole. Low things passing under it (a home, a tower lying on its side) left dotted silhouettes across half the floor,
        /// with their block outlines and their shadows, right where the party walks and fights.</summary>
        const float LensClearance=4;
        /// <summary>What does reach the lens goes altogether, every pass and its shadow: at a full fade the screen door still keeps 3 cells in 16, a huge dotted silhouette over the floor.
        /// Past 1 the shadow goes too. A hole for the hero stops at 1 and keeps the body and the shadow round it. Coming back it eases in over about half a second rather than popping solid.</summary>
        const float Gone=1.25f,ComeBack=.35f;
        /// <summary>The screen door opens all the way, so the hole is a clean window with a soft rim and the hero stands in it whole, not in a bubble of dots. A boss keeps more of its body and sets its own.</summary>
        const float OpenAllTheWay=1;
        static readonly int Depth=Shader.PropertyToID("_OccFadeDepth");
        static readonly int Fade=Shader.PropertyToID("_OccFade"),Ellipse=Shader.PropertyToID("_OccFadeEllipse");
        static readonly Vector4 WholeScreen=new(.5f,.5f,4,4);
        static readonly List<SceneryOcclusion> all=new();
        static int updated=-1;
        static MaterialPropertyBlock block;
        Renderer[] bodies;Bounds bounds;float amount,whole;
        /// <summary>A gateway's way through, in its own space. A hero seen through it isn't behind anything: going by the arch's whole box bit a hole in the arch around someone
        /// standing in plain view in its opening.</summary>
        public Bounds opening;
        void Awake()
        {
            bodies=GetComponentsInChildren<Renderer>(true);bounds=new Bounds(transform.position,Vector3.zero);
            foreach(var body in bodies)if(body is MeshRenderer or SkinnedMeshRenderer)bounds.Encapsulate(body.bounds);
        }
        void OnEnable()=>all.Add(this);
        void OnDisable(){all.Remove(this);if(amount>0){amount=0;whole=0;Write(Vector4.zero);}}
        void LateUpdate()
        {
            if(updated==Time.frameCount)return;updated=Time.frameCount;
            var camera=Camera.main;var hero=PartyController.Current!=null?PartyController.Current.Active:null;
            if(camera==null||hero==null)return;
            bool active=!hero.flight;
            var center=hero.transform.position+Vector3.up*.95f;var eye=camera.transform.position;
            var view=camera.WorldToViewportPoint(center);
            var ellipse=new Vector4(view.x,view.y,Mathf.Abs(camera.WorldToViewportPoint(center+camera.transform.right*.95f).x-view.x),Mathf.Abs(camera.WorldToViewportPoint(center+camera.transform.up*1.45f).y-view.y));
            var ray=new Ray(eye,center-eye);float distance=Vector3.Distance(eye,center);float step=Time.unscaledDeltaTime*FadeSpeed;
            foreach(var scenery in all)scenery.Refresh(active,eye,ray,distance,ellipse,step);
        }
        void Refresh(bool active,Vector3 eye,Ray ray,float distance,Vector4 ellipse,float step)
        {
            bool under=active&&bounds.max.y>eye.y-LensClearance&&bounds.SqrDistance(eye)<UnderLens*UnderLens;
            bool between=active&&!under&&bounds.IntersectRay(ray,out float entry)&&entry<distance-.15f&&!SeenThroughOpening(eye,ray.GetPoint(distance));
            float target=under||between?1:0;
            if(amount==0&&target==0)return;
            // The mask eases too: swapping from the whole body to the hero's hole mid-fade popped the rest solid in one frame.
            if(amount==0)whole=under?1:0;
            else if(under)whole=Mathf.MoveTowards(whole,1,step);
            else if(between)whole=Mathf.MoveTowards(whole,0,step);
            amount=Mathf.MoveTowards(amount,target,target<amount?step*ComeBack:step);
            Write(Vector4.Lerp(ellipse,WholeScreen,whole*whole));
        }
        bool SeenThroughOpening(Vector3 eye,Vector3 hero)
        {
            if(opening.size.sqrMagnitude<=0)return false;
            Vector3 a=transform.InverseTransformPoint(eye),b=transform.InverseTransformPoint(hero);float depth=b.z-a.z;
            if(Mathf.Abs(depth)<.001f)return false;
            for(int face=0;face<2;face++)
            {
                var at=Vector3.Lerp(a,b,((face==0?opening.min.z:opening.max.z)-a.z)/depth);
                if(Mathf.Abs(at.x-opening.center.x)>opening.extents.x||at.y<opening.min.y||at.y>opening.max.y)return false;
            }
            return true;
        }
        void Write(Vector4 ellipse)
        {
            block??=new MaterialPropertyBlock();
            foreach(var body in bodies)
            {
                if(body==null||body is not MeshRenderer&&body is not SkinnedMeshRenderer)continue;
                body.GetPropertyBlock(block);block.SetFloat(Fade,amount*Mathf.Lerp(1,Gone,whole));block.SetFloat(Depth,OpenAllTheWay);block.SetVector(Ellipse,ellipse);body.SetPropertyBlock(block);
            }
        }
    }
}
