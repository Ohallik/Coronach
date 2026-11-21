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
        /// <summary>A hand the fight itself asks for (chapter 7's Bridle clamps): it is offered and answers with a boss bar up, when every other prompt hides and the button is the attack.</summary>
        public virtual bool InFight=>false;
    }
}
