using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The rider (chapter 7: the Fibre Wisp on the Skyfields' and the Glass Choir's fibres, the Rail Warden on the Orrery's light-rails): a body that keeps to a line force runs down
    /// (<see cref="ForceLine"/>) and strikes only along it. It glides along its line toward the point of it nearest the hero; when a hero stands on the line within its reach it lights
    /// the line from itself toward him (the marked strip is the tell: the fibre brightens) and the force runs down it, or, if its attack says <c>projectile</c>, a bolt flies down it
    /// and the marking goes with the bolt. A body off the line is never struck. So the answer is to step off a lit line, and not to stand on one.
    /// <b>A jumper</b> (<c>tune.count</c> over nought: the wisp) hops to the line nearest the hero after each strike, and whenever the hero has kept off its line a while: the hop takes
    /// <c>tune.recover</c> seconds, and through it the body is open (a blow lands at half again). <b>A rail rider</b> (count nought: the Rail Warden) never leaves its rail; it slides back
    /// along it after a shot and rests. With no line within <c>tune.wake</c> it makes its own through where it stands (toward the hero for a jumper, along its own heading for a rail rider),
    /// so it fights anywhere.
    /// Reads: attacks[0] (telegraph, damage, cooldown, projectile); tune.reach (how far down the line a strike runs, 12), width (the strip, the line's own), recover (the hop, 1.1),
    /// count (over nought it hops; a body with no model is also drawn as that many motes), wake (how far it looks for a line, 16), tint (the motes), bark; its glide is the definition's speed.
    /// <para><b>A pour that stays</b> (<c>tune.linger</c> over nought: chapter 8's Pour Drone, a ladle on an overhead rail): the strip it strikes is left as a <see cref="LingeringArea"/> for that
    /// many seconds, of <c>tune.effect</c> (a Burn's <c>tick</c> a second, a Slow's <c>pace</c>). Step out from under its rail, and not back onto what it poured.</para>
    /// </summary>
    public sealed class EnemyRider:EnemyBehaviour
    {
        enum State{Ride,Told,Hop}
        State state;ForceLine line,target;Vector3 hopFrom,hopTo,strikeWay;float hopT,offLineSince=-1;
        readonly List<ForceLine> made=new();
        float Reach=>Or(tune.reach,12);float Strip=>tune.width>0?tune.width:line!=null?line.width:1.6f;float Hop=>Or(tune.recover,1.1f);float Looks=>Or(tune.wake,16);
        bool Jumper=>tune.count>0;
        /// <summary>One that keeps to a line keeps its place: it isn't walked home.</summary>
        public override bool Roams=>false;
        public override bool Movable=>!Jumper;
        /// <summary>The line it rides now, and whether it is in the air between two.</summary>
        public ForceLine Line=>line;
        public bool Hopping=>state==State.Hop;
        protected override void Begin()
        {
            // A wisp is its motes (ActorFactory): their colour is the definition's.
            if(visual!=null&&tune.tint.a>0&&visual.TryGetComponent<SwarmMotes>(out var motes))motes.tone=tune.tint;
            line=ForceLine.Nearest(body.position,Looks,false,null,Usable);
            if(line==null&&!Jumper)line=OwnRail();
        }
        /// <summary>A line it can ride: one laid where it is (the floor it walks, or the flight plane it flies), not one another body made for itself.</summary>
        bool Usable(ForceLine candidate)=>Mathf.Abs(candidate.From.y-body.position.y)<2.5f&&(!candidate.Made||made.Contains(candidate));
        /// <summary>A rail rider's own rail where none is laid: along its heading, the way it was set facing (the Orrery stands its wardens facing along their rail), and through it, half its reach
        /// behind as well as its reach ahead, as a laid rail runs through whatever rides it. Begun at its body, a hero two metres behind it was never on its rail and it never fought at all
        /// (QaPlanSystemsTests.EveryOrdinaryEnemysWarningSoundHasAMarkOnTheGround, November 12: the Rail Warden and the Pour Drone).</summary>
        ForceLine OwnRail(){var ahead=Flat(body.forward).normalized;return Own(body.position-ahead*(Reach*.5f),body.position+ahead*(Reach+2));}
        ForceLine Own(Vector3 from,Vector3 to)
        {
            from.y=to.y=body.position.y+(Flying?0:.05f);
            var own=ForceLine.Make(def.id+" line",from,to,tune.width>0?tune.width:1.6f);made.Add(own);
            // A jumper keeps two of its own at most: the one it is on and the one it is going to.
            while(made.Count>3){if(made[0]!=null&&made[0]!=line&&made[0]!=target)Object.Destroy(made[0].gameObject);made.RemoveAt(0);}
            return own;
        }
        /// <summary>The line to go to for a hero: the nearest one to him, or one of its own through him and where it is.</summary>
        ForceLine LineFor(CombatActor victim)
        {
            var at=victim.transform.position;
            var near=ForceLine.Nearest(at,Strip*.5f+1.2f,false,null,Usable);if(near!=null)return near;
            var way=Flat(at-body.position);if(way.sqrMagnitude<.01f)way=body.forward;way.Normalize();
            return Own(body.position-way*2,at+way*(Reach*.5f));
        }
        public override void Tick(CombatActor victim)
        {
            if(line==null)line=Jumper?LineFor(victim):OwnRail();
            switch(state)
            {
                case State.Told:
                    Posing=true;Swell=.25f*brain.TellProgress;
                    if(!brain.TellRunOut)return;
                    var mark=brain.EndTell(.3f);line.Dim();
                    var packet=brain.Blow(attack,HitWeight.Medium);
                    if(attack!=null&&attack.projectile&&mark!=null)
                    {
                        packet=ProjectileProvenance.Volley(packet);CombatAudio.HostileShot(body,Chest);
                        mark.Carry(new[]{Projectile.Fire(Chest,strikeWay,packet,18,mark.Length)});
                    }
                    else if(mark!=null)
                    {
                        StrikeIn(mark,packet,Chest);mark.Release();
                        // What it poured stays where it struck (chapter 8's Pour Drone): the strip burns, or slows, for its linger. Drawn as a danger only if it hurts (D217).
                        if(tune.linger>0)Leave(LingeringArea.LayLine(mark.Origin,mark.Forward,mark.Length,mark.Width,tune.linger,tune.effect,Or(tune.pace,.6f),tune.tick,health,attack!=null?attack.type:DamageType.Kinetic));
                        for(int i=1;i<=3;i++)CombatVfx.Burst(mark.Origin+mark.Forward*(mark.Length*i/4f)+Vector3.up*(Flying?0:.3f),Glint,"kill");
                        Sound("laserSmall_000",.2f,1.5f);
                    }
                    Posing=false;Swell=0;brain.Rest(Cooldown);
                    if(Jumper)BeginHop(victim);
                    else{state=State.Ride;slideBack=Time.time+.6f;}
                    return;
                case State.Hop:return;
            }
            // Riding: on its line, toward the point of it nearest the hero.
            var hero=victim.transform.position;bool on=line.Live&&line.Under(hero);
            float mine=line.Along(body.position),his=line.Along(hero);
            if(on&&Mathf.Abs(his-mine)<=Reach&&Flat(hero-body.position).magnitude<=Reach+Strip&&brain.Rested&&brain.Sees(victim)){LightUp(victim,mine,his);return;}
            if(Jumper)
            {
                // Off its line: it waits a beat, then makes for the line he is on (or one through him).
                if(on)offLineSince=-1;else if(offLineSince<0)offLineSince=Time.time;
                if(!on&&offLineSince>=0&&Time.time-offLineSince>JumpAfter&&brain.Rested){BeginHop(victim);return;}
            }
            else if(!line.Live){return;}
            Glide(Time.time<slideBack?mine-Mathf.Sign(his-mine)*3:his-Mathf.Sign(his-mine)*Mathf.Min(Reach*.5f,Mathf.Abs(his-mine)));
        }
        /// <summary>How long a jumper waits with the hero off its line before it makes for his.</summary>
        public const float JumpAfter=1.2f;
        float slideBack;
        static readonly Color Glint=new(.86f,.96f,1.3f,.9f);
        /// <summary>Along its line to a place on it, and kept on it.</summary>
        void Glide(float along)
        {
            float length=line.Length;along=Mathf.Clamp(along,0,length);
            var goal=Vector3.Lerp(line.From,line.To,length>.01f?along/length:0);goal.y=body.position.y;
            var way=Flat(goal-body.position);
            if(way.magnitude>.3f)brain.Walk(way,def.speed);
            // Held to the strip: what it drifted off the line it is drawn back.
            var onLine=line.Closest(body.position);onLine.y=body.position.y;var off=onLine-body.position;
            if(off.sqrMagnitude>.04f&&controller.enabled)controller.Move(off*Mathf.Min(1,6*Time.deltaTime));
            if(way.sqrMagnitude>.01f)body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(way.normalized),240*Time.deltaTime);
        }
        void LightUp(CombatActor victim,float mine,float his)
        {
            strikeWay=line.Direction*(his>=mine?1:-1);
            // From itself down the line toward him, as far as its reach or the line's end.
            float left=his>=mine?line.Length-mine:mine;float length=Mathf.Clamp(left+.5f,2,Reach);
            var from=body.position;from.y=Flying?body.position.y:line.Closest(body.position).y;
            if(!Jumper)body.rotation=Quaternion.LookRotation(strikeWay);
            Mark(Telegraph.Line(from,strikeWay,length,Strip,Tell,Telegraph.Threat,true),Tell,strikeWay,victim,"forceField_000",attack!=null&&attack.projectile?.8f:1.7f,attack!=null&&attack.projectile);
            line.Light(Tell);state=State.Told;
            if(!string.IsNullOrEmpty(tune.bark)&&PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,tune.bark);
            Debug.Log("RIDER_LIT "+def.id);
        }
        void BeginHop(CombatActor victim)
        {
            target=LineFor(victim);if(target==line&&line.Live&&line.Under(victim.transform.position)){state=State.Ride;return;}
            hopFrom=body.position;hopTo=target.Closest(victim.transform.position);
            // Land a step short of him on it, not on top of him.
            var back=Flat(hopFrom-hopTo);if(back.sqrMagnitude>.01f)hopTo+=back.normalized*Mathf.Min(3,back.magnitude*.5f);
            hopTo=target.Closest(hopTo);hopTo.y=body.position.y;hopT=0;state=State.Hop;
            Open(Hop);Sound("forceField_000",.18f,2.2f);Debug.Log("RIDER_HOP "+def.id);
        }
        /// <summary>The hop is carried out here, late every frame, because through it the body is open and the brain ticks nothing open.</summary>
        public override void Present()
        {
            if(state!=State.Hop||GameTime.Paused||!health.Alive)return;
            hopT+=Time.deltaTime/Hop;float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(hopT));
            var at=Vector3.Lerp(hopFrom,hopTo,k)+Vector3.up*(Flying?0:Mathf.Sin(k*Mathf.PI)*1.2f);
            if(controller.enabled)controller.Move(at-body.position);
            Posing=true;Swell=-.2f*Mathf.Sin(k*Mathf.PI);
            if(hopT<1)return;
            Land();Debug.Log("RIDER_ON "+def.id);
        }
        void Land(){if(target!=null)line=target;target=null;state=State.Ride;Posing=false;Swell=0;offLineSince=-1;}
        /// <summary>Taken out of its strike: back to riding. A hop is never cancelled: struck in the air, as it is meant to be, it still lands where it was going.</summary>
        public override void Cancel(){if(state==State.Hop)return;if(line!=null)line.Dim();if(state==State.Told)state=State.Ride;Posing=false;Swell=0;}
        // A machine under override (tune.standsDown: the Weld Arm) is given back, not broken: its drawn body stays at rest where it worked (MachineAtRest).
        public override void Fell(DamagePacket packet){TakeUp();if(tune.standsDown)MachineAtRest.Leave(visual,def.id);}
        public override void Dispose(){foreach(var own in made)if(own!=null)Object.Destroy(own.gameObject);made.Clear();TakeUp();}
    }
}
