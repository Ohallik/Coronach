using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Lattice.Tests.PlayMode
{
    public sealed class DrifterFanTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Flight");float until=Time.unscaledTime+10;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<until)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members)
            {hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;Place(hero,new Vector3(220,1,0));}
            yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static void Place(CombatActor hero,Vector3 position)
        {
            var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.SetPositionAndRotation(position,Quaternion.identity);cc.enabled=true;
            hero.GetComponent<FlightMotor>().Halt();hero.Health.InvulnerableUntil=0;hero.target=null;hero.TargetLocked=false;
        }
        IEnumerator CloseFan(string character,bool record)
        {
            var party=PartyController.Current;if(party.Active.character!=character)Assert.IsTrue(party.Swap());
            foreach(var hero in party.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            var victim=party.Active;Place(victim,new Vector3(200,1,0));
            var source=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),new Vector3(200,1,2.25f));source.Passive=true;
            yield return null;yield return null;Physics.SyncTransforms();
            var hits=new List<float>();Action<Health,DamagePacket,float> observe=(_,packet,damage)=>{if(packet.source==source.Health&&damage>0)hits.Add(damage);};
            victim.Health.Damaged+=observe;var seen=new HashSet<Projectile>();
            ProjectileProvenance.Capture capture=record?ProjectileProvenance.Begin(()=>Time.realtimeSinceStartupAsDouble):null;
            try
            {
                typeof(EnemyBrain).GetField("nextAttack",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(source,Time.time);
                source.Passive=false;bool told=false,released=false;float until=Time.unscaledTime+4;
                while(Time.unscaledTime<until)
                {
                    yield return null;
                    foreach(var shot in Projectile.Live)if(shot.Threatens(true))seen.Add(shot);
                    told|=source.Telegraphing;
                    if(told&&!source.Telegraphing&&source.Attacking){released=true;source.Passive=true;break;}
                }
                Assert.IsTrue(told&&released,"fixture must use the actual Drifter tell and emission");
                yield return new WaitForSecondsRealtime(.4f);
                Debug.Log($"DRIFTER_FAN hero={character} capture={record} shots={seen.Count} damageEvents={hits.Count} total={hits.Sum():R}");
                Assert.AreEqual(3,seen.Count,"the spread must still emit all three actual projectiles");
                if(capture!=null){Assert.AreEqual(3,capture.Launches);Assert.AreEqual(1,capture.Volleys);}
                Assert.AreEqual(1,hits.Count,"one close-range fan stacks multiple damage events on one hero hull");
                Assert.Greater(hits[0],0,"a real fan must still damage its victim");
            }
            finally{capture?.Dispose();victim.Health.Damaged-=observe;}
        }
        EnemyBrain Source(Vector3 position)
        {var source=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("ChoristerDrifter"),position);source.Passive=true;return source;}
        Dictionary<string,int> RecordHits()
        {
            var counts=new Dictionary<string,int>();
            foreach(var hero in PartyController.Current.members)
            {
                string id=hero.character;counts[id]=0;
                hero.Health.Damaged+=(_,packet,amount)=>{if(amount>0)counts[id]++;};
            }
            return counts;
        }
        IEnumerator Emit(EnemyBrain[] sources,Action<List<Projectile>> emitted=null)
        {
            yield return null;yield return null;Physics.SyncTransforms();
            var before=Projectile.Live.ToArray();var seen=new HashSet<Projectile>();
            var told=new bool[sources.Length];var released=new bool[sources.Length];
            foreach(var source in sources)
            {
                typeof(EnemyBrain).GetField("nextAttack",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(source,Time.time);
                source.Passive=false;
            }
            float until=Time.unscaledTime+4;
            while(Time.unscaledTime<until&&!released.All(x=>x))
            {
                yield return null;
                foreach(var shot in Projectile.Live)if(shot.Threatens(true)&&!before.Contains(shot))seen.Add(shot);
                for(int i=0;i<sources.Length;i++)
                {
                    told[i]|=sources[i].Telegraphing;
                    if(told[i]&&!sources[i].Telegraphing&&sources[i].Attacking){released[i]=true;sources[i].Passive=true;}
                }
            }
            Assert.IsTrue(told.All(x=>x)&&released.All(x=>x),"fixture did not complete each real tell");
            Assert.AreEqual(3*sources.Length,seen.Count,"each real fan must keep three projectiles");
            emitted?.Invoke(seen.ToList());
        }
        [UnityTest] public IEnumerator LaterVolleyDamagesTheSameHullAgainAfterPoolReuse()
        {
            var victim=PartyController.Current.Active;Place(victim,new Vector3(200,1,0));var hits=RecordHits();
            var source=Source(new Vector3(200,1,8));int[] first=null,second=null;
            yield return Emit(new[]{source},shots=>first=shots.Select(p=>p.GetInstanceID()).ToArray());
            yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(1,hits[victim.character]);
            yield return Emit(new[]{source},shots=>second=shots.Select(p=>p.GetInstanceID()).ToArray());
            yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(2,hits[victim.character],"a prior fan suppresses a later attack");
            CollectionAssert.AreEquivalent(first,second,"fixture must really reuse the projectile pool");
        }
        [UnityTest] public IEnumerator IndependentDriftersKeepIndependentDamage()
        {
            var victim=PartyController.Current.Active;Place(victim,new Vector3(200,1,0));var hits=RecordHits();
            var a=Source(new Vector3(198,1,8));var b=Source(new Vector3(202,1,8));
            yield return Emit(new[]{a,b});yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(2,hits[victim.character],"distinct enemies must not share a damage budget");
        }
        [UnityTest] public IEnumerator OneFanCanStillDamageBothHeroHulls()
        {
            var party=PartyController.Current;var active=party.Active;var other=party.members.First(h=>h!=active);
            Place(active,new Vector3(200,1,0));Place(other,new Vector3(203.5f,1,0));var hits=RecordHits();
            var source=Source(new Vector3(200,1,14));yield return Emit(new[]{source});yield return new WaitForSecondsRealtime(1.7f);
            Assert.AreEqual(1,hits[active.character]);Assert.AreEqual(1,hits[other.character],"first victim consumes the whole spread for the other hero");
        }
        [UnityTest] public IEnumerator ReleasedFanRetainsDamageAfterItsDrifterDies()
        {
            var victim=PartyController.Current.Active;Place(victim,new Vector3(200,1,0));var hits=RecordHits();
            var source=Source(new Vector3(200,1,8));yield return Emit(new[]{source});
            source.Health.Receive(new DamagePacket{source=victim.Health,amount=100000,type=DamageType.Pulse});Assert.IsFalse(source.Health.Alive);
            yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(1,hits[victim.character],"released fan was erased or lost its per-hull budget on source death");
        }
        [UnityTest] public IEnumerator PausedReleasedFanKeepsItsGeometryAndDamageBudget()
        {
            var victim=PartyController.Current.Active;Place(victim,new Vector3(200,1,0));var hits=RecordHits();
            var source=Source(new Vector3(200,1,8));List<Projectile> shots=null;yield return Emit(new[]{source},p=>shots=p);
            GameTime.Paused=true;var positions=shots.Select(p=>p.transform.position).ToArray();
            yield return new WaitForSecondsRealtime(.4f);
            CollectionAssert.AreEqual(positions,shots.Select(p=>p.transform.position).ToArray());Assert.AreEqual(0,hits[victim.character]);
            GameTime.Paused=false;yield return new WaitForSecondsRealtime(1.5f);Assert.AreEqual(1,hits[victim.character]);
        }
        [UnityTest] public IEnumerator InvulnerableContactDoesNotSpendTheDamageBudget()
        {
            var victim=PartyController.Current.Active;Place(victim,new Vector3(200,1,0));
            var source=Source(new Vector3(200,1,8));DamagePacket packet=default;
            yield return Emit(new[]{source},shots=>packet=(DamagePacket)typeof(Projectile).GetField("packet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(shots[0]));
            GameTime.Paused=true;
            // Policy fixture uses the packet from an actual emitted fan. The
            // physical integration is checked separately above for both heroes.
            victim.Health.InvulnerableUntil=GameTime.Now+10;Assert.AreEqual(0,victim.Health.Receive(packet));
            victim.Health.InvulnerableUntil=0;Assert.Greater(victim.Health.Receive(packet),0,"immune contact spent the attack's budget");
            Assert.AreEqual(0,victim.Health.Receive(packet),"same fan damages a vulnerable hull twice");
        }

        [UnityTest] public IEnumerator OneFanDamagesTarenOnlyOnceWithoutCapture()=>CloseFan("Taren",false);
        [UnityTest] public IEnumerator OneFanDamagesSelaOnlyOnceDuringCapture()=>CloseFan("Sela",true);
    }
}
