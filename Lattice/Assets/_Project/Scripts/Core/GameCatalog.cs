using Lattice.Data;
using UnityEngine;
namespace Lattice.Core
{
    public static class GameCatalog
    {
        public static T[] All<T>() where T:ScriptableObject=>Resources.LoadAll<T>("Definitions");
        public static T Find<T>(string id) where T:ScriptableObject=>Resources.Load<T>("Definitions/"+typeof(T).Name+"/"+id);
    }
}
