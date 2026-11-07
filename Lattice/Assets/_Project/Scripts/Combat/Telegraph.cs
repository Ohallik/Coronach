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
        /// <summary>The steepest ground a marking follows, rise over run, and the most any one step may rise. Sorrel's floor ends in a wall that rises 5 m for every metre: following
        /// the foot of it broke a marking into spikes. Under this it's a slope anyone could walk; past it the marking stops at the wall's foot and goes on, flat, under the hill.</summary>
        const float FollowedGrade=.7f,FollowedStep=2.2f;
        /// <summary>Sides a whole circle needs per square root of its radius so its straight edges stay under about 1.2 cm: a pixel from the ground camera.</summary>
        const float RoundSides=20.3f;

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
        static readonly List<Vector3> Vertices=new(),Scales=new();static readonly List<Vector2> Uvs=new();static readonly List<int> Triangles=new();static readonly List<float> OnFloor=new();
        /// <summary>The row being laid, and the one inside it on the way out from the origin.</summary>
        static List<float> Heights=new(),Before=new();
        static readonly int FillId=Shader.PropertyToID("_Fill"),FlashId=Shader.PropertyToID("_Flash"),FadeId=Shader.PropertyToID("_Fade"),
            ColorId=Shader.PropertyToID("_BaseColor"),SidesId=Shader.PropertyToID("_SideEdges"),InnerId=Shader.PropertyToID("_InnerEdge"),ThroughId=Shader.PropertyToID("_Through"),HatchId=Shader.PropertyToID("_Hatch");

        /// <summary>A light marking uses fewer ground samples: a swarm of ordinary attacks must not cost a frame.</summary>
        public static Telegraph Circle(Vector3 center,float radius,float duration,Color color,bool light=false)=>
            Create(Kind.Circle,center,Vector3.forward,radius,0,360,0,0,duration,color,light);
        public static Telegraph Ring(Vector3 center,float inner,float outer,float duration,Color color)=>
            Create(Kind.Ring,center,Vector3.forward,outer,inner,360,0,0,duration,color);
        public static Telegraph Cone(Vector3 origin,Vector3 forward,float radius,float angle,float duration,Color color,bool light=false)=>
            Create(Kind.Cone,origin,forward,radius,0,Mathf.Clamp(angle,5,360),0,0,duration,color,light);
        public static Telegraph Line(Vector3 origin,Vector3 direction,float length,float width,float duration,Color color,bool light=false)=>
            Create(Kind.Line,origin,direction,0,0,0,length,width,duration,color,light);

        bool light;
        static Telegraph Create(Kind kind,Vector3 origin,Vector3 forward,float radius,float inner,float angle,float length,float width,float duration,Color color,bool light=false)
        {
            var go=new GameObject("Telegraph "+kind);
            var marking=go.AddComponent<Telegraph>();marking.light=light;
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

        /// <summary>The shortest way out of the marking from a point inside it.</summary>
        public Vector3 WayOut(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;
            if(Shape==Kind.Line)
            {
                var across=Vector3.Cross(Vector3.up,Forward);float side=Vector3.Dot(offset,across);
                return side>=0?across:-across;
            }
            if(Shape==Kind.Ring&&offset.magnitude-Inner<Radius-offset.magnitude&&offset.sqrMagnitude>.01f)return -offset.normalized;
            return offset.sqrMagnitude>.01f?offset.normalized:Vector3.right;
        }
        /// <summary>How far this marking reaches: what makes it big enough that the way out can be off screen.</summary>
        public float Span=>Shape==Kind.Line?Length:Radius;
        /// <summary>The nearest point out of the marking from a point inside it: what the player has to be able to see to.</summary>
        public Vector3 EdgeFrom(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;float distance=offset.magnitude;
            var outward=distance>.01f?offset/distance:Forward;
            switch(Shape)
            {
                case Kind.Line:
                {
                    var across=Vector3.Cross(Vector3.up,Forward);float side=Vector3.Dot(offset,across);
                    return point+(side>=0?across:-across)*Mathf.Max(0,Width*.5f-Mathf.Abs(side));
                }
                case Kind.Ring:return distance-Inner<Radius-distance?point-outward*Mathf.Max(0,distance-Inner):point+outward*Mathf.Max(0,Radius-distance);
                case Kind.Cone when Angle<359:
                {
                    // A side of the fan can be nearer than its far edge: half a chamber's nearest way out is the line across it.
                    float off=Vector3.SignedAngle(Forward,offset,Vector3.up),sign=off<0?-1:1,inside=Angle*.5f-Mathf.Abs(off);
                    float toSide=inside<90?distance*Mathf.Sin(inside*Mathf.Deg2Rad):distance;
                    if(toSide<Radius-distance)return point+Vector3.Cross(Vector3.up,Quaternion.Euler(0,sign*Angle*.5f,0)*Forward)*(sign*Mathf.Max(0,toSide));
                    return point+outward*Mathf.Max(0,Radius-distance);
                }
                default:return point+outward*Mathf.Max(0,Radius-distance);
            }
        }
        /// <summary>Move the marking (a trail following a hero) and lay it on the ground again.</summary>
        public void MoveTo(Vector3 origin){Origin=origin;Build();}
        /// <summary>Reshape an expanding band.</summary>
        public void SetRing(float inner,float outer){Inner=Mathf.Max(0,inner);Radius=Mathf.Max(Inner+.05f,outer);Build();}
        /// <summary>The strike lands: a short flash, then the marking fades away. In real time: through a Flash the world runs at a quarter, and a spent ring stayed on the floor for over a
        /// second with the bar already saying OPENING.</summary>
        public void Release(){if(Released||cancelledAt>=0)return;Released=true;releasedAt=Time.unscaledTime;}
        /// <summary>The attack was interrupted (a break, a death): the marking fades without striking.</summary>
        public void Cancel(){if(cancelledAt>=0)return;cancelledAt=Time.unscaledTime;}
        /// <summary>Gone in this frame: a fight that has ended doesn't fade its threats out across the screen that pays out the win.</summary>
        public void Clear(){cancelledAt=Time.unscaledTime;if(view!=null)view.enabled=false;Destroy(gameObject);}

        public static void CancelAll(Object owner)
        {
            for(int i=Live.Count-1;i>=0;i--)if(Live[i]!=null&&Live[i].owner==owner)Live[i].Cancel();
        }
        public static void ClearAll(Object owner)
        {
            for(int i=Live.Count-1;i>=0;i--)if(Live[i]!=null&&Live[i].owner==owner)Live[i].Clear();
        }
        public Object owner;

        void Update()
        {
            // A marking can't outlive what made it: a body dismissed or removed mid-tell never got to cancel its own, and it stayed on the floor for good.
            if(cancelledAt<0&&!Released&&!ReferenceEquals(owner,null)&&owner==null)Cancel();
            if(!GameTime.CombatHeld)Elapsed+=Time.deltaTime;
            float fill=Steady?1:Mathf.Clamp01(Elapsed/Duration),flash=0,fade=1;
            if(releasedAt>=0)
            {
                float age=Time.unscaledTime-releasedAt;flash=Mathf.Clamp01(1-age/.12f);fade=Mathf.Clamp01(1-(age-.08f)/.22f);fill=1;
                if(age>.32f){Destroy(gameObject);return;}
            }
            else if(cancelledAt>=0)
            {
                float age=Time.unscaledTime-cancelledAt;fade=Mathf.Clamp01(1-age/.15f);
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
                block.SetFloat(ThroughId,ZoneController.Current!=null&&ZoneController.Current.Flight?0:1);
                block.SetVector(HatchId,Hatch());
            }
            // Moving markings rebuild every frame: reuse the buffers instead of allocating.
            var vertices=Vertices;var uv=Uvs;var scale=Scales;var triangles=Triangles;vertices.Clear();uv.Clear();scale.Clear();triangles.Clear();
            bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            int columns,rows;
            switch(Shape)
            {
                case Kind.Line:
                {
                    rows=light?Mathf.Clamp(Mathf.CeilToInt(Length/3),1,8):Mathf.Clamp(Mathf.CeilToInt(Length/1.2f),2,40);columns=light?1:3;
                    var right=Vector3.Cross(Vector3.up,Forward);
                    for(int r=0;r<=rows;r++)
                    {
                        float u=r/(float)rows;
                        NewRow();
                        for(int c=0;c<=columns;c++)Ground(Origin+Forward*(u*Length)+right*((c/(float)columns-.5f)*Width),c,r==0,Length/rows,flight);
                        Settle(Width/columns,false);
                        for(int c=0;c<=columns;c++)
                        {
                            float v=c/(float)columns;var point=Origin+Forward*(u*Length)+right*((v-.5f)*Width);point.y=Heights[c]+Lift;
                            vertices.Add(point);uv.Add(new Vector2(u,v));scale.Add(new Vector3(Length,Width,OnFloor[c]));
                        }
                    }
                    break;
                }
                default:
                {
                    float sweep=Shape==Kind.Cone?Angle:360;
                    float outer=Radius,inner=Shape==Kind.Ring?Inner:0;
                    // About 0.6 m of arc per ground sample: a small trail stays light, a wide breath follows the floor.
                    int samples=light?Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*outer/1.2f),Shape==Kind.Cone?4:12,24):Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*outer/.6f),Shape==Kind.Cone?6:16,64);
                    rows=light?1:Mathf.Clamp(Mathf.CeilToInt((outer-inner)/1.4f),1,12);
                    // The rim is finer than the ground it reads: heights carry between the samples, so a circle is round for no more raycasts than it took to be a polygon.
                    int every=Mathf.Max(1,Mathf.CeilToInt(RoundSides*Mathf.Sqrt(outer)*sweep/360/samples));columns=samples*every;
                    float start=-sweep*.5f;
                    for(int r=0;r<=rows;r++)
                    {
                        float u=r/(float)rows,radius=Mathf.Lerp(inner,outer,u);
                        NewRow();
                        for(int s=0;s<=samples;s++)Ground(Origin+Quaternion.Euler(0,start+sweep*s/samples,0)*Forward*radius,s,r==0,(outer-inner)/rows,flight);
                        Settle(radius*sweep*Mathf.Deg2Rad/samples,sweep>=359.9f);
                        for(int c=0;c<=columns;c++)
                        {
                            float v=c/(float)columns;var point=Origin+Quaternion.Euler(0,start+v*sweep,0)*Forward*radius;
                            int sample=c/every;float between=(c%every)/(float)every,floor=sample<samples?Mathf.Lerp(OnFloor[sample],OnFloor[sample+1],between):OnFloor[samples];
                            point.y=(sample<samples?Mathf.Lerp(Heights[sample],Heights[sample+1],between):Heights[samples])+Lift;
                            vertices.Add(point);uv.Add(new Vector2(u,v));
                            // The arc's real length at this radius: a fan's side line keeps its width all the way to the point.
                            scale.Add(new Vector3(Mathf.Max(.01f,outer-inner),Mathf.Max(.01f,radius*sweep*Mathf.Deg2Rad),floor));
                        }
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

        /// <summary>The hatch says marked and the rim says where it ends. Square across a lane its lines were the rungs of a ladder, so there they cross it at a slant; on anything else they run
        /// corner to corner on the fixed camera's screen, where lines at 45 degrees in the world lay almost flat and read as planks. The bigger the area the wider and fainter:
        /// a ring as wide as the chamber was a floor of stripes with a scrap of rim at its edge.</summary>
        Vector4 Hatch()
        {
            var across=Shape==Kind.Line?Quaternion.Euler(0,45,0)*Forward:new Vector3(-.607f,0,.795f);
            float size=Shape==Kind.Line?Mathf.Sqrt(Length*Width):Shape==Kind.Ring?10:Mathf.Sqrt(Mathf.PI*Radius*Radius*Angle/360);
            float thin=Mathf.InverseLerp(8,24,size);
            return new Vector4(across.x,across.z,Mathf.Lerp(.62f,1.6f,thin),Mathf.Lerp(1,.45f,thin));
        }
        static void NewRow(){var swap=Before;Before=Heights;Heights=swap;Heights.Clear();OnFloor.Clear();}
        /// <summary>The ground under one point of a row. Each asks from the height of the point inside it on the way out from the origin, so a marking climbs a real slope however high it goes,
        /// while a crate's top stays further off than the floor under it. Asked from the origin's own level, a slope past 3 m above it was missed and that vertex stayed flat under the hill, beside
        /// another on the wall's foot: the spikes. What's left under a hill is marked, so its rim doesn't show through from there.</summary>
        void Ground(Vector3 point,int index,bool first,float run,bool flight)
        {
            if(flight){Heights.Add(FlightHeight-Lift);OnFloor.Add(1);return;}
            float level=first?Origin.y:Before[index],most=first?FollowedStep:Mathf.Min(FollowedStep,run*FollowedGrade+.15f);
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,level+3f,point.z),Vector3.down,Hits,8f,~0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity,nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider is CharacterController||collider.GetComponentInParent<Health>()!=null)continue;
                // The surface nearest the marking's own level (a floor, not a ceiling above it). The highest used to win:
                // a marking crossing a sealed gate or a crate climbed it, and its outline broke into shards over the top.
                float off=Mathf.Abs(Hits[i].point.y-level);
                if(Hits[i].point.y<=level+most&&off<nearest){nearest=off;best=Hits[i].point.y;}
            }
            bool found=best>float.NegativeInfinity;Heights.Add(found?best:level);OnFloor.Add(found?1:0);
        }
        /// <summary>Along a row nothing stands proud of its neighbours by more than a walkable rise: a vertex that caught the foot of a wall between two that didn't was a spike on the rim.
        /// It only ever lowers, and what it lowers sinks under the ground that raised it. A whole circle's first and last samples are one point: they settle together, or the ring tears open there.</summary>
        static void Settle(float spacing,bool closed)
        {
            float most=spacing*FollowedGrade+.1f;int last=Heights.Count-1;
            for(int pass=closed?2:1;pass>0;pass--)
            {
                for(int i=1;i<=last;i++)Heights[i]=Mathf.Min(Heights[i],Heights[i-1]+most);
                for(int i=last-1;i>=0;i--)Heights[i]=Mathf.Min(Heights[i],Heights[i+1]+most);
                if(closed)Heights[0]=Heights[last]=Mathf.Min(Heights[0],Heights[last]);
            }
        }
    }
}
