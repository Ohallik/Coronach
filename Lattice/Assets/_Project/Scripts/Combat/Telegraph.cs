using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>
    /// A marked area on the ground (D198): the exact shape a boss attack will strike, filling from its origin as the tell
    /// runs, then flashing as it lands. The same shape answers <see cref="Contains"/>, so what the player sees is what is
    /// hit: a hero standing outside the marking is never struck by it. Shapes follow the ground they lie on (sampled
    /// once, or again when the marking moves); in flight they lie flat under the ships.
    /// </summary>
    public sealed class Telegraph:MonoBehaviour
    {
        public enum Kind{Circle,Ring,Cone,Line}
        public static readonly List<Telegraph> Live=new();
        public static readonly Color Threat=new(1f,.42f,.1f,1),Heavy=new(1f,.16f,.14f,1),Stagger=new(.72f,.4f,1f,1),Trail=new(.86f,.72f,.5f,1);
        /// <summary>Generous by a body's radius: a hero whose capsule overlaps the edge is inside.</summary>
        public const float BodyAllowance=.25f;
        const float Lift=.06f,FlightHeight=.15f;

        public Kind Shape{get;private set;}
        public Vector3 Origin{get;private set;}
        public Vector3 Forward{get;private set;}=Vector3.forward;
        public float Radius{get;private set;}
        public float Inner{get;private set;}
        public float Angle{get;private set;}=360;
        public float Length{get;private set;}
        public float Width{get;private set;}
        public float Duration{get;private set;}
        public float Elapsed{get;private set;}
        public float Remaining=>Mathf.Max(0,Duration-Elapsed);
        public bool Charged=>Elapsed>=Duration;
        public bool Released{get;private set;}
        /// <summary>A marking held at full charge (an expanding band, a burrow trail) instead of filling.</summary>
        public bool Steady;

        Mesh mesh;MeshRenderer view;MaterialPropertyBlock block;Color color;
        float releasedAt=-1,cancelledAt=-1;bool ownsMesh;
        static Material shared;
        static readonly RaycastHit[] Hits=new RaycastHit[8];
        static readonly List<Vector3> Vertices=new();static readonly List<Vector2> Uvs=new(),Scales=new();static readonly List<int> Triangles=new();
        static readonly int FillId=Shader.PropertyToID("_Fill"),FlashId=Shader.PropertyToID("_Flash"),FadeId=Shader.PropertyToID("_Fade"),
            ColorId=Shader.PropertyToID("_BaseColor"),SidesId=Shader.PropertyToID("_SideEdges"),InnerId=Shader.PropertyToID("_InnerEdge");

        public static Telegraph Circle(Vector3 center,float radius,float duration,Color color)=>
            Create(Kind.Circle,center,Vector3.forward,radius,0,360,0,0,duration,color);
        public static Telegraph Ring(Vector3 center,float inner,float outer,float duration,Color color)=>
            Create(Kind.Ring,center,Vector3.forward,outer,inner,360,0,0,duration,color);
        public static Telegraph Cone(Vector3 origin,Vector3 forward,float radius,float angle,float duration,Color color)=>
            Create(Kind.Cone,origin,forward,radius,0,Mathf.Clamp(angle,5,360),0,0,duration,color);
        public static Telegraph Line(Vector3 origin,Vector3 direction,float length,float width,float duration,Color color)=>
            Create(Kind.Line,origin,direction,0,0,0,length,width,duration,color);

        static Telegraph Create(Kind kind,Vector3 origin,Vector3 forward,float radius,float inner,float angle,float length,float width,float duration,Color color)
        {
            var go=new GameObject("Telegraph "+kind);
            var marking=go.AddComponent<Telegraph>();
            forward.y=0;if(forward.sqrMagnitude<1e-4f)forward=Vector3.forward;
            marking.Shape=kind;marking.Origin=origin;marking.Forward=forward.normalized;marking.Radius=radius;marking.Inner=inner;marking.Angle=angle;
            marking.Length=length;marking.Width=width;marking.Duration=Mathf.Max(.01f,duration);marking.color=color;
            marking.Build();Live.Add(marking);
            return marking;
        }

        void OnDestroy(){Live.Remove(this);if(ownsMesh&&mesh!=null)Destroy(mesh);}

        /// <summary>Planar test against the marked shape, including <see cref="BodyAllowance"/>.</summary>
        public bool Contains(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;float distance=offset.magnitude;
            switch(Shape)
            {
                case Kind.Circle:return distance<=Radius+BodyAllowance;
                case Kind.Ring:return distance>=Inner-BodyAllowance&&distance<=Radius+BodyAllowance;
                case Kind.Cone:
                    if(distance>Radius+BodyAllowance)return false;
                    if(Angle>=359||distance<.5f)return true;
                    return Vector3.Angle(Forward,offset)<=Angle*.5f+Mathf.Rad2Deg*BodyAllowance/Mathf.Max(.5f,distance);
                default:
                    float along=Vector3.Dot(offset,Forward);
                    float across=Mathf.Abs(Vector3.Dot(offset,Vector3.Cross(Vector3.up,Forward)));
                    return along>=-BodyAllowance&&along<=Length+BodyAllowance&&across<=Width*.5f+BodyAllowance;
            }
        }

        /// <summary>Move the marking (a trail following a hero) and lay it on the ground again.</summary>
        public void MoveTo(Vector3 origin){Origin=origin;Build();}
        /// <summary>Reshape an expanding band.</summary>
        public void SetRing(float inner,float outer){Inner=Mathf.Max(0,inner);Radius=Mathf.Max(Inner+.05f,outer);Build();}
        /// <summary>The strike lands: a short flash, then the marking fades away.</summary>
        public void Release(){if(Released||cancelledAt>=0)return;Released=true;releasedAt=Time.time;}
        /// <summary>The attack was interrupted (a break, a death): the marking fades without striking.</summary>
        public void Cancel(){if(cancelledAt>=0)return;cancelledAt=Time.time;}

        public static void CancelAll(Object owner)
        {
            for(int i=Live.Count-1;i>=0;i--)if(Live[i]!=null&&Live[i].owner==owner)Live[i].Cancel();
        }
        public Object owner;

        void Update()
        {
            if(!GameTime.Paused)Elapsed+=Time.deltaTime;
            float fill=Steady?1:Mathf.Clamp01(Elapsed/Duration),flash=0,fade=1;
            if(releasedAt>=0)
            {
                float age=Time.time-releasedAt;flash=Mathf.Clamp01(1-age/.12f);fade=Mathf.Clamp01(1-(age-.08f)/.22f);fill=1;
                if(age>.32f){Destroy(gameObject);return;}
            }
            else if(cancelledAt>=0)
            {
                float age=Time.time-cancelledAt;fade=Mathf.Clamp01(1-age/.15f);
                if(age>.16f){Destroy(gameObject);return;}
            }
            else fade=Mathf.Clamp01(Elapsed/.06f);
            block.SetFloat(FillId,fill);block.SetFloat(FlashId,flash);block.SetFloat(FadeId,fade);view.SetPropertyBlock(block);
        }

        void Build()
        {
            if(mesh==null){mesh=new Mesh{name="telegraph"};mesh.MarkDynamic();ownsMesh=true;}
            if(view==null)
            {
                gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
                view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
                if(shared==null)
                {
                    var shader=Resources.Load<Shader>("Effects/Telegraph");
                    shared=new Material(shader!=null?shader:Shader.Find("Sprites/Default")){name="telegraph (runtime)"};
                }
                view.sharedMaterial=shared;block=new MaterialPropertyBlock();
                block.SetColor(ColorId,color);
                block.SetFloat(SidesId,Shape==Kind.Line||Shape==Kind.Cone&&Angle<359?1:0);
                block.SetFloat(InnerId,Shape==Kind.Ring?1:0);
            }
            // Moving markings rebuild every frame: reuse the buffers instead of allocating.
            var vertices=Vertices;var uv=Uvs;var scale=Scales;var triangles=Triangles;vertices.Clear();uv.Clear();scale.Clear();triangles.Clear();
            bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            int columns,rows;
            switch(Shape)
            {
                case Kind.Line:
                {
                    rows=Mathf.Clamp(Mathf.CeilToInt(Length/1.2f),2,40);columns=3;
                    var right=Vector3.Cross(Vector3.up,Forward);
                    for(int r=0;r<=rows;r++)for(int c=0;c<=columns;c++)
                    {
                        float u=r/(float)rows,v=c/(float)columns;
                        vertices.Add(Ground(Origin+Forward*(u*Length)+right*((v-.5f)*Width),flight));uv.Add(new Vector2(u,v));scale.Add(new Vector2(Length,Width));
                    }
                    break;
                }
                default:
                {
                    float sweep=Shape==Kind.Cone?Angle:360;
                    float outer=Radius,inner=Shape==Kind.Ring?Inner:0;
                    // About 0.6 m of arc per segment: a small trail stays light, a wide breath stays round.
                    columns=Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*outer/.6f),Shape==Kind.Cone?6:16,64);
                    rows=Mathf.Clamp(Mathf.CeilToInt((outer-inner)/1.4f),1,12);
                    float start=-sweep*.5f;
                    for(int r=0;r<=rows;r++)for(int c=0;c<=columns;c++)
                    {
                        float u=r/(float)rows,v=c/(float)columns;float radius=Mathf.Lerp(inner,outer,u);
                        var direction=Quaternion.Euler(0,start+v*sweep,0)*Forward;
                        vertices.Add(Ground(Origin+direction*radius,flight));uv.Add(new Vector2(u,v));
                        scale.Add(new Vector2(Mathf.Max(.01f,outer-inner),Mathf.Max(.01f,outer*sweep*Mathf.Deg2Rad)));
                    }
                    break;
                }
            }
            for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)
            {
                int a=r*(columns+1)+c,b=a+1,d=a+columns+1,e=d+1;
                triangles.Add(a);triangles.Add(d);triangles.Add(b);triangles.Add(b);triangles.Add(d);triangles.Add(e);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,scale);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        }

        Vector3 Ground(Vector3 point,bool flight)
        {
            if(flight)return new Vector3(point.x,FlightHeight,point.z);
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,Origin.y+3f,point.z),Vector3.down,Hits,8f,~0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity;
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider is CharacterController||collider.GetComponentInParent<Health>()!=null)continue;
                // The highest walkable surface below the marking's own level (a floor, not a ceiling above it).
                if(Hits[i].point.y<=Origin.y+2.2f&&Hits[i].point.y>best)best=Hits[i].point.y;
            }
            return new Vector3(point.x,(best>float.NegativeInfinity?best:Origin.y)+Lift,point.z);
        }
    }
}
