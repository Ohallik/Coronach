using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Space zones (from LRB's Wick Reach). Everything flies at <see cref="FlightPlane"/>; what's drawn below it is backdrop, never an obstacle, and shows south of where it stands.</summary>
    public sealed partial class ZoneKit
    {
        public const float FlightPlane=1,FlightPitch=48;
        /// <summary>Where a thing shows on the flight plane. The flight camera looks down at 48 degrees, so anything below the lane shows south of where it is, and anything above it north.</summary>
        public static Vector2 ShowsAt(Vector3 p)=>new(p.x,p.z-(FlightPlane-p.y)/Mathf.Tan(FlightPitch*Mathf.Deg2Rad));
        /// <summary>Whether a backdrop piece of this reach leaves every kept thing readable: a wreck 14 m under the lane sat on the warp's claws from 13 m away, so backdrop is kept clear
        /// by where it shows, not by where it is. <paramref name="keep"/> is what must stay readable (ways out, things to light, signs), each with the room it needs.</summary>
        public static bool ClearOnScreen(Vector3 p,float reach,IEnumerable<(Vector2 at,float clear)> keep)
        {
            var shows=ShowsAt(p);
            foreach(var (at,clear) in keep)if(Vector2.Distance(shows,at)<clear+reach)return false;
            return true;
        }
        /// <summary>The same panorama on a shader that can take a turn, tinted for this region: from a camera that never rotates a sky is painted on the screen, the same mote in the same
        /// place 100 m apart. Add <see cref="SkyDrift"/> to the scene (<see cref="DriftingSky"/>) or it stands still.</summary>
        public static Material Sky(string name,Material source,Color tint,float rotation,float exposure)
        {
            if(source==null)return null;
            string path="Assets/_Project/Resources/WorldMaterials/"+name+".mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(sky==null){sky=new Material(source){name=name};AssetDatabase.CreateAsset(sky,path);}else sky.CopyPropertiesFromMaterial(source);
            var drifting=Shader.Find("Lattice/DriftingSky");if(drifting!=null)sky.shader=drifting;
            sky.SetColor("_Tint",tint);sky.SetFloat("_Rotation",rotation);sky.SetFloat("_Exposure",exposure);EditorUtility.SetDirty(sky);return sky;
        }
        public void DriftingSky()=>new GameObject("Sky drift",typeof(SkyDrift));
        /// <summary>The soft additive flare a strand of light or a mote of dust is drawn with, saved beside the scene. fromFloor draws the curtain's bell, which comes up from nothing and fades upward.</summary>
        public Material Glow(string asset="Glow",bool fromFloor=false,Color? colour=null)
        {
            var beam=new Material(Resources.Load<Material>("Effects/flare_01"));
            beam.SetFloat("_Blend",2);beam.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);beam.SetFloat("_DstBlend",(float)BlendMode.One);beam.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);beam.SetFloat("_DstBlendAlpha",(float)BlendMode.One);
            beam.SetColor("_BaseColor",colour??(fromFloor?new Color(1.3f,1.05f,.85f,1):new Color(1.8f,1.35f,.9f,1)));
            if(fromFloor)beam.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Particles/curtain_soft.png"));
            AssetDatabase.CreateAsset(beam,"Assets/_Project/Scenes/"+zone+"-"+asset+".mat");return beam;
        }
        /// <summary>A thin layer of motes through a volume, so an empty stretch still moves: dust in a lane, pollen over a town.</summary>
        public ParticleSystem Drift(string name,Vector3 center,Vector3 size,Material glow,Color colour,int count=700,float small=.14f,float large=.5f,float speed=.3f,float life=16)
        {
            var go=new GameObject(name,typeof(ParticleSystem));go.transform.position=center;
            var dust=go.GetComponent<ParticleSystem>();dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=dust.main;main.loop=true;main.prewarm=true;main.duration=life;main.startLifetime=life;main.startSpeed=speed;main.startSize=new ParticleSystem.MinMaxCurve(small,large);
            main.startColor=colour;main.maxParticles=count;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;
            var emission=dust.emission;emission.rateOverTime=count/life;
            var shape=dust.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
            var fade=dust.colorOverLifetime;fade.enabled=true;var ramp=new Gradient();
            ramp.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.25f),new GradientAlphaKey(1,.75f),new GradientAlphaKey(0,1)});fade.color=ramp;
            var view=go.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=glow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            return dust;
        }
        /// <summary>A fight along a lane: the first wave's spawner where WorldBuilder puts it, each further kind beside it, all on the flight plane. Clearing it sets <c>clear.id</c>.</summary>
        public EncounterVolume FlightFight(string id,Vector3 at,Vector3 trigger,params (string enemy,int count)[] waves)
        {
            var encounter=WorldBuilder.Encounter(id,new Vector3(at.x,FlightPlane,at.z),trigger,waves[0].enemy,waves[0].count);
            var spawners=new List<Spawner>(encounter.spawners);
            for(int i=1;i<waves.Length;i++)spawners.Add(Spawn(encounter,waves[i].enemy,waves[i].count,new Vector3(at.x+(i%2==0?-9:9),FlightPlane,at.z+7),3));
            encounter.spawners=spawners.ToArray();foreach(var s in spawners)s.transform.position=new Vector3(s.transform.position.x,FlightPlane,s.transform.position.z);
            return encounter;
        }
        /// <summary>One more spawner on an encounter, at an exact spot: the encounter's list is the caller's to reassign.</summary>
        public static Spawner Spawn(EncounterVolume encounter,string enemy,int count,Vector3 at,float radius)
        {
            var extra=new GameObject("Spawn_"+enemy,typeof(Spawner));extra.transform.SetParent(encounter.transform,false);extra.transform.position=at;
            var spawner=extra.GetComponent<Spawner>();spawner.definition=GameCatalog.Find<EnemyDef>(enemy);spawner.count=count;spawner.radius=count>1?radius:0;return spawner;
        }
        /// <summary>Walls at the flight plane either side of a lane that bends, and across its two ends: each leg's pair runs on past its ends by the corridor's half width, so the
        /// outside of a bend is closed. The far reaches stay visible beyond them.
        /// <para><paramref name="overrun"/> (default: the half width) is how far each leg's walls run past its ends. Run the half width past a bend, the inside wall reaches back across
        /// the next leg's lane by that much (the Aurora Shear's starboard wall stood across a current 50 m to starboard); a bend of turn T is closed on its outside by
        /// half x tan(T/2), so a lane of gentle bends can pass that and no more. The lane's own two ends always run the half width on, to meet the walls across them
        /// (run less, the corners between a stretch's wall and the end wall stood open). QaPlanEarlyTests.NoLaneWallStandsInTheNextStretchsOpenSpace holds every lane to it.</para></summary>
        public void FlightCorridor(IReadOnlyList<Vector2> lane,float half,float overrun=-1)
        {
            float past=overrun<0?half:overrun;
            var root=new GameObject("Lane limits");
            void Wall(Vector2 from,Vector2 to,string name)
            {
                var run=to-from;var wall=new GameObject(name,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(root.transform,true);
                wall.transform.SetPositionAndRotation(new Vector3((from.x+to.x)*.5f,FlightPlane,(from.y+to.y)*.5f),Quaternion.Euler(0,Mathf.Atan2(run.x,run.y)*Mathf.Rad2Deg,0));
                wall.GetComponent<BoxCollider>().size=new Vector3(2,12,run.magnitude);
            }
            for(int i=1;i<lane.Count;i++)
            {
                var along=(lane[i]-lane[i-1]).normalized;var across=new Vector2(along.y,-along.x)*half;
                float before=i==1?half:past,after=i==lane.Count-1?half:past;
                foreach(int side in new[]{-1,1})Wall(lane[i-1]-along*before+across*side,lane[i]+along*after+across*side,"Lane limit "+i+(side<0?" port":" starboard"));
            }
            var first=(lane[1]-lane[0]).normalized;var last=(lane[lane.Count-1]-lane[lane.Count-2]).normalized;
            Wall(lane[0]-first*half-new Vector2(first.y,-first.x)*half,lane[0]-first*half+new Vector2(first.y,-first.x)*half,"Lane limit astern");
            Wall(lane[lane.Count-1]+last*half-new Vector2(last.y,-last.x)*half,lane[lane.Count-1]+last*half+new Vector2(last.y,-last.x)*half,"Lane limit ahead");
        }
        /// <summary>Walls at the flight plane round a lane; the far reaches stay visible beyond them.</summary>
        public void FlightLimits(float xMin,float xMax,float zMin,float zMax)
        {
            float midX=(xMin+xMax)*.5f,midZ=(zMin+zMax)*.5f,wide=xMax-xMin+4,deep=zMax-zMin+4;
            WorldBuilder.Boundary("West limit",new Vector3(xMin,FlightPlane,midZ),new Vector3(2,12,deep));WorldBuilder.Boundary("East limit",new Vector3(xMax,FlightPlane,midZ),new Vector3(2,12,deep));
            WorldBuilder.Boundary("South limit",new Vector3(midX,FlightPlane,zMin),new Vector3(wide,12,2));WorldBuilder.Boundary("North limit",new Vector3(midX,FlightPlane,zMax),new Vector3(wide,12,2));
        }
    }
}
