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
        static readonly int Depth = Shader.PropertyToID("_OccFadeDepth");
        // The hero's real size on screen (1.9 m seen from 40 degrees, a little wider with the arms), not a 2 by 3 m bite out of the boss.
        const float HalfWidth = .65f, HalfHeight = 1f;
        // How far the screen-door opens at full fade: 10 of its 16 cells, against 13 for scenery. It has to read as seeing through,
        // so more of the body and its weak spot stay, and the hero's own silhouette makes up the difference.
        const float OpenShare = .6f;

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
                    var horizontal = camera.WorldToViewportPoint(center + camera.transform.right * HalfWidth);
                    var vertical = camera.WorldToViewportPoint(center + camera.transform.up * HalfHeight);
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
            if (blocked) HeroSilhouette.Show(hero);
            foreach (var body in bodies)
            {
                if (body == null || body is not MeshRenderer && body is not SkinnedMeshRenderer) continue;
                body.GetPropertyBlock(block); block.SetFloat(Fade, amount); block.SetFloat(Depth, OpenShare);
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
