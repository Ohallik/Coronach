using System.Collections;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Which of two prompts in reach a press answers (PromptService.FindInteraction, D309). Five chapters' plans are heard at Many Hands' chart table, each staged on the
    /// exact spot of the table's own chart; the nearest of two at the same distance was whichever a set happened to hold first, and after a rebuild chapter 10's first step
    /// opened the route chart instead of Idra's plan. The one the objective's arrow points at is now offered when it is within half a metre of being nearest, and two still
    /// level are told apart by name, never by order.</summary>
    public sealed class WantedPromptTests
    {
        sealed class Plain:InteractionPrompt{public int pressed;public override void Interact()=>pressed++;}
        Plain Stand(string name,Vector3 at){var made=new GameObject(name).AddComponent<Plain>();made.transform.position=at;made.range=3;return made;}

        [UnityTest] public IEnumerator ThePromptTheArrowPointsAtIsOfferedWhenAnotherStandsOnTheSameSpot()
        {
            var was=PromptService.Wanted;
            var here=new Vector3(500,0,500);var stand=here+Vector3.back*1.2f;
            // Named so that order by name favours the table's own chart: what the story wants has to win on its own account.
            var chart=Stand("A chart on the table",here);var plan=Stand("The plan heard at the table",here);
            yield return null;
            try
            {
                PromptService.Wanted=null;PromptService.FindInteraction(stand);
                Assert.AreSame(chart,PromptService.Interaction,"fixture: with nothing wanted, two prompts on one spot are told apart by name, and the chart's comes first");
                // And never by which was made first: the same two, made in the other order, give the same answer.
                Object.Destroy(chart.gameObject);Object.Destroy(plan.gameObject);yield return null;
                plan=Stand("The plan heard at the table",here);chart=Stand("A chart on the table",here);yield return null;
                PromptService.Wanted=null;PromptService.FindInteraction(stand);
                Assert.AreSame(chart,PromptService.Interaction,"which of two prompts on one spot is offered turned on the order they were made in");

                PromptService.Wanted=plan;PromptService.FindInteraction(stand);
                Assert.AreSame(plan,PromptService.Interaction,"the arrow points at the plan, the hero stands at the table, and the press would open the table's chart");

                // Within half a metre of being nearest it is still the one offered; further off than that, the nearer prompt is.
                plan.transform.position=here+Vector3.forward*(PromptService.WantedRoom-.1f);PromptService.FindInteraction(stand);
                Assert.AreSame(plan,PromptService.Interaction,"the wanted prompt stood a hand's breadth further off than another and lost the press");
                plan.transform.position=here+Vector3.forward*(PromptService.WantedRoom+.3f);PromptService.FindInteraction(stand);
                Assert.AreSame(chart,PromptService.Interaction,"a prompt well short of the wanted one was passed over for it: the arrow must not reach past what the hero stands at");

                // Out of its reach it is not offered at all, wanted or not; and one that is not available never is.
                plan.transform.position=here;plan.range=.5f;PromptService.FindInteraction(stand);
                Assert.AreSame(chart,PromptService.Interaction,"the wanted prompt was offered from outside its own reach");
            }
            finally
            {
                PromptService.Wanted=was;
                if(chart!=null)Object.Destroy(chart.gameObject);if(plan!=null)Object.Destroy(plan.gameObject);
            }
            yield return null;
        }
    }
}
