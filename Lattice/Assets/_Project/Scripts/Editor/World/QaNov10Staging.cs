using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The world half of the November 10 pass on the QA log's last open looks (docs/qa/ISSUES.md). Isolated: each step clears the roots it
    /// names and sets the few existing things it touches to absolute values, so a second run lands where the first did. Nothing it adds is
    /// solid unless it says so, and no zone's navigation changes. Runs after ChapterOneStaging: <c>Apply</c>, marker <c>QA_NOV10_STAGED_OK</c>;
    /// <c>-qa-zones a,b</c> limits it to those scenes.
    /// </summary>
    public static class QaNov10Staging
    {
        const string WorldMaterials="Assets/_Project/Resources/WorldMaterials/";
        public static void Apply()=>BatchTools.Run(()=>
        {
            string only=DevArgs.Value("-qa-zones");var zones=only?.Split(',');
            void Stage(string zone,System.Action step)
            {
                if(zones!=null&&!zones.Contains(zone))return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");step();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("QA_NOV10_STAGED "+zone);
            }
            Stage("Hushwell",SealFrames);
            Stage("Gullet_Tunnel",Mouth);
            Stage("Sorrel_Ridges",()=>{SlopeStrata();RigTorn();WreckSmoke();});
            Stage("TallowApproach",HullPanels);
            AssetDatabase.SaveAssets();Debug.Log("QA_NOV10_STAGED_OK");
        });
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static void ClearNamed(string name){foreach(var go in All<Transform>().Where(t=>t!=null&&t.name==name).Select(t=>t.gameObject).ToArray())if(go!=null)Object.DestroyImmediate(go);}
        /// <summary>A kit piece sized by its height and stood on a floor height the caller knows, centred where it's asked for. Drawn only.</summary>
        static GameObject Stand(string key,Transform parent,Vector3 at,float height,float yaw,float sink=0)
        {
            var go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*height,"Rock",false);go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var b=ModelGeometry.BoundsOf(go);go.transform.position+=new Vector3(at.x,at.y-sink,at.z)-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
            go.transform.SetParent(parent,true);return go;
        }
        /// <summary>A kit piece laid along a line, its long side stretched to the line's length, standing on the line's height.</summary>
        static GameObject Span(string key,Transform parent,Vector3 from,Vector3 to,float height,float sink=0)
        {
            var go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*height,"Rock",false);
            var b=ModelGeometry.BoundsOf(go);bool alongX=b.size.x>=b.size.z;float length=Vector3.Distance(new Vector3(from.x,0,from.z),new Vector3(to.x,0,to.z));
            var s=go.transform.localScale;if(alongX)s.x*=length/Mathf.Max(.01f,b.size.x);else s.z*=length/Mathf.Max(.01f,b.size.z);go.transform.localScale=s;
            var d=to-from;go.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg-(alongX?90:0),0);
            b=ModelGeometry.BoundsOf(go);var mid=(from+to)*.5f;go.transform.position+=new Vector3(mid.x,mid.y-sink,mid.z)-new Vector3(b.center.x,b.min.y,b.center.z);
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
            go.transform.SetParent(parent,true);return go;
        }
        static Material Variant(string name,string source,System.Action<Material> tune)
        {
            var from=AssetDatabase.LoadAssetAtPath<Material>(source)??throw new System.InvalidOperationException("Missing material "+source);
            string path=WorldMaterials+name+".mat";var made=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(made==null){made=new Material(from);AssetDatabase.CreateAsset(made,path);}
            made.CopyPropertiesFromMaterial(from);tune(made);EditorUtility.SetDirty(made);return made;
        }
        static void Tint(GameObject go,Color tint){var t=go.GetComponent<PropTint>()??go.AddComponent<PropTint>();t.tint=tint;}

        // ---------- Hushwell: the Bellows' doors (E4-hushwell-04) ----------
        /// <summary>How tall a door's sheet stands: well over a hero's head, low enough to read as a door between two posts and not a wall of navy veins, and its posts low enough
        /// that Hushwell's shallow camera sees over them to the chamber's name on the floor beyond (at 4.7 m the entry's post stood in that line).</summary>
        const float SealHeight=2.6f,JambWidth=1.5f;
        /// <summary>The Bellows' doors were bare 15 by 6 m slabs of the Gullet's membrane, wider than the way they close, so their hard ends stood out over the rock with nothing round them:
        /// a sheet of veined navy propped at the chamber's edge. Each is now the way's own width, tucked at both ends into a carved post of the cave's stone, with a low sill under it,
        /// so open or shut it's a doorway. The sheet's box is still what blocks the way, and its carving obstacle scales with it; the posts and sill are drawn only.</summary>
        static void SealFrames()
        {
            ClearNamed("Seal frame");
            foreach(var seal in All<Membrane>().Where(m=>m.name=="Bellows entry"||m.name=="Bellows exit"))
            {
                var at=seal.transform.position;float floor=HushwellLayout.Level(at.z);
                float left=Edge(at.x,at.z,-1),right=Edge(at.x,at.z,1),width=right-left;
                seal.transform.SetPositionAndRotation(new Vector3((left+right)*.5f,floor+SealHeight*.5f,at.z),Quaternion.identity);
                seal.transform.localScale=new Vector3(width+1.4f,SealHeight,.8f);
                var frame=new GameObject("Seal frame").transform;frame.SetParent(seal.transform.parent,true);
                // A jamb, not a pillar: the column fitted by height came out 3.7 m across and stood on the chamber's floor sign. Slimmed to a post, its outer half in the wall.
                foreach(var (x,yaw) in new[]{(left-.2f,90f),(right+.2f,-90f)})
                {
                    var jamb=Stand("HushwellColumn",frame,new Vector3(x,floor,at.z),SealHeight+.5f,yaw,.1f);
                    var b=ModelGeometry.BoundsOf(jamb);float slim=JambWidth/Mathf.Max(b.size.x,b.size.z);
                    jamb.transform.localScale=Vector3.Scale(jamb.transform.localScale,new Vector3(slim,1,slim));
                    b=ModelGeometry.BoundsOf(jamb);jamb.transform.position+=new Vector3(x-b.center.x,0,at.z-b.center.z);
                }
                Span("HushwellLowRidge",frame,new Vector3(left-.3f,floor,at.z),new Vector3(right+.3f,floor,at.z),.45f,.14f).name="Seal sill";
                Debug.Log($"QA_SEAL_FRAME {seal.name} left={left:F2} right={right:F2} floor={floor}");
            }
        }
        /// <summary>Where the cave floor along a door's line gives way to rock, walking out from its middle.</summary>
        static float Edge(float x,float z,int side){float step=.2f*side;while(Mathf.Abs(x)<40&&HushwellLayout.Clear(new Vector2(x+step,z))<0)x+=step;return x;}

        // ---------- the Gullet's mouth (E2-gullet-15) ----------
        /// <summary>The arrival showed cell floor and two ships, nothing that says the party has just flown into something: the builder's lip folds stand outside the tube, past the
        /// wall the camera sees. A lower lip now curves across the tube just behind where the ships come in, rising to tall folds where it meets each wall, with a row of pale
        /// teeth along its inner edge, so the first frame is a way in through a mouth. Drawn only: no ship goes back that way, and the collision shell is untouched. It stays low
        /// over the middle, under the flight plane, so nothing stands between the camera and a ship.</summary>
        static void Mouth()
        {
            ClearNamed("Mouth lower lip");
            var root=new GameObject("Mouth lower lip").transform;
            var flesh=Variant("gullet-lip",AssetDatabase.GetAssetPath(GulletWallMaterial()),m=>{m.SetColor("_BaseColor",new Color(.66f,.5f,.82f));m.SetColor("_EmissionColor",new Color(.06f,.03f,.1f));});
            var tooth=Variant("gullet-tooth","Assets/_Project/Art/Generated/Models/HushwellStalagmites/HushwellStalagmites_Toon.mat",m=>{m.SetTexture("_BaseMap",null);m.SetColor("_BaseColor",new Color(.9f,.86f,.94f));m.SetColor("_EmissionColor",new Color(.12f,.1f,.16f));});
            const int Pieces=11;float left=GulletProfile.LeftEdge(MouthZ(-1)),right=GulletProfile.RightEdge(MouthZ(1));
            Vector3 Point(float u){float x=Mathf.Lerp(left+1.2f,right-1.2f,(u+1)*.5f),z=MouthZ(u);return new Vector3(x,Bed(x,z),z);}
            for(int i=0;i<Pieces;i++)
            {
                float u0=Mathf.Lerp(-1,1,i/(float)Pieces),u1=Mathf.Lerp(-1,1,(i+1)/(float)Pieces),u=(u0+u1)*.5f;
                // Overlapping folds, tall where the lip meets the wall and low over the lane the ships use.
                Vector3 a=Point(u0),b=Point(u1),grow=(b-a)*.18f;
                float height=Mathf.Lerp(1.5f,6.5f,Mathf.Pow(Mathf.Abs(u),2.2f));
                var fold=Span(i%2==0?"GulletWallA":"GulletWallB",root,a-grow,b+grow,height,.6f);fold.name="Lip fold";
                foreach(var r in fold.GetComponentsInChildren<Renderer>())r.sharedMaterial=flesh;
                if(Mathf.Abs(u)>.86f)continue;
                // Teeth on the lip's inner (north) edge, leaning in toward the throat.
                var m=Point(u);float tz=m.z+1.5f;
                var t=Stand("HushwellStalagmites",root,new Vector3(m.x,Bed(m.x,tz),tz),Mathf.Lerp(1.3f,2f,Mathf.Abs(u)),(i*37)%60,.15f);t.name="Lip tooth";
                t.transform.RotateAround(new Vector3(m.x,Bed(m.x,tz),tz),Vector3.right,14);
                foreach(var r in t.GetComponentsInChildren<Renderer>())r.sharedMaterial=tooth;
            }
        }
        /// <summary>The lip's line: across the tube a few metres behind where the ships come in (z 8), bowed back toward the opening in the middle, so the play camera's first frame has it along its foot.</summary>
        static float MouthZ(float u)=>6.4f-2.2f*(1-u*u);
        static Material GulletWallMaterial()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/GulletWallA.prefab")??throw new System.InvalidOperationException("Missing GulletWallA");
            return prefab.GetComponentInChildren<Renderer>().sharedMaterial;
        }
        /// <summary>The tube's bed under a point, from its collision shell; its middle sits a metre under the flight plane.</summary>
        static float Bed(float x,float z)
        {
            Physics.SyncTransforms();
            foreach(var hit in Physics.RaycastAll(new Vector3(x,12,z),Vector3.down,30,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))if(hit.normal.y>.8f)return hit.point.y;
            return -1;
        }

        // ---------- Tallow's hull up close (E1-relay-24, E2-tallow-11) ----------
        /// <summary>The hull's generated paint is a 1024 square for a whole station, smeared where the projection stretched it, so at the dock its panels and windows were smudges
        /// beside the crisp ships and pad. A larger copy of the same paint would only be sharper smudges; crisp plating laid over it in world space (LatticeToon's hull panels,
        /// about four metres a tile) gives the eye seams and rivets at the distance it's seen from, and leaves its colours and lights as they were.</summary>
        static void HullPanels()
        {
            const string Hull="Assets/_Project/Art/Generated/Models/TallowStationHull/TallowStationHull_Toon.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(Hull)??throw new System.InvalidOperationException("Missing "+Hull);
            var seams=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Dressing/HullPanels_mask.png")??throw new System.InvalidOperationException("Missing hull panel map");
            material.SetFloat("_Panels",1);material.EnableKeyword(new UnityEngine.Rendering.LocalKeyword(material.shader,"_PANELS_ON"));material.SetTexture("_PanelMap",seams);material.SetVector("_PanelParams",new Vector4(.25f,.5f,0,0));
            EditorUtility.SetDirty(material);
        }

        // ---------- Sorrel: the arena's slope, the torn rig, the smoke (E2-sorrel-07, E2-gullet-06, E2-hushwell-16) ----------
        /// <summary>The slope beside the Burrower's arena fills most of the screen in the fight's second half, and was soft airbrushed bands beside crisp floor. Kit strata laid on it
        /// read as the curved brick walls an earlier review threw out, so the ground's own strata get cut edges and a ledge line instead, on steep faces only (LatticeToon's
        /// _StrataSlope.w; the floor is masked out by slope as before).</summary>
        static void SlopeStrata()
        {
            ClearNamed("Arena slope strata");
            var ground=AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+"sorrel-ground.mat")??throw new System.InvalidOperationException("Missing sorrel-ground");
            var slope=ground.GetVector("_StrataSlope");slope.w=1;ground.SetVector("_StrataSlope",slope);
            var bands=ground.GetVector("_StrataParams");bands.x=.85f;bands.y=.9f;ground.SetVector("_StrataParams",bands);
            EditorUtility.SetDirty(ground);
        }
        static readonly Color Burned=new(.44f,.39f,.36f);
        /// <summary>The site's rig still stood square and clean beside the hole the boss came out of: a 4 degree lean and a warm tint read as tidy machinery. It's the Burrower's deck,
        /// and the Burrower tore itself out of it: canted hard toward the bore, sooted near black, with deck plates torn up and lying askew round its footing, dark scorch across
        /// the plates, and smoke still rising from it. Drawn only, like ChapterOneStaging's lean, which this replaces: the wrapper's collider is the same box it was.</summary>
        static void RigTorn()
        {
            ClearNamed("Rig torn plates");ClearNamed("Rig smoke");ClearNamed("Rig deck scorch");
            var rig=All<Transform>().First(t=>t.name=="OutpostDrill"&&t.parent==null);
            Tint(rig.gameObject,Burned);
            var body=rig.GetChild(0);body.localRotation=Quaternion.identity;
            body.position+=rig.position-ModelGeometry.BoundsOf(body.gameObject).center;
            var upright=ModelGeometry.BoundsOf(body.gameObject);
            float floor=upright.min.y;var c=upright.center;var e=upright.extents;
            // Scorch on the deck plates themselves, which stay level as the tower cants on them.
            var scorch=new GameObject("Rig deck scorch").transform;
            var soot=ChapterOneStaging.Soft("rig-deck-scorch",new Color(.02f,.016f,.016f,.62f));
            foreach(var (dx,dz,rx,rz,yaw) in new[]{(.35f,-.3f,.3f,.22f,20f),(.6f,.35f,.18f,.16f,-30f),(-.35f,-.6f,.2f,.18f,60f),(.1f,-.7f,.16f,.12f,10f)})
                ChapterOneStaging.SoftPatch("Deck scorch",scorch,new Vector3(c.x+dx*e.x,DeckTop(rig,c+new Vector3(dx*e.x,0,dz*e.z),floor)+.03f,c.z+dz*e.z),rx*e.x*2,rz*e.z*2,yaw,soot,false);
            // Where plates were torn up: dark bays in the deck with their edges showing, next to the plates that lie askew.
            var bay=ChapterOneStaging.Soft("rig-deck-bay",new Color(.012f,.01f,.012f,.92f));
            foreach(var (dx,dz) in new[]{(.62f,-.55f),(-.2f,-.78f)})
                ChapterOneStaging.SoftPatch("Torn plate bay",scorch,new Vector3(c.x+dx*e.x,DeckTop(rig,c+new Vector3(dx*e.x,0,dz*e.z),floor)+.035f,c.z+dz*e.z),.95f,.8f,15,bay,false);
            body.localRotation=Quaternion.Euler(4,0,-11);body.position+=Vector3.down*.42f;
            var parts=new GameObject("Rig torn plates").transform;
            foreach(var (dx,dz,yaw,tilt,size) in new[]{(1.15f,-.55f,25f,24f,2.6f),(-1.1f,.9f,-40f,-18f,2.2f),(.3f,1.25f,70f,30f,1.9f),(-.6f,-1.2f,10f,-12f,2.4f)})
            {
                var at=new Vector3(c.x+dx*e.x,floor,c.z+dz*e.z);
                var plate=WorldBuilder.Piece("DeckFloor",Vector3.zero,new Vector3(size,.14f,size*.8f),"Ground",false);plate.name="Torn deck plate";
                plate.transform.SetPositionAndRotation(at+Vector3.up*(size*.1f),Quaternion.Euler(tilt,yaw,tilt*.4f));plate.transform.SetParent(parts,true);
                Tint(plate,new Color(.36f,.33f,.32f));
            }
            Smoke("Rig smoke",new Vector3(c.x,DeckTop(rig,c,floor)+.4f,c.z),1.1f,null);
        }
        /// <summary>The top of the deck under a point: the plates are their own flat pieces round the rig, with no collider of their own, so it's the highest low flat thing drawn there.</summary>
        static float DeckTop(Transform rig,Vector3 at,float floor)
        {
            float top=floor;
            foreach(var r in All<MeshRenderer>())
            {
                if(r.transform.IsChildOf(rig)||!r.enabled||r.name.Contains("scorch")||r.name.Contains("bay"))continue;
                var b=r.bounds;if(b.size.y>1.2f||b.max.y<=floor+.02f||b.max.y>floor+1.2f)continue;
                if(at.x<b.min.x||at.x>b.max.x||at.z<b.min.z||at.z>b.max.z)continue;
                top=Mathf.Max(top,b.max.y);
            }
            Debug.Log($"QA_DECK_TOP at={at} floor={floor:F2} top={top:F2}");
            return top;
        }
        /// <summary>The Burrower's wreck smokes too, where it burned out; it shows only when BossWreck shows the wreck.</summary>
        static void WreckSmoke()
        {
            var wreck=All<Transform>().FirstOrDefault(t=>t.name=="Burrower wreck"&&t.parent==null);if(wreck==null)return;
            foreach(var old in wreck.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Wreck smoke").ToArray())Object.DestroyImmediate(old.gameObject);
            var b=ModelGeometry.BoundsOf(wreck.Find("Rig").gameObject);
            Smoke("Wreck smoke",new Vector3(b.center.x,b.max.y-.3f,b.center.z),.8f,wreck);
        }
        /// <summary>A slow dark column of soft puffs: what says a machine burned, from any distance. A few dozen particles at most.</summary>
        static void Smoke(string name,Vector3 at,float scale,Transform parent)
        {
            var go=new GameObject(name,typeof(ParticleSystem));go.transform.position=at;go.transform.rotation=Quaternion.Euler(-90,0,0);if(parent!=null)go.transform.SetParent(parent,true);
            var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main;main.loop=true;main.prewarm=true;main.duration=6;main.startLifetime=new ParticleSystem.MinMaxCurve(5,7);main.startSpeed=new ParticleSystem.MinMaxCurve(.55f*scale,.85f*scale);
            main.startSize=new ParticleSystem.MinMaxCurve(1.2f*scale,1.8f*scale);main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.maxParticles=40;main.simulationSpace=ParticleSystemSimulationSpace.World;
            main.startColor=new Color(.16f,.14f,.14f,1);main.scalingMode=ParticleSystemScalingMode.Hierarchy;
            var emission=ps.emission;emission.rateOverTime=4.5f;
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=9;shape.radius=.5f*scale;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.6f),new Keyframe(1,2.6f)));
            var colour=ps.colorOverLifetime;colour.enabled=true;var fade=new Gradient();
            fade.SetKeys(new[]{new GradientColorKey(new Color(.22f,.2f,.19f),0),new GradientColorKey(new Color(.42f,.4f,.38f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.5f,.15f),new GradientAlphaKey(.3f,.6f),new GradientAlphaKey(0,1)});
            colour.color=fade;
            var drift=ps.velocityOverLifetime;drift.enabled=true;drift.space=ParticleSystemSimulationSpace.World;drift.x=new ParticleSystem.MinMaxCurve(.25f,.45f);drift.y=new ParticleSystem.MinMaxCurve(0,0);drift.z=new ParticleSystem.MinMaxCurve(.05f,.15f);
            var view=go.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=Resources.Load<Material>("Effects/circle_02");view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;view.sortingFudge=2;
        }
    }
}
