using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>An Ancelane relay lantern (D202). Dark until its flag is set; once relit, its crystal glows and its light
    /// is a ward: light-shy grazers inside it are slowed, drop their lunges and back out, and the first relighting throws a pulse
    /// that staggers nearby enemies, so lighting a relay mid-fight is worth the risk.</summary>
    public sealed class RelayLantern:MonoBehaviour
    {
        public static readonly List<RelayLantern> All=new();
        public string flag;public float radius=10;public Renderer[] crystals=System.Array.Empty<Renderer>();public Light glow;
        public Color litColor=new(1.6f,1.05f,.45f);
        /// <summary>Lit from the first frame, with no ceremony: a lantern nobody ever let go out.</summary>
        public bool alwaysLit;
        public bool Lit{get;private set;}
        bool initialized;MaterialPropertyBlock block;Telegraph ward;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        public static bool Warded(Vector3 point)
        {
            foreach(var lantern in All)
            {
                if(lantern==null||!lantern.Lit)continue;
                var offset=point-lantern.transform.position;offset.y=0;
                if(offset.sqrMagnitude<lantern.radius*lantern.radius)return true;
            }
            return false;
        }
        void Update()
        {
            if(GameServices.Current==null)return;
            bool lit=alwaysLit||!string.IsNullOrEmpty(flag)&&GameServices.Current.Flags.GetBool(flag);
            if(lit!=Lit||!initialized){bool relit=initialized&&lit&&!Lit;Lit=lit;initialized=true;Present(relit);}
            if(!Lit)return;
            foreach(var health in Health.All)
            {
                if(health==null||!health.Alive||health.friendly||health.id!="Gloomgrazer")continue;
                var offset=health.transform.position-transform.position;offset.y=0;
                if(offset.sqrMagnitude>=radius*radius)continue;
                health.SlowUntil=Mathf.Max(health.SlowUntil,Time.time+.25f);
                if(health.TryGetComponent<EnemyBrain>(out var brain))brain.Ward(transform.position);
            }
        }
        /// <summary>On a fighting floor the ward shows where it really ends and its light falls no further: nothing used to mark the
        /// edge, and a grazer in plain view of the glow could be outside it. A town's lanterns and the reach's stay as lit.</summary>
        void ShowWard()
        {
            var zone=ZoneController.Current;if(zone==null||!zone.Combat||zone.Flight)return;
            if(!Lit){if(ward!=null){ward.Cancel();ward=null;}return;}
            var body=GetComponent<Collider>();var floor=transform.position;if(body!=null)floor.y=body.bounds.min.y;
            if(glow!=null)
            {
                // The range that brings the pool on the floor to the ring, from a lamp this high.
                float high=glow.transform.position.y-floor.y;glow.range=Mathf.Sqrt(radius*radius+high*high);
            }
            if(ward==null){ward=Telegraph.Ring(floor,radius-.28f,radius,99999,new Color(1,.8f,.48f,.7f));ward.owner=this;ward.Steady=true;}
        }
        void Present(bool relit)
        {
            block??=new MaterialPropertyBlock();
            foreach(var crystal in crystals)
            {
                if(crystal==null)continue;crystal.GetPropertyBlock(block);
                block.SetColor("_EmissionColor",Lit?litColor:new Color(.05f,.07f,.12f));crystal.SetPropertyBlock(block);
            }
            if(glow!=null)glow.enabled=Lit;
            ShowWard();
            if(!relit)return;
            CombatVfx.Burst(transform.position+Vector3.up*2,new Color(1,.8f,.4f),"kill");
            AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*2,.3f,AudioBus.SFX,60,.06f,.7f);
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health.friendly||health.GetComponent<BossController>()!=null)continue;
                var offset=health.transform.position-transform.position;offset.y=0;
                if(offset.sqrMagnitude<radius*radius)health.Expose(1.2f);
            }
            Debug.Log("RELAY_LIT "+flag);
        }
    }
}
