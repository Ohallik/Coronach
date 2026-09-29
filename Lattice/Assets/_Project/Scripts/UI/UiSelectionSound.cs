using UnityEngine;
using UnityEngine.EventSystems;

namespace Lattice.UI
{
    // Selection moves between two live controls. Opening a menu, rebuilding a
    // page under the same focus name or clearing focus is not navigation.
    public sealed class UiSelectionSound:MonoBehaviour
    {
        GameObject previous;
        void LateUpdate()
        {
            var system=EventSystem.current;var current=system!=null?system.currentSelectedGameObject:null;
            if(current==previous)return;
            if(current!=null&&previous!=null&&previous.activeInHierarchy&&previous.name!=current.name)UiSounds.Move();
            previous=current;
        }
    }
}
