using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D389: the objective panel moves on the moment a boss's fight is over, not when the talk or the release after it sets its flag (the Foundry Saint's objective read
    /// "Release the Foundry Saint's arms" over the loot, s59 038).</summary>
    public sealed class HudTextD389Tests
    {
        static QuestDef Quest(out int step)
        {
            var quest=GameCatalog.Find<QuestDef>("HouseLeaves");Assert.IsNotNull(quest,"fixture: HouseLeaves");
            step=System.Array.FindIndex(quest.steps,s=>s.kind==ObjectiveKind.Flag&&s.target=="home.saintReleased");Assert.GreaterOrEqual(step,0,"fixture: the Saint's release step");
            return quest;
        }
        [Test] public void TheLineStaysOnTheFightWhileTheBossStandsAndMovesOnWhenItFalls()
        {
            var quest=Quest(out int step);var state=new GameState();
            // The old state, which the panel showed over the loot: the quest's own step is still the release, and its words name the fight.
            StringAssert.Contains("Release the Foundry Saint",QuestText.Step(quest,step),"fixture: the step's words don't name the fight");
            Assert.AreEqual(step,StoryQuests.PastTheFight(quest,step,state),"the line moved on with the boss still standing");
            state.flags["bossdown.FoundrySaint"]=true;
            int shown=StoryQuests.PastTheFight(quest,step,state);
            Assert.AreEqual(step+1,shown,"the boss is down and the panel still names its fight");
            StringAssert.DoesNotContain("Foundry Saint",QuestText.Step(quest,shown),"the line it moves on to still names the fight");
        }
        [Test] public void AStepThatIsNotABossFightIsNeverSkipped()
        {
            var quest=Quest(out int step);var state=new GameState();state.flags["bossdown.FoundrySaint"]=true;
            int joint=System.Array.FindIndex(quest.steps,s=>s.kind==ObjectiveKind.Interact);
            Assert.AreEqual(joint,StoryQuests.PastTheFight(quest,joint,state),"an Interact step was skipped");
            int last=quest.steps.Length-1;Assert.AreEqual(last,StoryQuests.PastTheFight(quest,last,state),"the last step was passed");
            Assert.AreEqual(-1,StoryQuests.PastTheFight(quest,-1,state),"a quest not begun was moved");
        }
    }
}
