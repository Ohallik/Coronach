using System.Collections;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>W9-picket-01. In a flight fight the camera keeps a ship in a side third under the corner panels' foot, a constant (<see cref="CameraRig.PanelFoot"/>, .69 of the
    /// frame's height). In the Picket of Teeth the alarm plate hangs under the party panel to about .605, and at nov26-r7-chain s45/179, the frame drawn back for the Flak Gate, the
    /// lead's ship sat under the plate's corner. The HUD now tells the camera its left column's real foot each frame; where nothing hangs below .69 the camera is told .69 and frames
    /// to the pixel as it did. The state as it was is the camera's switch <c>footAsBefore</c> (read by name, so this file also builds against the camera without it).</summary>
    public sealed class PicketPlateFootTests
    {
        static GameState State=>GameServices.Current.State;
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static void Flag(string key)=>GameServices.Current.Flags.SetBool(key,true);
        static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
        static FieldInfo AsBeforeSwitch=>typeof(CameraRig).GetField("footAsBefore",BindingFlags.NonPublic|BindingFlags.Static);
        static void AsBefore(bool on)=>AsBeforeSwitch?.SetValue(null,on);
        /// <summary>The foot the camera was told this frame, or the constant where the camera has no such thing.</summary>
        static float Told()=>typeof(CameraRig).GetProperty("LeftFoot",BindingFlags.Public|BindingFlags.Static)?.GetValue(null) is float told?told:CameraRig.PanelFoot;
        [UnitySetUp] public IEnumerator Boot()
        {
            AsBefore(false);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.6f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            AsBefore(false);GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Zone(string zone,string spawn="Arrival")
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var hero in PartyController.Current.members){hero.GetComponent<PlayerBrain>().AutoPilot=true;hero.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.8f);
        }
        static void Put(CombatActor actor,Vector3 at)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is FlightMotor ship)ship.Halt();
            actor.transform.position=at;body.enabled=true;Physics.SyncTransforms();
        }
        /// <summary>Holds both ships where they were put, noses up the lane, while the view settles: a fight's frame comes in over a second and a half and holds 1.6 s more.</summary>
        static IEnumerator Hold(Vector3 lead,Vector3 partner,float seconds)
        {
            for(float end=Time.unscaledTime+seconds;Time.unscaledTime<end;)
            {
                Put(Lead,lead);Put(Partner,partner);((FlightMotor)Lead.motor).KeepFacing(Vector3.forward);Camera.main.aspect=16f/9;
                yield return null;
            }
        }
        /// <summary>The batch runner's screen is 4:3, where the HUD (scaled to fit, Expand) is smaller against the frame's height and the alarm plate ends over .69: the fault is a
        /// 16:9 one (1920 by 1080 in the capture). The lens is drawn at 16:9 and the HUD scaled by the height, as Expand scales it on a 16:9 screen; put back by the returned action.</summary>
        static System.Action Sixteen9()
        {
            var scaler=Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(s=>s.name=="HUD");
            var mode=scaler.screenMatchMode;float match=scaler.matchWidthOrHeight;
            scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1;Camera.main.aspect=16f/9;
            return ()=>{if(scaler!=null){scaler.screenMatchMode=mode;scaler.matchWidthOrHeight=match;}if(Camera.main!=null)Camera.main.ResetAspect();};
        }
        /// <summary>Where the camera's own rule reads the ship (its middle .8 m up and 2.5 m round it), on the screen: its left and its top.</summary>
        static (float left,float top,float x,float y) Ship()
        {
            var view=Camera.main;var at=Lead.transform.position+Vector3.up*.8f;float left=1,top=0;
            for(int corner=0;corner<8;corner++)
            {
                var sign=new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1);
                var seen=view.WorldToViewportPoint(at+sign*2.5f);left=Mathf.Min(left,seen.x);top=Mathf.Max(top,seen.y);
            }
            var middle=view.WorldToViewportPoint(Lead.transform.position);
            return (left,top,middle.x,middle.y);
        }
        /// <summary>The alarm plate's lower edge and right edge on the screen, as shares of its height and width, read from the plate itself.</summary>
        static (float foot,float right) Plate()
        {
            var gauge=Object.FindFirstObjectByType<PicketGauge>();Assert.IsNotNull(gauge,"fixture: the HUD has a picket gauge");
            Assert.IsTrue(gauge.Showing,"fixture: the Picket's alarm plate is up");
            var root=(RectTransform)Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(r=>r.name=="PicketGauge");
            // Its right edge as a share of the 1080p layout's width, which is the frame's at 16:9.
            var box=PanelFade.ScreenBox(root);float scale=root.GetComponentInParent<Canvas>().scaleFactor;return (box.yMin/Screen.height,box.xMax/scale/1920);
        }

        /// <summary>The lead's and Idra's places in the last frame of s45's step 179 ('through the gap', frames.csv: the lead at (-0.60, 649.07), Idra at (0.37, 639.95)), the lock
        /// worked and a flak gate locked, as the still shows ('FLAK GATE 2400/2400'). As it was, the frame drawn back for the gate holds the ship in the left third under .69 and
        /// over the plate's foot; now it stands under the plate.</summary>
        [UnityTest] public IEnumerator TheLeadsShipStandsClearOfTheAlarmPlateWhereTheFrameDrawsBackForTheFlakGate()
        {
            DevLoadout.Apply("chapter8");
            Flag("vask.brekkWord");Flag("vask.lockAdmitted");Flag("vask.picketRead");State.questSteps["Harrowgate"]=2;
            ContinuousReview.ApplyPair(new QualityRoute{name="the Picket",scene="PicketOfTeeth",deployed=new[]{"Taren","Idra"}});
            Flag("vask.picketCrossed");Flag("clear.Buoy_4");foreach(string f in new[]{"vask.lock1","vask.lock2","vask.lock3","vask.lock4","vask.lockRemoved"})Flag(f);
            yield return Zone("PicketOfTeeth","Lock");
            for(int i=0;i<4&&Lead.character!="Taren";i++){PartyController.Current.Swap();yield return Frames(30);}
            Assert.AreEqual("Taren",Lead.character,"fixture: Taren leads the Picket");
            Vector3 lead=new(-.6f,1,649.07f),partner=new(.37f,1,639.95f);
            // The east gate: in the still the view stood 14 m east of the lead, drawn toward it (frames.csv's camera at x 14.2), with the gate right of the ships.
            var gate=Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None).Where(b=>b.definition!=null&&b.definition.id=="FlakGate"&&b.Health.Alive)
                .OrderByDescending(b=>b.transform.position.x).FirstOrDefault();
            Assert.IsNotNull(gate,"fixture: a flak gate stands at the lock");
            float[] tops=new float[2],lefts=new float[2];(float foot,float right) plate=default;float toldAfter=0;
            var restore=Sixteen9();
            try{
            for(int pass=0;pass<2;pass++)
            {
                bool before=pass==0;AsBefore(before);
                Lead.target=gate.Health;Lead.TargetLocked=true;
                yield return Hold(lead,partner,5);
                Lead.target=gate.Health;Lead.TargetLocked=true;yield return Hold(lead,partner,.5f);
                plate=Plate();var ship=Ship();tops[pass]=ship.top;lefts[pass]=ship.left;if(!before)toldAfter=Told();
                var camera=Camera.main.transform.position;
                TestContext.WriteLine($"PICKET_FOOT {(before?"before":"after")}: ship middle ({ship.x:0.000}, {ship.y:0.000}), its box's left {ship.left:0.000} top {ship.top:0.000}; plate foot {plate.foot:0.000} right {plate.right:0.000}; told {Told():0.000}; camera ({camera.x:0.0}, {camera.y:0.0}, {camera.z:0.0}); gate {gate.transform.position:F0}, {Vector3.Distance(gate.transform.position,lead):0} m; aspect {Camera.main.aspect:0.000}");
            }
            }
            finally{restore();}
            Assert.AreEqual(1-(Object.FindFirstObjectByType<GameHud>().ColumnFoot/1080f),plate.foot,.002f,"fixture: the plate stands where the HUD's column says it does");
            Assert.Less(plate.foot,CameraRig.PanelFoot,"fixture: the alarm plate hangs under the corner panels' foot");
            // As it was first: the ship in the plate's span and over its foot.
            Assert.Less(lefts[0],plate.right,"fixture, as it was: the ship stands in the plate's span");
            Assert.Greater(tops[0],plate.foot,"fixture, as it was: the ship stands up under the alarm plate's corner");
            Assert.That(tops[1]<=plate.foot+.002f||lefts[1]>=plate.right,$"the lead's ship stands under the alarm plate's corner: its top at {tops[1]:0.000} of the frame's height, the plate's foot at {plate.foot:0.000}");
            Assert.AreEqual(plate.foot,toldAfter,.002f,"the camera wasn't told the plate's foot");
        }

        /// <summary>Where no plate hangs under .69 the camera is told .69 on every frame, and a fight's frame with the ship in the left third stands to the pixel where it stood:
        /// the Aurora Shear (the current gauge, at the foot of the screen) and the flight yard (no gauge).</summary>
        [UnityTest] public IEnumerator WhereNoPlateHangsLowerTheFightsFrameIsAsItWasToThePixel()
        {
            // Each pass from the zone loaded afresh: a view settling from where the last pass left it, held back by the fight's hold, read 7 px apart with nothing told
            // differently (r10b-after2).
            foreach(string zone in new[]{"AuroraShear","Arena_Flight"})
            {
                float[] xs=new float[2],ys=new float[2],tops=new float[2];Vector3[] cameras=new Vector3[2];float least=1,most=0;
                for(int pass=0;pass<2;pass++)
                {
                yield return Boot();
                if(zone=="AuroraShear"){DevLoadout.Apply("chapter6");Flag("hoar.charted");Flag("hoar.idraHailed");Flag("hoar.towed");State.questSteps["LongSleep"]=2;}
                else DevLoadout.Apply("starter");
                yield return Zone(zone);
                Vector3 lead=Lead.transform.position,partner=lead+new Vector3(3,0,-4);
                // Down the screen and to the right of the lead, so the frame stands him up in its left third, against the panels' foot.
                var foe=ActorFactory.Enemy(GameCatalog.Find<Lattice.Data.EnemyDef>("Cantor"),lead+new Vector3(42,0,-20));foe.Passive=true;
                var restore=Sixteen9();
                try
                {
                    AsBefore(pass==0);
                    Lead.target=foe.Health;Lead.TargetLocked=true;yield return Hold(lead,partner,5);
                    for(int f=0;f<30;f++){Lead.target=foe.Health;Lead.TargetLocked=true;Put(Lead,lead);Put(Partner,partner);yield return null;if(pass==1){least=Mathf.Min(least,Told());most=Mathf.Max(most,Told());}}
                    var ship=Ship();xs[pass]=ship.x;ys[pass]=ship.y;tops[pass]=ship.top;cameras[pass]=Camera.main.transform.position;
                    TestContext.WriteLine($"PANEL_FOOT {zone} {(pass==0?"before":"after")}: ship ({ship.x:0.0000}, {ship.y:0.0000}) top {ship.top:0.000}; camera {cameras[pass]:F3}; told {least:0.0000} to {most:0.0000}");
                }
                finally{restore();AsBefore(false);}
                Object.Destroy(foe.gameObject);
                }
                Assert.Less(xs[0],CameraRig.PanelSide+.05f,$"fixture, {zone}: the ship stands in the left third, where the panels' foot holds it");
                Assert.Greater(tops[0],CameraRig.PanelFoot-.05f,$"fixture, {zone}: the ship stands up the frame against the panels' foot, which holds it there");
                Assert.AreEqual(CameraRig.PanelFoot,least,$"{zone}: the camera was told a foot other than the corner panels' with no plate under them");
                Assert.AreEqual(CameraRig.PanelFoot,most,$"{zone}: the camera was told a foot other than the corner panels' with no plate under them");
                // In pixels of the capture's 1920 by 1080 frame.
                Assert.AreEqual(xs[0]*1920,xs[1]*1920,1,$"{zone}: the ship moved across the screen where no plate hangs low");
                Assert.AreEqual(ys[0]*1080,ys[1]*1080,1,$"{zone}: the ship moved up or down the screen where no plate hangs low");
            }
        }
    }
}
