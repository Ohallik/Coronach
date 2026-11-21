using Lattice.Data;
using UnityEngine;

namespace Lattice.Core
{
    /// <summary>
    /// A place that takes another's place for a stretch of the story (<see cref="ZoneDef.standsFor"/>): chapter 9's Decks on separation day (DecksLeaving) is the Decks
    /// from the moment the story sends the party home until the districts fly, while chapter 1's Hub_Decks stays as it was built, object for object. Every way in goes
    /// through <see cref="SceneFlow"/>, so this one question there covers them all: the Halo's three docks, the chart's Decks stop, a warp, a save continued from the title.
    /// Before the window and after it the zone asked for is the zone loaded. Logs <c>ZONE_STANDS_IN asked=&gt;loaded</c>.
    /// </summary>
    public static class StandIns
    {
        /// <summary>The zone to load for <paramref name="scene"/> now: a zone standing in for it, or itself. The spawn is kept (a stand-in keeps the spawns of the place it stands for).</summary>
        public static string Resolve(string scene)
        {
            if(string.IsNullOrEmpty(scene)||scene=="Title"||GameServices.Current==null)return scene;
            var flags=GameServices.Current.Flags;
            foreach(var zone in GameCatalog.All<ZoneDef>())
            {
                if(zone==null||string.IsNullOrEmpty(zone.standsFor)||zone.scene==scene)continue;
                if(System.Array.IndexOf(zone.standsFor.Split(','),scene)<0)continue;
                if(!string.IsNullOrEmpty(zone.standsFrom)&&!flags.GetBool(zone.standsFrom))continue;
                if(!string.IsNullOrEmpty(zone.standsUntil)&&flags.GetBool(zone.standsUntil))continue;
                Debug.Log("ZONE_STANDS_IN "+scene+"=>"+zone.scene);
                return zone.scene;
            }
            return scene;
        }
    }
}
