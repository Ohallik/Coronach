using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The young Choir in the Muster's finished Bridle (chapter 8, the Foundry's heart; manifest 8.5). It is still asking to go: it pulls against the cage for <see cref="pull"/> seconds
    /// and eases for <see cref="ease"/> (Idra's "pull, nine; ease, four", which <c>TovraRelease</c> calls on), on its own clock from the zone's opening, so whoever stands at the door
    /// (<c>SelaCagedRoad</c>) sees the cadence before anyone names it. Pulling, the body draws up toward the vent and trembles, the clamps' lamps (<see cref="clamps"/>) go hard and
    /// the light in the cage swells; easing, it sinks and they dim. Once <see cref="freedFlag"/> is set (the release's last line) it hangs still a moment, then rises along
    /// <see cref="rise"/> (up through the vent) over <see cref="riseSeconds"/> and is gone; a save with the flag loads with it gone and the clamps dark. Logs <c>CHOIR_FREED name</c>.
    /// </summary>
    public sealed class CagedChoir:MonoBehaviour
    {
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        public float pull=9,ease=4;
        public string freedFlag="";
        /// <summary>The body (moved and turned as a whole), how far it draws up while pulling, the clamps' lamp renderers, their colour, and the light inside the cage.</summary>
        public Transform body;public float draw=.5f;public Renderer[] clamps=System.Array.Empty<Renderer>();public Color strain=new(.7f,.9f,1.3f,1);public Light inner;public float innerIntensity=2.2f;
        /// <summary>The way out, world points from the body's rest up through the vent; a beat before it goes, and how long it takes.</summary>
        public Vector3[] rise=System.Array.Empty<Vector3>();public float wait=2.4f,riseSeconds=6;

        float started;bool known,freed,gone;float freedAt=-1;Vector3 rest;Quaternion restTurn;MaterialPropertyBlock block;
        public float Period=>pull+ease;
        /// <summary>Whether it is pulling at this many seconds of its own clock (the first <see cref="pull"/> of each period).</summary>
        public bool PullingAt(float time)=>Mathf.Repeat(time,Period)<pull;
        public bool Pulling=>!freed&&PullingAt(GameTime.Now-started);
        public bool Gone=>gone;
        void Start(){started=GameTime.Now;block=new MaterialPropertyBlock();if(body!=null){rest=body.position;restTurn=body.rotation;}}
        void Update()
        {
            var services=GameServices.Current;if(services==null||body==null)return;
            bool set=!string.IsNullOrEmpty(freedFlag)&&services.Flags.GetBool(freedFlag);
            if(!known){known=true;if(set){freed=gone=true;body.gameObject.SetActive(false);Lamps(0);return;}}
            if(gone)return;
            if(set&&!freed){freed=true;freedAt=GameTime.Now;Debug.Log("CHOIR_FREED "+name);}
            if(freed){Go(GameTime.Now-freedAt);return;}
            float t=Mathf.Repeat(GameTime.Now-started,Period);
            // Pulling: it takes up over a second and a half, holds hard and trembles; easing: it lets go over a second and hangs.
            float taut=t<pull?Mathf.SmoothStep(0,1,Mathf.Clamp01(t/1.5f)):1-Mathf.SmoothStep(0,1,Mathf.Clamp01((t-pull)/1f));
            float tremble=t<pull&&t>1.5f?Mathf.Sin(Time.time*31)*.04f+Mathf.Sin(Time.time*17)*.03f:0;
            body.SetPositionAndRotation(rest+Vector3.up*(draw*taut+tremble),restTurn*Quaternion.Euler(-6*taut,0,tremble*20));
            Lamps(taut);
        }
        void Lamps(float amount)
        {
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var r in clamps){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor(EmissionId,strain*(.15f+.85f*amount));r.SetPropertyBlock(block);}
            if(inner!=null)inner.intensity=innerIntensity*(.35f+.65f*amount);
        }
        void Go(float since)
        {
            Lamps(0);
            if(since<wait||rise.Length==0){body.SetPositionAndRotation(rest,restTurn);return;}
            float s=Mathf.SmoothStep(0,1,Mathf.Clamp01((since-wait)/riseSeconds));
            float total=0;for(int i=1;i<rise.Length;i++)total+=Vector3.Distance(rise[i-1],rise[i]);
            float metres=s*total;var at=rise[0];
            for(int i=1;i<rise.Length;i++){float leg=Vector3.Distance(rise[i-1],rise[i]);if(metres<=leg||i==rise.Length-1){at=Vector3.Lerp(rise[i-1],rise[i],leg>0?Mathf.Clamp01(metres/leg):1);break;}metres-=leg;}
            // It turns its head up the way it is going: the first time it has gone anywhere.
            body.SetPositionAndRotation(at,Quaternion.Slerp(restTurn,Quaternion.LookRotation(Vector3.up,restTurn*Vector3.forward),Mathf.Clamp01(s*3)));
            if(inner!=null)inner.intensity=innerIntensity*(1-s);
            if(s>=1){gone=true;body.gameObject.SetActive(false);}
        }
    }
}
