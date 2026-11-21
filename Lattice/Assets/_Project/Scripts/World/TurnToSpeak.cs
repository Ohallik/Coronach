using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Someone who waits their turn: their <see cref="Npc"/> offers a talk only inside one of its windows (from a flag, until another), and otherwise stands there with
    /// nothing to say. Chapter 9's town meeting is eight talks in one room, each with somebody else, and a talk taken out of turn would hear Meret answer a question
    /// nobody had asked. Nothing changes while a conversation or a load holds the input, so a talk that closes its own window ends as it began.
    /// </summary>
    public sealed class TurnToSpeak:MonoBehaviour
    {
        [System.Serializable] public struct Window{public string from,until;}
        public Window[] windows=System.Array.Empty<Window>();
        Npc npc;
        void Awake(){npc=GetComponent<Npc>();Apply(true);}
        void Update()=>Apply(false);
        public bool Open
        {
            get
            {
                var services=GameServices.Current;if(services==null)return false;
                foreach(var w in windows)
                    if((string.IsNullOrEmpty(w.from)||services.Flags.GetBool(w.from))&&(string.IsNullOrEmpty(w.until)||!services.Flags.GetBool(w.until)))return true;
                return false;
            }
        }
        void Apply(bool loading)
        {
            if(npc==null||GameServices.Current==null||!loading&&GameServices.Current.Input.Blocked)return;
            bool open=Open;if(npc.enabled!=open)npc.enabled=open;
        }
    }
}
