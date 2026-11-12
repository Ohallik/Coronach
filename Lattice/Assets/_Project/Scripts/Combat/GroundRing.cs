using UnityEngine;

namespace Lattice.Combat
{
    // Place the bright crest of the owned circle_02 particle at the gameplay
    // boundary. Its padded square is not the damage diameter. Three radial
    // rows retain a soft, fixed-width edge without stretching that padding.
    public sealed class GroundRing:MonoBehaviour
    {
        /// <summary>What the party's own rings are drawn in: pale cream or, for Sela, the cold cyan of her shots. Orange, red and purple on the floor mean danger, and now that a marking is mostly its rim
        /// a Pulse ring in the danger's own orange looked like one.</summary>
        public static readonly Color Friendly=new(1.1f,1.06f,.95f);
        const int Sides=64;
        const float Crest=.5546875f;
        /// <summary>How finely the ground under the ring is read along each spoke, and the steepest of it the ring follows, as a marking does (D217): over that it stops at the foot.</summary>
        const float GroundStep=.7f,FollowedGrade=.7f;
        readonly float[] extents=new float[Sides];
        /// <summary>The ground's height along each spoke, from the middle out, a sample every <see cref="GroundStep"/>; empty in flight, where the ring lies on its plane.</summary>
        float[,] ground;int groundSamples;
        readonly Vector3[] vertices=new Vector3[Sides*3];
        readonly Vector2[] uv=new Vector2[Sides*3];
        MaterialPropertyBlock tint;
        Mesh surface;MeshRenderer ring;float height;
        public void Initialize(float maximumRadius,float planeHeight=.12f,float coverHeight=.75f)
        {
            height=planeHeight;tint=new MaterialPropertyBlock();
            ring=gameObject.AddComponent<MeshRenderer>();ring.sharedMaterial=Resources.Load<Material>("Effects/circle_02");
            ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var triangles=new int[Sides*12];
            for(int row=0;row<2;row++)for(int i=0;i<Sides;i++)
            {
                int a=row*Sides+i,b=row*Sides+(i+1)%Sides,c=a+Sides,d=b+Sides,k=(row*Sides+i)*6;
                triangles[k]=a;triangles[k+1]=b;triangles[k+2]=c;triangles[k+3]=b;triangles[k+4]=d;triangles[k+5]=c;
            }
            surface=new Mesh{name="Cover clipped particle ring"};surface.MarkDynamic();surface.vertices=vertices;surface.triangles=triangles;
            gameObject.AddComponent<MeshFilter>().sharedMesh=surface;
            var center=transform.position+Vector3.up*coverHeight;
            for(int i=0;i<Sides;i++)
            {
                float angle=i*Mathf.PI*2/Sides;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                extents[i]=CombatCover.Sweep(center,center+direction*(maximumRadius+.3f),.01f,out var hit)?Mathf.Max(0,hit.distance-.03f):maximumRadius+.3f;
            }
            if(planeHeight>0&&(Lattice.Core.ZoneController.Current==null||!Lattice.Core.ZoneController.Current.Flight))FollowGround(maximumRadius+.3f);
        }
        static readonly RaycastHit[] Hits=new RaycastHit[8];
        /// <summary>The ring lies on the ground it crosses, not on one flat plane at the hero's feet: on a plane, any rise in reach hid it, and by the slope beside the Burrower the hero's Pulse
        /// lost about the whole of its far half. Every other spoke is read and the ones between take their neighbours' mean, which halves the cost for a ring that lives under half a second.</summary>
        void FollowGround(float reach)
        {
            groundSamples=Mathf.CeilToInt(reach/GroundStep)+1;ground=new float[Sides,groundSamples];
            float origin=transform.position.y;
            for(int i=0;i<Sides;i+=2)
            {
                float angle=i*Mathf.PI*2/Sides;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float level=origin;
                for(int k=0;k<groundSamples;k++)
                {
                    var at=transform.TransformPoint(direction*(k*GroundStep));
                    if(Floor(at,level,k==0?.6f:GroundStep*FollowedGrade+.15f,out float y))level=y;
                    // Nothing under its own middle (cast mid-air over a gap): it keeps its plane, as it always did.
                    else if(k==0){for(;k<groundSamples;k++)ground[i,k]=0;break;}
                    // Past the steepest it follows the ring stops at the foot, as a wall stops it.
                    else{extents[i]=Mathf.Min(extents[i],Mathf.Max(0,(k-.5f)*GroundStep));for(;k<groundSamples;k++)ground[i,k]=level-origin;break;}
                    ground[i,k]=level-origin;
                }
            }
            for(int i=1;i<Sides;i+=2)
            {
                int a=i-1,b=(i+1)%Sides;extents[i]=Mathf.Min(extents[i],Mathf.Min(extents[a],extents[b]));
                for(int k=0;k<groundSamples;k++)ground[i,k]=(ground[a,k]+ground[b,k])*.5f;
            }
        }
        /// <summary>The floor nearest a level, no higher than a step over it: a crate's top or a ceiling isn't the floor. A body isn't ground.</summary>
        static bool Floor(Vector3 at,float level,float most,out float height)
        {
            height=level;float nearest=float.PositiveInfinity;
            int count=Physics.RaycastNonAlloc(new Vector3(at.x,level+3,at.z),Vector3.down,Hits,8,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider is CharacterController||collider.GetComponentInParent<Health>()!=null)continue;
                float y=Hits[i].point.y,off=Mathf.Abs(y-level);
                if(y<=level+most&&off<nearest){nearest=off;height=y;}
            }
            return nearest<float.PositiveInfinity;
        }
        float GroundAt(int side,float distance)
        {
            if(ground==null)return 0;
            float k=Mathf.Clamp(distance/GroundStep,0,groundSamples-1);int below=Mathf.Min((int)k,groundSamples-2);
            return groundSamples<2?ground[side,0]:Mathf.Lerp(ground[side,below],ground[side,below+1],k-below);
        }
        public void Draw(float radius,Color color)
        {
            float inner=Mathf.Max(0,radius-.22f);
            for(int row=0;row<3;row++)for(int i=0;i<Sides;i++)
            {
                float angle=i*Mathf.PI*2/Sides;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float distance=Mathf.Min(row==0?inner:row==1?radius:radius+.3f,extents[i]);
                int vertex=row*Sides+i;vertices[vertex]=direction*distance+Vector3.up*(height+GroundAt(i,distance));
                float sample=distance<=inner?.5f*distance/Mathf.Max(.001f,inner):distance<=radius?
                    Mathf.Lerp(.5f,Crest,Mathf.InverseLerp(inner,radius,distance)):Mathf.Lerp(Crest,.75f,(distance-radius)/.3f);
                uv[vertex]=Vector2.one*.5f+new Vector2(direction.x,direction.z)*(sample*.5f);
            }
            surface.vertices=vertices;surface.uv=uv;surface.RecalculateBounds();
            tint.SetColor("_BaseColor",color);ring.SetPropertyBlock(tint);
        }
        void OnDestroy(){if(surface!=null)Destroy(surface);}
    }
}
