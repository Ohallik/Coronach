using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A machine under somebody else's hand, given back (<c>tune.standsDown</c>: chapter 9's Weld Arm, the station's own welding arm under a Compact override). At nought the override is cut and
    /// the fighter goes, but the arm doesn't fall or burst: its drawn body is left where it stood, as scenery, and over <see cref="Seconds"/> it settles to rest (its far end down
    /// <see cref="Droop"/> degrees) while the override's cyan in its lamps goes over to the plant's own warm white. It stays there until the zone is loaded again. <see cref="Leave"/> makes it from
    /// a dying body's drawn part and hides the original, whose corpse then goes as every corpse does, unseen. Logs <c>MACHINE_AT_REST &lt;id&gt;</c>.
    /// </summary>
    public sealed class MachineAtRest:MonoBehaviour
    {
        public const float Seconds=1.4f,Droop=14;
        /// <summary>The plant's warm white, low: the lamps of a machine on the town's own clock.</summary>
        public static readonly Color Warm=new(1.1f,.86f,.58f,1);
        Quaternion from,to;float began;Renderer[] lamps;MaterialPropertyBlock block;static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        /// <summary>Settled: what a check reads.</summary>
        public bool Settled=>Time.time-began>=Seconds;
        public static MachineAtRest Leave(Transform drawn,string id)
        {
            if(drawn==null)return null;
            var copy=Instantiate(drawn.gameObject,drawn.position,drawn.rotation);copy.name=id+" at rest";copy.transform.localScale=drawn.lossyScale;
            // Scenery: nothing of the fighter's comes with it.
            foreach(var part in copy.GetComponentsInChildren<MonoBehaviour>(true))Destroy(part);
            foreach(var part in copy.GetComponentsInChildren<Collider>(true))Destroy(part);
            foreach(var part in copy.GetComponentsInChildren<Animator>(true))Destroy(part);
            foreach(var view in drawn.GetComponentsInChildren<Renderer>(true))view.enabled=false;
            var rest=copy.AddComponent<MachineAtRest>();Debug.Log("MACHINE_AT_REST "+id);return rest;
        }
        void Start()
        {
            began=Time.time;from=transform.rotation;var right=transform.right;right.y=0;
            to=(right.sqrMagnitude>.01f?Quaternion.AngleAxis(Droop,right.normalized):Quaternion.identity)*from;
            lamps=GetComponentsInChildren<Renderer>();block=new MaterialPropertyBlock();
        }
        bool done;
        void Update()
        {
            if(GameTime.Paused||done)return;
            float k=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-began)/Seconds));
            transform.rotation=Quaternion.Slerp(from,to,k);
            foreach(var lamp in lamps)
            {
                if(lamp==null)continue;var material=lamp.sharedMaterial;if(material==null||!material.HasProperty(EmissionId))continue;
                lamp.GetPropertyBlock(block);block.SetColor(EmissionId,Color.Lerp(material.GetColor(EmissionId),Warm*.6f,k));lamp.SetPropertyBlock(block);
            }
            done=k>=1;
        }
    }
}
