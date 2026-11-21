using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Heat that lives (chapter 8, the Bridle Foundry): a furnace's mouth, a crucible's open top, a vent onto the vein. Its <see cref="glows"/> (emission by property block, set every
    /// frame, so an editor still shows their material's own) and its <see cref="lights"/> swell and fall together over <see cref="period"/> seconds between <see cref="low"/> and
    /// <see cref="high"/> of their brightness, with a little flicker on top; each one out of step with its neighbours by where it stands. Never laid on a floor a hero can stand on:
    /// that heat is a pour's, and it is marked.
    /// </summary>
    public sealed class Breathing:MonoBehaviour
    {
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        public Renderer[] glows=System.Array.Empty<Renderer>();public Light[] lights=System.Array.Empty<Light>();
        public Color colour=new(1.25f,.7f,.3f,1);
        public float period=3.4f,low=.6f,high=1.1f,flicker=.08f,offset;
        float[] lit;MaterialPropertyBlock block;
        /// <summary>How bright it is at a time: its share of the colour and of its lights' intensity.</summary>
        public float At(float time)=>Mathf.Lerp(low,high,.5f+.5f*Mathf.Sin(time/period*Mathf.PI*2+offset))+flicker*(Mathf.PerlinNoise(time*3.1f+offset,offset)-.5f);
        void Start()
        {
            block=new MaterialPropertyBlock();offset+=transform.position.x*.37f+transform.position.z*.11f;
            lit=new float[lights.Length];for(int i=0;i<lights.Length;i++)lit[i]=lights[i]!=null?lights[i].intensity:0;
        }
        void Update()
        {
            float b=At(Time.time);
            foreach(var r in glows){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor(EmissionId,colour*b);r.SetPropertyBlock(block);}
            for(int i=0;i<lights.Length;i++)if(lights[i]!=null)lights[i].intensity=lit[i]*b;
        }
    }
}
