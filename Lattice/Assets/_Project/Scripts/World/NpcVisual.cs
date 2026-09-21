using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.World
{
    public sealed class NpcVisual:MonoBehaviour
    {
        public string character;
        void Start()
        {
            var def=GameCatalog.Find<CharacterDef>(character);if(def==null||def.natural==null)return;
            foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
            Instantiate(def.natural,transform).transform.localPosition=Vector3.zero;
        }
    }
}
