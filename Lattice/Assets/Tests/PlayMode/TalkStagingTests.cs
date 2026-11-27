using System.Collections;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Round eight: who can be seen while people talk on foot (ConversationStage, PartnerBrain.TalkEnded). Each case plays the place as the capture had it, first as it was
    /// (the class's switch, set by reflection and put back) and then as it is, and measures what the still showed.</summary>
    public sealed class TalkStagingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            Switch(typeof(PartnerBrain),"talkEndsAsBefore",false);Switch(typeof(ConversationStage),"stagedAsBefore",false);Switch(typeof(ConversationStage),"asRoundEight",false);
            Time.captureDeltaTime=0;var dialogue=DialogueSystem.Current;if(dialogue!=null&&dialogue.Running){dialogue.Cancel();yield return null;yield return null;}
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static void Switch(System.Type type,string field,bool on)
        {
            var f=type.GetField(field,BindingFlags.NonPublic|BindingFlags.Static);Assert.IsNotNull(f,$"fixture: {type.Name}.{field} is the switch for the state as it was");f.SetValue(null,on);
        }
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Camera.main.aspect=16f/9;
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=actor!=Lead;}
            deadline=Time.unscaledTime+10;while(!DialogueSystem.Current.Prepared&&Time.unscaledTime<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }
        static void ClearFights(){foreach(var fight in All<EncounterVolume>())Flag("clear."+fight.encounterId);}
        static void Put(CombatActor hero,Vector3 at)
        {var cc=hero.GetComponent<CharacterController>();cc.enabled=false;hero.transform.position=at;cc.enabled=true;Physics.SyncTransforms();}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static Npc Person(string speaker)=>All<Npc>().Single(n=>n.speaker==speaker&&n.gameObject.activeInHierarchy);
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        static Vector3 Right=>Flat(Camera.main.transform.right).normalized;
        static float Across(Vector3 a,Vector3 b)=>Mathf.Abs(Vector3.Dot(a-b,Right));
        static IEnumerator Talk(Npc person)
        {
            DialogueSystem.Current.AutoAdvance=false;person.Interact();
            float deadline=Time.unscaledTime+3;while(!DialogueSystem.Current.Running&&Time.unscaledTime<deadline)yield return null;
            Assert.IsTrue(DialogueSystem.Current.Running,$"fixture: the talk with {person.speaker} starts");
        }
        static IEnumerator EndTalk()
        {
            DialogueSystem.Current.Cancel();
            float deadline=Time.unscaledTime+5;while((DialogueSystem.Current.Running||GameInput.Current.Blocked)&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(GameInput.Current.Blocked,"fixture: the talk lets go of the controls");
        }
        static readonly HumanBodyBones[] Outline={HumanBodyBones.Head,HumanBodyBones.Neck,HumanBodyBones.Chest,HumanBodyBones.Hips,HumanBodyBones.LeftShoulder,HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot};
        /// <summary>A body's box on the screen as it stands posed: its bones (head, shoulders, hands, hips, feet) and a hand's breadth round them, the head's crown over its bone.
        /// Not its meshes' bounds: a skinned mesh's bounds are its bind pose's, arms spread, and Taren's still covered Teo's with the two 1.15 m apart.</summary>
        static Rect Box(Transform body)
        {
            var camera=Camera.main;var rig=body.GetComponentInChildren<Animator>();var points=new System.Collections.Generic.List<Vector3>();
            if(rig!=null&&rig.isHuman)foreach(var bone in Outline){var t=rig.GetBoneTransform(bone);if(t!=null)points.Add(t.position);}
            if(points.Count<4){points.Clear();points.Add(body.position);points.Add(body.position+Vector3.up*1.7f);}
            float x0=float.MaxValue,y0=float.MaxValue,x1=float.MinValue,y1=float.MinValue;
            foreach(var at in points)
                foreach(var off in new[]{Right*.15f,-Right*.15f,Vector3.up*.2f,Vector3.down*.1f})
                {
                    var p=camera.WorldToScreenPoint(at+off);
                    x0=Mathf.Min(x0,p.x);x1=Mathf.Max(x1,p.x);y0=Mathf.Min(y0,p.y);y1=Mathf.Max(y1,p.y);
                }
            return Rect.MinMaxRect(x0,y0,x1,y1);
        }
        static Vector3 Floor(float x,float z,float y)
        {Assert.IsTrue(NavMesh.SamplePosition(new Vector3(x,y,z),out var hit,1.5f,NavMesh.AllAreas),$"fixture: no floor at ({x}, {z})");return hit.position;}
        static readonly FieldInfo StageMark=typeof(ConversationStage).GetField("mark",BindingFlags.NonPublic|BindingFlags.Instance);
        static Vector3 MarkSet=>(Vector3)StageMark.GetValue(Object.FindFirstObjectByType<ConversationStage>());
        /// <summary>The share of the farther body's box on the screen that the nearer one's box covers.</summary>
        static float Under(Transform front,Transform behind)
        {
            var a=Box(front);var b=Box(behind);
            float w=Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)),h=Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
            return w*h/Mathf.Max(1,b.width*b.height);
        }

        /// <summary>W7-lanternwick-01 (nov25-whole s13/142): in the Archive, as Ishen's talk ended, Sela walked from where the stage had set her (beside Taren across the screen) to
        /// her place behind his nose, which with him facing Ishen up the screen lay straight down the screen from him: she crossed his legs and the two read as one shape. The replay's
        /// own place. As it was she leaves her place and is in line with him on the screen (under .6 m across, inside 2.6 m) for some frames; now she keeps it while he stands.</summary>
        [UnityTest] public IEnumerator InTheArchiveAsIshensTalkEndsSelaKeepsHerPlaceBesideTaren()
        {
            yield return Zone("Lanternwick");
            var ishen=Person("Ishen");var stand=new Vector3(2.94f,.19f,89.29f);
            foreach(bool before in new[]{true,false})
            {
                Switch(typeof(PartnerBrain),"talkEndsAsBefore",before);
                Put(Lead,stand);Put(Partner,stand+new Vector3(0,0,-4));yield return new WaitForSecondsRealtime(.4f);
                yield return Talk(ishen);
                // She walks to her mark for up to four seconds.
                yield return new WaitForSecondsRealtime(4.6f);
                var left=Partner.transform.position;var him=Lead.transform.position;
                Assert.Greater(Across(left,him),1.05f,"fixture: the stage set her beside him across the screen");
                yield return EndTalk();
                int inLine=0,frames=0;float moved=0,end=Time.unscaledTime+2.5f;
                while(Time.unscaledTime<end)
                {
                    var her=Partner.transform.position;var off=Flat(her-Lead.transform.position);
                    if(Across(her,Lead.transform.position)<.6f&&off.magnitude<2.6f)inLine++;
                    moved=Mathf.Max(moved,Flat(her-left).magnitude);frames++;
                    yield return null;
                }
                Assert.Less(Flat(Lead.transform.position-him).magnitude,.05f,"fixture: the lead stands where the talk left him");
                string seen=$"{(before?"as it was":"now")}: in line with him on the screen {inLine} of {frames} frames, {moved:0.00} m off where the talk left her";
                Debug.Log("TALK_STAGING archive "+seen);
                if(before){Assert.Greater(moved,1f,"fixture: as it was she walked off her place, "+seen);Assert.Greater(inLine,0,"fixture: as it was she crossed in front of his legs (s13/142), "+seen);}
                else{Assert.AreEqual(0,inLine,"as Ishen's talk ends Sela walks across Taren's legs, the two one shape (s13/142): "+seen);Assert.Less(moved,.3f,"she leaves the place beside him the talk gave her: "+seen);}
            }
        }

        /// <summary>W8-braided-river-01 (nov25-whole s20/401): at Marrow-3 Teo Marrow, a head shorter than Taren, stood 1.1 m up the screen from where the replay stopped him and .6 m
        /// across, and spoke from behind him. As it was Taren covers more than half Teo's height; now he steps a pace across the screen and turns back to Teo, and Teo's box on the
        /// screen is no longer under his.</summary>
        [UnityTest] public IEnumerator AtMarrowThreeTarenStepsAsideAndTeoIsSeen()
        {
            Flag("bossdown.Seedthane");Flag("xylos.seedthaneGone");
            yield return Zone("BraidedRiver");ClearFights();
            var teo=Person("Teo");var stand=new Vector3(355.17f,.68f,99.74f);
            Assert.Less(Flat(teo.transform.position-new Vector3(355,0,101)).magnitude,.5f,"fixture: Teo stands at his place by the carrier");
            float under=0;
            foreach(bool before in new[]{true,false})
            {
                Switch(typeof(ConversationStage),"stagedAsBefore",before);
                Put(Lead,stand);Lead.transform.rotation=Quaternion.LookRotation(Flat(teo.transform.position-stand));Put(Partner,stand+Right*2-Vector3.forward);
                yield return new WaitForSecondsRealtime(.4f);
                yield return Talk(teo);
                yield return new WaitForSecondsRealtime(2.5f);
                float covered=ConversationStage.Covered(Camera.main,Lead.transform,teo.transform,out float across),moved=Flat(Lead.transform.position-stand).magnitude,now=Under(Lead.transform,teo.transform);
                float turned=Vector3.Angle(Flat(Lead.transform.forward),Flat(teo.transform.position-Lead.transform.position));
                string seen=$"{(before?"as it was":"now")}: Taren covers {covered:0.00} of Teo's height, {now:0.00} of his box, {Mathf.Abs(across):0.00} m across; Taren moved {moved:0.00} m, {turned:0} degrees off Teo";
                Debug.Log("TALK_STAGING teo "+seen);
                if(before)
                {
                    Assert.Greater(covered,.5f,"fixture: as it was Teo is all but hidden behind Taren (s20/401), "+seen);Assert.Greater(now,.1f,"fixture: as it was Taren's posed body is over Teo's, "+seen);
                    Assert.Less(moved,.05f,"fixture: as it was the lead stands, "+seen);under=now;
                }
                else
                {
                    Assert.That(moved,Is.InRange(.5f,1.25f),"Taren steps a pace aside so that Teo is seen: "+seen);
                    Assert.GreaterOrEqual(Mathf.Abs(across),.95f,"Teo stands clear of him across the screen: "+seen);
                    Assert.Less(now,Mathf.Min(.05f,under*.25f),$"Teo's posed body on the screen is still under Taren's (it was {under:0.00}): "+seen);
                    Assert.Less(turned,25,"Taren turns back to Teo after his step: "+seen);
                    Assert.IsTrue(NavMesh.SamplePosition(Lead.transform.position,out var floor,.3f,NavMesh.AllAreas)&&Mathf.Abs(floor.position.y-stand.y)<.35f,"he stepped off the floor he stood on: "+seen);
                }
                yield return EndTalk();yield return new WaitForSecondsRealtime(.8f);
            }
        }

        /// <summary>W2-senn-01 (nov22-whole s19/156): on his island Old Senn stands down the screen from where the replay stopped Taren and 1.7 m across; turned to Taren he showed the
        /// camera his back. Nobody covers anybody there, so the lead mustn't move; Senn turns to Taren's side of the screen and no further from the lens than three-quarter.</summary>
        [UnityTest] public IEnumerator OnHisIslandOldSennTurnsHisFaceToTheCamera()
        {
            yield return Zone("Rootmarket");ClearFights();
            var senn=Person("Senn");var stand=new Vector3(-44.49f,.09f,13.81f);
            // As before round eight (his back to the lens), as round eight left him (20 degrees past profile: side-on, nov26-r8-chain s19/156), and now (three-quarter, 45).
            foreach(int state in new[]{0,1,2})
            {
                bool before=state==0;
                Switch(typeof(ConversationStage),"stagedAsBefore",before);Switch(typeof(ConversationStage),"asRoundEight",state==1);
                Put(Lead,stand);Put(Partner,stand+Right*2);yield return new WaitForSecondsRealtime(.4f);
                yield return Talk(senn);
                yield return new WaitForSecondsRealtime(2.5f);
                var facing=Flat(senn.transform.forward).normalized;var lens=Flat(Camera.main.transform.forward).normalized;
                float away=Vector3.Dot(facing,lens),side=Vector3.Dot(facing,Right)*Mathf.Sign(Vector3.Dot(Lead.transform.position-senn.transform.position,Right));
                float moved=Flat(Lead.transform.position-stand).magnitude;
                string seen=$"{(before?"as it was":state==1?"as round eight left it":"now")}: Senn faces {away:0.00} along the lens's way (1 is his back to it), {side:0.00} toward Taren's side; Taren moved {moved:0.00} m";
                Debug.Log("TALK_STAGING senn "+seen);
                Assert.Less(moved,.05f,"nobody covers anybody at Senn's: the lead must not move, "+seen);
                if(before)Assert.Greater(away,0,"fixture: as it was Old Senn shows the camera his back (s19/156), "+seen);
                else if(state==1)Assert.Greater(away,-.45f,"fixture: as round eight left it Old Senn stands side-on to the lens (nov26-r8-chain s19/156), "+seen);
                // Round nine: three-quarter to the lens (sin 45 degrees).
                else{Assert.LessOrEqual(away,-.6f,"Old Senn speaks side-on or with his back to the camera: "+seen);Assert.Greater(side,.5f,"Old Senn doesn't face Taren's side of the screen: "+seen);}
                yield return EndTalk();yield return new WaitForSecondsRealtime(1.5f);
            }
        }

        /// <summary>Talks where nobody was hidden: the lead doesn't move at all, and the person he talks to still turns to him as before (up the screen they face the lens already).
        /// Round nine: and where the lens sees her where she would stand, the partner's mark is the one round eight gave her (each talk had as it was, then as it is).</summary>
        [UnityTest] public IEnumerator AtOrdinaryTalksTheLeadDoesNotMove()
        {
            yield return Zone("Lanternwick");
            foreach(var (speaker,stand) in new[]{("Ishen",new Vector3(2.94f,.19f,89.29f)),("Pell",new Vector3(7.5f,.3f,36)),("Corra",new Vector3(-4,.3f,17)),("Aumi",new Vector3(19.7f,3.2f,52.5f))})
            {
                var person=Person(speaker);var marks=new Vector3[2];
                for(int pass=0;pass<2;pass++)
                {
                    Switch(typeof(ConversationStage),"asRoundEight",pass==0);
                    Put(Lead,stand);Put(Partner,stand+new Vector3(0,0,-4));yield return new WaitForSecondsRealtime(.4f);
                    var at=Lead.transform.position;
                    yield return Talk(person);
                    marks[pass]=MarkSet;
                    yield return new WaitForSecondsRealtime(2.5f);
                    float covered=ConversationStage.Covered(Camera.main,Lead.transform,person.transform,out float across),moved=Flat(Lead.transform.position-at).magnitude;
                    bool seenThere=ConversationStage.Seen(Camera.main.transform.position,marks[pass]);
                    string seen=$"with {speaker} {(pass==0?"as round eight left it":"now")}: covered {covered:0.00}, {Mathf.Abs(across):0.00} m across, the lead moved {moved:0.00} m; her mark ({marks[pass].x:0.00}, {marks[pass].z:0.00}), the lens sees her feet and knees there: {seenThere}";
                    Debug.Log("TALK_STAGING ordinary "+seen);
                    Assert.Less(moved,.05f,"a talk where nobody was hidden moves the lead: "+seen);
                    if(pass==1)Assert.Less(Flat(marks[1]-marks[0]).magnitude,.05f,$"at an ordinary talk her mark moved {Flat(marks[1]-marks[0]).magnitude:0.00} m from round eight's: "+seen);
                    yield return EndTalk();yield return new WaitForSecondsRealtime(.6f);
                }
            }
        }

        /// <summary>W12-harrowgate-01 (nov26-r8-chain s50/063): Pip's first word is a crossing on Harrowgate's gate landing, and as it opened Idra, coming up the ramp behind Taren,
        /// took the first place beside him, on his right, where the gatehouse roof's near edge cut her at the waist. Walked up the route's line as the replay walks it, she coming up
        /// behind him from where the capture had her. As it was the lens doesn't see her feet and knees where she comes to rest; now it does, and she stands a body's width beside him,
        /// in the 16:9 frame and above the dialogue box. She walks there.</summary>
        [UnityTest] public IEnumerator AtPipsFirstWordIdraComesToRestWhereTheLensSeesHerFeet()
        {
            DevLoadout.Apply("chapter8");
            foreach(bool before in new[]{true,false})
            {
                foreach(string f in new[]{"vask.lockAdmitted","vask.picketCrossed","vask.lockRemoved","vask.unswornMet","vask.vessaBeaten","bossdown.SlagColossus","vask.juvenileFreed","vask.roadSaid","vask.sistersHeard","vask.parley","met.Hessa"})Flag(f);
                GameServices.Current.Flags.SetBool("vask.pipMet",false);GameServices.Current.Flags.SetBool("met.Pip",false);
                yield return Zone("Harrowgate");ClearFights();
                Switch(typeof(ConversationStage),"asRoundEight",before);
                Assert.AreEqual("Idra",Partner.character,"fixture: Idra is out beside him, as in route 50");
                var from=Partner.transform.position;
                Put(Lead,Floor(8,27,3));Put(Partner,Floor(9.99f,25.95f,3));yield return new WaitForSecondsRealtime(.3f);from=Partner.transform.position;
                DialogueSystem.Current.AutoAdvance=false;Time.captureDeltaTime=1/60f;
                try{for(int frame=0;frame<360&&!DialogueSystem.Current.Running;frame++){Lead.motor.Move(Vector2.up,false,false);yield return null;}}
                finally{Time.captureDeltaTime=0;Lead.motor.Move(Vector2.zero,false,false);}
                Assert.IsTrue(DialogueSystem.Current.Running,"fixture: Pip's first word opens as the lead walks up the landing");
                var mark=MarkSet;
                yield return new WaitForSecondsRealtime(4.6f);
                var camera=Camera.main;var eye=camera.transform.position;var her=Partner.transform.position;var him=Lead.transform.position;
                bool feet=!SceneryOcclusion.Hides(eye,her+Vector3.up*.15f),knees=!SceneryOcclusion.Hides(eye,her+Vector3.up*.5f),waist=!SceneryOcclusion.Hides(eye,her+Vector3.up*1);
                bool Ray(float h){var p=her+Vector3.up*h;return !Physics.Linecast(eye,p+(eye-p).normalized*.45f,~0,QueryTriggerInteraction.Ignore);}
                var view=camera.WorldToViewportPoint(her);float across=Across(her,him),walked=Flat(her-from).magnitude;
                string seen=$"{(before?"as it was":"now")}: Taren at ({him.x:0.00}, {him.z:0.00}), Idra at ({her.x:0.00}, {her.y:0.00}, {her.z:0.00}) (mark ({mark.x:0.00}, {mark.z:0.00})), {across:0.00} m across from him, walked {walked:0.00} m; "
                    +$"the lens sees her feet {feet}, knees {knees}, waist {waist} (a ray to the colliders: feet {Ray(.15f)}, knees {Ray(.5f)}); her feet at ({view.x:0.00}, {view.y:0.00}) of the 16:9 frame";
                Debug.Log("TALK_STAGING pip-landing "+seen);
                if(before)Assert.IsFalse(feet&&knees,"fixture: as it was the gatehouse roof hides her feet and knees where she comes to rest (s50/063), "+seen);
                else
                {
                    Assert.IsTrue(feet&&knees,"at Pip's first word the partner comes to rest where scenery hides her legs: "+seen);
                    Assert.GreaterOrEqual(across,1.05f,"she no longer stands a body's width beside him across the screen: "+seen);
                    Assert.IsTrue(view.x>.05f&&view.x<.95f&&view.y>.34f&&view.y<.95f,"she stands out of the frame or under the dialogue box: "+seen);
                    Assert.Greater(walked,.5f,"fixture: she walks to her place: "+seen);
                }
                yield return EndTalk();
            }
        }
    }
}
