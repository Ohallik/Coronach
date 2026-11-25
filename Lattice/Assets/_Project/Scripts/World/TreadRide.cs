using System;
using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The ride out (chapter 9's Terminator, docs/maps/Terminator.md): once the third coupling is mated the engine car wakes (its lamps, its stack, its engine), and this prompt on its
    /// boarding step (<c>Ride the tread-hall out</c>) takes the party aboard. Input is the car's from then: both heroes out are drawn onto the step, the car pulls the hall out along
    /// <see cref="way"/> (<see cref="distance"/> metres, up to <see cref="top"/> m/s in <see cref="gather"/> seconds, then easing in to stop at the Crawl's tail), every car and cable behind
    /// it going with it, and the dawn falls back behind it (<see cref="DawnLine"/>: it comes no nearer than its ride gap while the car gathers way). At the end the party is set down beside
    /// the stopped car at <see cref="setDown"/>, <see cref="flag"/> (the flag it sets, named as every prompt names its own) is set, that spawn is the checkpoint and the game saves; the talk that follows is an AfterTalk on the same flag.
    /// Nothing a player does can fail it: standing still is all it asks. A load with the flag set has every car where the ride left it. Logs <c>RIDE_AWAKE</c>, <c>RIDE_BEGUN</c>,
    /// <c>RIDE_DONE</c>.
    /// </summary>
    public sealed class TreadRide:InteractionPrompt
    {
        public static event Action<TreadRide> Done;
        public string requiredFlag="home.coupling3",flag="home.rideDone",spawn="Tail";
        /// <summary>The engine car (moved), and everything that goes with it (the other cars, their cables and sleds: moved by the same amount).</summary>
        public Transform car;public Transform[] train=Array.Empty<Transform>();
        /// <summary>Where on the car the lead stands, and the partner beside him (both children of the car, so they ride with it).</summary>
        public Transform deck,deckBeside;
        /// <summary>Where the party is set down at the end (a point on the ground beside where the car stops), and the partner beside it.</summary>
        public Vector3 setDown,setDownBeside;
        /// <summary>The way the car goes (on the plan), how far, its top speed and how long it takes to get there.</summary>
        public Vector3 way=Vector3.back;public float distance=110,top=9,gather=3;
        /// <summary>What wakes on the car: its lamps (emission), its stack's smoke, its engine's sound.</summary>
        public Renderer[] lamps=Array.Empty<Renderer>();public ParticleSystem[] smoke=Array.Empty<ParticleSystem>();public Light headlamp;
        /// <summary>The pool each lamp lays on the deck under it (D393), off until the car wakes.</summary>
        public Light[] lampLights=Array.Empty<Light>();
        [ColorUsage(false,true)] public Color lampsOn=new(1.8f,1.35f,.75f);
        public string engine="thrusterFire_000";
        public bool Riding{get;private set;}
        /// <summary>The third coupling is mated: its lamps are lit and its engine runs.</summary>
        public bool Woken{get;private set;}
        /// <summary>How far the car has come (0 to <see cref="distance"/>).</summary>
        public float Travelled{get;private set;}
        public override bool Available=>isActiveAndEnabled&&!Riding&&Woken&&!Flag(flag)&&!BossController.AnyFightUnderway&&!GameTime.Paused;
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        Vector3 carRest;Vector3[] trainRest=Array.Empty<Vector3>();bool measured;AudioManager.Loop hum;MaterialPropertyBlock block;
        static bool Flag(string key)=>GameServices.Current!=null&&!string.IsNullOrEmpty(key)&&GameServices.Current.Flags.GetBool(key);
        void Measure()
        {
            if(measured)return;measured=true;
            if(car!=null)carRest=car.position;trainRest=new Vector3[train.Length];for(int i=0;i<train.Length;i++)if(train[i]!=null)trainRest[i]=train[i].position;
        }
        void Start()
        {
            prompt="Ride the tread-hall out";Measure();
            if(Flag(flag)){Move(distance);Wake(false);}
            else if(Flag(requiredFlag))Wake(false);
            else foreach(var s in smoke)if(s!=null)s.Stop();
        }
        void Update()
        {
            // The third coupling mated in play: the car wakes, with its engine.
            if(!Woken&&Flag(requiredFlag))Wake(true);
        }
        void Wake(bool loud)
        {
            Woken=true;block??=new MaterialPropertyBlock();
            foreach(var r in lamps){if(r==null)continue;r.GetPropertyBlock(block);block.SetColor(EmissionId,lampsOn);r.SetPropertyBlock(block);}
            foreach(var s in smoke)if(s!=null)s.Play();
            if(headlamp!=null)headlamp.enabled=true;
            foreach(var l in lampLights)if(l!=null)l.enabled=true;
            if(loud&&car!=null){AudioManager.PlayAt("explosionCrunch_000",null,car.position+Vector3.up*3,.3f,AudioBus.SFX,40,.05f,.5f);Debug.Log("RIDE_AWAKE");}
            if(car!=null&&hum==null&&!Flag(flag)){hum=AudioManager.StartLoop(engine,car,.12f,AudioBus.SFX,140);hum?.Set(.12f,.55f);}
        }
        /// <summary>Every car and cable where a ride of this many metres leaves it.</summary>
        void Move(float metres)
        {
            Measure();Travelled=metres;var shift=way.normalized*metres;shift.y=0;
            if(car!=null)car.position=carRest+shift;
            for(int i=0;i<train.Length;i++)if(train[i]!=null)train[i].position=trainRest[i]+shift;
        }
        public override void Interact(){if(Available)StartCoroutine(Ride());}
        [SetsFlag("home.rideDone")]
        IEnumerator Ride()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)yield break;
            var services=GameServices.Current;Riding=true;bool wasBlocked=services.Input.Blocked;services.Input.Blocked=true;
            var lead=party.Active;var bodies=(CombatActor[])party.members.Clone();var controllers=new CharacterController[bodies.Length];var from=new Vector3[bodies.Length];
            for(int i=0;i<bodies.Length;i++){if(bodies[i]==null)continue;controllers[i]=bodies[i].GetComponent<CharacterController>();if(controllers[i]!=null)controllers[i].enabled=false;from[i]=bodies[i].transform.position;(bodies[i].motor as GroundMotor)?.Halt();}
            Debug.Log("RIDE_BEGUN");
            Vector3 Place(int i)=>bodies[i]==lead||deckBeside==null?deck.position:deckBeside.position;
            var facing=way;facing.y=0;
            // Up onto the step first, so nobody is carried from where they stood.
            for(float t=0;t<.45f;)
            {
                if(!GameTime.Paused)t+=Time.unscaledDeltaTime;
                for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){bodies[i].transform.position=Vector3.Lerp(from[i],Place(i),Mathf.SmoothStep(0,1,t/.45f));Face(bodies[i].transform,facing);}
                yield return null;
            }
            AudioManager.PlayAt("explosionCrunch_000",null,car.position+Vector3.up*2,.26f,AudioBus.SFX,40,.05f,.45f);
            hum?.Set(.2f,.7f);
            // Out: gathering way to its top speed, holding it, easing in at the end so it stops where it was meant to.
            float speed=0,brake=top*top/(2*Mathf.Max(1,top/gather*1.2f));
            while(Travelled<distance)
            {
                float dt=GameTime.Paused?0:Time.deltaTime;float left=distance-Travelled;
                speed=left<=brake?Mathf.Max(.8f,top*Mathf.Sqrt(left/Mathf.Max(.01f,brake))):Mathf.Min(top,speed+top/gather*dt);
                Move(Mathf.Min(distance,Travelled+speed*dt));
                for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){bodies[i].transform.position=Place(i);Face(bodies[i].transform,facing);}
                hum?.Set(.12f+.12f*speed/top,.55f+.35f*speed/top);
                yield return null;
            }
            // Set down beside the stopped car.
            for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)bodies[i].transform.position=bodies[i]==lead?setDown:setDownBeside;
            Physics.SyncTransforms();
            for(int i=0;i<bodies.Length;i++){if(controllers[i]!=null)controllers[i].enabled=true;if(bodies[i]!=null)(bodies[i].motor as GroundMotor)?.Halt();}
            if(hum!=null){hum.Dispose();hum=null;}
            services.Flags.SetBool(flag,true);
            if(!string.IsNullOrEmpty(spawn))services.State.spawn=spawn;
            services.Input.Blocked=wasBlocked;Riding=false;
            services.Saves.Save("autosave",services.State);
            Debug.Log("RIDE_DONE");Done?.Invoke(this);
        }
        static void Face(Transform body,Vector3 along){along.y=0;if(along.sqrMagnitude>.001f)body.rotation=Quaternion.LookRotation(along.normalized);}
        protected override void OnDisable()
        {
            base.OnDisable();if(hum!=null){hum.Dispose();hum=null;}
            // A ride cut off by a load gives the party back their feet and their hands.
            if(!Riding)return;Riding=false;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            if(PartyController.Current!=null)foreach(var body in PartyController.Current.members){var cc=body!=null?body.GetComponent<CharacterController>():null;if(cc!=null)cc.enabled=true;}
        }
    }
}
