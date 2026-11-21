using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Something drawn that goes the passage's way with the party (chapter 10, the Returning Passage, docs/maps/ReturningPassage.md): the young Choir swimming alongside, Mother of Doors
    /// far off and deep, going the same way, Idra's cutter holding the convoy's flank. Either it keeps station by the lead (<see cref="ahead"/> metres along the passage's line past
    /// him, <see cref="side"/> to starboard of the line, at <see cref="depth"/>), never faster than <see cref="speed"/>; or, with a <see cref="pace"/>, it travels the line from where it
    /// was laid at that pace and stops at <see cref="stopAt"/>. Only from <see cref="from"/> (empty: from the start); from <see cref="release"/> it leaves for
    /// <see cref="releaseTo"/> at <see cref="releaseSpeed"/> and stays there, as it does in a zone opened with that flag set.
    /// <para>Drawn only, under the plane: it never touches a hero. Paused, it waits; while a conversation holds the controls it holds where it is (D275). <see cref="heldByTalk"/> is
    /// only ever turned off by the test that shows the old way failing first.</para>
    /// </summary>
    public sealed class Alongside:MonoBehaviour
    {
        public Vector3[] path=System.Array.Empty<Vector3>();
        public float ahead=10,side=14,depth=-6,speed=18;
        /// <summary>Above nought: it travels the line at this pace from where it lies, to <see cref="stopAt"/> metres along it.</summary>
        public float pace,stopAt=float.PositiveInfinity;
        public string from="",release="";public Vector3 releaseTo;public float releaseSpeed=12;
        /// <summary>A slow rise and fall about its station (metres), and how long one takes: a creature swimming, not a thing on a rail.</summary>
        public float sway,swayPeriod=7;
        public bool heldByTalk=true;
        float travelled=float.NaN,phase;bool releasedSaid;
        public bool Held=>heldByTalk&&GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        static bool Set(string flag)=>!string.IsNullOrEmpty(flag)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(flag);
        void Start()
        {
            phase=transform.position.x*.13f+transform.position.z*.07f;
            if(Set(release)){transform.position=releaseTo;releasedSaid=true;}
        }
        void Update()
        {
            if(GameTime.Paused||Held)return;
            float dt=Time.deltaTime;
            if(!string.IsNullOrEmpty(from)&&!Set(from))return;
            if(Set(release))
            {
                if(!releasedSaid){releasedSaid=true;Debug.Log("ALONGSIDE_GOES "+name);}
                transform.position=Vector3.MoveTowards(transform.position,releaseTo,releaseSpeed*dt);
                Face(releaseTo-transform.position,dt);return;
            }
            Vector3 target;Vector3 way;
            if(pace>0)
            {
                if(float.IsNaN(travelled))travelled=ConvoyFlow.Along(path,transform.position);
                travelled=Mathf.Min(stopAt,travelled+pace*dt);
                target=ConvoyFlow.At(path,travelled,side,out way);
            }
            else
            {
                var party=PartyController.Current;var lead=party!=null?party.Active:null;if(lead==null)return;
                float s=ConvoyFlow.Along(path,lead.transform.position)+ahead;
                target=ConvoyFlow.At(path,s,side,out way);
            }
            phase+=dt*Mathf.PI*2/Mathf.Max(1,swayPeriod);
            target.y=depth+sway*Mathf.Sin(phase);
            transform.position=Vector3.MoveTowards(transform.position,target,speed*dt);
            Face(way,dt);
        }
        void Face(Vector3 way,float dt)
        {
            way.y=0;if(way.sqrMagnitude<.01f)return;
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(way),40*dt);
        }
    }
}
