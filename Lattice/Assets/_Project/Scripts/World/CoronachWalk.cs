using System;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Chapter 9's Coronach as a whole, one to the zone (docs/maps/DecksLeaving.md, "The Coronach"). From the call to the close its own music plays over the zone's (<see cref="music"/>, one of the
    /// zone's <c>cueMusicIds</c>, asked of <see cref="MusicDirector"/> as an encounter's track is, so it outranks the zone and a fight and only a shop's counter could take it, and none is open);
    /// from the call until the party leaves for the Ring nothing barks (<see cref="BarkService.Hush"/>). It counts the needed doors heard, and the town thins in step with them: the first of
    /// <see cref="town"/> go aboard as the first doors are heard, each only once the party is away and it is out of the camera's view, all of them at once on a load. Nothing in it runs on a
    /// clock: a player can stand in the street for as long as they like and nothing moves on without them. The doors themselves are <see cref="CoronachDoor"/>s; the count's flags are
    /// <see cref="FlagAll"/>s. Logs <c>CORONACH_ON</c>, <c>CORONACH_DOORS n/m</c>, <c>CORONACH_OFF</c>.
    /// </summary>
    public sealed class CoronachWalk:MonoBehaviour
    {
        public string calledFlag="home.coronachCalled",walkedFlag="home.coronachWalked",launchedFlag="home.launched";
        /// <summary>The walk's own track, by title from docs/music/tracks.json.</summary>
        public string music;
        /// <summary>The needed doors' flags.</summary>
        public string[] doors=Array.Empty<string>();
        /// <summary>The town carrying things to the airlocks, in the order they go aboard.</summary>
        public GameObject[] town=Array.Empty<GameObject>();
        /// <summary>How far the lead must be from one of the town before it goes.</summary>
        public float goneFrom=10;
        /// <summary>Prompts with no business being pressed while the walk is on, switched off from the call until the launch: the three docks to the Halo (gate six's stood nearer than
        /// Sela's last word, and a press launched the party for the Halo mid-walk) and the still things Tam is shown, Tam having gone aboard.</summary>
        public Behaviour[] shut=Array.Empty<Behaviour>();
        /// <summary>The walk's people, lanterns and lamps are saved switched off, so an editor still of the zone shows the day as the Decks built it and not the walk standing in it twice over
        /// (their bodies are still there, in the navigation bake); play switches them back on as the zone wakes, and the walk's own parts decide who stands where.</summary>
        public Renderer[] drawn=Array.Empty<Renderer>();public Light[] lit=Array.Empty<Light>();
        void Awake(){foreach(var r in drawn)if(r!=null)r.enabled=true;foreach(var l in lit)if(l!=null)l.enabled=true;}
        public static CoronachWalk Current{get;private set;}
        bool settled,on,hushed,cued;int counted=-1;
        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        /// <summary>From the call to the close.</summary>
        public bool Walking=>Flag(calledFlag)&&!Flag(walkedFlag);
        public int DoorsHeard{get{int n=0;foreach(var door in doors)if(Flag(door))n++;return n;}}
        /// <summary>How many of the town have gone aboard by now, as the doors heard say.</summary>
        public int Aboard=>doors.Length==0||!Flag(calledFlag)?0:Mathf.Min(town.Length,town.Length*DoorsHeard/doors.Length);
        void OnEnable()=>Current=this;
        /// <summary>Before anything is offered on a load into the walk.</summary>
        void Start(){if(GameServices.Current==null)return;bool walk=Flag(calledFlag)&&!Flag(launchedFlag);foreach(var prompt in shut)if(prompt!=null)prompt.enabled=!walk;}
        void OnDisable(){if(Current==this)Current=null;Release();}
        void OnDestroy()=>Release();
        void Release(){if(cued){MusicDirector.Encounter(this,null,false);cued=false;}if(hushed){BarkService.Hush(this,false);hushed=false;}}
        void Update()
        {
            if(GameServices.Current==null)return;
            bool walking=Walking;
            if(walking!=on){on=walking;Debug.Log(on?"CORONACH_ON":"CORONACH_OFF");}
            // The zone's own music comes back to nobody: from the close the launch asks for its own.
            if(walking&&!string.IsNullOrEmpty(music)){if(MusicDirector.Current!=null&&MusicDirector.Current.Track!=music){MusicDirector.Encounter(this,music,true);cued=true;}}
            else if(cued){MusicDirector.Encounter(this,null,false);cued=false;}
            bool hush=Flag(calledFlag)&&!Flag(launchedFlag);
            if(hush!=hushed){BarkService.Hush(this,hush);hushed=hush;}
            foreach(var prompt in shut)if(prompt!=null&&prompt.enabled==hush)prompt.enabled=!hush;
            int heard=DoorsHeard;if(heard!=counted){counted=heard;if(Flag(calledFlag))Debug.Log("CORONACH_DOORS "+heard+"/"+doors.Length);}
            int aboard=Aboard;
            for(int i=0;i<town.Length;i++)
            {
                var one=town[i];if(one==null||!one.activeSelf||i>=aboard)continue;
                if(!settled||Away(one.transform.position)){one.SetActive(false);Debug.Log("CORONACH_TOWN_ABOARD "+one.name);}
            }
            settled=true;
        }
        bool Away(Vector3 at)
        {
            var party=Lattice.Combat.PartyController.Current;if(party==null||party.members==null||party.members.Length==0||party.Active==null)return false;
            var offset=party.Active.transform.position-at;offset.y=0;
            return offset.magnitude>=goneFrom&&!CoronachDoor.Seen(at);
        }
    }
}
