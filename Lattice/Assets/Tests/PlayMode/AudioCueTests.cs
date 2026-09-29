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
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Lattice.Tests.PlayMode
{
    // Every cue the game actually starts, with the transform that owns it.
    sealed class CueLog:System.IDisposable
    {
        readonly List<(string key,Transform owner)> cues=new();
        readonly System.Action<string,Transform> record;
        public CueLog(){record=(key,owner)=>cues.Add((key,owner));AudioManager.CueStarted+=record;}
        // "swing" matches the staged variations swing_0..swing_9; single clips match exactly.
        static bool Is(string key,string family)=>key==family||key.Length==family.Length+2&&key.StartsWith(family+"_")&&char.IsDigit(key[^1]);
        public int Count(string family,Transform owner=null)=>cues.Count(c=>Is(c.key,family)&&(owner==null||c.owner==owner));
        public string All=>string.Join(", ",cues.Select(c=>c.key));
        public void Clear()=>cues.Clear();
        public void Dispose()=>AudioManager.CueStarted-=record;
    }

    public sealed class AudioGroundCueTests
    {
        GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var shop=Object.FindFirstObjectByType<ShopUi>();if(shop!=null&&shop.IsOpen)shop.Close();
            if(fixture!=null)Object.Destroy(fixture);
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }
        static CombatActor Isolate(CombatActor actor)
        {
            var controller=actor.GetComponent<CharacterController>();controller.enabled=false;
            actor.transform.SetPositionAndRotation(new Vector3(200,0,0),Quaternion.identity);controller.enabled=true;
            actor.target=null;actor.TargetLocked=false;Physics.SyncTransforms();return actor;
        }
        [UnityTest] public IEnumerator MeleeMissesSwingAndOnlyDamagedBodiesImpactByTheirActualState()
        {
            var taren=Isolate(PartyController.Current.Active);Assert.AreEqual("Taren",taren.character);
            yield return null;
            using var log=new CueLog();
            Assert.IsTrue(taren.Attack());yield return new WaitForSecondsRealtime(.7f);
            Assert.Greater(log.Count("swing",taren.transform),0,"an ordinary swing is silent: "+log.All);
            Assert.AreEqual(0,log.Count("hit_soft")+log.Count("hit_armor")+log.Count("impactMetal_000"),"a miss sounds like a hit: "+log.All);

            fixture=new GameObject("Impact fixture");
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),taren.transform.position+Vector3.forward*1.9f);
            enemy.transform.SetParent(fixture.transform);enemy.Passive=true;enemy.enabled=false;
            var body=enemy.Health;body.maximum=body.integrity=100000;body.breakThreshold=1e9f;
            // Expected armor comes from the body's state at the moment of each
            // damaging contact: resisted type or a live, unbroken shield.
            var expected=new List<bool>();var types=new HashSet<DamageType>();
            body.Damaged+=(h,p,amount)=>{types.Add(p.type);if(amount>0)expected.Add(p.type==h.resistance||Time.time<h.ShieldUntil&&!h.Broken);};
            Physics.SyncTransforms();yield return null;

            // Learn the chain's damage types, then make ordinary contact unresisted.
            for(int strike=0;strike<3;strike++){Assert.IsTrue(taren.Attack());yield return new WaitForSecondsRealtime(.7f);}
            Assert.Greater(expected.Count,0,"fixture swing never reached the body");
            var unused=System.Enum.GetValues(typeof(DamageType)).Cast<DamageType>().Where(t=>!types.Contains(t)).ToArray();
            Assert.Greater(unused.Length,0,"the chain uses every damage type");
            body.resistance=unused[0];
            expected.Clear();log.Clear();
            for(int strike=0;strike<3;strike++){Assert.IsTrue(taren.Attack());yield return new WaitForSecondsRealtime(.7f);}
            Assert.IsFalse(types.Contains(body.resistance),"fixture resistance collides with the chain");
            Assert.Greater(expected.Count,0);
            Assert.AreEqual(expected.Count(e=>!e),log.Count("hit_soft",enemy.transform),"each unresisted hit must sound soft once: "+log.All);
            Assert.AreEqual(expected.Count(e=>e),log.Count("hit_armor",enemy.transform),log.All);
            Assert.Greater(log.Count("hit_soft",enemy.transform),0);
            Assert.AreEqual(0,log.Count("impactMetal_000"),"legacy window-start impact remains");

            body.ShieldUntil=Time.time+60;expected.Clear();log.Clear();
            for(int strike=0;strike<2;strike++){Assert.IsTrue(taren.Attack());yield return new WaitForSecondsRealtime(.7f);}
            Assert.Greater(expected.Count,0);Assert.IsTrue(expected.All(e=>e));
            Assert.AreEqual(expected.Count,log.Count("hit_armor",enemy.transform),"a shielded body must sound armored: "+log.All);
            Assert.AreEqual(0,log.Count("hit_soft",enemy.transform));
        }
        [UnityTest] public IEnumerator SelaNeedlesAreHerOwnCue()
        {
            Assert.IsTrue(PartyController.Current.Swap());yield return new WaitForSecondsRealtime(.6f);
            var sela=Isolate(PartyController.Current.Active);Assert.AreEqual("Sela",sela.character);
            yield return null;
            using var log=new CueLog();
            Assert.IsTrue(sela.Attack());yield return new WaitForSecondsRealtime(.6f);
            Assert.Greater(log.Count("needle_sela",sela.transform),0,"Sela's shot has no needle cue: "+log.All);
            Assert.AreEqual(0,log.Count("needle_taren")+log.Count("laserSmall_000"),"Sela's needle uses another shooter's cue: "+log.All);
            // Her Static Net launch must not borrow the hostile shot either.
            log.Clear();sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.Greater(log.Count("needle_sela",sela.transform),0,"Static Net launch is silent: "+log.All);
            Assert.AreEqual(0,log.Count("laserSmall_000"),"Static Net sounds like hostile fire: "+log.All);
        }
        [UnityTest] public IEnumerator DeathsDownAndReviveSoundFromTheBodyThatChanged()
        {
            var taren=Isolate(PartyController.Current.Active);Assert.AreEqual("Taren",taren.character);
            fixture=new GameObject("Lifecycle fixture");
            var hound=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Ridgehound"),taren.transform.position+new Vector3(5,0,5));
            var husk=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("SentinelHusk"),taren.transform.position+new Vector3(-5,0,5));
            foreach(var enemy in new[]{hound,husk}){enemy.transform.SetParent(fixture.transform);enemy.Passive=true;}
            Physics.SyncTransforms();yield return null;
            using var log=new CueLog();
            foreach(var enemy in new[]{hound,husk})
                enemy.Health.Receive(new DamagePacket{source=taren.Health,amount=enemy.Health.maximum*20,type=DamageType.Pulse});
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(hound.Health.Alive);Assert.IsFalse(husk.Health.Alive);
            Assert.AreEqual(1,log.Count("death_creature",hound.transform),"a creature death has no body cue: "+log.All);
            Assert.AreEqual(1,log.Count("death_mech",husk.transform),"a machine failure has no body cue: "+log.All);
            Assert.AreEqual(0,log.Count("death_mech",hound.transform)+log.Count("death_creature",husk.transform),"death cue ignores what the body is: "+log.All);
            Assert.AreEqual(0,log.Count("explosionCrunch_000"),"kills still use the generic killer-side crunch: "+log.All);

            log.Clear();
            taren.Health.Receive(new DamagePacket{amount=taren.Health.maximum*5,type=DamageType.Kinetic});
            yield return new WaitForSecondsRealtime(.2f);
            Assert.IsFalse(taren.Health.Alive);
            Assert.AreEqual(1,log.Count("hero_down",taren.transform),"a downed hero is silent: "+log.All);
            log.Clear();taren.Health.Heal(taren.Health.maximum*.5f);yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(1,log.Count("hero_revive",taren.transform),"revival is silent: "+log.All);
            Assert.AreEqual(0,log.Count("hero_down"));
        }
        [UnityTest] public IEnumerator GroundDodgeIsABodyAndRefractInterceptionRings()
        {
            var taren=Isolate(PartyController.Current.Active);Assert.AreEqual("Taren",taren.character);
            yield return null;
            using var log=new CueLog();
            Assert.IsTrue(taren.Dodge(Vector3.right));yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1,log.Count("dodge",taren.transform),"ground dodge has no body cue: "+log.All);
            Assert.AreEqual(0,log.Count("thrusterFire_000"),"a ground dodge still plays the ship thruster: "+log.All);

            Assert.IsTrue(PartyController.Current.Swap());yield return new WaitForSecondsRealtime(.6f);
            var sela=Isolate(PartyController.Current.Active);Assert.AreEqual("Sela",sela.character);
            sela.charge=100;sela.cooldowns[3]=0;Assert.IsTrue(sela.Skill(3));yield return new WaitForSecondsRealtime(.65f);
            Assert.IsTrue(sela.Refracting,"fixture Refract never armed");
            log.Clear();float before=sela.Health.integrity;
            sela.Health.Receive(new DamagePacket{amount=10,type=DamageType.Beam});yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(before,sela.Health.integrity,"fixture shot was not intercepted");
            Assert.AreEqual(1,log.Count("deflect",sela.transform),"Refract interception is silent: "+log.All);
        }
        [UnityTest] public IEnumerator MenuCuesFollowTheActualOutcome()
        {
            var shop=Object.FindFirstObjectByType<ShopUi>();Assert.IsNotNull(shop);
            var state=GameServices.Current.State;state.scrip=0;
            shop.Open(null);yield return null;yield return null;
            using var log=new CueLog();
            Button Row(string name){var go=GameObject.Find(name);Assert.IsNotNull(go,name);return go.GetComponent<Button>();}
            Row("ShopRow1").onClick.Invoke();yield return null;
            Assert.AreEqual(1,log.Count("ui_error"),"a refused purchase is silent: "+log.All);
            Assert.AreEqual(0,log.Count("ui_confirm"),"a refused purchase sounded accepted");

            log.Clear();state.scrip=100000;Row("ShopRow1").onClick.Invoke();yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(1,log.Count("ui_confirm"),log.All);Assert.AreEqual(0,log.Count("ui_error"));
            Assert.AreEqual(0,log.Count("ui_move"),"rebuilding the page under the same focus is not navigation: "+log.All);

            // Space inputs like a person: the UI cue cooldown merges repeats within 35 ms.
            EventSystem.current.SetSelectedGameObject(Row("ShopRow1").gameObject);yield return new WaitForSecondsRealtime(.1f);log.Clear();
            EventSystem.current.SetSelectedGameObject(Row("ShopRow2").gameObject);yield return new WaitForSecondsRealtime(.1f);
            Assert.AreEqual(1,log.Count("ui_move"),"moving between rows is silent: "+log.All);

            log.Clear();
            var back=Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b=>b.GetComponentInChildren<TMP_Text>()?.text=="Back");
            back.onClick.Invoke();yield return null;
            Assert.IsFalse(shop.IsOpen);
            Assert.AreEqual(1,log.Count("ui_cancel"),log.All);Assert.AreEqual(0,log.Count("ui_confirm"),"Back sounded like confirming");
        }
    }

    public sealed class AudioFlightCueTests
    {
        CombatActor taren,sela;GameObject fixture;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;if(hero.character=="Taren")taren=hero;else sela=hero;}
            foreach(AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();if(fixture!=null)Object.Destroy(fixture);SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor hero,Vector3 position)
        {
            var cc=hero.GetComponent<CharacterController>();var motor=hero.GetComponent<FlightMotor>();cc.enabled=false;motor.Halt();
            hero.transform.SetPositionAndRotation(position,Quaternion.identity);cc.enabled=true;hero.target=null;Physics.SyncTransforms();
        }
        [UnityTest] public IEnumerator ShipShotsStayDistinctFromHostileFire()
        {
            Place(taren,new Vector3(200,1,0));Place(sela,new Vector3(230,1,0));yield return null;
            using var log=new CueLog();
            Assert.IsTrue(taren.Attack());yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(log.Count("needle_taren",taren.transform),0,"Taren's ship shot has no cue: "+log.All);
            Assert.IsTrue(sela.Attack());yield return new WaitForSecondsRealtime(.3f);
            Assert.Greater(log.Count("needle_sela",sela.transform),0,"Sela's ship shot has no cue: "+log.All);
            Assert.AreEqual(0,log.Count("needle_sela",taren.transform)+log.Count("needle_taren",sela.transform));
            Assert.AreEqual(0,log.Count("laserSmall_000"),"a hero shot uses the hostile cue: "+log.All);
            log.Clear();sela.charge=100;sela.cooldowns[2]=0;Assert.IsTrue(sela.Skill(2));yield return new WaitForSecondsRealtime(1);
            Assert.Greater(log.Count("needle_sela",sela.transform),0,"flight Static Net launch is silent: "+log.All);
            Assert.AreEqual(0,log.Count("laserSmall_000"),"flight Static Net sounds like hostile fire: "+log.All);

            fixture=new GameObject("Hostile fire fixture");
            var drifter=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(200,1,9));
            drifter.transform.SetParent(fixture.transform);
            float deadline=Time.unscaledTime+12;
            while(log.Count("laserSmall_000",drifter.transform)==0&&Time.unscaledTime<deadline)yield return null;
            Assert.Greater(log.Count("laserSmall_000",drifter.transform),0,"hostile fire is silent: "+log.All);
        }
    }
}
