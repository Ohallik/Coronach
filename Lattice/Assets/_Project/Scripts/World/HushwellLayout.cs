using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Hushwell's plan (docs/maps/Hushwell.md), shared by the scene builder and
    /// the layout tests. Three levels descend northward: the miners' galleries
    /// (y 0), the pressure gallery and the Bellows chamber (y -8), and the
    /// nursery (y -16). Floors are wherever <see cref="Clear"/> is negative.
    /// </summary>
    public static class HushwellLayout
    {
        public const float Upper = 0, Middle = -8, Lower = -16;
        public const float MainHalfWidth = 5, PocketHalfWidth = 3.4f;
        public const float BellowsEntryZ = 239, BellowsExitZ = 286, EastVentX = 22, WestVentX = -18;
        public static readonly Vector2 Arrival = new(0, 2), Bellows = new(0, 262), Nursery = new(14, 334);

        // The one descending route, breach to nursery.
        public static readonly Vector2[] Main =
        {
            new(0, 2), new(0, 8), new(-4, 22), new(-11, 36), new(-13, 48), new(-9, 62), new(0, 72), new(7, 84),
            new(8, 96), new(6, 110), new(3, 122), new(1, 136), new(0, 148), new(0, 164), new(-2, 178), new(-4, 196),
            new(-2, 210), new(0, 220), new(1, 226), new(0, 240), new(0, 262), new(0, 284), new(3, 296), new(9, 308),
            new(13, 320), new(14, 334),
        };
        // Side passages behind breathing vent-membranes.
        public static readonly Vector2[] EastPassage = { new(12, 164), new(35, 164) };
        public static readonly Vector2[] WestPassage = { new(-2, 212), new(-31, 212) };

        public static readonly (string name, Vector2 center, float rx, float rz)[] Rooms =
        {
            ("Drill breach", new(0, 6), 15, 11),
            ("Gallery A", new(-12, 48), 12, 13),
            ("Gallery B", new(8, 84), 12, 12),
            ("Chamber 1", new(0, 164), 14, 12),
            ("Chamber 2", new(-4, 196), 13, 11),
            ("Chamber 3", new(1, 226), 14, 10),
            ("Bellows chamber", Bellows, 21, 21),
            ("East pocket", new(41, 164), 7, 7),
            ("West pocket", new(-37, 212), 7, 7),
            ("Nursery", Nursery, 18, 14),
        };

        /// <summary>Floor height along the descent. Ramps are the only slopes.</summary>
        public static float Level(float z) =>
            z < 122 ? Upper : z < 148 ? Mathf.Lerp(Upper, Middle, (z - 122) / 26) :
            z < 290 ? Middle : z < 316 ? Mathf.Lerp(Middle, Lower, (z - 290) / 26) : Lower;

        /// <summary>Signed clearance: negative on the cave floor, positive in rock.</summary>
        public static float Clear(Vector2 p)
        {
            float c = Distance(p, Main) - MainHalfWidth;
            c = Mathf.Min(c, Distance(p, EastPassage) - PocketHalfWidth);
            c = Mathf.Min(c, Distance(p, WestPassage) - PocketHalfWidth);
            foreach (var room in Rooms)
            {
                var q = new Vector2((p.x - room.center.x) / room.rx, (p.y - room.center.y) / room.rz);
                c = Mathf.Min(c, (q.magnitude - 1) * Mathf.Min(room.rx, room.rz));
            }
            // Lava-tube walls wander; a fixed seed keeps builds identical.
            return c + 1.1f * (Mathf.PerlinNoise((p.x + 311) * .13f, (p.y + 97) * .11f) - .5f);
        }

        public static float Distance(Vector2 p, Vector2[] path)
        {
            float distance = float.PositiveInfinity;
            for (int i = 1; i < path.Length; i++)
            {
                Vector2 a = path[i - 1], d = path[i] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude);
                distance = Mathf.Min(distance, Vector2.Distance(p, a + d * t));
            }
            return distance;
        }

        public static Vector3 OnFloor(Vector2 p) => new(p.x, Level(p.y), p.y);
    }
}
