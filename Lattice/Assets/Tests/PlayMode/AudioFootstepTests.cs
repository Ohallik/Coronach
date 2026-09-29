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
    // Physical floor/ramp fixtures; these do not establish subjective audition.
    public sealed class AudioFootstepTests
    {
        CombatActor actor;GameObject floor,wall;readonly List<string> cues=new();readonly List<float> gaps=new();readonly List<int> sides=new();
        Vector3 surfacePoint,surfaceNormal;
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");SceneFlow.Current.LoadZone("Arena_Ground");while(SceneFlow.Current.Loading)yield return null;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            PartyController.Current.enabled=false;
            foreach(var member in PartyController.Current.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;}
            floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Footstep physical surface";floor.transform.position=new Vector3(200,-.5f,200);floor.transform.localScale=new Vector3(50,1,50);
            surfacePoint=new Vector3(200,0,200);surfaceNormal=Vector3.up;Physics.SyncTransforms();AudioManager.CueStarted+=OnCue;
            yield return new WaitForSecondsRealtime(.7f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AudioManager.CueStarted-=OnCue;GameTime.Paused=false;Object.Destroy(floor);Object.Destroy(wall);
            SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        void OnCue(string key,Transform emitter)
        {
            if(actor==null||emitter!=actor.transform||!key.StartsWith("step_"))return;
            cues.Add(key);
            var driver=actor.GetComponentInChildren<GeneratedAnimator>();var rig=driver.GetComponentInChildren<Animator>();var profile=driver.strideProfile;
            var points=new[]{rig.GetBoneTransform(HumanBodyBones.LeftFoot).TransformPoint(profile.leftHeel),rig.GetBoneTransform(HumanBodyBones.LeftToes).TransformPoint(profile.leftToe),
                rig.GetBoneTransform(HumanBodyBones.RightFoot).TransformPoint(profile.rightHeel),rig.GetBoneTransform(HumanBodyBones.RightToes).TransformPoint(profile.rightToe)};
            gaps.Add(points.Min(p=>Mathf.Abs(Vector3.Dot(p-surfacePoint,surfaceNormal))));
            var sound=AudioManager.Current.GetComponentsInChildren<AudioSource>().Single(s=>s.isPlaying&&s.clip!=null&&s.clip.name==key);
            int nearest=Enumerable.Range(0,4).OrderBy(i=>(points[i]-sound.transform.position).sqrMagnitude).First();
            sides.Add(nearest/2);
        }
        IEnumerator Place(string hero,BodyForm form)
        {
            actor=PartyController.Current.members.Single(a=>a.character==hero);var cc=actor.GetComponent<CharacterController>();
            float surfaceY=surfacePoint.y-Vector3.Dot(new Vector3(0,0,188-surfacePoint.z),surfaceNormal)/surfaceNormal.y;
            cc.enabled=false;actor.transform.SetPositionAndRotation(new Vector3(200,surfaceY+1,188),Quaternion.identity);cc.enabled=true;
            actor.GetComponent<FormController>().Set(form);yield return new WaitForSecondsRealtime(.7f);
            float settle=Time.unscaledTime+.6f;while(Time.unscaledTime<settle){actor.motor.Move(Vector2.zero,false,false);yield return null;}
            cues.Clear();gaps.Clear();sides.Clear();
        }
        IEnumerator Drive(float seconds,Vector2 direction,bool sprint=false)
        {float until=Time.unscaledTime+seconds;while(Time.unscaledTime<until){actor.motor.Move(direction,sprint,false);yield return null;}}
        [UnityTest] public IEnumerator BothBodiesEmitPhysicalFootfallsWithoutHeelToeDoubleHits()
        {
            foreach(string hero in new[]{"Taren","Sela"})foreach(BodyForm form in new[]{BodyForm.Natural,BodyForm.Shaped})
            {
                yield return Place(hero,form);yield return Drive(4,Vector2.up*.36f);
                Assert.GreaterOrEqual(cues.Count,4,hero+" "+form+" has no repeated planted-foot sound");
                for(int i=1;i<sides.Count;i++)Assert.AreNotEqual(sides[i-1],sides[i],hero+" "+form+" same physical foot emitted again before the opposite foot");
                Assert.IsTrue(cues.All(k=>k.StartsWith("step_rock_")));Assert.Less(gaps.Max(),.065f,"foot sound occurs above the physical floor");
                TestContext.WriteLine($"FOOTSTEP_WALK {hero} {form} count={cues.Count} gap={gaps.Max():F6} alternating={string.Join(",",sides)}");
            }
        }
        [UnityTest] public IEnumerator SprintFootfallsFollowThePhysicalRamp()
        {
            floor.transform.rotation=Quaternion.Euler(-8,0,0);surfaceNormal=floor.transform.up;surfacePoint=floor.transform.TransformPoint(Vector3.up*.5f);Physics.SyncTransforms();
            yield return Place("Taren",BodyForm.Shaped);yield return Drive(2,Vector2.up,true);
            Assert.GreaterOrEqual(cues.Count,4);Assert.Less(gaps.Max(),.065f,"ramp foot sound is not a planted sole");
        }
        [UnityTest] public IEnumerator StationaryBlockedPausedChangingAndDownBodiesDoNotInventFootfalls()
        {
            yield return Place("Taren",BodyForm.Shaped);yield return Drive(1.3f,Vector2.up);Assert.IsNotEmpty(cues,"no positive moving control");
            yield return Drive(.4f,Vector2.zero);cues.Clear();yield return Drive(.7f,Vector2.zero);Assert.IsEmpty(cues,"stationary sound");
            wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Footstep blocked movement control";
            wall.transform.position=actor.transform.position+Vector3.forward*1.2f+Vector3.up;wall.transform.localScale=new Vector3(8,4,.5f);Physics.SyncTransforms();
            yield return Drive(.6f,Vector2.up);cues.Clear();Vector3 blocked=actor.transform.position;
            yield return Drive(.8f,Vector2.up);Assert.Less(Vector3.Distance(blocked,actor.transform.position),.015f,"wall control did not block motion");Assert.IsEmpty(cues,"blocked movement sound");
            Object.Destroy(wall);yield return Drive(.3f,Vector2.zero);cues.Clear();
            GameTime.Paused=true;yield return Drive(.4f,Vector2.up);Assert.IsEmpty(cues,"paused sound");GameTime.Paused=false;
            actor.GetComponent<FormController>().Set(BodyForm.Natural);yield return Drive(.5f,Vector2.up);Assert.IsEmpty(cues,"folding body sounded a step");
            yield return Drive(.3f,Vector2.zero);cues.Clear();actor.Health.Receive(new DamagePacket{amount=100000,type=DamageType.Kinetic});yield return Drive(.8f,Vector2.up);Assert.IsEmpty(cues,"downed sound");
        }
        [UnityTest] public IEnumerator SelaMovingFireRetainsIndependentFootfalls()
        {
            yield return Place("Sela",BodyForm.Shaped);float until=Time.unscaledTime+4.2f,next=0,previous=-1,maximumJump=0;int shots=0;
            while(Time.unscaledTime<until)
            {
                actor.motor.Move(Vector2.up,false,false);if(Time.unscaledTime<until-1.2f&&actor.Attack())shots++;
                var driver=actor.GetComponentInChildren<GeneratedAnimator>();var rig=driver.GetComponentInChildren<Animator>();
                var take=rig.IsInTransition(0)?rig.GetNextAnimatorStateInfo(0):rig.GetCurrentAnimatorStateInfo(0);
                // The driver can request Walk before Unity evaluates the initial
                // Idle -> Walk transition. Idle's unrelated cycle is not a gait.
                if(take.shortNameHash==Animator.StringToHash("Walk")||take.shortNameHash==Animator.StringToHash("Run"))
                {
                    float phase=Mathf.Repeat(take.normalizedTime,1);
                    if(previous>=0)maximumJump=Mathf.Max(maximumJump,Mathf.Abs(Mathf.DeltaAngle(previous*360,phase*360))/360);
                    previous=phase;
                }
                else previous=-1;
                if(Time.unscaledTime>=next)
                {
                    next=Time.unscaledTime+.1f;
                    Debug.Log($"FOOTSTEP_FIRE t={Time.unscaledTime:F3} v={actor.motor.Velocity.magnitude:F3} state={actor.State} clip={driver.LocomotionAnimation} phase={rig.GetCurrentAnimatorStateInfo(0).normalizedTime:F3} transition={rig.IsInTransition(0)} grounded={actor.GetComponent<CharacterController>().isGrounded} cues={cues.Count}");
                }
                yield return null;
            }
            Assert.GreaterOrEqual(cues.Count,4,"upper-body fire erased moving foot contact cues");Assert.Less(gaps.Max(),.065f);
            Assert.GreaterOrEqual(shots,8,"repeated-fire control did not fire enough actual shots");
            TestContext.WriteLine($"FOOTSTEP_FIRE_PHASE maxFrameJump={maximumJump:F6} cues={cues.Count}");
            Assert.Less(maximumJump,.15f,"pace changes jump the leg cycle during repeated ordinary attacks");
        }
        [UnityTest] public IEnumerator AirborneTravelDoesNotInventGroundContacts()
        {
            yield return Place("Sela",BodyForm.Natural);yield return Drive(1.5f,Vector2.up);Assert.IsNotEmpty(cues,"no grounded positive control");
            var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position+=Vector3.up*20;cc.enabled=true;
            cues.Clear();yield return Drive(.7f,Vector2.up);Assert.IsFalse(cc.isGrounded,"airborne control landed too soon");Assert.IsEmpty(cues,"airborne travel emitted footfalls");
        }
        [UnityTest] public IEnumerator PhysicalSurfaceOverridesSelectMetalAndSoilFamilies()
        {
            var surface=floor.AddComponent<AudioSurface>();
            foreach(var kind in new[]{FootstepSurface.Metal,FootstepSurface.Soil})
            {
                surface.material=kind;yield return Place("Taren",BodyForm.Natural);yield return Drive(3,Vector2.up*.36f);
                Assert.GreaterOrEqual(cues.Count,3,"surface has no planted-foot cues");
                string family=kind==FootstepSurface.Metal?"step_metal_":"step_soil_";
                Assert.IsTrue(cues.All(key=>key.StartsWith(family)),"physical material was ignored: "+kind);
                Assert.Less(gaps.Max(),.065f);
            }
        }
    }
}
