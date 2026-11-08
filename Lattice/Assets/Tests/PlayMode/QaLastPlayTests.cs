using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.Rpg;
using Lattice.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The last small batch before the final build, the play track. A healing gel trained a gear ladder; a tapping thumb could spend 240 scrip at the end of a talk; a target's plate dropped into the playfield; the way's mark was lost on a bright planet; a shot's tail ran on for metres; the bar told a hero breaking an organ to go and hit something else.
    /// Every check here fails on the build it was written against, for that reason.</summary>
    public sealed class QaLastPlayTests
    {
        Gamepad pad;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(null);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(pad!=null){InputSystem.RemoveDevice(pad);pad=null;}
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            // Back to the default device, which also drops any tag cached off an override: a pad press here mustn't decide what the next fixture's prompts say.
            PromptService.ForceDevice(PromptDevice.Keyboard);PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.35f);
        }
        static GameState State=>GameServices.Current.State;
        static CombatActor Hero=>PartyController.Current.Active;
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Rect Box(RectTransform rect)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
        static float Plate(string image)=>Picture(image).canvasRenderer.GetAlpha();
        static EnemyBrain Spawn(string id,Vector3 at){var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;return enemy;}
        IEnumerator PadPress(GamepadButton button)
        {
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
            InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
        }

        // ------------------------------------------------------------------ item 1: a gel is not a part
        [UnityTest] public IEnumerator AConsumableTrainsNoLadder()
        {
            yield return null;
            var bench=new Fabrication(State);var ladders=System.Enum.GetValues(typeof(Discipline)).Cast<Discipline>().ToArray();
            foreach(string id in new[]{"RepairGel","ChargeCell"})
            {
                var recipe=GameCatalog.Find<RecipeDef>(id);Assert.IsNotNull(recipe,"fixture: "+id+"'s recipe");Assert.IsNull(recipe.output,"fixture: "+id+" isn't a part");
                foreach(var input in recipe.inputs)State.materials[input.id]=input.count*12;
                State.craftingXp.Clear();State.consumables.TryGetValue(id,out int held);
                for(int made=0;made<12;made++)Assert.IsTrue(bench.Craft(recipe,false,1,out _),$"fixture: {id} {made+1} can be made anywhere");
                Assert.AreEqual(held+12,State.consumables[id],"fixture: the gels are in the bag");
                foreach(var ladder in ladders)Assert.AreEqual(0,bench.Xp(ladder),$"twelve of {id} trained {ladder}: a healing item has no link to a gear ladder, and it's a cheap way up one");
            }
            // A part still trains its own ladder, and only that.
            var part=GameCatalog.Find<RecipeDef>("EdgesT1");foreach(var input in part.inputs)State.materials[input.id]=input.count;
            State.craftingXp.Clear();Assert.IsTrue(bench.Craft(part,true,1,out _),"fixture: a Scrap Edge at the bench");
            foreach(var ladder in ladders)Assert.AreEqual(ladder==Discipline.Edges?part.tier*10:0,bench.Xp(ladder),$"a Scrap Edge left {ladder} at the wrong level");
        }

        // ------------------------------------------------------------------ item 2: a shop at the end of a talk
        [UnityTest] public IEnumerator AShopAtTheEndOfATalkLandsOnARowThatSpendsNothing()
        {
            yield return null;
            pad=InputSystem.AddDevice<Gamepad>();
            foreach(var (zone,node,speaker) in new[]{("Hub_Decks","MiraRepeat","Mira"),("Lanternwick","CorraRepeat","Corra")})
            {
                yield return Zone(zone);
                State.scrip=1000;int parts=State.parts.Count;
                var shop=Object.FindFirstObjectByType<ShopUi>();var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=true;
                Assert.IsTrue(dialogue.StartNode(node,speaker),"fixture: "+node+" starts");
                float deadline=Time.unscaledTime+20;while(!shop.IsOpen&&Time.unscaledTime<deadline)yield return null;
                Assert.IsTrue(shop.IsOpen,"fixture: "+node+" opens the shop");yield return null;
                string Under(){var row=EventSystem.current.currentSelectedGameObject;Assert.IsNotNull(row,"nothing has focus: a pad player is stuck");return row.GetComponentInChildren<TMP_Text>().text;}
                StringAssert.DoesNotContain("scrip",Under(),$"{speaker}'s shop comes up under a thumb still tapping through her lines with the cursor on something for sale: one more A spends it");
                Assert.AreNotEqual("ShopBack",EventSystem.current.currentSelectedGameObject.name,"the next tap would close the stall she has just opened");
                // That thumb, going on past the hold, as far as it likes: nothing is spent, nothing is sold, and the stall stays up.
                for(int tap=0;tap<2;tap++)
                {
                    yield return new WaitForSecondsRealtime(FirstPress.Hold+.05f);
                    yield return PadPress(GamepadButton.South);
                    Assert.AreEqual(1000,State.scrip,$"tap {tap+1} at {speaker}'s stall spent scrip");Assert.AreEqual(parts,State.parts.Count,$"tap {tap+1} at {speaker}'s stall moved a part");
                    Assert.IsTrue(shop.IsOpen,$"tap {tap+1} closed {speaker}'s stall");
                    StringAssert.DoesNotContain("scrip",Under(),$"tap {tap+1} left the cursor on something for sale");
                }
                yield return null;StringAssert.Contains("BUY",Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).First(t=>t.name=="Heading"&&t.gameObject.activeInHierarchy).text,"fixture: two taps are back on her stock");
                // One press down is the first offer, and a deliberate press there buys.
                yield return PadPress(GamepadButton.DpadDown);
                StringAssert.Contains("scrip",Under(),"the first offer isn't one press down");
                yield return PadPress(GamepadButton.South);
                Assert.Less(State.scrip,1000,"a deliberate press on an offer must buy");
                shop.Close();yield return null;yield return null;
            }
        }
        /// <summary>Three routes went on buying by pressing A where the shop's cursor happened to land. A route that does turns the page instead, and carries on with nothing bought.</summary>
        [UnityTest] public IEnumerator EveryRouteBuyingAtAStallNamesTheRow()
        {
            yield return null;int buys=0;
            foreach(string file in Directory.GetFiles(Path.Combine(Application.dataPath,"..","..","docs","quality","routes"),"*.json"))
            {
                var route=JsonUtility.FromJson<QualityRoute>(File.ReadAllText(file));if(route.steps==null)continue;
                foreach(var step in route.steps.Where(s=>s.expectedUi=="shop"&&s.name.StartsWith("buy ")))
                {buys++;Assert.IsFalse(string.IsNullOrEmpty(step.pickRow),$"{Path.GetFileName(file)}: '{step.name}' presses A wherever the cursor landed");}
            }
            Assert.Greater(buys,2,"fixture: the routes that buy");
        }

        // ------------------------------------------------------------------ item 3: the flight target's plate
        /// <summary>It stepped 170 units down off its own target, which put it in the middle of the playfield between the ship and what it was shooting. It stays at the top of the screen and fades, as any plate does. This replaces QaHudThirdTests.AFlightTargetBehindItsPlateMovesThePlateOffIt, which pinned the step.</summary>
        [UnityTest] public IEnumerator AFlightTargetBehindItsPlateFadesThePlateAndLeavesItWhereItIs()
        {
            yield return Zone("Arena_Flight");
            float anchoredHome=Text("Target").rectTransform.anchoredPosition.y;
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+30;hero.GetComponent<FlightMotor>().Halt();
            var dart=Spawn("ChoristerDart",hero.transform.position+Vector3.forward*10+Vector3.right*3);yield return null;yield return null;
            hero.target=dart.Health;hero.TargetLocked=true;yield return null;yield return null;yield return null;
            var camera=Camera.main;var plate=Picture("TargetPlate");var reticle=Picture("TargetReticle");Assert.IsTrue(plate.enabled&&reticle.enabled,"fixture: the plate and the lock are up");
            var home=Box(plate.rectTransform);
            // A Shellmine coming in from the top, with the lock's middle where the plate rests. The lock aims at the drawn body, a little over its root, so that's taken off.
            float level=dart.transform.position.y;
            for(int settling=0;settling<90;settling++)
            {
                Vector2 over=(Vector2)reticle.rectTransform.position-(Vector2)camera.WorldToScreenPoint(dart.transform.position);
                var ray=camera.ScreenPointToRay(home.center-over);dart.transform.position=ray.GetPoint((level-ray.origin.y)/ray.direction.y);
                Physics.SyncTransforms();yield return null;
            }
            yield return null;
            Assert.IsTrue(home.Contains((Vector2)reticle.rectTransform.position),"fixture: the target stands behind where the plate rests");
            Assert.AreEqual(anchoredHome,Text("Target").rectTransform.anchoredPosition.y,.5f,"the target's plate has left the top of the screen for the middle of the playfield, between the ship and what it's shooting");
            Assert.Less(Plate("TargetPlate"),.42f,"the plate stays solid over its own target");
            Assert.AreEqual(1,Text("Target").alpha,.01f,"its words go down with its plate");
        }

        // ------------------------------------------------------------------ item 4: the way's mark on a bright planet
        [UnityTest] public IEnumerator TheWaysMarkHasADarkEdgeAndTheBosssKeepsItsLook()
        {
            yield return new WaitForSecondsRealtime(.4f);
            var goal=Picture("GoalEdgeMark");var boss=Picture("BossEdgeMark");Assert.IsNotNull(goal,"fixture: the way's mark");Assert.IsNotNull(boss,"fixture: the boss's mark");
            Assert.AreSame(boss.sprite,goal.sprite,"fixture: they share the arrow's sprite, so an edge can't be painted into it");
            var edge=goal.GetComponent<Outline>();Assert.IsNotNull(edge,"cream on a cream planet, with nothing round it: the mark is lost where the screen has least else on it");
            Assert.IsTrue(edge.enabled,"the edge is off");
            var colour=edge.effectColor;Assert.Less(colour.r*.2126f+colour.g*.7152f+colour.b*.0722f,.1f,$"the edge ({colour}) isn't dark enough to hold on a bright planet");Assert.Greater(colour.a,.7f,$"the edge ({colour}) is too faint to hold on a bright planet");
            Assert.That(Mathf.Abs(edge.effectDistance.x),Is.InRange(2f,4f),"an edge under 2 units is lost at 720p, and over 4 is a heavier shape than the arrow it's round");
            Assert.That(Mathf.Abs(edge.effectDistance.y),Is.InRange(2f,4f),"the edge is uneven: an arrow can point any way");
            Assert.Greater(goal.color.b,.6f,"the mark isn't the objective's cream any more");
            Assert.IsNull(boss.GetComponent<Shadow>(),"the boss's mark has lost its look");
        }

        // ------------------------------------------------------------------ item 5: how long a shot's tail is drawn
        [UnityTest] public IEnumerator AShotsTailIsAComet()
        {
            yield return new WaitForSecondsRealtime(.2f);
            var hero=Hero;var packet=hero.Packet(1,DamageType.Beam,0);var from=hero.transform.position+Vector3.up*30;
            Projectile Fired(float speed){var known=new HashSet<Projectile>(Projectile.Live);Projectile.Fire(from,Vector3.forward,packet,speed);return Projectile.Live.First(s=>!known.Contains(s));}
            foreach(float speed in new[]{24f,30f})
            {
                var shot=Fired(speed);float tail=shot.GetComponent<TrailRenderer>().time*shot.Velocity.magnitude;
                Assert.LessOrEqual(tail,1.6f,$"a {speed} m/s shot draws {tail:0.0} m of tail: close to 300 px on a close camera, and two on one line read as a beam across the screen");
                Assert.Greater(tail,.8f,$"a {speed} m/s shot draws only {tail:0.0} m of tail: nothing says which way it's going");
            }
            // A slow shot's tail is short already. A shot from the pool doesn't keep the last one's.
            Projectile.Fire(from,Vector3.forward,packet,24,.01f);yield return null;yield return null;
            Assert.AreEqual(.12f,Fired(11).GetComponent<TrailRenderer>().time,.001f,"a slow shot's tail has been shortened with the fast ones, and it was never long");
        }

        // ------------------------------------------------------------------ item 6: what the bar says through the organ phase
        [UnityTest] public IEnumerator TheBarTellsAHeroOnAnOrganToBreakItAndAHeroOnTheBossToStrike()
        {
            var hero=Hero;hero.Health.InvulnerableUntil=GameTime.Now+60;
            var organ=new GameObject("Organ fixture").AddComponent<PressureOrgan>();organ.transform.position=hero.transform.position+Vector3.right*4;
            var boss=Spawn("BellowsBelow",hero.transform.position+Vector3.forward*12);boss.Passive=false;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(PressureOrgan.AnyPumping,"fixture: an organ pumps");
            var state=Text("BossState");hero.target=organ.Health;hero.TargetLocked=true;yield return null;yield return null;
            StringAssert.Contains("BRACED",state.text,"fixture: the bar says to break them");
            // The boss opens with the plate locked on an organ and the objective saying to break them.
            boss.Health.Expose(3);yield return null;yield return null;
            Assert.IsTrue(boss.Health.Broken,"fixture: the opening is real");
            StringAssert.Contains("BRACED",state.text,"three things on screen give different orders: the bar says to strike the boss, with the plate on an organ and the objective saying break them");
            Assert.Greater(state.color.b,.9f,"the line stays gold over words that aren't an opening");
            // The opening is real, so on the boss himself it's said.
            hero.target=boss.Health;yield return null;yield return null;
            StringAssert.Contains("OPENING",state.text,"a hero on the boss isn't told of a real opening");
            Assert.Less(state.color.b,.6f,"OPENING isn't gold");
            // With no organ left it's said to anyone: there's nothing else to tell him.
            hero.target=null;hero.TargetLocked=false;organ.Health.integrity=0;yield return null;yield return null;
            Assert.IsFalse(PressureOrgan.AnyPumping,"fixture: no organ pumps");
            StringAssert.Contains("OPENING",state.text,"with the organs gone the bar stops saying the boss is open");
            boss.Passive=true;Object.Destroy(boss.gameObject);Object.Destroy(organ.gameObject);
        }
    }
}
