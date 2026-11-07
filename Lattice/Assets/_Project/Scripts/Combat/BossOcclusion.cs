using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Keep the controlled hero readable through a large ground boss. The door opens only when the body itself stands between the lens and the hero's chest:
    /// a box round a drill lying diagonally is 6 m wide with nothing in its corners, and a hero standing there wore a patch of dots for nothing, the dead wreck's too.</summary>
    [DefaultExecutionOrder(900)]
    public sealed class BossOcclusion : MonoBehaviour
    {
        Renderer[] bodies;
        BodyCells[] shapes;
        /// <summary>A rigged body's pose changes under any shape taken once, so the capsule it's struck on stands for it. Rigid bodies have their real shape.</summary>
        CapsuleCollider hurtbox;
        MaterialPropertyBlock block;
        float amount;
        static readonly int Fade = Shader.PropertyToID("_OccFade");
        static readonly int Ellipse = Shader.PropertyToID("_OccFadeEllipse");
        static readonly int Depth = Shader.PropertyToID("_OccFadeDepth");
        static readonly int Soft = Shader.PropertyToID("_OccFadeSoft");
        // The hero's real size on screen (1.9 m seen from 40 degrees, a little wider with the arms), not a 2 by 3 m bite out of the boss. It's a little over him because it's open in
        // full only over him: from there it dissolves to nothing at this edge (_OccFadeSoft). A hard round patch of washed dots read as a hole or a sticker on the boss.
        const float HalfWidth = .68f, HalfHeight = 1.08f;
        // How far it opens over him: nearly two pixels in three, against four in five for scenery. It has to read as seeing through,
        // so more of the body and its weak spot stay. The hero shows in his own colours: a tinted copy over him read as a cyan glitch.
        const float OpenShare = .64f;

        void LateUpdate() => RefreshView(Camera.main, PartyController.Current != null ? PartyController.Current.Active : null, Time.unscaledDeltaTime);

        void Measure()
        {
            var defeat = GetComponent<DefeatPresentation>();
            bodies = (defeat != null && defeat.visual != null ? defeat.visual : transform).GetComponentsInChildren<Renderer>(true);
            shapes = new BodyCells[bodies.Length]; bool shapeless = false;
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] is SkinnedMeshRenderer) shapeless = true;
                else if (bodies[i] is MeshRenderer && Opens(bodies[i])) { shapes[i] = BodyCells.Of(bodies[i]); shapeless |= shapes[i] == null; }
            }
            if (shapeless) hurtbox = GetComponent<CapsuleCollider>();
        }
        /// <summary>What can't open can't be seen through, so it doesn't count: the wreck's scar and spoil are dressing beside it.</summary>
        static bool Opens(Renderer body)
        {
            foreach (var material in body.sharedMaterials) if (material != null && material.IsKeywordEnabled("_OCCFADE_ON")) return true;
            return false;
        }
        bool Hides(Vector3 eye, Vector3 end)
        {
            for (int i = 0; i < bodies.Length; i++)
                if (shapes[i] != null && bodies[i] != null && bodies[i].enabled && bodies[i].gameObject.activeInHierarchy && shapes[i].Crossed(eye, end)) return true;
            if (hurtbox == null || !hurtbox.enabled) return false;
            var middle = hurtbox.transform.TransformPoint(hurtbox.center); var axis = Vector3.up * Mathf.Max(.01f, hurtbox.height * .5f - hurtbox.radius);
            return Crosses(eye, end, middle - axis, middle + axis, hurtbox.radius);
        }
        /// <summary>Does one segment pass within a radius of another: a trigger's capsule, asked directly, whatever the physics scene has synced.</summary>
        static bool Crosses(Vector3 a, Vector3 b, Vector3 p, Vector3 q, float radius)
        {
            Vector3 d1 = b - a, d2 = q - p, r = a - p;
            float aa = Vector3.Dot(d1, d1), ee = Vector3.Dot(d2, d2), ff = Vector3.Dot(d2, r), cc = Vector3.Dot(d1, r), bb = Vector3.Dot(d1, d2), den = aa * ee - bb * bb;
            float s = den > 1e-6f ? Mathf.Clamp01((bb * ff - cc * ee) / den) : 0, t = (bb * s + ff) / ee;
            if (t < 0) { t = 0; s = Mathf.Clamp01(-cc / aa); } else if (t > 1) { t = 1; s = Mathf.Clamp01((bb - cc) / aa); }
            return (a + d1 * s - (p + d2 * t)).sqrMagnitude <= radius * radius;
        }

        // Also used by the editor's actual-pixel visibility probe. This only
        // changes renderer properties; it never changes a camera or actor.
        public void RefreshView(Camera camera, CombatActor hero, float delta)
        {
            block ??= new MaterialPropertyBlock();
            if (bodies == null) Measure();
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
                    var eye = camera.transform.position;
                    blocked = Hides(eye, center + (eye - center).normalized * .15f);
                }
            }
            amount = Mathf.MoveTowards(amount, blocked ? 1 : 0, Mathf.Max(0, delta) * 8);
            foreach (var body in bodies)
            {
                if (body == null || body is not MeshRenderer && body is not SkinnedMeshRenderer) continue;
                body.GetPropertyBlock(block); block.SetFloat(Fade, amount); block.SetFloat(Depth, OpenShare); block.SetFloat(Soft, 1);
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

        /// <summary>A rigid body's surface as cells a hand across, in its own frame, so it turns, tilts and swells with the body. Taken once from its mesh; a line asks it for nothing
        /// but a few lookups, where the mesh's 11000 triangles would cost a millisecond a frame. It says yes up to a cell off the surface, and never no on the real thing.</summary>
        sealed class BodyCells
        {
            const float Cell = .25f;
            const int MostCells = 40;
            readonly Transform frame; readonly Vector3 low, cell; readonly int nx, ny, nz; readonly bool[] filled;
            public static BodyCells Of(Renderer body)
            {
                var mesh = body.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                return mesh != null && mesh.isReadable ? new BodyCells(body.transform, mesh) : null;
            }
            BodyCells(Transform frame, Mesh mesh)
            {
                this.frame = frame; var box = mesh.bounds; low = box.min; var stretch = frame.lossyScale;
                nx = Count(box.size.x, stretch.x); ny = Count(box.size.y, stretch.y); nz = Count(box.size.z, stretch.z);
                cell = new Vector3(Mathf.Max(box.size.x, 1e-5f) / nx, Mathf.Max(box.size.y, 1e-5f) / ny, Mathf.Max(box.size.z, 1e-5f) / nz);
                filled = new bool[nx * ny * nz];
                var vertices = mesh.vertices; var triangles = mesh.triangles; float reach = Mathf.Min(cell.x, Mathf.Min(cell.y, cell.z)) * .5f;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    var a = vertices[triangles[t]]; var ab = vertices[triangles[t + 1]] - a; var ac = vertices[triangles[t + 2]] - a;
                    int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(ab.sqrMagnitude, Mathf.Max(ac.sqrMagnitude, (ab - ac).sqrMagnitude))) / reach), 1, 32);
                    for (int i = 0; i <= steps; i++) for (int j = 0; j <= steps - i; j++) filled[Index(a + ab * (i / (float)steps) + ac * (j / (float)steps))] = true;
                }
            }
            static int Count(float size, float stretch) => Mathf.Clamp(Mathf.CeilToInt(size * Mathf.Abs(stretch) / Cell), 1, MostCells);
            int Index(Vector3 point) =>
                Mathf.Clamp((int)((point.x - low.x) / cell.x), 0, nx - 1) + nx * (Mathf.Clamp((int)((point.y - low.y) / cell.y), 0, ny - 1) + ny * Mathf.Clamp((int)((point.z - low.z) / cell.z), 0, nz - 1));
            public bool Crossed(Vector3 from, Vector3 to)
            {
                var a = frame.InverseTransformPoint(from); var d = frame.InverseTransformPoint(to) - a;
                float enter = 0, leave = 1;
                if (!Slab(a.x, d.x, low.x, low.x + cell.x * nx, ref enter, ref leave) || !Slab(a.y, d.y, low.y, low.y + cell.y * ny, ref enter, ref leave) || !Slab(a.z, d.z, low.z, low.z + cell.z * nz, ref enter, ref leave)) return false;
                float across = Mathf.Max(Mathf.Abs(d.x) / cell.x, Mathf.Max(Mathf.Abs(d.y) / cell.y, Mathf.Abs(d.z) / cell.z)) * (leave - enter);
                int steps = Mathf.Clamp(Mathf.CeilToInt(across * 2), 1, 256);
                for (int i = 0; i <= steps; i++) if (filled[Index(a + d * Mathf.Lerp(enter, leave, i / (float)steps))]) return true;
                return false;
            }
            static bool Slab(float start, float delta, float min, float max, ref float enter, ref float leave)
            {
                if (Mathf.Abs(delta) < 1e-7f) return start >= min && start <= max;
                float near = (min - start) / delta, far = (max - start) / delta;
                if (near > far) { float swap = near; near = far; far = swap; }
                enter = Mathf.Max(enter, near); leave = Mathf.Min(leave, far);
                return enter <= leave;
            }
        }
    }
}
