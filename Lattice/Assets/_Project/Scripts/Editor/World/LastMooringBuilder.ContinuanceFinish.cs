using System.Collections.Generic;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Continuance finished as the Compact's flagship (D373; the whole-game capture of November 22, s66/021 and s66/153: its decks and walls read as a grey blockout, flat teal beams
    /// and white rectangles for windows). What is here is drawn only and stands nowhere a route walks or a fight is fought: the flank's frames and strakes and its windows set in
    /// frames with depth and a room's light in them, floods on the frames over the spine's head; on the hull's back the working machinery (cabinets with louvred tops, vent stacks,
    /// heat exchangers) where blank boxes stood, skylights in raised coamings with their mullions, the lamp posts' pools of light painted in the plate's own tone; the superstructure's
    /// faces in ribs, cornices and framed window bands; and blast-door leaves for every seal aboard and in the anchor chamber.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        internal static partial class Continuance
        {
            /// <summary>A lit room's window (warm, under the bloom's threshold: at LampWarm's 1.7 every window was a white rectangle), dark glass with a sheen, the frames' iron, a pool of
            /// lamplight laid on the plate.</summary>
            internal static Material WindowMat,GlassMat,IronMat,PoolMat;
            static void FinishMaterials()
            {
                WindowMat=ZoneKit.Toon("continuance-window",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(1,.9f,.72f));m.SetColor("_EmissionColor",new Color(.9f,.58f,.28f));m.SetFloat("_LightFloor",.6f);});
                GlassMat=ZoneKit.Toon("continuance-glass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.6f,.75f));m.SetColor("_RimColor",new Color(.44f,.64f,.74f));
                    m.SetFloat("_RimPower",2.2f);m.SetFloat("_LightFloor",.3f);m.SetColor("_EmissionColor",new Color(.02f,.05f,.07f));});
                IronMat=ZoneKit.Toon("continuance-iron",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.42f,.46f,.58f));m.SetColor("_RimColor",new Color(.56f,.62f,.68f));m.SetFloat("_RimPower",3);m.SetFloat("_LightFloor",.24f);});
                PoolMat=ZoneKit.Toon("continuance-pool",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(.1f,.08f,.05f));m.SetFloat("_LightFloor",.5f);});
            }
            static readonly Color FrameIron=new(.26f,.3f,.33f),FrameLit=new(.62f,.67f,.7f),RoomDark=new(.2f,.17f,.15f),GlassTone=new(.15f,.21f,.27f);

            /// <summary>A window in a wall that faces <paramref name="out_"/> (horizontal): a frame of four bars standing proud of the plate so the pane sits back in it, a sill and a hood that
            /// catch the light, a mullion; lit, a room's warm light with its ceiling strip bright at the head and the dark of what stands inside along the foot; dark, glass with a sheen.</summary>
            internal static void Window(ZoneKit.Shape frames,ZoneKit.Shape lit,ZoneKit.Shape glass,ZoneKit.Shape hoods,Vector3 at,Vector3 out_,float wide,float tall,bool on,Color plate)
            {
                var turn=Quaternion.LookRotation(-out_,Vector3.up);var right=turn*Vector3.right;
                Vector3 O(float x,float y,float d)=>at+right*x+Vector3.up*y+out_*d;
                float hw=wide*.5f,ht=tall*.5f;
                foreach(int side in new[]{-1,1})frames.Slot(0).Box(O(side*(hw+.06f),0,.1f),new Vector3(.13f,tall+.24f,.2f),turn,FrameIron,FrameLit);
                frames.Slot(0).Box(O(0,ht+.06f,.1f),new Vector3(wide+.24f,.13f,.2f),turn,FrameIron,FrameLit);
                frames.Slot(0).Box(O(0,-ht-.07f,.14f),new Vector3(wide+.4f,.1f,.3f),turn,FrameIron,new Color(.78f,.8f,.82f));
                hoods.Slot(0).Box(O(0,ht+.2f,.2f),new Vector3(wide+.5f,.07f,.42f),turn,ZoneKit.Shape.Dim(plate,.82f),plate);
                frames.Slot(0).Box(O(0,0,.05f),new Vector3(.05f,tall,.04f),turn,FrameIron);
                if(on)
                {
                    lit.Slot(0).Box(O(0,0,.02f),new Vector3(wide,tall,.03f),turn,Color.white);
                    lit.Slot(1).Box(O(0,ht-.07f,.035f),new Vector3(wide-.12f,.06f,.03f),turn,Color.white);
                    frames.Slot(0).Box(O(0,-ht+.1f,.035f),new Vector3(wide,.2f,.02f),turn,RoomDark);
                }
                else glass.Slot(0).Box(O(0,0,.02f),new Vector3(wide,tall,.03f),turn,GlassTone);
            }

            /// <summary>A blank box on the hull's back made into the machinery a working deck carries, by its kind: a cabinet with a louvred top, its access panel and a lit status
            /// strip; a vent stack with its grille and band; a heat exchanger lying in its saddles, finned. Nothing over 1.3 m: the ships fly three metres over the plate.</summary>
            static void Housing(ZoneKit.Shape body,ZoneKit.Shape trim,ZoneKit.Shape lamps,Vector2 p,int i)
            {
                var foot=W(p.x,HullTop,p.y);var turn=Quaternion.Euler(0,(i%2)*90,0);var x=turn*Vector3.right;var zf=turn*Vector3.forward;
                var case_=new Color(.74f,.77f,.8f);var caseTop=new Color(.86f,.88f,.9f);var dark=new Color(.16f,.19f,.22f);
                switch((i/3)%3)
                {
                    case 0:
                        body.Slot(0).Box(foot+Vector3.up*.06f,new Vector3(2.7f,.12f,1.7f),turn,dark,new Color(.3f,.34f,.38f));
                        body.Slot(0).Box(foot+Vector3.up*.52f,new Vector3(2.3f,.8f,1.3f),turn,case_,caseTop);
                        for(int k=0;k<6;k++)body.Slot(0).Box(foot+Vector3.up*.93f+x*(-.85f+k*.34f),new Vector3(.16f,.04f,1.05f),turn,dark);
                        trim.Slot(0).Box(foot+Vector3.up*.5f-zf*.66f+x*-.5f,new Vector3(.8f,.5f,.03f),turn,Teal);
                        foreach(float sx in new[]{-1.16f,1.16f})body.Slot(0).Box(foot+Vector3.up*.52f+x*sx,new Vector3(.06f,.82f,1.34f),turn,new Color(.5f,.55f,.6f));
                        lamps.Slot(1).Box(foot+Vector3.up*.66f-zf*.67f+x*.55f,new Vector3(.6f,.07f,.03f),turn,Color.white);
                        break;
                    case 1:
                        body.Slot(0).Box(foot+Vector3.up*.05f,new Vector3(1.9f,.1f,1.9f),turn,dark,new Color(.3f,.34f,.38f));
                        body.Slot(0).Post(foot+Vector3.up*.1f,.95f,.62f,.56f,16,case_,caseTop);
                        body.Slot(0).Disc(foot+Vector3.up*1.055f,Vector3.up,.5f,16,dark);
                        for(int k=0;k<4;k++)body.Slot(0).Box(foot+Vector3.up*1.07f,new Vector3(1f,.03f,.05f),Quaternion.Euler(0,k*45,0),new Color(.42f,.46f,.5f));
                        trim.Slot(0).Post(foot+Vector3.up*.62f,.14f,.6f,.6f,16,Teal,Teal);
                        lamps.Slot(1).Box(foot+Vector3.up*.9f-zf*.6f,new Vector3(.2f,.12f,.04f),turn,Color.white);
                        break;
                    default:
                        foreach(float sx in new[]{-.8f,.8f})body.Slot(0).Box(foot+Vector3.up*.22f+x*sx,new Vector3(.24f,.44f,1f),turn,dark,new Color(.34f,.38f,.42f));
                        body.Slot(0).Tube(new[]{foot+Vector3.up*.6f-x*1.3f,foot+Vector3.up*.6f+x*1.3f},new[]{.4f,.4f},14,case_,caseTop,1,.25f);
                        for(int k=0;k<7;k++){var c=foot+Vector3.up*.6f+x*(-1.05f+k*.35f);body.Slot(0).Tube(new[]{c-x*.03f,c+x*.03f},new[]{.5f,.5f},14,new Color(.42f,.47f,.52f),new Color(.42f,.47f,.52f),1,.25f);}
                        trim.Slot(0).Tube(new[]{foot+Vector3.up*.6f-x*1.36f,foot+Vector3.up*.6f-x*1.3f},new[]{.42f,.42f},14,Teal,Teal,1,.25f);
                        body.Slot(0).Tube(new[]{foot+Vector3.up*.6f+x*1.3f,foot+Vector3.up*.6f+x*1.7f,foot+Vector3.up*.08f+x*1.9f},new[]{.12f,.12f,.12f},8,dark,dark,1,.25f);
                        break;
                }
            }

            /// <summary>A lamp's light on the plate under it, in the cel look's steps: three rings, each the plate's own tone lifted a step and warmed a little (a darker pool on a white
            /// plate was paint, not light; a pool lerped toward the lamp's white read as a tan disc on the dark deck).</summary>
            static void Pool(ZoneKit.Shape pools,Vector3 lamp,Color plate,float reach)
            {
                var foot=new Vector3(lamp.x,HullTop+.012f,lamp.z);
                for(int k=0;k<3;k++)
                {
                    float r=reach*(1-k*.3f),lift=1.12f+k*.1f;
                    pools.Slot(0).Disc(foot+Vector3.up*(k*.004f),Vector3.up,r,28,new Color(Mathf.Min(1,plate.r*lift*1.04f+.01f),Mathf.Min(1,plate.g*lift+.008f),Mathf.Min(1,plate.b*lift*.94f)));
                }
            }

            /// <summary>The superstructure's south faces as a ship's: a frame rib every three and a half metres, a cornice under each tier's head, and its window band framed, a room lit
            /// behind two windows in three; a dish and two masts with their beacons on its crown.</summary>
            static void SuperstructureFinish(ZoneKit.Shape sup,ZoneKit.Shape frames,ZoneKit.Shape lit,ZoneKit.Shape glass,(float x0,float x1,float y0,float y1,float z0)[] tiers)
            {
                foreach(var (x0,x1,y0,y1,z0) in tiers)
                {
                    float face=z0-.03f;
                    for(float x=x0+.3f;x<=x1-.29f;x+=3.5f)sup.Slot(0).Box(W(x,(y0+y1)*.5f,face-.18f),new Vector3(.5f,y1-y0,.36f),Quaternion.identity,ZoneKit.Shape.Dim(Pale,.88f),White);
                    sup.Slot(0).Box(W((x0+x1)*.5f,y1-.3f,face-.3f),new Vector3(x1-x0+.4f,.36f,.6f),Quaternion.identity,ZoneKit.Shape.Dim(Pale,.8f),White);
                    sup.Slot(0).Box(W((x0+x1)*.5f,y0+.9f,face-.2f),new Vector3(x1-x0,.24f,.4f),Quaternion.identity,ZoneKit.Shape.Dim(Pale,.82f),White);
                    int n=0;
                    for(float x=x0+2.05f;x<x1-1.6f;x+=3.5f,n++)
                    {
                        float wy=(y0+y1)*.5f+.2f;bool on=H(Mathf.RoundToInt(x*3),Mathf.RoundToInt(y0))<.66f;
                        Window(frames,lit,glass,sup,W(x,wy,face),Vector3.back,2.2f,1.1f,on,Pale);
                    }
                }
            }

            /// <summary>A door leaf's two faces as a blast door's: a raised frame round two panels, the Compact's hazard band (teal and white) along its foot, the lit seam where the leaves
            /// meet and an amber lamp over it. <paramref name="s"/> slot 0 is the leaf's white, slot 1 its teal; <paramref name="lit"/> slot 0 the teal light, slot 1 the warm.</summary>
            internal static void SealLeaf(ZoneKit.Shape s,ZoneKit.Shape lit,Vector3 foot,Quaternion turn,int side,float width,float tall)
            {
                var right=turn*Vector3.right;var back=turn*Vector3.back;float lw=width*.5f-.06f;var c=foot+right*side*width*.25f;
                var pale=new Color(.84f,.87f,.89f);var rail=new Color(.66f,.7f,.74f);var inset=new Color(.76f,.8f,.83f);var teal=new Color(.18f,.56f,.58f);var white=new Color(.95f,.96f,.97f);
                foreach(int f in new[]{-1,1})
                {
                    var o=back*f*.12f;var d=back*f*.015f;
                    foreach(int e in new[]{-1,1})s.Slot(0).Box(c+o+d+right*e*(lw*.5f-.09f)+Vector3.up*tall*.5f,new Vector3(.14f,tall-.2f,.04f),turn,rail,white);
                    foreach(float y in new[]{.12f,tall*.52f,tall-.12f})s.Slot(0).Box(c+o+d+Vector3.up*y,new Vector3(lw-.04f,.14f,.04f),turn,rail,white);
                    foreach(var (y0,y1) in new[]{(.8f,tall*.52f-.12f),(tall*.52f+.12f,tall-.22f)})if(y1-y0>.2f)s.Slot(0).Box(c+o+Vector3.up*(y0+y1)*.5f,new Vector3(lw-.36f,y1-y0,.02f),turn,inset,pale);
                    for(float x=-lw*.5f+.24f;x<lw*.5f-.2f;x+=.3f){var band=c+o+d*1.3f+right*x+Vector3.up*.46f;s.Slot(1).Box(band,new Vector3(.12f,.34f,.03f),turn*Quaternion.Euler(0,0,32),teal);}
                    s.Slot(0).Box(c+o+d+Vector3.up*.46f,new Vector3(lw-.24f,.42f,.02f),turn,white);
                    lit.Slot(0).Box(foot+o+d*1.4f+right*side*.04f+Vector3.up*tall*.5f,new Vector3(.05f,tall-.6f,.03f),turn,Color.white);
                    lit.Slot(1).Box(foot+o+d*1.4f+right*side*.2f+Vector3.up*(tall-.3f),new Vector3(.22f,.12f,.04f),turn,Color.white);
                }
            }
        }
    }
}
