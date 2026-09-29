using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The Gullet's anatomy along its length, shared by the scene builder, the
    /// smoke route and map reviews. A Choir's interior passage: mouth, entry
    /// canal, feeding chamber, slalom throat with a salvage eddy, nursery gate
    /// chamber, the Cantor's coil and the exit valve. Valves are the sphincters
    /// that close as membranes until a chamber is cleared.
    /// </summary>
    public static class GulletProfile
    {
        public const float Start = -14, End = 905;
        public const float CacheZ = 410, CantorZ = 805, ExitZ = 890, PerformanceZ = 280;
        public static readonly float[] Valves = { 262, 490, 700 };
        public static readonly float[] Chambers = { 165, 365, 600 };

        // z, centre x, left and right half-widths. Asymmetric bellies and the
        // eddy pocket keep chambers from reading as one repeated tube.
        static readonly (float z, float x, float left, float right)[] knots =
        {
            (-14, 0, 21, 21),   // mouth lip, open to space
            (0, 0, 17, 17),
            (30, 2, 12, 12),    // entry canal
            (80, 6, 12, 12),
            (120, 4, 24, 20),   // feeding chamber opens
            (165, -2, 34, 26),  // widest, fuller on the left
            (215, 2, 22, 20),
            (250, 6, 12, 12),
            (262, 6, 9, 9),     // valve 0
            (275, 6, 13, 13),   // slalom throat
            (340, 0, 13, 13),
            (385, -4, 13, 14),
            (400, -4, 13, 29),  // salvage eddy: the current slows in a right-hand pocket
            (422, -4, 13, 29),
            (440, -2, 13, 14),
            (478, 4, 12, 12),
            (490, 4, 9, 9),     // valve 1
            (505, 4, 14, 14),
            (560, 0, 26, 24),   // nursery gate chamber
            (600, -3, 27, 25),
            (650, 0, 20, 20),
            (688, 2, 11, 11),
            (700, 2, 9, 9),     // valve 2
            (715, 2, 16, 16),
            (760, 0, 36, 36),   // the Cantor's coil: room for both ships and a boss to turn
            (805, 0, 38, 38),
            (850, 0, 30, 30),
            (874, 0, 12, 12),   // exit valve
            (890, 0, 10, 10),
            (905, 0, 14, 14),
        };

        // Tissue folds in the slalom throat: z, side (-1 left, +1 right), depth.
        public static readonly (float z, int side, float depth)[] Folds =
            { (298, -1, 8), (330, 1, 8), (362, -1, 8), (452, 1, 7) };

        public static readonly (float from, float to, string name)[] Sections =
        {
            (Start, 30, "Mouth"), (30, 110, "Entry canal"), (110, 255, "Feeding chamber"),
            (255, 270, "Valve"), (270, 485, "Slalom throat"), (390, 432, "Salvage eddy"),
            (485, 495, "Valve"), (495, 695, "Nursery gate chamber"), (695, 705, "Valve"),
            (705, 868, "The Cantor's coil"), (868, End, "Exit valve"),
        };

        static float Sample(float z, int field)
        {
            z = Mathf.Clamp(z, knots[0].z, knots[knots.Length - 1].z);
            int i = 0; while (i < knots.Length - 2 && z > knots[i + 1].z) i++;
            float t = (z - knots[i].z) / (knots[i + 1].z - knots[i].z);
            float Value(int k) { var n = knots[Mathf.Clamp(k, 0, knots.Length - 1)]; return field == 0 ? n.x : field == 1 ? n.left : n.right; }
            // Catmull-Rom through the knots: smooth tissue, no kinks at joins.
            float p0 = Value(i - 1), p1 = Value(i), p2 = Value(i + 1), p3 = Value(i + 2);
            return .5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (3 * p1 - p0 - 3 * p2 + p3) * t * t * t);
        }
        public static float Center(float z) => Sample(z, 0);
        public static float Left(float z) => Sample(z, 1);
        public static float Right(float z) => Sample(z, 2);
        public static float Width(float z) => Left(z) + Right(z);
        public static float LeftEdge(float z) => Center(z) - Left(z);
        public static float RightEdge(float z) => Center(z) + Right(z);

        /// <summary>The flight line a traveller follows, weaving around folds and
        /// keeping clear of the eddy, so routes never cut through tissue.</summary>
        public static float RouteX(float z)
        {
            float x = Center(z);
            foreach (var fold in Folds)
            {
                float d = (z - fold.z) / 16f;
                x -= fold.side * fold.depth * .75f * Mathf.Exp(-d * d);
            }
            float margin = 6;
            return Mathf.Clamp(x, LeftEdge(z) + margin, RightEdge(z) - margin);
        }
        public static string SectionAt(float z)
        {
            string found = null;
            foreach (var s in Sections) if (z >= s.from && z < s.to) found = s.name;
            return found;
        }
    }
}
