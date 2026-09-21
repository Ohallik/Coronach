using UnityEngine;
namespace Lattice.Core
{
    public abstract class InteractionPrompt:MonoBehaviour
    {
        public static readonly System.Collections.Generic.HashSet<InteractionPrompt> Active=new();
        protected virtual void OnEnable()=>Active.Add(this);
        protected virtual void OnDisable()=>Active.Remove(this);
        public string prompt="Interact";
        public float range=3;
        public abstract void Interact();
        public virtual bool Available=>true;
    }
}
