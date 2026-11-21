using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Strafers <b>in a pair</b> (<c>tune.pair</c>: chapter 9's Interceptor, Compact fast craft that work two to a target). Two of one kind awake within <see cref="EnemyStrafer.WingReach"/> of
    /// each other are a pair, and the pair has two roles. The one that <b>pins</b> holds <see cref="PinHold"/> off the lead and marks a tractor line to him (attacks[1]: its telegraph, its
    /// range is the line's reach, its cooldown the wait before the next); a ship it catches flies at <see cref="PinPace"/> of its speed, a line drawn between them, for up to
    /// <see cref="PinHolds"/> s. The catch does no harm. The one that <b>strikes</b> flies as any strafer, and runs its lane through whoever its partner has pinned. The line breaks when the
    /// ship rolls, when anything hits the pinner, when cover comes between them, at <see cref="EnemyTetherer.SnapsAt"/> times the line's reach, or when its time is up. So there are two
    /// answers, both drawn: break the line, or shoot the one holding it; then cross out of the lane. With its partner gone (dead, or never there) one fights alone as a striker.
    /// Logs <c>PAIR_FORMED &lt;id&gt;</c>, <c>PIN_ON &lt;id&gt; &lt;hero&gt;</c>, <c>PIN_OFF &lt;id&gt; &lt;why&gt;</c>, <c>PAIR_STRIKES &lt;id&gt; &lt;hero&gt;</c>.
    /// </summary>
    public sealed partial class EnemyStrafer
    {
        /// <summary>How far off the pinner holds, the pace a pinned ship keeps, how long a pin lasts, and the line's width.</summary>
        public const float PinHold=14,PinPace=.3f,PinHolds=3.5f,PinWidth=1.2f;
        static readonly Color PinTone=new(.5f,1.4f,1.6f,.95f);
        static Material pinMaterial;
        EnemyStrafer partner;bool pinner,pinTold;CombatActor pinned;float pinUntil;LineRenderer pinLine;
        /// <summary>The pinner's count of the ships it has pinned, and the last of them its striker has answered (cut its break and rest for): once each pin.</summary>
        int pinSerial,pinAnswered;
        /// <summary>Its partner, if it has one; whether it is the one that pins; whom it pins now.</summary>
        public EnemyStrafer Partner=>partner;
        public bool Pins=>partner!=null&&pinner;
        public CombatActor Pinned=>pinned;
        AttackDef Tractor=>def.attacks!=null&&def.attacks.Length>1&&def.attacks[1]!=null?def.attacks[1]:attack;
        /// <summary>Finds its partner, and flies the pinner's part if that is its role. True when this frame was the pinner's.</summary>
        bool PairTick(CombatActor victim)
        {
            if(partner!=null&&(partner.health==null||!partner.health.Alive))LeavePair();
            if(partner==null)Pair();
            if(!Pins)return false;
            if(pinned!=null){Hold();return true;}
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;var toward=distance>.01f?to/distance:body.forward;
            if(pinTold)
            {
                if(!brain.TellRunOut)return true;
                pinTold=false;var mark=brain.EndTell(.3f);brain.Rest(Tractor!=null?Tractor.cooldown:4);
                if(mark!=null)
                {
                    // The first hero in the line, with nothing solid between: caught. It hurts nobody; what it holds still for is its partner.
                    foreach(var hero in PartyController.Current.members)
                        if(hero.Health.Alive&&mark.Contains(hero.transform.position)&&CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f))){Pin(hero);break;}
                    mark.Release();
                }
                Sound("forceField_000",.2f,1.4f);
                return true;
            }
            float reach=Tractor!=null?Mathf.Max(6,Tractor.range):15;
            if(distance<reach&&brain.Rested&&brain.Sees(victim))
            {
                body.rotation=Quaternion.LookRotation(toward);float tell=Tractor!=null?Mathf.Max(.5f,Tractor.telegraph):.9f;
                Mark(Telegraph.Line(body.position,toward,reach,PinWidth,tell,Telegraph.Threat,true),tell,toward,victim,"forceField_000",.8f);
                pinTold=true;return true;
            }
            // It holds off at the line's reach, as a tetherer does, and comes round a wall for a line.
            brain.Spread(to,distance,PinHold);if(distance>.01f)body.rotation=Quaternion.LookRotation(toward);
            if(distance>PinHold)brain.Walk(to,def.speed);else if(distance<PinHold*.6f)brain.Walk(-to,def.speed);
            return true;
        }
        /// <summary>The ship its partner has pinned, if any; else the one it was going for.</summary>
        CombatActor Quarry(CombatActor victim)
        {
            var held=partner!=null&&partner.pinner?partner.pinned:null;
            if(held==null||!held.Health.Alive)return victim;
            // Mid-run as its partner pinned, it broke away and rested its whole cooldown after, and the pin ran out first (HomeCombatTests, November 14): the first time it is free
            // during a pin, it leaves off breaking away and resting, once.
            if(pinAnswered!=partner.pinSerial&&(state==State.Circle||state==State.Break))
            {
                pinAnswered=partner.pinSerial;brain.Rest(.15f);
                if(state==State.Break){state=State.Circle;side=-side;}
            }
            if(state==State.Circle&&brain.Rested&&!loggedStrike){loggedStrike=true;Debug.Log($"PAIR_STRIKES {def.id} {held.character}");}
            return held;
        }
        bool loggedStrike;
        void Pair()
        {
            foreach(var other in wing)
            {
                if(other==null||other==this||other.partner!=null||!other.tune.pair||other.def.id!=def.id||other.health==null||!other.health.Alive||other.brain.Passive)continue;
                if(Flat(other.body.position-body.position).magnitude>WingReach)continue;
                partner=other;other.partner=this;
                // The one woken first pins (the lower id: it has been about longest), so a pair's roles never swap while both fly.
                pinner=brain.GetInstanceID()<other.brain.GetInstanceID();other.pinner=!pinner;
                Debug.Log($"PAIR_FORMED {def.id}");return;
            }
        }
        void LeavePair()
        {
            if(partner!=null&&partner.partner==this){partner.partner=null;partner.pinner=false;}
            partner=null;pinner=false;
        }
        void Pin(CombatActor hero)
        {
            pinned=hero;pinUntil=Time.time+PinHolds;loggedStrike=false;pinSerial++;
            // The striker comes for him now: what the pin is for. It leaves off breaking away and resting, and lines up as soon as it is in reach; one mid-run does it as its run ends
            // (Quarry).
            if(partner!=null&&partner.health!=null&&partner.health.Alive)
            {
                partner.loggedStrike=false;
                if(partner.state==State.Circle||partner.state==State.Break){partner.pinAnswered=pinSerial;partner.brain.Rest(.15f);if(partner.state==State.Break){partner.state=State.Circle;partner.side=-partner.side;}}
            }
            if(pinMaterial==null){pinMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));pinMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);}
            pinLine=new GameObject("Tractor line").AddComponent<LineRenderer>();pinLine.positionCount=2;pinLine.useWorldSpace=true;pinLine.startWidth=.18f;pinLine.endWidth=.12f;
            pinLine.sharedMaterial=pinMaterial;pinLine.startColor=PinTone;pinLine.endColor=new Color(PinTone.r,PinTone.g,PinTone.b,.7f);pinLine.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            DrawPin();AudioManager.PlayAt("forceField_000",null,hero.transform.position,.2f,AudioBus.SFX,100,.1f,1.1f);
            Debug.Log($"PIN_ON {def.id} {hero.character}");
        }
        void Hold()
        {
            var hero=pinned;var gap=Flat(body.position-hero.transform.position);float distance=gap.magnitude;float reach=Tractor!=null?Mathf.Max(6,Tractor.range):15;
            string why=!hero.Health.Alive?"down":hero.State==ActorState.Dodge?"rolled":Time.time>=pinUntil?"spent":distance>reach*EnemyTetherer.SnapsAt?"stretched":
                !CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f))?"cover":null;
            if(why!=null){Unpin(why);return;}
            hero.Health.Hinder(PinPace);
            if(distance>.01f)body.rotation=Quaternion.LookRotation(-gap);
            DrawPin();
        }
        void DrawPin()
        {
            if(pinLine==null||pinned==null)return;
            pinLine.SetPosition(0,Chest);pinLine.SetPosition(1,pinned.transform.position+Vector3.up*(pinned.flight?0:.9f));
        }
        void Unpin(string why)
        {
            if(pinTold&&why!="gone"){pinTold=false;}
            if(pinned==null){if(pinLine!=null)Object.Destroy(pinLine.gameObject);pinLine=null;return;}
            var at=pinned.transform.position;pinned=null;if(pinLine!=null)Object.Destroy(pinLine.gameObject);pinLine=null;
            if(why!="gone"){brain.Rest(Tractor!=null?Tractor.cooldown:4);AudioManager.PlayAt("doorOpen_000",null,at,.16f,AudioBus.SFX,100,.1f,1.6f);}
            Debug.Log($"PIN_OFF {def.id} {why}");
        }
        /// <summary>The line is drawn late every frame, wherever either end has got to.</summary>
        void PresentPin(){if(pinned!=null)DrawPin();}
    }
}
