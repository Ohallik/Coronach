using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Room and outpost dressing (Decks, Tallow, Sorrel) against D119 and the station's construction.</summary>
    public sealed class RoomDressingTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);
        }
        static IEnumerator Load(string zone,float wait=10)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+wait;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            Assert.AreEqual(zone,SceneManager.GetActiveScene().name);
            yield return new WaitForSecondsRealtime(.3f);
        }
        static Transform Dressing()=>SceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Room dressing").transform;
        static Vector3 Arrival()=>Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival").transform.position;
        static void ClearParty(){foreach(var c in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))c.enabled=false;}

        [UnityTest] public IEnumerator DressingNeverHidesAHeroFromTheFixedCamera([Values("Hub_Decks","TallowDrift","Sorrel_Ridges","Hushwell")] string zone)
        {
            yield return Load(zone);ClearParty();
            var root=Dressing();
            Assert.Greater(root.GetComponentsInChildren<MeshRenderer>().Length,15,zone+" has almost no dressing");
            var report=SightlineAudit.Run(root,Arrival());
            Assert.Greater(report.samples,250,zone+": the sweep sampled too little floor");
            Assert.IsEmpty(report.hidden,zone+": dressing hides the hero:\n"+report);
        }

        [UnityTest] public IEnumerator TheSightlineAuditCatchesATallPieceOnOpenFloor()
        {
            // Negative control: a 2.4 m crate stack dropped on the open promenade must be reported.
            yield return Load("Hub_Decks");ClearParty();
            var root=Dressing();
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="Negative control stack";
            Object.Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(root,false);block.transform.position=new Vector3(-20,1.2f,-4.6f);block.transform.localScale=new Vector3(1.2f,2.4f,1.2f);
            var report=SightlineAudit.Run(root,Arrival());
            Object.DestroyImmediate(block);
            Assert.IsTrue(report.offenders.Contains("Negative control stack"),"the audit missed a 2.4 m stack on open floor:\n"+report);
        }

        [UnityTest] public IEnumerator PrivateCabinsAreClosedToTheOfficeAndGallery()
        {
            yield return Load("Hub_Decks");
            // The 16 m cross walls left side gaps into the four cabins behind closed hatches.
            // Probe lines run through each gap between furniture, stopping short of the bunks inside.
            var gaps=new[]{(new Vector3(-38.5f,.5f,8.8f),new Vector3(-38.5f,.5f,10.9f)),(new Vector3(-37.85f,.5f,20.2f),new Vector3(-37.85f,.5f,18.3f)),
                (new Vector3(-18.15f,.5f,8.8f),new Vector3(-18.15f,.5f,10.9f)),(new Vector3(-18.15f,.5f,20.2f),new Vector3(-18.15f,.5f,18.3f))};
            foreach(var (a,b) in gaps)Assert.IsTrue(Physics.Linecast(a,b,out _,~0,QueryTriggerInteraction.Ignore),"cabin side gap is open at "+a);
            // Negative control: without the closing walls the same probes pass straight through.
            var walls=Dressing().Cast<Transform>().Where(t=>t.name=="Cabin end wall").ToArray();
            Assert.AreEqual(4,walls.Length,"expected four cabin end walls");
            foreach(var w in walls)w.gameObject.SetActive(false);Physics.SyncTransforms();
            foreach(var (a,b) in gaps)Assert.IsFalse(Physics.Linecast(a,b,out var hit,~0,QueryTriggerInteraction.Ignore),"control: something else closes the gap at "+a+" ("+(hit.collider!=null?hit.collider.name:"")+")");
            foreach(var w in walls)w.gameObject.SetActive(true);
        }

        // ---------- a danger marking is never hidden by ground paint (November 12) ----------
        /// <summary>The earliest a marking draws: its fill, then its outline, at their shaders' own queues, as Telegraph makes them.</summary>
        static int MarkingQueue()=>Mathf.Min(Resources.Load<Shader>("Effects/Telegraph").renderQueue,Resources.Load<Shader>("Effects/TelegraphFill").renderQueue);
        static bool IsMarking(Material m)=>m.shader!=null&&m.shader.name.StartsWith("Lattice/Telegraph");
        /// <summary>Paint blended onto a floor: RoomDressing's and the ground kit's "Floor detail" batches by their name, and anything else flat whose underside lies on the
        /// ground right under it (a soft scorch, a puddle, a stencilled line). Words on a sign, markings themselves and what belongs to a body are not ground paint.</summary>
        static bool LiesOnTheFloor(Renderer r)
        {
            if(!r.enabled||!r.gameObject.activeInHierarchy||r.GetComponent<TMPro.TMP_Text>()!=null||r.GetComponentInParent<CombatActor>()!=null||r.GetComponentInParent<Npc>()!=null)return false;
            if(!r.sharedMaterials.Any(m=>m!=null&&!IsMarking(m)&&m.renderQueue>(int)RenderQueue.GeometryLast))return false;
            if(r.name.StartsWith("Floor detail"))return true;
            var b=r.bounds;if(Mathf.Max(b.size.x,b.size.z)<.2f||b.size.y>.15f*Mathf.Max(b.size.x,b.size.z))return false;
            return Physics.Raycast(new Vector3(b.center.x,b.max.y+.3f,b.center.z),Vector3.down,out var floor,b.size.y+.6f,~0,QueryTriggerInteraction.Ignore)&&floor.point.y>=b.min.y-.25f;
        }
        static string Over(Renderer r,int marking)
        {
            foreach(var m in r.sharedMaterials)
                if(m!=null&&!IsMarking(m)&&m.renderQueue>=marking)return $"{r.name} at ({r.bounds.center.x:0.0}, {r.bounds.center.z:0.0}) draws {m.name} at queue {m.renderQueue}, at or after a marking ({marking}), so it lies over one";
            return null;
        }
        /// <summary>Ground paint is blended over the floor after the floor is drawn, so whatever of it draws after a marking's fill and outline lies over the marking.
        /// RoomDressing's ground patches did, at the plain transparent queue (3000), until November 12. In every zone, everything lying on a floor draws first.</summary>
        [UnityTest] public IEnumerator NothingLyingOnAFloorDrawsOverADangerMarking()
        {
            int marking=MarkingQueue(),read=0;var late=new List<string>();
            Assert.Less(marking,(int)RenderQueue.Transparent,"fixture: markings draw at their own queue, before the plain transparent one");
            foreach(var zone in ContentRegistry.Zones)
            {
                yield return Load(zone,25);
                foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    if(LiesOnTheFloor(r)){read++;var over=Over(r,marking);if(over!=null)late.Add(zone+": "+over);}
            }
            Assert.Greater(read,300,"fixture: the zones' ground paint");
            Assert.IsEmpty(late,"ground paint that lies over a danger marking:\n"+string.Join("\n",late.Distinct().Take(40)));
        }
        [UnityTest] public IEnumerator TheMarkingOrderCheckCatchesPaintDrawnLate()
        {
            // Negative control: a ground patch at the plain transparent queue, and a flat unnamed patch lying on the ground at it, are both reported.
            yield return Load("Sorrel_Ridges");ClearParty();
            int marking=MarkingQueue();var at=Arrival();
            var paint=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).First(r=>r.name.StartsWith("Floor detail")&&r.sharedMaterial!=null&&r.sharedMaterial.renderQueue>(int)RenderQueue.GeometryLast);
            Assert.IsNull(Over(paint,marking),"fixture: Sorrel's ground paint draws before markings");
            var late=new Material(paint.sharedMaterial){renderQueue=(int)RenderQueue.Transparent};
            var named=GameObject.CreatePrimitive(PrimitiveType.Quad);named.name="Floor detail control";
            var flat=GameObject.CreatePrimitive(PrimitiveType.Quad);flat.name="Negative control scorch";
            try
            {
                // Each laid on the ground under its own spot: the arrival is a raised pad, and a patch laid at its height three metres off it hung in the air.
                foreach(var (quad,dx) in new[]{(named,3f),(flat,-3f)})
                {
                    Object.DestroyImmediate(quad.GetComponent<Collider>());quad.GetComponent<Renderer>().sharedMaterial=late;quad.transform.localScale=Vector3.one*2;
                    Assert.IsTrue(Physics.Raycast(at+new Vector3(dx,8,0),Vector3.down,out var ground,30,~0,QueryTriggerInteraction.Ignore),"fixture: ground beside the arrival");
                    quad.transform.SetPositionAndRotation(ground.point+Vector3.up*.03f,Quaternion.Euler(90,0,0));
                }
                Physics.SyncTransforms();
                foreach(var quad in new[]{named,flat})
                {
                    var r=quad.GetComponent<Renderer>();
                    Assert.IsTrue(LiesOnTheFloor(r),quad.name+" lies on the ground and the check doesn't see it as ground paint");
                    Assert.IsNotNull(Over(r,marking),quad.name+" draws after the markings and the check misses it");
                }
            }
            finally{Object.DestroyImmediate(named);Object.DestroyImmediate(flat);Object.DestroyImmediate(late);}
        }
    }
}
