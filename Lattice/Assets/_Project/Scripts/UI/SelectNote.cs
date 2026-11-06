using UnityEngine;
using UnityEngine.EventSystems;
namespace Lattice.UI
{
    /// <summary>Runs something when its row takes focus: a page's note follows the row the player is on.</summary>
    public sealed class SelectNote:MonoBehaviour,ISelectHandler
    {
        public System.Action selected;
        public void OnSelect(BaseEventData data)=>selected?.Invoke();
    }
}
