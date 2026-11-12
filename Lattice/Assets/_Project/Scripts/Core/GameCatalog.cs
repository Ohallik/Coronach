using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Core
{
    public static class GameCatalog
    {
        /// <summary>Every definition of a kind, in a fresh array the caller may sort or keep. In play it's read once per kind from that kind's own folder
        /// (Definitions/&lt;Type&gt;, where Find already looks and every definition lives) and kept: a LoadAll over all of Definitions on every call deserialized
        /// every definition there, enemies and their models included, and the Fabricate page called it on each press, the first of them the frame the bench
        /// opened (QA-034). Outside play, where builders write new definitions, it reads the whole folder every time, as it always did.</summary>
        public static T[] All<T>() where T:ScriptableObject
        {
            if(!Application.isPlaying)return Resources.LoadAll<T>("Definitions");
            if(!known.TryGetValue(typeof(T),out var found)||!Intact(found))known[typeof(T)]=found=Resources.LoadAll<T>("Definitions/"+typeof(T).Name);
            return (T[])found.Clone();
        }
        public static T Find<T>(string id) where T:ScriptableObject=>Resources.Load<T>("Definitions/"+typeof(T).Name+"/"+id);
        /// <summary>Reads the kinds a menu or a spawn asks for, behind a load's black rather than on the frame that first needs them.</summary>
        public static void Warm(){All<RecipeDef>();All<QuestDef>();All<TechPartDef>();All<ConsumableDef>();All<MaterialDef>();All<ZoneDef>();}
        static readonly Dictionary<System.Type,System.Array> known=new();
        /// <summary>A kept definition can't be unloaded while this holds it, but an editor that unloads assets by hand can still destroy one: read again then.</summary>
        static bool Intact(System.Array found){foreach(var item in found)if((Object)item==null)return false;return true;}
        /// <summary>Play mode reloads the domain here, but an editor that skips the reload would carry the last session's arrays in.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget()=>known.Clear();
    }
}
