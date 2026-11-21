using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>What Many Hands is made of: every texture painted here in code (nothing sourced, nothing ordered), near neutral and tinted in the vertices, so one plate covers every
    /// hull and one board every bench. Painted here and not borrowed from another region's builder, so nothing aboard changes when Driftreed's or Rootmarket's is retuned.</summary>
    public static partial class ManyHandsBuilder
    {
        internal static partial class Hands
        {
            internal static Material Plate,HullPlate,Panel,Corrugated,Wood,Cloth,Leaf,Soil,Weave,Lamp,Brass,Chalk,Glass,Water,Rope,Dark;
            internal static void Materials()
            {
                // The hauler's deck: two-metre plates, each its own age and tone, welded seams, rivet rows a hand in from each edge, and tread plate on one in four. 1024 texels to eight
                // metres: a hundred and twenty to the metre, as dense as the frame shows.
                var plate=ZoneKit.Texture("ManyHands/hands-plate",1024,(u,v)=>
                {
                    float x=u*4,y=v*4;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx,fy=y-cy;
                    // Every other row is offset by half a plate, as plating is laid.
                    if(cy%2==1){x+=.5f;cx=Mathf.FloorToInt(x);fx=x-cx;}
                    float own=ZoneKit.Tile(cx*3.1f+cy*7.7f,.5f,16,13),edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    float tone=.56f+.14f*(own-.5f)+.06f*(ZoneKit.Cloud(u,v,8,5)-.5f)+.04f*(ZoneKit.Tile(u*256,v*256,256,3)-.5f);
                    bool tread=((cx*7+cy*3)&3)==0;
                    if(tread&&edge>.06f){float dx=Mathf.Repeat(fx*14+fy*14,1),dy=Mathf.Repeat(fx*14-fy*14,1);bool lug=(Mathf.Abs(dx-.5f)<.16f&&Mathf.Abs(dy-.5f)<.42f)||(Mathf.Abs(dy-.5f)<.16f&&Mathf.Abs(dx-.5f)<.42f);tone+=lug?.08f:-.03f;}
                    if(edge<.012f)tone-=.26f;else if(edge<.022f)tone+=.05f;
                    // Rivets: a row of them along each edge.
                    float along=Mathf.Abs(fx-.5f)>Mathf.Abs(fy-.5f)?fy:fx,across=Mathf.Min(Mathf.Abs(fx-.5f)>Mathf.Abs(fy-.5f)?Mathf.Min(fx,1-fx):Mathf.Min(fy,1-fy),1);
                    if(Mathf.Abs(across-.05f)<.012f&&Mathf.Abs(Mathf.Repeat(along*10,1)-.5f)<.12f)tone+=.14f;
                    // Scuffs and old paint, a few streaks along the way trolleys go.
                    float scuff=ZoneKit.Cloud(u*2,v*2,12,21);if(scuff>.72f)tone-=.07f;
                    return new Color(tone,tone*.99f,tone*.96f,1);
                });
                // A hull's side: courses of plate a metre and a quarter tall laid level (the landform lays a wall's v by height), the seams staggered, rivet lines at each course, rust
                // run down from a few of them.
                var hull=ZoneKit.Texture("ManyHands/hands-hull",512,(u,v)=>
                {
                    float row=v*4;int r=Mathf.FloorToInt(row);float fy=row-r,x=u*3+(r%2)*.5f;int c=Mathf.FloorToInt(x);float fx=x-c;
                    float own=ZoneKit.Tile(c*2.3f+r*5.1f,.5f,16,9),tone=.52f+.16f*(own-.5f)+.05f*(ZoneKit.Cloud(u,v,6,4)-.5f);
                    float seam=Mathf.Min(Mathf.Min(fx,1-fx)*3,Mathf.Min(fy,1-fy));
                    if(seam<.025f)tone-=.24f;else if(fy>.08f&&fy<.11f&&Mathf.Abs(Mathf.Repeat(fx*12,1)-.5f)<.2f)tone+=.12f;
                    float run=ZoneKit.Tile(u*40,.5f,40,7);if(run>.82f&&fy<.8f)tone-=.1f*(1-fy)*(run-.82f)*5;
                    return new Color(tone,tone,tone*.97f,1);
                });
                // A fitted panel: what the liner's columns, the counters and the cabins' fronts are clad in. Four by four panels with a bead round each and a screw at each corner.
                var panel=ZoneKit.Texture("ManyHands/hands-panel",512,(u,v)=>
                {
                    float x=u*4,y=v*4;float fx=x-Mathf.Floor(x),fy=y-Mathf.Floor(y),edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    float tone=.66f+.05f*(ZoneKit.Cloud(u,v,8,2)-.5f)+.03f*(ZoneKit.Tile(u*128,v*128,128,6)-.5f);
                    if(edge<.02f)tone-=.22f;else if(edge<.05f)tone+=.06f;
                    if(Mathf.Abs(fx-.1f)<.025f&&Mathf.Abs(fy-.1f)<.025f||Mathf.Abs(fx-.9f)<.025f&&Mathf.Abs(fy-.9f)<.025f)tone-=.18f;
                    return new Color(tone,tone,tone,1);
                });
                // Corrugated container steel: ribs running up the side, the ribs' crests catching light, rust at the foot and round the doors' bars.
                var corrugated=ZoneKit.Texture("ManyHands/hands-corrugated",512,(u,v)=>
                {
                    float rib=Mathf.Abs(Mathf.Sin(u*Mathf.PI*16)),bar=Mathf.Abs(Mathf.Repeat(u*2,1)-.5f);
                    float tone=.5f+.24f*Mathf.Pow(rib,.6f)+.06f*(ZoneKit.Cloud(u,v,6,8)-.5f);
                    if(v<.12f)tone-=.14f*(1-v/.12f)*ZoneKit.Cloud(u*3,v,9,3);
                    if(Mathf.Abs(v-.06f)<.012f||Mathf.Abs(v-.94f)<.012f)tone-=.2f;
                    if(bar<.012f)tone-=.16f;
                    return new Color(tone,tone,tone,1);
                });
                var wood=ZoneKit.Texture("ManyHands/hands-wood",512,(u,v)=>
                {
                    float row=v*6;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                    float own=ZoneKit.Tile(board*3.7f,.5f,6,41),grain=ZoneKit.Tile(u*8+board*2.3f,v*90,90,6),end=Mathf.Abs(Mathf.Repeat(u+own*.63f,1)-.5f);
                    float tone=.62f+.18f*(own-.5f)+.1f*(grain-.5f)-(seam<.06f?.3f:0)-(end<.006f?.25f:0);
                    return new Color(tone*1.03f,tone,tone*.9f,1);
                });
                var cloth=ZoneKit.Texture("ManyHands/hands-cloth",256,(u,v)=>
                {
                    float warp=Mathf.Abs(Mathf.Sin(u*Mathf.PI*64)),weft=Mathf.Abs(Mathf.Sin(v*Mathf.PI*64));
                    float tone=.74f+.08f*(warp*weft-.25f)+.08f*(ZoneKit.Cloud(u,v,6,2)-.5f);return new Color(tone,tone,tone,1);
                });
                var leaf=ZoneKit.Texture("ManyHands/hands-leaf",256,(u,v)=>
                {
                    float clump=ZoneKit.Cloud(u,v,7,23),fine=ZoneKit.Cloud(u,v,20,9),vein=Mathf.Abs(u-.5f)<.035f?.1f:0;
                    float tone=.6f+.3f*(clump-.5f)+.16f*(fine-.5f)+vein;return new Color(tone,tone*1.02f,tone*.98f,1);
                });
                var soil=ZoneKit.Texture("ManyHands/hands-soil",512,(u,v)=>
                {
                    float big=ZoneKit.Cloud(u,v,5,13),fine=ZoneKit.Cloud(u,v,24,3),fleck=ZoneKit.Tile(u*96,v*96,96,11);
                    float row=Mathf.Abs(Mathf.Sin(v*Mathf.PI*10));
                    float tone=.6f+.2f*(big-.5f)+.18f*(fine-.5f)+.1f*row+(fleck>.9f?.12f:fleck<.07f?-.14f:0);return new Color(tone,tone*.95f,tone*.88f,1);
                });
                var weave=ZoneKit.Texture("ManyHands/hands-weave",512,(u,v)=>
                {
                    float x=u*12,y=v*12;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx-.5f,fy=y-cy-.5f;bool over=(cx+cy)%2==0;
                    float strand=over?1-Mathf.Abs(fy)*1.7f:1-Mathf.Abs(fx)*1.7f,edge=over?Mathf.Abs(fx):Mathf.Abs(fy);
                    float tone=.4f+.42f*Mathf.Clamp01(strand)-(edge>.44f?.16f:0)+.1f*(ZoneKit.Cloud(u,v,30,7)-.5f);return new Color(tone*1.04f,tone,tone*.88f,1);
                });
                // The ground kit's own layers for Many Hands (GroundKit reads ground-<name> from its folder): the liner's cream tile in the arcade and galley, boards in the garden and on the rafts.
                ZoneKit.Texture("Ground/ground-hands-liner",512,(u,v)=>
                {
                    // An old liner's promenade: cream slabs, each its own age, a small dark diamond set where four meet, and every fourth course a band of
                    // small mosaic in three tones. The first pass was a two-tone check, and read as a kitchen floor.
                    float x=u*4,y=v*4;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx,fy=y-cy,edge=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                    float tone=.84f+.05f*(ZoneKit.Tile(cx*1.7f,cy*2.3f,64,17)-.5f)+.04f*(ZoneKit.Cloud(u,v,6,9)-.5f);
                    if(cy%4==0&&fy>.18f&&fy<.82f){float mx=x*10,my=y*10;int tx=Mathf.FloorToInt(mx),ty=Mathf.FloorToInt(my);float t=ZoneKit.Tile(tx*1.3f,ty*2.1f,64,23);tone=t<.33f?.5f:t<.66f?.66f:.78f;
                        if(Mathf.Min(mx-tx,1-(mx-tx))<.08f||Mathf.Min(my-ty,1-(my-ty))<.08f)tone-=.12f;}
                    float cornerX=Mathf.Min(fx,1-fx),cornerY=Mathf.Min(fy,1-fy);if(cornerX+cornerY<.16f)tone=.36f;
                    if(edge<.012f)tone-=.16f;
                    if(ZoneKit.Tile(u*160,v*160,160,2)>.94f)tone-=.05f;
                    return new Color(tone,tone,tone,1);
                });
                ZoneKit.Texture("Ground/ground-hands-boards",512,(u,v)=>
                {
                    float row=u*8;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                    float own=ZoneKit.Tile(board*3.7f,.5f,8,31),grain=ZoneKit.Tile(u*120,v*12+board*2.3f,120,6),end=Mathf.Abs(Mathf.Repeat(v*2+own*.71f,1)-.5f);
                    float tone=.7f+.14f*(own-.5f)+.1f*(grain-.5f)-(seam<.05f?.26f:0)-(end<.01f?.22f:0);
                    if(Mathf.Abs(Mathf.Repeat(v*2+own*.71f+.5f,1)-.5f)<.02f&&Mathf.Abs(across-.5f)<.2f)tone-=.1f;
                    return new Color(tone,tone,tone,1);
                });
                Plate=ZoneKit.Toon("hands-plate",plate,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.52f,.66f));});
                HullPlate=ZoneKit.Toon("hands-hull",hull,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.72f,.74f,.78f));m.SetColor("_ShadowTint",new Color(.46f,.48f,.62f));});
                Panel=ZoneKit.Toon("hands-panel",panel,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.52f,.66f));});
                Corrugated=ZoneKit.Toon("hands-corrugated",corrugated,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.48f,.5f,.64f));});
                Wood=ZoneKit.Toon("hands-wood",wood,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.52f,.5f,.66f));});
                Cloth=ZoneKit.Toon("hands-cloth",cloth,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.54f,.52f,.66f));});
                Leaf=ZoneKit.Toon("hands-leaf",leaf,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.03f,1.1f,1.2f);m.SetColor("_ShadowTint",new Color(.44f,.52f,.58f));});
                Soil=ZoneKit.Toon("hands-soil",soil,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
                Weave=ZoneKit.Toon("hands-weave",weave,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.86f,.76f,.54f));m.SetColor("_ShadowTint",new Color(.5f,.5f,.64f));});
                Lamp=ZoneKit.Toon("hands-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.84f,.56f));m.SetColor("_EmissionColor",new Color(2.4f,1.6f,.7f));});
                Brass=ZoneKit.Toon("hands-brass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.74f,.42f));m.SetColor("_RimColor",new Color(1,.9f,.6f));m.SetFloat("_RampSteps",5);});
                Chalk=ZoneKit.Toon("hands-chalk",null,m=>m.SetColor("_BaseColor",new Color(.94f,.94f,.9f)),"Universal Render Pipeline/Unlit");
                Rope=ZoneKit.Toon("hands-rope",null,m=>m.SetColor("_BaseColor",new Color(.8f,.72f,.54f)),"Universal Render Pipeline/Unlit");
                Dark=ZoneKit.Toon("hands-dark",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.3f,.32f,.36f));});
                // Glass: a pale sheen over what's behind it, drawn as light, so the garden's panes and the galley's windows read as glass and never as a grey slab.
                Glass=ZoneKit.Toon("hands-glass",null,m=>{m.SetColor("_BaseColor",new Color(.62f,.8f,.9f,1));m.SetColor("_EmissionColor",new Color(.16f,.24f,.3f));},"Universal Render Pipeline/Unlit");
                // The hold's and the channel's water: green-dark under the rafts, glints kept small (chapter 3: a large ripple read as slabs of white), nearly opaque past a metre so the
                // bed doesn't show through as blotches (Driftreed's lesson).
                Water=ZoneKit.Toon("hands-water",null,m=>ZoneKit.WaterLook(m,new Color(.32f,.46f,.44f,.86f),new Color(.1f,.17f,.18f,.97f),new Color(.78f,.86f,.84f,1),1.1f,3.2f,.07f,.3f));
            }
        }
    }
}
