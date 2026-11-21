using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A section that comes home and couples on (chapter 9: tread-hall four rejoining the Meridian Crawl after the run, <c>HomeBuilder.Crawl.cs</c>, docs/maps/MeridianCrawl.md,
    /// "For the Terminator"). Once <see cref="flag"/> is set (the run's last coupling mated), the drawn <see cref="hall"/> stands coupled in its place; if the zone opens with that
    /// flag set and <see cref="doneFlag"/> not yet (the party has just come back from the run), it is seen coming: it starts <see cref="from"/> its place and rolls in over
    /// <see cref="seconds"/>, slowing as it closes, and couples with a knock. A save with <see cref="doneFlag"/> loads it coupled with nothing played. Only the drawn hall moves;
    /// what is walked on (its porch and gangway, on static boxes in the bake) stands in its coupled place throughout, behind a gate that its own talk opens. It rolls on the
    /// unscaled clock, so the talk that greets it doesn't stop it. Logs <c>COUPLES_IN name</c> and <c>COUPLED name</c>.
    /// </summary>
    public sealed class CouplesIn:MonoBehaviour
    {
        public string flag,doneFlag;
        /// <summary>What rolls in: drawn only (not static), turned about nothing, moved from <see cref="from"/> (world offset) to where it was built.</summary>
        public Transform hall;public Vector3 from=new(30,0,0);public float seconds=7;
        public string sound="impactMetal_000";
        /// <summary>What keeps the way onto it shut (the lip's gate and its carving obstacle): shown until the hall has coupled and <see cref="opensWith"/> is set, so nobody steps onto
        /// a porch that is still rolling in, at any moment of it.</summary>
        public GameObject[] shut=System.Array.Empty<GameObject>();public string opensWith;
        Vector3 rest;bool started,wasSet;float began=-1;
        /// <summary>Nought while it is still out, one once it is coupled.</summary>
        public float Coupled{get;private set;}
        void Start()
        {
            if(hall==null){enabled=false;return;}
            rest=hall.position;started=true;var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
            bool set=flags!=null&&!string.IsNullOrEmpty(flag)&&flags.GetBool(flag),done=flags!=null&&!string.IsNullOrEmpty(doneFlag)&&flags.GetBool(doneFlag);
            wasSet=set;
            // Not back yet: it stands in its place, hidden by its gate, and comes in if the flag is set while the zone is open.
            if(done)Place(1);else if(set)Roll();else Coupled=0;
            Gate(flags);
        }
        /// <summary>Open only once coupled and told so.</summary>
        public bool Open{get;private set;}
        void Gate(FlagService flags)
        {
            bool open=Coupled>=1&&(string.IsNullOrEmpty(opensWith)||flags!=null&&flags.GetBool(opensWith));if(open==Open&&started&&shutSet)return;
            Open=open;shutSet=true;foreach(var g in shut)if(g!=null&&g.activeSelf==open)g.SetActive(!open);
        }
        bool shutSet;
        void Roll(){began=Time.unscaledTime;Place(0);Debug.Log("COUPLES_IN "+name);}
        void Place(float t)
        {
            Coupled=t;float eased=1-(1-t)*(1-t)*(1-t);
            hall.position=rest+from*(1-eased);
        }
        void Update()
        {
            if(!started)return;
            Gate(GameServices.Current!=null?GameServices.Current.Flags:null);
            if(began<0)
            {
                // The flag set while the zone is open (nothing does that today, but a test or a later beat may): it comes in then.
                var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
                bool set=flags!=null&&!string.IsNullOrEmpty(flag)&&flags.GetBool(flag);
                if(set&&!wasSet&&Coupled<1)Roll();wasSet=set;
                return;
            }
            if(GameTime.Paused){began+=Time.unscaledDeltaTime;return;}
            float t=Mathf.Clamp01((Time.unscaledTime-began)/Mathf.Max(.1f,seconds));Place(t);
            if(t>=1)
            {
                began=-1;
                if(!string.IsNullOrEmpty(sound))AudioManager.PlayAt(sound,hall,rest+Vector3.up*2,.45f,AudioBus.SFX,40,.06f,.5f);
                Debug.Log("COUPLED "+name);
            }
        }
    }
}
