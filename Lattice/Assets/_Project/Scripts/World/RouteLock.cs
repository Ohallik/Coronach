using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The Compact route lock across the Picket of Teeth's clean lane (chapter 8), taken apart piece by piece in Idra's order: port collar, starboard collar, keel collar, keeper bar. Each
    /// piece's hand is a <see cref="DiscoveryPoint"/> beside it that sets its flag (<see cref="flags"/>, each offered only once the one before is off); this is the lock. As a piece's flag goes
    /// from unset to set it swings off the frame and drifts away tumbling, the lead says it's off (the <c>lock</c> bark, by its place in the count), and a beat later Idra calls the next
    /// (<c>hail.lock</c>, on the radio). Her first call comes once the party is at the lock (<see cref="startFlag"/>). The first piece off trips the lock's own alarm: the picket comes up to at
    /// least its hunter wing. With the keeper bar out, the rest of the frame swings off the lane and <see cref="blockers"/> go: the lane is open. A save loads the lock as its flags say, with
    /// nothing played. Logs <c>LOCK_PIECE n</c>, <c>LOCK_OPEN</c>.
    /// </summary>
    public sealed class RouteLock:MonoBehaviour
    {
        public string startFlag="vask.picketCrossed";
        public string[] flags={"vask.lock1","vask.lock2","vask.lock3","vask.lock4"};
        /// <summary>The drawn pieces, in the order they come off, and what is left of the frame.</summary>
        public Transform[] pieces=System.Array.Empty<Transform>();public Transform frame;
        /// <summary>What shuts the lane until the keeper bar is out.</summary>
        public GameObject blockers;
        /// <summary>Who calls each piece, and the seconds between a piece coming off and the call for the next.</summary>
        public string caller="Idra";public float callGap=2.2f;
        public int Off{get;private set;}
        public bool Open=>Off>=flags.Length;
        bool[] seen;float callAt=-1;int callNext=-1;bool started;
        readonly System.Collections.Generic.List<(Transform piece,Vector3 drift,Vector3 spin,float from)> drifting=new();
        void Start()
        {
            var flagsNow=GameServices.Current!=null?GameServices.Current.Flags:null;seen=new bool[flags.Length];
            for(int i=0;i<flags.Length;i++)
            {
                seen[i]=flagsNow!=null&&flagsNow.GetBool(flags[i]);
                if(seen[i]){Off++;if(i<pieces.Length&&pieces[i]!=null)pieces[i].gameObject.SetActive(false);}
            }
            if(Open)OpenLane(false);
            started=flagsNow!=null&&flagsNow.GetBool(startFlag);
            // A save made at the lock with pieces still to come: Idra calls the next as the zone settles.
            if(started&&!Open){callNext=Off;callAt=Time.time+1.6f;}
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;var flagsNow=services.Flags;
            if(!started&&flagsNow.GetBool(startFlag)){started=true;if(!Open){callNext=Off;callAt=Time.time+1.2f;}}
            for(int i=0;i<flags.Length;i++)
            {
                if(seen[i]||!flagsNow.GetBool(flags[i]))continue;
                seen[i]=true;Off++;Debug.Log("LOCK_PIECE "+(i+1));
                if(i<pieces.Length&&pieces[i]!=null)Drift(pieces[i],i);
                var party=PartyController.Current;string lead=party!=null&&party.Active!=null?party.Active.character:"Taren";
                BarkService.PlayAt(lead,"lock",i,true);
                AudioManager.PlayAt("impactMetal_000",null,transform.position,.5f,AudioBus.SFX,50);
                // Tampering trips the lock's own alarm: whatever the picket had heard, the hunters come.
                if(i==0&&PicketAlarm.Current!=null)PicketAlarm.Current.Raise(PicketAlarm.Hunters,"lock tampered");
                if(Open)OpenLane(true);else{callNext=Off;callAt=Time.time+callGap;}
            }
            if(callNext>=0&&Time.time>=callAt&&!services.Input.Blocked){BarkService.PlayAt(caller,"hail.lock",callNext,true);callNext=-1;}
            for(int i=drifting.Count-1;i>=0;i--)
            {
                var (piece,drift,spin,from)=drifting[i];if(piece==null){drifting.RemoveAt(i);continue;}
                float age=Time.time-from;
                piece.position+=drift*Time.deltaTime*Mathf.Lerp(1,.4f,age/4);piece.Rotate(spin*Time.deltaTime,Space.World);
                if(age>4.5f){piece.gameObject.SetActive(false);drifting.RemoveAt(i);}
            }
        }
        void Drift(Transform piece,int index)
        {
            // Off the lane's side it was on, and down: a collar swings out on its anchor rock's side, the keel drops, the bar goes forward and away.
            var side=piece.position-transform.position;side.y=0;if(side.sqrMagnitude<.5f)side=index==2?Vector3.down*3:Vector3.forward*2;
            var drift=side.normalized*3.2f+Vector3.down*1.4f;var spin=new Vector3(index%2==0?40:-35,25+index*9,index==3?60:-20);
            drifting.Add((piece,drift,spin,Time.time));
        }
        void OpenLane(bool inPlay)
        {
            if(blockers!=null)blockers.SetActive(false);
            if(frame!=null){if(inPlay)drifting.Add((frame,new Vector3(-3.5f,1.6f,2.2f),new Vector3(14,30,-22),Time.time));else frame.gameObject.SetActive(false);}
            callNext=-1;
            if(inPlay){Debug.Log("LOCK_OPEN");AudioManager.PlayAt("forceField_000",null,transform.position,.5f,AudioBus.SFX,50);}
        }
    }
}
