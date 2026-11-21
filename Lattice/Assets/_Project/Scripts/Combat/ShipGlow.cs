using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// How lit a ship is, where light is what hunts you (chapter 4's Spore Veil: visibility is the resource). A shot lights the hull a little, a skill a lot, a boost for as long as it is
    /// held; a roll and plain flying don't, and after a beat of nothing the light ebbs away. A place with the rule puts one on each hero ship (Lattice.World.GlowField, through
    /// <see cref="Of"/>); anywhere else a ship has none (<see cref="On"/> answers null), and so it is never dark (<see cref="DarkFor"/>). A swarm riding a ship that has flown dark for
    /// <see cref="ShedAfter"/> seconds lets go of it, and one awake in the field comes only for a lit ship or one that flies into it (EnemySwarmCloud); a lit ship wakes sleeping swarms
    /// from afar (GlowField). The HUD's gauge reads <see cref="Level"/> (Lattice.UI.GlowGauge), and the hull shows it too: a halo of spore light that grows with it.
    /// It only watches what the hero does (<see cref="CombatActor.AttackSequence"/> and the state it leaves, the flight motor's drive): nothing in the hero's own code changes.
    /// The first time a hull blazes the heroes say so, and Maru, once a save, on the radio.
    /// </summary>
    public sealed class ShipGlow:MonoBehaviour
    {
        /// <summary>What a shot, a skill and a second of boost add, how fast it ebbs once it does, how long after the last of them it holds first, and from what level a ship counts as lit
        /// (and under what it stops being: a band, so a ship on the line isn't lit and unlit every other frame).</summary>
        public const float Shot=.12f,Skill=.45f,Boost=.7f,Ebb=.45f,Holds=.5f,Lit=.35f,Unlit=.25f;
        /// <summary>Seconds flown dark that lose a ship its swarm: Maru's "go dark and count two".</summary>
        public const float ShedAfter=2;
        /// <summary>0 dark to 1 blazing.</summary>
        public float Level{get;private set;}
        /// <summary>When something last lit it.</summary>
        public float LastLit{get;private set;}=float.NegativeInfinity;
        public bool IsLit{get;private set;}
        /// <summary>Flown dark long enough that a swarm has lost it.</summary>
        public bool Dark=>Time.time-LastLit>=ShedAfter;
        public CombatActor Actor=>actor;
        /// <summary>A ship has just been lit.</summary>
        public static event System.Action<ShipGlow> WentLit;
        /// <summary>The flag that says Maru has told the party their hull is lit, once a save.</summary>
        public const string BrightHeard="wend.brightHeard";
        static readonly Color HaloTone=new(.62f,1.05f,.7f,.42f);
        CombatActor actor;FlightMotor motor;int seen;Transform halo;bool fullSaid;
        /// <summary>The ship's glow, put on it if it has none: what a place with the rule calls.</summary>
        public static ShipGlow Of(CombatActor hero)=>hero==null?null:hero.TryGetComponent<ShipGlow>(out var glow)?glow:hero.gameObject.AddComponent<ShipGlow>();
        /// <summary>The ship's glow if a place has given it one, or null: what everything that only reads it calls, so reading never puts one on.</summary>
        public static ShipGlow On(CombatActor hero)=>hero!=null&&hero.TryGetComponent<ShipGlow>(out var glow)&&glow.isActiveAndEnabled?glow:null;
        /// <summary>How long this ship has flown dark: with no glow on it it is never dark, and this is zero.</summary>
        public static float DarkFor(CombatActor hero){var glow=On(hero);return glow==null?0:Mathf.Max(0,Time.time-glow.LastLit);}
        void Awake(){actor=GetComponent<CombatActor>();motor=GetComponent<FlightMotor>();seen=actor!=null?actor.AttackSequence:0;LastLit=Time.time;}
        void Update()
        {
            if(actor==null||GameTime.Paused)return;
            if(!actor.flight||!actor.Health.Alive){Level=0;IsLit=false;seen=actor.AttackSequence;Show();return;}
            if(actor.AttackSequence!=seen)
            {
                seen=actor.AttackSequence;
                if(actor.State==ActorState.Attack)Light(Shot);else if(actor.State==ActorState.Skill)Light(Skill);
            }
            // A boost is a drive past the stick's own (the motor doubles it); a roll's dash is not light.
            if(motor!=null&&motor.EngineDrive>1.2f&&actor.State!=ActorState.Dodge)Light(Boost*Time.deltaTime);
            if(Time.time-LastLit>Holds)Level=Mathf.Max(0,Level-Ebb*Time.deltaTime);
            if(!IsLit&&Level>=Lit){IsLit=true;Debug.Log("SHIP_LIT "+actor.character);WentLit?.Invoke(this);}
            else if(IsLit&&Level<Unlit)IsLit=false;
            if(Level>=1&&!fullSaid)
            {
                fullSaid=true;
                var flags=GameServices.Current!=null?GameServices.Current.Flags:null;
                if(flags!=null&&!flags.GetBool(BrightHeard)){flags.SetBool(BrightHeard,true);BarkService.Play("Maru","hail.bright",true);}
                else BarkService.Play(actor.character,"glow");
            }
            else if(Level<.5f)fullSaid=false;
            Show();
        }
        void Light(float amount){Level=Mathf.Min(1,Level+amount);LastLit=Time.time;}
        /// <summary>Lit as a test or a scene needs it.</summary>
        public void Set(float level){Level=Mathf.Clamp01(level);IsLit=Level>=Lit;if(Level>0)LastLit=Time.time;}
        void Show()
        {
            bool shown=Level>.05f;
            if(halo==null)
            {
                if(!shown)return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Ship glow";Destroy(go.GetComponent<Collider>());go.transform.SetParent(transform,false);
                var view=go.GetComponent<Renderer>();view.sharedMaterial=ActorFactory.Energy(HaloTone);view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
                halo=go.transform;
            }
            if(halo.gameObject.activeSelf!=shown)halo.gameObject.SetActive(shown);
            if(shown)halo.localScale=Vector3.one*(1.2f+1.6f*Level);
        }
        void OnDestroy(){if(halo!=null)Destroy(halo.gameObject);}
    }
}
