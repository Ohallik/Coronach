using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Round eight's small things in the scenery, in the built scenes: what the looks kept seeing (docs/qa/ISSUES.md W8-many-hands-02, W5-many-hands-02), each measure
    /// shown to reject the state as it was first, then asked of the scene.</summary>
    public sealed class SceneryRoundEightTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("chapter5");
        }
        [UnityTearDown] public IEnumerator Cleanup(){GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static IEnumerator Load(string zone,string spawn)
        {
            SceneFlow.Current.LoadZone(zone,spawn);float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");yield return null;yield return null;
        }

        // ------------------------------------------------------------------ the concourse's brass inlay (W8-many-hands-02)
        const float InlayZ=9.8f,InlayWest=-35.5f,InlayEast=52.5f;
        /// <summary>Orange as a danger marking is orange (D217): an amber hue at a marking's saturation. Brass is the same hue far paler.</summary>
        static bool Orange(Color c){Color.RGBToHSV(c,out float h,out float s,out _);return h>.03f&&h<.12f&&s>.65f;}
        /// <summary>What the concourse's warm light and the grade make of a floor tone in the player: red lifted by half, blue dropped. Measured on three stills: the old inlay's
        /// .79/.52/.17 drew 252/161/32 (nov26-r7-chain s30/423), round eight's first, .77/.62/.36, drew 250/174/65 (nov26-r8-chain s30/419: the tone alone did not read as orange, and
        /// both reds are at the top of the range), and its second, .53/.52/.42, drew 208/158/77 (nov26-recon-r8b-30 s30/419), from which the gains are taken.</summary>
        static Color Drawn(Color tone)=>new Color(Mathf.Min(1,tone.r*1.53f),Mathf.Min(1,tone.g*1.2f),Mathf.Min(1,tone.b*.72f));
        /// <summary>Along a stretch of the north run, the inlay is two lines with the floor showing between them: nothing within 8 cm of the run's middle, something either side of it.</summary>
        static bool Paired(IEnumerable<Vector3> points)
        {
            var near=points.Where(p=>p.x>30&&p.x<50&&Mathf.Abs(p.z-InlayZ)<.45f).Select(p=>p.z-InlayZ).ToArray();
            return near.Length>0&&!near.Any(d=>Mathf.Abs(d)<.08f)&&near.Any(d=>d>.1f)&&near.Any(d=>d<-.1f);
        }
        /// <summary>Each end of the run is a bar across both its lines a little past where they stop, so it reads as ended, not cut off (and not as an arrow's point).</summary>
        static bool Ended(IEnumerable<Vector3> points)=>new[]{InlayWest-.1f,InlayEast+.1f}.All(x=>new[]{-.3f,.3f}.All(dz=>points.Any(p=>Mathf.Abs(p.x-x)<.03f&&Mathf.Abs(p.z-InlayZ-dz)<.03f)));

        [UnityTest] public IEnumerator TheConcoursesInlayIsAPairOfBrassLinesThatEndNotAnOrangeLineAcrossTheFloor()
        {
            // As it was: one quad strip .1 m wide down the run, its vertex brass on the brass paint (ManyHandsBuilder.Hands.LinerFloor before round eight).
            var paint=new Color(.92f,.74f,.42f);var wasTone=new Color(.86f,.7f,.4f)*paint;
            var was=new List<Vector3>();for(float x=InlayWest;x<52;x+=2)was.AddRange(new[]{new Vector3(x,0,InlayZ-.05f),new Vector3(x+2,0,InlayZ-.05f),new Vector3(x+2,0,InlayZ+.05f),new Vector3(x,0,InlayZ+.05f)});
            Assert.IsTrue(Orange(Drawn(wasTone)),"the measure reads the old inlay's "+wasTone+" as orange (nov26-r7-chain s30/423)");
            Assert.IsTrue(Orange(Drawn(new Color(.84f,.84f,.86f)*paint)),"the measure reads round eight's first inlay as orange, as the player drew it (nov26-r8-chain s30/419)");
            Assert.IsFalse(Paired(was),"the measure reads the old inlay as one line");Assert.IsFalse(Ended(was),"the measure reads the old inlay as cut off");

            yield return Load("ManyHands","Arrival");
            var floor=Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).FirstOrDefault(f=>f.name=="Floor detail the liner's medallions and inlay");
            Assert.IsNotNull(floor,"fixture: the liner's floor detail isn't in the scene");
            var mesh=floor.sharedMesh;var view=floor.GetComponent<MeshRenderer>();Assert.IsTrue(mesh.isReadable,"fixture: the floor detail's mesh can't be read");
            var points=mesh.vertices.Select(v=>floor.transform.TransformPoint(v)).ToArray();var tones=mesh.colors;
            Color body=view.sharedMaterial.HasProperty("_BaseColor")?view.sharedMaterial.GetColor("_BaseColor"):Color.white;
            var run=Enumerable.Range(0,points.Length).Where(i=>points[i].x>InlayWest-1&&points[i].x<InlayEast+1&&Mathf.Abs(points[i].z-InlayZ)<.45f).ToArray();
            Assert.Greater(run.Length,40,"fixture: no inlay down the promenade's north side");
            foreach(int i in run)Assert.IsFalse(Orange(Drawn(tones[i]*body)),$"the inlay at {points[i]} has the tone {tones[i]*body}, which the concourse's light draws {Drawn(tones[i]*body)}: a danger marking's orange on the floor (D217)");
            TestContext.WriteLine($"HANDS_INLAY tone={tones[run[0]]*body} drawn={Drawn(tones[run[0]]*body)}");
            Assert.IsTrue(Paired(points),"the inlay down the promenade is one line, not a pair with the floor between");
            Assert.IsTrue(Ended(points),"the inlay's run stops in the open with no end to it");
        }

        // ------------------------------------------------------------------ where Idra is set down at the dock (W5-many-hands-02)
        static readonly Vector3 CrateWas=new(-1.1141355f,.5683371f,-59.622112f);static readonly Quaternion CrateTurnWas=new(0,.8237066f,0,-.56701624f);
        /// <summary>What solid stands within a quarter metre of a body set down at this place, other than the party and the deck under it.</summary>
        static Collider[] Against(Vector3 at)
        {
            Physics.SyncTransforms();
            return Physics.OverlapCapsule(at+Vector3.up*.6f,at+Vector3.up*1.5f,.35f+.25f,~0,QueryTriggerInteraction.Ignore)
                .Where(c=>c.GetComponentInParent<CombatActor>()==null&&c.bounds.max.y>at.y+.3f&&c.bounds.min.y>at.y-1).ToArray();
        }
        [UnityTest] public IEnumerator ThePartnerIsSetDownAtTheDockClearOfItsCrates()
        {
            yield return Load("ManyHands","Arrival");
            var party=PartyController.Current;var partner=party.members.First(m=>m!=null&&m!=party.Active);var at=partner.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(at.x,at.z),new Vector2(-2,-60)),1,"fixture: the partner isn't set down where the capture had her (-2, -60): "+at);
            // As it was: the deck crate the dock's scatter stood with its corner on that place (nov25-whole s54/111), put back where it stood.
            var crate=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent==null&&t.name=="DeckCrate").OrderBy(t=>Vector3.Distance(t.position,CrateWas)).FirstOrDefault();
            Assert.IsNotNull(crate,"fixture: no deck crate on the dock");Assert.Less(Vector3.Distance(crate.position,CrateWas),2.5f,"fixture: the dock's crate isn't the one beside the arrival");
            var old=Object.Instantiate(crate.gameObject,CrateWas,CrateTurnWas);
            Assert.IsTrue(Against(at).Any(c=>c.transform.IsChildOf(old.transform)),"the measure doesn't find the crate as it stood, its corner on the place she is set down");
            Object.DestroyImmediate(old);
            var against=Against(at);
            Assert.IsEmpty(against,"the partner is set down against "+string.Join(", ",against.Select(c=>c.name+" at "+c.bounds.center)));
        }
    }
}
