using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.UI
{
    /// <summary>A conversation with someone standing there is a scene: the partner comes over and stands beside the hero, and all three look at each other. She used to be left across
    /// the room with her back to the speaker while saying her own lines. Nobody is moved through anything: she walks the baked navigation for a few seconds at most, a speaker still on
    /// a scripted walk keeps to it, and when the talk ends the speaker turns back to their post and she goes back to following. A talk that starts on its own at a discovery or a crossing
    /// has nobody to turn back: the party just faces whoever's held for it, or the thing itself.</summary>
    public sealed class ConversationStage:MonoBehaviour
    {
        const float Beside=1.7f,TurnRate=360,Settled=.3f,WalkLimit=4,SpeakerReach=9,JogFrom=3;
        /// <summary>How far apart she and the hero stand across the screen: a body's width. And how far she keeps across the screen from the speaker, should the free side be theirs.</summary>
        const float ClearOfHero=1.15f,ClearOfSpeaker=.9f,HeldReach=26;
        /// <summary>Across the screen and up it, from him, in the order she tries them. The later ones are for a narrow place: a terrace's edge or a wall on the free side.</summary>
        static readonly (float across,float up)[] Marks={(Beside,.3f),(Beside+.7f,.3f),(1.3f,1.4f),(1.3f,-1)};
        const float WalkTilt=.45f;
        NavMeshPath path;Vector3[] corners;int corner;float nextPath,walked;
        Npc speaker;Quaternion rest;bool staging,returning;Transform focus;
        CombatActor hero,partner;Vector3 mark;bool hasMark;int began;
        void Awake()=>path=new NavMeshPath();
        void OnEnable(){Npc.TalkRequested+=Begin;DiscoveryPoint.DialogueRequested+=Discovered;}
        void OnDisable()
        {
            Npc.TalkRequested-=Begin;DiscoveryPoint.DialogueRequested-=Discovered;
            if(speaker!=null&&Holds(speaker)&&(staging||returning))speaker.transform.rotation=rest;
            staging=returning=false;
        }
        /// <summary>Anyone not walking right now. Having a scripted walk isn't enough to skip them: Mira keeps hers, finished, for the rest of the game, at the first shop.</summary>
        static bool Holds(Npc person)=>!person.TryGetComponent<ScriptedWalk>(out var walk)||walk.Phase!=ScriptedWalk.Stage.Walking&&walk.Phase!=ScriptedWalk.Stage.Exiting;
        void Begin(string node,string who,System.Action<bool> done)
        {
            var party=PartyController.Current;if(party==null||party.Active==null||party.Active.flight)return;
            var lead=party.Active;var person=Find(who,lead.transform.position);if(person==null)return;
            if(speaker!=null&&speaker!=person&&Holds(speaker)&&returning)speaker.transform.rotation=rest;
            if(speaker!=person||!returning&&!staging){rest=person.transform.rotation;}
            speaker=person;Open(party,person.transform,who);
        }
        /// <summary>A talk that starts on its own: a discovery, or a crossing with someone held for it. It left her wherever she'd got to: off screen under the box through Hessa's opening, or
        /// across the hall while saying her own line about the cut housings. So she can be the one speaking, and still comes over.</summary>
        void Discovered(DiscoveryPoint point)
        {
            var party=PartyController.Current;if(point==null||party==null||party.Active==null||party.Active.flight)return;
            if(speaker!=null&&Holds(speaker)&&returning)speaker.transform.rotation=rest;
            speaker=null;Open(party,Held(point.dialogueSpeaker,party.Active.transform.position)??point.transform,null);
        }
        void Open(PartyController party,Transform look,string who)
        {
            hero=party.Active;focus=look;
            staging=true;returning=false;began=Time.frameCount;walked=0;nextPath=0;corners=null;hasMark=false;partner=null;
            foreach(var member in party.members)
                if(member!=null&&member!=hero&&member.character!=who&&member.Health.Alive&&member.TryGetComponent<PartnerBrain>(out var brain)&&brain.enabled)partner=member;
            if(partner!=null)hasMark=Mark(out mark);
        }
        /// <summary>A boss standing there for her own confrontation, by her id (Hessa, HessaKorl).</summary>
        static Transform Held(string who,Vector3 near)
        {
            if(string.IsNullOrEmpty(who))return null;
            Transform best=null;float closest=HeldReach*HeldReach;
            foreach(var health in Health.All)
            {
                if(health==null||health.friendly||health.id==null||!health.id.StartsWith(who))continue;
                float gap=(health.transform.position-near).sqrMagnitude;if(gap<closest){closest=gap;best=health.transform;}
            }
            return best;
        }
        /// <summary>The nearest person of that name in reach of the hero: a recorded message or a speaker off on the radio has nobody standing there.</summary>
        static Npc Find(string who,Vector3 near)
        {
            Npc best=null;float closest=SpeakerReach*SpeakerReach;
            foreach(var prompt in InteractionPrompt.Active)
            {
                if(prompt is not Npc npc||npc.speaker!=who)continue;
                float gap=(npc.transform.position-near).sqrMagnitude;if(gap<closest){closest=gap;best=npc;}
            }
            return best;
        }
        /// <summary>Beside the hero on the screen, not beside him as he faces: a mark at his side when he faces east lies straight behind him from this camera, one figure with two heads.
        /// So it's measured along the camera's own right, a body's width off, on the side away from whoever he's talking to, a little up the screen so she's above the dialogue box, and on floor she can reach.</summary>
        bool Mark(out Vector3 spot)
        {
            spot=default;
            var view=Quaternion.Euler(0,SightlineAudit.Yaw,0);Vector3 across=view*Vector3.right,up=view*Vector3.forward,at=hero.transform.position;
            float free=Vector3.Dot(focus.position-at,across)>=0?-1:1;
            foreach(float side in new[]{free,-free})
                foreach(var (reach,lift) in Marks)
                {
                    if(!NavMesh.SamplePosition(at+across*side*reach+up*lift,out var hit,.8f,NavMesh.AllAreas))continue;
                    if(Mathf.Abs(Vector3.Dot(hit.position-at,across))<ClearOfHero)continue;
                    if(Mathf.Abs(Vector3.Dot(hit.position-focus.position,across))<ClearOfSpeaker&&Flat(hit.position-focus.position).magnitude<4)continue;
                    spot=hit.position;return true;
                }
            return false;
        }
        void Update()
        {
            if(returning)
            {
                if(speaker==null||!Holds(speaker)){returning=false;return;}
                speaker.transform.rotation=Quaternion.RotateTowards(speaker.transform.rotation,rest,TurnRate*Time.unscaledDeltaTime);
                if(Quaternion.Angle(speaker.transform.rotation,rest)<.5f){speaker.transform.rotation=rest;returning=false;}
                return;
            }
            if(!staging)return;
            var dialogue=DialogueSystem.Current;
            if(focus==null||hero==null||Time.frameCount>began&&(dialogue==null||!dialogue.Running)){staging=false;returning=speaker!=null;return;}
            if(GameTime.Paused)return;
            Face(hero,focus.position);
            if(speaker!=null&&Holds(speaker))Turn(speaker.transform,hero.transform.position);
            if(partner!=null&&partner.Health.Alive&&!partner.Recovering)Stage();
        }
        void Stage()
        {
            var at=partner.transform.position;var toMark=Flat(mark-at);
            if(!hasMark||toMark.magnitude<=Settled||walked>WalkLimit){partner.motor.Move(Vector2.zero,false,false);Face(partner,focus.position);return;}
            walked+=Time.unscaledDeltaTime;
            var step=toMark;var nav=GroundNavigation.Current;
            if(nav!=null)
            {
                if(Time.unscaledTime>=nextPath)
                {
                    nextPath=Time.unscaledTime+.3f;
                    corners=nav.FindPath(at,mark,hero.transform.position,path)?path.corners:null;corner=1;
                }
                if(corners==null||corners.Length<2){partner.motor.Move(Vector2.zero,false,false);Face(partner,focus.position);return;}
                while(corner<corners.Length-1&&Flat(corners[corner]-at).magnitude<.35f)corner++;
                step=Flat(corners[corner]-at);
            }
            partner.motor.Move(new Vector2(step.x,step.z).normalized*(toMark.magnitude>JogFrom?1:WalkTilt),false,false);
        }
        static void Face(CombatActor who,Vector3 toward)
        {
            Turn(who.transform,toward);
            if(who.TryGetComponent<GroundMotor>(out var ground))ground.KeepFacing(who.transform.forward);
        }
        static void Turn(Transform who,Vector3 toward)
        {
            var look=Flat(toward-who.position);if(look.sqrMagnitude<.01f)return;
            who.rotation=Quaternion.RotateTowards(who.rotation,Quaternion.LookRotation(look),TurnRate*Time.unscaledDeltaTime);
        }
        static Vector3 Flat(Vector3 value){value.y=0;return value;}
    }
}
