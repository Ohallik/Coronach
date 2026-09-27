using UnityEngine;

namespace Lattice.Combat
{
    // The bright texture stripe lies on the actual gameplay boundary.
    public sealed class FlightEnergyArc:MonoBehaviour
    {
        const int Sides=64;
        LineRenderer edge;Material material;
        public static FlightEnergyArc Create(string label)
        {
            var go=new GameObject(label,typeof(FlightEnergyArc));var arc=go.GetComponent<FlightEnergyArc>();
            arc.edge=go.AddComponent<LineRenderer>();arc.edge.useWorldSpace=false;arc.edge.positionCount=Sides+1;
            arc.edge.startWidth=arc.edge.endWidth=.09f;arc.edge.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            arc.material=new Material(Resources.Load<Shader>("Effects/FlightEnergy"));
            arc.material.SetTexture("_BaseMap",Resources.Load<Material>("Effects/flare_01").GetTexture("_BaseMap"));
            arc.material.SetVector("_UvTransform",new Vector4(0,.2f,.5f,.4f));arc.material.SetFloat("_Intensity",1.6f);arc.edge.sharedMaterial=arc.material;
            return arc;
        }
        public void Draw(float radius,float halfAngle,Color color,bool clip)
        {
            edge.startColor=edge.endColor=color;
            for(int i=0;i<=Sides;i++)
            {
                float angle=Mathf.Lerp(-halfAngle,halfAngle,(float)i/Sides)*Mathf.Deg2Rad;
                var local=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*radius;
                var end=transform.TransformPoint(local);
                if(clip&&CombatCover.Sweep(transform.position,end,.01f,out var hit))
                    local=local.normalized*Mathf.Max(0,hit.distance-.025f);
                edge.SetPosition(i,local);
            }
        }
        void OnDestroy(){if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
