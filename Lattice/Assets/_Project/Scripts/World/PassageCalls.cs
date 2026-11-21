using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// What is said on the radio and by the heroes as the party flies the Returning Passage (chapter 10, docs/maps/ReturningPassage.md; manifest section 7): each call by its place along
    /// the passage's line, said once a load when the lead passes it the way it faces (north on the run and the tow, south flying back up the lane), only while its <see cref="needs"/>
    /// are set and its <see cref="unless"/> isn't, at least <see cref="Gap"/> seconds after the one before, never over a conversation. A pool said in turn (Pell's count, Ilo's names,
    /// Orrin's board) is said by the call's own place in it (<see cref="BarkService.PlayAt"/>), so a reload keeps the count in step. A call with no speaker is the lead's.
    /// </summary>
    public sealed class PassageCalls:MonoBehaviour
    {
        [System.Serializable] public struct Call{public string speaker,key;public int index;public float along;public bool back;public string needs,unless;}
        /// <summary>Long enough for a bark to be read and gone (BarkPanel holds one 3.5 s).</summary>
        public const float Gap=4.2f;
        public Vector3[] path=System.Array.Empty<Vector3>();
        public Call[] calls=System.Array.Empty<Call>();
        bool[] said=System.Array.Empty<bool>();float next;int queued=-1;
        static bool Set(string flag)=>string.IsNullOrEmpty(flag)||GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        static bool SetHere(string flag)=>!string.IsNullOrEmpty(flag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        void Start(){said=new bool[calls.Length];next=Time.unscaledTime+1.5f;}
        void Update()
        {
            var services=GameServices.Current;var party=PartyController.Current;var lead=party!=null?party.Active:null;
            if(services==null||lead==null||said.Length!=calls.Length)return;
            if(services.Input.Blocked){next=Mathf.Max(next,Time.unscaledTime+1.2f);return;}
            float s=ConvoyFlow.Along(path,lead.transform.position);
            // The first one due that hasn't been said, in the order they lie.
            if(queued<0)
                for(int i=0;i<calls.Length;i++)
                {
                    if(said[i])continue;var c=calls[i];
                    if(!Set(c.needs)||SetHere(c.unless)){continue;}
                    if(c.back?s<=c.along:s>=c.along){queued=i;break;}
                }
            if(queued<0||Time.unscaledTime<next)return;
            var call=calls[queued];said[queued]=true;queued=-1;
            // A call whose moment has gone by (passed far behind while another was being said) is dropped, not said late over the next one's place.
            if(call.back?s<call.along-160:s>call.along+160){Debug.Log("PASSAGE_CALL_PASSED "+call.key);return;}
            string who=string.IsNullOrEmpty(call.speaker)?lead.character:call.speaker;
            BarkService.PlayAt(who,call.key,call.index,true);next=Time.unscaledTime+Gap;
            Debug.Log("PASSAGE_CALL "+who+" "+call.key+" "+call.index);
        }
    }
}
