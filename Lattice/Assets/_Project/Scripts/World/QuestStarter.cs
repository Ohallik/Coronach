using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>Starts a side quest the first time its place is entered, so an
    /// optional area always carries its own objective. Starting is idempotent.</summary>
    public sealed class QuestStarter:MonoBehaviour
    {
        public string questId;
        void Start(){if(!string.IsNullOrEmpty(questId))RpgServices.Quests.Start(questId);}
    }
}
