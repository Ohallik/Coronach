using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Which zones the in-player look pass takes (scripts/look.ps1, ZoneLookProbe), and in what order. The stills themselves are for eyes; what is held here is that chapter 1's six
    /// stay first and as they were, so their stills still compare run against run, and that a zone built since is taken without anyone adding it to a list.</summary>
    public sealed class ZoneLookProbeTests
    {
        [Test] public void TheLookPassTakesItsSixFirstThenEveryOtherBuiltZoneByChapterAndId()
        {
            var zones=ZoneLookProbe.Zones(null);
            CollectionAssert.AreEqual(new[]{"Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowApproach","TallowDrift"},zones.Take(6).ToArray(),
                "the six zones the look pass has always taken are no longer first, in their order: their stills stop lining up with every earlier run");
            CollectionAssert.AllItemsAreUnique(zones,"a zone is taken twice, and its second three stills write over its first");
            foreach(var built in GameCatalog.All<ZoneDef>().Where(z=>Application.CanStreamedLevelBeLoaded(z.id)))
                CollectionAssert.Contains(zones,built.id,built.id+" is in the build and the look pass leaves it out");
            var later=zones.Skip(6).Select(id=>GameCatalog.Find<ZoneDef>(id)).ToArray();
            for(int i=0;i<later.Length;i++)
            {
                Assert.IsNotNull(later[i],zones[6+i]+" is taken and has no ZoneDef");
                Assert.IsTrue(Application.CanStreamedLevelBeLoaded(later[i].id),later[i].id+" is taken and isn't in the build: the pass would stop there");
                if(i==0)continue;
                int before=later[i-1].chapter<=0?int.MaxValue:later[i-1].chapter,after=later[i].chapter<=0?int.MaxValue:later[i].chapter;
                Assert.IsTrue(before<after||before==after&&string.CompareOrdinal(later[i-1].id,later[i].id)<0,$"{later[i-1].id} is taken before {later[i].id}: later zones go by chapter and then id, as the build list does");
            }
        }
        [Test] public void NamedZonesAreTakenAsNamedAndOnceEach()
        {
            CollectionAssert.AreEqual(new[]{"Lanternwick","Hub_Decks"},ZoneLookProbe.Zones(" Lanternwick, Hub_Decks,,Lanternwick "),"-look-zones isn't taken as given: in its order, each zone once");
        }
    }
}
