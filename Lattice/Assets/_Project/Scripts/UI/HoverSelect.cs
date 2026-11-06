using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
        public sealed class HoverSelect : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler
        {
            // Controller pass: every Button/Row carries one. A pane that rebuilds under a
            // parked mouse raises a pointer-enter without the mouse moving, and that must
            // not steal the cursor from a pad or a keyboard: only a mouse that moved selects.
            // While the pad is the active device a hover is ignored outright; the first real
            // mouse movement flips the device and hover is live.
            public static void Attach(Selectable selectable)
            {
                if (!selectable.TryGetComponent<HoverSelect>(out _))
                    selectable.gameObject.AddComponent<HoverSelect>();
            }

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (eventData.delta.sqrMagnitude <= 0f || Lattice.Core.PromptService.Device == Lattice.Core.PromptDevice.Gamepad)
                    return;
                var es = UnityEngine.EventSystems.EventSystem.current;
                if (es != null && GetComponent<Selectable>() is { interactable: true })
                    es.SetSelectedGameObject(gameObject);
            }
        }

}
