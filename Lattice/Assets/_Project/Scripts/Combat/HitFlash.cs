using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>A struck body's own answer: it goes white for a moment, and one with no rig of its own to recoil is pressed flat and set back along the blow. The four rigged
    /// fighters keep their authored torso recoil and are only flashed: anything laid on their root would move the legs it was authored to leave alone. Runs last in the frame,
    /// after a boss's see-through door has written the same renderers. On the body before DefeatPresentation, so a death hands the body back unposed before that takes its measure.</summary>
    [DefaultExecutionOrder(950)]
    public sealed class HitFlash:MonoBehaviour
    {
        static readonly int Flash=Shader.PropertyToID("_HitFlash");
        Health health;Transform visual;bool poses,shared;
        readonly List<Renderer> found=new(),bodies=new();readonly List<bool> bare=new();
        MaterialPropertyBlock block;
        Color tint;float strength,flashAge=1;bool flashing;
        Vector3 along,basePosition,writtenPosition,baseScale,writtenScale;float squash,recoil,poseAge=1;bool applied;
        /// <summary>The flash now on the body, 0 to 1, for a check to read.</summary>
        public float Shown{get;private set;}
        /// <summary>The body is off its rest by a blow's squash or recoil this frame.</summary>
        public bool Posed=>applied;
        void Awake(){health=GetComponent<Health>();health.Died+=OnDown;}
        void OnDestroy(){if(health!=null)health.Died-=OnDown;}
        void Start()
        {
            var presentation=GetComponent<DefeatPresentation>();visual=presentation!=null?presentation.visual:null;
            var enemy=GetComponent<EnemyBrain>();var rig=GetComponentInChildren<Animator>();
            // A mine swells through its tell and bursts from that size, the Bellows breathes with its scale and a serpent's body is its segments: each owns the transform this would write.
            poses=enemy!=null&&visual!=null&&enemy.definition!=null&&!enemy.Mine&&enemy.definition.archetype!=EnemyArchetype.Serpent&&enemy.definition.id!="BellowsBelow"&&(rig==null||!rig.isHuman);
            // These write the same renderers' property blocks every frame or on their own events: a block is then never handed back empty.
            shared=GetComponent<BossOcclusion>()!=null||GetComponent<BurrowerShutdown>()!=null;
        }
        /// <param name="pose">The body may be pressed and set back: not a killing blow, whose body is the defeat's from this frame, and not one a real shove is already carrying.</param>
        /// <param name="scale">How much of the squash and recoil this body takes: a boss, a fraction.</param>
        /// <param name="squashAtLeast">A break presses any body this flat, whatever broke it.</param>
        public void Struck(HitWeight weight,Vector3 direction,Color colour,bool pose,float scale,float squashAtLeast)
        {
            int w=(int)weight;if(w<=0)return;
            tint=colour;strength=Impact.Tune.Flash[w];flashAge=0;
            if(!flashing){flashing=true;Collect();}
            if(!pose||!poses||!health.Alive)return;
            float press=Mathf.Max(Impact.Tune.Squash[w],squashAtLeast)*scale,back=Impact.Tune.Recoil[w]*scale;
            if(press<=0&&back<=0)return;
            // A heavier blow still ringing isn't cut down by a lighter one.
            float left=poseAge<Impact.Tune.PoseSeconds?(1-poseAge/Impact.Tune.PoseSeconds)*(1-poseAge/Impact.Tune.PoseSeconds):0;
            if(press<squash*left&&back<recoil*left)return;
            squash=press;recoil=back;along=direction;poseAge=0;
        }
        void Collect()
        {
            bodies.Clear();bare.Clear();GetComponentsInChildren(false,found);
            foreach(var body in found)
            {
                if(body is not MeshRenderer&&body is not SkinnedMeshRenderer||body.sharedMaterial==null||!body.sharedMaterial.HasProperty(Flash))continue;
                bodies.Add(body);bare.Add(!shared&&!body.HasPropertyBlock());
            }
            found.Clear();
        }
        void Write(float amount)
        {
            block??=new MaterialPropertyBlock();var value=new Color(tint.r,tint.g,tint.b,amount);
            for(int i=0;i<bodies.Count;i++)
            {
                var body=bodies[i];if(body==null)continue;
                // Whatever else is on the renderer stays on it: a boss's door, a rig's dying lamps.
                body.GetPropertyBlock(block);block.SetColor(Flash,value);body.SetPropertyBlock(block);
            }
        }
        void Clear()
        {
            // The block is made on the first write; a flash whose first frame came after its .09 s (one long frame, as a boss with a see-through door is struck) went straight here,
            // and a renderer that keeps a block was handed none to read into (OrielCombatTests, November 12: ArgumentNullException "dest").
            block??=new MaterialPropertyBlock();
            flashing=false;Shown=0;
            for(int i=0;i<bodies.Count;i++)
            {
                var body=bodies[i];if(body==null)continue;
                // A renderer that had no block gets none back, so it batches as it did. One that had keeps everything but the flash.
                if(bare[i]){body.SetPropertyBlock(null);continue;}
                body.GetPropertyBlock(block);block.SetColor(Flash,Color.clear);body.SetPropertyBlock(block);
            }
            bodies.Clear();bare.Clear();
        }
        void OnDown(Health _,DamagePacket __)=>Settle();
        /// <summary>Hands the transform back as it was found, unless somebody has written it since.</summary>
        void Settle()
        {
            poseAge=Impact.Tune.PoseSeconds;
            if(!applied||visual==null){applied=false;return;}
            if(visual.localPosition==writtenPosition)visual.localPosition=basePosition;
            if(visual.localScale==writtenScale)visual.localScale=baseScale;
            applied=false;
        }
        void LateUpdate()
        {
            if(flashing)
            {
                // Held white through the stop it came with, then gone in a few frames of real time. A pause holds it as it is.
                if(!GameTime.Paused&&!GameTime.Stopped)flashAge+=Time.unscaledDeltaTime;
                float left=1-Mathf.Clamp01(flashAge/Impact.Tune.FlashSeconds);
                if(left<=0)Clear();else{Shown=strength*left*left*ImpactSettings.FlashScale;Write(Shown);}
            }
            if(visual==null)return;
            // Last frame's offset comes off first. A tell's pose, a hover and a boss's wind-up write this transform outright, and then nothing of ours is left in it to take back.
            if(applied)
            {
                if(visual.localPosition==writtenPosition)visual.localPosition=basePosition;
                if(visual.localScale==writtenScale)visual.localScale=baseScale;
                applied=false;
            }
            if(poseAge>=Impact.Tune.PoseSeconds||!health.Alive)return;
            // On the fight's clock: the stop holds the body where the blow put it, and it springs back when time does.
            float k=1-poseAge/Impact.Tune.PoseSeconds;k*=k;poseAge+=Time.deltaTime;
            basePosition=visual.localPosition;baseScale=visual.localScale;
            var back=along*(recoil*k);
            visual.localPosition=basePosition+(visual.parent!=null?visual.parent.InverseTransformVector(back):back);
            visual.localScale=Vector3.Scale(baseScale,new Vector3(1+squash*k,1-squash*k,1+squash*k));
            writtenPosition=visual.localPosition;writtenScale=visual.localScale;applied=true;
        }
    }
}
