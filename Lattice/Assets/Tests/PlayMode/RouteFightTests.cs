using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>The replay routes of chapter 3 on, checked against the built zones for the one thing their leg checks don't look at: where a route stands as it presses lock,
    /// the fight it names has begun. A fight begins when the party is inside its volume, and a route written from the lane's sides put the ship outside two of three barges'
    /// fights, where it pressed lock at nothing for three minutes (first player run, November 13). One case a zone, so a zone's own run reaches it.</summary>
    public sealed class RouteFightTests
    {
        /// <summary>The first route the generators of chapter 3 on wrote; the sixteen before it were recorded against the player.</summary>
        const int FirstRoute=17;
        sealed class Lock{public string route,step,scene,id,after;public Vector3 at;public float tolerance;public bool boss;}
        /// <summary>How much further inside a boss's volume its door walk has to end: the fight waits until the partner is through its gate too, and the partner stands this far
        /// behind whoever leads. A walk that ended seven tenths of a metre inside the Rotbloom's hall left Sela in the doorway and the fight unbegun (November 14).</summary>
        const float PartnerRoom=3;
        static List<Lock> Locks()
        {
            var locks=new List<Lock>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string file in System.IO.Directory.GetFiles(root,"chapter-*.json").OrderBy(f=>f,System.StringComparer.Ordinal))
            {
                string name=System.IO.Path.GetFileNameWithoutExtension(file);var parts=name.Split('-');
                if(parts.Length<3||!int.TryParse(parts[1],out int number)||number<FirstRoute)continue;
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null,after=null,door=null;Vector3? at=null;float tolerance=0;
                foreach(var step in route.steps)
                {
                    if(step.expectedScene!=scene){scene=step.expectedScene;at=null;door=null;}
                    if(step.navigate)
                    {
                        // After a chase the party is wherever the fight left it: a lock pressed from there is judged by nobody.
                        if(step.approachTarget||step.approachPartner){at=null;door=null;}else{at=step.point;after=step.name;tolerance=step.tolerance;door=string.IsNullOrEmpty(step.bossDoor)?null:step.bossDoor;}
                        continue;
                    }
                    if(!step.name.StartsWith("lock ")||!(at is Vector3 here))continue;
                    // 'lock the Seedthane': a boss, locked by its name where its door walk (bossDoor) ended.
                    if(!step.name.StartsWith("lock the "))locks.Add(new Lock{route=name,step=step.name,scene=scene,id=step.name.Substring(5),after=after,at=here,tolerance=tolerance});
                    else if(door!=null)locks.Add(new Lock{route=name,step=step.name,scene=scene,id=door,after=after,at=here,tolerance=tolerance,boss=true});
                }
            }
            return locks;
        }
        public static IEnumerable<string> Zones()=>Locks().Select(l=>l.scene).Distinct().OrderBy(z=>z,System.StringComparer.Ordinal);
        /// <summary>A press that takes the party out of a zone, made where a walk ended: the step that carries the next zone's name, pressed with the interact button outside any menu.</summary>
        sealed class Leave{public string route,step,scene,to,after;public Vector3 at;public float tolerance;}
        static List<Leave> Leaves()
        {
            var leaves=new List<Leave>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string file in System.IO.Directory.GetFiles(root,"chapter-*.json").OrderBy(f=>f,System.StringComparer.Ordinal))
            {
                string name=System.IO.Path.GetFileNameWithoutExtension(file);var parts=name.Split('-');
                if(parts.Length<3||!int.TryParse(parts[1],out int number)||number<FirstRoute)continue;
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null,after=null;Vector3? at=null;float tolerance=0;
                foreach(var step in route.steps)
                {
                    string named=string.IsNullOrEmpty(step.expectedScene)?scene:step.expectedScene;
                    if(named!=scene)
                    {
                        if(scene!=null&&scene!="Title"&&at is Vector3 here&&step.buttons!=null&&step.buttons.Contains("South")&&string.IsNullOrEmpty(step.pauseUi))
                            leaves.Add(new Leave{route=name,step=step.name,scene=scene,to=named,after=after,at=here,tolerance=tolerance});
                        scene=named;at=null;
                    }
                    if(!step.navigate)continue;
                    if(step.approachTarget||step.approachPartner)at=null;else{at=step.point;after=step.name;tolerance=step.tolerance;}
                }
            }
            return leaves;
        }
        public static IEnumerable<string> LeaveZones()=>Leaves().Select(l=>l.scene).Distinct().OrderBy(z=>z,System.StringComparer.Ordinal);
        /// <summary>A press made where a walk ended that has to set a flag (a beacon retuned, a cache opened, a pump run) or open a talk: a lone interact press outside any
        /// menu that waits on, or expects, a flag, or expects a conversation. Not the presses inside a conversation, which are made at no prompt.</summary>
        static List<Leave> Presses()
        {
            var presses=new List<Leave>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            foreach(string file in System.IO.Directory.GetFiles(root,"chapter-*.json").OrderBy(f=>f,System.StringComparer.Ordinal))
            {
                string name=System.IO.Path.GetFileNameWithoutExtension(file);var parts=name.Split('-');
                if(parts.Length<3||!int.TryParse(parts[1],out int number)||number<FirstRoute)continue;
                var route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));string scene=null,after=null;Vector3? at=null;float tolerance=0;
                foreach(var step in route.steps)
                {
                    string named=string.IsNullOrEmpty(step.expectedScene)?scene:step.expectedScene;
                    if(named!=scene){scene=named;at=null;}
                    if(step.navigate){if(step.approachTarget||step.approachPartner)at=null;else{at=step.point;after=step.name;tolerance=step.tolerance;}continue;}
                    // Anything said moves nobody, but a talk's own presses aren't made at a prompt.
                    // A press that opens a talk is made at a prompt too (a person, a thing to look at): the school raft's was pressed from a hair outside its reach.
                    bool waits=!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("flag:"),talk=!string.IsNullOrEmpty(step.until)&&step.until.StartsWith("ui:"),opens=string.IsNullOrEmpty(step.until)&&step.expectedUi=="dialogue";
                    if(at is Vector3 here&&scene!=null&&scene!="Title"&&!talk&&step.buttons!=null&&step.buttons.Length==1&&step.buttons[0]=="South"&&string.IsNullOrEmpty(step.pauseUi)&&string.IsNullOrEmpty(step.pickRow)
                        &&(waits||opens||!string.IsNullOrEmpty(step.expectedFlag)))
                        presses.Add(new Leave{route=name,step=step.name,scene=scene,to=waits?step.until.Substring(5):opens?"a talk":step.expectedFlag,after=after,at=here,tolerance=tolerance});
                }
            }
            return presses;
        }
        public static IEnumerable<string> PressZones()=>Presses().Select(l=>l.scene).Distinct().OrderBy(z=>z,System.StringComparer.Ordinal);
        /// <summary>How far inside a way's reach a route has to stand: a body stops a hair off its point, and a prompt at the very edge of its reach comes and goes.</summary>
        const float Inside=.15f;

        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}

        [Test] public void TheRoutesLockOntoFightsFromPlacesTheyWalkedTo()
        {
            var locks=Locks();
            Assert.Greater(locks.Count,20,"the routes of chapter 3 on lock onto almost nothing from a known place: the check below has nothing to judge");
        }
        [UnityTest] public IEnumerator WhereARouteLocksOntoAFightThatFightHasBegun([ValueSource(nameof(Zones))] string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return null;
            var volumes=Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var faults=new List<string>();int judged=0;
            foreach(var l in Locks().Where(l=>l.scene==zone))
            {
                var volume=volumes.FirstOrDefault(v=>v.encounterId==l.id);
                if(volume==null)
                {
                    // A boss whose fight's volume carries another name is judged by nobody here.
                    if(!BossController.Registered(l.id))faults.Add($"{l.route} '{l.step}': {zone} has no fight called {l.id}");
                    continue;
                }
                var box=volume.GetComponent<BoxCollider>();
                if(box==null){faults.Add($"{l.route} '{l.step}': {l.id} has no box to walk into");continue;}
                // In the box's own frame, level: the driver stops anywhere within its step's tolerance of the point, so the point has to be that far inside.
                var local=box.transform.InverseTransformPoint(new Vector3(l.at.x,box.transform.position.y,l.at.z))-box.center;var scale=box.transform.lossyScale;
                float outX=Mathf.Abs(local.x*scale.x)-box.size.x*Mathf.Abs(scale.x)/2,outZ=Mathf.Abs(local.z*scale.z)-box.size.z*Mathf.Abs(scale.z)/2,outside=Mathf.Max(outX,outZ)+l.tolerance+(l.boss?PartnerRoom:0);
                judged++;
                if(outside>0)faults.Add(l.boss?$"{l.route} '{l.step}': '{l.after}' ends within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), which is {outside:0.0} m short of {PartnerRoom:0} m inside {l.id}'s volume: the fight waits for the partner to be through its gate too, and may not begin":
                    $"{l.route} '{l.step}': after '{l.after}' the party stands within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), which is {outside:0.0} m short of being surely inside {l.id}'s volume: the fight may not have begun, and lock finds nothing");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
            Assert.Greater(judged+Locks().Count(l=>l.scene==zone&&BossController.Registered(l.id)),0,"fixture: nothing in "+zone+" was judged");
        }
        /// <summary>The Spore Veil's second cache was 7.2 m from where its route stopped, and reaches 6.5: the route had only ever opened it by pressing on the way past
        /// (November 13). Which prompt a press is for can't be read off a route, so this asks the least a press needs: where the walk before it ended, plus the walk's
        /// tolerance, some prompt of the zone is in reach on the plan with a little to spare.</summary>
        [UnityTest] public IEnumerator WhereARoutePressesForAFlagSomePromptIsInReach([ValueSource(nameof(PressZones))] string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return null;
            var prompts=Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var faults=new List<string>();
            foreach(var l in Presses().Where(l=>l.scene==zone))
            {
                float least=float.PositiveInfinity;InteractionPrompt nearest=null;
                foreach(var prompt in prompts)
                {
                    var gap=prompt.transform.position-l.at;gap.y=0;float over=gap.magnitude+l.tolerance+Inside-prompt.range;
                    if(over<least){least=over;nearest=prompt;}
                }
                if(nearest==null||least>0)faults.Add(nearest==null?$"{l.route} '{l.step}': {zone} has no prompt at all":
                    $"{l.route} '{l.step}' (for {l.to}): after '{l.after}' the party stands within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), and the nearest prompt to being in reach is '{nearest.name}' at ({nearest.transform.position.x:0.0}, {nearest.transform.position.z:0.0}), reaching {nearest.range:0.#} m: {least:0.00} m too far to be surely offered");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        [Test] public void TheRoutesLeaveZonesFromPlacesTheyWalkedTo()
        {
            Assert.Greater(Leaves().Count,10,"the routes of chapter 3 on leave almost no zone from a known place: the check below has nothing to judge");
        }
        /// <summary>Twice in one evening a route pressed at a way out from a tenth of a metre outside its reach, and stood there (Rootmarket's river gate, the root bridge back from
        /// Seedfall: November 13). A way's reach is on the plan (D272), so this is: where the route stopped walking, plus how far off that point the driver may stop, is inside the
        /// reach of a way to the zone the route goes to next.</summary>
        [UnityTest] public IEnumerator WhereARouteLeavesAZoneItStandsInReachOfTheWayOut([ValueSource(nameof(LeaveZones))] string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+30;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            yield return null;
            var prompts=Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var faults=new List<string>();
            foreach(var l in Leaves().Where(l=>l.scene==zone))
            {
                float least=float.PositiveInfinity;InteractionPrompt nearest=null;
                foreach(var prompt in prompts)
                {
                    string to=prompt is WarpBeacon warp?warp.scene:prompt is DockingPad pad?pad.scene:null;if(to!=l.to)continue;
                    var gap=prompt.transform.position-l.at;gap.y=0;float over=gap.magnitude+l.tolerance+Inside-prompt.range;
                    if(over<least){least=over;nearest=prompt;}
                }
                // Left some other way (a crossing, a lift, the zone's own end): nothing here to judge.
                if(nearest==null)continue;
                if(least>0)faults.Add($"{l.route} '{l.step}': after '{l.after}' the party stands within {l.tolerance:0.#} m of ({l.at.x:0.0}, {l.at.z:0.0}), and '{nearest.name}' at ({nearest.transform.position.x:0.0}, {nearest.transform.position.z:0.0}) reaches {nearest.range:0.#} m: {least:0.00} m too far to be surely offered");
            }
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
