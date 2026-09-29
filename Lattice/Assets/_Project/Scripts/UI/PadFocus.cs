using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Lattice.UI
{
    /// <summary>
    /// The selection guard. uGUI navigation is only alive while the EventSystem has a
    /// selected object, and three ordinary things empty it: a click on the background
    /// (deselectOnBackgroundClick), a panel destroying the row that was selected, and a
    /// surface that never chose one. A keyboard user can click something back; a pad
    /// user is simply stuck. This component lives on the persistent [UI] object and, on
    /// the next Navigate or Submit that finds nothing selected, puts the cursor back:
    /// on the last selectable that is still usable (so closing a sub-panel returns you
    /// to the button that opened it), else on the topmost usable selectable on screen.
    /// It never moves a selection that exists, and it stays out of the way of a modal
    /// that turned navigation events off on purpose (the dev bug desk).
    /// </summary>
    public sealed class PadFocus : MonoBehaviour
    {
        const int HistoryCap = 12;

        readonly List<GameObject> _history = new();
        bool _restoreRequested;
        bool _hooked;

        public static PadFocus Current { get; private set; }

        /// <summary>How many times the guard has had to put a selection back (tests/logs).</summary>
        public static int RestoreCount { get; private set; }

        void OnEnable()
        {
            Current = this;
            UiActions.Ensure();
            Hook(true);
            // Navigation feedback belongs to the same persistent selection owner.
            if (!TryGetComponent<UiSelectionSound>(out _)) gameObject.AddComponent<UiSelectionSound>();
        }

        void OnDisable()
        {
            Hook(false);
            if (Current == this)
                Current = null;
        }

        void Hook(bool on)
        {
            if (on == _hooked)
                return;
            _hooked = on;
            if (on)
            {
                UiActions.Navigate.performed += OnNavigate;
                UiActions.Submit.performed += OnSubmit;
            }
            else
            {
                UiActions.Navigate.performed -= OnNavigate;
                UiActions.Submit.performed -= OnSubmit;
            }
        }

        void OnNavigate(InputAction.CallbackContext ctx)
        {
            // PassThrough performs on every value change, the release to zero included.
            if (ctx.ReadValue<Vector2>().sqrMagnitude > 0.25f)
                _restoreRequested = true;
        }

        void OnSubmit(InputAction.CallbackContext ctx) => _restoreRequested = true;

        /// <summary>Tests and scene swaps: drop the trail.</summary>
        public void Forget()
        {
            _history.Clear();
            _restoreRequested = false;
        }

        void LateUpdate()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                _restoreRequested = false;
                return;
            }
            var current = es.currentSelectedGameObject;
            if (IsUsable(current))
            {
                Remember(current);
                _restoreRequested = false;
                return;
            }
            if (!_restoreRequested)
                return;
            _restoreRequested = false;
            if (!es.sendNavigationEvents)
                return;
            var target = PopUsable() ?? FindTopmost();
            if (target == null)
                return;
            es.SetSelectedGameObject(target);
            RestoreCount++;
        }

        void Remember(GameObject go)
        {
            if (_history.Count > 0 && _history[^1] == go)
                return;
            _history.Remove(go);
            _history.Add(go);
            if (_history.Count > HistoryCap)
                _history.RemoveAt(0);
        }

        GameObject PopUsable()
        {
            while (_history.Count > 0)
            {
                var go = _history[^1];
                _history.RemoveAt(_history.Count - 1);
                if (IsUsable(go))
                    return go;
            }
            return null;
        }

        public static bool IsUsable(GameObject go) =>
            go != null && go.activeInHierarchy
            && go.TryGetComponent<Selectable>(out var s) && s.enabled && s.IsInteractable();

        /// <summary>The usable selectable on the highest-sorted canvas, top-left first.</summary>
        static GameObject FindTopmost()
        {
            GameObject best = null;
            int bestOrder = int.MinValue;
            float bestY = float.MinValue, bestX = float.MaxValue;
            foreach (var s in FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            {
                if (!IsUsable(s.gameObject))
                    continue;
                var canvas = s.GetComponentInParent<Canvas>();
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;
                int order = canvas.isRootCanvas || canvas.overrideSorting
                    ? canvas.sortingOrder
                    : canvas.rootCanvas.sortingOrder;
                var p = s.transform.position;
                bool better = order > bestOrder
                    || (order == bestOrder && (p.y > bestY + 0.5f
                                               || (Mathf.Abs(p.y - bestY) <= 0.5f && p.x < bestX)));
                if (!better)
                    continue;
                best = s.gameObject;
                bestOrder = order;
                bestY = p.y;
                bestX = p.x;
            }
            return best;
        }
    }
}
