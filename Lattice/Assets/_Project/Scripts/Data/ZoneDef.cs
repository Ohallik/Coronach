using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/ZoneDef")]
    public sealed class ZoneDef:ScriptableObject
    {
        public string id,scene; public ZoneKind kind; public CameraProfile cameraProfile; public Hd2dProfile hd2dProfile; public AudioClip ambientAudio;public float ambientVolume=.055f; public string musicId; public Sprite mapIcon; public float flightPlane=1; public string[] safePockets;
        /// <summary>Outgoing damage of ordinary (non-boss) enemies here: the chapter grows more dangerous as it goes (D194).</summary>
        public float enemyThreat=1;
        /// <summary>Open landforms (D198): where visible scenery (a cliff rim, the drill) stands between the fixed ground
        /// camera and the party, the camera slides in along its own view line instead of hiding them. Stations keep
        /// their authored cutaway framing and leave this off.</summary>
        public bool cameraClearsScenery;
        public Material skybox;
        /// <summary>What a player reads for this place ("The Wick Reach"). Empty: DisplayNames' own table, then the id split into words.</summary>
        public string displayName;
        /// <summary>Sunlit pale ground: floor signs take the lighter, edgeless plate (SignStyle). The first chapters' bright zones are in SignStyle's own list.</summary>
        public bool brightFloor;
        /// <summary>Track titles from docs/music/tracks.json, beside <see cref="musicId"/> (the zone's own): what plays while an encounter here is awake
        /// (travel music, then a fight's), in a safe pocket of a combat zone, and with a shop open (empty: "Moonbase Market"). MusicIntake stages whatever is named.</summary>
        public string fightMusicId,pocketMusicId,shopMusicId;
        /// <summary>The chapter that builds this zone (3 to 10; 0 on the first thirteen until ContentScaffold stamps them). New scenes join the build list in this order.</summary>
        public int chapter;
        /// <summary>Nobody says anything on arriving here (the Glass Choir: the manifest's first silence, chapter 7 section 8). Off everywhere else.</summary>
        public bool quietArrival;
        /// <summary>Tracks a scripted moment here plays over the zone's own, by title from docs/music/tracks.json: the moment's own component asks MusicDirector for one while it
        /// lasts (chapter 9's Coronach walk and launch, on the Decks on separation day). MusicIntake stages whatever is named here, as it does the fields above.</summary>
        public string[] cueMusicIds;
        /// <summary>A place that stands in for another for a stretch of the story (chapter 9's Decks on separation day, for chapter 1's Hub_Decks): while <see cref="standsFrom"/> is set
        /// and <see cref="standsUntil"/> isn't, every load of a zone named in <see cref="standsFor"/> (comma-separated) loads this one instead, at the same spawn (Lattice.Core.StandIns,
        /// read by SceneFlow): a dock, a warp, the chart, a save. Empty: stands in for nothing.</summary>
        public string standsFor,standsFrom,standsUntil;
    }
}
