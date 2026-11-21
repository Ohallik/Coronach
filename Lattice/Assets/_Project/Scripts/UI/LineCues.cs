using System;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;

namespace Lattice.UI
{
    /// <summary>
    /// Who does what on which line of one conversation, in the room where it is said (chapter 9's town meeting and Hal's last machine). A town deciding is people turning
    /// to whoever has the floor, getting to their feet, putting a hand up; a machine taken apart is its pieces coming off in the order its owner says them. A conversation
    /// can't move a body (the camera is fixed, the speaker turns to the hero and nothing else does), so this watches the running node (<see cref="DialogueSystem.Node"/>) and the
    /// lines shown since it began, and plays each cue once as its line comes up. What a cue leaves stays where it was left. Once <see cref="doneFlag"/> is set a load puts
    /// everything straight where the last cue left it (the bench in its nine pieces), with nothing played. Logs <c>LINE_CUE &lt;node&gt; &lt;line&gt; &lt;act&gt; &lt;who&gt;</c>.
    /// </summary>
    public sealed class LineCues:MonoBehaviour
    {
        public enum Act{Face,Pose,Move,Show,Hide,Hold}
        [Serializable] public sealed class Cue
        {
            /// <summary>The line it comes on, counted from 1 (the node's first line).</summary>
            public int line=1;
            public Act act;
            /// <summary>Who or what it acts on: the body that turns, poses or moves; the thing shown, hidden or held.</summary>
            public Transform who;
            /// <summary>Face: what it turns toward. Hold: where the thing is put down once the talk is over (null: hidden).</summary>
            public Transform toward;
            /// <summary>Face: a hero by id instead of a transform ("Taren"); Hold: the hero whose right hand holds it.</summary>
            public string hero;
            public string pose;
            /// <summary>Move: where it ends, in the world, and how it is turned there (euler).</summary>
            public Vector3 to,turn;
            public float seconds=.8f,lift;
            /// <summary>How long after its line it starts (Move): pieces coming off one after another on the same line.</summary>
            public float delay;
            /// <summary>Face and Pose act on every direct child of <see cref="who"/> (a room turning to whoever has the floor).</summary>
            public bool group;
        }
        public string node,doneFlag;
        public Cue[] cues=Array.Empty<Cue>();
        int played=-1;bool running;
        readonly System.Collections.Generic.List<(Transform who,Vector3 from,Quaternion fromTurn,Vector3 to,Quaternion toTurn,float start,float seconds,float lift)> moving=new();
        readonly System.Collections.Generic.List<(Transform who,Transform toward,string hero)> facing=new();
        readonly System.Collections.Generic.List<(Transform thing,Transform rest)> held=new();

        void Start()
        {
            if(string.IsNullOrEmpty(doneFlag)||GameServices.Current==null||!GameServices.Current.Flags.GetBool(doneFlag))return;
            foreach(var cue in cues)Settle(cue);
            Debug.Log("LINE_CUES_SETTLED "+node);
        }
        /// <summary>A cue's last state, at once: the room as the talk left it.</summary>
        static void Settle(Cue cue)
        {
            if(cue.who==null)return;
            switch(cue.act)
            {
                case Act.Move:cue.who.SetPositionAndRotation(cue.to,Quaternion.Euler(cue.turn));break;
                case Act.Show:cue.who.gameObject.SetActive(true);break;
                case Act.Hide:cue.who.gameObject.SetActive(false);break;
                case Act.Pose:foreach(var body in Bodies(cue))if(body.TryGetComponent<Posed>(out var posed))posed.Show(cue.pose,true);break;
                case Act.Face:if(cue.toward!=null)foreach(var body in Bodies(cue))Turn(body,cue.toward.position,1);break;
                case Act.Hold:if(string.IsNullOrEmpty(cue.hero)&&cue.toward!=null)Grip(cue.who,Hand(cue.toward));break;
            }
        }
        static System.Collections.Generic.IEnumerable<Transform> Bodies(Cue cue)
        {
            if(!cue.group){yield return cue.who;yield break;}
            foreach(Transform child in cue.who)yield return child;
        }
        static void Grip(Transform thing,Transform hand){if(hand==null)return;thing.gameObject.SetActive(true);thing.SetParent(hand,false);thing.localPosition=Vector3.zero;thing.localRotation=Quaternion.identity;}
        static void Turn(Transform who,Vector3 toward,float share)
        {
            var look=toward-who.position;look.y=0;if(look.sqrMagnitude<.01f)return;
            who.rotation=Quaternion.Slerp(who.rotation,Quaternion.LookRotation(look),share);
        }
        static Transform Hero(string id)
        {
            var party=PartyController.Current;if(party==null)return null;
            foreach(var member in party.members)if(member!=null&&member.character==id)return member.transform;
            return null;
        }
        static Transform Hand(Transform hero)
        {
            var rig=hero!=null?hero.GetComponentInChildren<Animator>():null;
            return rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.RightHand)??hero:hero;
        }
        void Update()
        {
            var dialogue=DialogueSystem.Current;
            bool now=dialogue!=null&&dialogue.Running&&dialogue.Node==node;
            if(now&&!running){running=true;played=0;moving.Clear();facing.Clear();}
            if(!now&&running){running=false;Release();}
            if(running)
            {
                int shown=dialogue.LinesPresented-dialogue.NodeStartLines;
                foreach(var cue in cues)if(cue.line>played&&cue.line<=shown)Play(cue);
                played=Mathf.Max(played,shown);
            }
            for(int i=moving.Count-1;i>=0;i--)
            {
                var m=moving[i];if(m.who==null){moving.RemoveAt(i);continue;}
                if(Time.unscaledTime<m.start)continue;
                float t=Mathf.Clamp01((Time.unscaledTime-m.start)/Mathf.Max(.05f,m.seconds)),eased=Mathf.SmoothStep(0,1,t);
                m.who.SetPositionAndRotation(Vector3.Lerp(m.from,m.to,eased)+Vector3.up*m.lift*Mathf.Sin(eased*Mathf.PI),Quaternion.Slerp(m.fromTurn,m.toTurn,eased));
                if(t>=1)moving.RemoveAt(i);
            }
        }
        /// <summary>After the speaker's own turn (ConversationStage turns whoever is talked to toward the hero in Update): a body this has turned stays turned.</summary>
        void LateUpdate()
        {
            foreach(var (who,toward,hero) in facing)
            {
                if(who==null)continue;var target=toward!=null?toward:Hero(hero);
                if(target!=null)Turn(who,target.position,Mathf.Clamp01(Time.unscaledDeltaTime*6));
            }
        }
        void Play(Cue cue)
        {
            if(cue.who==null)return;
            Debug.Log($"LINE_CUE {node} {cue.line} {cue.act} {cue.who.name}");
            switch(cue.act)
            {
                case Act.Face:foreach(var body in Bodies(cue)){facing.RemoveAll(f=>f.who==body);facing.Add((body,cue.toward,cue.hero));}break;
                case Act.Pose:foreach(var body in Bodies(cue))if(body.TryGetComponent<Posed>(out var posed))posed.Show(cue.pose);break;
                case Act.Move:facing.RemoveAll(f=>f.who==cue.who);moving.Add((cue.who,cue.who.position,cue.who.rotation,cue.to,Quaternion.Euler(cue.turn),Time.unscaledTime+cue.delay,cue.seconds,cue.lift));break;
                case Act.Show:cue.who.gameObject.SetActive(true);break;
                case Act.Hide:cue.who.gameObject.SetActive(false);break;
                case Act.Hold:
                    // A hero's hold lasts the talk and is put down after it; anyone else keeps what they are given (the bound copy, in Mira's hand from the second line on).
                    if(string.IsNullOrEmpty(cue.hero)){Grip(cue.who,Hand(cue.toward));break;}
                    // A hero resting has no body to hold it with: then it stands lit where it would be put down.
                    var hand=Hand(Hero(cue.hero));
                    if(hand==null){if(cue.toward!=null){cue.who.gameObject.SetActive(true);cue.who.SetPositionAndRotation(cue.toward.position,cue.toward.rotation);}break;}
                    Grip(cue.who,hand);held.Add((cue.who,cue.toward));break;
            }
        }
        /// <summary>The talk is over: what a hero held is put down where the cue says (or put away), and nobody is held turned any more.</summary>
        void Release()
        {
            facing.Clear();
            foreach(var (thing,rest) in held)
            {
                if(thing==null)continue;thing.SetParent(transform,true);
                if(rest!=null){thing.SetPositionAndRotation(rest.position,rest.rotation);}else thing.gameObject.SetActive(false);
            }
            held.Clear();
        }
    }
}
