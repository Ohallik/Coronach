using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Many Hands' districts, each its own make and its own landmark (docs/maps/ManyHands.md, "Plan").</summary>
    public static partial class ManyHandsBuilder
    {
        internal static partial class Hands
        {
            // ---------------------------------------------------------------- shared pieces
            static Vector3 At(Vector2 p,float up=0)=>Kit.On(p.x,p.y,up);
            static Vector3 At(float x,float z,float up=0)=>Kit.On(x,z,up);
            static BoxCollider Solid(GameObject parent,Vector2 p,Vector3 size,float yaw=0,string name="Block",float lift=0)=>ZoneKit.Block(parent,At(p,lift+size.y*.5f),size,yaw,name);
            /// <summary>Nothing solid already stands here at a body's height: what is scattered goes on open floor, never onto a bed, a counter or a stall built before it.</summary>
            static bool Clear(Vector2 p){Physics.SyncTransforms();return !Physics.CheckSphere(At(p,1),.9f,~0,QueryTriggerInteraction.Ignore);}
            static GameObject Shade(GameObject go,bool casts){go.GetComponent<MeshRenderer>().shadowCastingMode=casts?ShadowCastingMode.On:ShadowCastingMode.Off;return go;}
            /// <summary>A dozen paints a convoy's hulls and boxes come in: hauler rust, ochre, slate, liner cream and teal, station white, carrier green and clay, navy, brick, olive, sand.</summary>
            static readonly Color[] Paints={new(.66f,.32f,.24f),new(.82f,.62f,.3f),new(.44f,.5f,.56f),new(.9f,.86f,.74f),new(.24f,.56f,.58f),new(.84f,.86f,.84f),new(.46f,.6f,.38f),
                new(.7f,.5f,.36f),new(.26f,.34f,.52f),new(.6f,.3f,.28f),new(.5f,.54f,.32f),new(.86f,.76f,.56f)};
            static readonly Color Rib=new(.5f,.42f,.32f),Hazard=new(.94f,.74f,.18f),Soot=new(.16f,.16f,.18f),Cream=new(.94f,.9f,.8f),Teal=new(.22f,.54f,.56f),Bronze=new(.66f,.48f,.28f),Straw=new(.88f,.78f,.54f);
            static readonly Color[] Dyes={new(.5f,.66f,.86f),new(.86f,.56f,.36f),new(.62f,.8f,.5f),new(.9f,.8f,.5f),new(.7f,.5f,.8f),new(.86f,.44f,.4f)};

            /// <summary>A shipping container: ribbed steel in its own paint, the door end barred, a darker foot. Stacked by the caller.</summary>
            static void Container(ZoneKit.Shape s,Vector3 foot,float yaw,Color paint,float length=6.1f)
            {
                var t=Quaternion.Euler(0,yaw,0);
                s.Slot(0).Box(foot+Vector3.up*1.3f,new Vector3(2.44f,2.6f,length),t,paint,ZoneKit.Shape.Dim(paint,1.1f),.36f);
                for(int k=0;k<4;k++)s.Slot(1).Box(foot+t*new Vector3(-.85f+k*.57f,1.3f,length*.5f+.04f),new Vector3(.07f,2.4f,.07f),t,Soot);
                s.Slot(1).Box(foot+t*new Vector3(0,.08f,0),new Vector3(2.5f,.16f,length+.06f),t,Soot);
            }
            /// <summary>A rail along an edge the party can reach: posts, two rails and a kick plate in hazard stripes, on a block a body stops at. Never invisible.</summary>
            static void Rail(string name,Vector3 a,Vector3 b,Color paint)
            {
                var s=new ZoneKit.Shape();float length=Vector3.Distance(a,b);int posts=Mathf.Max(1,Mathf.RoundToInt(length/2.4f));var run=(b-a).normalized;var across=Vector3.Cross(Vector3.up,run);
                for(int i=0;i<=posts;i++){var p=Vector3.Lerp(a,b,i/(float)posts);s.Slot(0).Post(p,1.16f,.055f,.045f,6,ZoneKit.Shape.Dim(paint,.8f),paint);}
                s.Slot(0).Beam(a+Vector3.up*1.14f,b+Vector3.up*1.14f,.09f,.07f,paint).Beam(a+Vector3.up*.62f,b+Vector3.up*.62f,.05f,.05f,ZoneKit.Shape.Dim(paint,.85f));
                int stripes=Mathf.Max(1,Mathf.RoundToInt(length/.5f));
                for(int i=0;i<stripes;i++)
                {
                    var p=a+run*(length*i/stripes);var q=a+run*(length*(i+1)/stripes);
                    s.Slot(1).Quad(p-across*.04f,q-across*.04f,q-across*.04f+Vector3.up*.2f,p-across*.04f+Vector3.up*.2f,-across,i%2==0?Hazard:Soot,.5f).Quad(p+across*.04f,q+across*.04f,q+across*.04f+Vector3.up*.2f,p+across*.04f+Vector3.up*.2f,across,i%2==0?Hazard:Soot,.5f);
                }
                var go=Kit.Built(name,s,Panel,Panel);ZoneKit.BlockAlong(go,a+Vector3.up*.62f,b+Vector3.up*.62f,.3f,1.24f,name);
            }
            /// <summary>A lamp on a post, its head lit; a point light on some, so no floor takes more than its share.</summary>
            static GameObject LampPost(string name,Vector2 at,float tall,Color pole,bool light,float range=8,float intensity=1.1f,float lift=0)
            {
                var foot=At(at,lift);var s=new ZoneKit.Shape().Slot(0).Post(foot,tall,.08f,.06f,8,ZoneKit.Shape.Dim(pole,.7f),pole).Beam(foot+new Vector3(0,tall-.1f,0),foot+new Vector3(.55f,tall-.1f,0),.06f,.06f,pole)
                    .Slot(1).Lump(foot+new Vector3(.55f,tall-.38f,0),new Vector3(.16f,.22f,.16f),3,.04f,Color.white,Color.white,8,5);
                var go=Kit.Built(name,s,Brass,Lamp);Solid(go,at,new Vector3(.3f,2.2f,.3f),0,"Lamp post",lift);
                if(light)Kit.Lamp(new Vector3(at.x+.55f,lift+tall-.5f,at.y),range,intensity,new Color(1,.8f,.52f));
                return go;
            }
            /// <summary>A stall: a counter on posts, a striped awning pitched to the back, goods on the counter. Its own small mesh, so the ground kit keeps its details off it.</summary>
            static GameObject Stall(string name,Vector2 at,float yaw,Color awning,Color edge,Color goods,Material stuff)
            {
                var s=new ZoneKit.Shape();var foot=At(at);var t=Quaternion.Euler(0,yaw,0);Color wood=new(.82f,.66f,.46f),dark=new(.5f,.4f,.3f);
                s.Slot(0).Box(foot+t*new Vector3(0,.52f,0),new Vector3(3.4f,.94f,1),t,dark,wood).Box(foot+t*new Vector3(0,1.02f,0),new Vector3(3.7f,.08f,1.25f),t,wood);
                foreach(var (x,z,tall) in new[]{(-1.8f,-.7f,2.3f),(1.8f,-.7f,2.3f),(-1.8f,1.1f,3f),(1.8f,1.1f,3f)})s.Slot(0).Post(foot+t*new Vector3(x,0,z),tall,.07f,.06f,6,dark,wood);
                Vector3 A=foot+t*new Vector3(-2.05f,3.05f,1.2f),B=foot+t*new Vector3(2.05f,3.05f,1.2f),C=foot+t*new Vector3(2.05f,2.2f,-1.05f),D=foot+t*new Vector3(-2.05f,2.2f,-1.05f);
                // Stripes across the awning, the convoy's way of telling one stall from the next at a glance.
                for(int k=0;k<6;k++){float a0=k/6f,a1=(k+1)/6f;s.Slot(1).Quad(Vector3.Lerp(A,B,a0),Vector3.Lerp(A,B,a1),Vector3.Lerp(D,C,a1),Vector3.Lerp(D,C,a0),t*new Vector3(0,1,-.5f),k%2==0?awning:Color.Lerp(awning,Color.white,.55f),.8f);}
                s.Slot(1).Quad(D,C,C+Vector3.down*.34f,D+Vector3.down*.34f,t*Vector3.back,edge,.8f);
                for(int k=0;k<5;k++)s.Slot(2).Lump(foot+t*new Vector3(-1.3f+k*.65f,1.16f,.06f*(k%2)),new Vector3(.18f,.13f,.18f),k+(int)at.x,.15f,Color.Lerp(goods,Color.white,.15f*(k%3)),ZoneKit.Shape.Dim(goods,.55f),7,4);
                var go=Kit.Built(name,s,Wood,Cloth,stuff);Solid(go,at,new Vector3(3.6f,1.1f,1.2f),yaw,"Stall counter");return go;
            }
            /// <summary>A bench of boards on brass legs.</summary>
            static void Bench(ZoneKit.Shape s,Vector3 foot,float yaw)
            {
                var t=Quaternion.Euler(0,yaw,0);Color wood=new(.78f,.6f,.42f);
                s.Slot(0).Box(foot+t*new Vector3(0,.44f,0),new Vector3(1.9f,.07f,.48f),t,wood).Box(foot+t*new Vector3(0,.74f,.24f),new Vector3(1.9f,.32f,.05f),t,wood);
                foreach(float x in new[]{-.8f,.8f})s.Slot(1).Box(foot+t*new Vector3(x,.22f,0),new Vector3(.06f,.44f,.44f),t,Bronze);
            }
            /// <summary>A planter of ferns and fronds, its box in a hull's paint.</summary>
            static void Planter(ZoneKit.Shape s,Vector3 foot,float size,Color box,System.Random rng)
            {
                s.Slot(0).Box(foot+Vector3.up*size*.3f,new Vector3(size,size*.6f,size),Quaternion.Euler(0,(float)rng.NextDouble()*20-10,0),box,ZoneKit.Shape.Dim(box,.6f));
                for(int k=0;k<5;k++)s.Slot(1).Frond(foot+new Vector3(((float)rng.NextDouble()-.5f)*size*.5f,size*.6f,((float)rng.NextDouble()-.5f)*size*.5f),
                    new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f),size*.9f,size*.24f,size*.7f,.4f,new Color(.36f,.56f,.3f),new Color(.6f,.82f,.44f));
            }

            // ---------------------------------------------------------------- the edge of the ship, and what lies beyond it
            static void Edges(ZoneKit kit)
            {
                // Rails along every edge a body can reach (the cold stores' roofs can't be reached), and under each a hull skirt going down out of sight, so the deck has a thickness
                // and the world's edge is the ship's.
                Color rail=new(.62f,.66f,.7f);
                Rail("South rail",new Vector3(XMin+.3f,0,ZMin+.3f),new Vector3(52.6f,0,ZMin+.3f),rail);
                Rail("West rail",new Vector3(XMin+.3f,0,ZMin+.3f),new Vector3(XMin+.3f,0,ZMax-.3f),rail);
                Rail("North rail, cabins",new Vector3(XMin+.3f,0,ZMax-.3f),new Vector3(-24.4f,0,ZMax-.3f),rail);
                Rail("North rail, gallery",new Vector3(-24,GalleryTop,ZMax-.3f),new Vector3(24,GalleryTop,ZMax-.3f),new Color(.86f,.7f,.4f));
                Rail("North rail, garden",new Vector3(24.4f,0,ZMax-.3f),new Vector3(XMax-.3f,0,ZMax-.3f),rail);
                Rail("East rail",new Vector3(XMax-.3f,0,-23.6f),new Vector3(XMax-.3f,0,ZMax-.3f),rail);
                var skirt=new ZoneKit.Shape();
                void Skirt(Vector3 a,Vector3 b,Vector3 outward,Color paint)
                {
                    float length=Vector3.Distance(a,b);int panels=Mathf.Max(1,Mathf.RoundToInt(length/6));
                    for(int i=0;i<panels;i++)
                    {
                        var p=Vector3.Lerp(a,b,i/(float)panels);var q=Vector3.Lerp(a,b,(i+1)/(float)panels);var tone=Color.Lerp(paint,Paints[(i*5)%Paints.Length],.25f);
                        skirt.Slot(0).Quad(p,q,q+Vector3.down*9+outward*-2,p+Vector3.down*9+outward*-2,outward,tone,.25f);
                    }
                }
                Skirt(new Vector3(XMin,0,ZMin),new Vector3(XMax,0,ZMin),Vector3.back,new Color(.6f,.36f,.28f));
                Skirt(new Vector3(XMin,0,ZMin),new Vector3(XMin,0,ZMax),Vector3.left,new Color(.42f,.5f,.56f));
                Skirt(new Vector3(XMax,0,ZMin),new Vector3(XMax,0,ZMax),Vector3.right,new Color(.46f,.58f,.4f));
                Skirt(new Vector3(XMin,0,ZMax),new Vector3(-24,0,ZMax),Vector3.forward,new Color(.5f,.66f,.68f));
                Skirt(new Vector3(-24,GalleryTop,ZMax),new Vector3(24,GalleryTop,ZMax),Vector3.forward,new Color(.5f,.66f,.68f));
                Skirt(new Vector3(24,0,ZMax),new Vector3(XMax,0,ZMax),Vector3.forward,new Color(.46f,.58f,.4f));
                Shade(kit.Built("Hull skirts",skirt,HullPlate),false);
            }
            /// <summary>Space past the rails, built and lit rather than left to the panorama: the moon the convoy hangs over, the settlements' queue of ships holding off to port, ships
            /// coming and going to starboard, the hauler's keel running on under the bow.</summary>
            static void Space(ZoneKit kit)
            {
                WorldArt.Planet("Sorrel",new Vector3(270,-430,760),300);
                // The traffic hangs under the star field's mesh: one drawn thing hundreds of metres across, so nothing takes a ship in space for a prop standing on the deck.
                var traffic=new GameObject("Traffic in space");
                void Ship(string key,Vector3 at,float size,float yaw,Color tint){if(!ZoneKit.Landed(key))return;var ship=kit.Loose(key,at,size,Quaternion.Euler(0,yaw,0),tint,false);ship.transform.SetParent(traffic.transform,true);}
                // The queue to port: settlements' ships, each its own make, holding off in a line.
                Ship("Hauler",new Vector3(-104,-9,-30),9,90,new Color(.86f,.74f,.6f));Ship("Skiff",new Vector3(-96,-6,-6),3.6f,80,new Color(.8f,.9f,1));Ship("PatrolCutter",new Vector3(-100,-8,16),4,95,new Color(.94f,.86f,.8f));
                Ship("Hauler",new Vector3(-110,-12,44),8,100,new Color(.7f,.82f,.74f));Ship("Skiff",new Vector3(-98,-7,70),3.4f,85,new Color(1,.86f,.8f));
                // Off the bow: two of the convoy's own lashed hulls going on, and a skiff.
                Ship("Hauler",new Vector3(-36,-12,82),10,10,new Color(.74f,.78f,.86f));Ship("TallowStationHull",new Vector3(38,-16,96),12,-20,new Color(.86f,.9f,.92f));Ship("Skiff",new Vector3(8,-5,70),3,30,new Color(.9f,.96f,1));
                // To starboard: traffic in and out of the dock.
                Ship("PatrolCutter",new Vector3(96,-7,8),4,-80,new Color(.86f,.92f,.96f));Ship("Skiff",new Vector3(92,-5,34),3.2f,-100,new Color(1,.9f,.82f));Ship("Hauler",new Vector3(110,-14,-40),9,-90,new Color(.82f,.7f,.62f));
                // Lights on the queue's ships: their running lamps, tiny and warm, the way a harbour reads at night.
                var lights=new ZoneKit.Shape();var rng=new System.Random(17);
                foreach(Transform ship in traffic.transform){var box=ZoneKit.Drawn(ship.gameObject);for(int k=0;k<6;k++)lights.Lump(box.center+new Vector3(((float)rng.NextDouble()-.5f)*box.size.x,box.extents.y*.4f,((float)rng.NextDouble()-.5f)*box.size.z),Vector3.one*.18f,k,.02f,Color.white,Color.white,6,4);}
                Shade(kit.Built("Running lamps",lights,Lamp),false);
                // Stars below the deck, crisp points rather than the panorama's soft ones, far enough down that the gallery's rail stands against them.
                var stars=new ZoneKit.Shape();
                for(int k=0;k<1400;k++)
                {
                    float x=(float)rng.NextDouble()*520-260,z=(float)rng.NextDouble()*440-160;if(x>XMin-6&&x<XMax+6&&z>ZMin-6&&z<ZMax+6){continue;}
                    float depth=40+(float)rng.NextDouble()*160,size=.12f+.34f*(float)Math.Pow(rng.NextDouble(),3);var p=new Vector3(x,-depth,z);
                    var tone=Color.Lerp(new Color(1,.94f,.84f),new Color(.78f,.88f,1),(float)rng.NextDouble());
                    stars.Disc(p,Vector3.up+new Vector3(0,0,-.6f),size*(depth/80),5,tone);
                }
                var starView=Shade(kit.Built("Stars below",stars,Lamp),false);traffic.transform.SetParent(starView.transform,true);
            }

            // ---------------------------------------------------------------- the dock, the freight yard and the freight spine
            static void Dock(ZoneKit kit)
            {
                // Landing pads: the party's, back to Tallow's approach; the second, to the hull, from the claim on. Ringed plate with hazard paint and four lamps.
                GameObject Pad(string name,Vector2 at,Color ring)
                {
                    var foot=At(at);var s=new ZoneKit.Shape();
                    s.Slot(0).Disc(foot+Vector3.up*.06f,Vector3.up,3.1f,28,new Color(.42f,.44f,.48f),0,new Color(.34f,.36f,.4f),.5f);
                    s.Slot(1).Disc(foot+Vector3.up*.065f,Vector3.up,2.8f,32,ring,2.5f).Disc(foot+Vector3.up*.065f,Vector3.up,.6f,16,ring);
                    for(int k=0;k<8;k++){float a=k*Mathf.PI/4;s.Slot(1).Quad(foot+new Vector3(Mathf.Cos(a)*1.1f,.066f,Mathf.Sin(a)*1.1f),foot+new Vector3(Mathf.Cos(a+.18f)*1.1f,.066f,Mathf.Sin(a+.18f)*1.1f),
                        foot+new Vector3(Mathf.Cos(a+.18f)*2.3f,.066f,Mathf.Sin(a+.18f)*2.3f),foot+new Vector3(Mathf.Cos(a)*2.3f,.066f,Mathf.Sin(a)*2.3f),Vector3.up,Soot,.5f);}
                    for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+.785f;var q=foot+new Vector3(Mathf.Cos(a)*3.3f,0,Mathf.Sin(a)*3.3f);s.Slot(0).Post(q,.7f,.09f,.07f,6,Soot,new Color(.5f,.52f,.56f)).Slot(2).Lump(q+Vector3.up*.78f,new Vector3(.12f,.14f,.12f),k,.04f,Color.white,Color.white,7,4);}
                    return Shade(kit.Built(name,s,Panel,Panel,Lamp),false);
                }
                var tallow=Pad("Tallow pad",TallowPad,Hazard);var launch=new GameObject("Launch to Tallow",typeof(DockingPad));launch.transform.position=At(TallowPad);tallow.transform.SetParent(launch.transform,true);
                var pad=launch.GetComponent<DockingPad>();pad.prompt="Launch — Tallow Approach";pad.range=4;pad.scene="TallowApproach";pad.spawn=City;
                Pad("Hull pad",HullPad,new Color(.36f,.74f,.78f));
                var hull=new GameObject("Launch to the hull",typeof(DockingPad));hull.transform.position=At(HullPad);
                var toHull=hull.GetComponent<DockingPad>();toHull.prompt="Launch — The Hull of Many Hands";toHull.range=4;toHull.scene=ManyHandsBuilder.Hull;toHull.spawn="Arrival";
                Gate("The hull pad opens with the claim",new[]{hull},"hands.claimRead",true);
                // Meret's cutter on its own pad, from the dinner's end to her going.
                Pad("Visitors' pad",MeretCutter,new Color(.86f,.88f,.9f));
                if(ZoneKit.Landed("CompactCutter")){var cutter=kit.Prop("CompactCutter",new Vector3(MeretCutter.x,.08f,MeretCutter.y),1.9f,110,false);cutter.name="Meret's cutter";
                    var here=new GameObject("Meret's cutter is docked",typeof(Whereabouts)).GetComponent<Whereabouts>();here.from="hands.dinnerDone";here.goneAt="chapter5.complete";here.targets=new[]{cutter};}

                // The dock door: a cargo portal across the spine's foot, its door slid back east against the yard, with what Pell chalked on it still showing.
                var door=new ZoneKit.Shape();Color frame=new(.56f,.3f,.24f),leaf=new(.7f,.42f,.3f);
                foreach(float x in new[]{-8.1f,8.1f}){door.Slot(0).Box(At(x,-53.5f,3.3f),new Vector3(1.2f,6.6f,1.4f),Quaternion.identity,frame,ZoneKit.Shape.Dim(frame,1.2f));
                    for(int k=0;k<9;k++)door.Slot(1).Quad(At(x-.6f,-54.21f,k*.7f+.2f),At(x+.6f,-54.21f,k*.7f+.2f),At(x+.6f,-54.21f,k*.7f+.55f),At(x-.6f,-54.21f,k*.7f+.55f),Vector3.back,k%2==0?Hazard:Soot,.5f);}
                // No header across the top: a beam six metres up over the spine's foot stood on the camera's line to everyone walking up from the dock.
                door.Slot(2).Box(At(12,-55.1f,2.95f),new Vector3(7.6f,5.9f,.4f),Quaternion.identity,leaf,ZoneKit.Shape.Dim(leaf,1.1f),.36f);
                var doorway=kit.Built("The dock door",door,HullPlate,Panel,Corrugated);ZoneKit.Fadeable(doorway);
                Solid(doorway,new Vector2(-8.1f,-53.5f),new Vector3(1.2f,6.6f,1.4f));Solid(doorway,new Vector2(8.1f,-53.5f),new Vector3(1.2f,6.6f,1.4f));Solid(doorway,new Vector2(12,-55.1f),new Vector3(7.6f,5.9f,.4f));
                // Pell's notice in chalk: the two words that showed, and the ends of the rest at the door's edge. Not a sign (SignStyle lays signs flat): letters on the leaf.
                foreach(var (text,x,z,size) in new[]{("MANY HANDS",11.6f,-55.32f,9.5f),("make light",14.6f,-55.32f,4.2f),("ask",14.9f,-55.32f,4.2f)})
                {
                    var chalk=WorldBuilder.Label(text,new Vector3(x,0,z),size);chalk.name="Chalk on the dock door";chalk.transform.rotation=Quaternion.identity;
                    var tmp=chalk.GetComponent<TMPro.TextMeshPro>();tmp.color=new Color(.96f,.96f,.92f);tmp.rectTransform.sizeDelta=new Vector2(7,1.6f);
                    chalk.transform.position=new Vector3(x,text=="MANY HANDS"?3.6f:text=="ask"?1.6f:2.4f,z);if(text!="MANY HANDS")chalk.transform.position+=Vector3.left*1.1f;
                }
                kit.Lamp(new Vector3(0,5.4f,-55),10,1.2f,new Color(1,.8f,.56f));

                // The yards: container stacks in their dozen paints in the south-west yard and along the freight yard, pallets and cable drums on the apron.
                // Each stack is drawn as its own piece and fades as other tall scenery does when it comes between the camera and the party: one two high stood across the freight
                // hall's arrival and filled half the frame (s29/070).
                var rng=new System.Random(23);var stackBlocks=new GameObject("Container stacks");int paint=0,stackCount=0;
                void Stack(Vector2 at,float yaw,int high){var one=new ZoneKit.Shape();for(int h=0;h<high;h++){Container(one,At(at,h*2.62f)+new Vector3(h==1?.18f:0,0,h==2?-.3f:0),yaw+(h==1?2:0),Paints[paint++%Paints.Length]);}
                    var view=Shade(kit.Built("Container stack "+(++stackCount),one,Corrugated,Panel),true);view.transform.SetParent(stackBlocks.transform,true);
                    if(high*2.62f>=ZoneKit.TallEnoughToBlock)ZoneKit.Fadeable(view);
                    Solid(view,at,new Vector3(2.5f,high*2.62f,6.2f),yaw,"Container stack");}
                // The yard's east stacks stand east of x 36.5, off the camera's side of the walk from the dock to the freight hall: two high at (34, -62.2) they stood between the
                // camera and the hall's arrival, and the party was drawn through a hole in them for every step of the way in (s29/070, D359).
                foreach(var (x,z,yaw,high) in new[]{(-73f,-60f,0f,3),(-69.6f,-60,0f,2),(-66.2f,-60,0f,1),(-73,-53,90f,2),(-58,-62.6f,90f,2),(-50,-62.6f,90f,1),(-44,-56,0f,2),(-40.6f,-56,0f,3),
                    (39.8f,-63f,90f,2),(46.4f,-63.6f,90f,1),(48.6f,-58,0f,3)})Stack(new Vector2(x,z),yaw,high);
                // Along the spine's east side, between it and the hall, stacked two and three high, a gap where the gantry stands.
                foreach(float z in new[]{-47f,-40.6f,-26.6f,-20.2f})Stack(new Vector2(9.2f,z),0,z<-30?3:2);
                kit.Scatter(rng,60,()=>new Vector2((float)rng.NextDouble()*120-76,(float)rng.NextDouble()*15-65.5f),p=>!KeepsOff(p)&&Clear(p)&&Height(p)<.05f&&p.x<50,KeepsOff,
                    ("DressCargoPallet",1.1f,true),("DressCableDrum",1.2f,true),("DeckCrate",1,true),("MusterCrate",1.1f,true),("DressFuelTank",1.6f,true),("DressPowerCell",1.2f,true),("HushwellCrates",1.1f,true),("DressCoreRack",1.6f,true),("DressWorkLamp",1.8f,true),("DressRailBuffer",1,true));
                // The partner is set down two metres to the lead's right at the arrival, (-2, -60): the scatter stood a deck crate with its corner on that place and Idra was drawn
                // into it after every travel to the ship (W5-many-hands-02, nov25-whole s54/111). What it stood within 1.6 m of the place goes 2 m on, south-east, onto open deck
                // clear of the crates beside it; moved after the scatter, so nothing else it laid moves.
                var setDown=Arrival+new Vector2(2,0);
                foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent==null&&(t.name=="DeckCrate"||t.name=="MusterCrate"||t.name=="HushwellCrates"||t.name=="DressCargoPallet")&&Vector2.Distance(new Vector2(t.position.x,t.position.z),setDown)<1.6f).ToArray())
                    t.position=new Vector3(setDown.x+1.6f,t.position.y,setDown.y-1.2f);
            }

            static void Spine(ZoneKit kit)
            {
                // The gantry crane straddling the spine: two legs, a beam nine metres up with its trolley, and a crate on its hook over the street.
                var g=new ZoneKit.Shape();Color crane=new(.86f,.62f,.2f);
                foreach(float x in new[]{-11.2f,9.4f}){g.Slot(0).Box(At(x,Gantry.y,4.6f),new Vector3(.9f,9.2f,.9f),Quaternion.identity,crane,ZoneKit.Shape.Dim(crane,1.1f));
                    for(int k=0;k<6;k++)g.Slot(0).Beam(At(x-.45f,Gantry.y-.46f,k*1.5f+.4f),At(x+.45f,Gantry.y-.46f,k*1.5f+1.15f),.06f,.06f,ZoneKit.Shape.Dim(crane,.8f));}
                g.Slot(0).Box(At(-.9f,Gantry.y,9.6f),new Vector3(21.6f,.9f,1.1f),Quaternion.identity,crane,ZoneKit.Shape.Dim(crane,1.15f));
                g.Slot(1).Box(At(2,Gantry.y,8.8f),new Vector3(1.6f,.8f,1.5f),Quaternion.identity,Soot).Tube(new[]{At(2,Gantry.y,8.4f),At(2,Gantry.y,5.2f)},new[]{.03f,.03f},4,Soot,Soot,1,1,false);
                g.Slot(2).Box(At(2,Gantry.y,4.4f),new Vector3(1.6f,1.4f,1.6f),Quaternion.Euler(0,12,0),new Color(.76f,.6f,.42f));
                var gantry=kit.Built("The gantry crane",g,Panel,Panel,Wood);ZoneKit.Fadeable(gantry);
                Solid(gantry,new Vector2(-11.2f,Gantry.y),new Vector3(.9f,9.2f,.9f));Solid(gantry,new Vector2(9.4f,Gantry.y),new Vector3(.9f,9.2f,.9f));
                // Lamps down the spine, both kerbs, every twelve metres; and the riggers' cradle on the quay side, free, saving at the freight yard.
                for(float z=-49;z<=-14;z+=12){LampPost("Spine lamp",new Vector2(-6.4f,z-1.2f),4.2f,new Color(.46f,.5f,.56f),z<-30);LampPost("Spine lamp",new Vector2(6.6f,z+6),4.2f,new Color(.46f,.5f,.56f),false);}
                var c=new ZoneKit.Shape();var foot=At(Cradle+new Vector2(-1.4f,0));var t=Quaternion.Euler(0,90,0);
                foreach(int end in new[]{-1,1})foreach(int leg in new[]{-1,1})c.Slot(0).Tube(new[]{foot+t*new Vector3(end*1.3f,0,leg*.7f),foot+t*new Vector3(end*1.2f,2.1f,0)},new[]{.08f,.07f},6,Soot,new Color(.5f,.54f,.58f));
                c.Slot(0).Beam(foot+t*new Vector3(-1.35f,2.1f,0),foot+t*new Vector3(1.35f,2.1f,0),.12f,.12f,Hazard);
                c.Slot(1).Lump(foot+Vector3.up*.75f,new Vector3(1.05f,.2f,.5f),5,.08f,new Color(.4f,.6f,.62f),new Color(.26f,.36f,.4f),10,5,.25f,t).Slot(2).Lump(foot+t*new Vector3(0,1.8f,0),new Vector3(.17f,.22f,.17f),3,.05f,Color.white,Color.white,8,5);
                var cradle=kit.Built("The riggers' cradle",c,Panel,Cloth,Lamp);ZoneKit.Block(cradle,foot+Vector3.up*.5f,new Vector3(2.6f,1,1.2f),90,"Cradle");
                var bay=new GameObject("Riggers' cradle point",typeof(RepairBay)).GetComponent<RepairBay>();bay.transform.position=At(Cradle);bay.transform.SetParent(cradle.transform,true);
                bay.free=true;bay.prompt="The riggers' cradle — repair and save";bay.saveSpawn="FreightHall";bay.range=2.4f;
                kit.Lamp(new Vector3(Cradle.x-1.4f,1.8f,Cradle.y),5,.8f,new Color(1,.82f,.56f));
                // Cargo on the quay side and the hazard line down both kerbs of the spine.
                var rng=new System.Random(29);
                kit.Scatter(rng,24,()=>new Vector2(-13.2f+(float)rng.NextDouble()*5.4f,-50+(float)rng.NextDouble()*36),p=>!KeepsOff(p)&&Clear(p)&&Hold(p)>RimWide+.5f,KeepsOff,("DressCargoPallet",1,true),("DressCableDrum",1.1f,true),("DeckCrate",.9f,true));
                var lines=new ZoneKit.Shape();
                foreach(float x in new[]{-6.9f,6.9f})for(float z=-52.6f;z<-12;z+=.6f)lines.Quad(At(x-.12f,z,.02f),At(x+.12f,z,.02f),At(x+.12f,z+.3f,.02f),At(x-.12f,z+.3f,.02f),Vector3.up,Hazard,.5f);
                // Painted bays: the hall's and the yard's floors marked out for cargo, each bay numbered in stencilled blocks; the dock's landing lanes.
                foreach(var (x0,x1,z0,z1,w) in new[]{(14f,50f,-51f,-18.6f,6f),(-34f,10f,-65f,-58f,5.5f),(13f,50f,-65f,-57.6f,6.2f)})
                {
                    for(float x=x0;x<=x1+.01f;x+=w)lines.Quad(At(x-.06f,z0,.018f),At(x+.06f,z0,.018f),At(x+.06f,z1,.018f),At(x-.06f,z1,.018f),Vector3.up,Hazard,.5f);
                    foreach(float z in new[]{z0,z1})lines.Quad(At(x0,z-.06f,.018f),At(x1,z-.06f,.018f),At(x1,z+.06f,.018f),At(x0,z+.06f,.018f),Vector3.up,Hazard,.5f);
                    int n=0;for(float x=x0+w*.5f;x<x1;x+=w){var mark=At(x,z0+1,.019f);for(int k=0;k<1+n%3;k++)lines.Quad(mark+new Vector3(-.5f+k*.4f,0,-.3f),mark+new Vector3(-.26f+k*.4f,0,-.3f),mark+new Vector3(-.26f+k*.4f,0,.3f),mark+new Vector3(-.5f+k*.4f,0,.3f),Vector3.up,Cream,.5f);n++;}
                }
                Shade(kit.Built("Floor detail spine kerbs",lines,Panel),false);
            }

            static void FreightHall(ZoneKit kit)
            {
                // Four fixed stacks for cover (the contract's), crates of the convoy's freight strapped on pallets.
                // Each its own piece, faded as tall scenery is when it comes between the camera and the party in Dessik Varn's fight (2.8 m, past ZoneKit.TallEnoughToBlock).
                var holder=new GameObject("Freight hall stacks");int k=0;
                foreach(var at in HallStacks)
                {
                    var s=new ZoneKit.Shape();var foot=At(at);float yaw=k*23;var t=Quaternion.Euler(0,yaw,0);
                    s.Slot(0).Box(foot+Vector3.up*.08f,new Vector3(3.3f,.16f,3.3f),t,new Color(.5f,.4f,.3f));
                    for(int h=0;h<2;h++)for(int i=0;i<2;i++){var c=Paints[(k*3+h+i)%Paints.Length];s.Slot(1).Box(foot+t*new Vector3(i*1.55f-.78f,.16f+h*1.3f+.65f,0),new Vector3(1.5f,1.28f,3.1f),t,c,ZoneKit.Shape.Dim(c,1.1f),.5f);}
                    s.Slot(2).Box(foot+t*new Vector3(0,2.78f,0),new Vector3(3.2f,.06f,.12f),t,Hazard).Box(foot+t*new Vector3(0,1.5f,1.58f),new Vector3(3.2f,2.7f,.04f),t,new Color(.24f,.24f,.26f));
                    var view=Shade(kit.Built("Hall cargo "+(k+1),s,Wood,Panel,Panel),true);view.transform.SetParent(holder.transform,true);ZoneKit.Fadeable(view);
                    Solid(view,at,new Vector3(3.2f,2.8f,3.2f),yaw,"Cargo stack");k++;
                }
                // Six loose crates (LooseCargo): each its own object with a solid box, not static and not in the bake.
                k=0;foreach(var at in HallCrates)
                {
                    var crate=new GameObject("Loose crate",typeof(BoxCollider));crate.transform.position=At(at,.8f);crate.transform.rotation=Quaternion.Euler(0,k*37,0);crate.GetComponent<BoxCollider>().size=Vector3.one*1.6f;
                    var shape=new ZoneKit.Shape().Slot(0).Box(Vector3.zero,Vector3.one*1.6f,Quaternion.identity,new Color(.78f,.62f,.42f),new Color(.86f,.7f,.48f),.6f)
                        .Slot(1).Box(new Vector3(0,.5f,0),new Vector3(1.64f,.1f,1.64f),Quaternion.identity,Soot).Box(new Vector3(0,-.5f,0),new Vector3(1.64f,.1f,1.64f),Quaternion.identity,Soot);
                    var view=kit.Built("Loose crate "+k,shape,Wood,Panel);view.transform.SetParent(crate.transform,false);
                    crate.AddComponent<LooseCargo>();k++;
                }
                // Light enough for every marking: four lamps on the walls' posts, warm work-light.
                foreach(var at in new[]{new Vector2(15,-19.6f),new Vector2(48.4f,-19.6f),new Vector2(48.4f,-50),new Vector2(15,-50)})LampPost("Hall lamp",at,5,new Color(.5f,.52f,.56f),at.x>40==at.y>-30,16,1.5f);
                // The walls the camera sees, dressed: pipe runs and cargo nets along the north wall, hazard paint on the east wall's foot, the hall's number.
                var w=new ZoneKit.Shape();
                for(int i=0;i<3;i++)w.Slot(0).Tube(new[]{At(13.4f,-17.2f,1.6f+i*.9f),At(50.6f,-17.2f,1.6f+i*.9f)},new[]{.16f,.16f},8,Paints[(i*4+2)%Paints.Length],Paints[(i*4+2)%Paints.Length],1,.5f,false);
                for(float z=-52;z<-17;z+=.8f)w.Slot(1).Quad(At(50.98f,z,0),At(50.98f,z+.4f,0),At(50.98f,z+.4f,.5f),At(50.98f,z,.5f),Vector3.left,Hazard,.5f);
                for(float x=13.4f;x<50.6f;x+=.8f)w.Slot(1).Quad(At(x,-17.02f,0),At(x+.4f,-17.02f,0),At(x+.4f,-17.02f,.5f),At(x,-17.02f,.5f),Vector3.back,Hazard,.5f);
                for(int i=0;i<5;i++){var a=At(18+i*7,-17.1f,3.1f);for(int j=0;j<6;j++)w.Slot(2).Rope(a+new Vector3(j*.5f-1.25f,0,0),a+new Vector3(j*.5f-1.25f,-2.2f,-.05f),.15f,.02f,new Color(.74f,.66f,.48f),4,4);}
                Shade(kit.Built("Hall walls dressed",w,Panel,Panel,Rope),false);
            }

            // ---------------------------------------------------------------- the arcade: the old liner's promenade
            static void Arcade(ZoneKit kit)
            {
                // The shopfront wall along the north, the liner's own cream and teal: a long run of bays, each with a porthole band above, broken at the spine.
                var wall=new ZoneKit.Shape();var wallBlocks=new GameObject("Arcade frontage");
                foreach(var (x0,x1) in new[]{(-36f,-8.6f),(8.6f,53f)})
                {
                    float length=x1-x0;int bays=Mathf.RoundToInt(length/4);
                    for(int i=0;i<bays;i++)
                    {
                        float a=x0+length*i/bays,b=x0+length*(i+1)/bays,mid=(a+b)*.5f;var tone=i%3==1?new Color(.86f,.84f,.76f):Cream;
                        wall.Slot(0).Box(At(mid,14.6f,2.1f),new Vector3(b-a-.05f,4.2f,1.4f),Quaternion.identity,tone,tone,.5f);
                        wall.Slot(1).Box(At(mid,13.88f,3.25f),new Vector3(b-a,.5f,.06f),Quaternion.identity,Teal).Box(At(a,13.86f,2.1f),new Vector3(.36f,4.2f,.12f),Quaternion.identity,new Color(.8f,.76f,.66f));
                        for(int p=0;p<2;p++)wall.Slot(2).Disc(At(a+(b-a)*(p+1)/3,13.86f,3.25f),Vector3.back,.2f,12,new Color(1,.88f,.62f),.14f,new Color(.8f,.66f,.36f));
                        // Shop windows and shutters at the foot of each bay, each its own trade's colours.
                        var shop=Paints[(i*7+(x0<0?3:0))%Paints.Length];
                        wall.Slot(1).Box(At(mid,13.86f,1.1f),new Vector3(b-a-.8f,1.8f,.05f),Quaternion.identity,ZoneKit.Shape.Dim(shop,.6f));
                        for(int r=0;r<5;r++)wall.Slot(1).Box(At(mid,13.83f,.4f+r*.33f),new Vector3(b-a-.9f,.05f,.04f),Quaternion.identity,ZoneKit.Shape.Dim(shop,1.15f));
                    }
                    ZoneKit.Block(wallBlocks,At((x0+x1)*.5f,14.6f,2.1f),new Vector3(x1-x0,4.2f,1.4f),0,"Arcade frontage");
                }
                Shade(kit.Built("The liner's frontage",wall,Panel,Panel,Lamp),true).transform.SetParent(wallBlocks.transform,true);
                // Corra's stall (parts and kettles: brass on the counter) and Dov's counter (blue eel), each in front of its bay.
                Stall("Corra's stall",Corra+new Vector2(0,3.4f),0,new Color(.86f,.36f,.3f),new Color(.95f,.84f,.4f),new Color(.95f,.76f,.4f),Brass);
                var dov=Stall("Dov's counter",Dov+new Vector2(0,3.4f),0,new Color(.34f,.56f,.8f),new Color(.86f,.56f,.36f),new Color(.34f,.56f,.8f),Cloth);
                var eels=new ZoneKit.Shape();for(int i=0;i<9;i++){float x=Dov.x-1.6f+i*.4f;eels.Slot(0).Tube(new[]{At(x,11.9f,2.1f),At(x+.04f,11.88f,1.5f),At(x,11.9f,1f)},new[]{.06f,.08f,.04f},6,new Color(.32f,.54f,.8f),new Color(.2f,.36f,.56f),1,1);}
                kit.Built("Dov's eel",eels,Cloth);
                // The arcade clock, the liner's own, on its column by the crossing: the landmark you meet coming up from the dock.
                var clock=new ZoneKit.Shape();var foot=At(Clock);
                clock.Slot(0).Post(foot,4.4f,.3f,.22f,12,Bronze,new Color(.86f,.7f,.4f)).Box(foot+Vector3.up*.3f,new Vector3(1,.6f,1),Quaternion.Euler(0,45,0),Bronze);
                clock.Slot(1).Disc(foot+new Vector3(0,5.2f,-.42f),new Vector3(0,.25f,-1),1.05f,28,Cream,0,new Color(.6f,.46f,.26f));
                clock.Slot(0).Tube(new[]{foot+new Vector3(0,4.3f,0),foot+new Vector3(0,6.25f,0)},new[]{1.08f,1.08f},28,Bronze,Bronze,1,.5f,false);
                for(int h=0;h<12;h++){float a=h*Mathf.PI/6;var tick=foot+new Vector3(Mathf.Sin(a)*.86f,5.2f+Mathf.Cos(a)*.86f,-.48f);clock.Slot(2).Box(tick,new Vector3(.06f,.16f,.02f),Quaternion.Euler(0,0,-h*30),Soot);}
                clock.Slot(2).Beam(foot+new Vector3(0,5.2f,-.5f),foot+new Vector3(.32f,5.62f,-.5f),.06f,.02f,Soot).Beam(foot+new Vector3(0,5.2f,-.5f),foot+new Vector3(-.12f,4.62f,-.5f),.04f,.02f,Soot);
                var clockView=kit.Built("The arcade clock",clock,Brass,Panel,Panel);Solid(clockView,Clock,new Vector3(1,2.4f,1),45,"Clock column");
                kit.Lamp(new Vector3(Clock.x,6.6f,Clock.y-.6f),9,1.2f,new Color(1,.86f,.6f));
                // Hadda's pump-and-press: Quill's pump (the generated Ancelane pump where it is taken in) bolted to her press bed, and the bench beside it; her mending frame (paid) beside that.
                GameObject press;
                if(ZoneKit.Landed("AncelanePump")){press=kit.Prop("AncelanePump",new Vector3(Press.x,0,Press.y),3,200);press.name="The pump-and-press";}
                else{var ps=new ZoneKit.Shape().Slot(0).Box(At(Press,.5f),new Vector3(2,1,1.4f),Quaternion.identity,new Color(.5f,.4f,.3f)).Slot(1).Post(At(Press,1),1.6f,.44f,.36f,12,new Color(.9f,.6f,.32f),new Color(1,.8f,.5f));press=kit.Built("The pump-and-press",ps,Wood,Brass);}
                ZoneKit.Block(press,At(Press,1.1f),new Vector3(2.2f,2.2f,2),0,"Press");
                var bed=new ZoneKit.Shape().Slot(0).Box(At(Press+new Vector2(-2.6f,.4f),.45f),new Vector3(2.4f,.9f,1.2f),Quaternion.identity,new Color(.42f,.36f,.3f),new Color(.6f,.5f,.4f)).Slot(1).Tube(new[]{At(Press,1.4f),At(Press+new Vector2(-1.6f,.4f),1.4f),At(Press+new Vector2(-2.6f,.4f),1)},new[]{.1f,.1f,.1f},8,new Color(.92f,.66f,.36f),new Color(.92f,.66f,.36f),1,.6f);
                var bedView=kit.Built("Hadda's press bed",bed,Wood,Brass);Solid(bedView,Press+new Vector2(-2.6f,.4f),new Vector3(2.4f,.9f,1.2f));
                var bench=new GameObject("The pump-and-press bench",typeof(Bench)).GetComponent<Bench>();bench.transform.position=At(BenchAt);bench.prompt="Fabricate — The Pump-and-Press";bench.range=2.4f;
                var frame=new ZoneKit.Shape();var at=At(ArcadeCradle);var turn=Quaternion.identity;
                foreach(int end in new[]{-1,1})foreach(int leg in new[]{-1,1})frame.Slot(0).Tube(new[]{at+turn*new Vector3(end*1.3f,0,leg*.7f),at+turn*new Vector3(end*1.2f,2,0)},new[]{.08f,.07f},6,Bronze,new Color(.86f,.7f,.42f));
                frame.Slot(0).Beam(at+new Vector3(-1.35f,2,0),at+new Vector3(1.35f,2,0),.1f,.1f,Bronze).Slot(1).Lump(at+Vector3.up*.75f,new Vector3(1.05f,.2f,.5f),5,.08f,new Color(.6f,.66f,.5f),new Color(.36f,.4f,.3f),10,5);
                var cradle=kit.Built("Hadda's mending frame",frame,Brass,Cloth);ZoneKit.Block(cradle,at+Vector3.up*.5f,new Vector3(2.6f,1,1.2f),0,"Frame");
                var repair=new GameObject("Mending frame point",typeof(RepairBay)).GetComponent<RepairBay>();repair.transform.position=At(ArcadeCradle+new Vector2(0,-1.2f));repair.transform.SetParent(cradle.transform,true);
                repair.prompt="Hadda's mending frame — repair and save";repair.saveSpawn="Arcade";repair.range=2.4f;
                // The promenade's middle: benches back to back, planters, and the liner's lamps; the south side a row of planters over the hall's wall and the hold's rim.
                var rng=new System.Random(31);var furniture=new ZoneKit.Shape();var midBlocks=new GameObject("Arcade benches");
                foreach(float x in new[]{-31f,-15f,-2.6f,14f,24f,36f,46f})
                {
                    var p=new Vector2(x,-4.4f);if(KeepsOff(p+new Vector2(-1,0))||KeepsOff(p+new Vector2(1,0)))continue;
                    Bench(furniture,At(p,0)+Vector3.forward*.3f,0);Bench(furniture,At(p,0)+Vector3.back*.3f,180);Solid(midBlocks,p,new Vector3(2,.9f,1.2f));
                }
                foreach(float x in new[]{13f,19.6f,26f,32.6f,39f,45.6f,51f}){var p=new Vector2(x,-12.6f);Planter(furniture,At(p),1.5f,x%2<1?Teal:Cream,rng);Solid(midBlocks,p,new Vector3(1.5f,.9f,1.5f));}
                foreach(float x in new[]{-34f,-28f,-11f}){var p=new Vector2(x,-9.4f);if(KeepsOff(p))continue;Planter(furniture,At(p),1.2f,Cream,rng);Solid(midBlocks,p,new Vector3(1.2f,.7f,1.2f));}
                Shade(kit.Built("Arcade furniture",furniture,Wood,Leaf),true).transform.SetParent(midBlocks.transform,true);
                // The row of planters over the hall's north wall keeps feet off the strip it would hide.
                ZoneKit.Block(midBlocks,At(32,-13.4f,.45f),new Vector3(42,.9f,3.4f),0,"Planter row");
                for(float x=-30;x<=48;x+=13)LampPost("Arcade lamp",new Vector2(x,12.4f),3.6f,Bronze,((int)x/13)%2==0);
                kit.Scatter(rng,20,()=>new Vector2(-34+(float)rng.NextDouble()*85,6+(float)rng.NextDouble()*6.4f),p=>!KeepsOff(p)&&Clear(p),KeepsOff,("DressPlanter",1,true),("DressKiosk",1.6f,true),("DeckBench",.9f,true));
                // The liner's runner down the promenade, teal with a cream border and a repeat, as liners carpeted theirs: the arcade's floor reads as a promenade and not a board.
                var runner=new ZoneKit.Shape();
                for(float x=-35;x<51;x+=1.2f)
                {
                    if(x>-8.6f&&x<7.4f)continue;
                    runner.Slot(0).Quad(At(x,1.1f,.015f),At(x+1.2f,1.1f,.015f),At(x+1.2f,4.9f,.015f),At(x,4.9f,.015f),Vector3.up,Teal,.5f);
                    foreach(float z in new[]{1.25f,4.6f})runner.Slot(0).Quad(At(x,z,.02f),At(x+1.2f,z,.02f),At(x+1.2f,z+.15f,.02f),At(x,z+.15f,.02f),Vector3.up,Cream,.5f);
                    var c=At(x+.6f,3,.02f);runner.Slot(0).Quad(c+new Vector3(0,0,-.5f),c+new Vector3(.4f,0,0),c+new Vector3(0,0,.5f),c+new Vector3(-.4f,0,0),Vector3.up,new Color(.86f,.7f,.4f),.5f);
                }
                Shade(kit.Built("Floor detail the liner's runner",runner,Cloth),false);
                // Lantern lines strung across the promenade from the frontage to posts on its south side, every people's colours on them: what a town that came together hangs over its street.
                var lines=new ZoneKit.Shape();var posts=new GameObject("Arcade line posts");int flag=0;Color[] colours={new(.3f,.56f,.6f),new(.46f,.62f,.34f),new(.86f,.62f,.3f),new(.7f,.44f,.62f),new(.84f,.36f,.3f),new(.9f,.84f,.6f)};
                foreach(float x in new[]{-30f,-18f,-5.2f,16f,28f,40f})
                {
                    var postFoot=At(x+3,-9.2f);lines.Slot(0).Post(postFoot,4.4f,.08f,.06f,7,Soot,new Color(.5f,.52f,.56f));ZoneKit.Block(posts,postFoot+Vector3.up*1.2f,new Vector3(.24f,2.4f,.24f),0,"Line post");
                    var a=At(x,14,4);var b=postFoot+Vector3.up*4.2f;lines.Slot(1).Rope(a,b,.8f,.02f,Cream,4,14);
                    for(int k=1;k<12;k++){float t=k/12f;var hang=Vector3.Lerp(a,b,t)+Vector3.down*.8f*4*t*(1-t);var across=Vector3.Cross(Vector3.up,(b-a).normalized);
                        if(k%4==0)lines.Slot(2).Lump(hang+Vector3.down*.3f,new Vector3(.14f,.2f,.14f),k,.04f,Color.white,Color.white,8,5);
                        else lines.Slot(3).Triangle(hang-across*.18f,hang+across*.18f,hang+Vector3.down*.5f,Vector3.back,colours[flag++%colours.Length],1);}
                }
                Shade(kit.Built("Arcade lines",lines,Panel,Rope,Lamp,Cloth),true).transform.SetParent(posts.transform,true);
            }

            // ---------------------------------------------------------------- the galley
            static void Galley(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Galley furniture");Color wood=new(.74f,.56f,.38f),top=new(.86f,.7f,.5f);
                // The hatch wall at the back, Binna's counter in front of it, and the hood over it with its vent stacks going up: the landmark.
                s.Slot(0).Box(At(65.5f,17.2f,2.4f),new Vector3(25,4.8f,2),Quaternion.identity,Cream,Cream,.5f).Box(At(65.5f,16.15f,3.8f),new Vector3(25,.5f,.06f),Quaternion.identity,Teal);
                s.Slot(0).Box(At(66,14,.5f),new Vector3(13,1,1.1f),Quaternion.identity,new Color(.62f,.5f,.4f),top);
                s.Slot(1).Box(At(66,15,3.5f),new Vector3(10,1.4f,2.4f),Quaternion.identity,new Color(.76f,.72f,.66f)).Quad(At(61,13.8f,2.8f),At(71,13.8f,2.8f),At(71,15,4.2f),At(61,15,4.2f),new Vector3(0,.4f,-1),new Color(.7f,.66f,.6f));
                // The stacks stop under the camera's line from the garden's walk behind the galley: at 7.8 to 9.4 m they stood across it and hid the party a stride at a time (D359).
                // From the garden's walk they are what stands over the hatch wall, and in the hood's grey panel they read as a bare grey pillar (s31/129, 164): each is a stovepipe
                // now, black iron browned with heat toward its mouth, a brass collar where it leaves the hood, brass bands up it, and an iron rain cowl on three stays (in copper
                // with iron bands they read as a striped bollard, d370-a). The cowls' tops stay under the camera's line to the garden's walk (8.2 m over the stacks from z 24.2, D359).
                foreach(var (x,tall) in new[]{(62.6f,6.1f),(66f,6.5f),(69.4f,5.9f)})
                {
                    var flue=At(x,15.6f);Color iron=new(1,.96f,.92f),heat=new(1.25f,1.02f,.84f),brass=new(.92f,.84f,.72f);
                    s.Slot(2).Tube(new[]{flue+Vector3.up*4.2f,flue+Vector3.up*(tall-1.2f),flue+Vector3.up*tall},new[]{.4f,.37f,.36f},12,iron,heat,1,.4f);
                    s.Slot(6).Tube(new[]{flue+Vector3.up*4.16f,flue+Vector3.up*4.44f},new[]{.5f,.48f},12,brass,brass,1,.5f);
                    foreach(float band in new[]{.4f,.75f})s.Slot(6).Tube(new[]{flue+Vector3.up*Mathf.Lerp(4.44f,tall,band),flue+Vector3.up*(Mathf.Lerp(4.44f,tall,band)+.07f)},new[]{.41f,.41f},12,brass,brass,1,.5f);
                    s.Slot(2).Disc(flue+Vector3.up*(tall+.02f),Vector3.up,.36f,12,Soot);
                    s.Slot(2).Tube(new[]{flue+Vector3.up*(tall+.28f),flue+Vector3.up*(tall+.36f),flue+Vector3.up*(tall+.6f)},new[]{.62f,.5f,.04f},12,new Color(1.3f,1.3f,1.3f),iron,1,.5f);
                    for(int k=0;k<3;k++){float a=k*Mathf.PI*2/3+.5f;var rim=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));s.Slot(2).Beam(flue+Vector3.up*(tall-.04f)+rim*.34f,flue+Vector3.up*(tall+.32f)+rim*.5f,.04f,.04f,iron);}
                }
                // The range on the counter: two fires under the big pots, the pots' lids off and steam over them, more pots along it, ladles and pans hung on the hatch wall
                // behind. With pots alone on a counter nothing read as cooking (s30/284).
                for(int i=0;i<6;i++)if(i!=1&&i!=4)s.Slot(3).Lump(At(61.6f+i*1.6f,14,1.22f),new Vector3(.32f,.22f,.32f),i,.05f,new Color(.7f,.6f,.48f),new Color(.4f,.32f,.26f),10,5);
                foreach(var (x,seed) in new[]{(63.2f,41),(68f,43)})
                {
                    // A hob of dark iron let into the counter, the fire in it, a pot on three short legs over it with something red-brown in it.
                    s.Slot(2).Box(At(x,14,1.02f),new Vector3(1.3f,.06f,.9f),Quaternion.identity,Soot);
                    HarrowgateBuilder.Hearths.Fire(kit,"Galley range fire",At(x,14,1.06f),.34f,.32f,5.5f,1.3f,seed);
                    s.Slot(2).Tube(new[]{At(x,14,1.34f),At(x,14,1.52f),At(x,14,1.78f),At(x,14,1.84f)},new[]{.34f,.42f,.4f,.43f},14,new Color(.34f,.32f,.3f),new Color(.46f,.44f,.4f),1,.5f,false)
                        .Slot(3).Disc(At(x,14,1.74f),Vector3.up,.38f,14,new Color(.62f,.3f,.18f));
                    foreach(int k in new[]{0,1,2}){float a=k*2.1f;s.Slot(2).Post(At(x+Mathf.Cos(a)*.3f,14+Mathf.Sin(a)*.3f,1.04f),.3f,.03f,.03f,4,Soot,Soot);}
                }
                // Pans and ladles on hooks along the hatch wall, a rail of brass under them.
                s.Slot(6).Beam(At(59,16.12f,2.55f),At(73,16.12f,2.55f),.05f,.05f,Bronze);
                for(int i=0;i<9;i++)
                {
                    float x=59.6f+i*1.6f;
                    if(i%3==1){s.Slot(2).Post(At(x,16.08f,1.6f),.75f,.025f,.025f,5,Soot,Soot).Disc(At(x,16.08f,1.55f)+Vector3.back*.02f,Vector3.back,.12f,8,new Color(.3f,.3f,.32f));}
                    else{s.Slot(2).Disc(At(x,16.1f,2.08f)+Vector3.back*.02f,Vector3.back,.24f+.06f*(i%2),14,new Color(.24f,.24f,.26f),0,new Color(.4f,.38f,.36f)).Post(At(x,16.1f,2.3f),.25f,.02f,.02f,4,Soot,Soot);}
                }
                Solid(blocks,new Vector2(66,14),new Vector3(13,1,1.1f));Solid(blocks,new Vector2(65.5f,17.2f),new Vector3(25,4.8f,2));
                // The hatch wall's top, seen from every step of the garden's walk behind it (it filled the frame's foot as a flat grey tiled slab, s31/129, 164, 208): Binna's herb
                // boxes, planted from the garden next door, a timber trough the length of the wall with what the galley cooks with growing in it and trailing over both faces.
                // Out of reach and under the camera's line to the walk (its leaves to 5.9 m; the line crosses the wall at 6.7 m).
                var herbs=new ZoneKit.Shape();var herbRng=new System.Random(371);Color plank=new(.62f,.46f,.3f),plankTop=new(.78f,.6f,.4f);
                herbs.Slot(0).Box(At(65.5f,16.42f,5.01f),new Vector3(24.6f,.42f,.12f),Quaternion.identity,plank,plankTop).Box(At(65.5f,17.98f,5.01f),new Vector3(24.6f,.42f,.12f),Quaternion.identity,plank,plankTop);
                for(float x=53.26f;x<78;x+=6.13f)herbs.Slot(0).Box(At(x,17.2f,5.01f),new Vector3(.12f,.42f,1.68f),Quaternion.identity,plank,plankTop);
                herbs.Slot(1).Box(At(65.5f,17.2f,5.1f),new Vector3(24.4f,.04f,1.46f),Quaternion.identity,new Color(.42f,.32f,.24f),new Color(.48f,.37f,.27f),.5f);
                for(float x=53.6f;x<77.6f;x+=.46f)for(int row=0;row<3;row++)
                {
                    if(herbRng.NextDouble()<.14)continue;var root=At(x+((float)herbRng.NextDouble()-.5f)*.2f,16.75f+row*.45f,5.12f);int kind=((int)(x/2.3f)+row)%4;
                    if(kind==0)herbs.Slot(2).Fern(root,.34f+(float)herbRng.NextDouble()*.18f,5,herbRng,new Color(.3f,.5f,.26f),new Color(.56f,.8f,.42f));
                    else if(kind==1)herbs.Slot(2).Lump(root+Vector3.up*.14f,new Vector3(.2f,.16f,.2f),(int)(x*10)+row,.25f,new Color(.5f,.74f,.38f),new Color(.3f,.46f,.24f),8,4);
                    else if(kind==2)herbs.Slot(2).Frond(root,new Vector3((float)herbRng.NextDouble()-.5f,0,(float)herbRng.NextDouble()-.5f),.5f,.14f,.55f,.25f,new Color(.36f,.52f,.26f),new Color(.56f,.72f,.34f));
                    else herbs.Slot(2).Lump(root+Vector3.up*.12f,new Vector3(.17f,.13f,.17f),(int)(x*10)+row+3,.3f,new Color(.62f,.72f,.4f),new Color(.4f,.5f,.26f),8,4);
                }
                // What trails over the faces: down the galley's side to the band and down the garden's side toward the channel.
                // Each strand leaves the trough's lip and clears the wall's edge before it drops (it is past the face by its middle, still over the top there).
                // Dark and short: long pale strands read as icicles down the wall (d370-a).
                for(float x=53.8f;x<77.4f;x+=.4f)foreach(var (z,dir) in new[]{(16.3f,Vector3.back),(18.1f,Vector3.forward)})
                {
                    if(herbRng.NextDouble()<.3)continue;float hang=.45f+(float)herbRng.NextDouble()*.6f;
                    herbs.Slot(2).Frond(At(x+((float)herbRng.NextDouble()-.5f)*.3f,z,5.2f),dir,.2f,.3f,.02f,hang,new Color(.24f,.4f,.2f),new Color(.36f,.54f,.26f),6);
                }
                Shade(kit.Built("Binna's herb boxes on the hatch wall",herbs,Wood,Soil,Leaf),true);
                var steam=kit.Drift("Galley steam",At(66,14,2.2f),new Vector3(8,.4f,.6f),kit.Glow("Steam",false,new Color(.9f,.9f,.92f,1)),new Color(.92f,.92f,.94f,.3f),40,.5f,1.2f,.6f,4);
                var main=steam.main;main.startSpeed=.6f;
                // The long tables, three rows, benches both sides, the middle row laid for fourteen. Trestle tables and plank benches on legs, the floor showing under them: built as
                // solid boxes down to the floor, a table and its two benches read as three steps of a bleacher (s30/284, 318, 327, 345). Their solids are as they were.
                Color dark=ZoneKit.Shape.Dim(wood,.62f);
                foreach(float z in new[]{7.5f,-1,-9.5f})
                {
                    s.Slot(4).Box(At(65.5f,z,.775f),new Vector3(16,.07f,1.2f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.82f),top);
                    foreach(float x in new[]{58.3f,63.2f,67.8f,72.7f})
                    {
                        foreach(float side in new[]{-.44f,.44f})s.Slot(4).Beam(At(x,z+side,0),At(x,z+side*.3f,.74f),.09f,.08f,dark);
                        s.Slot(4).Beam(At(x,z-.52f,.69f),At(x,z+.52f,.69f),.12f,.08f,dark).Beam(At(x,z-.48f,.05f),At(x,z+.48f,.05f),.1f,.08f,dark);
                    }
                    s.Slot(4).Beam(At(58.3f,z,.4f),At(72.7f,z,.4f),.08f,.08f,dark);
                    foreach(float side in new[]{-1.05f,1.05f})
                    {
                        s.Slot(4).Box(At(65.5f,z+side,.43f),new Vector3(15.4f,.06f,.36f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.9f),wood);
                        for(int k=0;k<6;k++){float x=58.2f+k*2.86f;foreach(float e in new[]{-.12f,.12f})s.Slot(4).Box(At(x,z+side+e,.2f),new Vector3(.07f,.4f,.07f),Quaternion.identity,dark);}
                    }
                    // Each place a bowl with something in it and a cup by it; down the middle bread, a pot of stew, platters of the garden's greens and roots, and jugs.
                    int places=z==-1?14:6;
                    for(int i=0;i<places;i++)
                    {
                        float x=58.4f+i*(14.4f/(places-1)),near=i%2==0?.32f:-.32f;
                        s.Slot(3).Lump(At(x,z+near,.86f),new Vector3(.17f,.07f,.17f),i,.02f,Cream,new Color(.6f,.56f,.5f),10,3);
                        s.Slot(5).Lump(At(x,z+near,.9f),new Vector3(.12f,.035f,.12f),i+7,.2f,i%3==0?new Color(.86f,.56f,.3f):new Color(.56f,.7f,.36f),new Color(.4f,.36f,.22f),8,3);
                        s.Slot(3).Tube(new[]{At(x+.24f,z+near*.9f,.81f),At(x+.24f,z+near*.9f,.95f)},new[]{.045f,.05f},8,ZoneKit.Shape.Dim(Teal,1.2f),Teal,1,.5f);
                    }
                    for(float x=59.6f;x<72;x+=2.9f)
                    {
                        int k=Mathf.RoundToInt(x*3);
                        s.Slot(3).Disc(At(x,z,.815f),Vector3.up,.26f,14,Cream,0,new Color(.8f,.76f,.66f));
                        for(int j=0;j<4;j++)s.Slot(5).Lump(At(x+(j%2-.5f)*.18f,z+(j/2-.5f)*.16f,.85f),new Vector3(.08f,.05f,.07f),k+j,.25f,j%2==0?new Color(.46f,.66f,.3f):new Color(.84f,.5f,.26f),new Color(.3f,.36f,.18f),7,3);
                        s.Slot(5).Lump(At(x+1.15f,z+.08f,.86f),new Vector3(.2f,.08f,.1f),k+5,.12f,new Color(.84f,.66f,.4f),new Color(.6f,.42f,.24f),9,4,.25f,Quaternion.Euler(0,k*13,0));
                        s.Slot(3).Tube(new[]{At(x-.9f,z-.1f,.81f),At(x-.9f,z-.1f,.98f),At(x-.9f,z-.1f,1.08f),At(x-.9f,z-.1f,1.12f)},new[]{.1f,.11f,.06f,.07f},10,new Color(.82f,.78f,.68f),Cream,1,.5f);
                    }
                    s.Slot(2).Tube(new[]{At(65.5f,z,.81f),At(65.5f,z,1.02f),At(65.5f,z,1.06f)},new[]{.24f,.26f,.27f},14,new Color(.34f,.32f,.3f),new Color(.44f,.42f,.38f),1,.5f,false).Slot(3).Disc(At(65.5f,z,1f),Vector3.up,.25f,14,new Color(.62f,.34f,.2f));
                    Solid(blocks,new Vector2(65.5f,z),new Vector3(16,.82f,1.2f));
                    foreach(float side in new[]{-1.05f,1.05f})Solid(blocks,new Vector2(65.5f,z+side),new Vector3(15.4f,.46f,.36f));
                }
                // A warm light over each table (lanterns strung on lines across the room drew bright bars over everyone at the tables from the fixed camera, ch45-after-1).
                foreach(float z in new[]{7.5f,-1,-9.5f})kit.Lamp(new Vector3(65.5f,2.4f,z),7,.9f,new Color(1,.78f,.5f));
                // The south counter row keeps feet off the strip the cold stores' wall would hide: the pantry's sacks and barrels behind it.
                s.Slot(0).Box(At(65.5f,-15.4f,.55f),new Vector3(25,1.1f,1.4f),Quaternion.identity,new Color(.6f,.5f,.4f),top);Solid(blocks,new Vector2(65.5f,-15.4f),new Vector3(25,1.1f,1.4f));
                for(int i=0;i<10;i++)s.Slot(3).Lump(At(55+i*2.4f,-19.6f,.4f),new Vector3(.5f,.42f,.4f),i+20,.12f,new Color(.82f,.74f,.56f),new Color(.6f,.52f,.4f),8,5);
                // The west wall either side of the galley door, and the east windows: frames onto space with a sill rail.
                s.Slot(0).Box(At(53.6f,-10.4f,1.5f),new Vector3(1.2f,3,11.4f),Quaternion.identity,Cream,Cream,.5f).Box(At(53.6f,13.6f,1.5f),new Vector3(1.2f,3,6),Quaternion.identity,Cream,Cream,.5f);
                Solid(blocks,new Vector2(53.6f,-10.4f),new Vector3(1.2f,3,11.4f));Solid(blocks,new Vector2(53.6f,13.6f),new Vector3(1.2f,3,6));
                for(float z=-14;z<=16;z+=2.5f)s.Slot(1).Box(At(77.5f,z,2.4f),new Vector3(.2f,4.8f,.2f),Quaternion.identity,Teal);
                s.Slot(1).Box(At(77.5f,1,4.75f),new Vector3(.3f,.3f,30),Quaternion.identity,Teal);
                var glint=new ZoneKit.Shape();for(float z=-13;z<16;z+=2.5f)glint.Quad(At(77.4f,z,.8f),At(77.4f,z+.5f,.8f),At(77.4f,z+1.5f,4.2f),At(77.4f,z+1,4.2f),Vector3.left,new Color(.7f,.86f,.94f),.5f);
                Shade(kit.Built("Galley window glints",glint,Glass),false);
                Shade(kit.Built("The galley",s,Panel,Panel,Dark,Panel,Wood,Cloth,Brass),true).transform.SetParent(blocks.transform,true);
                foreach(var at in new[]{new Vector2(55.2f,14.8f),new Vector2(75.6f,12.4f),new Vector2(56.6f,-6),new Vector2(75.6f,-12.6f)})LampPost("Galley lamp",at,3.4f,Bronze,at.y>0);
            }

            // ---------------------------------------------------------------- the garden under the carriers' ribs
            static void Garden(ZoneKit kit)
            {
                var beds=new ZoneKit.Shape();var plants=new ZoneKit.Shape();var ribs=new ZoneKit.Shape();var blocks=new GameObject("Garden beds");var rng=new System.Random(37);
                string[] crews={"Marrow-1","Marrow-2","Marrow-3"};Color[] hulls={new(.46f,.6f,.38f),new(.66f,.5f,.36f),new(.44f,.56f,.52f)};
                for(int b=0;b<3;b++)
                {
                    float cx=36+b*16;var c=hulls[b];
                    // The carrier's hold, cut down to a bed: its own hull plating for the bed's sides, soil inside, rows of what grows.
                    beds.Slot(0).Box(At(cx,40,.35f),new Vector3(6,.7f,27),Quaternion.identity,c,ZoneKit.Shape.Dim(c,1.15f),.5f);
                    beds.Slot(1).Box(At(cx,40,.72f),new Vector3(5.4f,.06f,26.4f),Quaternion.identity,new Color(.42f,.32f,.24f),new Color(.46f,.36f,.26f),.5f);
                    Solid(blocks,new Vector2(cx,40),new Vector3(6,.7f,27));
                    for(float z=27.6f;z<53;z+=1.1f)for(float x=-2;x<=2.01f;x+=1)
                    {
                        if(rng.NextDouble()<.18)continue;var root=At(cx+x+((float)rng.NextDouble()-.5f)*.3f,z,.74f);
                        int kind=(b+(int)(z/6))%3;
                        if(kind==0)plants.Slot(0).Fern(root,.55f+(float)rng.NextDouble()*.3f,5,rng,new Color(.3f,.5f,.26f),new Color(.56f,.8f,.42f));
                        else if(kind==1)plants.Slot(0).Frond(root,new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f),.8f,.2f,.7f,.3f,new Color(.4f,.56f,.28f),new Color(.74f,.84f,.4f));
                        else plants.Slot(1).Lump(root+Vector3.up*.18f,new Vector3(.26f,.22f,.26f),(int)(z*10+x),.25f,new Color(.5f,.74f,.38f),new Color(.3f,.46f,.24f),8,4);
                    }
                    // The carrier's ribs over its bed, bronze, four of them: what's left of the hull that brought the seed up.
                    for(int r=0;r<4;r++)
                    {
                        float z=29+r*7;var path=new Vector3[13];var radii=new float[13];
                        for(int i=0;i<13;i++){float a=Mathf.PI*i/12;path[i]=At(cx-Mathf.Cos(a)*3.4f,z,.7f+Mathf.Sin(a)*6.2f);radii[i]=.13f;}
                        ribs.Slot(0).Tube(path,radii,8,Rib,new Color(.58f,.48f,.36f),1,.5f,false);
                        ribs.Slot(1).Lump(At(cx,z,6.6f),new Vector3(.2f,.24f,.2f),r,.05f,Color.white,Color.white,8,5);
                    }
                    ribs.Slot(0).Tube(new[]{At(cx,28.6f,6.92f),At(cx,51.4f,6.92f)},new[]{.1f,.1f},8,Rib,Rib,1,.5f,false);
                    // Seedling trays stacked at the bed's south end, the crew's own colour on the frame.
                    for(int t=0;t<3;t++)beds.Slot(2).Box(At(cx-1.4f+t*1.4f,25.6f,.2f+t*.1f),new Vector3(1.1f,.12f,.7f),Quaternion.Euler(0,t*7,0),new Color(.4f,.32f,.24f),new Color(.44f,.66f,.34f),.5f);
                    Solid(blocks,new Vector2(cx,25.6f),new Vector3(4,.5f,.9f));
                    kit.Lamp(new Vector3(cx,6,40),14,.9f,new Color(.86f,1,.82f));
                }
                Shade(kit.Built("Garden beds",beds,HullPlate,Soil,Wood),true).transform.SetParent(blocks.transform,true);
                Shade(kit.Built("What grows in the beds",plants,Leaf,Leaf),false);
                var ribView=Shade(kit.Built("The carrier ribs",ribs,Panel,Lamp),true);ZoneKit.Fadeable(ribView);
                // The seed racks off Marrow-3, and behind them the blanket and the bowl nobody was meant to see.
                var racks=new ZoneKit.Shape();
                for(int i=0;i<3;i++)
                {
                    float z=44.6f+i*2.6f;racks.Slot(0).Box(At(75.6f,z,1.1f),new Vector3(.7f,2.2f,2.3f),Quaternion.identity,new Color(.5f,.42f,.32f));
                    for(int sh=0;sh<4;sh++)racks.Slot(1).Box(At(75.2f,z,.35f+sh*.55f),new Vector3(.12f,.18f,2.1f),Quaternion.identity,Color.Lerp(new Color(.5f,.72f,.36f),new Color(.8f,.66f,.36f),(i+sh)%3/2f));
                }
                racks.Slot(2).Quad(At(76.4f,48.6f,.03f),At(77.6f,48.6f,.03f),At(77.6f,51,.03f),At(76.4f,51,.03f),Vector3.up,new Color(.56f,.4f,.6f),.5f).Slot(1).Lump(At(76.8f,51.4f,.08f),new Vector3(.16f,.07f,.16f),3,.02f,Cream,new Color(.6f,.56f,.5f),10,3);
                var rackView=kit.Built("The seed racks off Marrow-3",racks,Wood,Panel,Cloth);Solid(rackView,new Vector2(75.6f,47.2f),new Vector3(.7f,2.2f,7.6f));
                // Dabb's pumps by the channel: the water the raft bay sends round, pumped up to the beds.
                var pumps=new ZoneKit.Shape();
                // The second stands on the channel's kerb, clear of the garden's way along it.
                foreach(var (x,z) in new[]{(76.4f,24.2f),(40.6f,22.85f)}){pumps.Slot(0).Post(At(x,z),1.3f,.18f,.16f,10,new Color(.4f,.5f,.48f),new Color(.56f,.66f,.62f)).Slot(1).Disc(At(x-.3f,z,1.05f),Vector3.left,.34f,14,Brass.color,.26f);
                    pumps.Slot(0).Tube(new[]{At(x,z,.2f),At(x,Mathf.Max(22.55f,z-1.4f),.1f),At(x,22.2f,-.6f)},new[]{.08f,.08f,.08f},6,new Color(.4f,.5f,.48f),new Color(.4f,.5f,.48f),1,.5f);}
                var pumpView=kit.Built("Dabb's pumps",pumps,Panel,Brass);Solid(pumpView,new Vector2(76.4f,24.2f),new Vector3(.5f,1.3f,.5f));Solid(pumpView,new Vector2(40.6f,22.85f),new Vector3(.5f,1.3f,.5f));
                // The east glass onto space: tall mullions in the carriers' bronze with glints across the panes.
                var glass=new ZoneKit.Shape();var glints=new ZoneKit.Shape();
                for(float z=19;z<=57;z+=3.2f){glass.Box(At(77.6f,z,3.4f),new Vector3(.18f,6.8f,.18f),Quaternion.identity,Rib);glints.Quad(At(77.5f,z+.4f,1),At(77.5f,z+1,1),At(77.5f,z+2.4f,5.6f),At(77.5f,z+1.8f,5.6f),Vector3.left,new Color(.7f,.88f,.96f),.5f);}
                glass.Box(At(77.6f,38,6.8f),new Vector3(.24f,.24f,39),Quaternion.identity,Rib);
                Shade(kit.Built("The garden's east glass",glass,Panel),false);Shade(kit.Built("Garden glints",glints,Glass),false);
                kit.Scatter(rng,28,()=>new Vector2(27+(float)rng.NextDouble()*50,24+(float)rng.NextDouble()*32),p=>!KeepsOff(p)&&Clear(p)&&Height(p)<.05f&&Height(p)>-.05f,KeepsOff,
                    ("DressPlanter",.9f,true),("DressWorkbench",1,true),("DressFanShrub",1.4f,true),("DressSucculent",.9f,true),("SpicePodPlant",1.6f,true),("OrchardTree",2.6f,true),("HushwellCrates",.9f,true));
            }

            // ---------------------------------------------------------------- the raft bay
            static void RaftBay(ZoneKit kit)
            {
                // The walks: single planks across, rope rails on posts, a box under each a body walks on.
                var walks=new ZoneKit.Shape();var walkBlocks=new GameObject("Raft walks");var rng=new System.Random(41);Color plank=new(.82f,.66f,.44f),dark=new(.5f,.4f,.3f);
                foreach(var (name,a,b) in Walks)
                {
                    var from=new Vector3(a.x,0,a.y);var to=new Vector3(b.x,0,b.y);var d=(to-from).normalized;var across=Vector3.Cross(Vector3.up,d);
                    walks.Slot(0).Deck(from-d*.6f,to+d*.6f,WalkHalf*2,.12f,plank,rng);
                    foreach(int side in new[]{-1,1})
                    {
                        var p0=from+across*side*(WalkHalf+.05f);var p1=to+across*side*(WalkHalf+.05f);
                        walks.Slot(0).Post(p0+Vector3.down*.3f,1.3f,.06f,.05f,6,dark,plank).Post(p1+Vector3.down*.3f,1.3f,.06f,.05f,6,dark,plank).Slot(1).Rope(p0+Vector3.up*.95f,p1+Vector3.up*.95f,.18f,.025f,Straw,4,8);
                        ZoneKit.BlockAlong(walkBlocks,p0+Vector3.up*.6f,p1+Vector3.up*.6f,.16f,1.2f,"Walk rail");
                    }
                    ZoneKit.BlockAlong(walkBlocks,new Vector3(from.x-d.x*2.2f,-.25f,from.z-d.z*2.2f),new Vector3(to.x+d.x*2.2f,-.25f,to.z+d.z*2.2f),WalkHalf*2,.5f,"Walk deck");
                }
                Shade(kit.Built("Raft walks",walks,Wood,Rope),true).transform.SetParent(walkBlocks.transform,true);
                // The mat raft's drying racks, dyed mats in bands, the raft bay's landmark; more along the north quay.
                var racks=new ZoneKit.Shape();var rackBlocks=new GameObject("Drying racks");int m=0;
                void Rack(Vector2 at,float yaw,int mats)
                {
                    var foot=At(at);var t=Quaternion.Euler(0,yaw,0);float wide=mats*1.3f;
                    // Half a metre lower than they were: at 3.2 m the mats' tops crossed the camera's line to the walk past Pim's quay (D359).
                    foreach(float x in new[]{-wide*.5f,wide*.5f})racks.Slot(0).Post(foot+t*new Vector3(x,0,0),2.7f,.07f,.06f,6,dark,plank);
                    racks.Slot(0).Beam(foot+t*new Vector3(-wide*.5f,2.6f,0),foot+t*new Vector3(wide*.5f,2.6f,0),.08f,.08f,dark);
                    for(int i=0;i<mats;i++){var c=foot+t*new Vector3(-wide*.5f+.65f+i*1.3f,1.6f,0);var tone=Dyes[m++%Dyes.Length];
                        racks.Slot(1).Quad(c+t*new Vector3(-.58f,-.95f,0),c+t*new Vector3(.58f,-.95f,0),c+t*new Vector3(.58f,.95f,0),c+t*new Vector3(-.58f,.95f,0),t*Vector3.back,tone,.6f);}
                    Solid(rackBlocks,at,new Vector3(wide,1.2f,.4f),yaw,"Rack");
                }
                Rack(new Vector2(-61,-17.4f),0,5);Rack(new Vector2(-54.6f,-22.2f),0,3);Rack(new Vector2(-70,2.6f),0,6);Rack(new Vector2(-50,9.6f),0,4);Rack(new Vector2(-40,-3.6f),0,3);
                Shade(kit.Built("Drying racks",racks,Wood,Weave),true).transform.SetParent(rackBlocks.transform,true);
                // Ona's weaving bench on the weavers' raft: a loom with a mat half made, the comb across it.
                var loom=new ZoneKit.Shape();var lf=At(Ona+new Vector2(0,-2.3f));
                loom.Slot(0).Box(lf+Vector3.up*.45f,new Vector3(2.8f,.9f,1),Quaternion.identity,dark,plank);
                foreach(float x in new[]{-1.3f,1.3f})loom.Slot(0).Post(lf+new Vector3(x,0,.4f),2,.07f,.06f,6,dark,plank);
                loom.Slot(0).Beam(lf+new Vector3(-1.4f,1.95f,.4f),lf+new Vector3(1.4f,1.95f,.4f),.08f,.08f,dark).Slot(1).Quad(lf+new Vector3(-1.2f,.95f,.42f),lf+new Vector3(1.2f,.95f,.42f),lf+new Vector3(1.2f,1.8f,.42f),lf+new Vector3(-1.2f,1.8f,.42f),Vector3.back,new Color(.86f,.56f,.36f),.6f);
                var loomView=kit.Built("Ona's weaving bench",loom,Wood,Weave);Solid(loomView,Ona+new Vector2(0,-2.3f),new Vector3(2.8f,.9f,1));
                // Maru's tide lines on the board at the back of the north quay, in chalk: her cord's knots drawn as waves and the turns marked.
                var board=new ZoneKit.Shape();var bf=At(TideBoard);
                board.Slot(0).Box(bf+new Vector3(0,1.4f,.3f),new Vector3(10,2.8f,.3f),Quaternion.identity,new Color(.34f,.3f,.26f),new Color(.4f,.36f,.3f),.6f);
                for(int line=0;line<4;line++){var path=new Vector3[33];var radii=new float[33];for(int i=0;i<33;i++){float x=-4.6f+i*.29f;path[i]=bf+new Vector3(x,.6f+line*.55f+Mathf.Sin(x*(1.2f+line*.4f)+line)*.18f,.12f);radii[i]=.025f;}board.Slot(1).Tube(path,radii,4,Color.white,Color.white,1,1,false);}
                for(int i=0;i<12;i++)board.Slot(1).Box(bf+new Vector3(-4.4f+i*.8f,2.5f,.12f),new Vector3(.04f,.3f,.02f),Quaternion.identity,Color.white);
                var boardView=kit.Built("Maru's tide lines",board,Wood,Chalk);Solid(boardView,TideBoard+new Vector2(0,.3f),new Vector3(10,2.8f,.3f));
                // The mooring rail on the hold's north-east wall: bare until it's settled, its running rings on it after.
                var rail=new ZoneKit.Shape();rail.Slot(0).Tube(new[]{At(-22.6f,-11.05f,.9f),At(-15.4f,-11.05f,.9f)},new[]{.07f,.07f},8,new Color(.6f,.64f,.68f),new Color(.6f,.64f,.68f),1,.5f);
                foreach(float x in new[]{-22f,-19f,-16f})rail.Slot(0).Box(At(x,-11.05f,.7f),new Vector3(.16f,.4f,.16f),Quaternion.identity,Soot);
                kit.Built("The mooring rail",rail,Panel);
                var rings=new ZoneKit.Shape();foreach(float x in new[]{-21.4f,-20.2f,-18.4f,-17.2f})rings.Tube(new[]{At(x,-11.05f,1.04f),At(x+.02f,-11.05f,.76f)},new[]{.14f,.14f},10,Brass.color,Brass.color,1,1,false);
                var ringView=kit.Built("Running rings",rings,Brass);Gate("The rings are fitted",new[]{ringView},"hands.mooringSettled",true);
                // Pulse lamps (Driftreed's), keeping their count over the water: two short, one long.
                int n=0;
                foreach(var at in new[]{new Vector2(-21,-20.2f),new Vector2(-55.6f,-36.4f),new Vector2(-64.6f,-17.6f),new Vector2(-50.4f,4.6f),new Vector2(-66,10.6f),new Vector2(-30.4f,-6.6f)})
                {
                    GameObject body;Renderer[] lamps;
                    if(ZoneKit.Landed("PulseBeacon")){body=kit.Prop("PulseBeacon",new Vector3(at.x,0,at.y),3.2f,(n*67)%360);body.name="Pulse lamp";lamps=body.GetComponentsInChildren<Renderer>();}
                    else{body=LampPost("Pulse lamp "+n,at,3,new Color(.58f,.46f,.32f),false);lamps=new[]{body.GetComponent<Renderer>()};}
                    var rhythm=body.AddComponent<LampRhythm>();rhythm.count=LampRhythm.Count.TwoShortOneLong;rhythm.retuned=rhythm.count;rhythm.litFlag=rhythm.retunedFlag=rhythm.darkFlag="";rhythm.lamps=lamps;rhythm.offset=(n%4)*.4f;
                    if(n%2==0){var light=kit.Lamp(new Vector3(at.x,3,at.y),8,1.2f,new Color(1,.78f,.46f));rhythm.lights=new[]{light};rhythm.intensity=1.2f;}
                    n++;
                }
                // Maru's ferry in the water off the ferry raft, its pole, and the line it runs on; canoes moored along the quay; floats.
                var ferry=new ZoneKit.Shape();var fc=new Vector3(-31,HoldWater+.12f,-36);
                for(int i=0;i<6;i++)ferry.Slot(0).Tube(new[]{fc+new Vector3(-1.3f+i*.52f,0,-2),fc+new Vector3(-1.3f+i*.52f,0,2)},new[]{.24f,.24f},7,Straw,new Color(.92f,.82f,.56f),1,.4f);
                ferry.Slot(1).Tube(new[]{fc+new Vector3(.4f,.2f,.4f),fc+new Vector3(1.6f,3.4f,-.6f)},new[]{.04f,.03f},5,new Color(.6f,.48f,.32f),new Color(.8f,.68f,.46f),1,.6f);
                ferry.Slot(2).Rope(At(-27,-29,.8f),new Vector3(-36,HoldWater+1.6f,-45),1.2f,.025f,Straw,4,12);
                for(int i=0;i<5;i++){var c=new Vector3(-12.9f-i*.1f,HoldWater+.1f,-40+i*3.2f);ferry.Slot(0).Lump(c+new Vector3(-1.4f,0,0),new Vector3(1.6f,.22f,.5f),i,.05f,Dyes[i%Dyes.Length],ZoneKit.Shape.Dim(Dyes[i%Dyes.Length],.6f),10,4,.5f,Quaternion.Euler(0,90,0));}
                for(int i=0;i<14;i++){var c=new Vector3(-70+(float)rng.NextDouble()*52,HoldWater+.04f,-47+(float)rng.NextDouble()*34);if(RaftLand(new Vector2(c.x,c.z))<1.5f||Hold(new Vector2(c.x,c.z))>-1.5f)continue;
                    ferry.Slot(3).Lump(c,new Vector3(.9f,.06f,.9f),i,.2f,new Color(.44f,.66f,.38f),new Color(.3f,.46f,.28f),10,3);}
                Shade(kit.Built("Ferry and floats",ferry,Weave,Wood,Rope,Leaf),false);
                kit.Scatter(rng,20,()=>new Vector2(-77+(float)rng.NextDouble()*40,-10+(float)rng.NextDouble()*25),p=>!KeepsOff(p)&&Clear(p)&&Hold(p)>RimWide+.6f&&Height(p)<.05f,KeepsOff,
                    ("HushwellCrates",.9f,true),("DressCableDrum",.9f,true),("DressPlanter",.8f,true),("DeckCrate",.8f,true),("DressWorkbench",.9f,true));
            }

            // ---------------------------------------------------------------- the cabins: Tallow's station rows
            /// <summary>The rows of cabins, each a run of doors facing the camera, painted cabin by cabin; the north row backs onto the rail and holds the breach.</summary>
            internal static readonly (float z,float x0,float x1)[] CabinRows={(25.1f,-76,-55),(25.1f,-49,-30),(35.1f,-76,-55),(35.1f,-49,-30),(45.1f,-76,-55),(45.1f,-49,-30),(55.6f,-76,-55),(55.6f,-49,-30)};
            static void Cabins(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Cabin rows");int cabin=0;Color[] coats={new(.84f,.88f,.86f),new(.6f,.76f,.76f),new(.88f,.82f,.68f),new(.7f,.78f,.86f),new(.86f,.74f,.7f)};
                foreach(var (z,x0,x1) in CabinRows)
                {
                    float deep=z>50?4:3,tall=2.6f;int count=Mathf.RoundToInt((x1-x0)/3.4f);
                    for(int i=0;i<count;i++)
                    {
                        float a=x0+(x1-x0)*i/count,b=x0+(x1-x0)*(i+1)/count,mid=(a+b)*.5f;var coat=coats[(cabin*3+i)%coats.Length];cabin++;
                        s.Slot(0).Box(At(mid,z,tall*.5f),new Vector3(b-a-.04f,tall,deep),Quaternion.identity,coat,ZoneKit.Shape.Dim(coat,.82f),.5f);
                        // Doors, windows, awnings and steps are CityDetail's (CabinFronts).
                        // a mat at each door, a pot or a stool at some
                        s.Slot(3).Quad(At(mid-1.1f,z-deep*.5f-.9f,.02f),At(mid-.1f,z-deep*.5f-.9f,.02f),At(mid-.1f,z-deep*.5f-.1f,.02f),At(mid-1.1f,z-deep*.5f-.1f,.02f),Vector3.up,Dyes[(cabin+i)%Dyes.Length],.6f);
                    }
                    s.Slot(1).Box(At((x0+x1)*.5f,z,tall+.08f),new Vector3(x1-x0+.2f,.16f,deep+.2f),Quaternion.identity,new Color(.4f,.46f,.5f));
                    ZoneKit.Block(blocks,At((x0+x1)*.5f,z,tall*.5f),new Vector3(x1-x0,tall,deep),0,"Cabin row");
                }
                // What the camera mostly sees of a cabin is its roof: each in its own sheet and tone, with what people keep up there (a vent, a water tank, a planter, a solar panel, a crate).
                var roofs=new ZoneKit.Shape();var rr=new System.Random(61);
                foreach(var (z,x0,x1) in CabinRows)
                {
                    float deep=z>50?4:3,top=2.68f;int count=Mathf.RoundToInt((x1-x0)/3.4f);
                    for(int i=0;i<count;i++)
                    {
                        float a=x0+(x1-x0)*i/count,b=x0+(x1-x0)*(i+1)/count,mid=(a+b)*.5f;var tone=Color.Lerp(new Color(.5f,.54f,.58f),Paints[rr.Next(Paints.Length)],.62f);
                        roofs.Slot(0).Box(At(mid,z,top),new Vector3(b-a-.2f,.06f,deep-.1f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.9f),tone,.5f);
                        foreach(float side in new[]{-1f,1f}){roofs.Slot(0).Box(At(mid,z+side*(deep*.5f-.12f),top+.08f),new Vector3(b-a-.2f,.12f,.16f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.55f));roofs.Slot(0).Box(At(mid+side*((b-a)*.5f-.2f),z,top+.08f),new Vector3(.16f,.12f,deep-.1f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.55f));}
                        for(int r=0;r<3;r++)roofs.Slot(0).Box(At(mid,z-deep*.5f+.5f+r*(deep-1)/2,top+.04f),new Vector3(b-a-.4f,.03f,.06f),Quaternion.identity,ZoneKit.Shape.Dim(tone,.7f));
                        for(int item=0;item<3;item++){if(item==2&&rr.Next(2)==0)continue;
                        int kind=rr.Next(6);var c=At(mid-.8f+item*.8f+(float)rr.NextDouble()*.3f,z+(float)rr.NextDouble()*.8f-.4f,top+.05f);
                        if(kind==0)roofs.Slot(1).Tube(new[]{c,c+Vector3.up*.9f},new[]{.18f,.16f},8,new Color(.56f,.58f,.6f),new Color(.4f,.42f,.44f),1,.5f).Slot(1).Disc(c+Vector3.up*.92f,Vector3.up,.24f,10,Soot);
                        else if(kind==1)roofs.Slot(1).Tube(new[]{c+new Vector3(-.7f,.4f,0),c+new Vector3(.7f,.4f,0)},new[]{.38f,.38f},12,new Color(.7f,.74f,.76f),new Color(.6f,.64f,.66f),1,.5f);
                        else if(kind==2)Planter(roofs,c,.9f,Paints[rr.Next(Paints.Length)],rr);
                        else if(kind==3)roofs.Slot(1).Box(c+Vector3.up*.25f,new Vector3(1.6f,.06f,1),Quaternion.Euler(-24,0,0),new Color(.18f,.24f,.36f));
                        else if(kind==4)roofs.Slot(1).Box(c+Vector3.up*.3f,new Vector3(.8f,.6f,.7f),Quaternion.Euler(0,rr.Next(40),0),new Color(.72f,.58f,.4f));
                        else roofs.Slot(1).Quad(c+new Vector3(-.5f,.02f,-.4f),c+new Vector3(.5f,.02f,-.4f),c+new Vector3(.5f,.02f,.4f),c+new Vector3(-.5f,.02f,.4f),Vector3.up,Dyes[rr.Next(Dyes.Length)],.6f);}
                    }
                }
                Shade(kit.Built("Cabin roofs",roofs,Panel,Panel,Leaf),true);
                var rowView=Shade(kit.Built("Cabin rows",s,Panel,Panel,Lamp,Cloth),true);rowView.transform.SetParent(blocks.transform,true);ZoneKit.Fadeable(rowView);
                // Laundry strung across the lanes from row to row, every neighbour's colours.
                var laundry=new ZoneKit.Shape();var rng=new System.Random(43);
                foreach(var (x,z0,z1) in new[]{(-72f,26.6f,33.6f),(-62f,26.6f,33.6f),(-44f,36.6f,43.6f),(-36f,26.6f,33.6f),(-68f,46.6f,53.6f),(-40f,46.6f,53.6f)})
                {
                    var a=At(x,z0,2.5f);var b=At(x+1.4f,z1,2.5f);laundry.Slot(0).Rope(a,b,.5f,.015f,Cream,4,10);
                    for(int i=1;i<6;i++){float t=i/6f;var p=Vector3.Lerp(a,b,t)+Vector3.down*.5f*4*t*(1-t);var tone=Dyes[rng.Next(Dyes.Length)];
                        laundry.Slot(1).Quad(p+new Vector3(-.05f,0,-.35f),p+new Vector3(-.05f,0,.35f),p+new Vector3(-.05f,-.7f,.35f),p+new Vector3(-.05f,-.7f,-.35f),Vector3.left,tone,.8f);}
                }
                Shade(kit.Built("Laundry",laundry,Rope,Cloth),false);
                // The breach: the cabin row's west end, whole until the fight; afterwards a welded patch in raw plate with its seams and tape, and the Steady Hand's cutters latched
                // on beyond the rail.
                var patch=new ZoneKit.Shape();
                patch.Slot(0).Box(At(Breach.x,53.56f,1.35f),new Vector3(8.4f,2.5f,.08f),Quaternion.identity,new Color(.7f,.72f,.74f));
                for(int i=0;i<5;i++)patch.Slot(1).Box(At(Breach.x-4.2f+i*2.1f,53.5f,1.35f),new Vector3(.08f,2.5f,.06f),Quaternion.identity,new Color(.96f,.86f,.6f));
                patch.Slot(1).Box(At(Breach.x,53.5f,2.6f),new Vector3(8.6f,.08f,.06f),Quaternion.identity,new Color(.96f,.86f,.6f));
                for(int i=0;i<9;i++)patch.Slot(2).Box(At(Breach.x-4+i*1,53.48f,1.35f+(i%2==0?.6f:-.6f)),new Vector3(.9f,.1f,.03f),Quaternion.Euler(0,0,i%2==0?20:-20),Hazard);
                var patchView=kit.Built("The breach, sealed",patch,Panel,Lamp,Panel);
                Gate("The breach is sealed",new[]{patchView},"bossdown.DessikVarn",true);
                if(ZoneKit.Landed("CompactCutter"))
                {
                    var latched=new GameObject("The Steady Hand's cutters");
                    foreach(var (x,yaw) in new[]{(-66f,170f),(-57f,190f)}){var cutter=kit.Loose("CompactCutter",new Vector3(x,-1.2f,62.4f),2.2f,Quaternion.Euler(0,yaw,0),null,false);cutter.transform.SetParent(latched.transform,true);}
                    Gate("The cutters latch on",new[]{latched},"bossdown.DessikVarn",true);
                    var sky=GameObject.Find("Stars below");if(sky!=null)latched.transform.SetParent(sky.transform,true);
                }
                kit.Scatter(rng,30,()=>new Vector2(-76+(float)rng.NextDouble()*46,19+(float)rng.NextDouble()*36),p=>!KeepsOff(p)&&Clear(p)&&!InACabinRow(p,1.4f)&&p.y<53,KeepsOff,
                    ("DressPlanter",.8f,true),("DressPowerCell",.9f,true),("DeckBench",.8f,true),("DressSucculent",.7f,true),("DressLightBollard",1,true),("DressSolarArray",1.1f,true));
            }
            static bool InACabinRow(Vector2 p,float margin){foreach(var (z,x0,x1) in CabinRows)if(p.x>x0-margin&&p.x<x1+margin&&Mathf.Abs(p.y-z)<(z>50?2:1.5f)+margin)return true;return false;}

            // ---------------------------------------------------------------- the lamp gallery
            static void LampGallery(ZoneKit kit)
            {
                float top=GalleryTop;var s=new ZoneKit.Shape();var blocks=new GameObject("Gallery furniture");var rng=new System.Random(47);
                // The stair up the spine's head: treads over the ramp, stringers and brass rails.
                for(int i=0;i<14;i++){float z=24+i;float y=top*(i+.5f)/14;s.Slot(0).Box(new Vector3(0,y-.12f,z+.5f),new Vector3(6,.24f,1.02f),Quaternion.identity,new Color(.5f,.62f,.64f),new Color(.62f,.74f,.74f));}
                foreach(float x in new[]{-3.1f,3.1f}){s.Slot(1).Beam(new Vector3(x,.6f,24),new Vector3(x,top+.6f,38),.12f,.4f,new Color(.4f,.5f,.52f)).Beam(new Vector3(x,1.2f,24),new Vector3(x,top+1.2f,38),.07f,.07f,Bronze);}
                // The balustrade round the terrace: brass rails on the south face and the ends, never where the stair comes up.
                foreach(var (a,b) in new[]{(new Vector3(-24,top,38.3f),new Vector3(-3.4f,top,38.3f)),(new Vector3(3.4f,top,38.3f),new Vector3(24,top,38.3f)),(new Vector3(-23.7f,top,38.3f),new Vector3(-23.7f,top,57.6f)),(new Vector3(23.7f,top,38.3f),new Vector3(23.7f,top,57.6f))})
                    Rail("Gallery balustrade",a,b,new Color(.86f,.7f,.42f));
                // Banners hung from the gallery's front, one for each people aboard, and lanterns between them: what the spine looks up at.
                Color[] peoples={new(.3f,.56f,.6f),new(.46f,.62f,.34f),new(.86f,.62f,.3f),new(.7f,.44f,.62f),new(.84f,.36f,.3f)};
                for(int i=0;i<8;i++){float x=-21+i*(i<4?4.4f:4.4f)+(i>=4?11.6f:0);var a=new Vector3(x,top-.1f,37.9f);var tone=peoples[i%peoples.Length];
                    s.Slot(2).Quad(a+new Vector3(-.7f,0,0),a+new Vector3(.7f,0,0),a+new Vector3(.7f,-2.4f,0),a+new Vector3(-.7f,-2.4f,0),Vector3.back,tone,.6f).Triangle(a+new Vector3(-.7f,-2.4f,0),a+new Vector3(.7f,-2.4f,0),a+new Vector3(0,-2.9f,0),Vector3.back,tone,.6f);
                    // Each people's device sewn on in cream, a pale border down its sides, a dark hem and a brass rod it hangs from: one flat colour to a banner read as a bare card (s30/428).
                    var face=a+Vector3.back*.015f;var pale=Color.Lerp(tone,Cream,.55f);var deep=ZoneKit.Shape.Dim(tone,.62f);
                    foreach(float side in new[]{-1f,1f})s.Slot(2).Quad(face+new Vector3(side*.7f,-.05f,0),face+new Vector3(side*.58f,-.05f,0),face+new Vector3(side*.58f,-2.38f,0),face+new Vector3(side*.7f,-2.38f,0),Vector3.back,pale,.6f);
                    s.Slot(2).Quad(face+new Vector3(-.7f,-2.12f,0),face+new Vector3(.7f,-2.12f,0),face+new Vector3(.7f,-2.32f,0),face+new Vector3(-.7f,-2.32f,0),Vector3.back,deep,.6f);
                    var mark=face+new Vector3(0,-1.05f,0);
                    switch(i%peoples.Length)
                    {
                        case 0:s.Slot(2).Disc(mark,Vector3.back,.36f,16,Cream,.22f);break;
                        case 1:s.Slot(2).Quad(mark+new Vector3(0,.4f,0),mark+new Vector3(.3f,0,0),mark+new Vector3(0,-.4f,0),mark+new Vector3(-.3f,0,0),Vector3.back,Cream,.6f);break;
                        case 2:foreach(var o in new[]{new Vector3(0,.22f,0),new Vector3(-.2f,-.14f,0),new Vector3(.2f,-.14f,0)})s.Slot(2).Disc(mark+o,Vector3.back,.13f,12,Cream);break;
                        case 3:s.Slot(2).Triangle(mark+new Vector3(-.34f,-.26f,0),mark+new Vector3(.34f,-.26f,0),mark+new Vector3(0,.36f,0),Vector3.back,Cream,.6f);break;
                        default:s.Slot(2).Quad(mark+new Vector3(-.3f,.06f,0),mark+new Vector3(.3f,.06f,0),mark+new Vector3(.3f,-.06f,0),mark+new Vector3(-.3f,-.06f,0),Vector3.back,Cream,.6f).Quad(mark+new Vector3(-.06f,.34f,0),mark+new Vector3(.06f,.34f,0),mark+new Vector3(.06f,-.34f,0),mark+new Vector3(-.06f,-.34f,0),Vector3.back,Cream,.6f);break;
                    }
                    s.Slot(6).Tube(new[]{a+new Vector3(-.84f,.02f,-.05f),a+new Vector3(.84f,.02f,-.05f)},new[]{.035f,.035f},6,Bronze,Bronze,1,1).Lump(a+new Vector3(-.88f,.02f,-.05f),Vector3.one*.06f,i,.05f,Bronze,Bronze,6,4).Lump(a+new Vector3(.88f,.02f,-.05f),Vector3.one*.06f,i+9,.05f,Bronze,Bronze,6,4);}
                // Sela's chart table: a big slanted table with the chart on it lit from inside, the scopes round it, Abri's desk and its piles of forms, Ilo's beside it.
                // The table was a crate with a pale lid and seven lines across it, which read as painted chevrons where five chapters' plans are heard round it (nov22b-whole s62/037,
                // D382). It is a table now: four turned legs, an apron, a shelf of rolled charts under it, the slanted top in a brass rail, and on it the route chart itself, dark
                // glass with the lanterns and roads lit in it where the pause menu's chart sets them (every ChartStop at its own place, joined as RouteChart joins them), inside a ruled
                // brass border. Drawn only: its solid is the one it had, so nobody stands any differently round it.
                var table=At(ChartTable,0);var slope=Quaternion.Euler(-8,0,0);var facing=slope*Vector3.up;
                Vector3 Top(float x,float z,float lift)=>table+Vector3.up*1.06f+slope*new Vector3(x,.04f+lift,z);
                var oak=new Color(.42f,.34f,.26f);var dark=new Color(.3f,.24f,.18f);
                s.Slot(3).Box(table+new Vector3(0,1.06f,0),new Vector3(3.4f,.08f,2.2f),slope,new Color(.5f,.4f,.3f));
                foreach(var (x,z) in new[]{(-1.48f,-.86f),(1.48f,-.86f),(-1.48f,.86f),(1.48f,.86f)})
                {
                    float under=1.02f+z*Mathf.Tan(8*Mathf.Deg2Rad);
                    s.Slot(3).Tube(new[]{table+new Vector3(x,0,z),table+new Vector3(x,.14f,z),table+new Vector3(x,.24f,z),table+new Vector3(x,under*.62f,z),table+new Vector3(x,under,z)},new[]{.11f,.1f,.07f,.085f,.075f},8,dark,oak,1,1);
                }
                foreach(var (x,z,w,d) in new[]{(0f,-.92f,2.96f,.06f),(0f,.92f,2.96f,.06f),(-1.56f,0f,.06f,1.7f),(1.56f,0f,.06f,1.7f)})s.Slot(3).Box(Top(x,z,-.15f),new Vector3(w,.18f,d),slope,dark,oak);
                s.Slot(3).Box(table+new Vector3(0,.3f,0),new Vector3(2.96f,.05f,1.72f),Quaternion.identity,dark,oak);
                for(int i=0;i<5;i++){float z=-.6f+i*.3f,x=-.3f+(i%2)*.5f,r=.06f+.012f*(i%3);s.Slot(2).Tube(new[]{table+new Vector3(x-.8f,.33f+r,z),table+new Vector3(x+.8f,.33f+r,z)},new[]{r,r},8,Cream,new Color(.86f,.8f,.64f),1,1);}
                // The chart: dark glass, its ruled border, the roads and the lanterns lit in it.
                s.Slot(4).Quad(Top(-1.56f,-.96f,.004f),Top(1.56f,-.96f,.004f),Top(1.56f,.96f,.004f),Top(-1.56f,.96f,.004f),facing,new Color(.07f,.11f,.17f),.4f);
                foreach(var (a,b) in new[]{(new Vector2(-1.46f,-.86f),new Vector2(1.46f,-.86f)),(new Vector2(1.46f,-.86f),new Vector2(1.46f,.86f)),(new Vector2(1.46f,.86f),new Vector2(-1.46f,.86f)),(new Vector2(-1.46f,.86f),new Vector2(-1.46f,-.86f))})
                    s.Slot(6).Beam(Top(a.x,a.y,.012f),Top(b.x,b.y,.012f),.025f,.008f,Bronze);
                var stops=Resources.LoadAll<ChartStop>("Definitions/ChartStop");
                Vector3 Chart(Vector2 p)=>Top(Mathf.Lerp(-1.3f,1.3f,Mathf.Clamp01(p.x)),Mathf.Lerp(-.72f,.72f,Mathf.Clamp01(p.y)),.012f);
                var roads=new HashSet<string>();int lit=0;
                foreach(var stop in stops)foreach(string id in stop.routes??Array.Empty<string>())
                {
                    var other=Array.Find(stops,o=>o.id==id);if(other==null||!roads.Add(string.CompareOrdinal(stop.id,id)<0?stop.id+"|"+id:id+"|"+stop.id))continue;
                    s.Slot(5).Tube(new[]{Chart(stop.position),Chart(other.position)},new[]{.016f,.016f},4,Color.white,Color.white,1,1,false);
                }
                foreach(var stop in stops){s.Slot(5).Disc(Chart(stop.position)+facing*.008f,facing,.06f,12,Color.white);lit++;}
                // The compass star in the chart's north-east corner, brass and teal, its north lit: the star the floor round the table once carried (D392).
                {
                    var hub=Top(1.1f,.5f,.014f);var east=slope*Vector3.right;var north=slope*Vector3.forward;
                    for(int k=0;k<16;k++)
                    {
                        float a=k*Mathf.PI/8,len=k%4==0?.26f:k%2==0?.19f:.14f;var d=east*Mathf.Sin(a)+north*Mathf.Cos(a);var sideA=east*Mathf.Sin(a-.32f)+north*Mathf.Cos(a-.32f);var sideB=east*Mathf.Sin(a+.32f)+north*Mathf.Cos(a+.32f);
                        int slot=k%2==0?6:4;var tone=k%2==0?Color.white:new Color(.3f,.5f,.54f);var shade=k%2==0?new Color(.8f,.74f,.6f):new Color(.22f,.4f,.44f);
                        s.Slot(slot).Triangle(hub+sideA*.07f,hub+d*len,hub,facing,tone).Triangle(hub,hub+d*len,hub+sideB*.07f,facing,shade);
                    }
                    s.Slot(5).Disc(hub+north*.31f+facing*.002f,facing,.03f,10,Color.white);
                }
                // A chart without its stops (a project with none yet) still shows the Halo's roads: the old plan's three, lit.
                if(lit==0)for(int i=0;i<3;i++)s.Slot(5).Tube(new[]{Top(-1.2f,-.5f+i*.5f,.012f),Top(0,-.3f+i*.4f,.012f),Top(1.2f,-.6f+i*.6f,.012f)},new[]{.016f,.016f,.016f},4,Color.white,Color.white,1,1,false);
                // The rail round the top, on posts, a hand above it: what keeps a rolled chart from going off the low edge.
                var rim=new[]{new Vector2(-1.68f,-1.08f),new Vector2(1.68f,-1.08f),new Vector2(1.68f,1.08f),new Vector2(-1.68f,1.08f)};
                for(int k=0;k<4;k++)
                {
                    var a=rim[k];var b=rim[(k+1)%4];s.Slot(6).Tube(new[]{Top(a.x,a.y,.07f),Top(b.x,b.y,.07f)},new[]{.03f,.03f},6,Bronze,Bronze,1,1);
                    for(float t=0;t<.99f;t+=.5f){var p=Vector2.Lerp(a,b,t);s.Slot(6).Tube(new[]{Top(p.x,p.y,-.01f),Top(p.x,p.y,.07f)},new[]{.022f,.022f},6,Bronze,Bronze,1,1,false);}
                }
                Debug.Log("HANDS_CHART_TABLE lanterns="+lit+" roads="+roads.Count);
                Solid(blocks,ChartTable,new Vector3(3.4f,1.2f,2.2f),0,"Chart table",top);
                foreach(var at in Scopes){var f=At(at);foreach(int k in new[]{0,1,2}){float a=k*Mathf.PI*2/3;s.Slot(6).Tube(new[]{f+new Vector3(Mathf.Cos(a)*.35f,0,Mathf.Sin(a)*.35f),f+Vector3.up*1.3f},new[]{.025f,.02f},4,Bronze,Bronze,1,1,false);}
                    var aim=(new Vector3(GalleryLamps[Array.IndexOf(Scopes,at)].x,top+2,GalleryLamps[Array.IndexOf(Scopes,at)].y)-(f+Vector3.up*1.35f)).normalized;
                    s.Slot(6).Tube(new[]{f+Vector3.up*1.35f-aim*.4f,f+Vector3.up*1.35f+aim*.5f},new[]{.06f,.045f},8,Bronze,new Color(.9f,.78f,.5f),1,1);Solid(blocks,at,new Vector3(.5f,1.3f,.5f),0,"Scope",top);}
                foreach(var (at,piles) in new[]{(AbriDesk,6),(IloDesk,3)})
                {
                    var d=At(at);s.Slot(3).Box(d+new Vector3(0,.4f,0),new Vector3(2,.8f,.9f),Quaternion.identity,new Color(.46f,.36f,.28f),new Color(.58f,.46f,.34f));
                    for(int i=0;i<piles;i++)s.Slot(4).Box(d+new Vector3(-.7f+i*.28f,.8f+.1f*(i%3)+.05f,((i*7)%3-1)*.12f),new Vector3(.24f,.1f+.1f*(i%3)*2,.32f),Quaternion.Euler(0,i*9,0),new Color(.94f,.92f,.86f));
                    Solid(blocks,at,new Vector3(2,.9f,.9f),0,"Desk",top);
                }
                // Ilo's case, on his desk.
                s.Slot(3).Box(At(IloDesk,.92f)+new Vector3(.5f,0,0),new Vector3(.6f,.24f,.42f),Quaternion.Euler(0,12,0),new Color(.3f,.26f,.24f));
                Shade(kit.Built("The lamp gallery",s,Panel,Panel,Cloth,Wood,Panel,Lamp,Brass),true).transform.SetParent(blocks.transform,true);
                // The three gallery lamps on their posts by the north rail, Pell's great lamp at the east end, lanterns along the gallery front.
                for(int i=0;i<3;i++)LampPost("Gallery lamp "+(i+1),GalleryLamps[i],3.8f,new Color(.86f,.7f,.42f),true,9,1.4f,0);
                GameObject great;
                if(ZoneKit.Landed("GreatLantern")){great=kit.Prop("GreatLantern",new Vector3(GreatLamp.x,0,GreatLamp.y),6.4f,200);great.name="Pell's great lamp";}
                else great=LampPost("Pell's great lamp",GreatLamp,6,new Color(.86f,.7f,.42f),false);
                kit.Lamp(new Vector3(GreatLamp.x,top+4.2f,GreatLamp.y),12,1.6f,new Color(1,.78f,.46f));
                for(float x=-20;x<=20;x+=8){if(Mathf.Abs(x)<4)continue;var lantern=new ZoneKit.Shape().Slot(0).Lump(new Vector3(x,top+.9f,38.5f),new Vector3(.18f,.26f,.18f),(int)x,.04f,Color.white,Color.white,8,5);Shade(kit.Built("Gallery front lantern "+x,lantern,Lamp),false);}
                kit.Scatter(rng,8,()=>new Vector2(-22+(float)rng.NextDouble()*44,40+(float)rng.NextDouble()*16),p=>!KeepsOff(p)&&Clear(p)&&Height(p)>top-.05f,KeepsOff,("DressPlanter",.9f,true),("DressShelfRack",1.6f,true),("DeckCrate",.8f,true));
                // Pell's work: racks of the convoy's lamps waiting to be trimmed along the gallery's back, every one lit low.
                var lamps=new ZoneKit.Shape();var glass=new ZoneKit.Shape();var held=new GameObject("Lamp racks");
                foreach(var (x,z,yaw) in new[]{(-22.6f,49f,90f),(-22.6f,43.6f,90f),(21,44.6f,-90f),(-7.6f,56.6f,0f),(6.6f,56.6f,0f)})
                {
                    var foot=At(x,z);var t=Quaternion.Euler(0,yaw,0);
                    lamps.Slot(0).Box(foot+Vector3.up*.9f,new Vector3(3.6f,1.8f,.6f),t,new Color(.44f,.36f,.28f)).Box(foot+t*new Vector3(0,.6f,-.32f),new Vector3(3.6f,.06f,.2f),t,new Color(.5f,.4f,.3f)).Box(foot+t*new Vector3(0,1.3f,-.32f),new Vector3(3.6f,.06f,.2f),t,new Color(.5f,.4f,.3f));
                    for(int k=0;k<10;k++)glass.Lump(foot+t*new Vector3(-1.6f+(k%5)*.8f,k<5?.78f:1.48f,-.32f),new Vector3(.12f,.16f,.12f),k+(int)x,.04f,Color.white,Color.white,7,4);
                    ZoneKit.Block(held,foot+Vector3.up*.9f,new Vector3(3.6f,1.8f,.6f),yaw,"Lamp rack");
                }
                Shade(kit.Built("Pell's lamp racks",lamps,Wood),true).transform.SetParent(held.transform,true);Shade(kit.Built("Lamps waiting to be trimmed",glass,Lamp),false);
                // The compass rose's ring painted round the chart table, Sela's own, and the gallery's rugs where people stand to read. Its star is in the chart on the table's top
                // (the chart's corner, above): painted on the floor, its sixteen points ran out from under the table and drew across its apron and legs from the play camera
                // (nov23-whole s55/035, D392).
                var rose=new ZoneKit.Shape();var c=At(ChartTable,.016f);
                for(int k=0;k<48;k++){float a=k*Mathf.PI/24,b=(k+1)*Mathf.PI/24;rose.Quad(c+new Vector3(Mathf.Sin(a)*5,0,Mathf.Cos(a)*5),c+new Vector3(Mathf.Sin(b)*5,0,Mathf.Cos(b)*5),c+new Vector3(Mathf.Sin(b)*5.3f,0,Mathf.Cos(b)*5.3f),c+new Vector3(Mathf.Sin(a)*5.3f,0,Mathf.Cos(a)*5.3f),Vector3.up,k%2==0?Cream:new Color(.3f,.46f,.5f),.5f);}
                foreach(var (x,z,w,d,k) in new[]{(-14f,44.6f,4.4f,2.6f,0),(-18f,52f,3f,4f,1),(10.6f,51.4f,3.4f,2.4f,2),(14f,42.4f,3f,2f,3)})
                {var rc=At(x,z,.012f);rose.Quad(rc+new Vector3(-w/2,0,-d/2),rc+new Vector3(w/2,0,-d/2),rc+new Vector3(w/2,0,d/2),rc+new Vector3(-w/2,0,d/2),Vector3.up,Dyes[k],.6f);
                    rose.Quad(rc+new Vector3(-w/2+.15f,.002f,-d/2+.15f),rc+new Vector3(w/2-.15f,.002f,-d/2+.15f),rc+new Vector3(w/2-.15f,.002f,-d/2+.3f),rc+new Vector3(-w/2+.15f,.002f,-d/2+.3f),Vector3.up,Cream,.6f);}
                Shade(kit.Built("Floor detail the compass rose and rugs",rose,Cloth),false);
            }

            // ---------------------------------------------------------------- between the arcade and the gallery: the two forecourts and the spine's head
            static void Forecourts(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Forecourt furniture");var rng=new System.Random(53);
                // The cabins' side: a notice board of the settlements' forms (Abri's queue's paper, pinned up), benches, potted trees.
                var nb=At(-18,34.4f);s.Slot(0).Box(nb+new Vector3(0,1.3f,0),new Vector3(4.4f,2.2f,.2f),Quaternion.identity,new Color(.46f,.36f,.28f));
                for(int i=0;i<14;i++)s.Slot(1).Box(nb+new Vector3(-1.9f+(i%7)*.62f,.75f+(i/7)*1.05f+((i*5)%3)*.06f,-.12f),new Vector3(.46f,.62f,.02f),Quaternion.Euler(0,0,((i*13)%9)-4),Color.Lerp(new Color(.96f,.94f,.88f),Dyes[i%Dyes.Length],.18f));
                Solid(blocks,new Vector2(-18,34.4f),new Vector3(4.4f,2.2f,.4f));
                foreach(var (x,z,yaw) in new[]{(-24f,30f,90f),(-12f,30f,90f),(16f,31f,90f),(22f,30f,90f)}){Bench(s,At(x,z),yaw);Solid(blocks,new Vector2(x,z),new Vector3(.6f,.9f,2));}
                foreach(var (x,z) in new[]{(-26f,36f),(-10.4f,36f),(10.4f,36f),(25f,35f),(-26f,17.6f),(24f,17.6f)}){Planter(s,At(x,z),1.4f,(int)x%2==0?Teal:Cream,rng);Solid(blocks,new Vector2(x,z),new Vector3(1.4f,.9f,1.4f));}
                // The garden's side: a seed-tray cart and an Okhari herb stall.
                Stall("Herb stall",new Vector2(16.6f,35.2f),0,new Color(.46f,.62f,.34f),new Color(.86f,.62f,.3f),new Color(.5f,.74f,.38f),Leaf);
                Shade(kit.Built("Forecourts",s,Wood,Panel),true).transform.SetParent(blocks.transform,true);
                // The pipe that brings the raft bay's water round to the garden, along the arcade's back, on brackets. At the spine's head it goes down into the deck and
                // crosses under a line of cover plates: carried over the walk at 4.8 m it lay across the whole frame of anyone coming up to the gallery stair, in front of
                // the partner and over the floor sign (first player run, November 14: it stands halfway up the fixed camera's line to a hero five metres past it).
                var pipe=new ZoneKit.Shape();Color pipeDark=new(.36f,.5f,.48f),pipeLight=new(.4f,.56f,.52f);
                foreach(var path in new[]{new[]{At(-36,15.8f,2.6f),At(-9.6f,15.8f,2.6f),At(-9.2f,15.8f,2.2f),At(-9.2f,15.8f,-.1f)},
                    new[]{At(9.2f,15.8f,-.1f),At(9.2f,15.8f,2.2f),At(9.6f,15.8f,2.6f),At(29,16.6f,2.6f),At(29.6f,19.6f,.4f)}})
                {
                    pipe.Tube(path,path.Select(_=>.22f).ToArray(),10,pipeDark,pipeLight,1,.4f,false);
                    foreach(var at in path)if(at.y>2.5f)pipe.Box(at+Vector3.down*1.3f,new Vector3(.12f,2.6f,.12f),Quaternion.identity,Soot);
                }
                // Its way under the walk: plates a hand wide of the pipe, flush with the deck, a seam every two metres.
                for(float x=-8.4f;x<8.5f;x+=2.1f)pipe.Box(At(x,15.8f,.02f),new Vector3(1.96f,.04f,.72f),Quaternion.identity,x>-1&&x<1?pipeLight:pipeDark);
                Shade(kit.Built("The raft bay's water, piped to the garden",pipe,Panel),true);
                for(float z=17;z<=23;z+=6){LampPost("Spine head lamp",new Vector2(-6.4f,z),4,Bronze,z<20);LampPost("Spine head lamp",new Vector2(6.4f,z),4,Bronze,false);}
                kit.Scatter(rng,30,()=>new Vector2(-27+(float)rng.NextDouble()*53,16+(float)rng.NextDouble()*21),p=>!KeepsOff(p)&&Clear(p)&&Height(p)<.05f&&Mathf.Abs(p.x)>4,KeepsOff,
                    ("DressPlanter",1,true),("DressKiosk",1.5f,true),("DressFanShrub",1.2f,true),("DressSucculent",.8f,true),("DressLightBollard",1,true),("DeckBench",.9f,true));
            }

            /// <summary>Things that are there from a flag on (or until one): a FlagGate on an always-active holder.</summary>
            static void Gate(string name,GameObject[] targets,string flag,bool showWhenSet)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets;
                foreach(var t in targets)t.SetActive(!showWhenSet);
            }
        }
    }
}
