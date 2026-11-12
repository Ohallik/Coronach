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
    /// <summary>The partner as the November 10 pass found her open in the QA log: shots that left along her heading with the boss behind her
    /// (E4-gullet-05, E4-hushwell-02), bolts at things nobody could see (E3-lanternwick-01), and a body parked under the HUD's plates
    /// (E3-lanternwick-01, E3-tallow-04, E4-gullet-02, E4-title-decks-03). Each check below failed on the November 7 tree.</summary>
    public sealed class QaNov10PartnerTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            HudKeepOut.Clear();
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }

        // ------------------------------------------------------------------ fixtures
        static CombatActor Other(CombatActor hero)=>PartyController.Current.members.First(m=>m!=hero);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();yield return new WaitForSecondsRealtime(.7f);
        }
        static CombatActor Lead(string hero)
        {
            var party=PartyController.Current;if(party.Active.character!=hero)Assert.IsTrue(party.Swap(),"fixture: the swap");
            Quiet();return party.Active;
        }
        /// <summary>Stands a hero facing +z.</summary>
        static void Place(CombatActor actor,Vector3 position)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(Vector3.forward);}
            else if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(Vector3.forward);}
            actor.transform.SetPositionAndRotation(position,Quaternion.identity);body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        /// <summary>Open floor far from the ground arena's walls.</summary>
        static void Floor()
        {var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Partner fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();}
        /// <summary>A body that never dies or breaks, and never acts.</summary>
        static EnemyBrain Dummy(string id,Vector3 at)
        {
            var enemy=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);enemy.Passive=true;
            var body=enemy.Health;body.maximum=body.integrity=1000000;body.breakThreshold=1e9f;return enemy;
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        /// <summary>Party shots that came into the air since the last look, by where they were and where they were heading. Pooled shots come back as new ones.</summary>
        sealed class ShotWatch
        {
            readonly HashSet<Projectile> known=new(Projectile.Live);
            public readonly List<(Vector3 at,Vector3 heading)> Fired=new();
            public void Look()
            {
                foreach(var shot in Projectile.Live)
                    if(known.Add(shot)&&shot.Threatens(false)){var heading=shot.Velocity;heading.y=0;Fired.Add((shot.transform.position,heading));}
                known.IntersectWith(Projectile.Live);
            }
        }
        static float OffTarget((Vector3 at,Vector3 heading) shot,Vector3 target){var to=target-shot.at;to.y=0;return Vector3.Angle(shot.heading,to);}
        /// <summary>The HUD is taken out of the fixture so the test's plate is the only one, and is put back every frame: the partner reads last frame's.</summary>
        static void QuietHud(){foreach(var hud in Object.FindObjectsByType<GameHud>(FindObjectsSortMode.None))hud.enabled=false;}
        static Rect PlateOver(Camera camera,Vector3 world,float width,float height)
        {
            var at=camera.WorldToScreenPoint(world);float scale=camera.pixelHeight/1080f;
            return new Rect(at.x-width*scale*.5f,at.y-height*scale*.5f,width*scale,height*scale);
        }
        static bool Covered(CombatActor actor,Camera camera)
        {
            var at=actor.transform.position;
            if(actor.flight)
            {
                var middle=camera.WorldToScreenPoint(at);float hull=Mathf.Abs(camera.WorldToScreenPoint(at+camera.transform.right*HeroCollision.HullRadius(actor.character)).x-middle.x);
                return HudKeepOut.Covers(middle,hull);
            }
            return HudKeepOut.Covers(camera.WorldToScreenPoint(at))||HudKeepOut.Covers(camera.WorldToScreenPoint(at+Vector3.up*.9f))||HudKeepOut.Covers(camera.WorldToScreenPoint(at+Vector3.up*1.7f));
        }

        // ------------------------------------------------------------------ where her shots go
        /// <summary>E4-gullet-05, E4-hushwell-02. A boss marking just north of her sends her out of it southward, straight away from a target 10 m north that
        /// stays on the screen. On November 7 she fired the whole way out: a shot leaves along the heading unless the target is inside the aim's cone,
        /// so her bolts went south, off the screen, with the target behind her. Now a gun fires only once it's pointed, and once the marking has gone
        /// she turns and fires at the target.</summary>
        [UnityTest] public IEnumerator TheCompanionsShotsLeaveTowardHerTarget([Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Floor();float y=flight?1:0;
            var lead=Lead("Taren");var sela=Other(lead);Place(lead,new Vector3(200,y,0));Place(sela,new Vector3(200,y,-3));
            var body=Dummy(flight?"ChoristerDart":"Ridgehound",new Vector3(200,y,7));
            var owner=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>("Burrower"),new Vector3(280,0,60));owner.Passive=true;
            yield return Frames(3);lead.target=body.Health;sela.charge=0;
            var mark=Telegraph.Circle(new Vector3(200,y,-1.5f),4,2.5f,Telegraph.Threat);mark.owner=owner.GetComponent<BossController>();
            var watch=new ShotWatch();sela.GetComponent<PartnerBrain>().enabled=true;
            for(float end=Time.unscaledTime+6;Time.unscaledTime<end;){sela.charge=0;lead.target=body.Health;yield return null;watch.Look();}
            sela.GetComponent<PartnerBrain>().enabled=false;
            Assert.Greater(watch.Fired.Count,0,"fixture: once the marking has gone she fires at the target");
            float worst=watch.Fired.Max(s=>OffTarget(s,body.transform.position));
            Assert.Less(worst,flight?30:40,$"a shot of hers left {worst:0} degrees off her target: run out of a marking with the trigger held, her bolts flew along her heading, away from what she was fighting ({watch.Fired.Count} shots)");
        }
        /// <summary>E3-lanternwick-01: her bolts flew in at things nobody could see. The fixture's camera looks straight down on the party, close enough that a
        /// target 9 m north of her is off its top edge while she is in the middle of it: on November 7 she fired at it. Then the camera draws back to show
        /// it, and she fires, so it was the screen holding her.</summary>
        [UnityTest] public IEnumerator TheCompanionHoldsFireAtWhatTheScreenDoesNotShow()
        {
            yield return Zone("Arena_Ground");Floor();
            var lead=Lead("Taren");var sela=Other(lead);Place(lead,new Vector3(200,0,0));Place(sela,new Vector3(200,0,-3));
            var body=Dummy("Ridgehound",new Vector3(200,0,6));
            var old=Camera.main;old.tag="Untagged";
            var eye=new GameObject("Partner fixture camera",typeof(Camera)){tag="MainCamera"}.GetComponent<Camera>();
            eye.transform.SetPositionAndRotation(new Vector3(200,12,-3),Quaternion.Euler(90,0,0));eye.fieldOfView=30;
            yield return Frames(3);Assert.AreEqual(eye,Camera.main,"fixture: the test's camera is the one the partner reads");
            var view=eye.WorldToViewportPoint(body.transform.position+Vector3.up*.9f);Assert.Greater(view.y,1,"fixture: the target is off the top of the screen");
            lead.target=body.Health;sela.charge=0;
            var watch=new ShotWatch();sela.GetComponent<PartnerBrain>().enabled=true;
            for(float end=Time.unscaledTime+3;Time.unscaledTime<end;){sela.charge=0;lead.target=body.Health;yield return null;watch.Look();}
            Assert.AreEqual(0,watch.Fired.Count,$"she fired {watch.Fired.Count} shots at a target the screen didn't show");
            eye.transform.position=new Vector3(200,22,1.5f);eye.fieldOfView=60;
            Assert.Less(eye.WorldToViewportPoint(body.transform.position+Vector3.up*.9f).y,.95f,"fixture: the target now shows");
            for(float end=Time.unscaledTime+3;watch.Fired.Count==0&&Time.unscaledTime<end;){sela.charge=0;lead.target=body.Health;yield return null;watch.Look();}
            sela.GetComponent<PartnerBrain>().enabled=false;
            Assert.Greater(watch.Fired.Count,0,"with the target on the screen she fires: it was the screen holding her");
        }

        // ------------------------------------------------------------------ where she stands on the screen
        /// <summary>E3-lanternwick-01, E3-tallow-04, E4-gullet-02, E4-title-decks-03. A plate stands over the place she follows the lead to (3 m behind him,
        /// 2 m to his right). On November 7 she walked or flew under it and stayed there: her place came from the lead's heading alone. Now she stands at
        /// the nearest place that clears it, close by, and stays clear rather than hopping in and out.</summary>
        [UnityTest] public IEnumerator TheCompanionStandsClearOfTheHudsPlates([Values(false,true)] bool flight)
        {
            yield return Zone(flight?"Arena_Flight":"Arena_Ground");if(!flight)Floor();float y=flight?1:0;
            var lead=Lead("Taren");var sela=Other(lead);Place(lead,new Vector3(200,y,0));Place(sela,new Vector3(203,y,-5));
            QuietHud();yield return Frames(3);
            var spot=new Vector3(202,y,-3);var camera=Camera.main;Assert.IsNotNull(camera,"fixture: a camera");
            void Plate(){HudKeepOut.Begin();HudKeepOut.Add(PlateOver(camera,spot+Vector3.up*(flight?0:.9f),flight?280:260,flight?280:300));}
            Plate();sela.GetComponent<PartnerBrain>().enabled=true;
            int frames=0,under=0;
            for(float began=Time.unscaledTime;Time.unscaledTime-began<4;)
            {
                yield return null;Plate();
                if(Time.unscaledTime-began<3)continue;
                frames++;if(Covered(sela,camera))under++;
            }
            sela.GetComponent<PartnerBrain>().enabled=false;
            Assert.Greater(frames,20,"fixture: a second of settled samples");
            Assert.AreEqual(0,under,$"she stood under a HUD plate in {under} of the last {frames} frames, at {sela.transform.position}");
            var gap=sela.transform.position-spot;gap.y=0;
            Assert.Less(gap.magnitude,7,"she cleared the plate by going far from where she'd have stood, rather than to the nearest place that clears it");
            var middle=camera.WorldToViewportPoint(sela.transform.position+Vector3.up*(flight?0:.9f));
            Assert.IsTrue(middle.z>0&&middle.x>0&&middle.x<1&&middle.y>0&&middle.y<1,$"she cleared the plate by leaving the screen: {middle}");
        }
    }
}
