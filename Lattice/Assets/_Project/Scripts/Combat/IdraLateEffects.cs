using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Take the Weight (S6a, Sync 16): for six seconds every enemy hit that would land on Idra's partner lands on her instead (Health.Covering), whole, through her
    /// own Plating and her brace if it's up, a Flash Guard and Counterweight answering as for a hit on her. It asks only of a hit that would have landed: one her partner
    /// rolls or Flashes was theirs to avoid. A cable runs between them while it holds; past its reach it hangs slack, grey, and covers nothing. What nobody dealt (the
    /// cold, rising water) is never hers to take.</summary>
    public sealed class WeightLine:MonoBehaviour
    {
        public const float Seconds=6,Reach=16;
        static readonly List<WeightLine> Active=new();
        public static WeightLine Latest{get;private set;}
        /// <summary>Hits it has taken off the partner.</summary>
        public int Taken{get;private set;}
        public CombatActor Owner=>owner;public CombatActor Covered=>covered;
        CombatActor owner,covered;float age;LineRenderer line;Material material;
        static readonly Color Taut=new(1.7f,.75f,.2f,.85f),Slack=new(.5f,.45f,.4f,.35f);
        public static WeightLine Take(CombatActor owner)
        {
            // One at a time: a second cast starts the six seconds again.
            foreach(var old in Active.ToArray())if(old!=null&&old.owner==owner)Destroy(old.gameObject);
            var go=new GameObject("Take the Weight",typeof(WeightLine));var weight=go.GetComponent<WeightLine>();weight.owner=owner;
            var party=PartyController.Current;
            if(party!=null)foreach(var member in party.members)if(member!=null&&member!=owner&&member.gameObject.activeInHierarchy){weight.covered=member;break;}
            weight.line=go.AddComponent<LineRenderer>();weight.line.positionCount=2;weight.line.useWorldSpace=true;weight.line.startWidth=weight.line.endWidth=.07f;
            weight.material=new Material(Resources.Load<Material>("Effects/flare_01"));weight.material.SetTexture("_BaseMap",Texture2D.whiteTexture);
            weight.line.sharedMaterial=weight.material;weight.line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Active.Add(weight);Latest=weight;Health.Covering=Cover;
            if(weight.covered!=null)
            {
                var at=weight.covered.transform.position;
                SkillRing.Expand(weight.covered.flight?at:CombatCover.Ground(at+Vector3.up),1.6f,GroundRing.Friendly,.5f,weight.covered.flight);
                CombatVfx.Burst(at+Vector3.up*(weight.covered.flight?.3f:1),new Color(1.6f,.8f,.25f),"shape");
            }
            weight.Draw();
            return weight;
        }
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        /// <summary>Whether it's taking her partner's hits this moment: both standing, its seconds not run, and the two of them within its reach.</summary>
        public bool Holds=>owner!=null&&owner.Health.Alive&&covered!=null&&covered.Health.Alive&&age<Seconds&&Flat(owner.transform.position,covered.transform.position)<=Reach;
        static Health Cover(Health hero)
        {
            foreach(var weight in Active)
            {
                if(weight==null||weight.covered==null||weight.covered.Health!=hero||!weight.Holds)continue;
                weight.Taken++;CombatVfx.Burst(weight.owner.transform.position+Vector3.up*(weight.owner.flight?.3f:1.2f),new Color(1.6f,.8f,.25f),"hit");
                return weight.owner.Health;
            }
            return null;
        }
        Vector3 Chest(CombatActor hero)=>hero.transform.position+Vector3.up*(hero.flight?.3f:1.1f);
        void Draw()
        {
            if(owner==null||covered==null){line.enabled=false;return;}
            line.enabled=true;line.SetPosition(0,Chest(owner));line.SetPosition(1,Chest(covered));
            var colour=Holds?Taut:Slack;line.startColor=line.endColor=colour;
        }
        void Update()
        {
            if(!GameTime.CombatHeld)age+=Time.deltaTime;
            if(age>=Seconds||owner==null||!owner.Health.Alive){Destroy(gameObject);return;}
            Draw();
        }
        void OnDestroy()
        {
            Active.Remove(this);if(Latest==this)Latest=null;if(material!=null)Destroy(material);
            if(Active.Count==0&&Health.Covering==(System.Func<Health,Health>)Cover)Health.Covering=null;
        }
    }

    /// <summary>Bulkhead (S6a, Sync 24): a wall of hard light Idra slams down across her aim, standing six seconds. While it stands, an enemy's shot that meets it burns out
    /// (and a marking that was carrying that shot goes with it, as at Shelter), and an ordinary body that walks into it is held on its own side. It never takes back a
    /// marked attack (a body telling one or making it goes where its paint said), never holds a heavy body or a boss (they're slowed through it), and never stops a hero
    /// or a hero's shot. It runs before the shots do each frame, so a shot is stopped on the step that would have carried it through.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class BulkheadWall:MonoBehaviour
    {
        /// <summary>How wide it stands on foot and in flight, how tall, how long, how far either side of it a held body is kept, and the pace a heavy body or a boss is
        /// slowed to while it pushes through.</summary>
        public const float Width=7,FlightWidth=9,Height=2.6f,Seconds=6,Hold=.8f,HeavyPace=.4f;
        public static BulkheadWall Latest{get;private set;}
        /// <summary>Shots it has stopped, and the bodies it has held back.</summary>
        public int Stopped{get;private set;}
        public int Held=>held.Count;
        /// <summary>Along its face, and half its width.</summary>
        public Vector3 Along{get;private set;}
        public float Half{get;private set;}
        /// <summary>The way it faces: away from her, at the time it was raised.</summary>
        public Vector3 Normal{get;private set;}
        float age;bool flight;Transform slab;Material glass;
        readonly Dictionary<EnemyBrain,float> sides=new();readonly HashSet<EnemyBrain> held=new();
        static readonly List<Projectile> meeting=new();
        static readonly Color Light=new(1.5f,.62f,.16f,.32f);
        public static BulkheadWall Raise(CombatActor owner,Vector3 centre,Vector3 aim,bool flight)
        {
            aim.y=0;aim=aim.sqrMagnitude>.01f?aim.normalized:Vector3.forward;
            var go=new GameObject("Bulkhead",typeof(BulkheadWall));go.transform.SetPositionAndRotation(flight?centre:CombatCover.Ground(centre+Vector3.up*2),Quaternion.LookRotation(aim,Vector3.up));
            var wall=go.GetComponent<BulkheadWall>();wall.flight=flight;wall.Normal=aim;wall.Along=Vector3.Cross(Vector3.up,aim).normalized;wall.Half=(flight?FlightWidth:Width)*.5f;Latest=wall;
            // A slab with no collider: it has to be seen, and nothing but what it decides to stop may be stopped by it.
            var slab=new GameObject("Slab",typeof(MeshFilter),typeof(MeshRenderer));slab.transform.SetParent(go.transform,false);
            slab.transform.localPosition=Vector3.up*(flight?0:Height*.5f);slab.transform.localScale=new Vector3(wall.Half*2,flight?2:Height,.22f);
            slab.GetComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            wall.glass=ActorFactory.Energy(Light);var view=slab.GetComponent<MeshRenderer>();view.sharedMaterial=wall.glass;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
            wall.slab=slab.transform;
            // Who stands which side of it as it comes down; a body under it goes to the far side (the slam shoves the light ones there).
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive||!body.TryGetComponent<EnemyBrain>(out var brain))continue;
                float across=Vector3.Dot(Flat(body.transform.position-go.transform.position),aim);wall.sides[brain]=across<-Hold?-1:1;
            }
            HeroSkills.Sound(owner,go.transform.position+Vector3.up,"forceField_000",.24f,.55f);
            return wall;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        /// <summary>Whether a point stands within its width, wherever it is either side of it.</summary>
        public bool Spans(Vector3 point)=>Mathf.Abs(Vector3.Dot(Flat(point-transform.position),Along))<=Half;
        void Update()
        {
            if(GameTime.CombatHeld)return;
            age+=Time.deltaTime;if(age>=Seconds){Destroy(gameObject);return;}
            float dt=Time.deltaTime;var centre=transform.position;
            meeting.Clear();
            foreach(var shot in Projectile.Live)
            {
                if(shot==null||!shot.Threatens(true))continue;
                var from=shot.transform.position-centre;var step=shot.Velocity*dt;
                float before=Vector3.Dot(Flat(from),Normal),after=Vector3.Dot(Flat(from+step),Normal);
                // It crosses the plane this step, or stands in the slab's thickness.
                if(before*after>0&&Mathf.Abs(before)>.15f)continue;
                float t=Mathf.Abs(before-after)<1e-5f?0:Mathf.Clamp01(before/(before-after));var at=from+step*t;
                if(Mathf.Abs(Vector3.Dot(Flat(at),Along))>Half)continue;
                if(flight?Mathf.Abs(at.y)>2:at.y<-.6f||at.y>Height+.4f)continue;
                meeting.Add(shot);
            }
            foreach(var shot in meeting){shot.Spend();Stopped++;}
            meeting.Clear();
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive||!body.TryGetComponent<EnemyBrain>(out var brain)||!brain.enabled)continue;
                var from=Flat(body.transform.position-centre);float sideways=Vector3.Dot(from,Along),across=Vector3.Dot(from,Normal);
                if(!sides.TryGetValue(brain,out float side)){side=across<0?-1:1;sides[brain]=side;}
                if(Mathf.Abs(sideways)>Half+.4f||Mathf.Abs(across)>=Hold)continue;
                // Heavy bodies and bosses go through, slowly; a body telling or making an attack goes where it painted.
                if(brain.definition!=null&&brain.definition.boss||brain.Heavy||!brain.CanBeMoved){body.Hinder(HeavyPace);continue;}
                if(brain.Telegraphing||brain.Attacking)continue;
                brain.Displace(Normal*(side*Hold-across));held.Add(brain);
            }
            // It breathes while it stands and goes in its last half second.
            float left=Seconds-age;slab.localScale=new Vector3(Half*2,(flight?2:Height)*Mathf.Clamp01(left/.5f),.22f+.03f*Mathf.Sin(age*6));
        }
        void OnDestroy(){if(Latest==this)Latest=null;}
    }
}
