using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The third look round (D377): the route chart's names stand on plates the routes don't strike through and the story's diamond doesn't sit on a name (nov22b s42 148), and a
    /// talk opened with the speaker in a side third brings her out from under the corner panels (Binna at Rootmarket, s22 018).</summary>
    public sealed class QaNov23ChartConversationTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("chapter3");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var menu=Object.FindFirstObjectByType<PauseMenu>();if(menu!=null&&menu.IsOpen)menu.Close();GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone)
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,"Arrival");deadline=Time.unscaledTime+25;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return null;yield return null;
        }
        static Rect Box(Component c){var corners=new Vector3[4];((RectTransform)c.transform).GetWorldCorners(corners);return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);}

        [UnityTest] public IEnumerator TheChartsNamesStandOnPlatesAndClearOfTheStorysDiamond()
        {
            GameServices.Current.Flags.SetBool(RouteChart.ChartedFlag,true);yield return Load("TallowDrift");
            var menu=Object.FindFirstObjectByType<PauseMenu>();menu.Open(PauseMenu.ChartPage);yield return null;yield return null;
            var failures=new List<string>();var diamonds=Object.FindObjectsByType<Image>(FindObjectsSortMode.None).Where(i=>i.name=="ChartObjective").ToArray();
            var holders=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.name.StartsWith(RouteChart.StopPrefix)).ToArray();Assert.Greater(holders.Length,5,"fixture: the chart's stops");
            foreach(var holder in holders)
            {
                var label=holder.GetComponentsInChildren<TMP_Text>().First(t=>t.name=="Label");var plate=holder.GetComponentsInChildren<Image>().FirstOrDefault(i=>i.name=="LabelPlate");
                if(plate==null){failures.Add($"{holder.name}: its name has no plate behind it, so a route can pass through the letters");continue;}
                label.ForceMeshUpdate();var ink=label.textBounds;var lb=Box(label);float units=((RectTransform)label.transform).lossyScale.x;
                var inkBox=Rect.MinMaxRect(lb.center.x+ink.min.x*units,lb.center.y+ink.min.y*units,lb.center.x+ink.max.x*units,lb.center.y+ink.max.y*units);
                var pb=Box(plate);if(pb.xMin>inkBox.xMin||pb.xMax<inkBox.xMax||pb.yMin>inkBox.yMin||pb.yMax<inkBox.yMax)failures.Add($"{holder.name}: its plate {pb} doesn't hold its letters {inkBox}");
                if(plate.transform.GetSiblingIndex()>label.transform.GetSiblingIndex())failures.Add($"{holder.name}: its plate is drawn over its letters");
                foreach(var d in diamonds){var db=Box(d);if(Mathf.Abs(db.center.x-pb.center.x)<db.width*.5f+pb.width*.5f&&db.center.y-db.height*.43f<inkBox.yMax&&db.center.y+db.height*.43f>inkBox.yMin&&db.Overlaps(inkBox))failures.Add($"{holder.name}: the diamond marker sits on its name");}
            }
            Assert.IsEmpty(failures,string.Join("\n",failures));
        }

        /// <summary>The speaker stands in the top-left corner under the party panel as the talk opens. With the framing off she stays there (the fixture); with it on, the view brings her down below the panels' foot.</summary>
        [UnityTest] public IEnumerator ASpeakerUnderACornerPanelIsBroughtOutFromUnderItWhenTheTalkOpens()
        {
            yield return Load("Hub_Decks");
            var mira=Object.FindObjectsByType<Lattice.World.Npc>(FindObjectsSortMode.None).FirstOrDefault(n=>n.speaker=="Mira");Assert.IsNotNull(mira,"fixture: Mira on the Decks");
            var party=PartyController.Current;var hero=party.Active;var camera=Camera.main;
            var ray=camera.ViewportPointToRay(new Vector3(.1f,.8f,0));var ground=new Plane(Vector3.up,new Vector3(0,hero.transform.position.y,0));Assert.IsTrue(ground.Raycast(ray,out float along),"fixture: floor under the corner");
            mira.transform.position=ray.GetPoint(along);Physics.SyncTransforms();yield return null;
            var framing=Object.FindFirstObjectByType<ConversationFraming>();Assert.IsNotNull(framing,"fixture: the framing is in the scene");
            float Top(){var box=new Bounds(mira.transform.position+Vector3.up*.9f,new Vector3(1,1.8f,1));float t=-1;for(int c=0;c<8;c++){var p=camera.WorldToViewportPoint(box.center+Vector3.Scale(box.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)));t=Mathf.Max(t,p.y);}return t;}
            float X()=>camera.WorldToViewportPoint(mira.transform.position).x;
            Assert.Less(X(),CameraRig.PanelSide,"fixture: she stands in the left side third");
            var dialogue=Lattice.Dialogue.DialogueSystem.Current;dialogue.AutoAdvance=false;framing.enabled=false;
            Assert.IsTrue(dialogue.StartNode("MiraFirst","Mira"),"fixture: the talk starts");yield return new WaitForSecondsRealtime(3f);
            Assert.Greater(Top(),CameraRig.PanelFoot,"fixture: with no framing she stands up under the panel, and the check must see her there");
            framing.enabled=true;yield return new WaitForSecondsRealtime(5f);
            TestContext.WriteLine($"SIDE_SPEAKER x {X():0.00} top {Top():0.00} (panels' foot {CameraRig.PanelFoot})");
            Assert.LessOrEqual(Top(),CameraRig.PanelFoot+.03f,"the speaker is still drawn up under the corner panels");
        }
    }
}
