using Lattice.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Lattice.World
{
    /// <summary>D425: the convoy's hulls, drawn only, taken into <see cref="DrawnHulls"/>. The Last Mooring's convoy moored under the plane (LastMooringBuilder.TheConvoy: one root,
    /// "The convoy, moored") has nothing of its own that runs, so its hulls are taken as its scene opens; one destroyed with its scene goes by itself. ConvoyFlow takes its groups'
    /// hulls the same way (<see cref="Take"/>). A hull is a child named for one ("Convoy: ", "District: "); the root's other children are drawn in one piece across the whole
    /// anchorage (its lighters, its running lights and their pools) and are no hull.</summary>
    public static class MooredHulls
    {
        public const string Root="The convoy, moored";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode)
        {
            foreach(var root in scene.GetRootGameObjects())if(root.name==Root)Take(root.transform,root.transform);
        }
        /// <summary>Each hull among a root's children into the drawn hulls, under <paramref name="owner"/>.</summary>
        public static void Take(Transform root,object owner)
        {
            if(root==null)return;
            foreach(Transform child in root)if(child.name.StartsWith("Convoy: ")||child.name.StartsWith("District: "))DrawnHulls.Add(child,owner);
        }
    }
}
