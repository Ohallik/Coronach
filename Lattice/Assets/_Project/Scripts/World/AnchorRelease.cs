using System;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Stillwater's last anchor, held and let go (chapter 10, docs/maps/StillwaterChamber.md, "The anchor"; wiring 6.6), one to the zone. <b>Held</b>, it pulls for <see cref="pull"/> seconds and
    /// eases for <see cref="ease"/> on its own clock, which stands still while anything holds the controls: the chain draws up and trembles and the four clamps' lamps go hard white as it
    /// pulls; it settles and they soften as it eases. When Ishen's sequence ends on Idra's "Ease." (<see cref="sequenceFlag"/>) the clock is set to an ease, so the word and the slack are
    /// one moment, and the last collar (<see cref="LastCollar"/>) is ridden on the ease after. <b>Let go</b> (<see cref="releasedFlag"/>, set by the collar in play), the party is held and the
    /// view is drawn back to take in the clamps, the chain and the window: the collar rings; the four clamps let go one by one, each jaw thrown open off the chain with a release of
    /// steam and its lamp going out; then all four are thrown right back together, a heavy blow and the room's tremor; the chain runs out through the collar, link after link, faster,
    /// until its end is gone down the bay; the cold light that held the chamber goes and the road's light comes up warm through the window and under the bay, and the road itself begins to
    /// flow (<see cref="road"/>); the music of the moment comes in. Then <see cref="node"/> plays as it all goes on (Meret: "It opens."), the view still drawn back; nothing barks from then
    /// until <see cref="custodyFlag"/>. A save made during it loads the anchor as its flags say, released, with nothing played but the talk if it was never heard.
    /// Logs <c>ANCHOR_PULLS</c>, <c>ANCHOR_EASES</c>, <c>ANCHOR_RELEASE_BEGIN</c>, <c>ANCHOR_CLAMP_LETS_GO &lt;n&gt;</c>, <c>ANCHOR_ALL_AS_ONE</c>, <c>ANCHOR_CHAIN_RUNS</c>,
    /// <c>ANCHOR_CHAIN_GONE</c>, <c>ANCHOR_TALK</c>, <c>ANCHOR_RELEASED_AT_LOAD</c>.
    /// </summary>
    public sealed class AnchorRelease:MonoBehaviour
    {
        public enum Stage{Holding,Opening,Talking,After}
        public string sequenceFlag="end.sequenceRead",releasedFlag="end.collarTaken",heardFlag="end.anchorReleased",custodyFlag="end.custody";
        public string node="MeretSeesItOpen",speaker="Meret";
        /// <summary>The moment's own track (a title the zone's cueMusicIds stage).</summary>
        public string music;
        public float pull=12,ease=3;
        /// <summary>Each clamp's jaw, on a pivot at its hinge, the axis (world) it opens about, and how far it opens at its own release and then thrown back with the rest.</summary>
        /// <summary>(Thrown back to 92 degrees, the first time anyone saw it on November 14, each jaw folded down on its pillar and the four clamps stood as stone arches round an empty
        /// ring: thrown open to 68 they are still jaws, wide off the chain.)</summary>
        public Transform[] jaws=Array.Empty<Transform>();public Vector3[] axes=Array.Empty<Vector3>();public float letGo=34,thrownBack=68;
        /// <summary>Each clamp's lamps (by property block: an editor still shows them as built) and its light, and where it vents as it lets go.</summary>
        public Renderer[] clampLamps=Array.Empty<Renderer>();public int[] lampClamp=Array.Empty<int>();public Light[] clampLights=Array.Empty<Light>();public Vector3[] vents=Array.Empty<Vector3>();
        /// <summary>The chain (its root moves: drawn only), how far it runs out before its end is gone, and the length of one link (a knock as each passes the collar).</summary>
        public Transform chain;public float chainRun=150,link=10,chainSeconds=6.5f;public Vector3 collar;
        /// <summary>The lights that held the chamber cold, and those the road lights it with once it moves; the road's glowing parts, and the rings of it that flow.</summary>
        public Light[] held=Array.Empty<Light>(),freed=Array.Empty<Light>();public Renderer[] roadGlow=Array.Empty<Renderer>();public Transform[] road=Array.Empty<Transform>();
        public Vector3 roadFlow=new(0,0,1);public float roadSpan=40,roadSpeed=6;
        [ColorUsage(false,true)] public Color roadStill=new(.12f,.2f,.32f),roadFree=new(1.9f,1.25f,.55f);
        [ColorUsage(false,true)] public Color lampPull=new(1.6f,1.65f,1.75f),lampEase=new(.35f,.8f,1.05f),lampOut=new(.05f,.05f,.06f);
        /// <summary>The key light, turned from the chamber's cold to the road's warm.</summary>
        public Light key;public Color keyHeld=new(.78f,.88f,1),keyFree=new(1,.84f,.62f);public float keyHeldIntensity=.7f,keyFreeIntensity=1.05f;
        /// <summary>The chamber's bed is its steel under the anchor's load (moaning, the chain knocking): once the load is gone it is eased down with the warm turn to this share of the zone's
        /// own level, and stays there, a load after the release included. Its clip is never swapped: a swap cuts it and starts it again.</summary>
        public float bedAfter=.3f;
        /// <summary>What the view draws back to take in, kept above the dialogue box, and how far it may go. <see cref="opening"/>, when it is set, is what it takes in while the anchor
        /// lets go: the clamps, the chain's head over them and the bow behind (the first stills of it, November 14: framed on <see cref="shows"/>, the well's floor filled the lower half of
        /// the frame, the anchor sat small on its top edge and the chain's head was out of it).</summary>
        public Bounds shows,opening;public float farthest=48;
        /// <summary>Seconds into the opening: each clamp's release in turn, all four together, the chain starting to run, and the talk.</summary>
        public float[] clampAt={1.1f,2.3f,3.5f,4.7f};public float togetherAt=6,talkAt=9.5f;
        static readonly Rect Above=Rect.MinMaxRect(.04f,.36f,.96f,.97f),HeroAbove=Rect.MinMaxRect(.1f,.38f,.9f,.92f);
        const int FramingRank=2;
        public static AnchorRelease Current{get;private set;}
        public Stage Now{get;private set;}
        /// <summary>The anchor's own clock: the share of the way through its cycle, whether it eases now, how many eases have begun since the zone opened, and the seconds left of this one.</summary>
        public bool Easing=>Phase>=pull;
        public int Eases{get;private set;}
        public float EaseLeft=>Easing?pull+ease-Phase:0;
        public float Phase{get;private set;}
        public int Released{get;private set;}
        public float ChainOut{get;private set;}
        public float Warmth{get;private set;}
        public bool Gone{get;private set;}
        float clock,roadClock,cueAt=-1;bool settled,sequenceSeen,talking,hushed,cued,together,chainSaid,goneSaid,wasEasing,opened;int knocks;
        Quaternion[] jawRest=Array.Empty<Quaternion>();Vector3 chainHome;Vector3[] roadHome=Array.Empty<Vector3>();float[] heldIntensity=Array.Empty<float>(),freedIntensity=Array.Empty<float>();
        MaterialPropertyBlock block;CameraRig rig;AudioManager.Loop run;
        static bool Flag(string key)=>!string.IsNullOrEmpty(key)&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        void OnEnable()=>Current=this;
        void OnDisable(){if(Current==this)Current=null;Quiet();}
        void Quiet(){if(cued){MusicDirector.Encounter(this,null,false);cued=false;}if(hushed){BarkService.Hush(this,false);hushed=false;}run?.Dispose();run=null;}
        void Awake()
        {
            block=new MaterialPropertyBlock();
            jawRest=jaws.Select(j=>j!=null?j.localRotation:Quaternion.identity).ToArray();chainHome=chain!=null?chain.position:Vector3.zero;
            roadHome=road.Select(r=>r!=null?r.position:Vector3.zero).ToArray();
            heldIntensity=held.Select(l=>l!=null?l.intensity:0).ToArray();freedIntensity=freed.Select(l=>l!=null?l.intensity:0).ToArray();
            foreach(var l in freed)if(l!=null)l.intensity=0;
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            var party=PartyController.Current;bool hasParty=party!=null&&party.Active!=null;
            if(!settled)
            {
                if(!hasParty)return;settled=true;sequenceSeen=Flag(sequenceFlag);
                if(Flag(releasedFlag)){Pose(true);Now=Flag(heardFlag)?Stage.After:Stage.Talking;Debug.Log("ANCHOR_RELEASED_AT_LOAD");if(!Flag(custodyFlag))Hush(true);if(Now==Stage.Talking)clock=talkAt;}
                else Pose(false);
            }
            bool heldNow=GameTime.CombatHeld;
            switch(Now)
            {
                case Stage.Holding:
                    if(Flag(releasedFlag)&&!services.Input.Blocked){Begin();return;}
                    // Idra's "Ease.": the word and the slack are one moment.
                    if(!sequenceSeen&&Flag(sequenceFlag)){sequenceSeen=true;Phase=pull;Eases++;wasEasing=true;Debug.Log("ANCHOR_EASES "+Eases+" (called)");}
                    if(!heldNow)Cycle(Time.deltaTime);
                    Breathe();
                    return;
                case Stage.Opening:
                    clock+=Time.deltaTime;Hold();Frame();Open();
                    if(clock>=talkAt){Now=Stage.Talking;}
                    return;
                case Stage.Talking:
                    clock+=Time.deltaTime;Frame();Open();
                    // Straight on from the opening (the party is still held by it, and the talk takes the hold over); at a load, once the zone has settled.
                    if(!talking&&!Flag(heardFlag)){if(opened||!services.Input.Blocked&&!BossController.AnyFightUnderway)Talk();}
                    if(Flag(heardFlag)&&!talking){Now=Stage.After;cueAt=Time.time+3;Debug.Log("ANCHOR_HEARD");}
                    return;
                default:
                    Open();Flow();
                    if(cueAt>0&&Time.time<cueAt)Frame();
                    if(hushed&&Flag(custodyFlag))Hush(false);
                    return;
            }
        }
        // ---------------------------------------------------------------- held: the anchor's cycle
        void Cycle(float dt)
        {
            Phase+=dt;
            if(Phase>=pull+ease){Phase-=pull+ease;Debug.Log("ANCHOR_PULLS");AudioManager.PlayAt("root_groan",null,collar+Vector3.up*2,.16f,AudioBus.SFX,50,.08f,.55f);}
            bool easing=Easing;
            if(easing&&!wasEasing){Eases++;Debug.Log("ANCHOR_EASES "+Eases);AudioManager.PlayAt("joint_hiss",null,collar+Vector3.up*2,.14f,AudioBus.SFX,50,.05f,.7f);}
            wasEasing=easing;
        }
        /// <summary>The chain's strain and the clamps' lamps as the cycle has them: up and trembling, hard white, through a pull; down and soft through an ease.</summary>
        void Breathe()
        {
            float strain=Easing?1-Mathf.SmoothStep(0,1,(Phase-pull)/Mathf.Min(1.2f,ease)):Mathf.SmoothStep(0,1,Mathf.Min(1,Phase/2.5f));
            float tremble=strain*.025f*Mathf.Sin(Time.time*47);
            if(chain!=null)chain.position=chainHome+Vector3.up*(.35f*strain+tremble);
            var lamp=Color.Lerp(lampEase,lampPull,strain);
            for(int i=0;i<clampLamps.Length;i++)Glow(clampLamps[i],lamp);
            foreach(var l in clampLights)if(l!=null)l.intensity=Mathf.Lerp(.8f,1.8f,strain);
        }
        // ---------------------------------------------------------------- let go
        void Begin()
        {
            Now=Stage.Opening;clock=0;Released=0;together=false;opened=true;Debug.Log("ANCHOR_RELEASE_BEGIN");
            if(!string.IsNullOrEmpty(music)){MusicDirector.Encounter(this,music,true);cued=true;}
            Hush(true);
            GameServices.Current.Input.Blocked=true;
            AudioManager.PlayAt("impactMetal_000",null,collar+Vector3.up,.34f,AudioBus.SFX,30,.05f,.5f);
            Impact.Current?.Hold(.12f,true);
            Rig()?.Tremor(.6f,.04f);
        }
        void Hush(bool on){if(on==hushed)return;hushed=on;BarkService.Hush(this,on);}
        /// <summary>The party is held through the opening; the talk holds it itself, and lets go as it ends.</summary>
        void Hold(){if(!talking)GameServices.Current.Input.Blocked=true;}
        CameraRig Rig(){if(rig==null)rig=FindFirstObjectByType<CameraRig>();return rig;}
        void Frame(){var view=opening.size.sqrMagnitude>0?opening:shows;if(view.size.sqrMagnitude<=0)return;Rig()?.FrameGround(view,Above,HeroAbove,farthest,FramingRank);}
        /// <summary>Where the opening has got to at <see cref="clock"/>: the clamps in turn, then together; the chain; the light; the road.</summary>
        void Open()
        {
            float t=clock;
            for(int i=0;i<jaws.Length;i++)
            {
                float at=i<clampAt.Length?clampAt[i]:togetherAt;
                float own=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-at)/.55f)),back=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-togetherAt)/.9f));
                if(jaws[i]!=null&&i<axes.Length)jaws[i].localRotation=Quaternion.AngleAxis(letGo*own+(thrownBack-letGo)*back,jaws[i].parent!=null?jaws[i].parent.InverseTransformDirection(axes[i]):axes[i])*jawRest[i];
                if(t>=at&&Released<=i)
                {
                    Released=i+1;Debug.Log("ANCHOR_CLAMP_LETS_GO "+(i+1));
                    var vent=i<vents.Length?vents[i]:collar;
                    AudioManager.PlayAt("joint_release",null,vent,.34f,AudioBus.SFX,30,.04f,UnityEngine.Random.Range(.92f,1.06f));
                    CombatVfx.Burst(vent,new Color(.88f,.92f,.96f,.6f),"steam");
                    Rig()?.Tremor(.5f,.05f);PadRumble.Pulse(.35f,.2f,.25f);
                }
            }
            for(int i=0;i<clampLamps.Length;i++)
            {
                int c=i<lampClamp.Length?lampClamp[i]:0;float at=c<clampAt.Length?clampAt[c]:togetherAt;
                Glow(clampLamps[i],t<at?lampPull:Color.Lerp(lampPull,lampOut,Mathf.Clamp01((t-at)/.4f)));
            }
            for(int i=0;i<clampLights.Length;i++){float at=i<clampAt.Length?clampAt[i]:togetherAt;if(clampLights[i]!=null)clampLights[i].intensity=t<at?1.8f:Mathf.Lerp(1.8f,0,(t-at)/.4f);}
            if(!together&&t>=togetherAt)
            {
                together=true;Debug.Log("ANCHOR_ALL_AS_ONE");
                AudioManager.PlayAt("joint_release",null,collar+Vector3.up*2,.5f,AudioBus.SFX,20,.03f,.8f);
                AudioManager.PlayAt("explosionCrunch_000",null,collar,.36f,AudioBus.SFX,20,.05f,.6f);
                foreach(var vent in vents)CombatVfx.Burst(vent+Vector3.up*.6f,new Color(.9f,.93f,.97f,.7f),"steam");
                var r=Rig();if(r!=null){r.Tremor(2.2f,.13f);r.Impulse(Vector3.down,.012f,.5f);}
                Impact.Current?.Hold(.16f,true);PadRumble.Pulse(.9f,.45f,.7f);
            }
            // The chain runs out: slow as the clamps' last grip lets go, then falling away faster, a knock as each link passes the collar.
            float c0=togetherAt-.2f;float u=Mathf.Clamp01((t-c0)/chainSeconds);
            ChainOut=chainRun*u*u;
            if(chain!=null)chain.position=chainHome+Vector3.down*ChainOut;
            if(u>0&&!chainSaid){chainSaid=true;Debug.Log("ANCHOR_CHAIN_RUNS");if(chain!=null){run=AudioManager.StartLoop("spaceEngineLow_000",chain,0);}}
            if(u>0&&u<1){run?.Set(Mathf.Lerp(.08f,.34f,u),Mathf.Lerp(.6f,1.15f,u));int passed=Mathf.FloorToInt(ChainOut/link);if(passed>knocks){knocks=passed;AudioManager.PlayAt("impactMetal_000",null,collar,Mathf.Lerp(.18f,.3f,u),AudioBus.SFX,40,.06f,UnityEngine.Random.Range(.5f,.62f));}}
            if(u>=1&&!goneSaid){goneSaid=true;Gone=true;run?.Dispose();run=null;if(chain!=null)chain.gameObject.SetActive(false);Debug.Log("ANCHOR_CHAIN_GONE");}
            // The light: from the moment all four go, the cold that held the chamber drains and the road's warm comes up, over six seconds.
            Warmth=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-togetherAt+.3f)/6));Light();Flow();
        }
        void Light()
        {
            for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].intensity=heldIntensity[i]*(1-Warmth);
            for(int i=0;i<freed.Length;i++)if(freed[i]!=null)freed[i].intensity=freedIntensity[i]*Warmth;
            if(key!=null){key.color=Color.Lerp(keyHeld,keyFree,Warmth);key.intensity=Mathf.Lerp(keyHeldIntensity,keyFreeIntensity,Warmth);}
            var glow=Color.Lerp(roadStill,roadFree,Warmth);foreach(var r in roadGlow)Glow(r,glow*(1+.12f*Warmth*Mathf.Sin(Time.time*1.3f)));
            // The steel's bed comes down as the light turns warm: the load it groaned under is gone.
            var zone=ZoneController.Current;if(zone!=null&&zone.definition!=null&&zone.definition.ambientAudio!=null)AudioManager.BedLevel(Mathf.Lerp(zone.definition.ambientVolume,zone.definition.ambientVolume*bedAfter,Warmth));
        }
        /// <summary>The road moving: its rings run along it, faster as it wakes, each going back to the far end as it passes the near one.</summary>
        void Flow()
        {
            roadClock+=Time.deltaTime*roadSpeed*Warmth;
            for(int i=0;i<road.Length;i++)if(road[i]!=null)road[i].position=roadHome[i]+roadFlow*Mathf.Repeat(roadClock+i*.0001f,roadSpan);
        }
        void Talk()
        {
            bool inside=true,refused=false;talking=true;
            bool sent=Npc.Say(node,speaker,completed=>{if(inside&&!completed){refused=true;return;}talking=false;});
            inside=false;if(!sent||refused){talking=false;return;}
            Debug.Log("ANCHOR_TALK "+node);
        }
        /// <summary>The anchor as its flags say, at load: held, or let go with the clamps thrown back, the chain gone and the road awake.</summary>
        void Pose(bool released)
        {
            if(!released){Breathe();foreach(var r in roadGlow)Glow(r,roadStill);return;}
            clock=Mathf.Max(togetherAt+chainSeconds+6,talkAt);Released=jaws.Length;together=chainSaid=goneSaid=true;Gone=true;ChainOut=chainRun;Warmth=1;
            for(int i=0;i<jaws.Length;i++)if(jaws[i]!=null&&i<axes.Length)jaws[i].localRotation=Quaternion.AngleAxis(thrownBack,jaws[i].parent!=null?jaws[i].parent.InverseTransformDirection(axes[i]):axes[i])*jawRest[i];
            foreach(var lamp in clampLamps)Glow(lamp,lampOut);foreach(var l in clampLights)if(l!=null)l.intensity=0;
            if(chain!=null)chain.gameObject.SetActive(false);
            Light();
        }
        void Glow(Renderer r,Color colour){if(r==null)return;r.GetPropertyBlock(block);block.SetColor("_EmissionColor",colour);r.SetPropertyBlock(block);}
    }
}
