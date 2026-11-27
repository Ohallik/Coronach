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
    /// has nobody to turn back: the party just faces whoever's held for it, or the thing itself.
    /// <para>Round eight: the camera is fixed, high, and looks one way in a zone, so a talk could open with one of the two out of sight. Someone standing up the screen from the lead
    /// was hidden by him (Teo Marrow, a head shorter than Taren and 1.1 m behind him from the lens, nov25-whole s20/401), and someone down the screen from him, turned to face him,
    /// showed the camera his back (Old Senn, s19/156). Now, as a talk with someone standing there opens, if either of the two covers half the other's height on the screen and they
    /// stand less than <see cref="ClearAcross"/> apart across it, the lead first walks a pace across the screen (away from them, else past them) to where they stand clear, when there is
    /// such a place on his own floor with nothing on the way and no trigger he isn't already in (<see cref="Aside"/>); and the person turned to him turns no further from the lens
    /// than three-quarter, toward his side of the screen (<see cref="Toward"/>). At a talk where neither covers the other the lead doesn't move.</para></summary>
    public sealed class ConversationStage:MonoBehaviour
    {
        const float Beside=1.7f,TurnRate=360,Settled=.3f,WalkLimit=4,SpeakerReach=9,JogFrom=3;
        /// <summary>How far apart across the screen the lead and the person he talks to stand at the least, middle to middle (two bodies' width and a little); the shortest and the
        /// longest step that buys it; how much of one body's height the other may cover on the screen; and how long his step may take.</summary>
        const float ClearAcross=1,PaceLeast=.6f,PaceMost=1.2f,MostCovered=.5f,StepFor=1.5f;
        /// <summary>How far past profile toward the lens a speaker turned to the lead faces at the least, in degrees: three-quarter, never the back of the head.</summary>
        const float TowardTheLens=20;
        /// <summary>For a test's broken state only (TalkStagingTests sets it by reflection and puts it back): the lead never steps aside and the speaker turns straight to him,
        /// as before round eight. Nothing else sets it.</summary>
        static bool stagedAsBefore;
        Vector3 aside;bool stepping;float stepUntil;
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
            staging=true;returning=false;began=Time.frameCount;walked=0;nextPath=0;corners=null;hasMark=false;partner=null;stepping=false;
            foreach(var member in party.members)
                if(member!=null&&member!=hero&&member.character!=who&&member.Health.Alive&&member.TryGetComponent<PartnerBrain>(out var brain)&&brain.enabled)partner=member;
            // Her mark is beside where he will stand: after his step, if he takes one.
            var at=hero.transform.position;
            if(who!=null&&speaker!=null&&!stagedAsBefore&&Aside(out aside)){stepping=true;stepUntil=Time.unscaledTime+StepFor;at=aside;}
            if(partner!=null)hasMark=Mark(at,out mark);
        }
        // ---------- neither of the two hidden on the screen (round eight) ----------
        static Vector3 Right(Camera camera)
        {
            var right=Flat(camera!=null?camera.transform.right:Quaternion.Euler(0,SightlineAudit.Yaw,0)*Vector3.right);
            return right.sqrMagnitude>.01f?right.normalized:Vector3.right;
        }
        /// <summary>The top of a body as it is drawn: its meshes, not a name hung over it nor that name's tag.</summary>
        static float Top(Transform body)
        {
            float top=float.MinValue;
            foreach(var drawn in body.GetComponentsInChildren<Renderer>())
                if(drawn.enabled&&(drawn is SkinnedMeshRenderer||drawn is MeshRenderer)&&drawn.GetComponentInParent<TMPro.TMP_Text>(true)==null)top=Mathf.Max(top,drawn.bounds.max.y);
            return top>float.MinValue?Mathf.Clamp(top,body.position.y+.8f,body.position.y+2.4f):body.position.y+1.7f;
        }
        /// <summary>How much of one of the two the other covers on the screen, as a share of the covered one's height as drawn: the person talked to standing up the screen behind the
        /// lead, or the lead behind them. None when they stand <see cref="ClearAcross"/> or more apart across the screen. <paramref name="across"/> is where they stand from him along
        /// the screen's width, in metres.</summary>
        public static float Covered(Camera camera,Transform lead,Transform them,out float across)
        {
            var a=lead.position;var b=them.position;across=Vector3.Dot(Flat(b-a),Right(camera));
            if(camera==null||Mathf.Abs(across)>=ClearAcross)return 0;
            float Y(Vector3 p)=>camera.WorldToScreenPoint(p).y;
            float leadFeet=Y(a),leadHead=Y(new Vector3(a.x,Top(lead),a.z)),feet=Y(b),head=Y(new Vector3(b.x,Top(them),b.z));
            bool leadNearer=Vector3.Dot(Flat(b-a),Flat(camera.transform.forward))>0;
            return leadNearer?Mathf.Clamp01((leadHead-feet)/Mathf.Max(1,head-feet)):Mathf.Clamp01((head-leadFeet)/Mathf.Max(1,leadHead-leadFeet));
        }
        /// <summary>Where the lead steps so that neither he nor the person he talks to covers the other: across the screen, away from them by as much as it takes to stand
        /// <see cref="ClearAcross"/> apart, else past them, a pace of <see cref="PaceLeast"/> to <see cref="PaceMost"/>. Nowhere free: he stays where he is.</summary>
        bool Aside(out Vector3 place)
        {
            place=default;var camera=Camera.main;if(camera==null||GroundNavigation.Current==null)return false;
            if(Covered(camera,hero.transform,speaker.transform,out float across)<MostCovered)return false;
            var at=hero.transform.position;var right=Right(camera);float away=across>=0?-1:1;
            foreach(var (side,step) in new[]{(away,ClearAcross-Mathf.Abs(across)),(-away,ClearAcross+Mathf.Abs(across))})
                if(Mathf.Max(PaceLeast,step)<=PaceMost&&Free(at,at+right*side*Mathf.Max(PaceLeast,step),out place))return true;
            return false;
        }
        static readonly Collider[] Near=new Collider[32],Had=new Collider[32];
        /// <summary>A place he can step straight to: on the floor he stands on (no ledge up or down, the baked floor unbroken on the way, nothing solid at the waist on the way)
        /// and inside no trigger he isn't already in (a way out, a fight's box, a crossing).</summary>
        bool Free(Vector3 from,Vector3 to,out Vector3 floor)
        {
            floor=to;
            if(!NavMesh.SamplePosition(from,out var here,.6f,NavMesh.AllAreas)||!NavMesh.SamplePosition(to,out var there,.3f,NavMesh.AllAreas))return false;
            if(Mathf.Abs(there.position.y-here.position.y)>.3f||Flat(there.position-to).magnitude>.2f||NavMesh.Raycast(here.position,there.position,out _,NavMesh.AllAreas))return false;
            if(!CombatCover.Clear(from+Vector3.up*.9f,there.position+Vector3.up*.9f,.25f))return false;
            int had=Triggers(from,Had),has=Triggers(there.position,Near);
            for(int i=0;i<has;i++)if(System.Array.IndexOf(Had,Near[i],0,had)<0)return false;
            floor=there.position;return true;
        }
        int Triggers(Vector3 at,Collider[] into)
        {
            int count=Physics.OverlapCapsuleNonAlloc(at+Vector3.up*.4f,at+Vector3.up*1.5f,.4f,into,~0,QueryTriggerInteraction.Collide),kept=0;
            for(int i=0;i<count;i++)
            {
                var c=into[i];
                if(c.isTrigger&&!c.transform.IsChildOf(hero.transform)&&(partner==null||!c.transform.IsChildOf(partner.transform))&&(speaker==null||!c.transform.IsChildOf(speaker.transform)))into[kept++]=c;
            }
            return kept;
        }
        /// <summary>His step aside, at a walk; then he turns back to whoever he's talking to.</summary>
        void Step()
        {
            var to=Flat(aside-hero.transform.position);
            if(to.magnitude<=.06f||Time.unscaledTime>=stepUntil){stepping=false;hero.motor.Move(Vector2.zero,false,false);Face(hero,focus.position);return;}
            // A walk in any form: a tilt that walks in the natural form runs at three metres a second in a fighting one.
            float tilt=hero.TryGetComponent<FormController>(out var form)&&form.Current!=Lattice.Data.BodyForm.Natural?.2f:WalkTilt;
            hero.motor.Move(new Vector2(to.x,to.z).normalized*tilt,false,false);
        }
        /// <summary>Where the person talked to turns: to the lead, but never further from the lens than <see cref="TowardTheLens"/> past profile, toward his side of the screen.
        /// Turned straight to a lead up the screen from him, Old Senn showed the camera the back of his head (s19/156).</summary>
        static Vector3 Toward(Vector3 from,Vector3 lead)
        {
            var look=Flat(lead-from);var camera=Camera.main;if(stagedAsBefore||look.sqrMagnitude<.01f)return lead;
            var ahead=Flat(camera!=null?camera.transform.forward:Quaternion.Euler(0,SightlineAudit.Yaw,0)*Vector3.forward).normalized;var right=Right(camera);
            float most=-Mathf.Sin(TowardTheLens*Mathf.Deg2Rad);
            if(Vector3.Dot(look.normalized,ahead)<=most)return lead;
            float side=Vector3.Dot(look,right)>=0?1:-1;
            return from+(right*side*Mathf.Cos(TowardTheLens*Mathf.Deg2Rad)+ahead*most)*look.magnitude;
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
        bool Mark(Vector3 at,out Vector3 spot)
        {
            spot=default;
            var view=Quaternion.Euler(0,SightlineAudit.Yaw,0);Vector3 across=view*Vector3.right,up=view*Vector3.forward;
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
            if(stepping)Step();else Face(hero,focus.position);
            if(speaker!=null&&Holds(speaker))Turn(speaker.transform,Toward(speaker.transform.position,hero.transform.position));
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
