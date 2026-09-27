using UnityEngine;

namespace Lattice.Combat
{
    // Place the bright crest of the owned circle_02 particle at the gameplay
    // boundary. Its padded square is not the damage diameter. Three radial
    // rows retain a soft, fixed-width edge without stretching that padding.
    public sealed class GroundRing:MonoBehaviour
    {
        const int Sides=64;
        const float Crest=.5546875f;
        readonly float[] extents=new float[Sides];
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
        }
        public void Draw(float radius,Color color)
        {
            float inner=Mathf.Max(0,radius-.22f);
            for(int row=0;row<3;row++)for(int i=0;i<Sides;i++)
            {
                float angle=i*Mathf.PI*2/Sides;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                float distance=Mathf.Min(row==0?inner:row==1?radius:radius+.3f,extents[i]);
                int vertex=row*Sides+i;vertices[vertex]=direction*distance+Vector3.up*height;
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
