namespace Lattice.Core
{
    /// <summary>A short card for the player: what a repair did, why it was refused. Systems post without knowing about
    /// UI; the quest card shows them.</summary>
    public static class Notices
    {
        public static event System.Action<string,string> Posted;
        public static void Post(string heading,string body)=>Posted?.Invoke(heading,body);
    }
}
