using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The ground of the zones retrofitted with the ground kit (November 11): a fixed three-quarter camera shows mostly floor, and these floors were one flat tone with sparse props
    /// (tools/look_density.py: a largest plain area of .34 to .61 of the frame, against .05 to .21 on the dressed Decks). Isolated, like QaNov10Staging: each zone's step replaces
    /// only the <c>Ground kit</c> root and the floor's own weights and material, adds nothing solid and changes no navigation, so a second run lands where the first did. It runs
    /// last, after every builder and staging pass of its zone (a LanternRoadBuilder rebuild makes a new floor mesh and puts the plain material back): <c>Apply</c>, marker
    /// <c>GROUND_STAGED_OK</c>; <c>-ground-zones a,b</c> limits it to those scenes. A zone with a fault is left unsaved (read <c>GROUND_PROBLEM</c>).
    /// </summary>
    public static class GroundStaging
    {
        public static readonly string[] Zones={"LanternGorge","Lanternwick","Sorrel_Ridges","Hushwell"};
        [Restage.Step("ground","LanternGorge,Lanternwick,Sorrel_Ridges,Hushwell",After="chapter1.town-life,chapter1.side-stories,chapter1.nov10",Chapter=2,ScenesArg="-ground-zones",Leaves=new[]{"LanternGorge: Ground kit #1","Lanternwick: Ground kit #1","Sorrel_Ridges: Ground kit #1","Hushwell: Ground kit #1"})]
        public static void Apply()=>BatchTools.Run(()=>
        {
            string only=DevArgs.Value("-ground-zones");var zones=only?.Split(',');
            void Stage(string zone,Action step)
            {
                if(zones!=null&&!zones.Contains(zone))return;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");step();
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("GROUND_STAGED "+zone);
            }
            FloorDetailOrder();
            Stage("LanternGorge",Gorge);Stage("Lanternwick",Lanternwick);Stage("Sorrel_Ridges",Sorrel);Stage("Hushwell",Hushwell);
            AssetDatabase.SaveAssets();Debug.Log("GROUND_STAGED_OK");
        });
        /// <summary>Read-only: states, for the Lantern Road's landforms, the measures the PlayMode suite holds them to (the teeth along the floor's edge, floor and wall meeting in
        /// shade, level floor lit in one toon band, lamps not burning the relay's walls, nothing of the kit's standing between the camera and anyone), so a rebuild can be read
        /// without a test run. Marker <c>GROUND_AUDIT_OK</c>.</summary>
        public static void Audit()=>BatchTools.Run(()=>
        {
            T[] All<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var (zone,least,most) in new[]{("LanternGorge",new Vector2(-6,0),new Vector2(6,30)),("Lanternwick",new Vector2(-14,-20),new Vector2(14,40)),("SunkenRelay",new Vector2(-7,-6),new Vector2(7,18))})
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                var floorView=GroundKit.FindFloor();var floor=floorView.sharedMesh;var walls=All<MeshFilter>().Single(f=>f.name.EndsWith(" cliffs")).sharedMesh;
                var floorAt=floor.vertices;var wallAt=walls.vertices;var wallTone=walls.colors;
                Vector2Int Key(Vector3 p)=>new(Mathf.RoundToInt(p.x*4),Mathf.RoundToInt(p.z*4));
                var onFloor=new HashSet<Vector2Int>();foreach(int i in floor.triangles)onFloor.Add(Key(floorAt[i]));
                int meet=0,shaded=0;for(int i=0;i<wallAt.Length;i++)if(onFloor.Contains(Key(wallAt[i]))){meet++;if(wallTone[i].grayscale<.75f)shaded++;}
                var sun=All<Light>().Single(l=>l.type==LightType.Directional);var toSun=-sun.transform.forward;float steps=floorView.GetComponent<Renderer>().sharedMaterial.GetFloat("_RampSteps");
                int Band(Vector3 normal)=>Mathf.FloorToInt(Vector3.Dot(normal,toSun)*steps+.5f);
                var normals=floor.normals;int level=Band(Vector3.up),off=0,read=0;
                for(int i=0;i<floorAt.Length;i++){var p=floorAt[i];if(p.x<least.x||p.x>most.x||p.z<least.y||p.z>most.y)continue;read++;if(Band(normals[i])!=level)off++;}
                int hiding=0;var kit=GameObject.Find(GroundKit.RootName);
                if(kit!=null)foreach(var piece in kit.GetComponentsInChildren<MeshRenderer>())foreach(var person in All<Lattice.World.Npc>())foreach(float h in new[]{.4f,.9f,1.5f})
                {
                    var target=person.transform.position+Vector3.up*h;var eye=target+Lattice.World.SightlineAudit.ToCamera*Lattice.World.SightlineAudit.Distance;
                    if(piece.bounds.IntersectRay(new Ray(eye,target-eye),out float along)&&along<Lattice.World.SightlineAudit.Distance-Lattice.World.SightlineAudit.NearIgnore)hiding++;
                }
                float worst=0;
                if(zone=="SunkenRelay")
                {
                    var normal=walls.normals;
                    foreach(var lamp in All<Light>().Where(l=>l.name=="Lamp light"))
                    {
                        var from=lamp.transform.position;
                        for(int i=0;i<wallAt.Length;i++)
                        {
                            var to=from-wallAt[i];float d2=to.sqrMagnitude;if(d2>49)continue;
                            float window=Mathf.Clamp01(1-(d2/(lamp.range*lamp.range))*(d2/(lamp.range*lamp.range)));
                            worst=Mathf.Max(worst,.6f*lamp.intensity*Mathf.Clamp01(Mathf.Clamp01(Vector3.Dot(normal[i],to.normalized))*window*window/Mathf.Max(d2,.0001f)));
                        }
                    }
                }
                Debug.Log($"GROUND_AUDIT {zone} teeth={GroundTerrain.ToothShare(floor):0.000} (<.13) meet={meet} shaded={shaded} ({(float)shaded/Mathf.Max(1,meet):0.00}>.6) levelWindow={read} offBand={off} (0) kitHidesSomeone={hiding} (0) lampOnWall={worst:0.00} (<1) floor={floor.vertexCount}v cliffs={walls.vertexCount}v");
            }
            Debug.Log("GROUND_AUDIT_OK");
        });
        /// <summary>Flat ground paint draws before danger markings, always. RoomDressing's ground patches were made at the plain transparent queue (3000), after a marking's fill and
        /// outline (2989, 2990), so a patch could lie over a marking. RoomDressing makes them at the kit's queue now; this moves the materials already made, whichever zones are staged.</summary>
        static void FloorDetailOrder()
        {
            int moved=0,marking=GroundKit.MarkingQueue;
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project/Resources/WorldMaterials"}))
            {
                var paint=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if(paint==null||!paint.name.StartsWith("dress-detail-")||paint.renderQueue<marking)continue;
                paint.renderQueue=GroundKit.DetailQueue-1;EditorUtility.SetDirty(paint);moved++;
            }
            // Water lies under a marking too, as WaterKit makes it (Transparent-30). The older pools (BiomeLandscapeUpgrade's nursery fallback, and every local water
            // LocalBiomeBake made from the licensed pack) were at the plain transparent queue: TallowDrift's refuge cistern lay over any marking on it.
            int water=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/_Project"}))
            {
                // A local water is set outright: it keeps its licensed shader graph's own queue (-1), and the editor read that as under a marking's while the game drew it at 3000.
                string path=AssetDatabase.GUIDToAssetPath(guid);var pool=AssetDatabase.LoadAssetAtPath<Material>(path);const int Under=(int)UnityEngine.Rendering.RenderQueue.Transparent-30;
                if(pool==null||pool.renderQueue==Under)continue;
                bool local=path.Contains("/LocalStaggart/")&&pool.name.EndsWith(" water");
                if(!local&&!(pool.IsKeywordEnabled("_WATER_ON")&&pool.renderQueue>=marking))continue;
                pool.renderQueue=Under;EditorUtility.SetDirty(pool);water++;
            }
            Debug.Log($"GROUND_DETAIL_ORDER moved={moved} water={water} marking={marking}");
        }
        static Texture2D Art(string file)=>AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/"+file+".png")??throw new InvalidOperationException("Missing texture "+file);
        static float Smooth(float from,float to,float x)=>GroundKit.Smooth(from,to,x);

        // ---------------------------------------------------------------- Lantern Gorge
        /// <summary>A dead relay road up a canyon at dusk. The road itself is what's left of Ancelane paving: whole in places, gone in others, its loose slabs lying along its edges.
        /// Off it the floor breaks into rubble and scree, and pale sand has drifted against the cliff feet and banked on the verge. Dry grass keeps to the verge and the walls,
        /// small pale flowers grow only where a relay's light used to fall, and broken lantern glass lies round the fallen towers. Scavenger carts have rutted the road from their
        /// camp to the relay door; the grazers' tracks come and go from the den. Cloud shadow drifts up the canyon.</summary>
        static void Gorge()
        {
            var path=LanternRoadBuilder.GorgePath;var den=new[]{LanternRoadBuilder.DenEntry,LanternRoadBuilder.Den};
            var kit=GroundKit.Open("LanternGorge",GroundKit.FindFloor(),41);
            float Way(Vector2 p)=>GroundKit.Distance(p,path);
            var relays=GroundKit.Named("Gorge relay");var towers=GroundKit.Named("Fallen relay tower","LanternShard","Road lantern");var camp=GroundKit.Named("MusterLeanTo","MusterCrate","MusterCutterRig");
            kit.ClearLane(path,1.7f).ClearLane(den,1.3f).Look("Canyon",Art("Lantern/canyon-cliff"));
            Color stone=new(1.04f,.86f,.7f),rubble=new(.78f,.6f,.56f),drift=new(1.14f,.96f,.76f);
            kit.Layers(null,1.1f,new GroundKit.Layer("pavers",stone,5),new GroundKit.Layer("scree",rubble,3.4f),new GroundKit.Layer("sand",drift,6));
            kit.PaintPath(0,path,2.5f,1,.5f,.7f);
            foreach(var relay in relays)kit.PaintAround(0,relay,4.2f,1,relays.IndexOf(relay));
            kit.PaintAround(0,new Vector2(0,228.5f),5.5f).PaintAround(0,new Vector2(1,-4),4.5f);
            kit.PaintPatches(1,5.5f,.4f,p=>Smooth(3.2f,5,Way(p)));
            foreach(var at in camp)kit.PaintAround(1,at,3.2f,1,5);
            // The den: ground the grazers have trodden to rubble, darker than the road.
            kit.PaintAround(1,LanternRoadBuilder.Den+new Vector2(-2,1),7.5f,1,31).Shade(p=>.14f*(1-Smooth(5,9.5f,Vector2.Distance(p,LanternRoadBuilder.Den))));
            kit.PaintFoot(2,2.6f,.45f).PaintPatches(2,8,.18f,p=>Smooth(4,6.5f,Way(p)),5);
            kit.ShadeRelief(LanternRoadBuilder.GorgeRelief);

            kit.Decals(new[]{"Slabs","Flagstones"},5.5f,p=>1-Smooth(.5f,2,Mathf.Abs(Way(p)-3.2f)),1.5f,2.6f,stone);
            kit.Decals(new[]{"CracksA","CracksB"},5,p=>Smooth(2.5f,4,Way(p))*Smooth(1,2.5f,kit.Edge(p)),2.4f,4.2f,new Color(.86f,.76f,.8f,.9f));
            // Bedrock showing through the open floor, here and there.
            kit.Decals(new[]{"Flagstones"},1.6f,p=>Smooth(4,6,Way(p))*Smooth(.5f,.62f,kit.Noise(p,10,37)),1.6f,2.6f,new Color(.92f,.72f,.64f));
            kit.Decals(new[]{"PebblesA","PebblesB"},7.5f,p=>.3f+.7f*Mathf.Max(1-Smooth(.4f,2.5f,kit.Solid(p)),1-Smooth(1.5f,4.5f,kit.Edge(p))),1.1f,2.1f,new Color(1,.86f,.76f));
            kit.Decals(new[]{"SandDrift"},1.5f,p=>1-Smooth(2.5f,5.5f,kit.Edge(p)),2.8f,4.6f,new Color(drift.r,drift.g,drift.b,.9f),false);
            kit.Decals(new[]{"Lichen"},2.4f,p=>Smooth(.5f,.62f,kit.Noise(p,14,3)),1,1.9f,new Color(.76f,.84f,.66f,.85f),false);
            kit.Decals(new[]{"Stain"},.9f,p=>Smooth(3,5,Way(p)),3,5,new Color(.55f,.45f,.55f,.8f),false);
            foreach(var (x,z) in new[]{(-7.4f,109.2f),(-5.2f,121.5f),(8.6f,201)})kit.Decal("Scorch",new Vector2(x,z),2.8f,x*40,new Color(1,.94f,.94f,.9f),false);
            // Scavenger carts between the camp and the door; the grazers' own trails to and from the den and across the dark stretches.
            kit.Strip("Ruts",path,.55f,1.7f,new Color(.6f,.48f,.5f,.55f),118,222);
            var prints=new Color(.52f,.4f,.44f,.85f);
            kit.Strip("Tracks",new[]{LanternRoadBuilder.Den,LanternRoadBuilder.DenEntry,new Vector2(-2,84),new Vector2(5,70)},0,3.4f,prints);
            kit.Strip("Tracks",new[]{new Vector2(-9,146),new Vector2(-3,158),new Vector2(6,168)},0,3.4f,prints);
            kit.Strip("Tracks",new[]{new Vector2(-8,14),new Vector2(-2,24),new Vector2(7,34)},0,3.4f,prints);
            // The road was surveyed once: a worn line runs out from each relay toward the middle of the way.
            foreach(var relay in relays){var to=relay+new Vector2(-Mathf.Sign(relay.x)*9,2.5f);kit.Strip("SurveyLine",new[]{relay+(to-relay).normalized*2.4f,to},0,2.4f,new Color(.74f,.92f,1,.6f));}
            kit.LampPools(2.6f,new Color(1,.8f,.55f,.42f));

            Color grass=new(1,.86f,.6f),dry=new(.92f,.76f,.66f),rock=new(1,.82f,.72f);
            float Verge(Vector2 p)=>Smooth(3.4f,5.5f,Way(p));
            kit.Cover(new[]{"TuftA","TuftC","Sprouts"},60,p=>Verge(p)*Smooth(.5f,.62f,kit.Noise(p,9,7)),.2f,.4f,grass,4);
            kit.Cover(new[]{"TuftB"},14,p=>(1-Smooth(1.5f,4,kit.Edge(p)))*Smooth(.45f,.6f,kit.Noise(p,7,9)),.3f,.52f,grass,2);
            kit.Cover(new[]{"Twigs","Succulent"},3,Verge,.24f,.44f,dry);
            kit.Cover(new[]{"Flowers"},20,p=>GroundKit.Nearest(p,relays)<6.5f?Smooth(.4f,.55f,kit.Noise(p,4,11)):0,.2f,.34f,new Color(1,.96f,.92f),2);
            kit.Cover(new[]{"Shards"},10,p=>GroundKit.Nearest(p,towers)<6?.8f:0,.1f,.24f,new Color(.6f,.78f,.98f));
            kit.Rocks(9,p=>Verge(p)*(.2f+.8f*Mathf.Max(1-Smooth(1.5f,4.5f,kit.Edge(p)),Smooth(.55f,.7f,kit.Noise(p,11,13)))),.07f,.3f,rock);
            // What the grazers dragged home.
            foreach(var (x,z,ribs) in new[]{(-29.6f,97.4f,true),(-24.2f,102.6f,false),(-31.2f,99.6f,false),(-25.4f,96.2f,true),(-21.6f,101.4f,false),(-9.4f,89.2f,false)})
                kit.Stand(ribs?"BonesA":"BonesB",new Vector2(x,z),ribs?.34f:.16f,new Color(.96f,.92f,.84f),x*z%2>1);
            kit.MovingShade("cloud",85,new Vector2(.9f,.4f),.55f,.5f,.025f,default,new Vector2(18,36));
            kit.Finish();
        }

        // ---------------------------------------------------------------- Lanternwick
        /// <summary>A canyon town people live in. Its one street and its three open places are laid in small setts, with a drain's dark line along each side of the street; the keepers'
        /// terrace, the dock and every doorstep and stall front are big flags; the yards between are the canyon's own floor, cracked, with weeds and tufts along the walls' feet.
        /// Leaves lie under the orchard's trees, water stands by the barrels, cart ruts run up the two side lanes, and every lamp lays a pool.</summary>
        static void Lanternwick()
        {
            var kit=GroundKit.Open("Lanternwick",GroundKit.FindFloor(),29);
            Vector2[] street={new(0,-22),new(0,96)},eastLane={new(5.5f,26),new(15,26.5f),new(21.5f,27.5f)},westLane={new(-5.5f,48),new(-20,47.5f)},terrace={new(21.5f,27.5f),new(22.5f,50),new(23,92)},orchard={new(-20,47.5f),new(-30,60),new(-33,82)};
            Vector2 square=new(0,-12),market=new(0,18),forecourt=new(1,91);
            float Laid(Vector2 p)=>Mathf.Max(p.y<-22||p.y>96?0:1-Smooth(4.9f,6.1f,Mathf.Abs(p.x)),Mathf.Max(1-Smooth(9.2f,11.4f,Vector2.Distance(p,square)),Mathf.Max(1-Smooth(9,11,Vector2.Distance(p,market)),1-Smooth(8,10,Vector2.Distance(p,forecourt)))));
            var stalls=GroundKit.Named("DressMarketStall","DressKiosk");var homes=GroundKit.Named("CanyonHouse","OutpostHab");var trees=GroundKit.Named("OrchardTree");var wet=GroundKit.Named("DressWaterBarrels","DressPlanter");
            kit.ClearLane(street,4.4f).ClearLane(eastLane,1.6f).ClearLane(westLane,1.6f).ClearLane(terrace,1.6f).ClearLane(orchard,1.5f).Look("Canyon",Art("Lantern/canyon-cliff"));
            Color setts=new(1.1f,.96f,.84f),flags=new(1.08f,.9f,.72f),leaves=new(.98f,.94f,.7f,.85f);
            kit.Layers(new Vector4(.04f,.26f,.4f,.34f),1,new GroundKit.Layer("cobble",setts,4),new GroundKit.Layer("pavers",flags,5),new GroundKit.Layer("litter",leaves,3.2f));
            kit.Paint(0,Laid);
            // The keepers' terrace and the walk up to it, the dock's apron, and a threshold at every door and stall.
            kit.PaintByHeight(1,2.5f,3.58f,.25f).PaintPath(1,eastLane,2,1,.25f,.6f).PaintAround(1,new Vector2(0,-22),6.5f);
            foreach(var stall in stalls)kit.PaintAround(1,stall+new Vector2(-Mathf.Sign(stall.x)*2.2f,0),2.3f,1,3);
            foreach(var home in homes)if(Mathf.Abs(home.x)<28)kit.PaintAround(1,home+new Vector2(-Mathf.Sign(home.x)*3.4f,0),2.1f,1,7);
            foreach(var tree in trees)kit.PaintAround(2,tree,2.9f,1,9);
            // The orchard's terrace is leaf and rough grass in patches, not bare floor.
            kit.PaintPatches(2,5,.36f,p=>p.x<-19.5f&&p.y>42&&p.y<92?1:0,13);
            // The street's drains: a dark line a hand outside the setts, where the street runs between homes.
            kit.Shade(p=>p.y<-1||p.y>82||Mathf.Abs(p.y-18)<11.5f?0:.2f*(1-Smooth(.18f,.42f,Mathf.Abs(Mathf.Abs(p.x)-5.95f))));
            // Trodden pale in front of every stall, and along the walk through the orchard.
            kit.Shade(p=>-.1f*(1-Smooth(1.2f,2.6f,GroundKit.Nearest(p,stalls)))).Shade(p=>-.13f*(1-Smooth(.7f,1.6f,GroundKit.Distance(p,orchard)+(kit.Noise(p,3,43)-.5f)*.8f)));

            float Bare(Vector2 p)=>1-Smooth(.2f,.6f,Laid(p));
            kit.Decals(new[]{"Slabs","Flagstones"},5,p=>Bare(p)*(1-Smooth(.6f,2.4f,Mathf.Abs(Mathf.Abs(p.x)-7.2f))),1.4f,2.4f,flags);
            kit.Decals(new[]{"CracksA","CracksB"},4.2f,p=>Bare(p)*Smooth(1,2.5f,kit.Edge(p)),2.2f,4,new Color(.86f,.78f,.8f,.9f));
            // Stepping stones up the orchard's walk.
            kit.Decals(new[]{"Flagstones","Slabs"},24,p=>1-Smooth(.3f,1.2f,GroundKit.Distance(p,orchard)),1.2f,1.9f,flags,true,.08f,5);
            kit.Decals(new[]{"PebblesA","PebblesB"},6,p=>Bare(p)*(.25f+.75f*Mathf.Max(1-Smooth(.3f,2,kit.Solid(p)),1-Smooth(1.5f,4,kit.Edge(p)))),1,1.9f,new Color(1,.88f,.78f));
            kit.Decals(new[]{"SandDrift"},1.3f,p=>Bare(p)*(1-Smooth(2,5,kit.Edge(p))),2.6f,4.2f,new Color(1.14f,.97f,.78f,.9f),false);
            kit.Decals(new[]{"Lichen"},2,p=>Smooth(.52f,.64f,kit.Noise(p,13,3))*(1-Smooth(1,4,kit.Solid(p))),.9f,1.7f,new Color(.78f,.86f,.68f,.85f),false);
            kit.Decals(new[]{"Stain"},7,p=>GroundKit.Nearest(p,wet)<3.2f||GroundKit.Nearest(p,stalls)<3.4f?.7f:0,1.6f,3,new Color(.56f,.46f,.5f,.75f),false);
            kit.Decals(new[]{"Litter"},16,p=>GroundKit.Nearest(p,trees)<5.5f?1:GroundKit.Nearest(p,stalls)<4?.35f:0,1.1f,2,leaves,false);
            foreach(var (x,z,rx,rz) in new[]{(13.6f,8.2f,.9f,.6f),(-13.4f,10.6f,.7f,.5f),(-7.2f,29.4f,.8f,.5f),(9.4f,37.6f,.6f,.9f),(-8.6f,71.5f,.9f,.6f),(8.8f,60.2f,.7f,.5f),(-24.6f,57.2f,.8f,.6f),(16.4f,14.2f,.6f,.5f)})
                kit.Puddle(new Vector2(x,z),rx,rz,x*z);
            kit.Strip("Ruts",eastLane,0,1.7f,new Color(.62f,.5f,.5f,.75f));kit.Strip("Ruts",westLane,0,1.7f,new Color(.62f,.5f,.5f,.75f));
            // The courier's line from the pad to the arch.
            kit.Strip("SurveyLine",new[]{new Vector2(0,-18.6f),new Vector2(0,-10.4f)},0,2.4f,new Color(1,.82f,.52f,.62f));
            kit.LampPools(2.5f,new Color(1,.8f,.55f,.36f));

            Color grass=new(1,.9f,.62f),rock=new(1,.84f,.74f);
            float Foot(Vector2 p)=>Bare(p)*Mathf.Max(1-Smooth(.3f,1.7f,kit.Solid(p)),1-Smooth(1,3.2f,kit.Edge(p)));
            kit.Cover(new[]{"TuftA","TuftC","Sprouts"},70,p=>Foot(p)*Smooth(.42f,.56f,kit.Noise(p,6,7)),.18f,.36f,grass,3);
            // The orchard's terrace is rough grass between its trees.
            kit.Cover(new[]{"TuftA","TuftC"},46,p=>p.x<-20&&p.y>42&&p.y<90?Smooth(.48f,.6f,kit.Noise(p,7,41)):0,.18f,.36f,grass,4,.12f,7);
            kit.Cover(new[]{"Sprouts","TuftC"},26,p=>Bare(p)*(1-Smooth(.3f,1.2f,Mathf.Abs(Mathf.Abs(p.x)-6.9f)))*Smooth(.45f,.6f,kit.Noise(p,5,17)),.08f,.18f,grass,2,.12f,3);
            kit.Cover(new[]{"Flowers"},40,p=>Bare(p)*(GroundKit.Nearest(p,homes)<5.2f||GroundKit.Nearest(p,trees)<4.5f?Smooth(.48f,.6f,kit.Noise(p,3.5f,11)):0),.2f,.34f,new Color(1,.97f,.9f),2);
            kit.Cover(new[]{"Fern"},9,p=>Bare(p)*(1-Smooth(.8f,2.6f,kit.Edge(p)))*Smooth(.5f,.62f,kit.Noise(p,8,19)),.2f,.36f,new Color(.82f,.92f,.66f));
            kit.Cover(new[]{"TuftB"},34,p=>GroundKit.Nearest(p,trees)<7?Smooth(.5f,.62f,kit.Noise(p,5,23)):0,.28f,.5f,grass,2);
            kit.Rocks(5,p=>Bare(p)*(.15f+.85f*(1-Smooth(1,3.5f,kit.Edge(p)))),.06f,.24f,rock);
            // Rubble where the terrace's face runs out into the ramp, so the ledge ends in something.
            foreach(var (x,z,h) in new[]{(16.9f,27.2f,.3f),(17.5f,26.4f,.2f),(16.3f,26.6f,.14f),(17.2f,28.3f,.18f),(16.6f,25.6f,.1f)})kit.Rock(new Vector2(x,z),h,h*2.2f,x*90,rock,(int)(x*10));
            kit.MovingShade("cloud",70,new Vector2(.8f,.3f),.45f,.5f,.025f,default,new Vector2(8,20));
            kit.Finish();
        }

        // ---------------------------------------------------------------- Sorrel Ridges
        /// <summary>A mined moon's basin. The three roads are graded aggregate, paler and greyer than the ochre they cross; rock breaks through the flats in rust patches and pale dust
        /// has settled in the lee of every slope. Around the pads the ground is scorched by landings, survey lines run out from the ore, and the plain between is cracked hardpan
        /// with loose stone. High dust shadow drifts across.</summary>
        static void Sorrel()
        {
            var basin=GameObject.Find("Continuous eroded basin")??throw new InvalidOperationException("GROUND_PROBLEM Sorrel_Ridges: no basin");
            var kit=GroundKit.Open("Sorrel_Ridges",basin.GetComponent<MeshFilter>(),17,3.3f,true);
            var roads=new[]{SorrelRedesign.Haul,SorrelRedesign.Seam,SorrelRedesign.Service};
            float Road(Vector2 p)=>Mathf.Min(GroundKit.Distance(p,roads[0]),Mathf.Min(GroundKit.Distance(p,roads[1]),GroundKit.Distance(p,roads[2])));
            var pads=GroundKit.Named("Launch");var ore=GroundKit.Named("CrystalCluster");var machines=GroundKit.Named("DeckCrate","HushwellCrates","HushwellDrillBit","HushwellOreCart","DressCableDrum","DressPipeRun","OutpostDrill");
            foreach(var road in roads)kit.ClearLane(road,2.4f);
            kit.Look("Sorrel",Art("Textures/sorrel-rock"));
            Color aggregate=new(.86f,.74f,.68f),rust=new(.8f,.64f,.56f),dust=new(1.08f,.95f,.78f);
            kit.Layers(null,1.3f,new GroundKit.Layer("gravel",aggregate,3.2f),new GroundKit.Layer("scree",rust,3.6f),new GroundKit.Layer("sand",dust,6.5f));
            foreach(var road in roads)kit.PaintPath(0,road,2.5f,1,.3f,.7f,Array.IndexOf(roads,road));
            // The outpost's yard is graded like the roads that leave it.
            kit.Paint(0,p=>{var q=new Vector2(p.x/26,(p.y-6)/24);return (1-Smooth(.75f,1,q.magnitude+(kit.Noise(p,6,51)-.5f)*.4f))*(1-.4f*Smooth(.45f,.62f,kit.Noise(p,5,53)));});
            kit.PaintPatches(1,6.5f,.44f,p=>Smooth(3.4f,5.4f,Road(p)));
            kit.PaintFoot(2,3,.4f).PaintPatches(2,9,.2f,p=>Smooth(4,6.5f,Road(p)),5);
            // The seam road runs down a dry wash: pale dust lies banked along both its verges, in runs.
            kit.Paint(2,p=>{float d=GroundKit.Distance(p,SorrelRedesign.Seam);return d>9?0:(1-Smooth(1.1f,2.4f,Mathf.Abs(d-5)+(kit.Noise(p,3,71)-.5f)*1.6f))*(1-.6f*Smooth(.46f,.62f,kit.Noise(p,7,73)));});

            kit.Decals(new[]{"CracksA","CracksB"},5,p=>Smooth(3,4.5f,Road(p))*Smooth(1.5f,3,kit.Edge(p)),2.6f,4.6f,new Color(.8f,.66f,.62f,.9f));
            kit.Decals(new[]{"PebblesA","PebblesB"},6,p=>.3f+.7f*Mathf.Max(1-Smooth(.4f,2.5f,kit.Solid(p)),1-Smooth(1.5f,5,kit.Edge(p))),1.1f,2.2f,new Color(1,.82f,.68f));
            kit.Decals(new[]{"Flagstones","Slabs"},1.6f,p=>Smooth(4,6,Road(p))*Smooth(.56f,.68f,kit.Noise(p,12,31)),1.6f,2.8f,new Color(.96f,.72f,.56f));
            kit.Decals(new[]{"SandDrift"},1.2f,p=>1-Smooth(2.5f,6,kit.Edge(p)),3,5,new Color(dust.r,dust.g,dust.b,.9f),false);
            kit.Decals(new[]{"Stain"},6,p=>GroundKit.Nearest(p,machines)<3.4f?.7f:0,1.6f,3.2f,new Color(.42f,.36f,.36f,.8f),false);
            // Landings scorch the ground round a pad.
            foreach(var pad in pads)foreach(var (dx,dz) in new[]{(4.6f,1.2f),(-4.4f,-1.6f),(1.4f,4.6f),(-1.8f,-4.8f)})kit.Decal("Scorch",pad+new Vector2(dx,dz),4.2f,dx*50,new Color(1,.95f,.92f,.85f),false);
            // Survey lines run out from each ore body toward the nearest road.
            foreach(var node in ore)
            {
                Vector2 nearest=node;float best=float.PositiveInfinity;
                foreach(var road in roads)for(int i=1;i<road.Length;i++){var d=road[i]-road[i-1];var q=road[i-1]+d*Mathf.Clamp01(Vector2.Dot(node-road[i-1],d)/d.sqrMagnitude);if((q-node).sqrMagnitude<best){best=(q-node).sqrMagnitude;nearest=q;}}
                if(best>4)kit.Strip("SurveyLine",new[]{Vector2.MoveTowards(node,nearest,2.6f),Vector2.MoveTowards(nearest,node,2.2f)},0,2.4f,new Color(.7f,.95f,1,.6f));
            }

            Color scrub=new(.96f,.84f,.56f),rock=new(1,.78f,.62f);
            float Open(Vector2 p)=>Smooth(3,5,Road(p));
            kit.Rocks(8,p=>Open(p)*(.2f+.8f*Mathf.Max(1-Smooth(1.5f,5,kit.Edge(p)),Smooth(.55f,.7f,kit.Noise(p,13,13)))),.07f,.32f,rock);
            kit.Cover(new[]{"Twigs","Succulent"},2.2f,Open,.2f,.42f,new Color(.9f,.78f,.62f));
            kit.Cover(new[]{"TuftC","Sprouts"},22,p=>Open(p)*(1-Smooth(1.5f,4.5f,kit.Edge(p)))*Smooth(.46f,.6f,kit.Noise(p,8,7)),.12f,.26f,scrub,3);
            // Scrub stands thicker along the seam's wash than anywhere else in the basin.
            kit.Cover(new[]{"TuftA","TuftB"},26,p=>{float d=GroundKit.Distance(p,SorrelRedesign.Seam);return d<3.4f||d>10?0:Smooth(.44f,.58f,kit.Noise(p,6,77));},.26f,.48f,scrub,4,.12f,9);
            kit.MovingShade("cloud",110,new Vector2(1.3f,.5f),.42f,.5f,.03f,default,new Vector2(30,10));
            kit.Finish();
        }

        // ---------------------------------------------------------------- Hushwell
        /// <summary>A mined cave under a moon, three levels down to a nursery, drawn in lit chunks that the layers are painted onto. One stone, worn three ways. Where feet and
        /// carriages have gone the dark crust is worn through to the bedrock under it: the same brown a value lighter and a little greyer, split by its own fractures, never a
        /// laid path, its edge following the stone's cracks and broken into grit. (Until November 12 the worn layer was the kit's sand, three times the floor's brightness,
        /// rippled like a dune and outlined in ink: pale paper shapes laid on the floor.) Mineral seams show through in places, and what's come off the walls lies along their
        /// feet. Each room keeps a floor of its own: the breach is machine-broken spoil and loose slab; gallery A is the miners' worn floor, rutted and drifted with dust;
        /// gallery B is the seam gallery, veined from wall to wall; the ramps are scree spilling down their sides, and the lower ramp's walls glow faintly from colonies at
        /// their feet; chamber 1 is where the cave starts to glow, fungus and specks of light; chamber 2 is rockfall, fans of rubble out from the walls; chamber 3 is seamed and
        /// worn smooth at the Bellows' door. The Bellows' own floor takes paint only: broad, calm patches of the same worn bedrock, no seams, no grit fringes, and nothing
        /// lying or standing anywhere in it, so a marking is always the clearest thing there (D119: nothing here stands over .3 m or is solid).</summary>
        static void Hushwell()
        {
            var cave=GameObject.Find("Cave rock and floor")??throw new InvalidOperationException("GROUND_PROBLEM Hushwell: no cave");
            var kit=GroundKit.Open("Hushwell",cave.GetComponent<MeshFilter>(),23,float.PositiveInfinity,true,
                p=>Lattice.World.HushwellLayout.Clear(new Vector2(p.x,p.z))<-.15f&&p.y<=Lattice.World.HushwellLayout.Level(p.z)+.3f);
            var main=Lattice.World.HushwellLayout.Main;var east=Lattice.World.HushwellLayout.EastPassage;var west=Lattice.World.HushwellLayout.WestPassage;
            var bellows=Lattice.World.HushwellLayout.Bellows;var pools=BiomeLandscapeUpgrade.NurseryPools;
            kit.ClearLane(main,1.5f).ClearLane(east,1.3f).ClearLane(west,1.3f).Look("Nursery",Art("Textures/sorrel-rock"));
            bool Dry(Vector2 p){foreach(var pool in pools)if(new Vector2((p.x-pool.x)/(pool.z+1.2f),(p.y-pool.y)/(pool.w+1.2f)).magnitude<1)return false;return true;}
            bool Fight(Vector2 p)=>Vector2.Distance(p,bellows)<21.5f;
            float Way(Vector2 p)=>Lattice.World.HushwellLayout.Distance(p,main);
            float Foot(Vector2 p)=>1-Smooth(.7f,2.8f,kit.Edge(p));
            // 1 anywhere but the Bellows' floor, where nothing lies or stands at all (a marking can reach the walls); nothing in the nursery's water.
            float Quiet(Vector2 p)=>!Dry(p)||Fight(p)?0:1;
            float Room(Vector2 p,float x,float z,float rx,float rz)=>1-Smooth(.8f,1.08f,new Vector2((p.x-x)/rx,(p.y-z)/rz).magnitude);
            float Breach(Vector2 p)=>Room(p,0,6,15,11);
            float GalleryA(Vector2 p)=>Room(p,-12,48,12,13);
            float GalleryB(Vector2 p)=>Room(p,8,84,12,12);
            float Chamber1(Vector2 p)=>Room(p,0,164,14,12);
            float Chamber2(Vector2 p)=>Room(p,-4,196,13,11);
            float Chamber3(Vector2 p)=>Room(p,1,226,14,10);
            float Ramp(Vector2 p)=>Mathf.Max(Smooth(116,122,p.y)*(1-Smooth(148,154,p.y)),Smooth(284,290,p.y)*(1-Smooth(316,322,p.y)));
            float Deep(Vector2 p)=>Smooth(118,150,p.y);
            float Pockets(Vector2 p)=>Mathf.Max(Room(p,41,164,7,7),Room(p,-37,212,7,7));
            var crystals=GroundKit.Named("CrystalCluster","HushwellCrystal");

            // ---- the stone, worn three ways
            var plain=AssetDatabase.LoadAssetAtPath<Material>(GroundKit.WorldMaterials+"hushwell-floor.mat")??throw new InvalidOperationException("GROUND_PROBLEM Hushwell: no floor material");
            kit.DrawnAs(UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Where(f=>f.name.StartsWith("Lit chunk")),plain);
            // Tints set by measure (linear, against the floor's own albedo of about .088,.029,.008), three tones a dim cave can tell apart: the red-brown dust; rubble about twice
            // as bright and still warm, dark in its own gaps; and the bedrock worn bare under the dust, about 2.5 times and grey, its cracks darker than the dust. The sand this
            // replaces was 3.2 times, rippled and inked. Brightness alone can't part them in this light: at 1.4 times the cave went one murky tone, with bedrock and scree both
            // twice and warm the ramps' rubble and worn way merged, and with the scree darker it merged with the dust. Hardly any ink: a line drawn round a patch is what made
            // it read as laid on top.
            kit.Layers(new Vector4(.05f,.44f,.45f,.03f),.9f,new GroundKit.Layer("bedrock",new Color(.58f,.56f,.6f,.92f),5.5f),new GroundKit.Layer("veins",new Color(.6f,.72f,.8f,.3f),7),new GroundKit.Layer("scree",new Color(.5f,.41f,.38f),3.2f));
            // Worn through where feet go, in broken runs; broadly across the miners' gallery and before the Bellows' door; on the Bellows' floor in small patches interleaved
            // with the dirt, so neither lies as one bare stretch, kept off the walls' feet, where the rubble is.
            kit.PaintPath(0,main,1.7f,1,.6f,.9f).PaintPath(0,east,1.4f,1,.55f,.7f,3).PaintPath(0,west,1.4f,1,.55f,.7f,5);
            // Down a ramp the way is worn whole, a broad cleared run through the scree from top to foot; and the side pockets are worn bare where the miners stood at the cache.
            kit.Paint(0,p=>Ramp(p)*(1-Smooth(2.5f,3.5f,Way(p)+(kit.Noise(p,2,37)-.5f)*1.2f)));
            kit.PaintAround(0,new Vector2(41,164),4.2f,1,41).PaintAround(0,new Vector2(-37,212),4.2f,1,43);
            kit.PaintPatches(0,6,.5f,GalleryA,9).PaintPatches(0,5,.4f,Chamber3,11);
            kit.PaintPatches(0,3.6f,.46f,p=>Fight(p)?Smooth(1.5f,3,kit.Edge(p)):0,13);
            // Seams: the seam gallery from wall to wall, chamber 1 (where the glow starts) and chamber 3 broadly, the side pockets, the lower ramp; never the breach, the miners'
            // gallery or the Bellows.
            kit.PaintPatches(1,6,.52f,GalleryB,21).PaintPatches(1,6,.46f,Chamber3,23).PaintPatches(1,5,.56f,Chamber1,22).PaintPatches(1,5,.44f,Pockets,24);
            kit.PaintPatches(1,7,.36f,p=>Dry(p)?Smooth(284,292,p.y):0,25);
            kit.PaintPatches(1,8,.12f,p=>Fight(p)||!Dry(p)?0:1-Mathf.Max(Breach(p),GalleryA(p)),27);
            // Grit and rubble: at every wall's foot; fringing every worn patch outside the Bellows, so its edge is broken stone and not a line; spoil across the breach; rockfall
            // fanning well out into chamber 2; scree spilling down both ramps from their walls most of the way to the middle.
            kit.PaintFoot(2,2.2f,.35f);
            kit.Fringe(2,0,.26f,.4f,p=>Fight(p)?0:1,61);
            kit.PaintPatches(2,5,.5f,Breach,31);
            kit.Paint(2,p=>Chamber2(p)*(1-Smooth(3,8,kit.Edge(p)+(kit.Noise(p,4,33)-.5f)*3.5f)));
            kit.Paint(2,p=>Ramp(p)*(1-Smooth(2.4f,5,kit.Edge(p)+(kit.Noise(p,3,35)-.5f)*2.6f)));
            // The scree lies over everything else, so down a ramp it buried the worn way: the way's middle is kept clear of it.
            kit.Erase(2,p=>Ramp(p)*(1-Smooth(1.7f,2.7f,Way(p)+(kit.Noise(p,2.5f,39)-.5f)*.8f)));
            // And it lies in heaps with dust between, not one band: in the ramps' light an unbroken band read as one tone with the wall above it.
            kit.Erase(2,p=>Ramp(p)*Smooth(.5f,.58f,kit.Noise(p,2.4f,45)));
            // Damp below the first ramp: hollows where seepage lies, darker than the dirt and never paler, so the deeper rooms aren't one dry tone; none in the Bellows, the pools
            // or on a ramp, where water doesn't lie.
            kit.Shade(p=>!Dry(p)||Fight(p)?0:.38f*Deep(p)*(1-Ramp(p))*Smooth(.56f,.64f,kit.Noise(p,4.5f,81)));
            // Everything that passes stirs the ramps' dust, and it lies a step paler there; in the ramps' light the floor was otherwise the wall's own tone.
            kit.Shade(p=>-.35f*Ramp(p));

            // ---- what lies on it (nothing on the Bellows' marked ground)
            kit.Decals(new[]{"CracksA","CracksB"},7,p=>Quiet(p)*(Fight(p)?0:1)*Smooth(.5f,1.5f,kit.Edge(p)),2.2f,4.4f,new Color(.5f,.54f,.7f,.9f));
            kit.Decals(new[]{"Lichen"},7,p=>Quiet(p)*(.3f+.7f*Foot(p)),1,2.1f,new Color(.66f,.9f,.94f,.85f),false);
            kit.Decals(new[]{"Stain"},4.5f,p=>Quiet(p)*(Fight(p)?0:1)*(.35f+.65f*Deep(p)),2.6f,5,new Color(.3f,.36f,.5f,.8f),false);
            kit.Decals(new[]{"PebblesA","PebblesB"},12,p=>Quiet(p)*(.15f+.85f*Mathf.Max(Foot(p),Chamber2(p)*.7f)),1,2.1f,new Color(.78f,.74f,.8f));
            // The breach: loose slab the drill broke out, and the grooves its carriage cut on the way in.
            kit.Decals(new[]{"Flagstones","Slabs"},9,p=>Quiet(p)*Breach(p),1.4f,2.6f,new Color(.7f,.62f,.6f),true,.08f,3);
            kit.Decals(new[]{"Flagstones","Slabs"},2.2f,p=>Quiet(p)*(1-Smooth(108,124,p.y))*(1-Breach(p))*Smooth(.52f,.64f,kit.Noise(p,9,5)),1.5f,2.6f,new Color(.7f,.66f,.7f));
            // The ramps: slabs broken off the worn way lie in the rubble beside it.
            kit.Decals(new[]{"Slabs","Flagstones"},16,p=>Quiet(p)*Ramp(p)*Smooth(2,2.8f,Way(p))*Smooth(.2f,.7f,kit.Edge(p)),1.1f,2,new Color(.62f,.6f,.66f),true,.08f,7);
            kit.Strip("Ruts",main,1.9f,1.6f,new Color(.4f,.38f,.46f,.6f),4,112);kit.Strip("Ruts",main,-1.6f,1.6f,new Color(.4f,.38f,.46f,.5f),2,30);
            // Gallery A: dust drifted against the walls, and the surveyors' line down the miners' way.
            kit.Decals(new[]{"SandDrift"},5,p=>Quiet(p)*GalleryA(p)*Foot(p),2.4f,4,new Color(.74f,.62f,.56f,.85f),false);
            kit.Strip("SurveyLine",main,-2.6f,2.2f,new Color(1,.8f,.46f,.6f),30,118);
            kit.Rocks(14,p=>Quiet(p)*Mathf.Max(Foot(p),Chamber2(p)*(1-Smooth(2,6,kit.Edge(p)))),.07f,.28f,new Color(.74f,.7f,.76f));
            // What grows here gives its own light: chamber 1 is where it starts, thick from wall to wall; deeper it keeps to the walls, and it gathers round the crystal seams.
            float Grows(Vector2 p)=>Quiet(p)*(Fight(p)?0:1)*Mathf.Max(Chamber1(p)*.9f,Mathf.Max(Foot(p)*(.2f+.8f*Deep(p)),GroundKit.Nearest(p,crystals)<5?.8f:0));
            kit.Glows(34,p=>Grows(p)*Smooth(.42f,.58f,kit.Noise(p,5,7)),.3f,.9f,new Color(.3f,.95f,1,.7f));
            kit.Cover(new[]{"FungusA","FungusB"},26,p=>Grows(p)*Smooth(.45f,.6f,kit.Noise(p,5,7)),.16f,.3f,new Color(.74f,1,.98f),3);
            kit.Cover(new[]{"Shards"},9,p=>Quiet(p)*(GroundKit.Nearest(p,crystals)<4.5f?.8f:0),.1f,.24f,new Color(.55f,.9f,1));
            // The lower ramp is the last way down to the nursery: colonies of glowing growth at its walls' feet, alternating sides, the nearer the nursery the more of them.
            // (The black on either side of that frame is the rock's cut top, hushwell-cut, which HushwellBuilder draws near black by design; no light or paint of the floor's
            // changes it, and lights tried there on November 12 lit nothing.)
            var colonies=new List<Vector2>();
            foreach(var (z,side) in new[]{(293f,1),(299f,-1),(305f,1),(311f,-1),(317f,1)})
            {
                int i=1;while(i<main.Length-1&&main[i].y<z)i++;
                Vector2 a=main[i-1],b=main[i],along=(b-a).normalized,mid=Vector2.Lerp(a,b,Mathf.InverseLerp(a.y,b.y,z)),out_=new Vector2(along.y,-along.x)*side;
                for(float s=2;s<10;s+=.2f){var p=mid+out_*s;if(kit.Edge(p)>.9f)continue;colonies.Add(p);break;}
            }
            float Colony(Vector2 p)=>Quiet(p)*(GroundKit.Nearest(p,colonies)<1.7f?1:0);
            kit.Glows(70,Colony,.25f,.7f,new Color(.3f,.95f,1,.75f),5);
            kit.Cover(new[]{"FungusA","FungusB"},45,Colony,.14f,.28f,new Color(.74f,1,.98f),3,.12f,5);
            if(colonies.Count<4)throw new InvalidOperationException("GROUND_PROBLEM Hushwell: the lower ramp's walls took "+colonies.Count+" of their five colonies");
            kit.LampPools(2.4f,new Color(1,.8f,.55f,.4f));
            kit.Finish();
        }
    }
}
