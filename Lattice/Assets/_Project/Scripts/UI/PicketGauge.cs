using Lattice.Combat;
using Lattice.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// How much the picket has heard (<see cref="PicketAlarm"/>), under the vitals, only where there is a picket (the Picket of Teeth, whose rule is staged detection): its stage by name and
    /// what that stage brings in a line of words, and a bar in three parts that fills toward the next stage while a beam is on the lead ship and drains after it has gone. "SEEN" while a
    /// beam is on the ship; "HIDDEN" where no live buoy could see it at any turn of its sweep (a rock's shadow, or out of every reach). Anywhere without a picket it is hidden. Built into
    /// the HUD's canvas by GameHud, in the place the glow and current gauges take in their own zones.
    /// </summary>
    public sealed class PicketGauge:MonoBehaviour
    {
        public const float Width=480;
        static readonly Color Back=new(.025f,.045f,.065f,.85f),Tick=new(.85f,.9f,.95f,.7f);
        /// <summary>Each stage's colour: cold, amber, the wing's orange, the carrier's red.</summary>
        public static readonly Color[] Tones={new(.62f,.84f,1f),new(1f,.68f,.2f),new(1f,.46f,.16f),new(1f,.24f,.16f)};
        GameObject root;Image fill;TMP_Text title,brings;
        public static string Name(int stage)=>stage switch{0=>"QUIET",1=>"AMBER",2=>"HUNTER WING",_=>"CARRIER"};
        /// <summary>What the stage brings, and what to do about it.</summary>
        public static string Brings(int stage)=>stage switch
        {
            0=>"Keep out of the beams, or in a rock's shadow",
            1=>"Half heard: get into a rock's shadow",
            2=>"Break the wing, or lose it in shadow",
            _=>"The carrier is awake: run the lane",
        };
        public static string Title(PicketAlarm alarm,bool hidden)=>"PICKET   "+Name(alarm.Stage)+(alarm.Seen?"  ·  SEEN":hidden?"  ·  HIDDEN":"");
        /// <summary>Whether no live buoy could see this point at any turn of its sweep.</summary>
        public static bool Hidden(Vector3 point)=>SensorSweep.HiddenFromAll(point);
        public void Build(Transform canvas)
        {
            root=new GameObject("PicketGauge",typeof(RectTransform));root.transform.SetParent(canvas,false);
            UiKit.Rect(root,new Vector2(0,1),new Vector2(0,1),new Vector2(297,736)-new Vector2(0,1080),new Vector2(564,110));
            var frame=UiKit.DarkFrame(root.transform,"PicketFrame");frame.raycastTarget=false;frame.pixelsPerUnitMultiplier=2;UiKit.Rect(frame.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).sizeDelta=Vector2.zero;
            title=UiKit.Legible(UiKit.Text(root.transform,"PicketTitle","PICKET",24,UiKit.LooseTextColor,TextAlignmentOptions.Left));
            UiKit.Rect(title.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(10,30),new Vector2(510,30));
            brings=UiKit.Legible(UiKit.Text(root.transform,"PicketBrings","",24,UiKit.LooseTextColor,TextAlignmentOptions.Left));
            UiKit.Rect(brings.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(10,-2),new Vector2(510,30));
            var back=UiKit.Panel(root.transform,"PicketBack",Back);back.raycastTarget=false;UiKit.Rect(back.gameObject,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(-15,-34),new Vector2(Width,14));
            fill=UiKit.Panel(back.transform,"PicketHeard",Tones[0]);fill.raycastTarget=false;var rect=UiKit.Rect(fill.gameObject,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(0,14));rect.pivot=new Vector2(0,.5f);
            // The bar is the three stages end to end: a tick where each begins.
            for(int k=1;k<3;k++){var tick=UiKit.Panel(back.transform,"PicketTick"+k,Tick);tick.raycastTarget=false;UiKit.Rect(tick.gameObject,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(Width*k/3f,0),new Vector2(3,22));}
            root.SetActive(false);
        }
        /// <summary>How far along the three stages the picket is: its stage and how near the next being seen has brought it.</summary>
        public static float Level(PicketAlarm alarm)=>Mathf.Clamp01((alarm.Stage+(alarm.Stage>=PicketAlarm.Carrier?0:alarm.Heard))/3f);
        void LateUpdate()
        {
            if(root==null)return;
            var alarm=PicketAlarm.Current;var party=PartyController.Current;var lead=party!=null?party.Active:null;
            bool show=alarm!=null&&lead!=null&&lead.flight;
            if(root.activeSelf!=show)root.SetActive(show);
            if(!show)return;
            var tone=Tones[Mathf.Clamp(alarm.Stage,0,3)];
            fill.rectTransform.sizeDelta=new Vector2(Width*Level(alarm),14);
            // Brighter while a beam is on the ship.
            fill.color=alarm.Seen?Color.Lerp(tone,Color.white,.35f):tone;
            string said=Title(alarm,!alarm.Seen&&Hidden(lead.transform.position));if(title.text!=said)title.text=said;
            title.color=alarm.Stage>0?tone:UiKit.LooseTextColor;
            string what=Brings(alarm.Stage);if(brings.text!=what)brings.text=what;
        }
    }
}
