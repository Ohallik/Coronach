using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// One of Cinder Halo's separation joints (chapter 9, the Separation Works; docs/maps/SeparationWorks.md, "The six joints"): a collar where two of the vessels the station was built from
    /// meet, held shut by locking dogs, which Taren has tightened every year for eleven years. The hand is a <see cref="DiscoveryPoint"/> at the screw jack at its foot ("Take the joint's
    /// weight"), which sets <see cref="WorldSwitch.flag"/>; this is the joint answering, in the release's own two beats (Oriel's, on home's own iron). It <b>takes</b>: the jack's
    /// <see cref="wheel"/> turns, the collar groans and its <see cref="dogs"/> draw back one after another along their own forward axis, a knock each. Then it <b>gives</b>: the
    /// <see cref="collar"/> turns a few degrees about its own forward axis, the two hull <see cref="leaves"/> part and slide into the walls, dust falls from the seam, the collar's lamp goes
    /// from <see cref="shutLamps"/> to <see cref="openLamps"/>, and <see cref="blocks"/> (the way it held shut: a Membrane holder) goes. A save with the flag loads it open with nothing played.
    /// Standing at the jack, the view draws back over <see cref="shows"/>, as a pump's does over its water. Logs <c>JOINT_OPEN flag</c> (the press) and <c>JOINT_GIVEN flag</c> (at rest).
    /// </summary>
    public sealed class SeparationJoint:WorldSwitch
    {
        static readonly Rect FramedIn=new(.06f,.18f,.88f,.74f);
        protected override string Kind=>"JOINT_OPEN";
        /// <summary>The jack's hand wheel, turned about its own <see cref="wheelAxis"/> as the weight is taken.</summary>
        public Transform wheel;public Vector3 wheelAxis=Vector3.forward;public float wheelTurns=2;
        /// <summary>The locking dogs round the collar, each drawn back <see cref="dogTravel"/> along its own forward axis in turn.</summary>
        public Transform[] dogs=Array.Empty<Transform>();public float dogTravel=.5f;
        /// <summary>The collar ring, turned <see cref="collarTurn"/> degrees about its own forward axis as it gives.</summary>
        public Transform collar;public float collarTurn=12;
        /// <summary>The two hull leaves that part, and how far (world offsets) each slides.</summary>
        public Transform[] leaves=Array.Empty<Transform>();public Vector3[] leafTravel=Array.Empty<Vector3>();
        public GameObject blocks,shutLamps,openLamps;public Light glow;public float glowShut=.4f,glowOpen=2.2f;
        public Bounds shows;public float showsFrom=2.5f;
        /// <summary>The two beats.</summary>
        public float take=1.6f,give=2.6f;
        public string groan="root_groan",knock="impactMetal_000",part="doorOpen_000";public float volume=.5f;
        float working=-1;bool measured,givenLogged;int knocked;CameraRig rig;
        Vector3[] dogRest=Array.Empty<Vector3>(),leafRest=Array.Empty<Vector3>();Quaternion wheelRest,collarRest;
        /// <summary>Where it is between shut (0) and open (1), for a test: the leaves' travel.</summary>
        public float Opened{get;private set;}
        public bool Working=>working>=0;
        void Measure()
        {
            if(measured)return;measured=true;
            dogRest=new Vector3[dogs.Length];for(int i=0;i<dogs.Length;i++)if(dogs[i]!=null)dogRest[i]=dogs[i].position;
            leafRest=new Vector3[leaves.Length];for(int i=0;i<leaves.Length;i++)if(leaves[i]!=null)leafRest[i]=leaves[i].position;
            if(wheel!=null)wheelRest=wheel.localRotation;if(collar!=null)collarRest=collar.localRotation;
        }
        void Awake()=>Measure();
        protected override void Settle(){Measure();Pose(1,1,1);if(blocks!=null)blocks.SetActive(false);Lamps(true);givenLogged=true;}
        protected override void Work()
        {
            Measure();working=0;knocked=0;
            AudioManager.PlayAt(groan,transform,transform.position+Vector3.up*2,volume,AudioBus.SFX,40,.06f,.7f);
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(take*.7f,.04f);
        }
        /// <summary>The wheel's turn, how far each dog is drawn (0 to 1 over the take, one after another), and the give (0 to 1).</summary>
        void Pose(float turned,float drawn,float given)
        {
            if(wheel!=null)wheel.localRotation=wheelRest*Quaternion.AngleAxis(wheelTurns*360*turned,wheelAxis);
            for(int i=0;i<dogs.Length;i++)
            {
                if(dogs[i]==null)continue;
                // Each dog in its turn: dog i is drawn over its own share of the take.
                float own=Mathf.Clamp01(drawn*dogs.Length-i);
                dogs[i].position=dogRest[i]+dogs[i].forward*dogTravel*Mathf.SmoothStep(0,1,own);
            }
            if(collar!=null)collar.localRotation=collarRest*Quaternion.AngleAxis(collarTurn*Mathf.SmoothStep(0,1,Mathf.Clamp01(given*1.6f)),Vector3.forward);
            float slide=Mathf.SmoothStep(0,1,Mathf.Clamp01((given-.2f)/.8f));Opened=slide;
            for(int i=0;i<leaves.Length;i++)if(leaves[i]!=null)leaves[i].position=leafRest[i]+(i<leafTravel.Length?leafTravel[i]:Vector3.zero)*slide;
        }
        void Lamps(bool open)
        {
            if(shutLamps!=null)shutLamps.SetActive(!open);if(openLamps!=null)openLamps.SetActive(open);
            if(glow!=null)glow.intensity=open?glowOpen:glowShut;
        }
        protected override void Update()
        {
            base.Update();
            if(working>=0&&!GameTime.Paused)
            {
                working+=Time.deltaTime;
                if(working<take)
                {
                    float d=working/take;Pose(d,d,0);
                    int due=Mathf.Min(dogs.Length,Mathf.FloorToInt(d*dogs.Length+.5f));
                    while(knocked<due)
                    {
                        var at=dogs.Length>0&&dogs[knocked]!=null?dogs[knocked].position:transform.position;
                        AudioManager.PlayAt(knock,transform,at,volume*.7f,AudioBus.SFX,40,.04f,.75f+.05f*knocked);knocked++;
                    }
                }
                else
                {
                    float before=working-Time.deltaTime;
                    if(before<take)
                    {
                        AudioManager.PlayAt(part,transform,transform.position+Vector3.up*2,volume,AudioBus.SFX,40,.06f,.55f);
                        CombatVfx.Burst(transform.position+Vector3.up*2.6f,new Color(.78f,.76f,.72f,.55f),"steam");Lamps(true);
                    }
                    float g=Mathf.Clamp01((working-take)/give);Pose(1,1,g);
                    // The way opens once the leaves are past half: not before they have visibly moved.
                    if(blocks!=null&&blocks.activeSelf&&g>.5f)blocks.SetActive(false);
                    if(g>=1){working=-1;if(!givenLogged){givenLogged=true;Debug.Log("JOINT_GIVEN "+flag);}}
                }
            }
            else if(!Worked)Lamps(false);
            Show();
        }
        void Show()
        {
            if(shows.size.sqrMagnitude<=0||GameServices.Current==null||GameServices.Current.Input.Blocked)return;
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var at=party.Active.transform.position;
            if(working<0)
            {
                // Not yet worked: only for someone standing at its jack, and only while the jack is offered.
                if(Worked||point==null||!point.Wanted)return;
                var offset=at-point.transform.position;offset.y=0;
                if(offset.magnitude>point.range+showsFrom)return;
            }
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig==null)return;
            var view=new Bounds(at+Vector3.up,Vector3.one*3);view.Encapsulate(shows);
            rig.FrameSceneInterest(this,view,FramedIn);
        }
    }
}
