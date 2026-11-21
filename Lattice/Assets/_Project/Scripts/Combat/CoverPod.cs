using System;
using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Cover that has to be made: a seed pod hung over a fighting floor (the Seedthane's nest on Marrow-3), and later an icicle, a slag bucket, anything knocked down to stand behind. While it hangs
    /// it is a body to hit, with integrity of its own, and nothing hides behind it. Broken loose (a hero's blows, or <see cref="Drop"/> from whatever shakes it down), it lands where it hung and
    /// becomes what the cover rule already reads: a solid thing with no integrity, which shots and marked strikes stop at (CombatCover) and bodies walk round (it carves the paths).
    /// The two states are two children, <see cref="hanging"/> and <see cref="fallen"/>: this object holds no integrity itself, or the fallen pod would count as a body and stop nothing.
    /// A fight starts over with every pod hung again; nothing here is in the save. Once <see cref="settledBy"/> has fallen, the pods are gone with it.
    /// </summary>
    public sealed class CoverPod:MonoBehaviour
    {
        public static readonly List<CoverPod> All=new();
        /// <summary>A pod has just come down.</summary>
        public static event Action<CoverPod> Fell;
        /// <summary>The pod on its stalk: carries the Health and its hurtbox.</summary>
        public GameObject hanging;
        /// <summary>The pod on the floor: a solid collider and a carving obstacle, and no Health anywhere above it.</summary>
        public GameObject fallen;
        /// <summary>The boss whose fall takes the pods away (empty: they stay as they are).</summary>
        public string settledBy;
        public bool Hanging=>hanging!=null&&hanging.activeSelf;
        public bool Down=>fallen!=null&&fallen.activeSelf;
        Health health;bool gone;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        void Awake()
        {
            if(hanging!=null&&hanging.TryGetComponent(out health))
            {
                health.Damaged+=(_,packet,amount)=>CombatActor.PayHit(packet,amount);
                health.Died+=(_,__)=>Drop();
            }
        }
        void Start()
        {
            if(fallen!=null)fallen.SetActive(false);
            Settle();
        }
        void Update()=>Settle();
        void Settle()
        {
            if(gone||string.IsNullOrEmpty(settledBy)||GameServices.Current==null||!GameServices.Current.Flags.GetBool("bossdown."+settledBy))return;
            gone=true;if(hanging!=null)hanging.SetActive(false);if(fallen!=null)fallen.SetActive(false);
        }
        /// <summary>Brings it down now, whoever asks: a blow that broke its stalk, or the thing it hangs from shaking it loose. Nobody is left standing inside it: every body under
        /// where it lands is set clear first, with the hanging pod already gone (a hero under a pod low enough to touch was inside its sphere, and a push from in there went nowhere).</summary>
        public void Drop()
        {
            if(gone||!Hanging)return;
            hanging.SetActive(false);
            var at=fallen!=null?fallen.transform.position:transform.position;
            ClearUnder(at);
            if(fallen!=null)fallen.SetActive(true);
            CombatVfx.Burst(at+Vector3.up*.6f,new Color(.78f,.7f,.42f,.7f),"shape");
            AudioManager.PlayAt("impactMetal_000",transform,at,.3f,AudioBus.SFX,60,.06f,.6f);
            Debug.Log("POD_DOWN "+name);
            Fell?.Invoke(this);
        }
        /// <summary>How far from where it lands a body's middle has to be to stand clear of the fallen pod: its box's half-diagonal on the floor (a metre and a half with no box).</summary>
        public float Footprint
        {
            get
            {
                if(fallen==null||!fallen.TryGetComponent<BoxCollider>(out var box))return 1.5f;
                var half=Vector3.Scale(box.size*.5f,fallen.transform.lossyScale);return new Vector2(half.x,half.z).magnitude;
            }
        }
        /// <summary>Whoever stands where it is about to land, hero or enemy, is moved out to its edge: away from its middle, or if that way is walled, round the other three ways. A boss is
        /// left where it is (it isn't knocked about by its own pods).</summary>
        void ClearUnder(Vector3 at)
        {
            float reach=Footprint;
            foreach(var feet in FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
            {
                if(feet==null||!feet.enabled||feet.GetComponent<BossController>()!=null)continue;
                if(feet.TryGetComponent<Health>(out var body)&&!body.Alive)continue;
                float need=reach+feet.radius+.05f;var off=feet.transform.position-at;off.y=0;if(off.sqrMagnitude>=need*need)continue;
                var facing=feet.transform.forward;facing.y=0;
                var away=off.sqrMagnitude>.0025f?off.normalized:facing.sqrMagnitude>.01f?-facing.normalized:Vector3.back;
                for(int turn=0;turn<4;turn++)
                {
                    var way=Quaternion.Euler(0,turn*90,0)*away;var now=feet.transform.position-at;now.y=0;
                    // How far along this way takes it to the edge: |now + way s| = need.
                    float along=Vector3.Dot(now,way),s=-along+Mathf.Sqrt(Mathf.Max(0,along*along-now.sqrMagnitude+need*need));
                    feet.Move(way*s);
                    var left=feet.transform.position-at;left.y=0;if(left.sqrMagnitude>=(need-.02f)*(need-.02f))break;
                }
                Debug.Log("POD_CLEARED "+feet.name);
            }
            Physics.SyncTransforms();
        }
        /// <summary>The hanging pod nearest a point, for whatever wants to knock one down (or keep one up).</summary>
        public static CoverPod NearestHanging(Vector3 point)
        {
            CoverPod best=null;float least=float.PositiveInfinity;
            foreach(var pod in All)
            {
                if(!pod.Hanging)continue;
                var offset=pod.transform.position-point;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=pod;
            }
            return best;
        }
    }
}
