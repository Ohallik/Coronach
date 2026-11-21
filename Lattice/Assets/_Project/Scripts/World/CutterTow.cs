using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Idra Sorn's striped cutter in the Aurora Shear (chapter 6): the tow out of the first current as it turns, and then the cutter to follow down to Lastlight.
    /// <para>Once <see cref="flag"/> is set (her hail's last line) and nothing is being said, the cutter puts a line on the lead ship and flies <see cref="tow"/>: the party comes
    /// after it on the line, held to it whatever the stick says, and is let go at the line's end, in a current running its way. That sets <see cref="doneFlag"/>; a save that has
    /// it loads with the tow over. From then the cutter leads along <see cref="lead"/>, waiting at each point until the lead ship is within <see cref="waitFor"/> metres of it, and
    /// at the last it stays (the way down). Logs <c>TOW_START</c>, <c>TOW_DONE</c>, <c>CUTTER_LEADS i</c>.</para>
    /// </summary>
    public sealed class CutterTow:MonoBehaviour
    {
        public string flag="",doneFlag="";
        /// <summary>The cutter's drawn body: it flies, the line hangs from its stern.</summary>
        public Transform cutter;
        public LineRenderer line;
        /// <summary>The tow's way at the plane, from where the cutter waits for the hail to where it lets the party go.</summary>
        public Vector3[] tow=System.Array.Empty<Vector3>();
        /// <summary>Where it leads afterwards, the last point the way down.</summary>
        public Vector3[] lead=System.Array.Empty<Vector3>();
        public float towSpeed=11,leadSpeed=16,waitFor=34,lineLength=7;
        public bool Towing=>state==State.Towing;
        public int Leg=>leg;
        enum State{Waiting,Towing,Leading}
        State state;int leg;float along;
        bool Has(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        void Start()
        {
            if(line!=null)line.enabled=false;
            if(Has(doneFlag)){state=State.Leading;leg=Ahead();if(cutter!=null&&lead.Length>0)Face(lead[Mathf.Min(leg,lead.Length-1)],leg>0?lead[leg]-lead[leg-1]:Vector3.forward);}
            else if(cutter!=null&&tow.Length>0)Face(tow[0],tow.Length>1?tow[1]-tow[0]:Vector3.forward);
        }
        /// <summary>The first leading point not yet behind the lead ship (a reload puts the cutter ahead of the party, not back where the tow ended).</summary>
        int Ahead()
        {
            var party=PartyController.Current;if(party==null||party.members==null||party.Active==null||lead.Length==0)return 0;
            var at=party.Active.transform.position;int best=0;float nearest=float.PositiveInfinity;
            for(int i=0;i<lead.Length;i++){float d=Flat(lead[i]-at).magnitude;if(d<nearest){nearest=d;best=i;}}
            return Mathf.Min(lead.Length-1,best+1);
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        void Face(Vector3 at,Vector3 heading){cutter.position=at;heading.y=0;if(heading.sqrMagnitude>.01f)cutter.rotation=Quaternion.LookRotation(heading);}
        void Update()
        {
            if(cutter==null||GameTime.Paused||GameServices.Current==null)return;
            var party=PartyController.Current;
            switch(state)
            {
                case State.Waiting:
                    if(!Has(flag)||Dialogue())return;
                    if(Has(doneFlag)){state=State.Leading;return;}
                    state=State.Towing;along=0;Debug.Log("TOW_START");if(line!=null)line.enabled=true;
                    break;
                case State.Towing:
                {
                    along+=towSpeed*Time.deltaTime;var at=Along(tow,along,out var heading,out bool end);Face(at,heading);
                    if(party!=null&&party.members!=null)
                    {
                        int k=0;
                        foreach(var ship in party.members)
                        {
                            if(ship==null||!ship.flight)continue;
                            // Held on the line: astern of the cutter, the partner a hull's width beside the lead.
                            var side=Vector3.Cross(Vector3.up,heading.normalized)*(k==0?0:4);var goal=at-heading.normalized*(lineLength+3*k)+side;goal.y=ship.transform.position.y;
                            ship.Health.Hinder(0);var body=ship.GetComponent<CharacterController>();
                            if(body!=null&&body.enabled)body.Move(Vector3.ClampMagnitude(goal-ship.transform.position,towSpeed*1.6f*Time.deltaTime+Mathf.Max(0,(goal-ship.transform.position).magnitude-1)*.08f));
                            k++;
                        }
                        if(line!=null&&party.Active!=null){line.positionCount=2;line.SetPosition(0,cutter.position-cutter.forward*2.4f+Vector3.up*.3f);line.SetPosition(1,party.Active.transform.position+Vector3.up*.3f);}
                    }
                    if(!end)return;
                    state=State.Leading;leg=0;if(line!=null)line.enabled=false;
                    GameServices.Current.Flags.SetBool(doneFlag,true);GameServices.Current.Saves.Save("autosave",GameServices.Current.State);Debug.Log("TOW_DONE");
                    break;
                }
                case State.Leading:
                {
                    if(lead.Length==0||leg>=lead.Length)return;
                    var goal=lead[leg];var gap=goal-cutter.position;gap.y=0;
                    if(gap.magnitude>.5f){var step=Vector3.ClampMagnitude(gap,leadSpeed*Time.deltaTime);Face(cutter.position+step,gap);return;}
                    // At a point: it waits for the lead ship, then goes on; at the last it stays.
                    if(leg==lead.Length-1)return;
                    var lead0=party!=null&&party.members!=null?party.Active:null;
                    if(lead0!=null&&Flat(lead0.transform.position-cutter.position).magnitude<waitFor){leg++;Debug.Log("CUTTER_LEADS "+leg);}
                    break;
                }
            }
        }
        static bool Dialogue()=>GameServices.Current.Input.Blocked;
        static Vector3 Along(Vector3[] path,float s,out Vector3 heading,out bool end)
        {
            heading=Vector3.forward;end=false;
            for(int i=1;i<path.Length;i++)
            {
                var d=path[i]-path[i-1];float leg=d.magnitude;heading=d;
                if(s<=leg)return path[i-1]+d*(s/Mathf.Max(.0001f,leg));
                s-=leg;
            }
            end=true;return path.Length>0?path[path.Length-1]:Vector3.zero;
        }
    }
}
