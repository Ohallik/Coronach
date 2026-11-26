using Lattice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Lattice.World
{
    /// <summary>D426: aboard the Meridian Crawl with the tread-hall ridden in and Dorrow not yet told, the objective panel says so. The story's step there is still "Re-mate three
    /// couplings and bring the tread-hall in ahead of the dawn", which only Dorrow's talk completes, and after the Terminator's own "Ride the tread-hall out" and "Climb aboard the
    /// Meridian Crawl" (DawnLine, D423) the panel went back to it for the walk to her: a line the player had just done, read again (nov25-r6-chain s56/065, 080). The words are
    /// this zone's; the arrow stays the story's (on Dorrow). Nothing in the Crawl's scene runs for it, so its holder is made as the scene opens and goes with it.</summary>
    public sealed class CrawlLine:MonoBehaviour
    {
        public const string Zone="MeridianCrawl",RideFlag="home.rideDone",ToldFlag="home.treadRecovered",Words="Tell Dorrow the tread-hall is in";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!=Zone)return;
            var holder=new GameObject("The Crawl's line (the tread-hall is in)");SceneManager.MoveGameObjectToScene(holder,scene);holder.AddComponent<CrawlLine>();
        }
        /// <summary>Whether the line is this zone's to say: the ride is done and Dorrow hasn't been told.</summary>
        public static bool Due(FlagService flags)=>flags!=null&&flags.GetBool(RideFlag)&&!flags.GetBool(ToldFlag);
        void Update()
        {
            var services=GameServices.Current;
            if(services!=null&&Due(services.Flags))ZoneLine.Say(this,Words,null);else ZoneLine.Clear(this);
        }
        void OnDisable(){ZoneLine.Clear(this);}
    }
}
