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
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    /// <summary>What the whole-game capture of November 22 still found on the HUD and in the words over the world (D356): two to four damage numbers on one body ('295 273 42!
    /// 33' on the Bellows, s08 067; '78!' over '48', s14 144); a panel's words on a grey of snow while its plate came back (s35 101); the glow gauge's words into its frame
    /// (s23 170); a collared head's card naming a lock and not whose (s10 123); nothing to say which way a locked target off the screen is; the feed's lines run into a roof's
    /// painted TEA ROOM (s29 046); names and floor words caught at a fifth to a half of their strength (s30 277, s31 125, s20 019, s25 090, s30 118, s22 014); and PRESSURE LIFT
    /// drawn across Sela in the lift's door (s16 144). Each measured check was shown finding its fault on the old state first: the old rules behind a switch set only by runs
    /// nov22-hud1 and 2 (since removed), the rest by feeding the check what the capture showed. Stills of the HUD in those states go to Builds/quality/focused/nov22-hud.</summary>
    public sealed class QaNov22HudTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();PromptService.ForceDevice(PromptDevice.Gamepad);SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
            DevLoadout.Apply("hushwell");yield return Zone("Arena_Ground");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var dialogue=DialogueSystem.Current;
            if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            foreach(var marking in Telegraph.Live.ToArray())if(marking!=null)Object.Destroy(marking.gameObject);
            PromptService.ForceDevice(null);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            float deadline=Time.unscaledTime+10;while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            SceneFlow.Current.LoadZone(zone,spawn);deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;var partner=actor.GetComponent<PartnerBrain>();if(partner!=null)partner.enabled=false;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            DialogueSystem.Current.AutoAdvance=false;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static CombatActor Hero=>PartyController.Current.Active;
        static CombatActor Partner{get{var party=PartyController.Current;return party.members[1-party.index];}}
        static GameHud Hud=>Object.FindFirstObjectByType<GameHud>();
        static TMP_Text Text(string name)=>Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name==name);
        static Image Picture(string name)=>Object.FindObjectsByType<Image>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(i=>i.name==name);
        static Rect Box(RectTransform rect,float inset=0)
        {var c=new Vector3[4];rect.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x+inset,c[0].y+inset,c[2].x-inset,c[2].y-inset);}
        static void Guard(){foreach(var m in PartyController.Current.members)m.Health.InvulnerableUntil=GameTime.Now+120;}
        static IEnumerator Settle(float seconds=.4f){yield return new WaitForSecondsRealtime(seconds);}
        static Vector3 TowardCamera{get{var view=ZoneController.Current.definition.cameraProfile;var back=-(Quaternion.Euler(view.pitch,view.yaw,0)*Vector3.forward);back.y=0;return back.normalized;}}
        static Vector3 Ahead(float metres)=>Hero.transform.position-TowardCamera*metres;
        /// <summary>The ground under a screen point: where a body stands whose point that high over its feet is drawn at that screen point.</summary>
        static Vector3 Under(Vector2 screen,float height=0)
        {var ray=Camera.main.ScreenPointToRay(screen);float y=Hero.transform.position.y+height;return ray.GetPoint((y-ray.origin.y)/ray.direction.y)-Vector3.up*height;}
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();if(cc!=null)cc.enabled=false;hero.transform.position=at;if(cc!=null)cc.enabled=true;Physics.SyncTransforms();}
        /// <summary>A hero on the screen, feet to head, as wide as a body.</summary>
        static Rect Body(CombatActor hero)
        {
            var camera=Camera.main;var at=hero.transform.position;
            Vector2 feet=camera.WorldToScreenPoint(at),head=camera.WorldToScreenPoint(at+Vector3.up*1.8f);float half=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*.4f).x-feet.x);
            return Rect.MinMaxRect(feet.x-half,Mathf.Min(feet.y,head.y),feet.x+half,Mathf.Max(feet.y,head.y));
        }
        /// <summary>A check run on the state as it is: it must find nothing.</summary>
        static IEnumerator Now(string fault,System.Func<List<string>,IEnumerator> check)
        {
            var after=new List<string>();yield return check(after);
            Assert.IsFalse(after.Any(f=>f.Contains("fixture:")),string.Join(" | ",after.Where(f=>f.Contains("fixture:")).Distinct()));
            Assert.IsEmpty(after,fault+":\n"+string.Join("\n",after.Distinct().Take(20)));
        }

        // ------------------------------------------------------------------ 1. damage numbers: one an attacker, never touching
        static Health Dummy(Vector3 at)
        {
            var body=new GameObject("Body fixture").AddComponent<Health>();body.id="Fixture";body.maximum=body.integrity=1e7f;body.breakThreshold=1e9f;
            body.weakness=DamageType.Plasma;body.resistance=DamageType.Kinetic;body.transform.position=at;return body;
        }
        static TMP_Text[] Numbers()=>Hud.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="Damage"&&t.isActiveAndEnabled&&t.alpha>.01f).ToArray();
        static Rect Glyphs(TMP_Text text)
        {
            text.ForceMeshUpdate();var b=text.textBounds;Vector3 lo=text.transform.TransformPoint(b.min),hi=text.transform.TransformPoint(b.max);
            return Rect.MinMaxRect(Mathf.Min(lo.x,hi.x),Mathf.Min(lo.y,hi.y),Mathf.Max(lo.x,hi.x),Mathf.Max(lo.y,hi.y));
        }
        /// <summary>How many numbers are up, and whether any two touch (their letters a pixel apart or less).</summary>
        static void ReadNumbers(string body,int most,List<string> faults)
        {
            var up=Numbers();float pixel=Screen.height/1080f;
            if(up.Length>most)faults.Add($"{up.Length} numbers on {body} at once, where {most} hit it: "+string.Join("  ",up.Select(n=>n.text)));
            var boxes=up.Select(Glyphs).ToArray();
            for(int i=0;i<boxes.Length;i++)for(int j=i+1;j<boxes.Length;j++)
            {
                var a=boxes[i];a.xMin-=pixel;a.xMax+=pixel;a.yMin-=pixel;a.yMax+=pixel;
                if(a.Overlaps(boxes[j]))faults.Add($"\"{up[i].text}\" touches \"{up[j].text}\" on {body}");
            }
        }
        /// <summary>The Bellows at s08 067: both heroes striking one body, each by turns with a plain blow and one at its weakness, some of them criticals, and the ground's pulse under
        /// it. Then a hero bitten by one hound, burned by another's burning strip and caught by the ground: what nobody swung and a burn's bite share one number.</summary>
        IEnumerator Piling(List<string> faults)
        {
            var body=Dummy(Ahead(6)+Vector3.right*2);var taren=Hero.Health;var sela=Partner.Health;
            float start=Time.unscaledTime,nextTaren=start,nextSela=start+.1f,nextGround=start+.2f;int tarenHits=0,selaHits=0;bool still=false;
            while(Time.unscaledTime<start+2.6f)
            {
                float now=Time.unscaledTime;
                if(now>=nextTaren){body.Receive(new DamagePacket{source=taren,amount=40+tarenHits*7,type=tarenHits%2==0?DamageType.Pulse:DamageType.Plasma,isCrit=tarenHits%3==2,weight=HitWeight.Light});tarenHits++;nextTaren+=.27f;}
                if(now>=nextSela){body.Receive(new DamagePacket{source=sela,amount=30+selaHits*5,type=selaHits%2==0?DamageType.Plasma:DamageType.Beam,weight=HitWeight.Light});selaHits++;nextSela+=.41f;}
                if(now>=nextGround){body.Receive(new DamagePacket{amount=12,type=DamageType.Pulse});nextGround+=.5f;}
                yield return null;
                ReadNumbers("one body struck by Taren, Sela and the ground",3,faults);
                if(!still&&now-start>1.3f)
                {
                    still=true;const string name="numbers-two-heroes-and-the-ground";
                    // Where each number's letters stand on the screen, for an overlay on the still (a canvas turned to the camera for the still has shown no numbers since November 21).
                    var head=Camera.main.WorldToScreenPoint(body.transform.position+Vector3.up*1.9f);
                    TestContext.WriteLine($"NUMBER_RECTS {name} screen={Screen.width}x{Screen.height} head=({head.x:0},{head.y:0}) "+string.Join(" ",Numbers().Select(n=>{var g=Glyphs(n);return $"[{n.text}|{g.xMin:0},{g.yMin:0},{g.width:0},{g.height:0}|{n.alpha:0.00}]";})));
                    yield return Still(name);
                }
            }
            if(tarenHits<8||selaHits<5)faults.Add("fixture: too few hits landed");
            Object.Destroy(body.gameObject);yield return Settle(1.4f);
            var hero=Hero;hero.Health.maximum=hero.Health.integrity=1e6f;hero.Health.InvulnerableUntil=0;
            var biter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),hero.transform.position+Vector3.right*30);biter.Passive=true;
            var burner=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),hero.transform.position+Vector3.left*30);burner.Passive=true;
            yield return null;
            start=Time.unscaledTime;float nextBite=start,nextBurn=start+.15f;nextGround=start+.3f;int landed=0;
            while(Time.unscaledTime<start+2.2f)
            {
                float now=Time.unscaledTime;
                if(now>=nextBite){if(hero.Health.Receive(new DamagePacket{source=biter.Health,amount=20,type=DamageType.Kinetic,weight=HitWeight.Light})>0)landed++;nextBite+=.3f;}
                if(now>=nextBurn){if(hero.Health.Receive(new DamagePacket{source=burner.Health,amount=9,type=DamageType.Plasma,weight=HitWeight.Tick,deathAttack=true})>0)landed++;nextBurn+=.5f;}
                if(now>=nextGround){if(hero.Health.Receive(new DamagePacket{amount=7,type=DamageType.Pulse})>0)landed++;nextGround+=.45f;}
                yield return null;
                ReadNumbers("a hero bitten, burned and caught by the ground",2,faults);
            }
            if(landed<10)faults.Add($"fixture: only {landed} blows landed on the hero");
            biter.Passive=true;burner.Passive=true;Object.Destroy(biter.gameObject);Object.Destroy(burner.gameObject);
            hero.Health.integrity=hero.Health.maximum;yield return Settle(1.4f);
        }
        [UnityTest] public IEnumerator NumbersOnOneBodyAreOneForEachAttackerAndNeverTouch()
        {
            // On the November 22 rule (a number for each kind of hit inside a second, rising) this found 111 and 128 piles and touches (runs nov22-hud1, 2): '180' on '267!',
            // four numbers on the body at once ('180 267! 125 169!').
            yield return Now("numbers piled or touching on one body",Piling);
        }
        /// <summary>A number added to keeps adding while it lives, and a critical in it keeps its mark when plain hits follow.</summary>
        [UnityTest] public IEnumerator AnAttackersNumberRunsOnWhileItLivesAndKeepsItsCriticalsMark()
        {
            yield return Settle();
            var body=Dummy(Ahead(6));var taren=Hero.Health;
            float landed=body.Receive(new DamagePacket{source=taren,amount=100,type=DamageType.Pulse,isCrit=true});yield return null;
            var number=Numbers().Single();Assert.AreNotEqual(FontStyles.Normal,number.fontStyle&FontStyles.Italic,"fixture: a critical's number leans");
            for(int i=0;i<5;i++){yield return new WaitForSecondsRealtime(.6f);landed+=body.Receive(new DamagePacket{source=taren,amount=10,type=DamageType.Pulse});yield return null;}
            var up=Numbers();
            Assert.AreEqual(1,up.Length,"Taren's hits in one stream three seconds long are more than one number: "+string.Join(" ",up.Select(n=>n.text)));
            Assert.AreEqual(landed,float.Parse(up[0].text.TrimEnd('!')),1.01f,"the number doesn't add up what Taren landed");
            Assert.AreNotEqual(FontStyles.Normal,up[0].fontStyle&FontStyles.Italic,"the critical's mark went when plain hits were added to it");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsEmpty(Numbers(),"the number never leaves once Taren stops");
            Object.Destroy(body.gameObject);
        }

        // ------------------------------------------------------------------ 3. a fading panel's words, at every step
        static float Through(float plate,float solid,float band)=>(1-plate*solid)*(1-band);
        [UnityTest] public IEnumerator AFadingPanelsWordsLetNoMoreThanATwentiethOfTheWorldThroughAtAnyStep()
        {
            Guard();yield return Settle();
            var frame=Picture("ObjectiveFrame");float solid=frame.color.a;
            // The rule before: the band came up in step with the plate's going. Fed that, the check finds the grey of s35 101.
            int grey=0;for(float p=1;p>=PanelFade.Floor-.001f;p-=.05f){float b=Mathf.Clamp01((1-p)/(1-PanelFade.Floor))*PanelFade.BandMost;if(Through(p,solid,b)>.065f)grey++;}
            Assert.Greater(grey,0,"the check finds nothing in the band's old rule, which the capture showed as white words on a grey of snow (s35 101)");
            var at=Under(Box(frame.rectTransform).center,.75f);var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),at);hound.Passive=false;
            var faults=new List<string>();float lowest=1;bool kept=false;
            void Read(string when)
            {
                var band=Picture("ObjectiveWordsBand");float p=frame.canvasRenderer.GetAlpha();lowest=Mathf.Min(lowest,p);
                float b=band!=null&&band.isActiveAndEnabled?band.canvasRenderer.GetAlpha()*band.color.a:0;float t=Through(p,solid,b);
                if(t>.065f)faults.Add($"{when}: the plate at {p:0.00} and the words' band at {b:0.00} let {t:P0} of the world through behind them");
            }
            for(float end=Time.unscaledTime+.6f;Time.unscaledTime<end;){hound.transform.position=at;Physics.SyncTransforms();yield return null;Read("stepping aside");}
            Assert.Less(lowest,.42f,"fixture: an awake enemy is behind the objective");
            hound.Passive=true;Object.Destroy(hound.gameObject);
            for(float end=Time.unscaledTime+4f;Time.unscaledTime<end&&frame.canvasRenderer.GetAlpha()<.995f;)
            {
                yield return null;Read("coming back");
                if(!kept&&frame.canvasRenderer.GetAlpha()>.6f&&frame.canvasRenderer.GetAlpha()<.75f){kept=true;yield return Still("objective-coming-back");}
            }
            Assert.Greater(frame.canvasRenderer.GetAlpha(),.99f,"fixture: the plate comes back");
            Assert.IsEmpty(faults,string.Join("\n",faults.Distinct().Take(12)));
        }

        // ------------------------------------------------------------------ 4. the glow gauge's words
        [UnityTest] public IEnumerator TheGlowGaugesWordsStandAtTheTypeFloorClearOfItsFrame()
        {
            yield return null;
            var root=Hud.GetComponentsInChildren<RectTransform>(true).First(r=>r.name=="GlowGauge");var words=Text("GlowLabel");var frame=Picture("GlowFrame");
            var ship=new GameObject("Glow fixture");var glow=ship.AddComponent<ShipGlow>();
            var said=new List<string>();glow.Set(.6f);said.Add(GlowGauge.Words(glow));glow.Set(.1f);said.Add(GlowGauge.Words(glow));
            yield return new WaitForSecondsRealtime(GlowGauge.DarkAfter+.1f);said.Add(GlowGauge.Words(glow));
            Assert.AreEqual(3,said.Distinct().Count(),"fixture: lit, fading and dark each say their own thing: "+string.Join(" | ",said));
            var faults=new List<string>();string old=null;
            root.gameObject.SetActive(true);
            try
            {
                float scale=root.GetComponentInParent<Canvas>().scaleFactor;var inside=Box(frame.rectTransform);inside=Rect.MinMaxRect(inside.xMin+24*scale,inside.yMin,inside.xMax-24*scale,inside.yMax);
                string Fits(string line)
                {
                    words.text=line;words.ForceMeshUpdate();var b=words.textBounds;Vector3 lo=words.transform.TransformPoint(b.min),hi=words.transform.TransformPoint(b.max);
                    if(words.fontSize<23.99f)return $"'{line}' is set at {words.fontSize:0.0}, under the type floor";
                    if(hi.x>inside.xMax||lo.x<inside.xMin)return $"'{line}' runs from {lo.x:0} to {hi.x:0}: into its frame, whose inside is {inside.xMin:0} to {inside.xMax:0}";
                    return null;
                }
                old=Fits("SHIP'S GLOW   DARK  ·  SWARMS LOSE YOU");
                foreach(var line in said)if(Fits(line) is string fault)faults.Add(fault);
            }
            finally{root.gameObject.SetActive(false);Object.Destroy(ship);}
            Assert.IsNotNull(old,"the check passes the words that ran into the frame at s23 170");
            Assert.IsEmpty(faults,string.Join("\n",faults));
            // A still of the gauge as a flight shows it, dark.
            yield return Zone("Arena_Flight");ShipGlow.Of(Hero);yield return Settle(GlowGauge.DarkAfter+.3f);
            yield return Still("glow-gauge-dark");
        }

        // ------------------------------------------------------------------ 5. a collared head's card
        [UnityTest] public IEnumerator ACollaredHeadsCardSaysWhoseLockItIs()
        {
            static string Fault(string card)=>!card.Contains("COLLAR LOCK")?$"fixture: the card isn't on the lock: '{card.Replace("\n"," / ")}'":!card.Contains("CANTOR")?$"the card names a lock and not whose it is: '{card.Replace("\n"," / ")}'":null;
            Assert.IsNotNull(Fault("COLLAR LOCK   28808/30250\nLB  LOCK    D-pad left/right  SWITCH"),"the check passes the card at s10 123");
            yield return Zone("Arena_Flight");
            var hero=Hero;hero.GetComponent<FlightMotor>().Halt();hero.Health.InvulnerableUntil=GameTime.Now+60;
            var cantor=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Cantor"),hero.transform.position+new Vector3(7,0,5));cantor.Passive=false;cantor.enabled=true;
            yield return null;yield return null;yield return null;cantor.Passive=false;
            var collar=cantor.GetComponent<CantorCollar>();var head=cantor.Health;
            foreach(var link in collar.Links)link.Receive(new DamagePacket{source=hero.Health,amount=1e8f,type=DamageType.Pulse});
            // The title card has its moment first: the target card stands in its place after it.
            yield return Settle(2.4f);
            var faults=new List<string>();
            hero.target=head;hero.TargetLocked=false;for(int i=0;i<4;i++)yield return null;
            // Aimed and not locked, the card stands only for a body on the screen; the serpent goes where it likes, so this is read when its head is in view.
            var seen=Camera.main.WorldToViewportPoint(head.transform.position);
            if(seen.z>0&&seen.x>.05f&&seen.x<.95f&&seen.y>.05f&&seen.y<.95f){string aimed=Text("Target").text;if(Fault(aimed) is string one)faults.Add("aimed: "+one);}
            else TestContext.WriteLine("COLLAR_CARD the head was off the screen when aimed at; the locked card is read alone");
            hero.TargetLocked=true;for(int i=0;i<3;i++)yield return null;
            string locked=Text("Target").text;if(Fault(locked) is string two)faults.Add("locked: "+two);
            if(!locked.StartsWith("LOCKED"))faults.Add($"locked, the card doesn't say so: '{locked.Replace("\n"," / ")}'");
            yield return Still("collar-lock-card");
            hero.target=null;hero.TargetLocked=false;cantor.Passive=true;Object.Destroy(cantor.gameObject);yield return null;
            Assert.IsFalse(faults.Any(f=>f.Contains("fixture:")),string.Join("\n",faults));
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ 6. a locked target off the screen
        static string Marked(Image mark,Camera camera,Component body)
        {
            if(mark==null||!mark.isActiveAndEnabled)return "a locked target off the screen has nothing at the screen's edge pointing to it";
            var at=(Vector2)mark.rectTransform.position;var target=camera.WorldToScreenPoint(body.transform.position+Vector3.up);
            var middle=new Vector2(camera.pixelWidth*.5f,camera.pixelHeight*.5f);var way=((Vector2)target-middle)*(target.z<0?-1:1);way.Normalize();
            var points=((Vector2)(mark.rectTransform.rotation*Vector3.up)).normalized;
            if(Vector2.Dot(points,way)<.8f)return $"the mark points {points}, and the target lies {way} from the middle of the screen";
            if(Vector2.Dot((at-middle).normalized,way)<.8f)return $"the mark stands at {at}, not on the target's side of the screen ({way})";
            float edge=Mathf.Min(Mathf.Min(at.x,at.y),Mathf.Min(camera.pixelWidth-at.x,camera.pixelHeight-at.y));
            if(edge>camera.pixelHeight*.2f)return $"the mark stands {edge:0} px in from the screen's edge";
            return null;
        }
        [UnityTest] public IEnumerator ALockedTargetOffTheScreenIsMarkedAtTheEdgeOnItsSide()
        {
            Guard();yield return Settle();var camera=Camera.main;
            Assert.IsNotNull(Marked(null,camera,Hero),"the check passes a lock off the screen with no mark");
            var faults=new List<string>();
            foreach(var (screen,side) in new[]{(new Vector2(.7f,-.35f),"below"),(new Vector2(1.4f,.6f),"right"),(new Vector2(-.3f,.3f),"left")})
            {
                var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Under(new Vector2(camera.pixelWidth*screen.x,camera.pixelHeight*screen.y)));hound.Passive=true;
                yield return null;
                var seen=camera.WorldToViewportPoint(hound.transform.position+Vector3.up);
                if(seen.z>0&&seen.x>=0&&seen.x<=1&&seen.y>=0&&seen.y<=1){faults.Add($"fixture: the hound {side} is on the screen");Object.Destroy(hound.gameObject);continue;}
                Hero.target=hound.Health;Hero.TargetLocked=false;yield return null;yield return null;yield return null;
                var mark=Picture("LockEdgeMark");if(mark!=null&&mark.isActiveAndEnabled)faults.Add($"{side}: a mark for a target only aimed at, not locked");
                Hero.TargetLocked=true;yield return null;yield return null;yield return null;
                if(Marked(Picture("LockEdgeMark"),camera,hound) is string fault)faults.Add($"{side}: {fault}");
                if(side=="below")yield return Still("lock-edge-mark-below");
                Hero.target=null;Hero.TargetLocked=false;Object.Destroy(hound.gameObject);yield return null;yield return null;
            }
            // Locked on a body on the screen there's nothing at the edge.
            var near=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),Ahead(5));near.Passive=true;yield return null;
            Hero.target=near.Health;Hero.TargetLocked=true;yield return null;yield return null;yield return null;
            var shown=Picture("LockEdgeMark");if(shown!=null&&shown.isActiveAndEnabled)faults.Add("a lock on a body in plain sight has a mark at the edge as well");
            Hero.target=null;Hero.TargetLocked=false;Object.Destroy(near.gameObject);
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ 7. the pickup feed's band
        [UnityTest] public IEnumerator ThePickupFeedsBandDarkensTheWorldUnderItsLinesAndPastTheirEnds()
        {
            yield return Settle();
            foreach(string id in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore"})RpgServices.Inventory.Grant(id,22);
            yield return null;yield return null;
            var band=Picture("PickupBand");Assert.IsTrue(band!=null&&band.isActiveAndEnabled,"fixture: the feed's up");
            var lines=Hud.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="PickupLine"&&t.isActiveAndEnabled).ToArray();Assert.GreaterOrEqual(lines.Length,4,"fixture: four lines");
            float scale=band.canvas.scaleFactor;
            string Fault(float strength)
            {
                if(strength<.78f)return $"the feed's band is at {strength:0.00}: a bright painted sign behind it reads through as part of the lines (TEA ROOM, s29 046)";
                var box=Box(band.rectTransform);
                foreach(var line in lines){var ink=Glyphs(line);if(ink.xMax+48*scale>box.xMax)return $"'{line.text}' ends {(box.xMax-ink.xMax)/scale:0} units short of the band's edge: the world's own letters start right after it";}
                return null;
            }
            var made=band.color;band.color=new Color(made.r,made.g,made.b,.66f);string old=Fault(.66f*band.canvasRenderer.GetAlpha());band.color=made;
            Assert.IsNotNull(old,"the check passes the band at s29 046");
            yield return Still("feed-band");
            Assert.IsNull(Fault(band.color.a*band.canvasRenderer.GetAlpha()));
        }

        // ------------------------------------------------------------------ 8. a word is read or gone
        static TextMeshPro FixtureSign(string words,bool hanging)
        {
            var view=ZoneController.Current.definition.cameraProfile;
            var go=new GameObject("Sign_Fixture",typeof(TextMeshPro));go.transform.rotation=Quaternion.Euler(hanging?60:72,view.yaw,0);
            var text=go.GetComponent<TextMeshPro>();text.font=UiSkin.Display;text.text=words;text.fontSize=UiKit.WorldFontSize(40);text.alignment=TextAlignmentOptions.Center;text.textWrappingMode=TextWrappingModes.NoWrap;
            go.AddComponent<SignFade>().bossOnly=hanging;return text;
        }
        static Rect Letters(TextMeshPro sign)
        {
            var camera=Camera.main;var size=sign.GetPreferredValues();
            Vector3 at=sign.transform.position,across=sign.transform.right*(size.x*.5f),up=sign.transform.up*(size.y*.5f);
            Vector3 one=camera.WorldToScreenPoint(at-across-up),two=camera.WorldToScreenPoint(at+across+up);
            return Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));
        }
        /// <summary>A floor word through a conversation closing, at a run past the vitals' foot and back, and under a hero who stands on it and steps off: every frame it is read
        /// (full strength), a trace (a body on its letters) or gone. Anything between is the ghost the capture caught.</summary>
        IEnumerator Ghosts(List<string> faults)
        {
            var camera=Camera.main;float pixel=camera.pixelHeight/1080f;var sign=FixtureSign("FIRST ARCH",false);
            void Lay(Vector2 screen){sign.transform.position=Under(screen,.05f)+Vector3.up*.05f;}
            void Look(string when){float a=sign.alpha;if(a>.1f&&a<.95f)faults.Add($"{when}: the word at {a:0.00}");}
            // Open floor: the first of these places where the word shows (a test screen is often 4:3, where the panels reach further in).
            var open=Vector2.zero;
            foreach(var share in new[]{new Vector2(.3f,.66f),new Vector2(.68f,.6f),new Vector2(.62f,.36f),new Vector2(.36f,.36f),new Vector2(.72f,.45f)})
            {open=new Vector2(camera.pixelWidth*share.x,camera.pixelHeight*share.y);Lay(open);yield return Settle(.6f);if(sign.alpha>.95f)break;}
            if(sign.alpha<.95f){faults.Add($"fixture: a word on open floor shows ({sign.alpha:0.00})");Object.Destroy(sign.gameObject);yield break;}
            // Someone standing just in front of the lens, under a steep camera: a grower on his rope line 21 m short of THE PRESS took it to its trace (s18 091).
            var eye=camera.transform.position;var look=camera.transform.forward;var flat=new Vector3(look.x,0,look.z).normalized;float ground=Hero.transform.position.y;
            float back=(.1f-(ground-eye.y)*look.y)/Vector3.Dot(flat,look);var partnerWas=Partner.transform.position;
            Put(Partner,new Vector3(eye.x,ground,eye.z)+flat*back);for(int i=0;i<20;i++){yield return null;if(sign.alpha<.95f)faults.Add($"with nobody on it and someone under the lens, a word on open floor is at {sign.alpha:0.00}");}
            Put(Partner,partnerWas);yield return Settle(.4f);
            GameServices.Current.Input.Blocked=true;for(int i=0;i<4;i++)yield return null;
            GameServices.Current.Input.Blocked=false;for(int i=0;i<30;i++){yield return null;Look("the frames after a conversation closes");}
            var vitals=Box(Picture("VitalsFrame").rectTransform);var from=new Vector2(vitals.center.x,vitals.yMin-260*pixel);
            for(float s=0;s<1.6f;s+=Time.unscaledDeltaTime){Lay(from+Vector2.up*Mathf.PingPong(s*460*pixel,320*pixel));yield return null;Look("at a run up to the vitals' foot and back");}
            Lay(open);yield return Settle(.6f);
            var at=sign.transform.position;var home=Hero.transform.position;
            Put(Hero,at+Vector3.up*.1f);for(int i=0;i<20;i++){yield return null;Look("a hero standing on its letters");}
            if(!Letters(sign).Overlaps(Body(Hero)))faults.Add("fixture: the hero isn't over the word's letters");
            Put(Hero,home);for(int i=0;i<40;i++){yield return null;Look("as the hero steps off it");}
            Object.Destroy(sign.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator AWordIsReadOrGoneAndNeverAGhost()
        {
            Guard();yield return Settle();
            // On the November 22 fades (a tenth of a second down, a third back) this found 39 frames between read and gone (run nov22-hud1): .17, .33, .50, .67, .83 the frames
            // after a conversation closed. The body under the lens is from run nov22-hud2's probe at s18 091: a grower .07 m in front of the lens traced THE PRESS in 52 of 120 frames.
            yield return Now("a word caught between read and gone",Ghosts);
        }
        /// <summary>s16/144: Sela in the lift's door with PRESSURE LIFT drawn across her. A hanging sign over a hero's body steps aside as a floor word does; a person's name over
        /// a hero stays with its person.</summary>
        IEnumerator OverAHero(List<string> faults)
        {
            var hero=Hero;var sign=FixtureSign("PRESSURE LIFT",true);
            sign.transform.position=hero.transform.position+Vector3.up*1.1f+TowardCamera*1.2f;yield return Settle(.5f);
            var letters=Letters(sign);var body=Body(hero);float over=Mathf.Min(letters.yMax,body.yMax)-Mathf.Max(letters.yMin,body.yMin);
            if(!letters.Overlaps(body)||over<letters.height*.5f)faults.Add($"fixture: the sign's letters {letters} aren't over the hero {body}");
            else if(sign.alpha>.1f)faults.Add($"a hanging sign stays at {sign.alpha:0.00} across the hero under it");
            Object.Destroy(sign.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator AHangingSignStepsAsideForAHerosBodyAndANameStaysWithItsPerson()
        {
            Guard();yield return Settle();
            // On the November 22 rule the hanging sign stood at 1.00 across the hero (runs nov22-hud1, 2).
            yield return Now("a hanging sign drawn across a hero",OverAHero);
            var hero=Hero;var name=FixtureSign("ORRIN",true);name.GetComponent<SignFade>().nameTag=true;
            name.transform.position=hero.transform.position+Vector3.up*1.1f+TowardCamera*1.2f;yield return Settle(.5f);
            Assert.Greater(name.alpha,.95f,"a person's name went when a hero walked behind it");
            Object.Destroy(name.gameObject);
        }
        /// <summary>A word lying under the plane in a flight zone (a gate's name, a pad's) with the lead's ship over its letters: drawn nearer the camera than it lies, it was drawn
        /// over the ship. It goes to its trace while the hull is over it and comes back after.</summary>
        IEnumerator UnderAShip(List<string> faults)
        {
            var ship=Hero;ship.GetComponent<FlightMotor>().Halt();var camera=Camera.main;
            var sign=FixtureSign("THE STEWARD",false);sign.GetComponent<SignFade>().bossOnly=true;
            sign.transform.position=ship.transform.position+Vector3.down*.7f;yield return Settle(.5f);
            float hull=HeroCollision.HullRadius(ship.character);PanelFade.ScreenBounds(camera,new Bounds(ship.transform.position,new Vector3(hull*2,hull*.5f,hull*2)),out var body);
            var letters=Letters(sign);float over=Mathf.Min(letters.yMax,body.yMax)-Mathf.Max(letters.yMin,body.yMin);
            if(!letters.Overlaps(body)||over<letters.height*.25f)faults.Add($"fixture: the word's letters {letters} aren't under the ship {body}");
            else if(sign.alpha>.1f)faults.Add($"a word lying under the plane is drawn at {sign.alpha:0.00} over the ship flying across it");
            sign.transform.position=ship.transform.position+Vector3.forward*9+Vector3.down*.7f;yield return Settle(.6f);
            if(sign.alpha<.95f)faults.Add($"a word lying ahead of the ship, clear of it, stays at {sign.alpha:0.00}");
            Object.Destroy(sign.gameObject);yield return null;
        }
        [UnityTest] public IEnumerator AWordLyingUnderTheShipsStepsAsideForAShipOverIt()
        {
            yield return Zone("Arena_Flight");Guard();yield return Settle();
            // On the November 22 rule the word stood at 1.00 over the ship (run nov22-hud2).
            yield return Now("a word under the plane drawn over a ship",UnderAShip);
        }

        // ------------------------------------------------------------------ the stills for the lead
        /// <summary>What the game's camera draws this frame with every canvas over it, as a 1920 by 1080 still in the mirror's Builds/quality/focused/nov22-hud.</summary>
        static IEnumerator Still(string name)
        {
            yield return null;
            const int W=1920,H=1080;var camera=Camera.main;Assert.IsNotNull(camera,"fixture: no game camera");
            var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay&&c.enabled).ToArray();
            var rt=new RenderTexture(W,H,24,RenderTextureFormat.ARGB32);var image=new Texture2D(W,H,TextureFormat.RGB24,false);var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;
                foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=camera.nearClipPlane+.1f+.01f*(100-c.sortingOrder);}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,W,H),0,0);image.Apply();
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/focused/nov22-hud"));Directory.CreateDirectory(folder);
                string file=Path.Combine(folder,name+".png");File.WriteAllBytes(file,image.EncodeToPNG());TestContext.WriteLine("HUD_STILL "+file);
            }
            finally
            {
                foreach(var c in canvases)if(c!=null){c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;}
                camera.targetTexture=target;RenderTexture.active=active;Object.Destroy(rt);Object.Destroy(image);
            }
        }
    }
}
