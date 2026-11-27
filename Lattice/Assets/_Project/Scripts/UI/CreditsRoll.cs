using System;
namespace Lattice.UI
{
    /// <summary>What the credits say (ch10-wiring section 8): the frame and its headings from the chapter 10 manifest's section 10, every name, source and licence under them from
    /// docs/CREDITS.md (and docs/ASSET_SOURCES.md, which records the fonts, the ambience and the licensed shaders the game ships with), the tracks and their maker from
    /// docs/music/tracks.json. Only a heading with someone under it is used, as the manifest asks: writing, the streets, testing and special thanks name nobody in docs/CREDITS.md, so they
    /// are not on the roll until it does. A line added to docs/CREDITS.md is added here; nothing is here that isn't there. Credits draws it.</summary>
    public static class CreditsRoll
    {
        /// <summary>Before the roll, over the convoy under its new sky, one after the other, in plain type.</summary>
        public const string FirstLine="Nobody came through this alone.",SecondLine="These are the hands.";
        /// <summary>The roll's last section, the player's, and the one line under it.</summary>
        public const string PlayerHeading="AND WHOEVER WALKED IT THIS FAR",PlayerLine="Thank you. Easy on the landing.";
        /// <summary>After the roll, alone on the screen, before the title returns.</summary>
        public const string LastLine="The pears keep a schedule.";
        public sealed class Section
        {
            /// <summary>The manifest's heading, in the game's voice, and what it is for, in plain words under it.</summary>
            public string heading,role;
            /// <summary>Who, and what they made or what it is and under which licence.</summary>
            public (string name,string detail)[] people=Array.Empty<(string,string)>();
            /// <summary>Titles listed under the people, two to a row (the music).</summary>
            public string[] titles=Array.Empty<string>();
        }
        /// <summary>Every track the game plays, by the title its maker gave it (docs/music/tracks.json; all of them are staged in Resources/Audio/Music).</summary>
        public static readonly string[] Tracks=
        {
            "Adventure Awaits","Alien Boss Battle","Alien Pulse","Boss of the Ages","Despano Lopo","Dventure at Dusk","Endless Flight","Final Stand","Galactic Conquest",
            "Glass Ruans","Glitch Boss","Hub Town Groove","Jungle Planet Groove","March of the Frozen Host","Moon Caverns","Moonbase Market","Old Village","Regal Alien Town",
            "Riverbed Stomp","Seed Shakers of Xylos","Shop Theme","Starfield Frontier","Starfight","Starward Journey","Swamp Sprint","Title Theme","Village Circle",
        };
        /// <summary>A track's title as the roll shows it. Two of the files came with a slip in their names ("Dventure at Dusk", "Glass Ruans"); the game still asks for each track by
        /// its file's name (the zones' definitions, the tests and docs/music/tracks.json all use it), and the roll shows the title that was meant. Twelve looks read the two as typos.</summary>
        public static string Shown(string track)=>track switch{"Dventure at Dusk"=>"Adventure at Dusk","Glass Ruans"=>"Glass Ruins",_=>track};
        /// <summary>The tracks as the roll lists them, in <see cref="Tracks"/>' order.</summary>
        public static readonly string[] Titles=Array.ConvertAll(Tracks,Shown);
        public static readonly Section[] Sections=
        {
            new(){heading="WHO DREW THE CHART",role="Direction and design",people=new[]{("Nathan","The original game")}},
            new(){heading="WHO PAINTED THE LINE",role="Art, portraits, animation and interface",people=new[]{("Nathan","Original generated art, from image generation and the Meshy pipeline")}},
            new(){heading="WHO HUMMED",role="Music and sound",people=new[]{("Nathan","Original music, made with Suno")},titles=Titles},
            new(){heading="WHO KEPT THE LAMPS",role="Engineering and tools",people=new[]{("Nathan","Code and tools adapted from his Frostbound project · written with Claude Code and Codex")}},
            new(){heading="WHAT WE BORROWED, WITH THANKS",role="Engines, libraries, licensed and third-party work",people=new[]
            {
                ("Unity","Unity Technologies · the engine and its packages, under the Unity licences"),
                ("Yarn Spinner 3.2.4","Yarn Spinner contributors · MIT License"),
                ("Liberation Sans, from TextMesh Pro","Red Hat, Inc. and Google Corporation · SIL Open Font License"),
                ("Cinzel and Nunito","The Cinzel Project Authors and the Nunito Project Authors · SIL Open Font License 1.1"),
                ("Kenney","Sci-Fi Sounds, Impact Sounds, Interface Sounds, RPG Audio and the Particle Pack · CC0 · kenney.nl"),
                ("Quaternius","Universal Animation Libraries 1 and 2, and Ultimate Animated Animals · CC0"),
                ("Sonniss","Ambience from the GDC game audio bundles · royalty-free"),
                ("Staggart Creations","Stylized Water 3 and Stylized Grass Shader · Unity Asset Store licence"),
                ("Real-ESRGAN","Xintao Wang and contributors · BSD 3-Clause License · used to make the portraits, skies and planets"),
                ("Zig and mingw-w64","Zig contributors and the mingw-w64 project · MIT License and Zope Public License 2.1 · used to build a frame-timing diagnostic"),
            }},
        };
    }
}
