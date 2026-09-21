using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
        public sealed class HoverSelect : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
        {
            // Controller pass: every Button/Row carries one. While the pad is the active
            // device a hover is ignored: a pane that rebuilds under a parked mouse raises
            // a pointer-enter without the mouse moving, and that must not steal the pad
            // cursor. The first real mouse movement flips the device and hover is live.
            public static void Attach(Selectable selectable)
            {
                if (!selectable.TryGetComponent<HoverSelect>(out _))
                    selectable.gameObject.AddComponent<HoverSelect>();
            }

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (Lattice.Core.PromptService.Device == Lattice.Core.PromptDevice.Gamepad)
                    return;
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null && GetComponent<Selectable>() is { interactable: true })
                    es.SetSelectedGameObject(gameObject);
            }
        }

}
