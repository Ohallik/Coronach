using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // Independent restraint equipment. No severed link resolves the animal or
    // awards loot; the generated module shares the original bounded lifetime.
    [DefaultExecutionOrder(800)]
    public sealed class CantorCollar:MonoBehaviour
    {
        public static readonly List<CantorCollar> All=new();
        readonly Health[] links=new Health[3];
        readonly CollarBand[] bands=new CollarBand[4];
        Health health;
        SerpentSegments body;
        DefeatPresentation presentation;
        public IReadOnlyList<Health> Links=>links;
        public Transform Presentation(int index)=>bands[index].Root;
        public int Remaining {get{int count=0;foreach(var link in links)if(link!=null&&link.Alive)count++;return count;}}
        public bool SweepAttached=>links[0]!=null&&links[0].Alive;
        public bool ChorusAttached=>links[1]!=null&&links[1].Alive;
        public bool VolleyAttached=>links[2]!=null&&links[2].Alive;
        public bool Resolved=>health!=null&&!health.Alive;
        void OnEnable(){All.Add(this);foreach(var band in bands)band?.SetOwnerActive(true);}
        void OnDisable(){All.Remove(this);foreach(var band in bands)band?.SetOwnerActive(false);}
        public void Initialize(EnemyDef definition,SerpentSegments body)
        {
            this.body=body;presentation=GetComponent<DefeatPresentation>();presentation.Recovered+=CompleteRecovery;
            health=GetComponent<Health>();body.Initialize(definition,health);
            health.maximum=health.integrity=definition.integrity*.25f;health.Damageable=false;
            GetComponent<Hurtbox>().multiplier=2;
            foreach(var part in body.Parts)
            {
                // Old anatomy multipliers cannot become a bypass around the lock.
                part.GetComponent<Hurtbox>().owner=null;part.GetComponent<Collider>().enabled=false;
            }
            var names=new[]{"Sweep link","Chorus link","Volley link"};
            for(int i=0;i<links.Length;i++)
            {
                var node=new GameObject(names[i]);node.transform.SetParent(body.Parts[i*2],false);
                var link=node.AddComponent<Health>();link.id="Cantor - "+names[i].ToLowerInvariant();
                link.maximum=link.integrity=definition.integrity*.25f;link.weakness=health.weakness;link.resistance=health.resistance;
                link.breakThreshold=definition.breakThreshold;
                link.reactionOwner=health;links[i]=link;
                var shape=node.AddComponent<CapsuleCollider>();shape.isTrigger=true;shape.radius=1.15f;shape.height=2.3f;
                var box=node.AddComponent<Hurtbox>();box.owner=link;box.multiplier=2;
                int index=i;
                bands[i]=new CollarBand(node.transform,Vector3.zero,i,definition.collarPlate);
                link.Died+=(_,__)=>Cut(index);
            }
            bands[3]=new CollarBand(transform,Vector3.up*SerpentSegments.CenterHeight,3,definition.collarPlate);
            health.Died+=Released;health.Revived+=Rearmed;
        }
        void Cut(int index)
        {
            links[index].GetComponent<Collider>().enabled=false;bands[index].Open();
            health.Damageable=Remaining==0;
            CombatVfx.Burst(links[index].transform.position,new Color(1,.65f,.2f),"failure");
            AudioManager.PlayAt("doorOpen_000",links[index].transform,links[index].transform.position,.16f,AudioBus.SFX,60);
            Debug.Log($"CANTOR_LINK_CUT index={index} remaining={Remaining}");
        }
        void Released(Health _,DamagePacket packet){health.Damageable=false;bands[3].Open();}
        void Rearmed(Health _)
        {
            foreach(var link in links){link.ResetFull();link.GetComponent<Collider>().enabled=true;}
            foreach(var band in bands)band.Restore();health.Damageable=false;
        }
        void CompleteRecovery()
        {
            if(!health.Alive)return;
            // The presentation first restores its death-time collider snapshot,
            // when every link was cut. Fresh equipment owns the final state.
            foreach(var link in links)link.GetComponent<Collider>().enabled=link.Alive;
            body.ResumeFromRestoredPose();
            GetComponent<EnemyBrain>().RestoreAfterRecovery();
            GetComponent<BossController>().RestoreAfterRecovery();
        }
        void LateUpdate()
        {
            if(GameTime.Paused)return;
            foreach(var band in bands)band?.Advance();
        }
        void OnDestroy()
        {
            if(health!=null){health.Died-=Released;health.Revived-=Rearmed;}
            if(presentation!=null)presentation.Recovered-=CompleteRecovery;
            foreach(var band in bands)band?.Dispose();
        }

        sealed class CollarBand
        {
            static readonly Vector3 PlateScale=new Vector3(.85f,.3f,.45f);
            readonly Transform[] plates=new Transform[8];
            readonly Vector3[] rest=new Vector3[8],positions=new Vector3[8],directions=new Vector3[8];
            readonly Quaternion[] rotations=new Quaternion[8],worldRotations=new Quaternion[8];
            readonly Transform anchor;
            readonly Vector3 center;
            public Transform Root {get;}
            float opened=float.PositiveInfinity;
            public CollarBand(Transform parent,Vector3 center,int index,GameObject platePrefab)
            {
                anchor=parent;this.center=center;
                // Discarded equipment has its own lifetime and world pose; it
                // must not extend the departing animal's body bounds.
                Root=new GameObject("Cantor collar equipment "+index).transform;Follow();
                for(int i=0;i<plates.Length;i++)
                {
                    float angle=i*Mathf.PI/4;var radial=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    var go=platePrefab!=null?Object.Instantiate(platePrefab,Root):ActorFactory.Visual("Collar blockout plate",PrimitiveType.Cube,Root,PlateScale,radial*1.1f,"Rock");
                    go.transform.localPosition=radial*1.1f;go.transform.localScale=PlateScale;
                    var temporaryCollider=go.GetComponent<Collider>();if(temporaryCollider!=null)temporaryCollider.enabled=false;
                    // The generated pale outer face is local +Y; point it away
                    // from the animal instead of showing the dark underside.
                    plates[i]=go.transform;go.transform.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg-90);
                    rest[i]=go.transform.localPosition;rotations[i]=go.transform.localRotation;
                    var properties=new MaterialPropertyBlock();properties.SetColor("_BaseColor",platePrefab!=null?(index==3?new Color(1,.91f,.74f):Color.white):(index==3?new Color(.85f,.63f,.3f):new Color(.7f,.46f,.2f)));
                    if(platePrefab==null)properties.SetColor("_EmissionColor",new Color(.18f,.07f,.01f));
                    go.GetComponent<Renderer>().SetPropertyBlock(properties);
                }
            }
            public void Open()
            {
                if(!float.IsPositiveInfinity(opened))return;Follow();opened=GameTime.Now;
                for(int i=0;i<plates.Length;i++)
                {
                    positions[i]=plates[i].position;worldRotations[i]=plates[i].rotation;
                    directions[i]=plates[i].parent.TransformDirection((rest[i]-(rest[0]+rest[4])*.5f).normalized);
                }
            }
            public void Advance()
            {
                if(float.IsPositiveInfinity(opened)){Follow();return;}float age=GameTime.Now-opened;
                for(int i=0;i<plates.Length;i++)
                {
                    plates[i].position=positions[i]+directions[i]*(1-Mathf.Exp(-5*age))*1.4f-Vector3.up*(1.8f*age*age);
                    plates[i].rotation=worldRotations[i]*Quaternion.Euler(age*50,0,age*35);
                    plates[i].localScale=PlateScale*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.7f,1.2f,age)));
                    if(age>=1.2f)plates[i].gameObject.SetActive(false);
                }
            }
            public void Restore()
            {
                opened=float.PositiveInfinity;Follow();
                for(int i=0;i<plates.Length;i++)
                {plates[i].gameObject.SetActive(true);plates[i].SetLocalPositionAndRotation(rest[i],rotations[i]);plates[i].localScale=PlateScale;}
            }
            void Follow(){if(anchor!=null)Root.SetPositionAndRotation(anchor.TransformPoint(center),anchor.rotation);}
            public void SetOwnerActive(bool active){if(Root!=null)Root.gameObject.SetActive(active);}
            public void Dispose(){if(Root!=null)Object.Destroy(Root.gameObject);}
        }
    }
}
