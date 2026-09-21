using System.Collections;
using System.IO;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Controlled damage comparison using real attack volumes and real fabricated equipment.</summary>
    public sealed class BalanceProbe:MonoBehaviour
    {
        CombatActor actor;float measured;int hitCount;bool failed;
        IEnumerator Start()
        {
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            yield return new WaitForSecondsRealtime(1);
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Destroy(enemy.gameObject);
            foreach(var member in PartyController.Current.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            actor=PartyController.Current.Active;var state=GameServices.Current.State;var memberState=state.party.Find(m=>m.id==actor.character);memberState.level=1;memberState.equipped.Clear();PartyController.Current.RefreshStats();
            yield return Fight("Ridgehound",true);int ridgeHits=hitCount;if(ridgeHits!=4)Fail("level-one Ridgehound expected four ordinary combo hits, got "+ridgeHits);
            memberState.level=3;PartyController.Current.RefreshStats();yield return Fight("Burrower",false);float baseline=measured;
            var inv=new Inventory(state);var part=GameCatalog.Find<TechPartDef>("EdgesT2");var fabrication=new Fabrication(state);state.materials["ScrapAlloy"]=15;state.materials["RidgeCrystal"]=2;
            for(int i=0;i<3;i++)if(!fabrication.Craft(GameCatalog.Find<RecipeDef>("EdgesT1"),true,1,out _))Fail("T1 practice recipe refused");
            if(!fabrication.Craft(GameCatalog.Find<RecipeDef>("EdgesT2"),true,1,out var edge))Fail("T2 fabrication refused");else inv.Equip(memberState,edge,part);PartyController.Current.RefreshStats();
            yield return Fight("Burrower",false);float equipped=measured,ratio=equipped/baseline;
            string path=DevArgs.Value("-balance");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(new{ridgeHits,level=3,baselineSeconds=baseline,t2Seconds=equipped,ratio,mode="stationary target, Taren basic attacks, actual colliders, no skills or partner damage"},Newtonsoft.Json.Formatting.Indented));
            if(ratio<.60f||ratio>.77f)Fail("T2 duration ratio outside 0.60–0.77: "+ratio);
            string shot=DevArgs.Value("-screenshot");if(!string.IsNullOrEmpty(shot)){ScreenCapture.CaptureScreenshot(shot);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(shot);yield return new WaitForSecondsRealtime(.6f);}
            if(!failed)Debug.Log("BALANCE_OK ridgeHits="+ridgeHits+" T2ratio="+ratio);Application.Quit(failed?1:0);
        }
        IEnumerator Fight(string id,bool countHits)
        {
            actor.combo=0;actor.charge=0;actor.target=null;Random.InitState(1729);hitCount=0;
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=new Vector3(0,0,-8);cc.enabled=true;
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),new Vector3(0,0,-5.8f));enemy.Passive=true;
            actor.motor.Move(Vector2.up,false,true);actor.target=enemy.Health;yield return new WaitForSecondsRealtime(.6f);
            // Start each trial at the same facing, distance, random seed, HP and combo phase.
            Random.InitState(1729);float start=Time.time,until=Time.realtimeSinceStartup+150;
            while(enemy!=null&&enemy.Health.Alive&&Time.realtimeSinceStartup<until)
            {
                actor.motor.Move(Vector2.zero,false,true);if(actor.Attack())hitCount++;yield return null;
            }
            measured=Time.time-start;if(enemy!=null&&enemy.Health.Alive)Fail(id+" damage trial timed out");
            yield return new WaitForSecondsRealtime(.6f);
        }
        void Fail(string why){failed=true;Debug.LogError("FAILED: balance "+why);}
    }
}
