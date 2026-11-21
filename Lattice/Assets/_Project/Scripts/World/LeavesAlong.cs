using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Drawn things that leave along a path once a flag is set: the young of Sorrel's nursery going up their mother's road after her (chapter 10, docs/maps/SorrelSecondSky.md). From
    /// <see cref="after"/> seconds past the flag this object moves through <see cref="path"/> (world points, from where it stands) over <see cref="seconds"/>, and is gone at the end. A zone
    /// that opens with the flag already set opens with them gone, and nothing plays. Nothing it moves is solid, so nobody is pushed; a pause holds it.
    /// </summary>
    public sealed class LeavesAlong:MonoBehaviour
    {
        public string flag;public Vector3[] path=System.Array.Empty<Vector3>();public float after=3,seconds=9;
        Vector3 home;float clock=-1;bool unsetSeen;
        void Start()
        {
            home=transform.position;var services=GameServices.Current;
            if(services!=null&&!string.IsNullOrEmpty(flag)&&services.Flags.GetBool(flag))gameObject.SetActive(false);
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null||string.IsNullOrEmpty(flag)||GameTime.Paused)return;
            if(!services.Flags.GetBool(flag)){unsetSeen=true;clock=-1;return;}
            if(!unsetSeen){gameObject.SetActive(false);return;}
            if(clock<0){clock=0;Debug.Log("LEAVES_ALONG "+name);}
            clock+=Time.deltaTime;float t=Mathf.Clamp01((clock-after)/Mathf.Max(.1f,seconds));
            transform.position=At(t);
            if(t>=1){Debug.Log("LEAVES_ALONG_GONE "+name);gameObject.SetActive(false);}
        }
        /// <summary>Where it stands a share <paramref name="t"/> of the way along: from home through each point, eased in.</summary>
        public Vector3 At(float t)
        {
            if(path==null||path.Length==0)return home;
            float eased=t*t*(3-2*t)*.5f+t*.5f,legs=path.Length,along=eased*legs;int leg=Mathf.Min(path.Length-1,Mathf.FloorToInt(along));
            var from=leg==0?home:path[leg-1];return Vector3.Lerp(from,path[leg],along-leg);
        }
    }
}
