using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who yields on her feet (chapter 10's Meret in the control well; docs/maps/StillwaterChamber.md, "Boss arena contract: the well", "Her yield"): her person stands hidden through the
    /// duel and takes over from the fighter's body the frame it hands over (<c>DefeatPresentation.Yielded</c>), at the same place and facing, <b>upright</b>: its rig plays its own idle,
    /// never the beaten clip a captain gives up in (<see cref="YieldedCaptain"/>'s), because she does not fall, kneel or shout. A load with <see cref="downFlag"/> stands her at her post;
    /// once <see cref="goneFlag"/> is set (she has gone with Ilo) a load doesn't show her. Logs <c>YIELDS_STANDING &lt;id&gt;</c>.
    /// </summary>
    public sealed class YieldsStanding:MonoBehaviour
    {
        public string bossId="MeretWell",downFlag="bossdown.MeretWell",goneFlag="end.custody";
        /// <summary>Her person (drawn, with whatever walks her off once she goes), hidden until she yields.</summary>
        public GameObject body;
        /// <summary>Where she stands on a later load, and which way.</summary>
        public Vector3 post;public float postYaw=180;
        public bool Standing{get;private set;}
        void OnEnable()=>DefeatPresentation.Yielded+=TakeOver;
        void OnDisable()=>DefeatPresentation.Yielded-=TakeOver;
        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        void Start()
        {
            if(body==null)return;
            if(Flag(goneFlag)){body.SetActive(false);return;}
            if(Flag(downFlag)){body.transform.SetPositionAndRotation(post,Quaternion.Euler(0,postYaw,0));Show();}else body.SetActive(false);
        }
        void TakeOver(string id,Vector3 position,Quaternion rotation)
        {
            if(id!=bossId||body==null||Standing)return;
            body.transform.SetPositionAndRotation(position,rotation);Show();
        }
        /// <summary>A fighter's body still playing out its yield: she takes over when it hands over.</summary>
        bool BodyLeaving()
        {
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))if(enemy.definition!=null&&enemy.definition.id==bossId&&!enemy.Health.Alive)return true;
            return false;
        }
        void Update()
        {
            // A save made in the moment between her yielding and the hand-over: she stands at her post.
            if(Standing||body==null||!Flag(downFlag)||Flag(goneFlag)||BodyLeaving())return;
            body.transform.SetPositionAndRotation(post,Quaternion.Euler(0,postYaw,0));Show();
        }
        void Show()
        {
            Standing=true;body.SetActive(true);
            // Upright: her own idle from its first frame, so the prefab's bind pose never shows for a frame.
            foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.speed=1;rig.Update(0);}
            Debug.Log("YIELDS_STANDING "+bossId);
        }
    }
}
