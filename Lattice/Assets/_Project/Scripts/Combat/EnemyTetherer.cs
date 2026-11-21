using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The tetherer (the Claim Drone; later the Tow-drone, the Interceptor, a Veil Ray's pull), in flight or on foot: it holds off at range, marks a line to a hero, and what it
    /// fires down the line is a tether. Caught, a hero moves at <c>tune.pace</c> of his speed and, with a <c>tune.speed</c>, is reeled toward it, while everything else in the
    /// fight has a held target. The line is drawn for as long as it holds. It lets go when he rolls, when anything hits the tetherer (the line is cut), when cover comes between
    /// them, when he gets <see cref="SnapsAt"/> times its reach away, or after <c>tune.linger</c> seconds. The catch itself is an ordinary marked hit and can be rolled like one.
    /// Reads: attacks[0] (telegraph, range to hold at, damage of the catch, cooldown), tune.reach (the line, the shot's own by default), pace (.5), speed (the reel, none),
    /// linger (how long it can hold, 5 s).
    /// </summary>
    public sealed class EnemyTetherer:EnemyBehaviour
    {
        static readonly List<EnemyTetherer> holding=new();
        static Material lineMaterial;
        static readonly Color LineTone=new(1.3f,.8f,.3f,.95f);
        CombatActor held;float until;LineRenderer line;
        readonly List<CombatActor> caught=new();
        float Reach=>Or(tune.reach,Mathf.Max(13,Range));float Pace=>Or(tune.pace,.5f);float Holds=>Or(tune.linger,5);
        public const float SnapsAt=1.5f,LineWidth=1.2f;
        public CombatActor Held=>held;
        /// <summary>A tow-drone's heading (the way it was set facing as it woke) and how far it has hauled its catch along it: what a check reads.</summary>
        Vector3 towWay;float towed;
        public Vector3 TowWay=>towWay;public float Towed=>towed;
        protected override void Begin(){towWay=Flat(body.forward).sqrMagnitude>.01f?Flat(body.forward).normalized:Vector3.forward;}
        public override bool Roams=>held==null;
        /// <summary>Whether any tether holds this hero now.</summary>
        public static bool On(CombatActor hero){foreach(var tether in holding)if(tether.held==hero)return true;return false;}
        public override void Tick(CombatActor victim)
        {
            if(held!=null){Hold();return;}
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(brain.Telegraphing)
            {
                if(!brain.TellRunOut)return;
                var mark=brain.EndTell();brain.Rest(Cooldown);if(mark==null)return;
                caught.Clear();StrikeIn(mark,brain.Blow(attack,HitWeight.Light),Chest,caught);mark.Release();
                CombatAudio.HostileShot(body,Chest);
                if(caught.Count>0)Attach(caught[0]);
                return;
            }
            if(distance<Range&&brain.Rested&&brain.Sees(victim))
            {
                var aim=distance>.01f?to/distance:body.forward;body.rotation=Quaternion.LookRotation(aim);
                Mark(Telegraph.Line(body.position,aim,Reach,LineWidth,Tell,Telegraph.Threat,true),Tell,aim,victim,"laserSmall_000",1.7f,true);
                return;
            }
            brain.Spread(to,distance,Range);
            if(distance>.01f)body.rotation=Quaternion.LookRotation(to);
            // It holds the far end of its reach, as a shooter does, and comes round a wall for a line.
            if(distance>Range*.85f)brain.Walk(to,def.speed);else if(distance<Range*.4f)brain.Walk(-to,def.speed);
        }
        void Attach(CombatActor hero)
        {
            held=hero;until=Time.time+Holds;holding.Add(this);
            if(lineMaterial==null){lineMaterial=new Material(Resources.Load<Material>("Effects/flare_01"));lineMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);}
            var go=new GameObject("Tether line");line=go.AddComponent<LineRenderer>();line.positionCount=2;line.useWorldSpace=true;line.startWidth=.16f;line.endWidth=.1f;
            line.sharedMaterial=lineMaterial;line.startColor=LineTone;line.endColor=new Color(LineTone.r,LineTone.g,LineTone.b,.7f);line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Draw();AudioManager.PlayAt("forceField_000",null,hero.transform.position,.2f,AudioBus.SFX,100,.1f,1.25f);
            Debug.Log("TETHER_ON "+def.id);
        }
        void Hold()
        {
            var hero=held;var gap=Flat(body.position-hero.transform.position);float distance=gap.magnitude;
            string why=!hero.Health.Alive?"down":hero.State==ActorState.Dodge?"rolled":Time.time>=until?"spent":distance>Reach*SnapsAt?"stretched":
                !CombatCover.Clear(Chest,hero.transform.position+Vector3.up*(hero.flight?0:.9f))?"cover":null;
            if(why!=null){Free(why);return;}
            hero.Health.Hinder(Pace);
            // Reeled, never onto its muzzle: it stops him a few metres short.
            if(tune.speed>0&&distance>3.5f&&hero.TryGetComponent<CharacterController>(out var reeled)&&reeled.enabled)reeled.Move(gap/distance*(tune.speed*Time.deltaTime));
            // A tow-drone hauls its catch the way it was set facing, as it hauls hulls, the line taut behind it: its controller stops at a wall, so it drags nobody through one.
            if(tune.tow>0&&controller!=null&&controller.enabled){controller.Move(towWay*(tune.tow*Time.deltaTime));towed+=tune.tow*Time.deltaTime;}
            if(tune.tow>0){if(towWay.sqrMagnitude>.01f)body.rotation=Quaternion.RotateTowards(body.rotation,Quaternion.LookRotation(towWay),240*Time.deltaTime);}
            else if(distance>.01f)body.rotation=Quaternion.LookRotation(-gap);
            Draw();
        }
        void Draw()
        {
            if(line==null||held==null)return;
            line.SetPosition(0,Chest);line.SetPosition(1,held.transform.position+Vector3.up*(held.flight?0:.9f));
        }
        void Free(string why)
        {
            if(held==null)return;
            var at=held.transform.position;held=null;holding.Remove(this);if(line!=null)Object.Destroy(line.gameObject);line=null;
            brain.Rest(Cooldown);AudioManager.PlayAt("doorOpen_000",null,at,.16f,AudioBus.SFX,100,.1f,1.6f);
            Debug.Log($"TETHER_OFF {def.id} {why}");
        }
        public override void Present(){Draw();}
        public override void Struck(DamagePacket packet,float amount){if(amount>0)Free("cut");}
        public override void Cancel()=>Free("broken");
        public override void Fell(DamagePacket packet)=>Free("cut");
        public override void Dispose(){held=null;holding.Remove(this);if(line!=null)Object.Destroy(line.gameObject);}
    }
}
