using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The ambusher (the Reedstalker; later the Lurelight, the Ash Crawler, the Rime-stalker): it waits under a marked hide (<see cref="AmbushSpot"/>), where nothing can reach
    /// it, comes up when a hero steps inside the hide's ring, bites quickly <c>tune.count</c> times, each bite marked, and then stands spent in the open for <c>tune.recover</c>
    /// seconds before it makes for another hide and goes under again. The ring is the whole warning and the spent beat is the whole opening: step in to draw it, roll the bites,
    /// hit it before it is gone. It hides only once it is awake: a body on show before its fight, or held for a scene, stands where it was put.
    /// Reads: attacks[0] (telegraph of the first bite, range, damage, cooldown is unused), tune.wake (the ring, 3.2 m), count (bites, 2), speed (its pace between hides, 5.5),
    /// recover (spent in the open, 1.4 s), disguised, tint.
    /// <para><b>Disguised</b> (chapter 4's Lurelight): its hide is in the open. It stands on show as what it mimics, a lamp-post, solid and out of reach, and its lamp flickers in short
    /// flashes that never hold a long (<see cref="LureStalk.Flicker"/>), which is how a real lamp (two short, one long) is told from it. It springs, blazing, at whoever steps inside its
    /// ring, and goes between hides in hops, never under. Its lamp is drawn in <c>tune.tint</c>, or its own emission.</para>
    /// <para><b>A lunge</b> (<c>tune.reach</c> over nought: chapter 8's Ash Crawler): its bite is a lunge down a marked lane that long (stopping at a wall), and it goes the length of it. <b>Hot
    /// ground</b> (any ambusher, wherever the world says how hot the ground is: <see cref="GroundHeat"/>, the Scoria Flows' crust): it never goes under where the ground is hot (a seam about to
    /// go, or open melt), and one whose hide turns hot under it, or that stands on open melt, comes up and makes for a cold one at once: it flees bright flow. Elsewhere the ground is cold.</para>
    /// </summary>
    public sealed class EnemyAmbusher:EnemyBehaviour
    {
        enum State{Fresh,Seeking,Diving,Hidden,Rising,Biting,Closing,Spent,Lunging}
        bool Lunges=>tune.reach>0;Vector3 lungeWay;float lungeLeft;
        /// <summary>How long its lunge takes to cover its lane.</summary>
        public const float LungeSeconds=.16f;
        /// <summary>Somewhere it would go under: not on ground the world says is hot; where a dawn line comes on (<see cref="Dawn"/>, the Terminator), only in the warm band ahead of it and
        /// out of shade.</summary>
        static bool Cold(Vector3 point)=>Dawn.Here?Dawn.Warm(point):!GroundHeat.Hot(point);
        /// <summary>Somewhere it can't stand a moment longer: open melt; where a dawn comes on, shade, or the line's own edge.</summary>
        static bool Bright(Vector3 point)=>Dawn.Here?Dawn.Shade(point)||Dawn.Ahead(point)<Dawn.BandFrom:GroundHeat.Bright(point);
        /// <summary>It has fled hot ground since it woke: what a check reads.</summary>
        public int Fled{get;private set;}
        static readonly List<EnemyAmbusher> living=new();
        State state;AmbushSpot spot;float sink,sinkGoal,until;int bites;Vector3 rest;Renderer[] drawn;bool under,hopped;
        float Wake=>Or(tune.wake,3.2f);int Bites=>tune.count>0?tune.count:2;float Pace=>Or(tune.speed,5.5f);float SpentFor=>Or(tune.recover,1.4f);
        bool Disguised=>tune.disguised;
        MaterialPropertyBlock lampBlock;Color[] lamps;static readonly int EmissionId=Shader.PropertyToID("_EmissionColor");
        /// <summary>Its lamp now, as a share of its own light: what a check reads of a disguised one.</summary>
        public float LampLevel{get;private set;}
        /// <summary>How long it takes to go under and to come up, the tell of each bite after the first, how far it looks for a hide, and how close it comes before it bites.</summary>
        public const float Dive=.3f,Rise=.35f,NextBite=.5f,Search=16,BiteFrom=1.7f;
        const int MostMade=6;
        public bool Hidden=>state==State.Hidden;
        public AmbushSpot Spot=>spot;
        public override bool Movable=>state==State.Seeking||state==State.Spent||state==State.Fresh;
        public override bool Roams=>false;
        protected override void Begin()
        {
            living.Add(this);
            if(visual!=null){rest=visual.localPosition;drawn=visual.GetComponentsInChildren<Renderer>(true);}
            if(Disguised&&drawn!=null)
            {
                lamps=new Color[drawn.Length];
                for(int i=0;i<drawn.Length;i++){var material=drawn[i]!=null?drawn[i].sharedMaterial:null;lamps[i]=tune.tint.a>0?tune.tint:material!=null&&material.HasProperty(EmissionId)?material.GetColor(EmissionId):Color.black;}
            }
        }
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);
            // The ground says where it can't stay: its hide gone hot, or open melt where it stands or is going. Up and away to a cold one.
            if(Time.time>=fleeAgain&&(spot!=null&&(state==State.Hidden||state==State.Diving||state==State.Seeking)&&!Cold(spot.transform.position)||(state==State.Spent||state==State.Seeking&&!Dawn.Here)&&Bright(body.position))){Flee();return;}
            switch(state)
            {
                case State.Fresh:
                    spot=Choose(null);spot.Take(brain,Wake);state=State.Seeking;break;
                case State.Lunging:
                    // The length of its lane, fast: what it marked is what it struck as the lunge began.
                    float step=Mathf.Min(lungeLeft,tune.reach/LungeSeconds*Time.deltaTime);lungeLeft-=step;
                    if(controller!=null&&controller.enabled)controller.Move(lungeWay*step);
                    if(lungeLeft>.01f)return;
                    PassThrough(false);bites++;
                    if(bites<Bites){state=State.Closing;until=Time.time+.16f;}
                    else{state=State.Spent;until=Time.time+SpentFor;Debug.Log("AMBUSH_SPENT "+def.id);}
                    break;
                case State.Seeking:
                    var way=Flat(spot.transform.position-body.position);
                    // No step carries it past its hide: at a long frame (a hitch, a machine starved by another Unity) a crawler's 2 m step went from .84 m short to 1.16 m past and back
                    // again for ever, and it paced round a cold hide and never went under (HarrowgateCombatTests.AnAmbusherGoesUnderAtTheHideItWalksToHoweverLongAFrameIs).
                    if(way.magnitude>.6f){brain.Walk(way,Mathf.Min(Pace,way.magnitude/Mathf.Max(Time.deltaTime,.0001f)));Face(way,540);return;}
                    state=State.Diving;until=Time.time+Dive;sinkGoal=1;Splash(.8f);break;
                case State.Diving:
                    if(Time.time>=until){state=State.Hidden;Under(true);FirstHide();}
                    break;
                case State.Hidden:
                    foreach(var hero in PartyController.Current.members)
                    {
                        if(!hero.Health.Alive||!spot.Within(hero.transform.position))continue;
                        // A hunter of the dawn line won't come up into shade after anyone: shade is the answer to it.
                        if(Dawn.Here&&Dawn.Shade(hero.transform.position))continue;
                        Under(false);sinkGoal=0;state=State.Rising;until=Time.time+Rise;bites=0;Splash(1.25f);
                        var toward=Flat(hero.transform.position-body.position);if(toward.sqrMagnitude>.01f)body.rotation=Quaternion.LookRotation(toward);
                        Debug.Log("AMBUSH_ERUPT "+def.id);if(Disguised)BarkService.Play(hero.character,"lure");break;
                    }
                    break;
                case State.Rising:
                case State.Closing:
                    // It comes up at him, and closes again between bites: what it marks next it marks from where it has got to.
                    if(to.magnitude>BiteFrom){brain.Walk(to,Pace*2.2f);Face(to,720);}
                    if(Time.time>=until)Bite(victim,to);
                    break;
                case State.Biting:
                    if(!brain.TellRunOut)return;
                    var mark=brain.EndTell(.3f);
                    if(mark!=null){StrikeIn(mark,brain.Blow(attack,HitWeight.Medium),Chest);mark.Release();}
                    if(Lunges){state=State.Lunging;PassThrough(true);Sound("swing_1",.2f,.8f);Debug.Log("AMBUSH_LUNGE "+def.id);break;}
                    bites++;
                    if(bites<Bites){state=State.Closing;until=Time.time+.16f;}
                    else{state=State.Spent;until=Time.time+SpentFor;Debug.Log("AMBUSH_SPENT "+def.id);}
                    break;
                case State.Spent:
                    if(Time.time<until)return;
                    var next=Choose(spot);if(next!=spot){spot.Leave(brain);spot=next;spot.Take(brain,Wake);}
                    state=State.Seeking;break;
            }
        }
        void Bite(CombatActor victim,Vector3 to)
        {
            var aim=to.sqrMagnitude>.01f?to.normalized:body.forward;body.rotation=Quaternion.LookRotation(aim);
            float tell=bites==0?Tell:NextBite;
            if(Lunges)
            {
                // The lane it lunges down, stopping at a wall: what is painted is where it goes.
                float length=tune.reach;if(Wall(aim,length,out float gap))length=Mathf.Max(1,gap);
                // A hunter of the dawn line won't lunge into shade: a lane that runs into it is broken off, and it makes for the band.
                if(Dawn.Here&&(Dawn.Shade(body.position+aim*length)||Dawn.Shade(body.position+aim*length*.5f))){Flee();return;}
                lungeWay=aim;lungeLeft=Mathf.Max(0,length-.6f);
                Mark(Telegraph.Line(body.position,aim,length+(controller!=null?controller.radius:.5f),Mathf.Max(1.4f,tune.width),tell,Telegraph.Threat,true),tell,aim,victim,"water_splash",1.1f);
            }
            else Mark(Telegraph.Circle(body.position+aim*1.1f,Mathf.Max(1,Range*.7f),tell,Telegraph.Threat,true),tell,aim,victim,"water_splash",1.35f);
            state=State.Biting;
        }
        /// <summary>Hot ground under it or its hide: it comes up where it is and makes for the nearest cold hide, at once.</summary>
        void Flee()
        {
            if(under){Under(false);Splash(.9f);}sinkGoal=0;
            if(brain.Telegraphing)brain.Interrupt();
            var next=Choose(spot);if(next!=spot){if(spot!=null)spot.Leave(brain);spot=next;spot.Take(brain,Wake);}
            state=State.Seeking;Fled++;fleeAgain=Time.time+.8f;Debug.Log("AMBUSH_FLEES "+def.id);
        }
        float fleeAgain;
        /// <summary>A free hide other than the one it has: the nearest in reach, or one it makes for itself beside where it lives, or, with enough made already, the one it has.</summary>
        AmbushSpot Choose(AmbushSpot not)
        {
            var from=not!=null?not.transform.position:body.position;
            var found=AmbushSpot.Nearest(from,Search,not);if(found!=null&&Cold(found.transform.position))return found;
            // On ground the world says is hot, the nearest free hide that isn't (the first one found, if it was hot).
            if(found!=null)
            {
                found=null;float least=Search*Search;
                foreach(var other in AmbushSpot.All)
                {
                    if(other==null||other==not||!other.Free||!Cold(other.transform.position))continue;
                    var offset=other.transform.position-from;offset.y=0;if(offset.sqrMagnitude>=least)continue;least=offset.sqrMagnitude;found=other;
                }
                if(found!=null)return found;
            }
            // Where a dawn comes on, it hunts along the line: a hide of its own in the warm band across from whoever leads, moved there rather than made anew once it has made its share.
            if(Dawn.Here)
            {
                var lead=PartyController.Current!=null&&PartyController.Current.Active!=null?PartyController.Current.Active.transform.position:body.position;
                if(!Dawn.Into(lead,out var warm)&&!Dawn.Into(body.position,out warm))return not!=null?not:AmbushSpot.Make(body.position,Wake);
                if(!Flying){var ground=CombatCover.Ground(warm+Vector3.up*3);if(Mathf.Abs(ground.y-warm.y)<3)warm=ground;}
                if(not!=null&&not.made&&not.Free||not!=null&&not.made&&not.Occupant==brain){not.transform.position=warm;return not;}
                return AmbushSpot.Make(warm,Wake);
            }
            int made=0;foreach(var other in AmbushSpot.All)if(other!=null&&other.made)made++;
            if(not!=null&&made>=MostMade)return not;
            var at=not==null?body.position:brain.Home+Quaternion.Euler(0,made*137.5f,0)*Vector3.forward*(Wake*1.9f);
            // A hide of its own never goes on hot ground either: round its home until one is cold, or none.
            for(int turn=1;turn<6&&!Cold(at);turn++)at=brain.Home+Quaternion.Euler(0,made*137.5f+turn*60,0)*Vector3.forward*(Wake*1.9f);
            if(!Cold(at))return not!=null?not:AmbushSpot.Make(body.position,Wake);
            if(!Flying){var ground=CombatCover.Ground(at+Vector3.up*3);if(Mathf.Abs(ground.y-body.position.y)>1.2f)return not!=null?not:AmbushSpot.Make(body.position,Wake);at=ground;}
            return AmbushSpot.Make(at,Wake);
        }
        /// <summary>Set once the lead has said what a ringed hide is.</summary>
        public const string HideSeen="seen.ambushHide";
        /// <summary>The first time anything goes under in front of the party, whoever leads says what the ring is, once in a save. Nothing else told a player that a bank with nothing
        /// on it was a fight still going, or that stepping into the ring is how it goes on (Braided River, November 13). A disguised one has its own line as it springs.</summary>
        void FirstHide()
        {
            var services=GameServices.Current;var party=PartyController.Current;
            if(Disguised||services==null||services.Flags==null||party==null||party.Active==null||services.Flags.GetBool(HideSeen))return;
            services.Flags.SetBool(HideSeen,true);BarkService.Play(party.Active.character,"hide",true);
        }
        /// <summary>Under: out of reach, out of the way and out of sight, with its hide ringed. Up: a body again.</summary>
        void Under(bool hidden)
        {
            under=hidden;health.Damageable=!hidden;
            // A disguised one stays where it stands and as it looks: a lamp-post nobody can hurt, and as solid as one.
            if(!Disguised)
            {
                if(controller!=null)controller.enabled=!hidden;
                if(drawn!=null)foreach(var view in drawn)if(view!=null)view.enabled=!hidden;
            }
            if(spot!=null)spot.Show(hidden);
        }
        /// <summary>A disguised one's lamp: the lure's flicker while it waits, a blaze as it springs, embers while it is spent.</summary>
        void Lamp()
        {
            if(lamps==null||GameTime.Paused)return;
            LampLevel=state==State.Rising||state==State.Biting||state==State.Closing?2.2f:state==State.Spent?.3f:state==State.Seeking?.6f:LureStalk.Flicker(Time.time,brain.GetInstanceID());
            lampBlock??=new MaterialPropertyBlock();
            for(int i=0;i<drawn.Length;i++)
            {
                if(drawn[i]==null)continue;
                drawn[i].GetPropertyBlock(lampBlock);lampBlock.SetColor(EmissionId,lamps[i]*LampLevel);drawn[i].SetPropertyBlock(lampBlock);
            }
        }
        void Splash(float pitch)
        {
            var at=spot!=null?spot.transform.position:body.position;
            AudioManager.PlayAt("water_splash",spot!=null?spot.transform:null,at,.24f,AudioBus.SFX,100,.08f,pitch);
            CombatVfx.Burst(at+Vector3.up*.3f,new Color(.8f,.95f,.9f,.8f),"shape");
        }
        public override void Present()
        {
            if(visual==null)return;
            if(Disguised)
            {
                Lamp();
                // Between hides it hops, as a lamp-post on one leg would.
                bool hopping=state==State.Seeking&&!GameTime.Paused;
                if(hopping){visual.localPosition=rest+Vector3.up*(.45f*Mathf.Abs(Mathf.Sin(Time.time*9)));hopped=true;}
                else if(hopped){visual.localPosition=rest;hopped=false;}
                return;
            }
            float before=sink;if(!GameTime.Paused)sink=Mathf.MoveTowards(sink,sinkGoal,Time.deltaTime/Dive);
            if(sink<=0&&before<=0)return;
            float depth=(controller!=null?controller.height:1.5f)+.4f;
            visual.localPosition=rest+Vector3.down*(depth*Mathf.SmoothStep(0,1,sink));
        }
        public override void Cancel()
        {
            // A break or a reel takes its bites from it, and it has had its turn: it stands spent, then goes for a hide.
            if(state==State.Rising||state==State.Biting||state==State.Closing||state==State.Lunging){state=State.Spent;until=Time.time+SpentFor;PassThrough(false);}
        }
        public override void Fell(DamagePacket packet){Surface();Leave();}
        public override void Dispose(){Surface();Leave();}
        void Surface()
        {
            if(under)Under(false);sink=sinkGoal=0;if(visual!=null)visual.localPosition=rest;
        }
        void Leave()
        {
            if(!living.Remove(this))return;
            if(spot!=null){spot.Show(false);spot.Leave(brain);}
            // The hides it made go with the last thing that could use them.
            if(living.Count>0)return;
            for(int i=AmbushSpot.All.Count-1;i>=0;i--){var made=AmbushSpot.All[i];if(made!=null&&made.made)Object.Destroy(made.gameObject);}
        }
    }
}
