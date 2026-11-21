using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Weather that closes in: the White Reach's whiteouts. The wind drops first (the warning: the blown snow stops and goes quiet for <see cref="warning"/> seconds), then visibility
    /// closes for <see cref="hold"/> seconds and lifts. While it is closed the zone's fog comes in from <see cref="nearFog"/> to <see cref="farFog"/> metres past the lead (measured
    /// from the camera, so the party itself stays clear and the top of the frame goes white), blown snow streams round the lead, and a hero outside every <see cref="shelters"/> box walks at <see cref="pace"/>. Inside one (a vent pocket:
    /// its steam marks it) the whiteout doesn't reach: no slow, and the fog only half in. Danger markings take no fog (their shaders have none), so a whiteout never hides a blow.
    /// The first one closes once <see cref="startFlag"/> is set and the conversation that set it has closed (Idra's lesson, with nothing about); after that one closes each time the
    /// lead walks into a <see cref="fronts"/> box, once a visit each (the stalkers' field). Leaving <see cref="field"/> lifts one early. Nothing is saved: a load starts calm.
    /// Logs <c>WHITEOUT_WARNING</c>, <c>WHITEOUT_CLOSED</c>, <c>WHITEOUT_LIFTED</c>; plays the lead's <c>whiteout</c> bark as it closes.
    /// </summary>
    public sealed class Whiteout:MonoBehaviour
    {
        public string startFlag;
        /// <summary>Set once the first whiteout has lifted (and at load, if the save has the first one's flag: it has been), for whoever waits it out with the party.</summary>
        public string liftedFlag;
        public Bounds field;
        public Bounds[] fronts=System.Array.Empty<Bounds>(),shelters=System.Array.Empty<Bounds>();
        public float warning=3.5f,closing=2.5f,hold=22,lifting=4;
        public float nearFog=2,farFog=18,pace=.7f;
        public Color white=new(.88f,.92f,.97f);
        /// <summary>The blown snow: streams round the lead while it is closed (its emission is set here), and a lighter drift across the field when calm.</summary>
        public ParticleSystem snow;public float calmRate=40,closedRate=900;
        public string bark="whiteout";
        public enum Phase{Calm,Warning,Closing,Closed,Lifting}
        public Phase Now{get;private set;}
        /// <summary>0 clear to 1 fully closed, as the fog stands now.</summary>
        public float Density{get;private set;}
        public static Whiteout Current{get;private set;}
        float timer;bool started,firstDone,firstRunning;bool[] crossed;
        bool fogWas;float startWas,endWas;Color colourWas;FogMode modeWas;
        void OnEnable(){Current=this;}
        void Start()
        {
            crossed=new bool[fronts.Length];
            fogWas=RenderSettings.fog;startWas=RenderSettings.fogStartDistance;endWas=RenderSettings.fogEndDistance;colourWas=RenderSettings.fogColor;modeWas=RenderSettings.fogMode;
            // A save that already has the lesson doesn't play it again: the first whiteout has been.
            firstDone=Has(startFlag);started=true;Rate(calmRate);
            if(firstDone&&!string.IsNullOrEmpty(liftedFlag)&&!Has(liftedFlag))GameServices.Current.Flags.SetBool(liftedFlag,true);
        }
        static bool Has(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        void Rate(float rate){if(snow==null)return;var emission=snow.emission;emission.rateOverTime=rate;}
        public bool Sheltered(Vector3 at){foreach(var box in shelters)if(box.Contains(new Vector3(at.x,box.center.y,at.z)))return true;return false;}
        /// <summary>Begins one now, from its warning (a test, or a front).</summary>
        public void Begin(){if(Now!=Phase.Calm)return;Now=Phase.Warning;timer=0;Rate(0);Debug.Log("WHITEOUT_WARNING");}
        void Update()
        {
            var services=GameServices.Current;var party=PartyController.Current;
            if(!started||services==null||party==null||party.Active==null||GameTime.Paused)return;
            var lead=party.Active.transform.position;
            if(Now==Phase.Calm)
            {
                if(!firstDone&&Has(startFlag)&&!services.Input.Blocked){firstDone=firstRunning=true;Begin();Now=Phase.Closing;timer=0;Close();}
                else if(firstDone)for(int i=0;i<fronts.Length;i++)if(!crossed[i]&&fronts[i].Contains(new Vector3(lead.x,fronts[i].center.y,lead.z))){crossed[i]=true;Begin();break;}
            }
            timer+=Time.deltaTime;
            switch(Now)
            {
                case Phase.Warning:if(timer>=warning){Now=Phase.Closing;timer=0;Close();}break;
                case Phase.Closing:Density=Mathf.SmoothStep(0,1,Mathf.Clamp01(timer/closing));if(timer>=closing){Now=Phase.Closed;timer=0;Debug.Log("WHITEOUT_CLOSED");}break;
                case Phase.Closed:Density=1;if(timer>=hold||!field.Contains(new Vector3(lead.x,field.center.y,lead.z))){Now=Phase.Lifting;timer=0;Rate(calmRate);}break;
                case Phase.Lifting:Density=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(timer/lifting));if(timer>=lifting){Now=Phase.Calm;Density=0;timer=0;Restore();Debug.Log("WHITEOUT_LIFTED");if(firstRunning&&!string.IsNullOrEmpty(liftedFlag))services.Flags.SetBool(liftedFlag,true);firstRunning=false;}break;
            }
            if(Now!=Phase.Calm||Density>0)Fog(Sheltered(lead)?Density*.5f:Density);
            if(Density>.5f)foreach(var hero in party.members)if(hero!=null&&hero.Health.Alive&&!Sheltered(hero.transform.position))hero.Health.Hinder(pace,.2f);
            if(snow!=null&&Now!=Phase.Calm)snow.transform.position=lead+Vector3.up*2;
        }
        void Close()
        {
            Rate(closedRate);
            var lead=PartyController.Current!=null?PartyController.Current.Active:null;
            if(lead!=null&&!string.IsNullOrEmpty(bark))BarkService.Play(lead.character,bark,true);
        }
        void Fog(float amount)
        {
            // Measured from the camera, which stands the ground camera's distance from the lead: the lead is always on the clear side of the fog's start.
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;float camera=SightlineAudit.Distance;
            RenderSettings.fogStartDistance=Mathf.Lerp(fogWas?startWas:400,camera+nearFog,amount);
            RenderSettings.fogEndDistance=Mathf.Lerp(fogWas?endWas:600,camera+farFog,amount);
            RenderSettings.fogColor=Color.Lerp(colourWas,white,amount);
            if(amount<=0)Restore();
        }
        void Restore(){RenderSettings.fog=fogWas;RenderSettings.fogStartDistance=startWas;RenderSettings.fogEndDistance=endWas;RenderSettings.fogColor=colourWas;RenderSettings.fogMode=modeWas;}
        void OnDisable(){if(started)Restore();if(Current==this)Current=null;}
    }
}
