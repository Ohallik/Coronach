using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>Visual finish and recoverable bodies outlive lethal gameplay resolution.</summary>
    [DefaultExecutionOrder(600)]
    public sealed class DefeatPresentation : MonoBehaviour
    {
        public Transform visual;
        public event System.Action Recovered;
        /// <summary>A body that gave up rather than died has stopped being a fighter here: id, where, facing.</summary>
        public static event System.Action<string,Vector3,Quaternion> Yielded;
        bool yields;
        /// <summary>A mine has no body left to lie down: it swells, bursts and is gone. The Bellows has no rig: its breath goes out of it.</summary>
        bool bursts,deflates;
        /// <summary>The size a mine bursts from, as a share of its rest: what its tell had swollen it to.</summary>
        float burstFrom=1;
        Vector3 visualScale,flatScale;
        bool Flier=>enemy!=null&&(enemy.definition.id.StartsWith("Chorister")||enemy.definition.id=="MusterSkiff");
        Health health;CombatActor actor;EnemyBrain enemy;
        Collider[] colliders;bool[] collisionEnabled;
        bool down,recovering,rigid;
        float started,duration,hold;
        // Corpses leave deliberately: a short settle-and-shrink with a puff of
        // dust, never a full-size body vanishing between two frames.
        public const float Cleanup=.45f;
        bool cleaning;Vector3 cleanupScale,cleanupPosition;
        Vector3 origin,terminal;
        Quaternion originRotation,terminalRotation;
        Vector3 supportPoint,supportNormal;Vector3[] supportVertices;
        Transform[] sections;Vector3[] sectionOrigins;Quaternion[] sectionRotations;
        FlightFailurePresentation flightFailure;
        CantorRelease cantorRelease;
        BossDeparture departure;
        /// <summary>Bosses whose own ending already takes their light and paint: the Burrower burns out to its wreck's tint (BurrowerShutdown, BossWreck), the Host Marshal is taken by
        /// the frost, and the Last Warden stands down unbeaten.</summary>
        static readonly HashSet<string> OwnColour=new(){"Burrower","HostMarshal","LastWarden"};
        BodyGoesDark dark;bool knockedClear;
        void Awake()
        {
            health=GetComponent<Health>();actor=GetComponent<CombatActor>();enemy=GetComponent<EnemyBrain>();
            health.Died+=BeginDown;health.Revived+=BeginRecovery;
        }
        void Start(){if(visual!=null)visualScale=visual.localScale;if(!health.Alive)BeginDown(health,default);}
        void OnDestroy(){if(health!=null){health.Died-=BeginDown;health.Revived-=BeginRecovery;}}
        void BeginDown(Health _,DamagePacket packet)
        {
            if(down)return;
            down=true;recovering=false;started=GameTime.Now;
            bool flier=Flier;
            duration=actor!=null?CombatActor.DownDuration:enemy.definition.boss?1.5f:
                enemy.definition.id=="SentinelHusk"||enemy.definition.id.StartsWith("Muster")&&enemy.definition.id!="MusterSkiff"?1.15f:enemy.definition.id=="Ridgehound"||enemy.definition.id=="Gloomgrazer"?.9f:flier?1.1f:.75f;
            hold=enemy!=null&&enemy.definition.boss?1.65f:flier?.6f:1.15f;
            yields=enemy!=null&&enemy.definition.yieldsOnDefeat;
            if(yields){duration=.35f;hold=.75f;}
            bursts=enemy!=null&&enemy.Mine;deflates=enemy!=null&&enemy.definition.id=="BellowsBelow";
            if(bursts){duration=.62f;hold=0;}
            colliders=GetComponentsInChildren<Collider>(true);collisionEnabled=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++)
            {
                collisionEnabled[i]=colliders[i].enabled;
                // A recoverable ship still occupies space. Its damage trigger
                // shuts down, while its motionless hull stops a rescuer passing
                // through it. Ground corpses and enemies retain their cleanup.
                // A killed body never blocks the way (QaPlanCombatTests), the Rotbloom's included: D343's "solid until it is gone" never held (EnemyBrain switched the capsule off
                // after this), and a huge body draws in off whoever stood at it instead (D355).
                colliders[i].enabled=actor!=null&&actor.flight&&colliders[i] is CharacterController&&collisionEnabled[i];
            }
            foreach(var motion in GetComponentsInChildren<ProceduralMotion>())motion.enabled=false;
            if(TryGetComponent<SerpentSegments>(out var segments))
            {
                segments.enabled=false;sections=new Transform[segments.Parts.Count];
                sectionOrigins=new Vector3[sections.Length];sectionRotations=new Quaternion[sections.Length];
                for(int i=0;i<sections.Length;i++)
                {sections[i]=segments.Parts[i];if(sections[i]!=null){sectionOrigins[i]=sections[i].localPosition;sectionRotations[i]=sections[i].localRotation;}}
            }
            var animator=GetComponentInChildren<Animator>();
            rigid=!yields&&(animator==null||!animator.isHuman||actor!=null&&actor.flight);
            if(animator!=null)
            {
                animator.updateMode=AnimatorUpdateMode.UnscaledTime;
                if(rigid)animator.enabled=false;
            }
            if(actor!=null&&actor.flight)
            {
                var form=GetComponent<FormController>();form.FinishForDefeat();visual=form.flight.transform;
                flightFailure=GetComponent<FlightFailurePresentation>()??gameObject.AddComponent<FlightFailurePresentation>();
                flightFailure.Begin(actor,visual,packet);return;
            }
            if(enemy!=null&&enemy.definition.id=="Cantor"&&visual!=null)
            {
                cantorRelease=GetComponent<CantorRelease>()??gameObject.AddComponent<CantorRelease>();
                cantorRelease.Begin(visual,GetComponent<SerpentSegments>().Parts);return;
            }
            // Driven off, it leaves on its own feet: no body lies down here.
            if(enemy!=null&&visual!=null&&TryGetComponent(out departure)){departure.Begin(visual,packet);return;}
            // Killed, a boss goes dark at once: its lights go out and its colour drains (to ash if it is destroyed, cold if it is a machine shut down). Not what yields, leaves or is freed.
            if(enemy!=null&&enemy.definition.boss&&!yields&&visual!=null&&!OwnColour.Contains(enemy.definition.id))
            {
                var ending=BossController.EndingOf(enemy.definition.id);
                if(ending==BossController.Ending.Destroyed||ending==BossController.Ending.ShutDown){dark=gameObject.AddComponent<BodyGoesDark>();dark.Begin(transform,ending==BossController.Ending.Destroyed);}
            }
            if(!rigid||visual==null)return;
            origin=visual.localPosition;originRotation=visual.localRotation;
            if(visualScale==Vector3.zero)visualScale=visual.localScale;
            if(bursts){terminal=origin;terminalRotation=originRotation;burstFrom=visualScale.x>0?visual.localScale.x/visualScale.x:1;return;}
            bool airborne=actor!=null&&actor.flight||Flier;
            terminalRotation=originRotation*Quaternion.Euler(airborne?12:8,0,airborne?58:enemy!=null&&enemy.definition.boss?42:78);
            terminal=origin+Vector3.down*(airborne?.22f:.12f);
            // It draws in as it empties. Spread wider than it lived, it swallowed whoever had been fighting beside it, and they stood in its ribs for as long as it lay there: its body is 2.6 m out
            // where its capsule stops a hero at 1.75 m. At this width someone at the capsule stands on the rim, and anyone a little further stands clear.
            // D355: to six tenths, inside where its capsule stops a hero (1.75 m of its 2.6), so nobody at its side is left standing in its shell as it goes down.
            if(deflates){terminalRotation=originRotation;flatScale=new Vector3(visualScale.x*.6f,visualScale.y*.24f,visualScale.z*.6f);}
            // A boss's body ends as what it is. Tipped over by 42 degrees like an ordinary body, a huge one swung its top metres across the floor the party stood on, and the
            // Rotbloom, a rooted bloom the size of a room, stood on whole with the heroes inside it (s26/457). What shuts down (a machine, a frame) stops where it stands and
            // bows a little; what is huge and destroyed (the bloom, the crucible on legs) wilts and comes down where it stands, folding into itself. What leaves is BossDeparture's,
            // what yields stands where it gave up; only an ordinary body falls over. A break or a stagger never comes here: this runs on death alone.
            else if(enemy!=null&&enemy.definition.boss&&!airborne)
            {
                var ending=BossController.EndingOf(enemy.definition.id);
                bool huge=TryGetComponent<CharacterController>(out var capsule)&&(capsule.height>=4.5f||capsule.radius>=2.2f);
                if(ending==BossController.Ending.ShutDown){terminalRotation=originRotation*Quaternion.Euler(5,0,2);terminal=origin+Vector3.down*.06f;}
                // Folding into itself means drawing in, not only down: squashed in height alone the Rotbloom, a flat bloom under a camera that looks down on it, stood whole and
                // full size with Taren in its petals until it was cleared away (nov22-whole s26/454 to 469). Drawn in to half its reach (the Rotbloom's 4.5 m to 2.25, inside its 3.4 m
                // capsule) the petals fold off whoever stood among them in the first second.
                else if(huge){terminalRotation=originRotation*Quaternion.Euler(4,0,3);deflates=true;flatScale=new Vector3(visualScale.x*.5f,visualScale.y*.4f,visualScale.z*.5f);}
            }
            // Drawn in round the middle of what it draws, not round its root: a body hung off-centre on its root would otherwise be carried across the floor as it folds.
            if(deflates&&visualScale.x>0&&flatScale.x<visualScale.x)
            {
                bool any=false;var drawn=new Bounds();
                foreach(var r in visual.GetComponentsInChildren<Renderer>())if(r.enabled&&(r is MeshRenderer||r is SkinnedMeshRenderer)){if(!any){drawn=r.bounds;any=true;}else drawn.Encapsulate(r.bounds);}
                if(any){var off=drawn.center-visual.position;off.y=0;terminal+=visual.parent.InverseTransformVector(off*(1-flatScale.x/visualScale.x));}
            }
            if(enemy!=null&&airborne)
            {
                // A killed flier loses lift: it tumbles and falls away below the
                // flight plane instead of hovering on a slight tilt.
                terminalRotation=originRotation*Quaternion.Euler(30,0,150);
                terminal=origin+visual.parent.InverseTransformVector(Vector3.down*1.8f);
            }
            bool hound=enemy!=null&&(enemy.definition.id=="Ridgehound"||enemy.definition.id=="Gloomgrazer");
            bool groundCreature=enemy!=null&&(hound||enemy.definition.id=="Scrapmite"||enemy.definition.id=="Burrower");
            if(groundCreature)
            {
                // Inspected against each generated body: hound on its flank,
                // mite on its upper shell, burrower along its lower side.
                // A shared tilt left them balancing on a paw, leg or jaw.
                Vector3 rest=hound?new Vector3(0,0,98):
                    enemy.definition.id=="Scrapmite"?new Vector3(0,0,170):new Vector3(-10,0,128);
                terminalRotation=originRotation*Quaternion.Euler(rest);
            }
            if(groundCreature&&CaptureSupport())
            {
                var lying=terminal;
                (terminalRotation,terminal)=Supported(terminalRotation,lying);
                // D355: a hound lies on whichever flank keeps it off the heroes. Killed with its head at Taren's side it rolled its back across him, and for the two seconds it lay
                // there his torso stood in it (Lantern Gorge, nov22-whole s14/017 to 022: two Gloomgrazers, one each side of him).
                if(hound&&InHeroes(supportVertices,terminal,terminalRotation,visualScale,out Vector3 _)>0)
                {
                    var (otherRotation,otherAt)=Supported(originRotation*Quaternion.Euler(0,0,-98),lying);
                    if(InHeroes(supportVertices,otherAt,otherRotation,visualScale,out Vector3 _)<InHeroes(supportVertices,terminal,terminalRotation,visualScale,out Vector3 _)){terminalRotation=otherRotation;terminal=otherAt;}
                }
                if(!enemy.definition.boss)FallClear(supportVertices);
            }
            else if(!airborne)
            {
                // The remaining flight archetypes retain their existing branch
                // until their separate failure-motion review.
                // Calibrate the collapsed generated geometry once, rather than
                // trusting the inflated bounds of the grafted animal skeleton.
                float surface=transform.position.y;
                if(Physics.Raycast(transform.position+Vector3.up*2,Vector3.down,out var ground,6,~0,QueryTriggerInteraction.Ignore))surface=ground.point.y;
                visual.localRotation=terminalRotation;visual.localPosition=terminal;if(deflates)visual.localScale=flatScale;
                float bottom=MinimumY(visual);
                if(!float.IsInfinity(bottom))terminal+=visual.parent.InverseTransformVector(Vector3.up*(surface+.025f-bottom));
                visual.localPosition=origin;visual.localRotation=originRotation;if(deflates)visual.localScale=visualScale;
                if(enemy!=null&&!enemy.definition.boss)FallClear(LocalPoints());
            }
        }
        /// <summary>The rotation and place a ground creature lies at on the floor under it, from the rest it rolls to and the place it starts from.</summary>
        (Quaternion,Vector3) Supported(Quaternion rest,Vector3 from)
        {
            var rotation=Quaternion.Inverse(visual.parent.rotation)*Quaternion.FromToRotation(transform.up,supportNormal)*visual.parent.rotation*rest;
            visual.localRotation=rotation;visual.localPosition=from;SupportRigidBody();var at=visual.localPosition;
            visual.localPosition=origin;visual.localRotation=originRotation;
            return (rotation,at);
        }
        /// <summary>What it draws, in its visual's own frame, as it stands in the frame it died: every vertex of its meshes.</summary>
        Vector3[] LocalPoints()
        {
            var points=new List<Vector3>();var mesh=new List<Vector3>();
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                if(skin.sharedMesh!=null)foreach(var p in GeneratedGeometry.WorldSkinPoints(skin))points.Add(visual.InverseTransformPoint(p));
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;filter.sharedMesh.GetVertices(mesh);
                foreach(var p in mesh)points.Add(visual.InverseTransformPoint(filter.transform.TransformPoint(p)));
            }
            return points.ToArray();
        }
        /// <summary>A body that falls does so clear of the heroes on foot: drawn where it would lie, none of it comes within a third of a metre of a hero's capsule, between his feet and
        /// the top of his head. Laid just off his capsule, the two Gloomgrazers still read from the game's camera as lying in him, one at each side.</summary>
        const float HeroClearance=.35f,MostSlide=1.4f;
        /// <summary>How deep what it would draw, lying at this pose, stands inside the nearest hero's capsule on the plan (0: in none), and the way its body leaves him.</summary>
        float InHeroes(Vector3[] local,Vector3 position,Quaternion rotation,Vector3 scale,out Vector3 away)
        {
            away=Vector3.zero;float deepest=0;var party=PartyController.Current;
            if(party==null||local==null||local.Length==0||visual==null||visual.parent==null)return 0;
            var pose=visual.parent.localToWorldMatrix*Matrix4x4.TRS(position,rotation,scale);int stride=Mathf.Max(1,local.Length/900);
            foreach(var hero in party.members)
            {
                if(hero==null||hero.Health==null||!hero.Health.Alive||hero.flight||!hero.TryGetComponent<CharacterController>(out var capsule))continue;
                var foot=hero.transform.position;float r=capsule.radius+HeroClearance,top=foot.y+capsule.height;
                for(int i=0;i<local.Length;i+=stride)
                {
                    var p=pose.MultiplyPoint3x4(local[i]);if(p.y<foot.y+.05f||p.y>top)continue;
                    float dx=p.x-foot.x,dz=p.z-foot.z,d=Mathf.Sqrt(dx*dx+dz*dz);
                    if(d<r&&r-d>deepest){deepest=r-d;away=transform.position-foot;}
                }
            }
            away.y=0;away=away.sqrMagnitude>.0001f?away.normalized:-transform.forward;
            return deepest;
        }
        /// <summary>Where it lies is moved off any hero it would lie in, along the way from him through its middle, as the killing blow knocks it back: it slides there as it falls.</summary>
        void FallClear(Vector3[] local)
        {
            float slid=0;
            for(int i=0;i<5&&slid<MostSlide;i++)
            {
                float depth=InHeroes(local,terminal,terminalRotation,visualScale,out var away);
                if(depth<=0)break;
                float step=Mathf.Min(depth+.03f,MostSlide-slid);slid+=step;
                terminal+=visual.parent.InverseTransformVector(away*step);
            }
            if(slid>0){knockedClear=true;Debug.Log($"BODY_FALLS_CLEAR {enemy.definition.id} slid={slid:0.00}");}
        }
        bool CaptureSupport()
        {
            float nearest=float.PositiveInfinity;supportVertices=null;
            foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*2,Vector3.down,6,~0,QueryTriggerInteraction.Ignore))
            {
                if(!CombatCover.Opaque(hit.collider)||hit.normal.y<.5f||hit.distance>=nearest)continue;
                nearest=hit.distance;supportPoint=hit.point;supportNormal=hit.normal;
            }
            if(float.IsInfinity(nearest))return false;
            // The nonhumanoid Animator has stopped on the actual lethal pose.
            // Cache that geometry once; intermediate tilt needs support too.
            var vertices=new List<Vector3>();
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                foreach(var p in GeneratedGeometry.WorldSkinPoints(skin))vertices.Add(visual.InverseTransformPoint(p));
            foreach(var filter in visual.GetComponentsInChildren<MeshFilter>())
                if(filter.sharedMesh!=null)foreach(var p in filter.sharedMesh.vertices)
                    vertices.Add(visual.InverseTransformPoint(filter.transform.TransformPoint(p)));
            supportVertices=vertices.ToArray();return supportVertices.Length>0;
        }
        void SupportRigidBody()
        {
            if(supportVertices==null||supportVertices.Length==0)return;
            // Dot in local space avoids transforming every vertex each frame.
            // Transpose retains any parent scale; a direction alone would not.
            var localNormal=visual.localToWorldMatrix.transpose.MultiplyVector(supportNormal);
            float minimum=float.PositiveInfinity;
            foreach(var p in supportVertices)minimum=Mathf.Min(minimum,Vector3.Dot(p,localNormal));
            minimum+=Vector3.Dot(visual.position-supportPoint,supportNormal);
            visual.position+=supportNormal*(.025f-minimum);
        }
        static float MinimumY(Transform root)
        {
            float result=float.PositiveInfinity;var points=new List<Vector3>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach(var p in GeneratedGeometry.WorldSkinPoints(skin))result=Mathf.Min(result,p.y);
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;filter.sharedMesh.GetVertices(points);
                foreach(var p in points)result=Mathf.Min(result,filter.transform.TransformPoint(p).y);
            }
            return result;
        }
        void BeginRecovery(Health _)
        {
            if(!down)return;recovering=true;started=GameTime.Now;
            if(dark!=null){dark.Restore();dark=null;}
            if(flightFailure!=null)flightFailure.BeginRecovery();
            if(cantorRelease!=null)cantorRelease.Restore();
        }
        void LateUpdate()
        {
            if(!down)return;
            float elapsed=GameTime.Now-started;
            if(departure!=null)
            {
                if(!GameTime.Paused)departure.Advance(elapsed);
                if(elapsed>=departure.Duration)Destroy(gameObject);
                return;
            }
            if(cantorRelease!=null)
            {
                if(recovering)
                {
                    if(elapsed>=CombatActor.ReviveDuration)
                    {
                        for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collisionEnabled[i];
                        down=recovering=false;cantorRelease=null;Recovered?.Invoke();
                    }
                }
                else
                {
                    if(!GameTime.Paused)cantorRelease.Advance(elapsed);
                    if(elapsed>=CantorRelease.Duration)Destroy(gameObject);
                }
                return;
            }
            if(flightFailure!=null)
            {
                if(!health.Alive)
                    for(int i=0;i<colliders.Length;i++)if(colliders[i] is CharacterController)colliders[i].enabled=actor.flight&&collisionEnabled[i];
                flightFailure.Advance(elapsed,recovering);
            }
            if(recovering)
            {
                if(flightFailure==null&&rigid&&visual!=null)
                {
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/CombatActor.ReviveDuration));
                    visual.localPosition=Vector3.Lerp(terminal,origin,t);visual.localRotation=Quaternion.Slerp(terminalRotation,originRotation,t);
                    if(deflates)visual.localScale=Vector3.Lerp(flatScale,visualScale,t);else if(bursts)visual.localScale=visualScale;
                }
                if(elapsed>=CombatActor.ReviveDuration)
                {
                    if(flightFailure!=null)flightFailure.Finish();
                    for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=collisionEnabled[i];
                    down=recovering=false;Recovered?.Invoke();
                }
                return;
            }
            if(flightFailure==null&&rigid&&visual!=null)
            {
                // A boss goes down fastest at first (D355): eased in, its first half second moved it a quarter of the way and read as standing.
                float u=Mathf.Clamp01(elapsed/duration),t=enemy!=null&&enemy.definition.boss?1-(1-u)*(1-u):Mathf.SmoothStep(0,1,u);
                // A body knocked clear of a hero (FallClear) is off him before it has rolled far, so no frame of its fall swings it through him.
                visual.localPosition=Vector3.Lerp(origin,terminal,knockedClear?Mathf.Sqrt(t):t);visual.localRotation=Quaternion.Slerp(originRotation,terminalRotation,t);
                SupportRigidBody();
                if(deflates)visual.localScale=Vector3.Lerp(visualScale,flatScale,t);
                else if(bursts)visual.localScale=visualScale*(elapsed<.12f?Mathf.Lerp(burstFrom,1.5f,elapsed/.12f):Mathf.Lerp(1.5f,.2f,(elapsed-.12f)/.48f));
            }
            if(sections!=null)
                for(int i=0;i<sections.Length;i++)
                {
                    if(sections[i]==null)continue;
                    float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-i*.09f)/duration));
                    sections[i].localPosition=sectionOrigins[i]+Vector3.down*(.65f*t);
                    sections[i].localRotation=sectionRotations[i]*Quaternion.Euler(8*t,0,(38+i*5)*t);
                }
            if(yields&&elapsed>=duration+hold)
            {
                // One body: she never drops or shrinks, and the person she is now stands exactly here from this frame on.
                CombatVfx.Burst(transform.position+Vector3.up,new Color(.62f,.72f,.8f),"hit");
                Yielded?.Invoke(enemy.definition.id,transform.position,transform.rotation);Destroy(gameObject);return;
            }
            if(enemy!=null&&elapsed>=duration+hold)
            {
                // The whole root, so separate serpent sections leave with the body.
                var body=transform;
                if(!cleaning)
                {
                    cleaning=true;cleanupScale=body.localScale;cleanupPosition=body.position;
                    var bounds=new Bounds(transform.position+Vector3.up*.5f,Vector3.zero);
                    foreach(var renderer in GetComponentsInChildren<Renderer>())if(renderer.enabled)bounds.Encapsulate(renderer.bounds);
                    CombatVfx.Burst(bounds.center,new Color(.62f,.72f,.8f),"hit");
                }
                float t=Mathf.Clamp01((elapsed-duration-hold)/Cleanup);
                body.localScale=cleanupScale*(1-t*t);
                body.position=cleanupPosition+Vector3.down*(.15f*t);
                if(t>=1)Destroy(gameObject);
            }
        }
    }
}
