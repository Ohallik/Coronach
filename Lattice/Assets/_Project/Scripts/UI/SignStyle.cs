using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>One look for every sign in the world, set as a zone loads so every builder's signs agree and no rebuild shrinks them.
    /// Readers measured floor signs at 8 to 12 px of thin Cinzel that vanished on a light floor, slanted by the ground camera's yaw, and half sunk
    /// because each stood on end through its own middle. A floor sign now lies almost flat on the real ground, reads level to the camera at a
    /// size made for this zone's camera, in a heavier face over a soft dark painted plate. Every sign shares one text and one plate material.
    /// A person's name is PeopleMarks' business, and only takes the face.
    /// <para>The rules every word in the world keeps (the November 20 capture found sixty cut, and nine reviewers out of ten found the same faults):</para>
    /// <list type="bullet">
    /// <item>Nothing standing within <see cref="Reach"/> of a word, between it and the camera, cuts it: the word is drawn that much nearer the camera along its own lines of sight
    /// (Lattice/SignText, Lattice/SignPlate), so it covers the same pixels and is drawn over the lamp post, the well, the crate or the awning in front of it. What stands further off
    /// (a building's wall) still hides it, whole as a rule; the per-zone check (WorldLabelTests) lists any it cuts.</item>
    /// <item>A floor word holds one size on the screen within a band: nearer the camera than the camera's own distance it grows no more than <see cref="Band"/> times its made size.</item>
    /// <item>A word stands out from what is behind it: the ground under each floor word is looked at from straight over it as the zone loads, and its plate is as dark as that ground
    /// needs for white type to stand 3 to 1 (WCAG's large text) against it. A name has a plate of its own.</item>
    /// <item>Upright, gone under the HUD, a fight's bars or a conversation, and never under the type floor: SignFade and PeopleMarks.</item>
    /// </list></summary>
    public static class SignStyle
    {
        /// <summary>How tall a floor sign's em stands on a 1080p screen. Its capitals come to about 24 px after the floor's foreshortening.</summary>
        public const float FloorPixels=40;
        /// <summary>A warp's sign. Below the type floor nothing is readable, and one hung high above its gate looks bigger than it is.
        /// The floor is 24 and a hair: at the ground camera exactly 24 rounds to 23.999998, and the checks compare with "under 24".</summary>
        public const float WarpPixels=30,TypeFloorPixels=24.5f;
        /// <summary>Degrees from upright. 90 would be flat paint: at 72 it stays grounded from this camera and loses less to foreshortening.</summary>
        public const float FloorTilt=72,HangingTilt=60;
        public const string PlateName="Sign plate";
        /// <summary>How far nearer the camera a word is drawn, in metres. From the ground camera's 40 degrees that clears anything up to about 3.8 m tall standing right in front of it
        /// (a lamp's head, an awning, a stall's board, bunting); a building's wall stands taller and further off, and still hides what is behind it.</summary>
        public const float Reach=6;
        /// <summary>The most a floor word grows on the screen as it comes toward the foot of the screen, over its made size. At the foot of Tallow's screen PRESSURE LIFT stood 1.3 times
        /// the size of a word at the middle, and nearly twice one at the top.</summary>
        public const float Band=1.1f;
        /// <summary>Ships fly here: anything below it in a flight zone is lying on the floor under them.</summary>
        const float FlightPlane=1,Feather=.22f,MostOverLaid=.45f;
        /// <summary>Sunlit ground. A plate made for the Decks' dark floors is a dirty brown rectangle here, with tyre tracks running through it. On these it's lighter and warmer, and feathered so wide that it has no edge: a soft shade under words that hold by their own outline.</summary>
        static readonly HashSet<string> BrightFloors=new(){"Sorrel_Ridges","Lanternwick","LanternGorge"};
        const float WideFeather=.95f;
        /// <summary>How far a lying sign's plane runs on below its lower edge before the floor cuts it: its .03 m rest, down the plane at 72 degrees. A plate that feathered on past this was sliced by the floor in a hard straight line at about two thirds strength, so on bright ground its lower rim stops here, at nothing.</summary>
        const float DownToTheFloor=.09f;
        /// <summary>Ground pale enough all through for dark ink: its darker fifth at .4 in linear light, with room over what the play camera's own view of it reads (BOARDING SPINE, probed
        /// at .35, stood on .31). Under it (FOURTH RIB's .26, MESSAGE BOARD's .19) the slate stood 2.3 to 2.7
        /// against the darker parts, and white ink on the full plate reads instead.</summary>
        const float PaleThrough=.4f;
        /// <summary>The most a lighter plate (the pale shade, the Decks' dark plate) is laid on: on the screen they take only a quarter to a third off bright ground, which the grade
        /// compresses (a .62 plate took .47 to .35, measured November 21), so on brighter ground a word gets the full plate.</summary>
        const float Quiet=.26f;
        /// <summary>The dark ink: the HUD's deep slate (UiKit.PanelColor).</summary>
        static readonly Color Ink=new(.06f,.09f,.14f);
        static Material face,paint,paleGround,strong,shown;
        static readonly Dictionary<long,Mesh> plates=new();
        static readonly RaycastHit[] hits=new RaycastHit[16];
        static readonly int ReachId=Shader.PropertyToID("_Reach"),BandId=Shader.PropertyToID("_Band"),MadeId=Shader.PropertyToID("_SignMadeDistance");

        static Material Face
        {
            get
            {
                if(face==null)face=Resources.Load<Material>("Fonts & Materials/Cinzel SDF Sign");
                return face!=null?face:UiSkin.Display.material;
            }
        }
        static Shader text;
        static Shader Text=>text!=null?text:text=Resources.Load<Shader>("Effects/SignText");
        /// <summary>The face every sign is drawn in: the sign face on the shader that reaches toward the camera. One material for every sign in every zone.</summary>
        static Material Shown
        {
            get
            {
                if(shown!=null)return shown;
                if(Text==null||!Text.isSupported)return shown=Face;
                shown=new Material(Face){name="Cinzel SDF Sign (in the world)",shader=Text,hideFlags=HideFlags.DontUnloadUnusedAsset};
                shown.SetFloat(ReachId,Reach);shown.SetFloat(BandId,Band);return shown;
            }
        }
        static Material Reaching(Material from,Color color,string name,float band,float reach=Reach)
        {
            var made=new Material(from){name=name,hideFlags=HideFlags.DontUnloadUnusedAsset};made.SetColor("_BaseColor",color);made.SetFloat(ReachId,reach);made.SetFloat(BandId,band);return made;
        }
        static Material Paint
        {
            get
            {
                if(paint!=null)return paint;
                var shader=Resources.Load<Shader>("Effects/SignPlate");
                var bare=new Material(shader!=null?shader:Shader.Find("Sprites/Default"));
                paint=Reaching(bare,bare.HasProperty("_BaseColor")?bare.GetColor("_BaseColor"):new Color(.03f,.05f,.08f,.62f),"sign plate (runtime)",Band);Object.Destroy(bare);
                return paint;
            }
        }
        static Material PaleGround=>paleGround!=null?paleGround:paleGround=Reaching(Paint,new Color(.1f,.07f,.05f,.42f),"sign plate on bright ground (runtime)",Band);
        /// <summary>Snow, pale stone, a white hull: dark enough that white type stands 3 to 1 on ground as bright as there is.</summary>
        static Material Strong=>strong!=null?strong:strong=Reaching(Paint,new Color(.03f,.05f,.09f,.95f),"sign plate at full strength (runtime)",Band);
        /// <summary>The most Sorrel's sunlit floors are given: a warm shade just under half, which is what they're held to (a dark plate on Sorrel's tan was a dirty
        /// brown rectangle with tyre tracks through it; QaWorldThirdTests keeps SORREL OUTPOST's under .5).</summary>
        static Material SunlitMost=>sunlitMost!=null?sunlitMost:sunlitMost=Reaching(Paint,new Color(.08f,.06f,.04f,.49f),"sign plate on sunlit ground, its most (runtime)",Band);
        static Material sunlitMost,light;
        /// <summary>Behind dark ink on a hanging sign: a pale plate, so the slate stands on light whatever is behind it.</summary>
        static Material Light=>light!=null?light:light=Reaching(Paint,new Color(.9f,.92f,.95f,.6f),"sign plate under dark ink (runtime)",Band);
        /// <summary>A person's name: the legible face, reaching toward the camera like a sign's, its underlay taken to a full dark halo round every letter. Over a notice board's
        /// printed lines ORRIN read as part of the board; on snow OLD TERN's gold was a third of the way to white. The halo is in the name's own text: a plate of its own was a
        /// second thing hanging in the air over every head (QaPlanWorldTests.NothingFloatsOrSinksIn, 'Sign_Jun floats 0.66 m'), and TextMeshPro's mark is drawn over the letters.</summary>
        public static Material NameFace(TMP_FontAsset font)
        {
            if(font==null)return null;
            if(nameFaces.TryGetValue(font,out var known)&&known!=null)return known;
            var legible=UiKit.LegibleMaterial(font);if(legible==null)return null;
            if(Text==null||!Text.isSupported)return nameFaces[font]=legible;
            var made=new Material(legible){name=legible.name+" (in the world)",shader=Text,hideFlags=HideFlags.DontUnloadUnusedAsset};
            made.EnableKeyword("UNDERLAY_ON");
            made.SetColor("_UnderlayColor",new Color(0,.01f,.03f,1));made.SetFloat("_UnderlayDilate",1);made.SetFloat("_UnderlaySoftness",.45f);
            made.SetFloat("_UnderlayOffsetX",0);made.SetFloat("_UnderlayOffsetY",-.25f);
            ShaderUtilities.UpdateShaderRatios(made);
            made.SetFloat(ReachId,Reach);made.SetFloat(BandId,0);return nameFaces[font]=made;
        }
        static readonly Dictionary<TMP_FontAsset,Material> nameFaces=new();
        /// <summary>A person's name with a dark tag behind it, in its own text: the tag is TextMeshPro's mark over an unseen copy of the name, and the name itself is set back over it
        /// (a zero-width break, then a step back by the copy's width). TextMeshPro writes every mark after every letter, so `Lattice/SignText` draws marks in a pass of their own,
        /// before the letters. The tag stands nine tenths of an em past the line the name sits in, all round, as a sign's plate stands past its word: at two fifths, and at three
        /// quarters by half an em, the ground round a name's letters (snow, a notice board's paper, a lamp-lit floor) showed past the tag's edge (WorldLabelTests, November 21).</summary>
        public static string Backed(TextMeshPro text,string words)=>Backing(text,words)+words;
        /// <summary>The tag alone, the pen set back to where it began: the words that follow are drawn on it.</summary>
        public static string Backing(TextMeshPro text,string words)
        {
            if(text==null||string.IsNullOrEmpty(words))return "";
            float em=text.fontSize*.1f;if(em<=0)return "";
            float wide=text.GetPreferredValues(words).x/em;
            return NameBacking+"<color=#00000000>"+words+"</color></mark>​<space=-"+wide.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)+"em>";
        }
        const string NameBacking="<mark=#05080EDB padding=\"90,90,90,90\">";

        public static void Apply()
        {
            var zone=ZoneController.Current;bool flight=zone!=null&&zone.Flight;
            // The list is the first chapters'; a zone since says so itself (ZoneDef.brightFloor).
            bool first=!flight&&zone!=null&&zone.definition!=null&&BrightFloors.Contains(zone.definition.id);
            bool bright=first||!flight&&zone!=null&&zone.definition!=null&&zone.definition.brightFloor;
            // Sorrel's own tan, where a dark plate was a dirty brown rectangle with tyre tracks through it (QaWorldThirdTests holds SORREL OUTPOST's under .5): its words take no
            // more than SunlitMost.
            bool sorrel=first&&zone.definition.id=="Sorrel_Ridges";
            var view=zone!=null&&zone.definition!=null?zone.definition.cameraProfile:null;
            float yaw=view!=null?view.yaw:0;
            float slide=view!=null?Mathf.Sin(view.pitch*Mathf.Deg2Rad)/Mathf.Max(1,view.distance):0;
            Shader.SetGlobalFloat(MadeId,view!=null?view.distance:0);
            var people=new HashSet<string>();
            foreach(var npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None))people.Add(DisplayNames.Speaker(npc.speaker));
            var warps=Object.FindObjectsByType<WarpBeacon>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var signs=new List<(TextMeshPro sign,Vector2 size,bool lying)>();
            foreach(var sign in Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!sign.name.StartsWith("Sign_"))continue;
                sign.font=UiSkin.Display;sign.fontSharedMaterial=Shown;
                if(string.IsNullOrEmpty(sign.text)||people.Contains(sign.text)||sign.GetComponentInParent<Npc>(true)!=null)continue;
                bool gate=IsWarp(sign.text,warps),lying=!gate&&(!flight||sign.transform.position.y<FlightPlane);
                if(lying)sign.fontSize=Mathf.Max(sign.fontSize,UiKit.WorldFontSize(FloorPixels));
                else if(gate&&view!=null)
                {
                    // Closer to the camera than the floor it answers to, so smaller to stand the same height on screen.
                    float plane=flight?FlightPlane:Ground(sign.transform.position,Vector3.zero,flight,out float below)?below:sign.transform.position.y;
                    float near=1-Mathf.Max(0,sign.transform.position.y-plane)*slide;
                    sign.fontSize=Mathf.Max(UiKit.WorldFontSize(WarpPixels)*near,UiKit.WorldFontSize(TypeFloorPixels));
                }
                else sign.fontSize=Mathf.Max(sign.fontSize,UiKit.WorldFontSize(TypeFloorPixels));
                var size=PlateSize(sign);
                sign.transform.rotation=Quaternion.Euler(lying?FloorTilt:HangingTilt,yaw,0);
                if(lying)Settle(sign,size,flight);
                var tone=sign.color;sign.color=new Color(.96f,.98f,1,tone.a);
                signs.Add((sign,size,lying));
                var fade=sign.gameObject.AddComponent<SignFade>();fade.bossOnly=!lying||flight||Overhead(sign.transform.position);
            }
            // Each word's plate, by what is behind it from the play camera. The fade found no plate when it woke, so it's told.
            var behind=view==null?new (float low,float high)[signs.Count]:Behind(signs,view,flight);
            for(int i=0;i<signs.Count;i++)
            {
                var (sign,size,lying)=signs[i];var (low,high)=behind[i];
                // On ground pale all through (its darker fifth pale too) a word is printed in dark ink with a pale halo, over the lightest plate there is: a plate under a danger
                // marking can't darken light that is drawn after it (D349's ruling), so on snow and pale stone the ink does it. One that lies across pale and dark keeps white ink
                // and the plate.
                if(low>=PaleThrough)
                {
                    sign.color=new Color(Ink.r,Ink.g,Ink.b,sign.color.a);
                    // A hanging sign's plate is the dark ink's own pale ground; a floor sign keeps the lightest dark plate, as every floor sign is held to one (QaChapterOneLookTests).
                    AddPlate(sign,size,lying?Shape.Soft:Shape.Narrow,lying?PaleGround:Light);
                    if(sign.TryGetComponent<SignFade>(out var inked))inked.FindPlate();
                    continue;
                }
                // The least plate that leaves what is behind the letters dark enough, with room to spare. A pale soft shade where it does (the first chapters' sunlit floors were
                // given one and Sorrel's is held to it: QaWorldThirdTests, a soft shade on its tan, never a box); the dark plate the Decks' floors have always had; the strong one,
                // feathered as wide as the pale, on snow, pale stone, a white hull or a gate's lit ring.
                if(lying&&bright&&high<=Quiet)AddPlate(sign,size,Shape.Soft,PaleGround);
                else if(lying&&sorrel)AddPlate(sign,size,Shape.Soft,SunlitMost);
                // A sign hanging over a flight zone's plane has debris drifting behind it, which the probe at load can't see: WARP — THE WICK REACH's dash and T stood on a pale chunk
                // passing behind them (s12 026, D356). It takes the full plate, which over open space is no darker than the space it hides.
                else if(!bright&&high<=Quiet&&!(flight&&!lying))AddPlate(sign,size,Shape.Narrow,Paint);
                else AddPlate(sign,size,Shape.Full,Strong);
                if(sign.TryGetComponent<SignFade>(out var fade))fade.FindPlate();
            }
        }
        static bool IsWarp(string text,WarpBeacon[] warps)
        {
            if(text.StartsWith("Warp"))return true;
            foreach(var warp in warps)if(warp.prompt.StartsWith(text))return true;
            return false;
        }
        static Vector2 PlateSize(TextMeshPro sign)
        {
            var font=sign.font;float em=sign.fontSize*.1f,scale=em/font.faceInfo.pointSize*font.faceInfo.scale,width=0;
            foreach(char c in sign.text)width+=font.characterLookupTable.TryGetValue(c,out var known)?known.glyph.metrics.horizontalAdvance*scale:em*.7f;
            return new Vector2(width+em*.5f+.3f,em*1.15f+.2f);
        }
        /// <summary>Its lower edge rests on the ground under its whole width, so uneven floor can't sink it and it never needs a builder's guess at the height.</summary>
        static void Settle(TextMeshPro sign,Vector2 size,bool flight)
        {
            var at=sign.transform.position;float highest=float.NegativeInfinity;var along=sign.transform.right*size.x*.5f;var across=Vector3.forward*.4f;
            foreach(var offset in new[]{Vector3.zero,along,-along,across,-across})
                if(Ground(at,offset,flight,out float ground))highest=Mathf.Max(highest,ground);
            if(float.IsNegativeInfinity(highest))return;
            at.y=highest+.03f+size.y*.5f*Mathf.Cos(FloorTilt*Mathf.Deg2Rad);sign.transform.position=at;
        }
        /// <summary>The floor under a point: props stand on it, and a body or a seal isn't it. Nor is a low wall a wide sign's end reaches over: the real floor is near where the builder laid the sign.</summary>
        static bool Ground(Vector3 at,Vector3 offset,bool flight,out float ground)
        {
            ground=0;bool found=false;
            int count=Physics.RaycastNonAlloc(at+offset+Vector3.up*1.5f,Vector3.down,hits,flight?14:7,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(hit.normal.y<.6f||hit.point.y>at.y+MostOverLaid||hit.collider is CharacterController||hit.collider.GetComponentInParent<Membrane>()!=null)continue;
                var extent=hit.collider.bounds.size;if(extent.x<3&&extent.z<3)continue;
                if(!found||hit.point.y>ground){ground=hit.point.y;found=true;}
            }
            return found;
        }
        /// <summary>Resting on a door's lintel or the like, with a floor a long way under it: nobody stands on it, so no hero thins it out. A floor with another deck under it is still a floor: the Decks' floor tiles have one 3 m down. What sets a lintel apart is that it's narrow.</summary>
        static bool Overhead(Vector3 at)
        {
            float high=float.NegativeInfinity,low=float.PositiveInfinity;bool narrow=false;
            int count=Physics.RaycastNonAlloc(at+Vector3.up*.5f,Vector3.down,hits,6,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                if(hits[i].normal.y<.6f||hits[i].collider is CharacterController)continue;
                float y=hits[i].point.y;var extent=hits[i].collider.bounds.size;bool slim=Mathf.Min(extent.x,extent.z)<1;
                if(y>high+.01f){high=y;narrow=slim;}else if(y>high-.01f)narrow&=slim;
                low=Mathf.Min(low,y);
            }
            return narrow&&high-low>2;
        }

        // ---------- what is behind a word ----------
        const int ProbeWide=384,ProbeHigh=216;
        static System.Type cameraData;
        /// <summary>How bright what is behind each word is, in linear light, its darker fifth and its brighter fifth: from where the play camera stands when the word is in the middle of the screen, with
        /// its post-processing, and with nothing nearer the camera than a metre in front of the word, every word and plate and the party out of the picture. The floor under a
        /// floor sign; for a gate's name the lit ring behind it, and for one over open space what shows through. A small render a word as the zone loads. With nothing to render
        /// with (a run without graphics) everything reads dark, which leaves each plate as it always was.</summary>
        static (float low,float high)[] Behind(List<(TextMeshPro sign,Vector2 size,bool lying)> signs,CameraProfile view,bool flight)
        {
            var found=new (float low,float high)[signs.Count];if(signs.Count==0||SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return found;
            var hidden=new List<Renderer>();
            foreach(var t in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None)){var r=t.GetComponent<Renderer>();if(r!=null&&!r.forceRenderingOff){r.forceRenderingOff=true;hidden.Add(r);}}
            foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))if(r.name==PlateName&&!r.forceRenderingOff){r.forceRenderingOff=true;hidden.Add(r);}
            var party=Lattice.Combat.PartyController.Current;
            if(party!=null&&party.members!=null)foreach(var m in party.members)if(m!=null)foreach(var r in m.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff){r.forceRenderingOff=true;hidden.Add(r);}
            var go=new GameObject("Sign probe"){hideFlags=HideFlags.HideAndDontSave};var probe=go.AddComponent<Camera>();probe.enabled=false;
            probe.clearFlags=CameraClearFlags.SolidColor;probe.backgroundColor=Color.black;probe.cullingMask=~(1<<5);probe.allowMSAA=false;probe.fieldOfView=view.fov;
            WithPost(go);
            var target=new RenderTexture(ProbeWide,ProbeHigh,24,RenderTextureFormat.ARGB32);target.Create();var image=new Texture2D(ProbeWide,ProbeHigh,TextureFormat.RGBA32,false);
            var before=RenderTexture.active;var light=new List<float>(4096);var turn=Quaternion.Euler(view.pitch,view.yaw,0);
            var main=Camera.main;float seeing=main!=null?main.farClipPlane:0;
            try
            {
                probe.targetTexture=target;
                for(int i=0;i<signs.Count;i++)
                {
                    var (sign,size,lying)=signs[i];var t=sign.transform;var focus=t.position;if(flight)focus.y=FlightPlane;else if(!lying&&Ground(t.position,Vector3.zero,false,out float floor))focus.y=floor;
                    probe.transform.SetPositionAndRotation(focus-turn*Vector3.forward*view.distance,turn);
                    // As far as the play camera sees: at 400 m past the word, the way down's planet (760 m under the lane) was cut from the probe, which read the black it was cleared
                    // to, and gave THE WAY DOWN the faint plate where it lies across Hoarfell's lit limb (WorldLabelTests, November 21).
                    float far=Vector3.Distance(probe.transform.position,t.position);probe.nearClipPlane=Mathf.Max(.3f,far-1);probe.farClipPlane=Mathf.Max(far+400,seeing);
                    // Where its letters and their plate's middle fall on this view.
                    float xMin=1e9f,xMax=-1e9f,yMin=1e9f,yMax=-1e9f;
                    foreach(var dx in new[]{-.5f,.5f})foreach(var dy in new[]{-.5f,.5f})
                    {
                        var p=probe.WorldToScreenPoint(t.TransformPoint(new Vector3(dx*size.x,dy*size.y,0)));
                        xMin=Mathf.Min(xMin,p.x);xMax=Mathf.Max(xMax,p.x);yMin=Mathf.Min(yMin,p.y);yMax=Mathf.Max(yMax,p.y);
                    }
                    int x0=Mathf.Clamp((int)xMin,0,ProbeWide-1),x1=Mathf.Clamp((int)xMax,0,ProbeWide-1),y0=Mathf.Clamp((int)yMin,0,ProbeHigh-1),y1=Mathf.Clamp((int)yMax,0,ProbeHigh-1);
                    probe.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,ProbeWide,ProbeHigh),0,0,false);
                    var pixels=image.GetPixels32();light.Clear();
                    for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){var c=pixels[y*ProbeWide+x];light.Add(.2126f*Linear(c.r)+.7152f*Linear(c.g)+.0722f*Linear(c.b));}
                    if(light.Count==0)continue;
                    light.Sort();found[i]=(light[Mathf.Clamp(Mathf.RoundToInt(.2f*(light.Count-1)),0,light.Count-1)],light[Mathf.Clamp(Mathf.RoundToInt(.8f*(light.Count-1)),0,light.Count-1)]);
                }
            }
            finally
            {
                RenderTexture.active=before;probe.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(image);Object.Destroy(go);
                foreach(var r in hidden)if(r!=null)r.forceRenderingOff=false;
            }
            return found;
        }
        /// <summary>The probe sees what the player sees after the grade: URP's own camera data with post-processing on, its volumes as the game camera's (reached by name, since
        /// this assembly doesn't reference URP).</summary>
        static void WithPost(GameObject probe)
        {
            cameraData??=System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");if(cameraData==null)return;
            var data=probe.GetComponent(cameraData)??probe.AddComponent(cameraData);cameraData.GetProperty("renderPostProcessing")?.SetValue(data,true);
            var main=Camera.main;var from=main!=null?main.GetComponent(cameraData):null;var mask=cameraData.GetProperty("volumeLayerMask");
            if(from!=null&&mask!=null)mask.SetValue(data,mask.GetValue(from));
        }
        static float Linear(byte c){float v=c/255f;return v<=.04045f?v/12.92f:Mathf.Pow((v+.055f)/1.055f,2.4f);}

        // ---------- plates ----------
        /// <summary>Narrow: solid to the plate's edge, feathered a hand beyond (the Decks' floors, a gate's sign). Soft: solid under the whole word, feathered a metre to its sides, half that over it and down to the floor under it:
        /// a shade, not a box, that still darkens every letter's ground. Full: the full-strength plate, solid a quarter metre past the word each side and fifteen centimetres over and under, feathered half a metre all round, under it as
        /// well: drawn nearer the camera than the floor (Reach), it can't be sliced by it, and a plate that stopped at the floor line left bright ground right under the letters
        /// (BEAST PENS stood 2.8 to 1 on a .95 plate, November 21).</summary>
        enum Shape{Narrow,Soft,Full}
        static readonly Vector2 FullMargin=new(.25f,.15f);const float FullFeather=.5f;
        static void AddPlate(TextMeshPro sign,Vector2 size,Shape shape,Material material)
        {
            var plate=new GameObject(PlateName,typeof(MeshFilter),typeof(MeshRenderer));plate.layer=sign.gameObject.layer;
            plate.transform.SetParent(sign.transform,false);plate.transform.localPosition=Vector3.forward*.01f;
            plate.GetComponent<MeshFilter>().sharedMesh=PlateMesh(size,shape);
            var view=plate.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.receiveShadows=false;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        /// <summary>A flat plate whose paint feathers out to nothing at its rim. Plates of one size share a mesh for the life of the game.</summary>
        static Mesh PlateMesh(Vector2 size,Shape shape)
        {
            if(shape==Shape.Full)size+=new Vector2(FullMargin.x*2,FullMargin.y*2);
            bool wide=shape==Shape.Soft;float reach=shape==Shape.Full?FullFeather:wide?WideFeather:Feather;
            long key=Mathf.RoundToInt(size.x*10)*100000L+Mathf.RoundToInt(size.y*10)+(long)shape*25000;
            if(plates.TryGetValue(key,out var mesh)&&mesh!=null)return mesh;
            float x=size.x*.5f,y=size.y*.5f,inX=x,inY=y;
            float[] xs={-x-reach,-inX,inX,x+reach},ys={wide?-y-DownToTheFloor:-y-reach,-inY,inY,y+(wide?reach*.5f:reach)};
            var vertices=new Vector3[16];var colors=new Color32[16];var triangles=new int[54];int t=0;
            for(int j=0;j<4;j++)for(int i=0;i<4;i++)
            {
                vertices[j*4+i]=new Vector3(xs[i],ys[j],0);
                colors[j*4+i]=new Color32(255,255,255,(byte)((i==1||i==2)&&(j==1||j==2)?255:0));
                if(i==3||j==3)continue;
                int a=j*4+i;
                triangles[t++]=a;triangles[t++]=a+4;triangles[t++]=a+1;
                triangles[t++]=a+1;triangles[t++]=a+4;triangles[t++]=a+5;
            }
            mesh=new Mesh{name="sign plate "+key,vertices=vertices,colors32=colors,triangles=triangles};mesh.RecalculateBounds();
            plates[key]=mesh;return mesh;
        }
    }
}
