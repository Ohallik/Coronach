using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Keep the controlled hero readable through a large ground boss.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class BossOcclusion : MonoBehaviour
    {
        Renderer[] bodies;
        MaterialPropertyBlock block;
        float amount;
        static readonly int Fade = Shader.PropertyToID("_OccFade");
        static readonly int Ellipse = Shader.PropertyToID("_OccFadeEllipse");

        void LateUpdate() => RefreshView(Camera.main, PartyController.Current != null ? PartyController.Current.Active : null, Time.unscaledDeltaTime);

        // Also used by the editor's actual-pixel visibility probe. This only
        // changes renderer properties; it never changes a camera or actor.
        public void RefreshView(Camera camera, CombatActor hero, float delta)
        {
            block ??= new MaterialPropertyBlock();
            if (bodies == null)
            {
                var defeat = GetComponent<DefeatPresentation>();
                bodies = (defeat != null && defeat.visual != null ? defeat.visual : transform).GetComponentsInChildren<Renderer>(true);
            }
            bool blocked = false;
            Vector4 ellipse = Vector4.zero;
            if (camera != null && hero != null && !hero.flight)
            {
                var capsule = hero.GetComponent<CharacterController>();
                var center = hero.transform.position + Vector3.up * (capsule != null ? capsule.center.y : .95f);
                var view = camera.WorldToViewportPoint(center);
                if (view.z > camera.nearClipPlane)
                {
                    var horizontal = camera.WorldToViewportPoint(center + camera.transform.right * .95f);
                    var vertical = camera.WorldToViewportPoint(center + camera.transform.up * 1.45f);
                    ellipse = new Vector4(view.x, view.y, Mathf.Abs(horizontal.x - view.x), Mathf.Abs(vertical.y - view.y));
                    var ray = new Ray(camera.transform.position, center - camera.transform.position);
                    float distance = Vector3.Distance(camera.transform.position, center);
                    foreach (var body in bodies)
                        if (body != null && body.enabled && body.gameObject.activeInHierarchy &&
                            body is MeshRenderer or SkinnedMeshRenderer && body.bounds.IntersectRay(ray, out float entry) && entry < distance - .15f)
                        { blocked = true; break; }
                }
            }
            amount = Mathf.MoveTowards(amount, blocked ? 1 : 0, Mathf.Max(0, delta) * 8);
            foreach (var body in bodies)
            {
                if (body == null || body is not MeshRenderer && body is not SkinnedMeshRenderer) continue;
                body.GetPropertyBlock(block); block.SetFloat(Fade, amount);
                block.SetVector(Ellipse, ellipse); body.SetPropertyBlock(block);
            }
        }
        void OnDisable()
        {
            amount = 0;
            if (bodies == null || block == null) return;
            foreach (var body in bodies)
                if (body != null) { body.GetPropertyBlock(block); block.SetFloat(Fade, 0); body.SetPropertyBlock(block); }
        }
    }
}
