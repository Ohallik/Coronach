using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A body held in a pose no clip of ours has: sitting on a bench or on what it owns, a hand up for a vote, getting to its feet. The rig's own Animator is off and the
    /// pose is muscles (Unity's human pose, so one sitting pose fits every humanoid body), measured by the builder from the body's own idle and saved with it, so an editor
    /// still shows the pose as play does. <see cref="Show"/> eases to another of its poses (LineCues calls it on a line: Oda stands on her first word, four hands go up).
    /// <para>A pose that also carries its bones' own turns (<see cref="Pose.turns"/>, for <see cref="bones"/>) is written as those turns: a human pose is held to the avatar's
    /// muscle limits, and a seat's thighs, raised level, came back half-raised, so in the player every seated body knelt through its bench with its feet under the deck (the
    /// November 20 capture's town meeting; the editor's stills drew the same, hidden behind the benches).</para>
    /// </summary>
    public sealed class Posed:MonoBehaviour
    {
        [System.Serializable] public sealed class Pose
        {
            public string name;public Vector3 body;public Quaternion turn=Quaternion.identity;public float[] muscles=System.Array.Empty<float>();
            /// <summary>Each of <see cref="bones"/>' local turn in this pose, and the first bone's (the hips') local place.</summary>
            public Quaternion[] turns=System.Array.Empty<Quaternion>();public Vector3 hips;
        }
        /// <summary>The bones a pose's <see cref="Pose.turns"/> are for, the hips first.</summary>
        public Transform[] bones=System.Array.Empty<Transform>();
        public Pose[] poses=System.Array.Empty<Pose>();
        /// <summary>The pose it holds as it loads.</summary>
        public string current;
        /// <summary>How long a change of pose takes.</summary>
        public float seconds=.7f;
        /// <summary>The height of what it sits on (0: it stands). A body sitting on a bench turns its shoulders to whoever speaks, never its seat: <see cref="WithinSeat"/>.</summary>
        public float seat;
        /// <summary>How far a seated body turns from the way its seat faces (degrees, either way).</summary>
        public const float SeatTurn=30;
        Animator rig;HumanPoseHandler handler;HumanPose pose;Pose from,to;float since=1;float seatYaw;bool seatKnown;
        public string Showing=>to!=null?to.name:current;
        /// <summary>Sitting now: it has a seat and is not standing up from it.</summary>
        public bool Seated=>seat>0&&Showing!="stand"&&Showing!="standhand";
        void Awake()
        {
            rig=GetComponentInChildren<Animator>(true);
            if(rig!=null&&rig.avatar!=null&&rig.avatar.isHuman)handler=new HumanPoseHandler(rig.avatar,rig.transform);
            if(rig!=null)rig.enabled=false;
            to=Find(current);from=to;since=seconds;
            seatYaw=transform.eulerAngles.y;seatKnown=true;
        }
        /// <summary>A turn toward <paramref name="want"/> as far as a seated body turns: within <see cref="SeatTurn"/> of the way its seat faces (as it was placed). Standing, the turn itself.</summary>
        public Quaternion WithinSeat(Quaternion want)
        {
            if(!Seated)return want;
            float faces=seatKnown?seatYaw:transform.eulerAngles.y;
            return Quaternion.Euler(0,faces+Mathf.Clamp(Mathf.DeltaAngle(faces,want.eulerAngles.y),-SeatTurn,SeatTurn),0);
        }
        void OnDestroy(){handler?.Dispose();handler=null;}
        Pose Find(string name){foreach(var p in poses)if(p!=null&&p.name==name)return p;return null;}
        public bool Has(string name)=>Find(name)!=null;
        /// <summary>Eases to the named pose (at once with <paramref name="now"/>). A name it doesn't have changes nothing.</summary>
        public void Show(string name,bool now=false)
        {
            var next=Find(name);if(next==null||next==to&&!now)return;
            from=now?next:Blend();to=next;since=now?seconds:0;current=name;
        }
        Pose Blend()
        {
            if(from==null||to==null||since>=seconds)return to;
            float t=Mathf.SmoothStep(0,1,since/Mathf.Max(.01f,seconds));var mixed=new Pose{name="",body=Vector3.Lerp(from.body,to.body,t),turn=Quaternion.Slerp(from.turn,to.turn,t),muscles=new float[to.muscles.Length]};
            for(int i=0;i<mixed.muscles.Length;i++)mixed.muscles[i]=Mathf.Lerp(i<from.muscles.Length?from.muscles[i]:0,to.muscles[i],t);
            if(to.turns.Length>0&&from.turns.Length==to.turns.Length)
            {
                mixed.turns=new Quaternion[to.turns.Length];for(int i=0;i<mixed.turns.Length;i++)mixed.turns[i]=Quaternion.Slerp(from.turns[i],to.turns[i],t);
                mixed.hips=Vector3.Lerp(from.hips,to.hips,t);
            }
            else{mixed.turns=to.turns;mixed.hips=to.hips;}
            return mixed;
        }
        /// <summary>Writes the pose it holds as it loads onto the rig at once, with nothing running: what a still of the scene taken outside play calls, so it shows the body as play does
        /// whatever else has touched the rig since the builder posed it.</summary>
        public void Hold()
        {
            var body=GetComponentInChildren<Animator>(true);var held=Find(current);
            if(held!=null&&Write(held))return;
            if(body==null||body.avatar==null||!body.avatar.isHuman||held==null||held.muscles.Length==0)return;
            using var once=new HumanPoseHandler(body.avatar,body.transform);
            var written=new HumanPose{bodyPosition=held.body,bodyRotation=held.turn,muscles=(float[])held.muscles.Clone()};
            once.SetHumanPose(ref written);
        }
        void LateUpdate()
        {
            if(to==null||(handler==null||to.muscles.Length==0)&&to.turns.Length==0)return;
            // With the rig's Animator off nothing moves a settled pose: it is written once more as it settles, and then left.
            if(since>=seconds+.1f)return;
            since+=Time.deltaTime;var shown=Blend();
            if(Write(shown))return;
            if(handler==null||shown.muscles.Length==0)return;
            if(pose.muscles==null||pose.muscles.Length!=shown.muscles.Length)pose.muscles=new float[shown.muscles.Length];
            System.Array.Copy(shown.muscles,pose.muscles,shown.muscles.Length);pose.bodyPosition=shown.body;pose.bodyRotation=shown.turn;
            handler.SetHumanPose(ref pose);
        }
        /// <summary>Writes a pose as its bones' own turns, if it carries them for these bones.</summary>
        bool Write(Pose shown)
        {
            if(shown.turns.Length==0||shown.turns.Length!=bones.Length)return false;
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null)bones[i].localRotation=shown.turns[i];
            if(bones.Length>0&&bones[0]!=null)bones[0].localPosition=shown.hips;
            return true;
        }
    }
}
