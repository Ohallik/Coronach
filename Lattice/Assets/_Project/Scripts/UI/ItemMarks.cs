using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>What kind of thing a list row holds, as a mark at its left: a part's slot by its own picture (the item sheet has one for each), and a consumable or a material by a
    /// line drawn here, in the chart's thin bright line, one for each kind (a heal, Charge, both, a revive, a draught, a meal, a material). The sheet has pictures for two
    /// consumables and six materials of the forty: the Back Table's row of six showed one picture and five blanks, so every consumable and material takes its kind's line.</summary>
    public static class ItemMarks
    {
        public enum Kind{None,Emitter,Edge,Frame,Drive,Module,Heal,Charge,Both,Revive,Draught,Meal,Material}
        public static Kind Of(string id)
        {
            var part=GameCatalog.Find<TechPartDef>(id);if(part!=null)return Of(part.slot);
            var item=GameCatalog.Find<ConsumableDef>(id);if(item!=null)return Of(item);
            return GameCatalog.Find<MaterialDef>(id)!=null?Kind.Material:Kind.None;
        }
        public static Kind Of(GearSlot slot)=>slot switch{GearSlot.Emitter=>Kind.Emitter,GearSlot.Edge=>Kind.Edge,GearSlot.Frame=>Kind.Frame,GearSlot.Drive=>Kind.Drive,_=>Kind.Module};
        public static Kind Of(ConsumableDef item)
        {
            if(item==null)return Kind.None;
            if(item.revive)return Kind.Revive;
            if(item.seconds>0)return item.meal?Kind.Meal:Kind.Draught;
            bool heals=item.heal>0||item.healPercent>0;
            return heals&&item.charge>0?Kind.Both:item.charge>0?Kind.Charge:Kind.Heal;
        }
        /// <summary>The mark's picture and the tint it's drawn in. A part's picture is drawn as it is.</summary>
        public static (Sprite sprite,Color tint) For(Kind kind)=>kind switch
        {
            Kind.Emitter=>(UiSkin.Icon("Emitter"),Color.white),Kind.Edge=>(UiSkin.Icon("Edge"),Color.white),Kind.Frame=>(UiSkin.Icon("Frame"),Color.white),
            Kind.Drive=>(UiSkin.Icon("Drive"),Color.white),Kind.Module=>(UiSkin.Icon("Module"),Color.white),
            Kind.Heal=>(Line("mark-heal",Heal),new Color(.55f,.93f,.86f)),Kind.Charge=>(Line("mark-charge",Charge),new Color(.78f,.66f,1f)),
            Kind.Both=>(Line("mark-both",Both),new Color(.62f,.9f,1f)),Kind.Revive=>(Line("mark-revive",Revive),new Color(1f,.72f,.6f)),
            Kind.Draught=>(Line("mark-draught",Draught),new Color(.6f,.86f,1f)),Kind.Meal=>(Line("mark-meal",Meal),new Color(1f,.8f,.5f)),
            Kind.Material=>(Line("mark-material",Material),new Color(.86f,.8f,.66f)),_=>(null,Color.clear)
        };
        /// <summary>Whether the mark is a line drawn here, which stands on a plate of its own, as the sheet's pictures stand on theirs.</summary>
        public static bool Drawn(Kind kind)=>kind>=Kind.Heal;

        // ------------------------------------------------------------ the lines, in -1 to 1
        static readonly Vector2[][] Heal={Ring(.8f),new[]{new Vector2(0,-.42f),new(0,.42f)},new[]{new Vector2(-.42f,0),new(.42f,0)}};
        static readonly Vector2[][] Charge={Ring(.8f),new[]{new Vector2(.16f,.58f),new(-.24f,.02f),new(.14f,.02f),new(-.18f,-.58f)}};
        static readonly Vector2[][] Both={Ring(.8f),Ring(.6f),new[]{new Vector2(0,-.3f),new(0,.3f)},new[]{new Vector2(-.3f,0),new(.3f,0)}};
        static readonly Vector2[][] Revive={Ring(.8f),new[]{new Vector2(0,-.5f),new(0,.46f)},new[]{new Vector2(-.3f,.16f),new(0,.46f),new(.3f,.16f)},new[]{new Vector2(-.38f,-.5f),new(.38f,-.5f)}};
        static readonly Vector2[][] Draught={new[]{new Vector2(-.14f,.72f),new(.14f,.72f)},new[]{new Vector2(-.1f,.72f),new(-.1f,.26f),new(-.5f,-.42f),new(-.4f,-.66f),new(.4f,-.66f),new(.5f,-.42f),new(.1f,.26f),new(.1f,.72f)},new[]{new Vector2(-.34f,-.2f),new(.34f,-.2f)}};
        static readonly Vector2[][] Meal={Arc(new Vector2(0,-.08f),.62f,180,360),new[]{new Vector2(-.74f,-.08f),new(.74f,-.08f)},new[]{new Vector2(-.2f,.12f),new(-.12f,.32f),new(-.2f,.52f)},new[]{new Vector2(.14f,.12f),new(.22f,.32f),new(.14f,.52f)}};
        static readonly Vector2[][] Material={Hex(.74f),new[]{new Vector2(0,.74f),new(0,0),new(.64f,-.37f)},new[]{new Vector2(0,0),new(-.64f,-.37f)}};
        static Vector2[] Ring(float r)=>Arc(Vector2.zero,r,0,360);
        static Vector2[] Arc(Vector2 centre,float r,float from,float to)
        {var points=new List<Vector2>();for(int i=0;i<=32;i++){float a=Mathf.Lerp(from,to,i/32f)*Mathf.Deg2Rad;points.Add(centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}return points.ToArray();}
        static Vector2[] Hex(float r){var points=new Vector2[7];for(int i=0;i<7;i++){float a=(90+60*i)*Mathf.Deg2Rad;points[i]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}return points;}
        static float ToSegment(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude));return (p-(a+ab*t)).magnitude;}

        static readonly Dictionary<string,Sprite> drawn=new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget()=>drawn.Clear();
        /// <summary>Polylines stroked a hair over the chart's line, soft at the edge, white: the row tints it.</summary>
        static Sprite Line(string id,Vector2[][] strokes)
        {
            if(drawn.TryGetValue(id,out var known)&&known!=null)return known;
            const int size=96;const float width=.075f,soft=.05f;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var p=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1);float near=float.MaxValue;
                foreach(var stroke in strokes)for(int i=1;i<stroke.Length;i++)near=Mathf.Min(near,ToSegment(p,stroke[i-1],stroke[i]));
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*Mathf.SmoothStep(1,0,Mathf.InverseLerp(width,width+soft,near))));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=id,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontUnloadUnusedAsset};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=id;sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;
            return drawn[id]=sprite;
        }
    }
}
