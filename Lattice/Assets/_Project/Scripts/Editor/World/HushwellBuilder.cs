using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Hushwell, the moon cave above the Choir nursery (docs/maps/Hushwell.md).
    /// Isolated build: its own zone, boss and scene definitions, one scene and
    /// its navigation. No other map, definition regeneration, save migration or
    /// paid intake. The plan lives in <see cref="HushwellLayout"/>.
    /// </summary>
    public static class HushwellBuilder
    {
        static bool blockout;
        const string Materials="Assets/_Project/Resources/WorldMaterials/";
        // Ground camera: pitch 40, yaw 20. Rock between the camera and a floor
        // stays under the sightline; far walls stand tall.
        static readonly Vector2 CameraForward=new(Mathf.Sin(20*Mathf.Deg2Rad),Mathf.Cos(20*Mathf.Deg2Rad));
        static readonly string[] Kit={"HushwellWall","HushwellColumn","HushwellSlope","HushwellMarker","HushwellVent","HushwellEggCradle","HushwellScaffold","HushwellOreCart","HushwellLift","HushwellStalagmites","HushwellRubble","HushwellCrystalBed","HushwellLowRidge","HushwellFallenLamp","HushwellDrillBit","HushwellChoirGrowth","HushwellShells","HushwellCrates","DeckCrate","ChoirPod"};

        public static void Blockout()=>BatchTools.Run(()=>{Build(true);AssetDatabase.SaveAssets();Debug.Log("HUSHWELL_BLOCKOUT_OK");});
        public static void Final()=>BatchTools.Run(()=>{Build(false);AssetDatabase.SaveAssets();Debug.Log("HUSHWELL_FINAL_OK");});

        static void Build(bool grey)
        {
            blockout=grey;
            if(!grey)foreach(string id in Kit)
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+id+".prefab")==null)throw new InvalidOperationException("Missing generated Hushwell piece "+id);
            var zone=Definitions();
            var scene=WorldBuilder.Begin("Hushwell");
            UnityEngine.Object.FindFirstObjectByType<ZoneController>().definition=zone;
            // A cave: a dim key through the cut-away roof, cool fill, and the
            // mineral light carrying the colour.
            var key=GameObject.Find("KeyLight").GetComponent<Light>();key.intensity=.5f;key.color=new Color(.8f,.86f,1);
            RenderSettings.ambientLight=new Color(.12f,.13f,.2f);

            Terrain();
            WorldBuilder.Spawn("Arrival",HushwellLayout.OnFloor(HushwellLayout.Arrival));
            new GameObject("Side quest",typeof(QuestStarter)).GetComponent<QuestStarter>().questId="Hushwell";
            Breach();Galleries();PressureGallery();BellowsChamber();Nursery();Dress();
            Walls();
            WorldBuilder.Boundary("West cave limit",new Vector3(-66,0,170),new Vector3(2,60,400));
            WorldBuilder.Boundary("East cave limit",new Vector3(66,0,170),new Vector3(2,60,400));
            WorldBuilder.Boundary("South cave limit",new Vector3(0,0,-26),new Vector3(134,60,2));
            WorldBuilder.Boundary("North cave limit",new Vector3(0,0,366),new Vector3(134,60,2));
            BakeNavigation();
            WorldBuilder.Save(scene,"Hushwell");
            BatchTools.RegisterScenes();
        }

        static ZoneDef Definitions()
        {
            var cave=ArenaBuilder.Asset<Hd2dProfile>("Cave",p=>{p.tint=new Color(.9f,.95f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.6f;});
            var ground=GameCatalog.Find<CameraProfile>("Ground")??throw new InvalidOperationException("Ground camera profile missing");
            var zone=ArenaBuilder.Asset<ZoneDef>("Hushwell",z=>{z.id=z.scene="Hushwell";z.kind=ZoneKind.GroundCombat;z.cameraProfile=ground;z.hd2dProfile=cave;z.musicId="Moon Caverns";z.skybox=null;});
            // B02 pattern prototype on placeholder geometry: inhale, ring, exhale.
            var spit=ArenaBuilder.Asset<AttackDef>("BellowsSpit",a=>{a.damage=16;a.range=14;a.telegraph=.9f;a.cooldown=2.2f;a.projectile=true;a.type=DamageType.Kinetic;});
            var loot=ArenaBuilder.Asset<LootTable>("BellowsBelow",l=>l.entries=new[]{new LootEntry{id="HuskCore",count=2,chance=1},new LootEntry{id="RidgeCrystal",count=3,chance=1}});
            ArenaBuilder.Asset<EnemyDef>("BellowsBelow",e=>{e.id="BellowsBelow";e.archetype=EnemyArchetype.Spitter;e.weakness=DamageType.Pulse;e.resistance=DamageType.Kinetic;e.boss=true;
                e.integrity=12000;e.breakThreshold=600;e.xp=260;e.speed=2.2f;e.attacks=new[]{spit};e.lootTable=loot;});
            // Side quest: the cave starts it on entry; the Bellows and the nursery finish it.
            ArenaBuilder.Asset<QuestDef>("Hushwell",q=>{q.id="Hushwell";q.title="What the drill found";
                q.steps=new[]{new Objective{kind=ObjectiveKind.Kill,target="BellowsBelow",count=1},new Objective{kind=ObjectiveKind.Interact,target="HushwellNursery",count=1}};
                q.rewardXp=220;q.rewardScrip=150;q.flagsOnComplete=new string[0];});
            AssetDatabase.SaveAssets();return zone;
        }

        // ---- Areas ---------------------------------------------------------

        static void Breach()
        {
            // The drill bore broke through the roof here; the miners' scaffold
            // stands under it and is the way back up.
            var climb=Prop("HushwellScaffold",0,-3.2f,2.6f,180).AddComponent<WarpBeacon>();
            climb.prompt="Climb the drill bore — Sorrel";climb.range=3.5f;climb.scene="Sorrel_Ridges";climb.spawn="Hushwell";climb.requiredFlag="";
            Prop("HushwellOreCart",8,9,1.6f,-25);Prop("DeckCrate",-9,10,1.1f,15);Prop("DeckCrate",-10.5f,8.5f,.9f,40);
            Prop("HushwellScaffold",-10,1,2.6f,90);
            Light("Work light",new Vector3(-8,3,2),new Color(1,.78f,.5f),2.2f,12);Light("Work light",new Vector3(7,3,11),new Color(1,.78f,.5f),2f,11);
            Safe("Drill breach",new Vector3(0,1,5),new Vector3(28,5,20));
            WorldBuilder.Label("DRILL BREACH",new Vector3(0,.1f,-1));
        }
        static void Galleries()
        {
            Encounter("Hushwell_0",new Vector2(-12,48),new Vector3(22,5,24),"Scrapmite",6);
            Encounter("Hushwell_1",new Vector2(8,84),new Vector3(22,5,22),"Scrapmite",6);
            Mine("hushwell_0",-22,42);Mine("hushwell_1",18,90);
            Prop("HushwellColumn",-19,56,5,20);Prop("HushwellColumn",-3,44,4.5f,-10);Prop("HushwellColumn",16,78,5,35);
            Prop("HushwellOreCart",-4,30,1.6f,60);
            // The survey-grid stretch: the grooves at their most regular, laid
            // out like a surveyor's grid before the ramp down.
            foreach(float z in new[]{100f,106f,112f})Prop("HushwellMarker",Route(z),z,.4f,0,false,4);
            Light("Mineral light",new Vector3(-22,2,42),new Color(.2f,.9f,1),2.4f,10);Light("Mineral light",new Vector3(18,2,90),new Color(.2f,.9f,1),2.4f,10);
            WorldBuilder.Label("SURVEY GRID",new Vector3(Route(106),.1f,106));
        }
        static void PressureGallery()
        {
            Encounter("Hushwell_2",new Vector2(0,164),new Vector3(26,5,22),"Ridgehound",3);
            Encounter("Hushwell_3",new Vector2(1,226),new Vector3(26,5,18),"Ridgehound",4);
            // Floor vents scald on the exhale; the ledges between them stay dry.
            foreach(var p in new[]{new Vector2(-7,158),new Vector2(8,170),new Vector2(-12,198),new Vector2(5,191),new Vector2(-7,230),new Vector2(9,222)})
            {
                var vent=Prop("HushwellVent",p.x,p.y,1.4f,p.x*7,true,3.2f);
                var pulse=new GameObject("Vent pulse",typeof(PressurePulse)).GetComponent<PressurePulse>();pulse.transform.position=HushwellLayout.OnFloor(p);
                pulse.hazardRadius=3.2f;pulse.throat=vent.GetComponentInChildren<Renderer>();
                Light("Vent glow",HushwellLayout.OnFloor(p)+Vector3.up*1.6f,new Color(.2f,.9f,1),1.2f,6);
            }
            VentPassage("East vent",new Vector2(HushwellLayout.EastVentX,164));
            VentPassage("West vent",new Vector2(HushwellLayout.WestVentX,212));
            // Behind the east vent: the miners' last cache. Behind the west: a crystal seam.
            var cache=Prop("DeckCrate",43,166,1.1f,20).AddComponent<SalvageField>();cache.cacheId="HushwellMiners";cache.requiredFlag="bossdown.Burrower";cache.prompt="Open the miners' cache";cache.range=2.6f;
            var trigger=cache.gameObject.AddComponent<SphereCollider>();trigger.isTrigger=true;trigger.radius=2.2f;
            Mine("hushwell_2",-40,213);
            Light("Mineral light",new Vector3(-40,-6,213),new Color(.2f,.9f,1),2.4f,10);
            Prop("HushwellColumn",-14,188,5,-15);Prop("HushwellColumn",10,202,4.5f,25);
            WorldBuilder.Label("PRESSURE GALLERY",new Vector3(0,-7.9f,150));
        }
        static void VentPassage(string label,Vector2 at)
        {
            var membrane=WorldBuilder.Membrane(label+" membrane",HushwellLayout.OnFloor(at)+Vector3.up*2,HushwellLayout.PocketHalfWidth*2+2,false);
            membrane.transform.rotation=Quaternion.Euler(0,90,0);Seal(membrane,false);
            var throat=Prop("HushwellVent",at.x,at.y+HushwellLayout.PocketHalfWidth+.6f,1,0,false,1.8f);
            var pulse=new GameObject(label+" pulse",typeof(PressurePulse)).GetComponent<PressurePulse>();pulse.transform.position=HushwellLayout.OnFloor(at);
            pulse.vent=membrane;pulse.throat=throat.GetComponentInChildren<Renderer>();
        }
        static void BellowsChamber()
        {
            var entry=WorldBuilder.Membrane("Bellows entry",new Vector3(Route(HushwellLayout.BellowsEntryZ),HushwellLayout.Middle+2,HushwellLayout.BellowsEntryZ),15,true);
            var exit=WorldBuilder.Membrane("Bellows exit",new Vector3(Route(HushwellLayout.BellowsExitZ),HushwellLayout.Middle+2,HushwellLayout.BellowsExitZ),15,false);
            Seal(entry,true);Seal(exit,false);
            var b=HushwellLayout.Bellows;
            var encounter=WorldBuilder.Encounter("Hushwell_Bellows",new Vector3(b.x,HushwellLayout.Middle+1,b.y),new Vector3(30,5,26),"BellowsBelow",1,new[]{entry,exit});
            encounter.GetComponentInChildren<Spawner>().transform.position=new Vector3(b.x,HushwellLayout.Middle,b.y+6);
            Prop("HushwellMarker",b.x,b.y,.4f,0,false,9);
            foreach(int side in new[]{-1,1})
            {
                var p=new Vector2(b.x+side*15,b.y+8);
                var organ=new GameObject(side<0?"Pressure organ west":"Pressure organ east");organ.transform.position=HushwellLayout.OnFloor(p);organ.isStatic=true;
                var health=organ.AddComponent<Health>();health.id="PressureOrgan";health.maximum=health.integrity=1500;health.breakThreshold=100000;health.weakness=DamageType.Kinetic;health.resistance=DamageType.Beam;
                var body=organ.AddComponent<SphereCollider>();body.center=Vector3.up*1.5f;body.radius=1.5f;organ.AddComponent<Hurtbox>().owner=health;
                var visual=blockout?ArenaBuilder.Block("Pressure organ blockout",Vector3.zero,new Vector3(3,3,3),"Enemy"):WorldBuilder.Piece("ChoirPod",Vector3.zero,Vector3.one*3.2f,"Emission",false);
                visual.isStatic=false;visual.transform.SetParent(organ.transform,false);visual.transform.localPosition=Vector3.up*1.5f;
                foreach(var c in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                organ.AddComponent<PressureOrgan>().visual=visual.transform;
                Light("Organ glow",HushwellLayout.OnFloor(p)+Vector3.up*3,new Color(.6f,.4f,1),2,9);
            }
            Prop("HushwellColumn",b.x-13,b.y+15,5,10);Prop("HushwellColumn",b.x+13,b.y+15,5,-20);
            WorldBuilder.Label("BELLOWS CHAMBER",new Vector3(b.x,HushwellLayout.Middle+.1f,b.y-17));
        }
        static void Nursery()
        {
            var n=HushwellLayout.Nursery;
            Prop("HushwellMarker",n.x,n.y,.4f,0,false,8);
            for(int i=0;i<5;i++){float a=(i*72+18)*Mathf.Deg2Rad;Prop("HushwellEggCradle",n.x+Mathf.Cos(a)*6.5f,n.y+Mathf.Sin(a)*5.5f,1.8f,-i*72+90,true,3.4f);}
            var look=new GameObject("The nursery",typeof(DiscoveryPoint)).GetComponent<DiscoveryPoint>();look.transform.position=HushwellLayout.OnFloor(n);
            look.prompt="Look at the eggs";look.range=9;look.flag="hushwell.nursery";
            var lift=Prop("HushwellLift",n.x+13,n.y+6,4,-90).AddComponent<WarpBeacon>();
            lift.prompt="Ride the drill-shaft lift — Sorrel";lift.lockedPrompt="The lift is dead until you look around";lift.range=4;lift.scene="Sorrel_Ridges";lift.spawn="Hushwell";lift.requiredFlag="hushwell.nursery";
            Light("Nursery glow",HushwellLayout.OnFloor(n)+Vector3.up*3,new Color(.45f,.5f,1),3,14);
            Light("Lift lamp",HushwellLayout.OnFloor(new Vector2(n.x+13,n.y+6))+Vector3.up*3,new Color(1,.82f,.55f),1.6f,8);
            Safe("Nursery",new Vector3(n.x,HushwellLayout.Lower+1,n.y),new Vector3(34,5,26));
            WorldBuilder.Label("NURSERY",new Vector3(n.x,HushwellLayout.Lower+.1f,n.y-12));
        }

        // ---- Dressing: what the miners left and what the nursery grows -----

        static void Dress()
        {
            // Each piece sits at least 4 m off the route line and 5 m from
            // spawners, vents and ore; the bake's route checks prove it.
            Prop("HushwellCrates",10,1,1.6f,-15);Prop("HushwellDrillBit",-6,-1,1,35,true,2.6f);Prop("HushwellFallenLamp",-4,13,.8f,70,true,2.6f);
            Prop("HushwellRubble",-20,52,1,10,true,2.6f);Prop("HushwellStalagmites",-4,54,3.2f,-20);Prop("HushwellRubble",-3,40,1,60,true,2.2f);
            Prop("HushwellStalagmites",0,93,3,15);Prop("HushwellRubble",14,76,1,-30,true,2.4f);
            Prop("HushwellLowRidge",4,157,1.1f,-10,true,4);Prop("HushwellRubble",-10,170,1,20,true,2.4f);
            Prop("HushwellChoirGrowth",4,203,1.8f,30);Prop("HushwellLowRidge",-6,190,1.1f,25,true,4);
            Prop("HushwellChoirGrowth",-8,221,1.8f,-40);Prop("HushwellStalagmites",10,232,3.4f,50);
            Prop("HushwellChoirGrowth",-14,250,2,10);Prop("HushwellChoirGrowth",14,251,1.9f,-70);
            Prop("HushwellShells",0,329,1,20,true,2.4f);Prop("HushwellShells",27,329,1,-50,true,2.2f);
            Prop("HushwellChoirGrowth",3,343,2,0);Prop("HushwellChoirGrowth",21,346,1.8f,120);
            Prop("HushwellFallenLamp",38,168,.8f,-60,true,2.6f);Prop("HushwellCrates",36,160,1.4f,25);
        }

        // ---- Walls: grooved faces on the far side of every floor -----------

        static void Walls()
        {
            var placed=new List<Vector2>();
            void Try(Vector2 p)
            {
                float c=HushwellLayout.Clear(p);if(c<.3f||c>1.6f)return;
                var outward=Gradient(p);if(outward.sqrMagnitude<.01f)return;outward.Normalize();
                // Rock on the camera side stays a low lip: no face there.
                if(Vector2.Dot(-outward,CameraForward)>0)return;
                // A face stands only where the rock behind it is full height, so
                // it can never hide a floor further along the camera.
                var behind=p+outward*1.5f;if(Height(behind)-HushwellLayout.Level(behind.y)<5.2f)return;
                // The slab is straight and 5 m wide: on a bend both ends must stay
                // in rock, or its ends poke into the floor beyond the curve.
                var along=new Vector2(-outward.y,outward.x);float top=HushwellLayout.Level(p.y)+5.2f;
                foreach(float end in new[]{-2.6f,0,2.6f})
                {
                    var q=p+along*end;if(HushwellLayout.Clear(q)<.15f||Hides(q,top)||Hides(q-outward*.8f,top))return;
                    var back=q+outward*1.5f;if(Height(back)-HushwellLayout.Level(back.y)<5.2f)return;
                }
                if(placed.Any(q=>(q-p).sqrMagnitude<4.4f*4.4f))return;placed.Add(p);
                float yaw=Mathf.Atan2(-outward.x,-outward.y)*Mathf.Rad2Deg;
                var go=Prop("HushwellWall",p.x,p.y,5,yaw,true);go.name="Grooved wall";
            }
            var main=HushwellLayout.Main;
            foreach(var path in new[]{main,HushwellLayout.EastPassage,HushwellLayout.WestPassage})
                for(int i=1;i<path.Length;i++)
                {
                    Vector2 a=path[i-1],d=path[i]-a;int steps=Mathf.CeilToInt(d.magnitude/2);var normal=new Vector2(-d.y,d.x).normalized;
                    float half=path==main?HushwellLayout.MainHalfWidth:HushwellLayout.PocketHalfWidth;
                    for(int s=0;s<steps;s++)foreach(int side in new[]{-1,1}){var q=a+d*(s/(float)steps)+normal*side*(half+.9f);Try(Refine(q));}
                }
            foreach(var room in HushwellLayout.Rooms)
            {
                int steps=Mathf.CeilToInt(2*Mathf.PI*Mathf.Max(room.rx,room.rz)/2);
                for(int s=0;s<steps;s++){float a=s*2*Mathf.PI/steps;Try(Refine(room.center+new Vector2(Mathf.Cos(a)*(room.rx+.9f),Mathf.Sin(a)*(room.rz+.9f))));}
            }
            Debug.Log("HUSHWELL_WALLS count="+placed.Count);
        }
        /// <summary>Would a face this tall at this spot hide a hero on any floor
        /// further along the camera, including lower floors down a ramp?</summary>
        static bool Hides(Vector2 at,float top)
        {
            for(float d=.5f;d<=16;d+=.5f){var q=at+CameraForward*d;if(HushwellLayout.Clear(q)<0&&top>HushwellLayout.Level(q.y)+.7f+.84f*d)return true;}
            return false;
        }
        static Vector2 Gradient(Vector2 p)=>new Vector2(HushwellLayout.Clear(p+Vector2.right*.5f)-HushwellLayout.Clear(p-Vector2.right*.5f),HushwellLayout.Clear(p+Vector2.up*.5f)-HushwellLayout.Clear(p-Vector2.up*.5f));
        /// <summary>Walk a candidate onto the rock just behind the floor edge.</summary>
        static Vector2 Refine(Vector2 p)
        {
            for(int i=0;i<6;i++){var g=Gradient(p);if(g.sqrMagnitude<.0001f)break;float c=HushwellLayout.Clear(p);p-=g.normalized*(c-.9f);}
            return p;
        }

        // ---- Terrain -------------------------------------------------------

        static float Height(Vector2 p)
        {
            float level=HushwellLayout.Level(p.y),clear=HushwellLayout.Clear(p);
            if(clear<=0)return level;
            // Distance from this rock, looking along the camera, to the next floor.
            // Rock stays under the sightline to that floor, measured from the
            // lower of the two levels so a ramp's shoulder never hides the next
            // chamber; a steep lip of at least 1.5 m still keeps heroes off it.
            float ahead=float.PositiveInfinity,baseLevel=level;
            for(float d=.5f;d<=8;d+=.5f){var q=p+CameraForward*d;if(HushwellLayout.Clear(q)<0){ahead=d;baseLevel=Mathf.Min(level,HushwellLayout.Level(q.y));break;}}
            float tall=6+1.4f*Mathf.PerlinNoise((p.x+40)*.09f,(p.y+12)*.07f);
            float top=baseLevel+Mathf.Min(tall,Mathf.Max(1.5f,.6f+.8f*ahead))-level;
            return level+top*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/1.2f));
        }
        static void Terrain()
        {
            const float step=.75f,x0=-64,z0=-24;const int nx=171,nz=519;
            var vertices=new Vector3[nx*nz];var uv=new Vector2[vertices.Length];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){var p=new Vector2(x0+x*step,z0+z*step);int i=z*nx+x;vertices[i]=new Vector3(p.x,Height(p),p.y);uv[i]=p/6;}
            var floor=new List<int>();var wall=new List<int>();var top=new List<int>();
            void Tri(int a,int b,int c)
            {
                var n=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).normalized;
                float above=(vertices[a].y+vertices[b].y+vertices[c].y)/3-HushwellLayout.Level((vertices[a].z+vertices[b].z+vertices[c].z)/3);
                var list=above<.25f?floor:n.y>.82f&&above>1.3f?top:wall;list.Add(a);list.Add(b);list.Add(c);
            }
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++){int i=z*nx+x;Tri(i,i+nx,i+1);Tri(i+1,i+nx,i+nx+1);}
            var mesh=new Mesh{name="Hushwell cave",indexFormat=IndexFormat.UInt32};mesh.vertices=vertices;mesh.uv=uv;mesh.subMeshCount=3;
            mesh.SetTriangles(floor,0);mesh.SetTriangles(wall,1);mesh.SetTriangles(top,2);mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path="Assets/_Project/Scenes/Hushwell-Terrain.asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);prior=mesh;}else{EditorUtility.CopySerialized(mesh,prior);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);}
            var cave=new GameObject("Cave rock and floor",typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));cave.isStatic=true;
            cave.GetComponent<MeshFilter>().sharedMesh=prior;cave.GetComponent<MeshCollider>().sharedMesh=prior;
            cave.GetComponent<Renderer>().sharedMaterials=blockout?new[]{Resources.Load<Material>("Blockout/Ground"),Resources.Load<Material>("Blockout/Rock"),Resources.Load<Material>("Blockout/Rock")}:
                new[]{Tinted("hushwell-floor","sorrel-ground",new Color(.5f,.45f,.46f)),Tinted("hushwell-rock","sorrel-rock",new Color(.4f,.35f,.34f)),Tinted("hushwell-cut","sorrel-rock",new Color(.11f,.1f,.11f))};
        }
        static Material Tinted(string name,string source,Color tint)
        {
            string path=Materials+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Materials+source+".mat"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",tint);EditorUtility.SetDirty(material);return material;
        }

        // ---- Pieces --------------------------------------------------------

        static float Route(float z)
        {
            var main=HushwellLayout.Main;
            for(int i=1;i<main.Length;i++)if(z<=main[i].y){float t=Mathf.InverseLerp(main[i-1].y,main[i].y,z);return Mathf.Lerp(main[i-1].x,main[i].x,t);}
            return main[main.Length-1].x;
        }
        static GameObject Prop(string key,float x,float z,float height,float yaw=0,bool solid=true,float footprint=0)
        {
            var envelope=footprint>0?new Vector3(footprint,height,footprint):key=="HushwellWall"?new Vector3(5,height,.8f):key=="HushwellLift"?new Vector3(3.2f,height,3.2f):Vector3.one*height;
            string grey=key.StartsWith("Crystal")||key=="HushwellVent"?"Emission":key.StartsWith("Hushwell")&&(key.EndsWith("Cart")||key.EndsWith("Scaffold")||key.EndsWith("Lift"))?"Sela":"Rock";
            GameObject go;
            if(blockout)go=ArenaBuilder.Block(key+" blockout",Vector3.zero,envelope,grey);
            else
            {
                if(footprint>0)
                {
                    // Footprint pieces are fitted across, not by height.
                    var probe=WorldBuilder.Piece(key,Vector3.zero,Vector3.one,"Rock",false);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                    height=footprint*b.size.y/Mathf.Max(b.size.x,b.size.z);
                }
                go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*height,"Rock",solid);
            }
            go.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(go);
            // Grooved marker slabs are inlaid: only their carved face stands proud.
            float surface=HushwellLayout.Level(z)+(key=="HushwellMarker"?-(bounds.size.y-.06f):.01f);
            go.transform.position+=new Vector3(x,surface,z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            if(!solid)foreach(var c in go.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            go.isStatic=solid;return go;
        }
        static void Encounter(string id,Vector2 at,Vector3 size,string enemy,int count)
        {
            float level=HushwellLayout.Level(at.y);
            var encounter=WorldBuilder.Encounter(id,new Vector3(at.x,level+1,at.y),size,enemy,count);
            encounter.GetComponentInChildren<Spawner>().transform.position=new Vector3(at.x,level,at.y+4);
        }
        static void Mine(string id,float x,float z)
        {
            var node=Prop("HushwellCrystalBed",x,z,1.2f,x*11,true,2.6f).AddComponent<MineNode>();node.nodeId=id;node.prompt="Mine Ridge Crystal";node.amount=1;
        }
        static void Light(string name,Vector3 position,Color color,float intensity,float range)
        {
            var light=new GameObject(name,typeof(Light)).GetComponent<Light>();light.transform.position=position;
            light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;
        }
        static void Safe(string name,Vector3 center,Vector3 size)
        {
            var safe=new GameObject(name+" (safe)",typeof(BoxCollider),typeof(SafePocket));safe.transform.position=center;
            var box=safe.GetComponent<BoxCollider>();box.size=size;box.isTrigger=true;
        }
        static void Seal(Membrane seal,bool open)
        {
            var box=seal.GetComponent<BoxCollider>()??throw new InvalidOperationException(seal.name+" needs measured box collision");
            var obstacle=seal.gameObject.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=box.center;obstacle.size=box.size;
            obstacle.carving=true;obstacle.carveOnlyStationary=false;obstacle.enabled=!open;
        }

        // ---- Navigation ----------------------------------------------------

        static void BakeNavigation()
        {
            Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();
            foreach(var collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(!collider.enabled||collider.isTrigger||!collider.gameObject.isStatic||collider.GetComponentInParent<Membrane>()!=null)continue;
                if(collider is BoxCollider box)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
                else if(collider is SphereCollider sphere)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Sphere,transform=sphere.transform.localToWorldMatrix*Matrix4x4.Translate(sphere.center),size=Vector3.one*sphere.radius*2,area=0});
                else if(collider is MeshCollider mesh&&mesh.sharedMesh!=null)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,sourceObject=mesh.sharedMesh,transform=mesh.transform.localToWorldMatrix,area=0});
            }
            var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.55f;settings.agentHeight=2;settings.agentClimb=.3f;settings.agentSlope=35;
            settings.overrideVoxelSize=true;settings.voxelSize=.15f;
            var built=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,-4,171),new Vector3(132,36,394)),Vector3.zero,Quaternion.identity);
            if(built==null)throw new InvalidOperationException("Hushwell navigation bake failed");
            // Check the cave itself with every seal open; the sealed states are
            // runtime behaviour, covered by the play tests.
            var seals=UnityEngine.Object.FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None).Where(o=>o.enabled).ToArray();
            foreach(var seal in seals)seal.enabled=false;
            var instance=NavMesh.AddNavMeshData(built);
            try
            {
                var start=HushwellLayout.OnFloor(HushwellLayout.Arrival);
                bool Reach(Vector3 goal,float sample)=>NavMesh.SamplePosition(goal,out var hit,sample,NavMesh.AllAreas)&&Complete(start,hit.position);
                // Every route point, both pockets and the nursery connect to the breach.
                var points=HushwellLayout.Main.Concat(HushwellLayout.EastPassage).Concat(HushwellLayout.WestPassage).Concat(HushwellLayout.Rooms.Select(r=>r.center)).ToList();
                foreach(var p in points)if(!Reach(HushwellLayout.OnFloor(p),2))throw new InvalidOperationException("Hushwell route blocked at "+p);
                foreach(var node in UnityEngine.Object.FindObjectsByType<MineNode>(FindObjectsSortMode.None))
                {
                    var p=node.transform.position;bool ok=false;
                    for(int a=0;a<16&&!ok;a++){var probe=p+Quaternion.Euler(0,a*22.5f,0)*Vector3.forward*2.4f;probe.y=HushwellLayout.Level(p.z);ok=NavMesh.SamplePosition(probe,out var hit,.6f,NavMesh.AllAreas)&&Vector3.Distance(hit.position,p)<=node.range&&Complete(start,hit.position);}
                    if(!ok)throw new InvalidOperationException("Hushwell ore has no reachable approach: "+node.nodeId);
                }
                // Rock is scenery: no wall top may join the walkable cave.
                int tops=0;
                for(float x=-60;x<=60;x+=3)for(float z=-20;z<=360;z+=3)
                {
                    var p=new Vector2(x,z);if(HushwellLayout.Clear(p)<3)continue;
                    if(NavMesh.SamplePosition(new Vector3(x,Height(p),z),out var hit,.5f,NavMesh.AllAreas)&&Complete(start,hit.position))
                        throw new InvalidOperationException("Hushwell rock top joins the route at "+hit.position);
                    tops++;
                }
                Debug.Log("HUSHWELL_ROUTE_BAKE_OK points="+points.Count+" rock-samples="+tops);
            }
            finally{instance.Remove();foreach(var seal in seals)seal.enabled=true;}
            string file="Assets/_Project/Scenes/Hushwell-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
            if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}
            else{EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
            new GameObject("Cave paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
        }
        static bool Complete(Vector3 from,Vector3 to){var path=new NavMeshPath();return NavMesh.CalculatePath(from,to,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;}
    }
}
