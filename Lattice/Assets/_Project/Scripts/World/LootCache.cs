using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>A chest off the path: what going to look pays. It gives everything it holds, once, and a flag in the save keeps it open (as a mined crystal stays mined).
    /// Its contents are loot entries, so it can hold a named part, "any Prime drive", materials or consumables; a part comes rolled, as a drop does. An entry's chance isn't read here:
    /// a cache is a promise, not a roll. Like a bench or a crystal, it waits for a fight around it to end. (LootCache, not Cache: UnityEngine has a Cache of its own, and a builder
    /// that imports both couldn't say which it meant.)</summary>
    public sealed class LootCache:InteractionPrompt
    {
        public string cacheId;
        /// <summary>Whose it is, for the card: "Growers' cache".</summary>
        public string label="Cache";
        public LootEntry[] contents=System.Array.Empty<LootEntry>();
        public int scrip;
        /// <summary>What goes when it's opened (a lid, a glow). Unset, the whole object goes.</summary>
        public GameObject visual;
        string Flag=>"cache."+cacheId;
        bool Taken=>GameServices.Current.Flags.GetBool(Flag);
        public override bool Available=>!string.IsNullOrEmpty(cacheId)&&!Taken&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.InteractionCalm);
        /// <summary>For a zone's builder: makes this object a cache with these contents.</summary>
        public static LootCache On(GameObject body,string id,string label,int scrip,params LootEntry[] contents)
        {
            var cache=body.AddComponent<LootCache>();cache.cacheId=id;cache.label=label;cache.scrip=scrip;cache.contents=contents??System.Array.Empty<LootEntry>();cache.prompt="Open cache";
            return cache;
        }
        void Start(){if(Taken)Opened();}
        void Opened(){if(visual!=null)visual.SetActive(false);else gameObject.SetActive(false);}
        public override void Interact()
        {
            if(!Available)return;
            var inventory=RpgServices.Inventory;var found=new List<string>();
            foreach(var entry in contents??System.Array.Empty<LootEntry>()){string name=inventory.GrantLoot(entry);if(name!=null)found.Add(name);}
            if(scrip>0){GameServices.Current.State.scrip+=scrip;Inventory.Announce("scrip",scrip);found.Add(scrip+" scrip");}
            GameServices.Current.Flags.SetBool(Flag,true);
            Notices.Post("CACHE OPENED",label+": "+(found.Count>0?string.Join(", ",found):"nothing left in it"));AudioManager.Play("doorOpen_000",.2f);
            Debug.Log("CACHE_OK "+cacheId);Opened();
        }
    }
}
