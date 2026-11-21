using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The Skyfields' rule shown before it is said: something near a fibre that falls along it, not to the ground. Ishen's book slides off his crate and falls sideways onto the old
    /// wall the camp's fibre runs along, and lies there on the wall's face (manifest 8.6). It starts as the conversation of <see cref="startsWith"/> opens (a crossing), after
    /// <see cref="delay"/> seconds, which is about when Taren says it is sliding; it slides along <see cref="path"/> slowly, then falls the rest of the way quickly, turning from
    /// <see cref="restTurn"/> to <see cref="landTurn"/>, and lands with a knock. A save with <see cref="fallenFlag"/> has it lying where it landed. Logs <c>FALLS_ALONG name</c>.
    /// </summary>
    public sealed class FallsAlong:MonoBehaviour
    {
        public DiscoveryPoint startsWith;
        public string fallenFlag;
        /// <summary>World points, from where it rests to where it lands: the first leg is the slide, the rest the fall.</summary>
        public Vector3[] path=System.Array.Empty<Vector3>();
        public Vector3 restTurn,landTurn;
        public float delay=2.6f,slide=1.4f,fall=.45f;
        public string sound="impactMetal_000";
        public bool Fallen{get;private set;}
        bool known;
        void OnEnable()=>DiscoveryPoint.DialogueRequested+=Asked;
        void OnDisable()=>DiscoveryPoint.DialogueRequested-=Asked;
        void Asked(DiscoveryPoint point){if(point==startsWith&&!Fallen&&isActiveAndEnabled)StartCoroutine(Fall());}
        void Update()
        {
            if(known||GameServices.Current==null||path.Length<2)return;known=true;
            if(!string.IsNullOrEmpty(fallenFlag)&&GameServices.Current.Flags.GetBool(fallenFlag)){Fallen=true;transform.SetPositionAndRotation(path[^1],Quaternion.Euler(landTurn));}
            else transform.SetPositionAndRotation(path[0],Quaternion.Euler(restTurn));
        }
        IEnumerator Fall()
        {
            Fallen=true;
            for(float t=0;t<delay;t+=Time.unscaledDeltaTime)yield return null;
            Debug.Log("FALLS_ALONG "+name);
            var rest=Quaternion.Euler(restTurn);var land=Quaternion.Euler(landTurn);
            // The slide: it creeps to the crate's edge as if the crate were tilting under it.
            for(float t=0;t<slide;t+=Time.unscaledDeltaTime){float s=Mathf.Clamp01(t/slide);transform.position=Vector3.Lerp(path[0],path[1],s*s);yield return null;}
            // The fall: sideways, to the wall, faster and faster, turning onto its face.
            float total=0;for(int i=2;i<path.Length;i++)total+=Vector3.Distance(path[i-1],path[i]);
            for(float t=0;t<fall;t+=Time.unscaledDeltaTime)
            {
                float s=Mathf.Clamp01(t/fall);float metres=s*s*total;var at=path[1];
                for(int i=2;i<path.Length;i++){float leg=Vector3.Distance(path[i-1],path[i]);if(metres<=leg||i==path.Length-1){at=Vector3.Lerp(path[i-1],path[i],leg>0?Mathf.Clamp01(metres/leg):1);break;}metres-=leg;}
                transform.SetPositionAndRotation(at,Quaternion.Slerp(rest,land,s));yield return null;
            }
            transform.SetPositionAndRotation(path[^1],land);
            AudioManager.PlayAt(sound,transform,transform.position,.22f,AudioBus.SFX,60,.06f,1.6f);
            CombatVfx.Burst(transform.position,new Color(.86f,.8f,.66f,.5f),"shape");
        }
    }
}
