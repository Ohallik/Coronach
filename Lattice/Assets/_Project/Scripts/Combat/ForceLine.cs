using System;
using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A line force runs down (chapter 7): a severed route fibre where it lies along the Skyfields' or the Glass Choir's floor, one of the engine hall's twelve that the Last Warden plucks,
    /// a light-rail in the Orrery. It is the strip of floor (or of the flight plane) under the fibre, from this object's place to <see cref="end"/>, <see cref="width"/> across: what a
    /// rider's shock and the Warden's strikes mark and land on. A builder stands one wherever such a line is (a fibre strung overhead gets its line on the floor beneath it); a body that
    /// needs one where none is near makes its own (<see cref="Make"/>), so it fights anywhere. One held by a clamp (<see cref="clamp"/>) goes slack when that clamp opens and carries
    /// nothing from then on. It draws nothing itself: what draws the fibre (BridleHold, a builder's strand) may read <see cref="Glow"/> and <see cref="Slack"/>.
    /// </summary>
    public sealed class ForceLine:MonoBehaviour
    {
        public static readonly List<ForceLine> All=new();
        /// <summary>A line has gone slack (its clamp opened).</summary>
        public static event Action<ForceLine> WentSlack;
        /// <summary>Its far end, in the world: this object's position is its near end (the engine's end, in the hall).</summary>
        public Vector3 end;
        /// <summary>The index of the <see cref="BridleClamp"/> that holds it, or 0 for none.</summary>
        public int clamp;
        /// <summary>How wide a strip of floor a strike down it covers, in metres.</summary>
        public float width=1.8f;
        bool slack;float litUntil=-1,litFor=1;
        /// <summary>Made by a body that needed one, not stood by a builder: it is that body's, and nothing else takes it for the room's.</summary>
        public bool Made{get;private set;}
        /// <summary>Carries nothing any more: its clamp has opened.</summary>
        public bool Slack=>slack;
        /// <summary>It can carry a strike: standing, and not slack.</summary>
        public bool Live=>!slack&&isActiveAndEnabled;
        /// <summary>How bright it is for the strike being told down it, 0 to 1: a tenth the moment it is lit, rising through the tell, gone as it lands. (From nought, a line lit this frame
        /// read as unlit to anything that asked in the same frame.)</summary>
        public float Glow=>Time.time<litUntil?Mathf.Max(.1f,Mathf.Clamp01(1-(litUntil-Time.time)/Mathf.Max(.05f,litFor))):0;
        public Vector3 From=>transform.position;
        public Vector3 To=>end;
        /// <summary>Its way along the floor from its near end, and its length there.</summary>
        public Vector3 Direction{get{var d=end-transform.position;d.y=0;return d.sqrMagnitude>.0001f?d.normalized:transform.forward;}}
        public float Length{get{var d=end-transform.position;d.y=0;return d.magnitude;}}
        /// <summary>How far along it a point lies, from its near end (may be under nought or past its length).</summary>
        public float Along(Vector3 point){var o=point-transform.position;o.y=0;return Vector3.Dot(o,Direction);}
        /// <summary>The point of it nearest a point, on the floor at its own height.</summary>
        public Vector3 Closest(Vector3 point)=>Vector3.Lerp(From,To,Length>.01f?Mathf.Clamp01(Along(point)/Length):0);
        /// <summary>How far a point is from it across the floor.</summary>
        public float Distance(Vector3 point){var o=point-Closest(point);o.y=0;return o.magnitude;}
        /// <summary>Whether a point stands on its strip, a body's allowance and all.</summary>
        public bool Under(Vector3 point)=>Distance(point)<=width*.5f+Telegraph.BodyAllowance;
        /// <summary>Its bearing seen from a point, in degrees from north: the order a fan of lines round the engine goes in.</summary>
        public float BearingFrom(Vector3 point){var o=Vector3.Lerp(From,To,.5f)-point;o.y=0;return Mathf.Repeat(Mathf.Atan2(o.x,o.z)*Mathf.Rad2Deg,360);}
        /// <summary>Lit for a strike told down it: it brightens over <paramref name="seconds"/>.</summary>
        public void Light(float seconds){litFor=Mathf.Max(.05f,seconds);litUntil=Time.time+litFor;}
        public void Dim()=>litUntil=-1;
        /// <summary>It lets go: nothing runs down it again.</summary>
        public void GoSlack(){if(slack)return;slack=true;litUntil=-1;Debug.Log("FORCE_LINE_SLACK "+name);WentSlack?.Invoke(this);}
        /// <summary>The live line nearest a point within a distance, or none. <paramref name="room"/> leaves out the lines bodies made for themselves.</summary>
        public static ForceLine Nearest(Vector3 point,float within,bool room=false,ForceLine not=null,Func<ForceLine,bool> where=null)
        {
            ForceLine best=null;float least=within;
            foreach(var line in All)
            {
                if(line==null||!line.Live||line==not||room&&line.Made||where!=null&&!where(line))continue;
                float d=line.Distance(point);if(d<least){least=d;best=line;}
            }
            return best;
        }
        /// <summary>A line a body makes for itself where the room has none near it: from one point to another, on the floor (or the flight plane) at the first point's height.</summary>
        public static ForceLine Make(string name,Vector3 from,Vector3 to,float width,Transform keeper=null)
        {
            var go=new GameObject(name);go.transform.position=from;if(keeper!=null)go.transform.SetParent(keeper,true);
            var line=go.AddComponent<ForceLine>();to.y=from.y;line.end=to;line.width=width;line.Made=true;return line;
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
    }
}
