using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class PerformanceProbe:MonoBehaviour
    {
        [System.Serializable]sealed class Result{public string gpu,zone;public int width,height,samples,minimumEnemies;public float seconds,fps,medianMs,p95Ms,p99Ms;}
        IEnumerator Start()
        {
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            yield return new WaitForSecondsRealtime(2);
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            var actor=PartyController.Current.Active;actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.Health.InvulnerableUntil=float.PositiveInfinity;
            var enemies=new List<EnemyBrain>();
            for(int i=0;i<12;i++)enemies.Add(Spawn(i));
            var times=new List<float>();float until=Time.realtimeSinceStartup+30;int minimum=12;
            while(Time.realtimeSinceStartup<until)
            {
                for(int i=0;i<12;i++)if(enemies[i]==null||!enemies[i].Health.Alive)enemies[i]=Spawn(i);
                minimum=Mathf.Min(minimum,enemies.Count(e=>e!=null&&e.Health.Alive));
                actor.target=enemies.First(e=>e!=null&&e.Health.Alive).Health;var d=actor.target.transform.position-actor.transform.position;d.y=0;
                actor.motor.Move(new Vector2(d.x,d.z).normalized*.03f,false,true);actor.Attack();
                times.Add(Time.unscaledDeltaTime*1000);yield return null;
            }
            var sorted=times.OrderBy(t=>t).ToArray();float total=times.Sum()/1000;
            var result=new Result{gpu=SystemInfo.graphicsDeviceName,zone=SceneFlow.Current.Zone,width=Screen.width,height=Screen.height,samples=times.Count,minimumEnemies=minimum,seconds=total,fps=times.Count/total,medianMs=sorted[sorted.Length/2],p95Ms=sorted[(int)(sorted.Length*.95f)],p99Ms=sorted[(int)(sorted.Length*.99f)]};
            string path=DevArgs.Value("-perf");Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,JsonUtility.ToJson(result,true));
            string shot=DevArgs.Value("-screenshot");ScreenCapture.CaptureScreenshot(shot);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(shot);yield return new WaitForSecondsRealtime(.6f);
            bool ok=result.fps>=60&&result.minimumEnemies>=12&&result.samples>=600&&result.width==1920&&result.height==1080&&result.zone=="Gullet_Tunnel";
            if(ok)Debug.Log($"PERF_OK fps={result.fps:0.0} median={result.medianMs:0.00} p95={result.p95Ms:0.00} enemies={minimum}");else Debug.LogError("FAILED: performance contract "+JsonUtility.ToJson(result));Application.Quit(ok?0:1);
        }
        EnemyBrain Spawn(int i)
        {
            var actor=PartyController.Current.Active;float a=i*Mathf.PI/6;
            return ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(i%3==0?"ChoristerDrifter":"ChoristerDart"),actor.transform.position+new Vector3(Mathf.Cos(a)*12,0,Mathf.Sin(a)*12));
        }
    }
}
