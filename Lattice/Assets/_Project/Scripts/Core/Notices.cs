namespace Lattice.Core
{
    /// <summary>A short card for the player: what a repair did, why it was refused. Systems post without knowing about
    /// UI; the quest card shows them.</summary>
    public static class Notices
    {
        public static event System.Action<string,string> Posted;
        public static void Post(string heading,string body)=>Posted?.Invoke(heading,body);
        /// <summary>True while a card posted with <see cref="Pay"/> is being handed to whoever listens.</summary>
        public static bool Paying{get;private set;}
        /// <summary>A card for what something paid, not for what a press did: a part out of a drop or a reward, a pouch too full to take one. A press's answer is shown next,
        /// ahead of everything; this waits its turn behind the quest or the fight that paid it, and behind the level that came with it (QuestToast). A quest's reward over a
        /// carry cap put POUCH FULL up ahead of that quest's own QUEST COMPLETE.</summary>
        public static void Pay(string heading,string body){Paying=true;try{Posted?.Invoke(heading,body);}finally{Paying=false;}}
    }
}
