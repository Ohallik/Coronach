using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Molten metal run into a trough in the floor (chapter 8, the Bridle Foundry; docs/maps/BridleFoundry.md). Two kinds, one rule: <b>marked means hot</b>.
    /// <list type="bullet">
    /// <item>A <b>ladle line</b> (<see cref="period"/> over nought): a ladle on an overhead rail runs out from its bay over the trough, tips, pours, rights itself and runs back, on a
    /// clock a player reads by watching it come. The strip is marked as the ladle tips (<see cref="warn"/>), stays marked through the pour (<see cref="pour"/>), and is gone as the
    /// metal crusts dark behind it; then there is the rest of the period to cross.</item>
    /// <item>A <b>tap run</b> (<see cref="period"/> nought): a furnace's run, pouring for as long as it runs, marked steady, with a waist-high seal over it (<see cref="seal"/>) so
    /// nothing walks or paths into it. Nobody crosses it until it is cooled.</item>
    /// </list>
    /// A pour strikes a hero standing in its marking while it pours: <see cref="strike"/> of his Integrity, never the last of it (the place's damage: no enemy is its source, so
    /// difficulty leaves it alone), and throws him clear out of the near side; once a pour, and nothing through a dodge's cover. <see cref="armFlag"/> (coolant standing in the
    /// trough until a sluice drains it) keeps a ladle at rest until it is set; <see cref="cooledFlag"/> (a sluice flooding it) crusts it for good. A save loads it in the state its
    /// flags say, with nothing played.
    /// <b>The marking wins over the glow</b>: the strip is the trough and a margin each side, so the marking's rim lies on the dark kerb and never on bright metal; the metal is drawn
    /// below the marking's lift and before it in the queue (the builder's material), and only ever lit while it is marked.
    /// Logs <c>POUR_STRUCK hero name</c>, <c>POUR_ARMED name</c>, <c>POUR_COOLED flag</c>.
    /// </summary>
    public sealed class PourChannel:MonoBehaviour
    {
        public static readonly List<PourChannel> All=new();
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor"),BaseId=Shader.PropertyToID("_BaseColor");

        /// <summary>The strip a pour strikes: from this object's place along <see cref="along"/> for <see cref="length"/>, <see cref="width"/> across (trough and margins).</summary>
        public Vector3 along=Vector3.right;public float length=10,width=3.6f;
        /// <summary>The clock. Each period (shifted by <see cref="phase"/>) the ladle runs out for <see cref="travel"/>, tips with the strip marked for <see cref="warn"/>, pours for
        /// <see cref="pour"/>, and runs back for <see cref="travel"/>; the rest of the period it waits in its bay. Nought: a tap run.</summary>
        public float period=10,phase,travel=1.6f,warn=1.4f,pour=2.4f;
        public string armFlag="",cooledFlag="";
        /// <summary>A pour's blow (a share of a hero's Integrity) and how far it throws him.</summary>
        public float strike=.2f,throwClear=3.2f;
        /// <summary>What is drawn: the metal in the trough (lit by property block from crust to molten), the falling stream (shown while it pours), the ladle and where it rests and
        /// pours from (world points; it tips about its own forward axis by <see cref="tip"/> degrees), and the light over the trough.</summary>
        public Renderer[] metal=System.Array.Empty<Renderer>();public GameObject stream;public Transform ladle;public Vector3 ladleRest,ladleOver;public float tip=62;public Light glow;
        /// <summary>What a ladle hangs by, when its rail runs over the camera (the Foundry's ladle line L2): moved with the ladle along its run, never tipped with it.</summary>
        public Transform hanger;
        /// <summary>The metal's emission at full heat, its colour as crust and as melt, and its light's brightness at full heat. Yellow-white, not the marking's orange, and held
        /// near the bloom's threshold so its halo doesn't reach the marking's rim on the kerb.</summary>
        public Color molten=new(1.1f,.94f,.66f,1),crust=new(.16f,.13f,.12f,1),hot=new(1,.93f,.78f,1);
        public float glowIntensity=2.4f;
        /// <summary>Ember: metal hot in its channel and not yet dangerous to look at, deep red, dimmer and redder than any marking (Telegraph.Threat is (1, .42, .1)). A tap run
        /// glows it for as long as it runs, and every <see cref="surgeEvery"/> seconds a surge of fresh metal down it for <see cref="surgeFor"/> warms it to <see cref="emberHot"/>,
        /// still redder and dimmer than its marking. A ladle's trough is dark at rest, warms to ember under its marking as the ladle tips, and is the melt's pale yellow-white only
        /// while it pours: the one bright metal in the place is the metal about to strike.</summary>
        public Color ember=new(.3f,.025f,.01f,1),emberHot=new(.48f,.06f,.02f,1),emberBase=new(.14f,.04f,.03f,1);public float surgeEvery=7,surgeFor=2.2f;
        /// <summary>How far into a surge a tap run is at a clock time (0 its ember, 1 <see cref="emberHot"/>).</summary>
        public float SurgeAt(float time){if(period>0||surgeEvery<=0)return 0;float t=Mathf.Repeat(time,surgeEvery);return t<surgeFor?Mathf.Sin(Mathf.Clamp01(t/surgeFor)*Mathf.PI):0;}
        /// <summary>The seal a tap run keeps over its trough (a Membrane holder with a carving obstacle): off once cooled.</summary>
        public GameObject seal;
        /// <summary>What it says: the bark the lead gives as a hero is struck.</summary>
        public string sound="thrusterFire_000",bark="";

        float started=float.NaN;Telegraph marking,fading;bool pouring,cooled,armed,known;float heat,warmth;readonly bool[] struck=new bool[3];MaterialPropertyBlock block;Quaternion upright;
        public bool Cooled=>cooled;
        public bool Armed=>armed&&!cooled;
        /// <summary>Whether a pour is marked now, and whether it is pouring (striking) now.</summary>
        public bool Marked=>marking!=null&&!marking.Released;
        public bool Pouring=>pouring;
        /// <summary>A marking is drawn over it: marked, or the last pour's marking still flashing and fading as the metal goes dark under it.</summary>
        public bool Shown=>Marked||fading!=null;
        public Telegraph Marking=>marking;
        /// <summary>The channel's own time: seconds since the zone opened, shifted by its phase.</summary>
        public float Clock=>(float.IsNaN(started)?0:GameTime.Now-started)+phase;
        public enum Stage{Resting,Out,Warning,Pouring,Back}
        /// <summary>Where in its period a ladle line is at a clock time.</summary>
        public Stage StageAt(float time)
        {
            if(period<=0)return Stage.Pouring;
            float t=Mathf.Repeat(time,period);
            if(t<travel)return Stage.Out;if(t<travel+warn)return Stage.Warning;if(t<travel+warn+pour)return Stage.Pouring;if(t<travel*2+warn+pour)return Stage.Back;return Stage.Resting;
        }
        /// <summary>Seconds from a clock time to the next time the strip is marked (nought while it is).</summary>
        public float UntilMarked(float time){if(period<=0)return 0;var s=StageAt(time);if(s==Stage.Warning||s==Stage.Pouring)return 0;return Mathf.Repeat(travel-Mathf.Repeat(time,period),period);}
        /// <summary>The strip, as a world rectangle's test: a body's allowance and all.</summary>
        public bool Contains(Vector3 point)
        {
            var o=point-transform.position;o.y=0;var a=Flat(along);float on=Vector3.Dot(o,a),across=Mathf.Abs(Vector3.Dot(o,Vector3.Cross(Vector3.up,a)));
            return on>=-Telegraph.BodyAllowance&&on<=length+Telegraph.BodyAllowance&&across<=width*.5f+Telegraph.BodyAllowance;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v.sqrMagnitude>.0001f?v.normalized:Vector3.forward;}

        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);if(marking!=null){marking.Clear();marking=null;}pouring=false;}
        void Start(){started=GameTime.Now;block=new MaterialPropertyBlock();if(ladle!=null)upright=ladle.rotation;Show(0,false);}
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            bool coolNow=!string.IsNullOrEmpty(cooledFlag)&&services.Flags.GetBool(cooledFlag),armNow=string.IsNullOrEmpty(armFlag)||services.Flags.GetBool(armFlag);
            if(!known){known=true;cooled=coolNow;armed=armNow;if(cooled)Settle();}
            if(coolNow&&!cooled){cooled=true;Cool();}
            if(cooled)return;
            // Armed in sight, it starts its period from the top: the ladle is seen to run out before anything is marked.
            if(armNow&&!armed){armed=true;started=GameTime.Now+phase;Debug.Log("POUR_ARMED "+name);}
            if(!armed){Show(0,false);Pose(Stage.Resting,0);return;}
            if(period<=0){Run();return;}
            float time=Clock;var stage=StageAt(time);float t=Mathf.Repeat(time,period);
            Pose(stage,t);
            // The marking goes down as the ladle tips and stays through the pour; the pour's end releases it, the metal going dark behind.
            if((stage==Stage.Warning||stage==Stage.Pouring)&&marking==null)Mark(Mathf.Max(.05f,travel+warn-t));
            bool now=stage==Stage.Pouring;
            if(now&&!pouring){pouring=true;System.Array.Clear(struck,0,struck.Length);AudioManager.PlayAt(sound,transform,Middle,.32f,AudioBus.SFX,60,.06f,.8f);}
            if(!now&&pouring){pouring=false;if(marking!=null){marking.Release();fading=marking;marking=null;}}
            if(pouring)Strike();
            // Ember under the marking as the ladle tips, the melt while it pours; dark again as fast as the released marking fades (a third of a second): nothing glows on the floor
            // that isn't marked.
            heat=pouring?Mathf.MoveTowards(heat,1,Time.deltaTime*6):Mathf.MoveTowards(heat,0,Time.deltaTime*3.8f);
            warmth=Marked?Mathf.MoveTowards(warmth,1,Time.deltaTime/Mathf.Max(.2f,warn)):Mathf.MoveTowards(warmth,0,Time.deltaTime*3.8f);
            Show(heat,pouring,-1,warmth);
        }
        Vector3 Middle=>transform.position+Flat(along)*(length*.5f);
        void Mark(float tell)
        {
            if(marking!=null)marking.Clear();
            marking=Telegraph.Line(transform.position,Flat(along),length,width,tell,Telegraph.Threat);marking.owner=this;
        }
        /// <summary>A tap run: marked steady and pouring for as long as it runs. Its seal is what keeps a body out, so it strikes only while that is shut: a check that opens every
        /// seal to walk a zone's floor walks across it, as it walks across deep water.</summary>
        void Run()
        {
            if(marking==null){Mark(1);marking.Steady=true;}
            if(seal!=null&&!seal.activeSelf)seal.SetActive(true);
            pouring=true;heat=1;Show(1,true,SurgeAt(Clock));
            if(seal==null||!seal.TryGetComponent<Membrane>(out var held)||!held.Open)Strike();
        }
        void Strike()
        {
            var party=PartyController.Current;if(party==null||marking==null||marking.Elapsed<.5f||GameTime.CombatHeld)return;
            for(int k=0;k<party.members.Length&&k<struck.Length;k++)
            {
                var hero=party.members[k];if(hero==null||struck[k]||!hero.Health.Alive||hero.flight)continue;
                var at=hero.transform.position;if(!marking.Contains(at)||GameTime.Now<hero.Health.InvulnerableUntil)continue;
                struck[k]=true;
                float share=Mathf.Min(hero.Health.maximum*strike,Mathf.Max(0,hero.Health.integrity-1));
                if(share>0)hero.Health.Receive(new DamagePacket{amount=share,type=DamageType.Plasma,tag="pour"});
                // Out of the near side and on: a body thrown back into the metal it was thrown out of would be struck twice for one mistake.
                if(hero.motor is GroundMotor ground)ground.Dash(marking.WayOut(at),throwClear);
                Debug.Log("POUR_STRUCK "+hero.character+" "+name);
                if(!string.IsNullOrEmpty(bark)&&party.Active==hero)BarkService.Play(hero.character,bark,true);
            }
        }
        /// <summary>Coolant floods it: steam along the trough, the metal crusts, the stream stops, the seal goes; for good.</summary>
        void Cool()
        {
            Debug.Log("POUR_COOLED "+cooledFlag);
            var a=Flat(along);for(float s=.5f;s<length;s+=Mathf.Max(1.5f,length/6))CombatVfx.Burst(transform.position+a*s+Vector3.up*.4f,new Color(.86f,.92f,.92f,.6f),"steam");
            AudioManager.PlayAt("water_rush",transform,Middle,.42f,AudioBus.SFX,50);
            Settle();
        }
        void Settle()
        {
            if(marking!=null){marking.Clear();marking=null;}
            pouring=false;heat=0;if(seal!=null)seal.SetActive(false);Show(0,false);Pose(Stage.Resting,0);
        }
        void Pose(Stage stage,float t)
        {
            if(ladle==null)return;
            float out_=stage switch{Stage.Out=>Mathf.SmoothStep(0,1,t/travel),Stage.Warning or Stage.Pouring=>1,Stage.Back=>1-Mathf.SmoothStep(0,1,(t-travel-warn-pour)/travel),_=>0};
            float lean=stage switch{Stage.Warning=>Mathf.SmoothStep(0,1,(t-travel)/warn),Stage.Pouring=>1,Stage.Back=>Mathf.Clamp01(1-(t-travel-warn-pour)/.4f),_=>0};
            ladle.SetPositionAndRotation(Vector3.Lerp(ladleRest,ladleOver,out_),upright*Quaternion.AngleAxis(lean*tip,Vector3.forward));
            if(hanger!=null)hanger.position=Vector3.Lerp(ladleRest,ladleOver,out_);
        }
        /// <summary>The metal, its stream and its light. A ladle's: <paramref name="amount"/> of the melt over <paramref name="warm"/> of ember. A tap run's (<paramref name="surge"/>
        /// nought or more): ember, warming to <see cref="emberHot"/> through a surge.</summary>
        void Show(float amount,bool streaming,float surge=-1,float warm=0)
        {
            if(stream!=null&&stream.activeSelf!=streaming)stream.SetActive(streaming);
            if(block==null)block=new MaterialPropertyBlock();
            foreach(var r in metal)
            {
                if(r==null)continue;r.GetPropertyBlock(block);
                // A shimmer through a run's surface, so a tap run reads as flowing and not as a lit slab.
                float flow=streaming?.88f+.12f*Mathf.Sin(Time.time*5.3f+r.transform.position.x*.7f):1;
                var lit=surge<0?Color.Lerp(ember*warm,molten,amount)*flow:Color.Lerp(ember,emberHot,surge)*flow;
                var tone=surge<0?Color.Lerp(Color.Lerp(crust,emberBase,warm),hot,amount):emberBase;
                block.SetColor(EmissionId,lit);block.SetColor(BaseId,tone);r.SetPropertyBlock(block);
            }
            if(glow!=null)
            {
                // A run's light is the ember's, low and amber; a ladle's is the melt's, only while it pours (an ember light under a warning tinted the floor round its marking).
                float level=surge<0?amount:.3f+.15f*surge;glow.intensity=glowIntensity*level;
                glow.color=surge>=0?new Color(1,.6f,.36f):hot;glow.enabled=level>.02f;
            }
        }
    }
}
