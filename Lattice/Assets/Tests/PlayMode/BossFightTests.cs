using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>D198: boss specials mark what they strike, strike only there, open up when they should, and grow by phase.</summary>
    public sealed class BossFightTests
    {
        readonly List<string> log=new();
        void Heard(string text,string stack,LogType type){if(text.StartsWith("BOSS_")||text.StartsWith("BURROWER_"))log.Add(text);}
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");log.Clear();Application.logMessageReceived+=Heard;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Application.logMessageReceived-=Heard;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.5f);
        }
        static void Place(CombatActor actor,Vector3 position){var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=position;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=PartyController.Current.Active);
        static void Fresh(CombatActor actor){actor.Health.maximum=actor.Health.integrity=5000;actor.Health.InvulnerableUntil=0;}
        static IEnumerator Boss(string id,Vector3 at,System.Action<EnemyBrain,BossController> found)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;
            yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
            found(enemy,enemy.GetComponent<BossController>());
        }
        static Telegraph Marking(BossController boss)=>Telegraph.Live.FirstOrDefault(t=>t!=null&&t.owner==boss&&!t.Released);
        static IEnumerator Until(System.Func<bool> condition,float seconds){float end=Time.unscaledTime+seconds;while(!condition()&&Time.unscaledTime<end)yield return null;}
        static float Flat(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));

        [UnityTest] public IEnumerator MarkedStrikesLandInsideTheirMarkingAndNowhereElse()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            var failures=new List<string>();
            // Tail sweep: behind the rig is inside, in front of it is outside.
            rig.transform.rotation=Quaternion.LookRotation(Vector3.forward);
            Place(Lead,new Vector3(0,.1f,-3.6f));Place(Partner,new Vector3(0,.1f,4.5f));Fresh(Lead);Fresh(Partner);
            Assert.IsTrue(boss.Perform("TailSweep"));yield return null;
            var marking=Marking(boss);
            Assert.IsNotNull(marking,"the tail sweep marks no ground");Assert.IsTrue(boss.Telegraphing,"the tail sweep has no tell");
            Assert.IsTrue(marking.Contains(Lead.transform.position),"fixture: the hero behind the rig must stand in the marking");
            Assert.IsFalse(marking.Contains(Partner.transform.position),"fixture: the hero in front must stand outside it");
            float inside=Lead.Health.integrity,outside=Partner.Health.integrity;
            yield return Until(()=>!boss.Busy,4);
            if(!(Lead.Health.integrity<inside))failures.Add("tail sweep missed the hero inside its marking");
            if(Partner.Health.integrity!=outside)failures.Add("tail sweep struck the hero outside its marking");
            // Spoil: each clump lands inside its own circle; a hero far outside every circle is untouched.
            yield return new WaitForSecondsRealtime(.5f);
            Place(Lead,new Vector3(0,.1f,-9));Place(Partner,new Vector3(0,.1f,24));Fresh(Lead);Fresh(Partner);
            inside=Lead.Health.integrity;outside=Partner.Health.integrity;
            Assert.IsTrue(boss.Perform("SpoilToss"));yield return null;
            var circles=Telegraph.Live.Where(t=>t.owner==boss).ToArray();
            Assert.GreaterOrEqual(circles.Length,2,"the spoil marks no landing circles");
            Assert.IsTrue(circles.Any(c=>c.Contains(Lead.transform.position)),"no clump is aimed at the hero");
            Assert.IsFalse(circles.Any(c=>c.Contains(Partner.transform.position)),"fixture: the far hero must be outside every circle");
            yield return Until(()=>!boss.Busy,4);
            if(!(Lead.Health.integrity<inside))failures.Add("spoil missed the hero inside its circle");
            if(Partner.Health.integrity!=outside)failures.Add("spoil struck the hero outside every circle");
            Assert.IsEmpty(failures,string.Join("; ",failures));
        }

        [UnityTest] public IEnumerator TheBurrowerSticksFastWhenItsChargeRamsRock()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(10,.1f,0),(e,b)=>{rig=e;boss=b;});
            // The arena's east wall stands at x = 21; the lane runs from the rig through the hero into it.
            Place(Lead,new Vector3(15,.1f,1.5f));Place(Partner,new Vector3(-10,.1f,-25));
            Lead.Health.InvulnerableUntil=GameTime.Now+30;
            Assert.IsTrue(boss.Perform("DrillCharge"));yield return null;
            var lane=Marking(boss);Assert.IsNotNull(lane,"the charge marks no lane");Assert.AreEqual(Telegraph.Kind.Line,lane.Shape);
            yield return Until(()=>log.Contains("BURROWER_STUCK"),4);
            Assert.Contains("BURROWER_STUCK",log,"the charge never meets the wall; it ended at "+rig.transform.position);
            Assert.IsTrue(rig.Health.Broken,"a rig wedged in rock is not open to punishment");
            Assert.Greater(rig.transform.position.x,16,"the rig stalled short of the wall");
            Assert.IsTrue(boss.Busy,"a wedged rig starts something else at once");
            yield return Until(()=>!rig.Health.Broken,4);
            yield return Until(()=>!boss.Busy,2);
            Assert.IsFalse(boss.Busy,"the rig never pulls free");
        }

        [UnityTest] public IEnumerator TheBurrowerHuntsFromBelowEruptsAtTheHeroAndIsExposed()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            rig.Health.integrity=rig.Health.maximum*.5f;yield return null;Assert.GreaterOrEqual(boss.Phase,2);
            Place(Lead,new Vector3(6,.1f,-6));Place(Partner,new Vector3(-12,.1f,-25));Fresh(Lead);
            Assert.IsTrue(boss.Perform("Burrow"));
            yield return Until(()=>boss.Burrowed,2);
            Assert.IsTrue(boss.Burrowed,"the rig never dives");Assert.IsFalse(rig.Health.Targetable,"a buried rig can still be targeted");
            float before=rig.Health.integrity;rig.Health.Receive(new DamagePacket{source=Lead.Health,amount=500,type=DamageType.Plasma});
            Assert.AreEqual(before,rig.Health.integrity,"a buried rig takes damage");
            // The hero moves; the dust ring follows.
            yield return new WaitForSecondsRealtime(.4f);Place(Lead,new Vector3(-4,.1f,-8));
            yield return new WaitForSecondsRealtime(1.1f);
            var trail=Marking(boss);Assert.IsNotNull(trail,"nothing marks the hunt from below");
            Assert.Less(Flat(trail.Origin,Lead.transform.position),3.2f,"the hunt does not follow the hero");
            float hp=Lead.Health.integrity;
            yield return Until(()=>!boss.Burrowed,3);
            Assert.IsFalse(boss.Burrowed,"the rig never erupts");
            Assert.Less(Flat(rig.transform.position,Lead.transform.position),3.4f,"the rig erupted away from where it was hunting");
            Assert.Less(Lead.Health.integrity,hp,"a hero standing on the eruption was not struck");
            Assert.IsTrue(rig.Health.Broken,"the rig is not exposed after erupting");
            Assert.IsTrue(rig.Health.Targetable);
        }

        [UnityTest] public IEnumerator SeismicRingsAreEscapedByDodgingThroughThem()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            rig.Health.integrity=rig.Health.maximum*.25f;yield return null;Assert.AreEqual(3,boss.Phase);
            Place(Lead,new Vector3(9,.1f,0));Place(Partner,new Vector3(-11,.1f,0));Fresh(Lead);Fresh(Partner);
            float lead=Lead.Health.integrity,partner=Partner.Health.integrity;int dodges=0;
            Assert.IsTrue(boss.Perform("SeismicRings"));
            // A dodge timed to a band is a Flash Move, which slows time for 1.5 s; allow for three of them.
            float end=Time.unscaledTime+15;
            while(boss.Busy&&Time.unscaledTime<end)
            {
                // The lead reads each band's arrival from the tell and dodges through it; the partner stands still.
                if(boss.Telegraphing&&boss.TelegraphRemaining<.07f&&Lead.State!=ActorState.Dodge&&Lead.Dodge(Vector3.forward))dodges++;
                yield return null;
            }
            Assert.IsFalse(boss.Busy);
            Assert.GreaterOrEqual(dodges,3,"the bands never told the lead when they would arrive");
            Assert.AreEqual(lead,Lead.Health.integrity,"a hero dodging through each band was struck");
            Assert.Less(Partner.Health.integrity,partner,"the bands passed a standing hero without striking");
        }

        [UnityTest] public IEnumerator APhaseShiftBracesShovesThePartyAndCallsMitesThatLeaveWithTheFight()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            Place(Lead,new Vector3(0,.1f,-4));Place(Partner,new Vector3(-14,.1f,-25));Fresh(Lead);
            rig.Health.Receive(new DamagePacket{source=Lead.Health,amount=50,type=DamageType.Plasma});
            Assert.IsTrue(boss.Engaged);
            rig.Passive=false;rig.Health.integrity=rig.Health.maximum*.58f;var start=Lead.transform.position;
            yield return Until(()=>boss.Shifting,3);
            Assert.IsTrue(boss.Shifting,"crossing a phase threshold mid-fight plays no shift");
            float hp=rig.Health.integrity;rig.Health.Receive(new DamagePacket{source=Lead.Health,amount=500,type=DamageType.Plasma});
            Assert.AreEqual(hp,rig.Health.integrity,"the bracing rig can be hurt during its shift");
            yield return Until(()=>!boss.Shifting,3);
            Assert.Greater(Flat(Lead.transform.position,start),2,"the shift's shockwave did not shove the hero clear");
            var mites=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(e=>e.definition.id=="Scrapmite"&&e.Health.Alive).ToArray();
            Assert.AreEqual(2,mites.Length,"the shift called no Scrapmites out of the spoil");
            rig.Health.Receive(new DamagePacket{source=Lead.Health,amount=rig.Health.maximum*50,type=DamageType.Plasma});
            yield return null;yield return null;
            Assert.IsTrue(mites.All(m=>m==null),"called mites outlive the fight");
        }

        [UnityTest] public IEnumerator TheBellowsInhaleDrawsAStandingHeroIn()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain bellows=null;BossController boss=null;yield return Boss("BellowsBelow",new Vector3(0,.1f,0),(e,b)=>{bellows=e;boss=b;});
            Place(Lead,new Vector3(0,.1f,-8));Place(Partner,new Vector3(-14,.1f,-25));Fresh(Lead);
            float start=Lead.Health.integrity;
            Assert.IsTrue(boss.Perform("Breath"));yield return null;
            var ring=Marking(boss);Assert.IsNotNull(ring,"the breath marks no ring");
            float before=Flat(Lead.transform.position,bellows.transform.position);
            yield return Until(()=>!boss.Busy,4);
            float after=Flat(Lead.transform.position,bellows.transform.position);
            TestContext.WriteLine($"BELLOWS_PULL before={before:F2} after={after:F2}");
            Assert.Less(after,before-.8f,"the inhale did not draw the standing hero in");
            Assert.Less(Lead.Health.integrity,start,"the exhale missed a hero drawn inside its ring");
        }

        [UnityTest] public IEnumerator TheBellowsBubblesHuntTheHeroAndBurstWhereMarked()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain bellows=null;BossController boss=null;yield return Boss("BellowsBelow",new Vector3(0,.1f,0),(e,b)=>{bellows=e;boss=b;});
            bellows.Health.integrity=bellows.Health.maximum*.45f;yield return null;Assert.GreaterOrEqual(boss.Phase,2);
            Place(Lead,new Vector3(6,.1f,-3));Place(Partner,new Vector3(-14,.1f,-25));Fresh(Lead);
            float health=Lead.Health.integrity;
            Assert.IsTrue(boss.Perform("Bubbles"));yield return null;
            var marks=Telegraph.Live.Where(t=>t.owner==boss).ToArray();Assert.GreaterOrEqual(marks.Length,5,"no bubbles are marked");
            float start=marks.Average(m=>Flat(m.Origin,Lead.transform.position));
            yield return new WaitForSecondsRealtime(1.6f);
            float later=marks.Where(m=>m!=null).Average(m=>Flat(m.Origin,Lead.transform.position));
            Assert.Less(later,start-2f,"the bubbles do not drift after the hero");
            yield return Until(()=>!boss.Busy,5);
            Assert.Less(Lead.Health.integrity,health,"bubbles that reached a standing hero never burst on him");
        }

        [UnityTest] public IEnumerator TheWoundedCantorSingsRotatingSpiralArms()
        {
            yield return Zone("Arena_Flight");
            EnemyBrain cantor=null;BossController boss=null;yield return Boss("Cantor",new Vector3(0,1,10),(e,b)=>{cantor=e;boss=b;});
            Place(Lead,new Vector3(0,1,-6));Place(Partner,new Vector3(30,1,-30));Lead.Health.InvulnerableUntil=GameTime.Now+30;
            cantor.Health.integrity=cantor.Health.maximum*.6f;yield return null;Assert.AreEqual(2,boss.HealthPhase);
            foreach(var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject);
            yield return null;
            Assert.IsTrue(boss.Perform("SongSpiral"));
            var seen=new HashSet<Projectile>();var headings=new List<float>();float end=Time.unscaledTime+4;
            while(boss.Busy&&Time.unscaledTime<end)
            {
                foreach(var shot in Projectile.Live)if(shot!=null&&shot.Threatens(true)&&seen.Add(shot))
                {var v=shot.Velocity;headings.Add(Mathf.Atan2(v.x,v.z)*Mathf.Rad2Deg);}
                yield return null;
            }
            TestContext.WriteLine($"SONG_SPIRAL shots={seen.Count}");
            Assert.GreaterOrEqual(seen.Count,30,"the spiral sings too few shots");
            var buckets=headings.Select(h=>Mathf.FloorToInt(Mathf.Repeat(h,360)/30)).Distinct().Count();
            Assert.GreaterOrEqual(buckets,10,"the spiral arms do not rotate around the Cantor");
        }

        [UnityTest] public IEnumerator TheBossBarNamesTheFightAndMarksItsPhases()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            Place(Lead,new Vector3(0,.1f,-14));Place(Partner,new Vector3(-12,.1f,-24));
            foreach(var m in PartyController.Current.members)m.Health.InvulnerableUntil=GameTime.Now+60;
            var hud=Object.FindFirstObjectByType<GameHud>();
            TMPro.TMP_Text Named(string name)=>hud.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(t=>t.name==name);
            UnityEngine.UI.Image Bar(string name)=>hud.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(t=>t.name==name);
            yield return null;
            Assert.IsNotNull(Named("BossName"),"the HUD has no boss bar");
            Assert.IsFalse(Named("BossName").gameObject.activeInHierarchy,"a passive boss shows its bar");
            rig.Passive=false;yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(Named("BossName").gameObject.activeInHierarchy,"the boss bar does not appear for an awake boss");
            Assert.AreEqual("THE BURROWER",Named("BossName").text);
            rig.Health.integrity=rig.Health.maximum*.5f;yield return new WaitForSecondsRealtime(.2f);
            var fill=Bar("BossIntegrity");var back=Bar("BossIntegrityBack");
            Assert.That(fill.rectTransform.rect.width/back.rectTransform.rect.width,Is.EqualTo(.5f).Within(.02f),"the bar does not show the boss's integrity");
            Assert.AreEqual(2,hud.GetComponentsInChildren<UnityEngine.UI.Image>(true).Count(i=>i.name.StartsWith("BossNotch")&&i.gameObject.activeInHierarchy),"the bar does not mark the Burrower's two phase thresholds");
            Place(Lead,new Vector3(0,.1f,-60));Place(Partner,new Vector3(4,.1f,-60));yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(Named("BossName").gameObject.activeInHierarchy,"the boss bar stays up far from the fight");
        }

        [UnityTest] public IEnumerator TheWoundedBurrowerFightsWithItsWholeSet()
        {
            yield return Zone("Arena_Ground");
            EnemyBrain rig=null;BossController boss=null;yield return Boss("Burrower",new Vector3(0,.1f,0),(e,b)=>{rig=e;boss=b;});
            rig.Health.integrity=rig.Health.maximum*.25f;
            Place(Lead,new Vector3(0,.1f,-9));Place(Partner,new Vector3(-12,.1f,-24));
            foreach(var m in PartyController.Current.members){m.Health.maximum=m.Health.integrity=100000;m.Health.InvulnerableUntil=GameTime.Now+120;}
            rig.Passive=false;
            yield return Until(()=>log.Count(l=>l.StartsWith("BOSS_MOVE Burrower"))>=7,50);
            var moves=log.Where(l=>l.StartsWith("BOSS_MOVE Burrower")).Select(l=>l.Split(' ')[2]).Distinct().ToArray();
            TestContext.WriteLine("BURROWER_MOVES "+string.Join(",",moves));
            Assert.GreaterOrEqual(moves.Length,4,"the wounded Burrower repeats a narrow set: "+string.Join(",",moves));
            Assert.IsTrue(moves.Contains("Burrow")||moves.Contains("SeismicRings"),"the wounded Burrower never uses its later-phase moves");
        }
    }
}
