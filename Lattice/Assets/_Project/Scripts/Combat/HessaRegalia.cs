using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>
    /// Hessa Korl's captain's gear (D360): the yoke across her shoulders with a stolen relay lantern hung at each end, the Muster's standard on a pole above her head, and a mantle
    /// over her shoulders and down her back. Made by the chapter's builder into her boss body (<c>HessaRegaliaBuilder</c>), as its own child after the body, so the body's own mesh is
    /// still the first renderer anything finds. It stands where her upper chest is each frame, turned as her body is, so it follows her own animation: the lance's lunge, the
    /// backstep, the sweeping turn. Drawn only: nothing here is a collider or a hurtbox, and what she hits and is hit by is her capsule, as it was.
    /// <para>When she yields, the fighter's body goes and the woman under guard, in her own plain body, takes its place in the same frame (<c>YieldedCaptain</c>). Her gear doesn't go
    /// with the fighter: it stays where it was that frame and comes down in the next half second, the standard thrown down behind her, the yoke and mantle off her back to the
    /// floor, the two lanterns set down where they hung, still burning. It is done before her first word (she speaks .4 s after taking over), and lies there through her talk.</para>
    /// </summary>
    public sealed class HessaRegalia:MonoBehaviour
    {
        /// <summary>The bone it rides on and where it sits in that bone's own space, and the body it turns with (her drawn body's root) and how it is turned against it: measured on
        /// the bound pose when the body was made.</summary>
        public Transform anchor,basis;public Vector3 offset;public Quaternion turn=Quaternion.identity;
        /// <summary>How long the gear takes to come down when she yields: inside the .4 s before she speaks.</summary>
        public const float Falls=.38f;
        /// <summary>Set once she has yielded and it lies on the floor: what a check reads.</summary>
        public bool Down{get;private set;}
        string owner;
        void Start(){var brain=GetComponentInParent<EnemyBrain>();owner=brain!=null&&brain.definition!=null?brain.definition.id:null;}
        void OnEnable()=>DefeatPresentation.Yielded+=Yield;
        void OnDisable()=>DefeatPresentation.Yielded-=Yield;
        void LateUpdate()=>Follow();
        /// <summary>Puts it where the bone is now, turned as her body is. Also called by an editor still, where nothing updates.</summary>
        public void Follow(){if(anchor!=null&&!Down)transform.SetPositionAndRotation(anchor.TransformPoint(offset),(basis!=null?basis.rotation:anchor.rotation)*turn);}
        void Yield(string id,Vector3 feet,Quaternion facing)
        {
            if(Down||owner==null||id!=owner)return;
            // Off the fighter before it is destroyed at the end of this frame, where it stands now.
            Down=true;anchor=null;basis=null;transform.SetParent(null,true);
            var back=facing*Vector3.back;back.y=0;back=back.sqrMagnitude>.01f?back.normalized:Vector3.back;
            StartCoroutine(Fall(feet.y,back));
        }
        IEnumerator Fall(float floor,Vector3 back)
        {
            var standard=new List<Transform>();var lanterns=new List<Transform>();var rest=new List<Transform>();
            foreach(Transform part in transform)
            {
                if(part.name.StartsWith("Standard"))standard.Add(part);
                else if(part.name=="Lantern cage"||part.name=="Stolen relay glass")lanterns.Add(part);
                else rest.Add(part);
            }
            var axis=Vector3.Cross(Vector3.up,back);
            // The standard and the yoke with its mantle topple backward from their feet and lie on the floor; the lanterns drop straight and stand.
            var falling=new List<(Transform pivot,Vector3 from,Vector3 to,Quaternion start,Quaternion lie)>();
            void Topple(List<Transform> parts,string name,float angle,float slide)
            {
                if(parts.Count==0)return;var box=Box(parts);
                var pivot=new GameObject(name).transform;pivot.SetParent(transform,false);pivot.position=new Vector3(box.center.x,box.min.y,box.center.z);
                foreach(var part in parts)part.SetParent(pivot,true);
                falling.Add((pivot,pivot.position,new Vector3(pivot.position.x,floor+.04f,pivot.position.z)+back*slide,pivot.rotation,Quaternion.AngleAxis(angle,axis)*pivot.rotation));
            }
            Topple(standard,"Her standard, thrown down",82,.25f);Topple(rest,"Her yoke and mantle, off her back",78,.15f);
            Transform set=null;
            if(lanterns.Count>0)
            {
                var box=Box(lanterns);set=new GameObject("Her lanterns, set down").transform;set.SetParent(transform,false);set.position=new Vector3(box.center.x,box.min.y,box.center.z);
                foreach(var part in lanterns)part.SetParent(set,true);
                falling.Add((set,set.position,new Vector3(set.position.x,floor+.02f,set.position.z),set.rotation,set.rotation));
            }
            // Gravity's ease: slow off, quick at the floor. Real time, so a talk that holds the game's clock doesn't hang it in the air.
            for(float t=0;t<Falls;)
            {
                t+=Time.unscaledDeltaTime;float k=Mathf.Clamp01(t/Falls);k*=k;
                foreach(var (pivot,from,to,start,lie) in falling)if(pivot!=null){pivot.position=Vector3.Lerp(from,to,k);pivot.rotation=Quaternion.Slerp(start,lie,k);}
                yield return null;
            }
            foreach(var (pivot,_,to,_,lie) in falling)if(pivot!=null){pivot.position=to;pivot.rotation=lie;}
        }
        static Bounds Box(List<Transform> parts)
        {
            bool any=false;var box=new Bounds();
            foreach(var part in parts)foreach(var view in part.GetComponentsInChildren<Renderer>()){if(any)box.Encapsulate(view.bounds);else{box=view.bounds;any=true;}}
            if(!any)box=new Bounds(parts[0].position,Vector3.zero);
            return box;
        }
    }
}
