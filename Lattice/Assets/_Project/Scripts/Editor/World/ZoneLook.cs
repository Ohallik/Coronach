using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Authored light and colour grade for every zone (D189). Changes only the
    /// sun's colour/intensity (and elevation where stated), the ambient
    /// gradient, distance fog and each zone's post volume. Geometry, collision,
    /// navigation, cameras and gameplay objects are untouched. No blur: D060.
    /// </summary>
    public static class ZoneLook
    {
        public sealed class Spec
        {
            public Color sun=Color.white;public float sunIntensity=1.2f;public float? sunPitch;
            public float shadowStrength=.85f;
            public Color sky,equator,ground;
            public bool fog;public Color fogColor;public float fogStart=60,fogEnd=260;
            public float exposure,contrast,saturation;
            public Color splitShadows=new Color(.5f,.5f,.5f),splitHighlights=new Color(.5f,.5f,.5f);public float splitBalance;
            public float bloomThreshold=1f,bloomIntensity=.5f,bloomScatter=.65f;public Color bloomTint=Color.white;
            public float vignette=.2f;
            public bool practicals;
        }

        static Color C(float r,float g,float b)=>new Color(r,g,b,1);

        static readonly Color Neutral=new Color(.5f,.5f,.5f,1);
        public static readonly System.Collections.Generic.Dictionary<string,Spec> Specs=new()
        {
            // Dry moon in a long afternoon: warm sun, cool sky fill so shadows read
            // blue-violet against the ochre ground, warm bounce from below.
            ["Sorrel_Ridges"]=new Spec{sun=C(1f,.93f,.8f),sunIntensity=1.45f,sunPitch=44,shadowStrength=.85f,
                sky=C(.44f,.52f,.7f),equator=C(.42f,.39f,.38f),ground=C(.3f,.22f,.16f),
                fog=true,fogColor=C(.78f,.66f,.62f),fogStart=60,fogEnd=260,
                contrast=14,saturation=6,splitShadows=C(.42f,.47f,.6f),splitHighlights=C(.58f,.53f,.46f),splitBalance=0,
                bloomThreshold=1f,bloomIntensity=.45f,vignette=.22f},
            ["Arena_Ground"]=new Spec{sun=C(1f,.94f,.84f),sunIntensity=1.45f,shadowStrength=.85f,
                sky=C(.46f,.54f,.7f),equator=C(.42f,.39f,.37f),ground=C(.3f,.22f,.16f),
                fog=true,fogColor=C(.78f,.66f,.62f),fogStart=60,fogEnd=260,
                contrast=10,saturation=0,splitShadows=C(.42f,.47f,.6f),splitHighlights=C(.58f,.53f,.46f),
                bloomThreshold=1f,bloomIntensity=.45f,vignette=.2f},
            // Station deck: neutral hull light, gradient fill, glowing trim.
            ["Hub_Decks"]=new Spec{practicals=true,sun=C(1f,.98f,.95f),sunIntensity=1.2f,shadowStrength=.8f,
                sky=C(.4f,.45f,.56f),equator=C(.34f,.34f,.37f),ground=C(.2f,.17f,.15f),
                contrast=10,saturation=-4,splitShadows=Neutral,splitHighlights=Neutral,
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.2f},
            ["TallowDrift"]=new Spec{practicals=true,sun=C(1f,.97f,.92f),sunIntensity=1.2f,shadowStrength=.8f,
                sky=C(.42f,.45f,.54f),equator=C(.36f,.34f,.34f),ground=C(.2f,.16f,.14f),
                contrast=10,saturation=0,splitShadows=Neutral,splitHighlights=Neutral,
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.2f},
            // Open space: hard warm star, deep blue fill.
            ["Hub_CinderHalo"]=new Spec{sun=C(1f,.96f,.9f),sunIntensity=1.4f,shadowStrength=.85f,
                sky=C(.32f,.38f,.52f),equator=C(.24f,.26f,.32f),ground=C(.12f,.11f,.14f),
                contrast=14,saturation=4,splitShadows=C(.44f,.48f,.58f),splitHighlights=Neutral,
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.24f},
            ["Arena_Flight"]=new Spec{sun=C(1f,.96f,.9f),sunIntensity=1.4f,shadowStrength=.85f,
                sky=C(.32f,.38f,.52f),equator=C(.24f,.26f,.32f),ground=C(.12f,.11f,.14f),
                contrast=14,saturation=4,splitShadows=C(.44f,.48f,.58f),splitHighlights=Neutral,
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.24f},
            ["TallowApproach"]=new Spec{sun=C(1f,.95f,.88f),sunIntensity=1.4f,shadowStrength=.85f,
                sky=C(.32f,.37f,.5f),equator=C(.24f,.25f,.31f),ground=C(.12f,.11f,.14f),
                contrast=14,saturation=4,splitShadows=C(.44f,.48f,.58f),splitHighlights=Neutral,
                bloomThreshold=.9f,bloomIntensity=.65f,vignette=.24f},
            // Living warp tunnel: cold key, violet fill, bioluminescent veins bloom.
            ["Gullet_Tunnel"]=new Spec{sun=C(.86f,.9f,1f),sunIntensity=.85f,shadowStrength=.8f,
                sky=C(.2f,.28f,.42f),equator=C(.2f,.17f,.28f),ground=C(.09f,.07f,.12f),
                contrast=12,saturation=6,splitShadows=Neutral,splitHighlights=Neutral,
                bloomThreshold=.85f,bloomIntensity=.75f,bloomTint=C(.85f,.96f,1f),vignette=.26f},
            // Cave: its practical lights author the look; let crystals glow. The fill was set darker while the cave
            // was one renderer and its lamps never reached the floor; with lit chunks (D193) it rises a little so
            // the floor between pools still reads.
            ["Hushwell"]=new Spec{sun=C(.8f,.86f,1f),sunIntensity=.62f,shadowStrength=.8f,
                sky=C(.2f,.22f,.32f),equator=C(.16f,.15f,.18f),ground=C(.09f,.08f,.08f),
                exposure=.15f,contrast=10,saturation=4,splitShadows=Neutral,splitHighlights=Neutral,
                bloomThreshold=.85f,bloomIntensity=.7f,vignette=.24f},
        };

        /// <summary>Applies a spec to the open scene. Writes into <paramref name="profile"/>, which may be a temporary clone.</summary>
        public static void Configure(string zone,VolumeProfile profile)
        {
            var s=Specs[zone];
            var sun=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).ToArray();
            if(sun.Length!=1)throw new System.InvalidOperationException(zone+" needs exactly one directional light, found "+sun.Length);
            sun[0].color=s.sun;sun[0].intensity=s.sunIntensity;sun[0].shadowStrength=s.shadowStrength;
            if(s.sunPitch is float pitch){var e=sun[0].transform.eulerAngles;sun[0].transform.rotation=Quaternion.Euler(pitch,e.y,e.z);}
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=s.sky;RenderSettings.ambientEquatorColor=s.equator;RenderSettings.ambientGroundColor=s.ground;
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=s.fog;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=s.fogColor;
            RenderSettings.fogStartDistance=s.fogStart;RenderSettings.fogEndDistance=s.fogEnd;

            // Neutral/ACES curves dulled the toon palette in review (preview-01); keep linear output.
            if(profile.TryGet<Tonemapping>(out var tone))tone.active=false;
            var adjust=Get<ColorAdjustments>(profile);adjust.postExposure.Override(s.exposure);adjust.contrast.Override(s.contrast);adjust.saturation.Override(s.saturation);
            var split=Get<SplitToning>(profile);split.shadows.Override(s.splitShadows);split.highlights.Override(s.splitHighlights);split.balance.Override(s.splitBalance);
            var bloom=Get<Bloom>(profile);bloom.threshold.Override(s.bloomThreshold);bloom.intensity.Override(s.bloomIntensity);bloom.scatter.Override(s.bloomScatter);bloom.tint.Override(s.bloomTint);
            var vignette=Get<Vignette>(profile);vignette.intensity.Override(s.vignette);vignette.smoothness.Override(.45f);
            foreach(var dof in profile.components.OfType<DepthOfField>())dof.active=false; // D060: never blur.
            Practicals(s.practicals);
        }

        const string PracticalRoot="Practical lights",PracticalName="Practical light";
        /// <summary>People and services stand in warm pools of light, parented to their anchor. Rebuilt idempotently; no shadows or colliders.</summary>
        static void Practicals(bool enabled)
        {
            foreach(var old in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&(t.parent==null&&t.name==PracticalRoot||t.name==PracticalName)).ToArray())
                if(old!=null)Object.DestroyImmediate(old.gameObject);
            if(!enabled)return;
            var anchors=Object.FindObjectsByType<Lattice.Core.InteractionPrompt>(FindObjectsSortMode.None)
                .Where(p=>p is Lattice.World.Npc||p is Lattice.World.Shop||p is Lattice.World.Bench||p is Lattice.World.RepairBay)
                .OrderBy(p=>p.transform.position.x).ThenBy(p=>p.transform.position.z).ToList();
            // A pool of light on empty floor reads as a spotlight to nowhere: light
            // only anchors with a visible body or standing fixture within reach.
            var standing=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!(r is ParticleSystemRenderer)&&!(r is LineRenderer)&&r.bounds.size.y>.6f&&r.bounds.size.x<6&&r.bounds.size.z<6).ToArray();
            var placed=new System.Collections.Generic.List<Vector3>();
            foreach(var anchor in anchors)
            {
                var at=anchor.transform.position;
                var visible=standing.FirstOrDefault(r=>{var b=r.bounds;var c=new Vector2(b.center.x,b.center.z);return Vector2.Distance(c,new Vector2(at.x,at.z))<2.2f&&b.max.y>at.y+.6f&&b.min.y<at.y+.5f;});
                Debug.Log($"ZONE_LOOK_ANCHOR {anchor.GetType().Name} '{anchor.name}' at {at} visible={(visible!=null?visible.name:"none")}");
                if(visible==null)continue;
                if(placed.Any(p=>Vector3.Distance(p,at)<3.5f))continue;placed.Add(at);
                // Child of its anchor, so a speaker who leaves (Sela joining the party) takes the light along.
                var go=new GameObject(PracticalName);go.transform.SetParent(anchor.transform,false);go.transform.position=at+Vector3.up*1.3f; // the toon ramp bands distance attenuation only: a pool needs a low source
                var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.8f,.58f);
                light.range=6f;light.intensity=1.8f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.Auto;
            }
            Debug.Log("ZONE_LOOK_PRACTICALS "+placed.Count);
        }

        static T Get<T>(VolumeProfile profile) where T:VolumeComponent
        {
            if(!profile.TryGet<T>(out var c))
            {
                c=profile.Add<T>();
                if(EditorUtility.IsPersistent(profile))AssetDatabase.AddObjectToAsset(c,profile);
            }
            c.active=true;return c;
        }

        public static Volume SceneVolume(string zone)
        {
            var volumes=Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Where(v=>v.isGlobal&&v.sharedProfile!=null).ToArray();
            if(volumes.Length!=1)throw new System.InvalidOperationException(zone+" needs exactly one global volume, found "+volumes.Length);
            return volumes[0];
        }

        /// <summary>Temporary preview for LookDev: clone the profile so no asset changes.</summary>
        public static void Preview(string zone)
        {
            if(!Specs.ContainsKey(zone))return;
            var volume=SceneVolume(zone);var clone=Object.Instantiate(volume.sharedProfile);
            clone.components=volume.sharedProfile.components.Select(Object.Instantiate).ToList();
            volume.sharedProfile=clone;Configure(zone,clone);
        }

        /// <summary>Isolated scene refresh: lighting and grade only, then save.</summary>
        public static void Apply()=>BatchTools.Run(()=>
        {
            string only=Lattice.Core.DevArgs.Value("-look-zones");
            foreach(var zone in Specs.Keys)
            {
                if(!string.IsNullOrEmpty(only)&&!only.Split(',').Contains(zone))continue;
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                var volume=SceneVolume(zone);
                if(!EditorUtility.IsPersistent(volume.sharedProfile))throw new System.InvalidOperationException(zone+" volume profile is not an asset");
                Configure(zone,volume.sharedProfile);EditorUtility.SetDirty(volume.sharedProfile);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("ZONE_LOOK_APPLIED "+zone);
            }
            AssetDatabase.SaveAssets();Debug.Log("ZONE_LOOK_OK");
        });
    }
}
