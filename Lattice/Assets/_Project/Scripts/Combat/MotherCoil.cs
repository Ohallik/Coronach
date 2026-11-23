using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Mother of Doors' body (chapter 10, SorrelSecondSky; docs/maps/SorrelSecondSky.md, "Boss arena contract"): her head is the boss's own body at the crater's middle, raised and facing
    /// the party, and this lays the rest of her round the nursery. Her neck runs back from her head and down to a coil of <see cref="BossController.MotherTune.Drums"/> drums
    /// (<c>EnemyDef.bodySegment</c>, the Cantor's kind grown old) and her tail (<c>tailSegment</c>), once round the nursery and a little over, <see cref="BossController.MotherTune.CoilDepth"/>
    /// under the plane: one creature, never a head beside a pile of parts. It is laid at load, dormant or awake (a dormant boss's own Start waits for its fight, so ActorFactory adds this
    /// beside her), and stays where it was laid however her root is turned or moved: every drum is placed in world space along the path each frame.
    /// <para>What her moves ask of it: <see cref="Heave"/> (the coil surges up toward the plane), <see cref="Lash"/> (the drums nearest a bearing whip out along it), <see cref="Straining"/>
    /// (from the third clamp open: the coil hauls round against the last clamp). It breathes slowly the rest of the time, and a held conversation holds it (D275: nothing of hers moves
    /// while the controls are held).</para>
    /// <para>Her release (<see cref="Release"/>, from her Kit's <c>down</c>): every clamp lets go at once, she lifts her head, turns, and goes up her road (<see cref="BossExit"/>
    /// <c>MotherOfDoors</c>) with her whole coil following the way her head went, over <see cref="BossController.MotherTune.ReleaseSeconds"/>. The view draws back to show her going, the
    /// fight's music goes out and nothing barks until <c>end.motherFreed</c> is set (<c>SurvivorMotherGoes</c>, the zone's after-talk) or <see cref="BossController.MotherTune.QuietAtMost"/>
    /// has passed. Logs <c>MOTHER_LAID drums=n</c>, <c>MOTHER_GOES</c>, <c>MOTHER_GONE</c>, <c>MOTHER_QUIET_ENDS</c>.</para>
    /// </summary>
    public sealed class MotherCoil:MonoBehaviour
    {
        Transform head;Quaternion headBase;
        readonly List<Transform> drums=new();
        float[] along;
        /// <summary>Her path from the root back along her body, in world space, and the distance along it to each point.</summary>
        readonly List<Vector3> path=new();readonly List<float> distance=new();
        Vector3 centre,forward,right;float bed,link=7.6f,tailLink=9;
        bool laid;
        public bool Laid=>laid;
        /// <summary>The middle of her coil (her root as she was laid), and its centreline's height.</summary>
        public Vector3 Centre=>centre;
        public float Bed=>bed;
        public IReadOnlyList<Transform> Drums=>drums;
        public Transform Head=>head;
        /// <summary>From the third clamp open until she goes: the coil hauls against the last clamp.</summary>
        public bool Straining;
        float heave,heaveTo,lashTo,lashAmount,strainClock,breathClock;Vector3 lashWay;
        /// <summary>The coil surges up toward the plane by this share of <see cref="BossController.MotherTune.HeaveRise"/> (her Heave), easing there.</summary>
        public void Heave(float share)=>heaveTo=Mathf.Clamp01(share);
        /// <summary>The drums on this bearing from her middle whip out along it, by this share of <see cref="BossController.MotherTune.LashReach"/> (her Lash).</summary>
        public void Lash(Vector3 way,float share){way.y=0;if(way.sqrMagnitude>.0001f)lashWay=way.normalized;lashTo=Mathf.Clamp01(share);}

        bool settled;
        void Awake()=>Settle();
        /// <summary>Her head is the body ActorFactory instanced (her root's first child): centred a little over the plane and lifted at the snout, so it looks over its own coil at whoever
        /// comes. Once.</summary>
        public void Settle()
        {
            if(settled)return;settled=true;
            head=transform.childCount>0?transform.GetChild(0):null;
            if(head!=null)
            {
                headBase=head.localRotation;head.localRotation=Quaternion.Euler(-BossController.MotherTune.HeadRaised,0,0)*headBase;
                SerpentSegments.CenterVisual(head.gameObject,BossController.MotherTune.HeadLift);
            }
        }
        void Start(){Lay();}
        /// <summary>Lays the coil round where her root stands now, behind the way she faces, from her definition's drums (her brain's, or the one given: an editor still has no brain).
        /// Once: after that the coil is where it was laid.</summary>
        public void Lay(EnemyDef given=null)
        {
            if(laid)return;laid=true;Settle();
            var brain=GetComponent<EnemyBrain>();var definition=given!=null?given:brain!=null?brain.definition:null;
            centre=transform.position;forward=transform.forward;forward.y=0;forward=forward.sqrMagnitude>.0001f?forward.normalized:Vector3.forward;right=Vector3.Cross(Vector3.up,forward);
            bed=centre.y-BossController.MotherTune.CoilDepth;
            // The drums first, so their own length sets how far apart they lie.
            int count=BossController.MotherTune.Drums;
            for(int i=0;i<=count;i++)
            {
                bool tail=i==count;var prefab=definition!=null?(tail?definition.tailSegment:definition.bodySegment):null;
                var go=new GameObject(tail?"Tail":"Drum_"+i);go.transform.SetParent(transform,false);
                var visual=prefab!=null?Instantiate(prefab,go.transform):ActorFactory.Visual("ART_PENDING_MotherDrum",PrimitiveType.Capsule,go.transform,new Vector3(6,4.5f,6),Vector3.zero,"Enemy");
                go.transform.rotation=Quaternion.identity;SerpentSegments.CenterVisual(visual,0);
                // A drum's length is its own (the intake's 9 m, the tail's 11): neighbours overlap by a seventh, so a bend never opens a gap between them.
                float length=Length(visual);if(length>.5f){if(tail)tailLink=length*.82f;else if(i==0)link=length*.85f;}
                drums.Add(go.transform);
            }
            BuildPath();
            along=new float[drums.Count];
            float back=BossController.MotherTune.HeadBack;
            for(int i=0;i<count;i++)along[i]=back+link*(i+.5f);
            along[count]=back+link*count+tailLink*.5f;
            Place(0);
            Debug.Log($"MOTHER_LAID drums={count} link={link:0.0} tail={tailLink:0.0} body={along[count]+tailLink*.5f:0}m");
        }
        static float Length(GameObject visual)
        {
            var renderers=visual.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return 0;
            var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);return bounds.size.z;
        }
        /// <summary>Her path as laid: from her root back through her head, down her neck to the coil's far side, then once round the nursery and a little over, drawing in as it goes so the
        /// tail lies inside her neck's join and never through it.</summary>
        void BuildPath()
        {
            path.Clear();distance.Clear();
            float lift=BossController.MotherTune.HeadLift,R=BossController.MotherTune.CoilRadius;
            var headBack=centre-forward*BossController.MotherTune.HeadBack+Vector3.up*(lift-.5f);
            Add(centre+Vector3.up*lift);
            for(float s=1;s<=BossController.MotherTune.HeadBack;s+=1)Add(Vector3.Lerp(centre+Vector3.up*lift,headBack,s/BossController.MotherTune.HeadBack));
            // The neck: down and back to the ring's far point, easing into it along the ring's own heading there.
            var ringStart=centre-forward*R;ringStart.y=bed;
            var control=headBack-forward*4+Vector3.down*1.2f;
            for(int k=1;k<=12;k++){float t=k/12f,u=1-t;Add(u*u*headBack+2*u*t*control+t*t*ringStart);}
            // Round her right side, past her front, round her left and on to where she began, drawing in by the spiral.
            for(float turn=4;turn<=400;turn+=4)
            {
                float a=turn*Mathf.Deg2Rad,r=R-BossController.MotherTune.Spiral*turn/360f;
                var at=centre+(-forward*Mathf.Cos(a)+right*Mathf.Sin(a))*r;at.y=bed;Add(at);
            }
            void Add(Vector3 point){distance.Add(path.Count==0?0:distance[path.Count-1]+Vector3.Distance(path[path.Count-1],point));path.Add(point);}
        }
        Vector3 PathAt(float s)
        {
            if(path.Count==0)return transform.position;
            if(s<=0)return path[0];
            int last=path.Count-1;if(s>=distance[last]){var tail=path[last]-path[last-1];return path[last]+tail.normalized*(s-distance[last]);}
            int lo=0,hi=last;while(hi-lo>1){int mid=(lo+hi)/2;if(distance[mid]<=s)lo=mid;else hi=mid;}
            float span=distance[hi]-distance[lo];return Vector3.Lerp(path[lo],path[hi],span>1e-4f?(s-distance[lo])/span:0);
        }
        /// <summary>Every drum on its place along the path, toward her head, with what her moves ask of the coil on top.</summary>
        void Place(float dt)
        {
            if(along==null)return;
            breathClock+=dt;if(Straining)strainClock+=dt;
            heave=Mathf.MoveTowards(heave,heaveTo,dt*1.6f);lashAmount=Mathf.MoveTowards(lashAmount,lashTo,dt*(lashTo>lashAmount?6:1.4f));
            for(int i=0;i<drums.Count;i++)
            {
                if(drums[i]==null)continue;
                float s=along[i]+(releasing?0:Straining?Mathf.Sin(strainClock*1.1f)*1.4f:0);
                var at=PathAt(s);var ahead=PathAt(s-1.2f);var behind=PathAt(s+1.2f);
                if(!releasing)
                {
                    // Breathing travels down her; heaving, the coil comes up toward the plane; lashing, the drums on that bearing whip out.
                    at.y+=Mathf.Sin(breathClock*.7f-i*.55f)*BossController.MotherTune.Breath+heave*BossController.MotherTune.HeaveRise*(.6f+.4f*Mathf.Sin(i*1.3f));
                    if(lashAmount>.001f)
                    {
                        var out_=at-centre;out_.y=0;float facing=out_.sqrMagnitude>.01f?Vector3.Dot(out_.normalized,lashWay):0;
                        if(facing>.75f)at+=lashWay*(lashAmount*BossController.MotherTune.LashReach*Mathf.InverseLerp(.75f,1,facing));
                    }
                    if(Straining)at+=Vector3.up*(.35f*Mathf.Sin(strainClock*7+i))*.4f;
                }
                var toward=ahead-behind;if(toward.sqrMagnitude<.0001f)toward=forward;
                drums[i].SetPositionAndRotation(at,Quaternion.LookRotation(toward.normalized));
            }
        }
        void LateUpdate()
        {
            if(!laid)return;
            // A pause or a conversation holds her body as it holds the fight's clocks (GameTime.CombatHeld): nothing of hers moves while the controls are held. Her going is the other
            // half of D275: it moves no hero and is the zone's after-talk's own staging (SurvivorMotherGoes opens as she goes), so only a pause holds it. Held with the controls, she
            // stood frozen half way up her road for the whole of Dace's line (QaDebug release stills, November 14).
            float dt=releasing?(GameTime.Paused?0:Time.deltaTime):GameTime.CombatHeld?0:Time.deltaTime;
            if(releasing)Release(dt);
            Place(dt);
        }

        // ---------------------------------------------------------------- her release
        bool releasing,gone,quiet,pastMouth;float age,quietAge;Vector3 from,turnCentre,exit,road;Quaternion facing;float travelled,speed;int beat;
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        CameraRig rig;
        public bool Releasing=>releasing;
        public bool Gone=>gone;
        /// <summary>She is let go: from here this owns her body until she is gone up her road. Begun once.</summary>
        public void Release()
        {
            if(releasing)return;releasing=true;age=0;beat=0;from=transform.position;facing=transform.rotation;travelled=0;speed=0;
            heaveTo=heave=0;lashTo=lashAmount=0;Straining=false;
            var way=BossExit.For(GetComponent<Health>().id);
            exit=way!=null?way.transform.position:centre-forward*BossController.MotherTune.RoadIfNone;exit.y=from.y;
            // Her road runs on the way it leaves her middle: through its mouth and away.
            road=Flat(exit-centre);road=road.sqrMagnitude>.01f?road.normalized:-forward;pastMouth=false;
            // She turns toward her own left, round the side away from her neck, and comes about heading for the road.
            var left=-right;turnCentre=from+left*BossController.MotherTune.TurnRadius;
            // The fight's music goes out and nothing barks over her going, until the zone's after-talk has been heard (SurvivorMotherGoes).
            MusicDirector.Encounter(this,null,true);BarkService.Hush(this,true);quiet=true;quietAge=0;
            var at=transform.position+Vector3.up*2;
            Tremor(1.4f,.14f);
            AudioManager.PlayAt("joint_release",null,centre,.46f,AudioBus.SFX,16);
            SkillRing.Expand(new Vector3(centre.x,centre.y,centre.z),BossController.MotherTune.CoilRadius+10,GroundRing.Friendly,1.6f,ZoneController.Current==null||ZoneController.Current.Flight);
            CombatVfx.Burst(at,GroundRing.Friendly,"kill");
            foreach(var drum in drums)if(drum!=null)CombatVfx.Burst(drum.position+Vector3.up*2,GroundRing.Friendly,"hit");
            Debug.Log("MOTHER_GOES");
        }
        void Release(float dt)
        {
            age+=dt;quietAge+=dt;
            // Her voice, then the weight of her moving, then the road taking her.
            if(beat==0&&age>=.3f){beat++;AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up*3,.42f,AudioBus.SFX,16,.2f,.55f);}
            if(beat==1&&age>=BossController.MotherTune.LiftSeconds){beat++;AudioManager.PlayAt("district_lift",null,transform.position,.42f,AudioBus.SFX,16);Tremor(.8f,.08f);}
            if(beat==2&&age>=BossController.MotherTune.LiftSeconds+BossController.MotherTune.TurnSeconds){beat++;AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up*3,.3f,AudioBus.SFX,30,.2f,.5f);}
            if(beat==3&&age>=BossController.MotherTune.ReleaseSeconds*.6f){beat++;AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up*3,.32f,AudioBus.SFX,30,.2f,.72f);}
            float lift=BossController.MotherTune.LiftSeconds,turn=BossController.MotherTune.TurnSeconds;
            Vector3 at;Quaternion look;
            if(age<lift)
            {
                // She lifts her head where she lies.
                float k=Mathf.SmoothStep(0,1,age/lift);at=from+Vector3.up*BossController.MotherTune.ReleaseRise*k;look=facing;
                if(head!=null)head.localRotation=Quaternion.Euler(-BossController.MotherTune.HeadRaised*(1+1.2f*k),0,0)*headBase;
            }
            else if(age<lift+turn)
            {
                // She comes about on a half circle to her left, heading for her road.
                float k=Mathf.SmoothStep(0,1,(age-lift)/turn),a=k*Mathf.PI;var left=-right;
                at=turnCentre+(-left*Mathf.Cos(a)+forward*Mathf.Sin(a))*BossController.MotherTune.TurnRadius+Vector3.up*BossController.MotherTune.ReleaseRise;
                var heading=left*Mathf.Sin(a)+forward*Mathf.Cos(a);heading.y=0;
                look=Quaternion.LookRotation(heading.sqrMagnitude>.001f?heading.normalized:forward);
            }
            else
            {
                // And goes: for the road's mouth, then straight on up the road, faster as she clears the crater.
                speed=Mathf.MoveTowards(speed,BossController.MotherTune.GoSpeed,dt*BossController.MotherTune.GoSpeed/2.5f);
                var here=transform.position;if(!pastMouth&&Flat(exit-here).magnitude<=4)pastMouth=true;
                var to=pastMouth?road:exit-here;to.y=0;var way=to.sqrMagnitude>.01f?to.normalized:road;
                at=here+way*speed*dt;look=Quaternion.LookRotation(Vector3.Slerp(transform.forward,way,Mathf.Clamp01(dt*2.5f)));travelled+=speed*dt;
            }
            transform.SetPositionAndRotation(at,look);
            // Where her head has been is where her body goes: the path grows from the front.
            if(path.Count==0||(at-path[0]).sqrMagnitude>.16f)Prepend(at+Vector3.up*BossController.MotherTune.HeadLift);
            Frame();
            // Gone once her tail is up the road past its mouth (out of the crater and the view), or at the latest by her release's time.
            var tail=drums.Count>0?drums[drums.Count-1]:null;
            bool up=tail!=null&&age>lift+turn&&Vector3.Dot(Flat(tail.position-exit),road)>0;
            if(!gone&&(up||age>=BossController.MotherTune.ReleaseSeconds))
            {
                gone=true;foreach(var renderer in GetComponentsInChildren<Renderer>())renderer.enabled=false;
                Debug.Log("MOTHER_GONE");
            }
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
            if(quiet&&(quietAge>=BossController.MotherTune.QuietAtMost||gone&&flags!=null&&flags.GetBool("end.motherFreed")&&!GameServices.Current.Input.Blocked))
            {
                quiet=false;MusicDirector.Encounter(this,null,false);BarkService.Hush(this,false);Debug.Log("MOTHER_QUIET_ENDS");
                if(gone)Destroy(gameObject);
            }
        }
        void Prepend(Vector3 point)
        {
            float step=Vector3.Distance(point,path[0]);path.Insert(0,point);distance.Insert(0,0);
            for(int i=1;i<distance.Count;i++)distance[i]+=step;
        }
        /// <summary>The view draws back to show her going, for as long as she is going: on a shelf the ground view frames the party and her head (rank 1, over an ordinary fight's framing) as
        /// far back as that takes, in flight the scene's interest. Held only a few seconds at the ground fight's own 36 m, she was off the top of the frame by five seconds and the rest of
        /// her going (and the young after her) happened where nobody could see it (QaDebug release stills, November 14).</summary>
        void Frame()
        {
            if(gone)return;
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();var party=PartyController.Current;if(rig==null||party==null||party.Active==null)return;
            var view=new Bounds(transform.position+Vector3.up*2,new Vector3(14,8,14));
            for(int i=0;i<Mathf.Min(3,drums.Count);i++)if(drums[i]!=null)view.Encapsulate(drums[i].position);
            // In flight the scene's interest is all the view holds, so the ship goes in it too: framed on her alone, a launch from the east clamp's works as she went left the flight
            // controls on screen and no ship anywhere for two stills, then an empty crater (whole-game capture, November 20, s63/162 and 163). On a shelf the ground framing keeps
            // the party by itself.
            if(party.Active.flight)
            {
                var lead=party.Active;view.Encapsulate(new Bounds(lead.transform.position+Vector3.up*.8f,Vector3.one*HeroCollision.HullRadius(lead.character)*2));
                rig.FrameSceneInterest(this,view,new Rect(.08f,.16f,.84f,.72f));
            }
            else rig.FrameGround(view,Rect.MinMaxRect(.03f,.13f,.97f,.93f),Rect.MinMaxRect(.14f,.24f,.86f,.84f),BossController.MotherTune.FramedFarthest,1);
        }
        static CameraRig shaker;
        static void Tremor(float seconds,float amplitude){if(shaker==null)shaker=FindFirstObjectByType<CameraRig>();if(shaker!=null)shaker.Tremor(seconds,amplitude);}
        void OnDestroy(){if(quiet){MusicDirector.Encounter(this,null,false);BarkService.Hush(this,false);}}
    }
}
