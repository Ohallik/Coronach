using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>A killed boss goes dark at once (D355): its lights go out and its colour drains, to ash if it is destroyed, cold if it is a machine shut down. The Rotbloom stood whole in
    /// its own colours for three seconds after its integrity was gone and the Bellows kept its seams lit (nov22-whole s26/454 to 469, s08/092 to 098): a body seen from above that only
    /// sinks in height looks the same as one alive. Added by DefeatPresentation as the body is beaten; on its own component, so it runs through a boss whose own ending stops that
    /// presentation (the Foundry Saint and the Tollkeeper settle where they stand). Runs after the flash and the see-through door, and writes only its own values into the block they share.</summary>
    /// <para>D363 (nov22b-whole s49/047 to 057): the ash it went to was paler than the body. Every generated body carries its colour in its texture under a white base colour, and the
    /// ash took that white's lightness (.45) and washed the texture's grain to under half, toward white: the Slag Colossus's dark iron came out a flat pale ochre the colour of the
    /// Foundry's floor (mean lightness 53 to 80 of 255 in BossDeathTests' own still, its contrast halved), and on the kill's frame the killing blow's flash burned along every
    /// curved plate of it, a glass shell. Now the paint is darkened over the texture as it is (destroyed: two fifths, shut down: half, driven off: three fifths), the killing blow's
    /// light on a body going dark is a glow, never the rim of a white shell, and it all runs through the kill's stop rather than waiting for it. A hero standing in a dead body sees
    /// out through a clean opening, not through two thirds of its pixels (BossOcclusion's living door).</para>
    /// <para>D385 (nov23-whole s49/052, s36/071, s29/034, s21/280, s08/092): the first frame after the end was still the living body or paler. The darkening began at nought on the
    /// frame after it was added, while the killing blow's flash burned at full on the kill's own frame (HitFlash writes after the boss's Died), the see-through door stood at its living
    /// two pixels in three (BossOcclusion), and the lights were still on: a still taken as the step ended caught a pale chrome Colossus, a pale Marshal, a washed Hulljack, a speckled
    /// Taren in the Floodcrown. Now the end is written on the frame it happens: half dark at once (<see cref="FirstShare"/>), its lights out (or to a tenth, driven off), the killing
    /// blow's light held to <see cref="KillLight"/> by HitFlash itself (<see cref="MostFlash"/>), and the door opened whole by BossOcclusion on a beaten body.</para>
    /// <para>D412 (nov24-whole s29/037 and 043, s26/473, s36/103): it was dark and not drained. Darkening the paint over the texture keeps the texture's own colours in their
    /// proportions, so a body whose colour is in its texture kept it: the Hulljack's yellow and black hazard stripes read plainly at half and at full dark, the Rotbloom half dark
    /// on its end's frame was still pink and cream, and the Host Marshal, its rust in its texture, went dark brown however blue the frost it was taken toward. Now a colourful
    /// texture is drained too: the shader's base map strength takes it toward its paint (to <see cref="Grain"/> of its own colour and contrast, as far as it is colourful: a grey
    /// body keeps its grain), the paint goes toward grey (<see cref="HueKept"/> of its own hue) and its tone, and its lightness is held on the curve it always had, measured with
    /// the texture's own mean (read once off the GPU). The end's own frame is most of the way there (<see cref="FirstShare"/>).</para>
    [DefaultExecutionOrder(960)]
    public sealed class BodyGoesDark:MonoBehaviour
    {
        /// <summary>How long it takes, its lights first: out by the time its paint is half drained.</summary>
        public const float Seconds=.7f;
        /// <summary>How dark it is on the frame its end comes: its lights out and most of its paint and colour gone, so no frame after the end is the living body. D412: at half the
        /// Rotbloom still read pink and cream on its end's frame (s26/473).</summary>
        public const float FirstShare=.8f;
        /// <summary>D412: what a colourful texture keeps of its own colour and contrast on a body fully dark (the shader's _BaseMapStrength, times this), and how colourful a
        /// texture is (<see cref="MeanOf"/>) before it is drained at all and once it is drained this far.</summary>
        public const float Grain=.35f,PlainChroma=.08f,ColourfulChroma=.2f;
        /// <summary>D412: what its paint keeps of its own hue, the rest grey (then its tone).</summary>
        public const float HueKept=.15f;
        /// <summary>The most the killing blow lights a body going dark: none. Its light is added to a dark body's colour before the screen's curve, so .03 at its rims lifted the
        /// darkest bodies over what they were alive on the end's own frame (BossDeathTests: the Slag Colossus .21 to .22, the Foundry Saint .25 to .26, Vessa Korl .22 to .23).
        /// The end is its own beat: the darkening, the stop, the sparks and the shake.</summary>
        public const float KillLight=0;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor"),OldColor=Shader.PropertyToID("_Color"),Emission=Shader.PropertyToID("_EmissionColor"),
            Flash=Shader.PropertyToID("_HitFlash"),DoorDepth=Shader.PropertyToID("_OccFadeDepth"),Strength=Shader.PropertyToID("_BaseMapStrength"),BaseMap=Shader.PropertyToID("_BaseMap"),Glow=Shader.PropertyToID("_Intensity");
        Renderer[] painted;Color[] paintRest;Vector4[] lightRest;bool[] lit;int[] paintId;
        /// <summary>D412: each renderer's base map strength as its material has it (-1: its shader has none), what it drains to, and its texture's mean lightness (linear).</summary>
        float[] strengthRest,strengthDark,grainLight,glowRest;
        MaterialPropertyBlock block;float age,share,paintKeep,lightKeep;bool ashes,done,restoring;Color tone=Color.white;
        /// <summary>How dark it is now, 0 its colours to 1 dark, for a check to read.</summary>
        public float Share=>share;
        /// <summary>How many of its renderers it darkens, for a check to read.</summary>
        public int Painted=>painted!=null?painted.Length:0;
        /// <summary>The most a blow's flash may light this body now (HitFlash asks, on the kill's own frame too).</summary>
        public float MostFlash=>restoring?float.PositiveInfinity:KillLight*(1-share);
        /// <param name="body">The whole body: everything drawn under its root, not only its presentation's model (a body staged with art of its own hangs that beside it).</param>
        public void Begin(Transform body,bool destroyed){ashes=destroyed;paintKeep=destroyed?.4f:.5f;lightKeep=0;Gather(body);}
        /// <summary>A boss driven off (BossDeparture) or leaving by its own road (Mother of Doors): it goes as one beaten, its lights down to a tenth and its paint to half, from the frame it is beaten.</summary>
        public void BeginBeaten(Transform body){ashes=false;paintKeep=.5f;lightKeep=.1f;Gather(body);}
        /// <summary>Its dark taken toward a colour (the Host Marshal's frost): the grey its paint drains to is tinted by this, as light as it would be untinted.</summary>
        public void Toward(Color colour){tone=new Color(Mathf.Clamp01(colour.r),Mathf.Clamp01(colour.g),Mathf.Clamp01(colour.b),1);Paint(share);}
        void Gather(Transform body)
        {
            share=0;age=0;done=false;block??=new MaterialPropertyBlock();
            var list=new List<Renderer>();var ids=new List<int>();
            foreach(var r in body.GetComponentsInChildren<Renderer>(true))
            {
                if(!(r is MeshRenderer||r is SkinnedMeshRenderer)||r.sharedMaterial==null||r.name=="Telegraph"||r.name=="Knot")continue;
                var m=r.sharedMaterial;int id=m.HasProperty(BaseColor)?BaseColor:m.HasProperty(OldColor)?OldColor:0;if(id==0)continue;
                list.Add(r);ids.Add(id);
            }
            painted=list.ToArray();paintId=ids.ToArray();paintRest=new Color[painted.Length];lightRest=new Vector4[painted.Length];lit=new bool[painted.Length];
            strengthRest=new float[painted.Length];strengthDark=new float[painted.Length];grainLight=new float[painted.Length];glowRest=new float[painted.Length];
            for(int i=0;i<painted.Length;i++)
            {
                var m=painted[i].sharedMaterial;paintRest[i]=m.GetColor(paintId[i]);
                lit[i]=m.HasProperty(Emission);lightRest[i]=lit[i]?m.GetVector(Emission):Vector4.zero;
                // D412: a colourful texture is drained toward its paint; a grey one keeps its grain (the Slag Colossus's iron, D363's "still its surface").
                // An energy part (the EnergyBubble shader: a rim of light, unlit) is a light: it goes out with the rest of them (the Hulljack's foot rings, Vessa's pole head).
                glowRest[i]=m.HasProperty(Glow)?m.GetFloat(Glow):-1;
                strengthRest[i]=m.HasProperty(Strength)?m.GetFloat(Strength):-1;grainLight[i]=1;strengthDark[i]=strengthRest[i];
                if(strengthRest[i]>0&&m.HasProperty(BaseMap))
                {
                    var (light,chroma)=MeanOf(m.GetTexture(BaseMap));grainLight[i]=light;
                    strengthDark[i]=strengthRest[i]*Mathf.Lerp(1,Grain,Mathf.InverseLerp(PlainChroma,ColourfulChroma,chroma));
                }
            }
            // Written now, in the Died that began it: this component's own LateUpdate may not run until the next frame, and the still taken as the fight's step ends is this one.
            share=FirstShare;age=Seconds*(1-Mathf.Sqrt(1-FirstShare));Paint(share);
        }
        /// <summary>Its colours given back, as it stood (a body revived), and this gone.</summary>
        public void Restore(){restoring=true;Paint(0);Destroy(this);}
        void LateUpdate()
        {
            if(painted==null)return;
            if(!done)
            {
                // Through the kill's stop, which holds the game's clock and not a pause: waiting for it, the body stood in its colours under the killing blow's light for the
                // whole stop (s49/047). Fastest at first, and no frame moves it by more than a small step however long it ran (a hitch is not a pop).
                if(!GameTime.Paused)age+=Time.unscaledDeltaTime;
                float left=1-Mathf.Clamp01(age/Seconds);
                share=Mathf.MoveTowards(share,1-left*left,Impact.Tune.MostStepAFrame);
                if(share>=1)done=true;
            }
            // Written every frame, dark or not yet: the see-through door writes its own share over a hero into the same block each frame before this (BossOcclusion).
            Paint(share);
        }
        /// <summary>A texture's mean lightness (linear) and how colourful it is (0 grey to 1 pure colour), read once off the GPU at sixteen texels a side and kept: what the shader
        /// multiplies the paint by, on average, and whether it carries the body's colour.</summary>
        static readonly Dictionary<Texture,(float light,float chroma)> means=new();
        static (float light,float chroma) MeanOf(Texture texture)
        {
            if(texture==null)return (1,0);
            if(means.TryGetValue(texture,out var known))return known;
            var mean=(light:1f,chroma:0f);
            var target=RenderTexture.GetTemporary(16,16,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var active=RenderTexture.active;
            try
            {
                Graphics.Blit(texture,target);RenderTexture.active=target;
                var read=new Texture2D(16,16,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,16,16),0,0,false);var texels=read.GetPixels();Destroy(read);
                // How colourful, as the eye has it (in the screen's curve, each texel's spread over its brightest, weighted by how bright): a dark navy is as colourful as a light one.
                double light=0,spread=0,top=0;
                foreach(var c in texels)
                {
                    light+=Light(c);float r=Mathf.LinearToGammaSpace(c.r),g=Mathf.LinearToGammaSpace(c.g),b=Mathf.LinearToGammaSpace(c.b),hi=Mathf.Max(r,Mathf.Max(g,b));
                    spread+=hi-Mathf.Min(r,Mathf.Min(g,b));top+=hi;
                }
                // Nothing read (no GPU to draw with) is taken as white, so the paint alone is drained as it always was.
                if(light>.0001)mean=((float)(light/texels.Length),top>0?(float)(spread/top):0);
            }
            finally{RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);}
            means[texture]=mean;return mean;
        }
        static float Light(Color c)=>.2126f*c.r+.7152f*c.g+.0722f*c.b;
        static bool LinearSpace=>QualitySettings.activeColorSpace==ColorSpace.Linear;
        /// <summary>Its paint a share <paramref name="k"/> of the way to dark: as light as its paint, darkened as it always was (destroyed, to two fifths; shut down or driven off,
        /// to half; in the colour space the block is given, so shares of how light it looks), times its texture as drawn at that share, and its hue drained toward grey and its tone.
        /// D412: darkened alone over its texture, a body kept the texture's colours (the Hulljack's stripes, the Marshal's rust under its frost).</summary>
        Color Shade(int i,float k)
        {
            var rest=paintRest[i];if(k<=0)return rest;
            bool linear=LinearSpace;var paint=linear?rest.linear:rest;float lightness=Light(paint);if(lightness<1e-4f)return rest;
            float grain=grainLight[i],from=Mathf.Max(0,strengthRest[i]),now=Mathf.Lerp(from,Mathf.Max(0,strengthDark[i]),k);
            float shown=1-now+now*grain,living=lightness*(1-from+from*grain),keep=Mathf.Lerp(1,ashes?.4f:paintKeep,k);
            float target=living*(linear?Mathf.Pow(keep,2.2f):keep)/Mathf.Max(.02f,shown);
            var toned=linear?tone.linear:tone;var grey=Color.Lerp(new Color(lightness,lightness,lightness),paint,HueKept);
            var hue=Color.Lerp(paint,new Color(grey.r*toned.r,grey.g*toned.g,grey.b*toned.b),k);float hueLight=Mathf.Max(1e-4f,Light(hue));
            var shade=new Color(hue.r*target/hueLight,hue.g*target/hueLight,hue.b*target/hueLight,rest.a);
            return linear?new Color(Mathf.LinearToGammaSpace(shade.r),Mathf.LinearToGammaSpace(shade.g),Mathf.LinearToGammaSpace(shade.b),rest.a):shade;
        }
        void Paint(float k)
        {
            if(painted==null)return;
            for(int i=0;i<painted.Length;i++)
            {
                var r=painted[i];if(r==null)continue;
                r.GetPropertyBlock(block);
                var shade=Shade(i,k);
                // An energy part goes out whole, its light and its veil: it is alpha blended, so its light alone out would leave a dark shell where it glowed.
                if(glowRest[i]>=0){float glowing=1-Mathf.Clamp01(k*2),left=lightKeep+(1-lightKeep)*glowing*glowing;block.SetFloat(Glow,glowRest[i]*left);shade.a*=left;}
                block.SetColor(paintId[i],shade);
                if(strengthRest[i]>=0)block.SetFloat(Strength,Mathf.Lerp(strengthRest[i],strengthDark[i],k));
                // HDR emission is a shader-space vector, so it is scaled as one (BurrowerShutdown), and it goes down twice as fast as the paint drains. Never brighter than the body
                // already is: a boss whose own ending put its lights out at once (the Debris Cutter, dead in space) keeps them out.
                if(lit[i])
                {
                    float on=1-Mathf.Clamp01(k*2);var light=lightRest[i]*(lightKeep+(1-lightKeep)*on*on);light.w=lightRest[i].w;
                    if(!restoring&&block.HasVector(Emission)){var now=block.GetVector(Emission);light=new Vector4(Mathf.Min(light.x,now.x),Mathf.Min(light.y,now.y),Mathf.Min(light.z,now.z),light.w);}
                    block.SetVector(Emission,light);
                }
                if(!restoring)
                {
                    // The killing blow's light (HitFlash, written just before this) burns along a body's silhouette: on a boss of curved plates at full strength that was a white
                    // glass shell. On a body going dark it is a short glow that goes as the dark comes.
                    if(block.HasVector(Flash)){var flash=block.GetVector(Flash);float most=KillLight*(1-k);if(flash.w>most){flash.w=most;block.SetVector(Flash,flash);}}
                    // Over a hero standing in it the door opens whole: through the living door's two pixels in three he was a speckled figure inside a dead body (s49/049).
                    block.SetFloat(DoorDepth,1);
                }
                r.SetPropertyBlock(block);
            }
        }
    }
}
