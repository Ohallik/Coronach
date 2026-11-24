using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Living bodies standing inside each other in the whole-game capture of November 20: the partner inside the lead at the Host Marshal (s36/062), the partner's ship inside
    /// a Chorister Drifter she was shooting in the Gullet (s10b/089), and Gloomgrazers' bodies through Taren in Lantern Gorge (s14/017). Each is measured here in a fixture, the brains
    /// running as in play, and each line written (BODIES_APART).</summary>
    public sealed class BodiesKeepApartTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static float Flat(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        static CombatActor Lead=>PartyController.Current.Active;
        static CombatActor Partner=>PartyController.Current.members.First(m=>m!=Lead);
        static void Quiet(){foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}}
        static void Place(CombatActor actor,Vector3 position,Vector3 facing)
        {
            var body=actor.GetComponent<CharacterController>();body.enabled=false;
            if(actor.motor is GroundMotor ground){ground.Halt();ground.KeepFacing(facing);}
            if(actor.motor is FlightMotor ship){ship.Halt();ship.KeepFacing(facing);}
            actor.transform.SetPositionAndRotation(position,Quaternion.LookRotation(facing));body.enabled=true;actor.target=null;actor.TargetLocked=false;
            Physics.SyncTransforms();
        }
        static IEnumerator Zone(string loadout,string zone,string leader)
        {
            DevLoadout.Apply(loadout);
            SceneFlow.Current.LoadZone(zone);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,"fixture: "+zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))Object.Destroy(enemy.gameObject);
            Quiet();yield return new WaitForSecondsRealtime(.6f);
            for(int i=0;i<4&&Lead.character!=leader;i++){PartyController.Current.Swap();Quiet();yield return null;}
            Assert.AreEqual(leader,Lead.character,"fixture: "+leader+" leads");
            foreach(var hero in PartyController.Current.members){hero.Health.integrity=hero.Health.maximum;hero.Health.InvulnerableUntil=float.PositiveInfinity;}
        }
        static EnemyBrain Body(string id,Vector3 at,Vector3 facing,bool passive=true)
        {
            var body=ActorFactory.Enemy(GameCatalog.Find<EnemyDef>(id),at);body.Passive=passive;body.Health.maximum=body.Health.integrity=1000000;body.Health.breakThreshold=1e9f;
            body.transform.rotation=Quaternion.LookRotation(facing);return body;
        }
        static readonly FieldInfo DrawnRadius=typeof(EnemyBrain).GetField("bodyRadius",BindingFlags.Instance|BindingFlags.NonPublic);
        static float Drawn(EnemyBrain body)=>DrawnRadius!=null?(float)DrawnRadius.GetValue(body):0;
        static float Capsule(Component body)=>body.GetComponent<CharacterController>().radius;

        // ================================================================== 1. the partner and the lead on one body
        /// <summary>How far apart two heroes' middles stand at the least: both capsules (0.35 m each) and the width their drawn bodies carry past them (an arm, a shoulder plate, a hammer
        /// head), so that neither stands in the other.</summary>
        const float HeroesApart=1.1f;
        /// <summary>Taren in melee on the near side of a body, held there; Idra out beside him, starting three metres behind him on the same line, so the straight way to the body is through
        /// him. She has to take another side of it, never stand closer to him than <see cref="HeroesApart"/>, never move him, and land her swings.</summary>
        IEnumerator OneBody(string id)
        {
            yield return Zone("chapter7","Arena_Ground","Taren");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bodies fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var lead=Lead;var partner=Partner;Assert.AreEqual("Idra",partner.character,"fixture: Idra is out beside him");
            var body=Body(id,new Vector3(230,0,0),Vector3.back);yield return null;yield return null;
            float bodyR=Capsule(body),leadR=Capsule(lead);
            var stand=new Vector3(230,0,-(bodyR+leadR+.25f));Place(lead,stand,Vector3.forward);Place(partner,stand+Vector3.back*3,Vector3.forward);
            lead.target=body.Health;lead.TargetLocked=true;yield return null;
            float landed=0;int swings=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==partner.Health&&amount>0){landed+=amount;swings++;}};
            partner.GetComponent<PartnerBrain>().enabled=true;float began=Time.unscaledTime,nearest=float.PositiveInfinity,drift=0;string at="";
            while(Time.unscaledTime-began<8)
            {
                partner.charge=0;lead.target=body.Health;
                yield return null;
                float gap=Flat(lead.transform.position,partner.transform.position);if(gap<nearest){nearest=gap;at=$"({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) at {Time.unscaledTime-began:0.0} s";}
                drift=Mathf.Max(drift,Flat(lead.transform.position,stand));
            }
            var fromBody=partner.transform.position-body.transform.position;fromBody.y=0;var leadFrom=stand-body.transform.position;leadFrom.y=0;
            TestContext.WriteLine($"BODIES_APART ground {id} bodyCapsule={bodyR:0.00} bodyDrawn={Drawn(body):0.00} nearestToLead={nearest:0.00} {at} leadMoved={drift:0.000} landed={landed:0} swings={swings} ends at ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) {Flat(partner.transform.position,body.transform.position):0.00} m off the body, {Vector3.Angle(fromBody,leadFrom):0} degrees round it from the lead");
            Assert.GreaterOrEqual(nearest,HeroesApart,$"meleeing a {id} beside the lead the partner came within {nearest:0.00} m of his middle ({at}): she stood in him");
            Assert.Less(drift,.05f,$"meleeing a {id} the partner moved the lead {drift:0.00} m");
            Assert.Greater(swings,1,$"meleeing a {id} beside the lead the partner landed {swings} blows in eight seconds");
        }
        [UnityTest,Timeout(120000)] public IEnumerator BesideTheLeadAtTheHostMarshalThePartnerTakesAnotherSideOfIt()=>OneBody("HostMarshal");
        /// <summary>The lead walking up to a body both strike, as route 65 walks Taren to the Tollkeeper's gate motor: Idra starts behind him and to his right, so her straight way to the body
        /// crosses his. In the scout of November 21 (nov21-recon1 s65, step 124) she stood on his way striking, he slid off her into the motor's housing and stopped 0.72 m short of the route's
        /// place. The ground he walks to is his: he reaches his place at his own pace, she is never within <see cref="HeroesApart"/> of him, and she takes her own side.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator WalkingUpToABodyTheLeadHasHisWayAndHisPlace()
        {
            yield return Zone("chapter7","Arena_Ground","Taren");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bodies fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var lead=Lead;var partner=Partner;Assert.AreEqual("Idra",partner.character,"fixture: Idra is out beside him");
            var body=Body("HostMarshal",new Vector3(230,0,0),Vector3.back);yield return null;yield return null;
            var place=new Vector3(230,0,-(Capsule(body)+Capsule(lead)+.25f));var from=place+new Vector3(1.4f,0,-6.5f);
            Place(lead,from,(place-from).normalized);Place(partner,from+new Vector3(1.1f,0,.4f),Vector3.forward);
            lead.target=body.Health;lead.TargetLocked=true;yield return null;
            int swings=0;body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==partner.Health&&amount>0)swings++;};
            partner.GetComponent<PartnerBrain>().enabled=true;float began=Time.unscaledTime,nearest=float.PositiveInfinity,arrived=-1;string at="";
            while(Time.unscaledTime-began<8)
            {
                partner.charge=0;lead.target=body.Health;
                var way=place-lead.transform.position;way.y=0;
                lead.motor.Move(arrived<0&&way.magnitude>.3f?new Vector2(way.x,way.z).normalized:Vector2.zero,false,false);
                yield return null;
                if(arrived<0&&Flat(lead.transform.position,place)<=.5f)arrived=Time.unscaledTime-began;
                float gap=Flat(lead.transform.position,partner.transform.position);if(gap<nearest){nearest=gap;at=$"lead ({lead.transform.position.x:0.00},{lead.transform.position.z:0.00}) Idra ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) at {Time.unscaledTime-began:0.0} s";}
            }
            float walk=Flat(from,place)/4f;
            TestContext.WriteLine($"BODIES_APART walking up: {Flat(from,place):0.0} m to his place, arrived={(arrived<0?"never":arrived.ToString("0.00")+" s")} nearest={nearest:0.00} ({at}) swings={swings} ends at ({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) lead ({lead.transform.position.x:0.00},{lead.transform.position.z:0.00})");
            Assert.GreaterOrEqual(arrived,0f,$"walking up to the body the lead never reached his place: he ended at ({lead.transform.position.x:0.00},{lead.transform.position.z:0.00}), {Flat(lead.transform.position,place):0.00} m from it");
            Assert.Less(arrived,walk+1.5f,$"walking up to the body the lead took {arrived:0.0} s to reach his place, against {walk:0.0} s at a walk");
            Assert.GreaterOrEqual(nearest,HeroesApart,$"as the lead walked up to the body the partner came within {nearest:0.00} m of him ({at})");
            Assert.Greater(swings,1,$"beside the lead at the body the partner landed {swings} blows");
        }
        [UnityTest,Timeout(120000)] public IEnumerator BesideTheLeadAtAHoundThePartnerTakesAnotherSideOfIt()=>OneBody("Ridgehound");

        // ================================================================== 2. the partner's ship and a living enemy
        /// <summary>Sela's ship out beside Taren in flight, the lead's lock on a Chorister Drifter: she closes on it and fires, and her hull never comes inside its body (its capsule, or what
        /// it draws if that is wider), all the while landing her shots.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator InFlightThePartnersHullKeepsOffTheDrifterItFights()
        {
            yield return Zone("starter","Arena_Flight","Taren");
            var lead=Lead;var partner=Partner;
            var body=Body("ChoristerDrifter",new Vector3(30,1,0),Vector3.back);yield return null;yield return null;
            Place(lead,new Vector3(30,1,-12),Vector3.forward);Place(partner,new Vector3(36,1,-16),Vector3.forward);
            lead.target=body.Health;lead.TargetLocked=true;yield return null;
            float hull=HeroCollision.HullRadius(partner.character),bodyR=Mathf.Max(Capsule(body),Drawn(body)),landed=0;int hits=0;
            body.Health.Damaged+=(_,packet,amount)=>{if(packet.source==partner.Health&&amount>0){landed+=amount;hits++;}};
            partner.GetComponent<PartnerBrain>().enabled=true;float began=Time.unscaledTime,nearest=float.PositiveInfinity;string at="";
            while(Time.unscaledTime-began<10)
            {
                lead.target=body.Health;lead.motor.Move(Vector2.zero,false,true);
                yield return null;
                float gap=Flat(partner.transform.position,body.transform.position)-hull-bodyR;if(gap<nearest){nearest=gap;at=$"({partner.transform.position.x:0.00},{partner.transform.position.z:0.00}) at {Time.unscaledTime-began:0.0} s, {partner.State}";}
            }
            TestContext.WriteLine($"BODIES_APART flight Drifter capsule={Capsule(body):0.00} drawn={Drawn(body):0.00} hull={hull:0.00} nearestGap={nearest:0.00} {at} hits={hits} landed={landed:0}");
            Assert.GreaterOrEqual(nearest,0f,$"in flight the partner's hull came {-nearest:0.00} m inside the Drifter she was fighting ({at})");
            Assert.GreaterOrEqual(hits,4,"keeping off the Drifter the partner stopped landing her shots on it");
        }

        // ================================================================== 3. a small melee body and the hero
        /// <summary>How far a body's drawn front reaches ahead of its middle, along the way it faces, from its meshes in its own frame (its capsule's radius at the least).</summary>
        static float Front(EnemyBrain body)
        {
            float most=Capsule(body);var toRoot=body.transform.worldToLocalMatrix;
            foreach(var drawn in body.GetComponentsInChildren<Renderer>())
            {
                Bounds box;Matrix4x4 into;
                if(drawn is SkinnedMeshRenderer skin){box=skin.localBounds;into=toRoot*(skin.rootBone!=null?skin.rootBone:skin.transform).localToWorldMatrix;}
                else if(drawn is MeshRenderer&&drawn.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null){box=filter.sharedMesh.bounds;into=toRoot*drawn.transform.localToWorldMatrix;}
                else continue;
                if(drawn.name=="Telegraph"||drawn.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)continue;
                for(int i=0;i<8;i++)most=Mathf.Max(most,into.MultiplyPoint3x4(box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))).z);
            }
            return most;
        }
        /// <summary>How close a body's spine (its middle to its drawn front, the way it faces) comes to a hero's middle, on the plan.</summary>
        static float Spine(EnemyBrain body,float front,Vector3 hero)
        {
            var from=body.transform.position;var way=body.transform.forward;way.y=0;way.Normalize();var off=hero-from;off.y=0;
            float t=Mathf.Clamp(Vector3.Dot(off,way),0,front);return (off-way*t).magnitude;
        }
        /// <summary>A small melee body awake against Taren, who stands still: it closes, marks and bites as it always has. Its capsule stopped a few centimetres off his, but the capsule
        /// is round its middle and a hound or a grazer is drawn a body's length ahead of that: run in to his middle, its head and shoulders stood in him (Lantern Gorge, nov20-whole s14/017).
        /// Its drawn front now stops at his capsule, and he is never moved by it.</summary>
        IEnumerator SmallBody(string id)
        {
            yield return Zone("chapter2","Arena_Ground","Taren");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bodies fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(140,1,140);Physics.SyncTransforms();
            var lead=Lead;var partner=Partner;Place(partner,new Vector3(260,0,30),Vector3.forward);
            var stand=new Vector3(230,0,0);Place(lead,stand,Vector3.forward);
            var body=Body(id,new Vector3(230,0,9),Vector3.back,false);yield return null;yield return null;
            float bodyR=Capsule(body),heroR=Capsule(lead),front=Front(body),nearest=float.PositiveInfinity,inside=float.PositiveInfinity,drift=0;int marks=0,bites=0;bool telling=false;string at="";
            lead.Health.InvulnerableUntil=0;lead.Health.maximum=lead.Health.integrity=1000000;
            lead.Health.Damaged+=(_,packet,amount)=>{if(packet.source==body.Health)bites++;};
            float began=Time.unscaledTime;
            while(Time.unscaledTime-began<14)
            {
                yield return null;
                if(body.Telegraphing&&!telling)marks++;telling=body.Telegraphing;
                nearest=Mathf.Min(nearest,Flat(body.transform.position,lead.transform.position)-bodyR-heroR);
                float spine=Spine(body,front,lead.transform.position)-heroR;if(spine<inside){inside=spine;at=$"at {Time.unscaledTime-began:0.0} s, {Flat(body.transform.position,lead.transform.position):0.00} m middle to middle, telling={body.Telegraphing}";}
                drift=Mathf.Max(drift,Flat(lead.transform.position,stand));
            }
            TestContext.WriteLine($"BODIES_APART melee {id} capsule={bodyR:0.00} drawn={Drawn(body):0.00} front={front:0.00} hero={heroR:0.00} capsuleGap={nearest:0.000} spineGap={inside:0.000} {at} marks={marks} bites={bites} heroMoved={drift:0.000}");
            Assert.GreaterOrEqual(nearest,-.02f,$"a {id}'s capsule came {-nearest:0.00} m inside the hero's");
            Assert.GreaterOrEqual(inside,-.05f,$"a {id}'s drawn body came {-inside:0.00} m inside the hero's capsule ({at})");
            Assert.Less(drift,.05f,$"a {id} moved the hero {drift:0.00} m");
            Assert.GreaterOrEqual(marks,3,$"a {id} marked {marks} attacks in fourteen seconds against a hero standing still");
            Assert.GreaterOrEqual(bites,2,$"a {id} landed {bites} of {marks} attacks on a hero standing still");
        }
        [UnityTest,Timeout(120000)] public IEnumerator AGloomgrazersDrawnBodyStopsAtTheHerosCapsule()=>SmallBody("Gloomgrazer");
        [UnityTest,Timeout(120000)] public IEnumerator ARidgehoundsDrawnBodyStopsAtTheHerosCapsule()=>SmallBody("Ridgehound");

        // ================================================================== 4. what is drawn, against the hero's capsule, in the places the capture of November 22 found it (D355)
        /// <summary>Every point a body draws this frame, in the world: a skin where its bones hold it, a mesh where it hangs (a stride keeps it to a few thousand).</summary>
        internal static List<Vector3> DrawnPoints(Component body,int most=4000)
        {
            var points=new List<Vector3>();var mesh=new List<Vector3>();
            foreach(var r in body.GetComponentsInChildren<Renderer>())
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy||r.name=="Telegraph"||r.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)continue;
                if(r is SkinnedMeshRenderer skin&&skin.sharedMesh!=null)
                {
                    try{points.AddRange(GeneratedGeometry.WorldSkinPoints(skin));}
                    catch(System.InvalidOperationException)
                    {
                        // A rig that isn't a generated one (a person's): its skin as Unity poses it, its renderer's scale baked in.
                        var baked=new Mesh();skin.BakeMesh(baked,true);var pose=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
                        baked.GetVertices(mesh);foreach(var p in mesh)points.Add(pose.MultiplyPoint3x4(p));Object.Destroy(baked);
                    }
                }
                else if(r is MeshRenderer&&r.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null){filter.sharedMesh.GetVertices(mesh);foreach(var p in mesh)points.Add(r.transform.TransformPoint(p));}
            }
            if(points.Count<=most)return points;
            int stride=points.Count/most+1;var kept=new List<Vector3>(most+1);for(int i=0;i<points.Count;i+=stride)kept.Add(points[i]);return kept;
        }
        /// <summary>How deep the deepest of those points stands inside a hero's capsule on the plan (grown by <paramref name="margin"/>), between his feet and the top of his head (0: none).</summary>
        internal static float Inside(List<Vector3> points,CombatActor hero,float margin=0)
        {
            var foot=hero.transform.position;var capsule=hero.GetComponent<CharacterController>();float r=capsule.radius+margin,top=foot.y+capsule.height,deepest=0;
            foreach(var p in points)
            {
                if(p.y<foot.y+.05f||p.y>top)continue;
                float d=Flat(p,foot);if(d<r)deepest=Mathf.Max(deepest,r-d);
            }
            return deepest;
        }
        static Vector3 SpawnerOf(string encounter,string id)
        {
            var volume=Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None).FirstOrDefault(v=>v.encounterId==encounter);
            Assert.IsNotNull(volume,"fixture: the zone has the encounter "+encounter);
            foreach(var box in volume.GetComponents<Collider>())box.enabled=false;
            var spawner=volume.spawners.FirstOrDefault(s=>s!=null&&s.definition!=null&&s.definition.id==id);Assert.IsNotNull(spawner,$"fixture: {encounter} spawns a {id}");
            return spawner.transform.position;
        }
        /// <summary>How far round a hero's capsule a body that has fallen lies clear of him: just off it, two bodies at his sides still read from the game's camera as lying in him.</summary>
        const float LiesClear=.25f;
        /// <summary>When a grazer has finished falling (DefeatPresentation's .9 s, and a frame): from the blow until then it is judged against his capsule alone, since it was killed in
        /// contact with him; from then until it is cleared away, against the quarter metre round it.</summary>
        const float Fallen=.95f;
        static void Hold(CombatActor hero){hero.Health.maximum=hero.Health.integrity=1000000;hero.Health.InvulnerableUntil=0;}

        /// <summary>Two Gloomgrazers killed one at each side of Taren in Lantern Gorge, where the capture's two were (nov22-whole s14/017 to 022): each fell onto its flank and rolled its
        /// back across him, and for the two seconds the bodies lay there his torso stood in them. Now a body lies on the flank that keeps it off the heroes, and slides off any hero it
        /// would still lie in as it falls: from the blow to its clearing up, none of what either draws comes within a quarter metre of his capsule. Shown to fail first on the same two bodies tipped
        /// by hand onto the flank the old roll laid them on.</summary>
        [UnityTest,Timeout(120000)] public IEnumerator GloomgrazersKilledAtTheHerosSidesLieClearOfHim()
        {
            LogAssert.ignoreFailingMessages=true;// a zone's own fixtures may miss what the test took away; this watches the bodies
            yield return Zone("chapter2","LanternGorge","Taren");
            var lead=Lead;var stand=new Vector3(1.95f,.12f,23.46f);Place(lead,stand,Vector3.forward);Place(Partner,stand+new Vector3(14,0,-10),Vector3.forward);Hold(lead);
            float apart=.55f+Capsule(lead)+.05f;
            // The old roll (98 degrees about its spine) swings its back to its left: each stands with Taren at its left flank, one facing up the path and one down it.
            var west=Body("Gloomgrazer",stand+new Vector3(apart,0,0),Vector3.forward);var east=Body("Gloomgrazer",stand+new Vector3(-apart,0,0),Vector3.back);
            foreach(var b in new[]{west,east}){b.Health.maximum=b.Health.integrity=100;b.Health.breakThreshold=1e9f;}
            yield return new WaitForSecondsRealtime(.6f);Place(lead,stand,Vector3.forward);
            Assert.AreEqual(0f,Mathf.Max(Inside(DrawnPoints(west),lead),Inside(DrawnPoints(east),lead)),.05f,"fixture: standing, neither body is in him");
            // The fault first: each laid on the old flank by hand, its lowest point on the floor.
            float old=0;
            foreach(var b in new[]{west,east})
            {
                var shown=b.GetComponent<DefeatPresentation>().visual;var rest=shown.localRotation;var at=shown.localPosition;
                shown.localRotation=rest*Quaternion.Euler(0,0,98);float lowest=DrawnPoints(b).Min(p=>p.y);shown.position+=Vector3.up*(b.transform.position.y-lowest);
                old=Mathf.Max(old,Inside(DrawnPoints(b),lead));shown.localRotation=rest;shown.localPosition=at;
            }
            Assert.Greater(old,.1f,"the check doesn't see a body laid on the old flank in the hero");
            var lead0=lead.transform.position;
            foreach(var b in new[]{west,east})b.Health.Receive(new DamagePacket{source=lead.Health,amount=1e6f,type=DamageType.Plasma});
            float falling=0,deepest=0,began=Time.unscaledTime;string at2="";bool shot=false;
            while(Time.unscaledTime-began<2.1f&&(west!=null||east!=null))
            {
                yield return null;float since=Time.unscaledTime-began;
                if(!shot&&since>=1.2f){shot=true;BossDeathTests.GameStill("gorge-grazers-lying-1.2s");}
                foreach(var b in new[]{west,east})if(b!=null)
                {
                    var drawn=DrawnPoints(b);falling=Mathf.Max(falling,Inside(drawn,lead));
                    if(since<Fallen)continue;float d=Inside(drawn,lead,LiesClear);if(d>deepest){deepest=d;at2=$"{b.name} at {since:0.00} s";}
                }
            }
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine($"BODIES_APART corpses Gloomgrazer old-flank={old:0.00} falling={falling:0.00} lying={deepest:0.00} {at2} heroMoved={Flat(lead.transform.position,lead0):0.00}");
            Assert.LessOrEqual(falling,.03f,$"a Gloomgrazer killed at Taren's side fell {falling:0.00} m into him");
            Assert.LessOrEqual(deepest,.03f,$"a Gloomgrazer killed at Taren's side lay {deepest:0.00} m inside a quarter metre round him ({at2})");
        }

        /// <summary>A pack of Gloomgrazers fighting Taren where the capture's did in the Sunken Relay's flooded gallery (nov22-whole s15/052, Relay_1): he stands, they come in, mark and
        /// bite from every side for twelve seconds, and none of what any of them draws comes inside his capsule; killed where they stand, their bodies lie clear of him.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator APackOfGloomgrazersInTheRelayNeverStandsInTheHero()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return Zone("chapter2","SunkenRelay","Taren");
            var lead=Lead;var stand=new Vector3(.07f,.1f,54.5f);Place(lead,stand,Vector3.forward);Place(Partner,stand+new Vector3(14,0,-12),Vector3.forward);Hold(lead);
            var from=SpawnerOf("Relay_1","Gloomgrazer");var pack=new List<EnemyBrain>();
            for(int i=0;i<3;i++){var b=Body("Gloomgrazer",from+Quaternion.Euler(0,i*120,0)*Vector3.forward*1.6f,(stand-from).normalized,false);b.Health.maximum=b.Health.integrity=1000000;pack.Add(b);}
            int bites=0;lead.Health.Damaged+=(_,packet,amount)=>{if(pack.Any(b=>b!=null&&packet.source==b.Health))bites++;};
            float deepest=0,began=Time.unscaledTime;string at="";int frame=0;
            while(Time.unscaledTime-began<12)
            {
                yield return null;if(++frame%3!=0)continue;
                foreach(var b in pack)if(b!=null){float d=Inside(DrawnPoints(b),lead);if(d>deepest){deepest=d;at=$"at {Time.unscaledTime-began:0.0} s, {Flat(b.transform.position,lead.transform.position):0.00} m middle to middle, lunging={b.Telegraphing}";}}
            }
            foreach(var b in pack)if(b!=null)b.Health.Receive(new DamagePacket{source=lead.Health,amount=1e7f,type=DamageType.Plasma});
            float lying=0,falling=0;began=Time.unscaledTime;
            while(Time.unscaledTime-began<2.1f)
            {
                yield return null;float since=Time.unscaledTime-began;
                foreach(var b in pack)if(b!=null){var drawn=DrawnPoints(b);falling=Mathf.Max(falling,Inside(drawn,lead));if(since>=Fallen)lying=Mathf.Max(lying,Inside(drawn,lead,LiesClear));}
            }
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine($"BODIES_APART pack Gloomgrazer x3 from {from} deepest-alive={deepest:0.00} ({at}) falling={falling:0.00} lying={lying:0.00} bites={bites} heroMoved={Flat(lead.transform.position,stand):0.00}");
            Assert.Greater(bites,2,"fixture: the pack fought him");
            Assert.LessOrEqual(deepest,.08f,$"a Gloomgrazer fighting him stood {deepest:0.00} m inside him ({at})");
            Assert.LessOrEqual(falling,.03f,$"a Gloomgrazer killed fighting him fell {falling:0.00} m into him");
            Assert.LessOrEqual(lying,.03f,$"a Gloomgrazer killed fighting him lay {lying:0.00} m inside a quarter metre round him");
        }

        /// <summary>Hessa Korl fighting Taren in her arena, where the capture's stood (nov22-whole s15/149): her lance, chain, sidearm and sweep for twenty seconds against a hero who stands,
        /// and none of what she draws (her body, her lance) comes inside his capsule. The still showed her a metre and a half to two behind him, drawn above him by the camera's angle.
        /// Her moves are played through BossController.Perform, as the shared boss tests play them: on her own she waits for her encounter to wake her.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator HessaKorlNeverStandsInTheHero()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return Zone("chapter2","SunkenRelay","Taren");
            var lead=Lead;var stand=new Vector3(1.79f,.11f,125.12f);Place(lead,stand,Vector3.forward);Place(Partner,stand+new Vector3(16,0,-8),Vector3.forward);Hold(lead);
            var from=SpawnerOf("Relay_Hessa","HessaKorl");
            // Walked in to her lance's reach, and her moves played through in turn, as her fight would (on her own she waits for the encounter to wake her).
            var hessa=Body("HessaKorl",stand+(from-stand).normalized*3.2f,(stand-from).normalized);hessa.Health.maximum=hessa.Health.integrity=1e7f;var boss=hessa.GetComponent<BossController>();
            int hits=0;lead.Health.Damaged+=(_,packet,amount)=>{if(packet.source==hessa.Health)hits++;};
            float deepest=0,nearest=float.PositiveInfinity,began=Time.unscaledTime;string at="";int frame=0,next=0;var moves=new[]{"HookLance","GrappleChain","SweepingCut","HookLance","Sidearm","GrappleChain"};
            while(Time.unscaledTime-began<20&&hessa!=null)
            {
                lead.motor.Move(Vector2.zero,false,false);
                if(!boss.Busy&&boss.Perform(moves[next%moves.Length]))next++;
                yield return null;nearest=Mathf.Min(nearest,Flat(hessa.transform.position,lead.transform.position));if(++frame%3!=0)continue;
                float d=Inside(DrawnPoints(hessa),lead);if(d>deepest){deepest=d;at=$"at {Time.unscaledTime-began:0.0} s, {Flat(hessa.transform.position,lead.transform.position):0.00} m middle to middle";}
            }
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine($"BODIES_APART Hessa capsule={Capsule(hessa):0.00} drawn={Drawn(hessa):0.00} nearest={nearest:0.00} deepest={deepest:0.00} ({at}) moves={next} hits={hits} heroMoved={Flat(lead.transform.position,stand):0.00}");
            Assert.GreaterOrEqual(next,4,"fixture: she played her moves on him");Assert.LessOrEqual(nearest,2f,$"fixture: she came to close quarters with him (nearest {nearest:0.00} m)");
            Assert.LessOrEqual(deepest,.08f,$"Hessa Korl stood {deepest:0.00} m inside Taren ({at})");
        }

        // ================================================================== 5. a hero walked into a boss stands outside what it draws at the floor (D355, continued)
        /// <summary>How far a boss's drawn outline reaches from its middle at the floor (what it draws from just over the floor to half a metre up), on each of sixteen bearings.</summary>
        internal static float[] FloorOutline(EnemyBrain body,float floor)
        {
            var reach=new float[16];var middle=body.transform.position;
            foreach(var p in DrawnPoints(body,40000))
            {
                if(p.y<floor+.02f||p.y>floor+.5f)continue;
                var off=p-middle;off.y=0;float d=off.magnitude;if(d<.01f)continue;
                int bearing=Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(off.x,off.z)*Mathf.Rad2Deg,360)/22.5f)%16;
                reach[bearing]=Mathf.Max(reach[bearing],d);
            }
            return reach;
        }
        /// <summary>Taren walked straight at a boss's middle from each of sixteen bearings until whatever stops him stops him: on each, how far the boss's outline at the floor reaches
        /// past his capsule's edge (positive: what it draws is in him). The deepest bearing, and the line for the lead.</summary>
        internal static IEnumerator WalkedIn(string id,System.Action<float,string> result,int[] only=null)
        {
            yield return Zone("chapter10","Arena_Ground","Taren");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bodies fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(160,1,160);Physics.SyncTransforms();
            var lead=Lead;Place(Partner,new Vector3(280,0,40),Vector3.forward);Hold(lead);lead.Health.InvulnerableUntil=float.PositiveInfinity;
            var at=new Vector3(230,0,10);var boss=Body(id,at,Vector3.back);yield return new WaitForSecondsRealtime(.8f);
            var outline=FloorOutline(boss,at.y);float worst=float.NegativeInfinity,touching=0;string where="",touchedAt="",table="";
            for(int bearing=0;bearing<16;bearing++)
            {
                if(boss==null)break;
                var way=Quaternion.Euler(0,bearing*22.5f,0)*Vector3.forward;float start=Mathf.Max(outline[bearing],Capsule(boss))+2.5f;
                Place(lead,at+way*start,-way);
                for(float t=0;t<1.8f;t+=Time.deltaTime){lead.motor.Move(new Vector2(-way.x,-way.z),false,false);yield return null;}
                lead.motor.Move(Vector2.zero,false,false);yield return null;
                float stood=Flat(lead.transform.position,at),inside=outline[bearing]-(stood-Capsule(lead));
                if((only==null||System.Array.IndexOf(only,bearing)>=0)&&inside>worst){worst=inside;where=$"bearing {bearing*22.5f:0}: outline {outline[bearing]:0.00} m, stopped {stood:0.00} m from its middle";}
                // And what it draws that is actually in him there, at any height to his head (a body on legs has open floor inside its outline).
                float d=Inside(DrawnPoints(boss),lead);if(d>touching){touching=d;touchedAt=$"bearing {bearing*22.5f:0}";}
                table+=$" {bearing*22.5f:0}:{inside:0.00}/{d:0.00}";
            }
            if(boss!=null)Object.Destroy(boss.gameObject);
            result(worst,$"BODIES_APART walked-in {id} capsule={Capsule(boss):0.00} outline-past-his-edge={worst:0.00} m ({where}) drawn-in-his-capsule={touching:0.00} m ({touchedAt}) | by bearing, outline past his edge / drawn in him:{table}");
        }
        /// <summary>The bosses whose solids were made to match what they draw at the floor (D355, continued): walked into from any side, a hero stands with no more than a few centimetres of
        /// what it draws at the floor inside his capsule. Each was measured before its change, on the same walk (the line BODIES_APART walked-in, and the decision log).</summary>
        static IEnumerable<string> MatchedBosses()=>new[]{"BellowsBelow","FoundrySaint"};
        /// <summary>The bearings (of sixteen, counted from the side the fixture walks in on, the body facing it) each matched boss was made to match: all of the Bellows' dome; the Foundry
        /// Saint's sides, where its feet stood 0.65 m past the old frame. Its front and back are its fight's: its arms' collars stand at its back, where he strikes them.</summary>
        static int[] MatchedBearings(string id)=>id=="FoundrySaint"?new[]{3,4,5,11,12,13}:null;
        [UnityTest,Timeout(150000)] public IEnumerator AHeroWalkedIntoABossStandsOutsideWhatItDrawsAtTheFloor([ValueSource(nameof(MatchedBosses))] string id)
        {
            LogAssert.ignoreFailingMessages=true;float worst=0;string line="";
            yield return WalkedIn(id,(w,l)=>{worst=w;line=l;},MatchedBearings(id));
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.LessOrEqual(worst,.1f,line);
        }
        /// <summary>Measure, not judge: the same walk into every boss still on the list, for the lead's table.</summary>
        static IEnumerable<string> ListedBosses()=>new[]{"FoundrySaint","SlagColossus","Floodcrown","DessikVarn","Rotbloom","Burrower","Seedthane"};
        [UnityTest,Timeout(150000)] public IEnumerator HowFarAHeroWalkedIntoAListedBossStandsInIt([ValueSource(nameof(ListedBosses))] string id)
        {
            LogAssert.ignoreFailingMessages=true;string line="";
            yield return WalkedIn(id,(_,l)=>line=l);
            LogAssert.ignoreFailingMessages=false;
            TestContext.WriteLine(line);Debug.Log(line);Assert.IsNotEmpty(line);
        }

        // ================================================================== 6. the Burrower's wreck lies clear of the heroes (D374)
        /// <summary>When the Burrower has finished falling (a boss's 1.5 s, and a little).</summary>
        const float BurrowerFallen=1.6f;
        /// <summary>The Burrower killed with Taren at its side (nov22b-whole s06/053, 064): it rolled onto its lower side where it stood, its fins swung over him, and the wreck that takes
        /// its place there (BossWreck, at the body's own pose) stayed for the rest of the scene with him standing in it. Now its fall slides it off any hero it would lie in, as an
        /// ordinary body's does. Taren is stood just clear of it on each of sixteen bearings; the one where the old roll laid it deepest in him is where it is killed (shown to fail first:
        /// laid there by hand on the old roll). From the blow until its body starts to give way to the wreck, none of what it draws comes inside his capsule, nor, once it has fallen,
        /// within a quarter metre of it.</summary>
        [UnityTest,Timeout(150000)] public IEnumerator TheBurrowerKilledBesideTheHeroLiesClearOfHim()
        {
            LogAssert.ignoreFailingMessages=true;
            yield return Zone("chapter10","Arena_Ground","Taren");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Bodies fixture floor";floor.transform.position=new Vector3(230,-.5f,0);floor.transform.localScale=new Vector3(160,1,160);Physics.SyncTransforms();
            var lead=Lead;Place(Partner,new Vector3(280,0,40),Vector3.forward);Hold(lead);lead.Health.InvulnerableUntil=float.PositiveInfinity;
            var at=new Vector3(230,0,10);var boss=Body("Burrower",at,Vector3.back);yield return new WaitForSecondsRealtime(.8f);
            var outline=FloorOutline(boss,at.y);var shown=boss.GetComponent<DefeatPresentation>().visual;Assert.IsNotNull(shown,"fixture: the Burrower has a body to lay down");
            // Where a hero stands just clear of it, living, on a bearing: outside its floor outline and its capsule, and moved out until nothing it draws is in him.
            Vector3 Beside(int bearing)
            {
                var way=Quaternion.Euler(0,bearing*22.5f,0)*Vector3.forward;float r=Mathf.Max(outline[bearing],Capsule(boss))+Capsule(lead)+.05f;
                var living=DrawnPoints(boss,12000);
                for(int k=0;k<40;k++){Place(lead,at+way*r,-way);if(Inside(living,lead)<=0)break;r+=.1f;}
                return at+way*r;
            }
            // The fault first: the old roll by hand (DefeatPresentation's rest for it, onto its lower side where it stood), its lowest point on the floor, against him on every bearing.
            var rest=shown.localRotation;var rested=shown.localPosition;
            shown.localRotation=rest*Quaternion.Euler(-10,0,128);float lowest=DrawnPoints(boss,40000).Min(p=>p.y);shown.position+=Vector3.up*(at.y+.025f-lowest);
            var rolled=DrawnPoints(boss,12000);shown.localRotation=rest;shown.localPosition=rested;
            float old=0;int worst=0;string table="";
            for(int bearing=0;bearing<16;bearing++){Beside(bearing);float d=Inside(rolled,lead,LiesClear);table+=$" {bearing*22.5f:0}:{d:0.00}";if(d>old){old=d;worst=bearing;}}
            var place=Beside(worst);var lead0=lead.transform.position;
            Assert.AreEqual(0f,Inside(DrawnPoints(boss,12000),lead),.02f,"fixture: standing, the Burrower is not in him");
            Assert.Greater(old,.1f,"the check doesn't see the Burrower laid on the old roll in the hero:"+table);
            float full=boss.transform.localScale.x;
            var health=boss.Health;health.maximum=health.integrity=100;health.Damageable=true;health.InvulnerableUntil=0;health.ShieldUntil=0;health.Plate=null;health.Holds=null;
            health.Receive(new DamagePacket{source=lead.Health,amount=1e7f,type=DamageType.Kinetic});
            float falling=0,lying=0,began=GameTime.Now,watched=Time.unscaledTime;string when="";bool shot=false;int looked=0;
            while(Time.unscaledTime-watched<8&&boss!=null&&boss.transform.localScale.x>=full*.999f)
            {
                yield return null;float since=GameTime.Now-began;
                var drawn=DrawnPoints(boss,12000);falling=Mathf.Max(falling,Inside(drawn,lead));looked++;
                if(!shot&&since>=2.4f){shot=true;BossDeathTests.GameStill("burrower-lying-2.4s");}
                if(since<BurrowerFallen)continue;float d=Inside(drawn,lead,LiesClear);if(d>lying){lying=d;when=$"{since:0.00} s";}
            }
            float until=GameTime.Now-began;
            LogAssert.ignoreFailingMessages=false;
            string line=$"BODIES_APART Burrower killed at bearing {worst*22.5f:0} ({Flat(place,at):0.00} m out) old-roll={old:0.00} falling={falling:0.00} lying={lying:0.00} {when} watched-to={until:0.00} s frames={looked} heroMoved={Flat(lead.transform.position,lead0):0.00} | old roll by bearing:{table}";
            TestContext.WriteLine(line);Debug.Log(line);
            Assert.GreaterOrEqual(until,BurrowerFallen+.5f,"fixture: the body lay on for a while before giving way: "+line);
            Assert.LessOrEqual(falling,.03f,$"the Burrower killed at Taren's side fell {falling:0.00} m into him: "+line);
            Assert.LessOrEqual(lying,.03f,$"the Burrower killed at Taren's side lay {lying:0.00} m inside a quarter metre round him ({when}): "+line);
        }
    }
}
