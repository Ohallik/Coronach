using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who lives by the crust (chapter 8's Scoria Flows): on a <see cref="Guide"/>'s body, it holds the guide at a flow's bank until the crust on the way ahead will stay dark long
    /// enough to cross, as anyone who lives there does. Looks along the guide's current leg up to <see cref="ahead"/> metres; while any of it is lit, open, or will light within
    /// <see cref="need"/> seconds, the guide waits where it stands (its component is switched off). Once on crust it never stops: it crosses. Brekk walked behind the party over open
    /// melt before this (the lead's look pass). Logs <c>CRUST_WAIT &lt;name&gt;</c> as it starts to wait.
    /// </summary>
    public sealed class CrustWait:MonoBehaviour
    {
        public Guide guide;
        public float ahead=8,need=3.2f;
        bool waiting;
        public bool Waiting=>waiting;
        void Update()
        {
            if(guide==null)return;var field=CrustField.Current;
            bool hold=field!=null&&field.At(transform.position)==null&&Unsafe(field);
            if(hold&&!waiting)Debug.Log("CRUST_WAIT "+name);
            waiting=hold;guide.enabled=!hold;
        }
        bool Unsafe(CrustField field)
        {
            int stage=guide.Stage;if(stage<0||stage>=guide.legs.Length)return false;var path=guide.legs[stage].path;if(path==null||path.Length<2)return false;
            // From the point of the leg nearest where the guide stands, on along the leg.
            int from=0;float best=float.PositiveInfinity;
            for(int i=0;i<path.Length;i++){float d=(path[i]-transform.position).sqrMagnitude;if(d<best){best=d;from=i;}}
            var at=transform.position;float left=ahead;
            for(int i=from;i<path.Length&&left>0;i++)
            {
                var to=path[i];var run=to-at;run.y=0;float length=run.magnitude;
                for(float d=0;d<=Mathf.Min(length,left);d+=.5f)if(field.SafeFor(at+run.normalized*d)<need)return true;
                left-=length;at=to;
            }
            return false;
        }
    }
}
