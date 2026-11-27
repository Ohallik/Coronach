using Lattice.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Many Hands' second pass of detail (the lead's look at the first stills): the cabins' fronts as homes, the liner's own floor, the freight deck as a working one, the Ilu's
    /// goods on the north quay, and Driftreed's school raft once its bell is home.</summary>
    public static partial class ManyHandsBuilder
    {
        internal static partial class Hands
        {
            internal static readonly Vector2 SchoolRaft=new(-24,-41.4f);
            static Material Window;
            static void Detail(ZoneKit kit)
            {
                Window=ZoneKit.Toon("hands-window",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(.8f,.62f,.38f));});
                CabinFronts(kit);
                LinerFloor(kit);
                FreightDeck(kit);
                WorkingDeck(kit);
                QuayGoods(kit);
                SchoolRaftAboard(kit);
                Arrivals.Build(kit);
            }

            // ---------------------------------------------------------------- the cabins' fronts: a door with its step and awning, a lit window with a frame, a pot, per cabin
            static void CabinFronts(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(67);Color[] glass={new(1,.82f,.52f),new(.62f,.86f,.84f),new(1,.7f,.62f),new(.9f,.9f,.66f)};int n=0;
                foreach(var (z,x0,x1) in CabinRows)
                {
                    float deep=z>50?4:3,face=z-deep*.5f;int count=Mathf.RoundToInt((x1-x0)/3.4f);
                    for(int i=0;i<count;i++,n++)
                    {
                        float a=x0+(x1-x0)*i/count,b=x0+(x1-x0)*(i+1)/count,mid=(a+b)*.5f;if(z>50&&mid>-66&&mid<-58)continue;
                        var door=Paints[(n*5+2)%Paints.Length];var frame=new Color(.3f,.32f,.34f);
                        // The door, set in a frame, with its handle; the step before it; an awning over it in the household's own stripe.
                        s.Slot(0).Box(At(mid-.6f,face-.05f,1.08f),new Vector3(1.1f,2.16f,.08f),Quaternion.identity,frame).Box(At(mid-.6f,face-.1f,1),new Vector3(.86f,1.96f,.05f),Quaternion.identity,door,ZoneKit.Shape.Dim(door,1.1f));
                        s.Slot(0).Box(At(mid-.3f,face-.14f,1),new Vector3(.06f,.14f,.04f),Quaternion.identity,new Color(.86f,.7f,.4f));
                        s.Slot(0).Box(At(mid-.6f,face-.3f,.08f),new Vector3(1.2f,.16f,.5f),Quaternion.identity,new Color(.46f,.48f,.5f),new Color(.6f,.62f,.62f));
                        var stripe=Dyes[(n*3+1)%Dyes.Length];
                        for(int k=0;k<4;k++){float x=mid-1.3f+k*.35f;s.Slot(1).Quad(At(x,face-.05f,2.45f),At(x+.35f,face-.05f,2.45f),At(x+.35f,face-.75f,2.1f),At(x,face-.75f,2.1f),new Vector3(0,1,-.5f),k%2==0?stripe:Cream,.8f);}
                        // The window: a frame, lit glass in the household's own light, a cross bar, a sill with something growing on it.
                        var lit=glass[(n*7)%glass.Length];
                        s.Slot(0).Box(At(mid+.75f,face-.05f,1.55f),new Vector3(1,.8f,.08f),Quaternion.identity,frame).Slot(2).Box(At(mid+.75f,face-.1f,1.55f),new Vector3(.84f,.64f,.04f),Quaternion.identity,lit);
                        s.Slot(0).Box(At(mid+.75f,face-.13f,1.55f),new Vector3(.05f,.64f,.03f),Quaternion.identity,frame).Box(At(mid+.75f,face-.13f,1.55f),new Vector3(.84f,.05f,.03f),Quaternion.identity,frame);
                        s.Slot(0).Box(At(mid+.75f,face-.16f,1.18f),new Vector3(1.1f,.06f,.2f),Quaternion.identity,new Color(.6f,.5f,.4f));
                        s.Slot(3).Lump(At(mid+.75f,face-.18f,1.3f),new Vector3(.22f,.12f,.1f),n,.25f,new Color(.5f,.74f,.38f),new Color(.3f,.46f,.24f),8,4);
                        // A pot by most doors, a stool or a crate by some.
                        int what=rng.Next(4);var by=At(mid+.1f,face-.45f);
                        if(what<2){s.Slot(0).Tube(new[]{by,by+Vector3.up*.38f},new[]{.16f,.2f},10,new Color(.66f,.42f,.3f),new Color(.74f,.5f,.36f),1,1,false);s.Slot(3).Lump(by+Vector3.up*.46f,new Vector3(.2f,.18f,.2f),n,.3f,new Color(.46f,.7f,.36f),new Color(.3f,.46f,.24f),8,4);}
                        else if(what==2)s.Slot(0).Post(by,.42f,.15f,.15f,8,new Color(.6f,.46f,.32f),new Color(.7f,.54f,.38f));
                    }
                }
                Shade(kit.Built("Cabin fronts",s,Panel,Cloth,Window,Leaf),true);
            }

            // ---------------------------------------------------------------- the liner's own floor: medallions where ways cross, brass inlay down each side
            static void LinerFloor(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();
                void Medallion(Vector2 at,float radius,int seed)
                {
                    var c=At(at,.021f);
                    for(int ring=0;ring<4;ring++)
                    {
                        float r0=radius*(1-ring*.22f),r1=r0-radius*.12f;Color tone=ring%2==0?Teal:Cream;if(ring==3)tone=new Color(.86f,.7f,.4f);
                        for(int k=0;k<36;k++){float a=k*Mathf.PI/18,b=(k+1)*Mathf.PI/18;s.Quad(c+new Vector3(Mathf.Sin(a)*r1,0,Mathf.Cos(a)*r1),c+new Vector3(Mathf.Sin(b)*r1,0,Mathf.Cos(b)*r1),c+new Vector3(Mathf.Sin(b)*r0,0,Mathf.Cos(b)*r0),c+new Vector3(Mathf.Sin(a)*r0,0,Mathf.Cos(a)*r0),Vector3.up,(k+ring+seed)%6==0?new Color(.7f,.5f,.36f):tone,.5f);}
                    }
                    for(int k=0;k<8;k++){float a=k*Mathf.PI/4;var tip=c+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*radius*.3f;var l=c+new Vector3(Mathf.Sin(a-.4f),0,Mathf.Cos(a-.4f))*radius*.1f;var r=c+new Vector3(Mathf.Sin(a+.4f),0,Mathf.Cos(a+.4f))*radius*.1f;
                        s.Triangle(l,tip,c+Vector3.up*.001f,Vector3.up,new Color(.86f,.7f,.4f),.5f).Triangle(c+Vector3.up*.001f,tip,r,Vector3.up,new Color(.66f,.5f,.3f),.5f);}
                }
                Medallion(Crossing,3.4f,0);
                foreach(float x in new[]{-26f,-14f,16f,30f,44f})Medallion(new Vector2(x,-2.6f),1.3f,(int)x);
                // Brass inlay down both sides of the promenade and round the crossing: a pair of thin lines .3 m apart with the floor between them, and a bar across both where each
                // run ends. It was one line .1 m wide whose vertex brass on the brass paint came out .79/.52/.17, a danger marking's orange (D217) that crossed the concourse from the
                // frame's edge and stopped in the open (W8-many-hands-02, nov26-r7-chain s30/423). Now it is the paint's own brass a little dulled, paired, and ended (a pointed end
                // read as an arrow on the floor).
                // The paint's own brass a little dulled (.84/.84/.86 on it) still drew 250/174/65 in the player, as saturated as a danger marking: the concourse's warm light and
                // the grade lift red and drop blue (the twelfth look, nov26-r8-chain s30/419). The inlay's tone is now a khaki on the paint, which they draw as a dull gold darker
                // than the lit floor.
                var inlay=new Color(.58f,.70f,1f);
                void Inlay(Vector2 a,Vector2 b)
                {
                    var along=(b-a).normalized;var across=new Vector2(-along.y,along.x);float run=Vector2.Distance(a,b);
                    for(float t=0;t<run-.01f;t+=2)
                    {
                        var p0=a+along*t;var p1=a+along*Mathf.Min(run,t+2);
                        foreach(var (from,to) in new[]{(-.18f,-.12f),(.12f,.18f)})s.Quad(At(p0+across*from,.02f),At(p1+across*from,.02f),At(p1+across*to,.02f),At(p0+across*to,.02f),Vector3.up,inlay,.5f);
                    }
                    foreach(var (e0,e1) in new[]{(a-along*.1f,a+along*.04f),(b-along*.04f,b+along*.1f)})s.Quad(At(e0-across*.3f,.022f),At(e1-across*.3f,.022f),At(e1+across*.3f,.022f),At(e0+across*.3f,.022f),Vector3.up,inlay,.5f);
                }
                foreach(float z in new[]{-6.8f,9.8f})Inlay(new Vector2(-35.5f,z),new Vector2(52.5f,z));
                foreach(float x in new[]{-7.6f,7.6f})Inlay(new Vector2(x,-6.8f),new Vector2(x,9.8f));
                Shade(kit.Built("Floor detail the liner's medallions and inlay",s,Brass),false);
            }

            // ---------------------------------------------------------------- the freight deck as a working one
            static void FreightDeck(ZoneKit kit)
            {
                var paint=new ZoneKit.Shape();
                // Lane paint down the spine: a dashed centre line and the walkway's edge, both kerbs; tie-down rings in rows across the hauler's plate.
                for(float z=-52;z<-12;z+=3)paint.Quad(At(-.08f,z,.019f),At(.08f,z,.019f),At(.08f,z+1.6f,.019f),At(-.08f,z+1.6f,.019f),Vector3.up,Cream,.5f);
                foreach(float x in new[]{-5.2f,5.2f})paint.Quad(At(x-.1f,-52.4f,.019f),At(x+.1f,-52.4f,.019f),At(x+.1f,-12.4f,.019f),At(x-.1f,-12.4f,.019f),Vector3.up,new Color(.46f,.7f,.5f),.5f);
                void Rings(float x0,float x1,float z0,float z1,float step)
                {
                    for(float x=x0;x<=x1;x+=step)for(float z=z0;z<=z1;z+=step)
                    {
                        var p=new Vector2(x,z);if(KeepsOff(p)&&FromWays(p)<1.2f)continue;var c=At(p,.02f);
                        paint.Disc(c,Vector3.up,.17f,10,new Color(.2f,.2f,.22f),.1f,new Color(.36f,.36f,.38f)).Disc(c+Vector3.up*.001f,Vector3.up,.1f,8,new Color(.14f,.14f,.15f));
                    }
                }
                Rings(15,49,-49,-21,4);Rings(-12.6f,-8.4f,-50,-14,4.5f);Rings(-34,10,-64.6f,-57,5);Rings(14,49,-64.6f,-58,5);Rings(-76,-38,-64.6f,-52,5);
                Shade(kit.Built("Floor detail freight lanes and rings",paint,Panel),false);
                // Pallet stacks: slatted pallets with their loads strapped on, in the hall's corners (clear of the fight's floor), on the quay side of the spine, in the yard.
                var stacks=new ZoneKit.Shape();var held=new GameObject("Pallet stacks");var rng=new System.Random(73);int k=0;
                foreach(var (x,z,yaw,high) in new[]{(16.4f,-20.6f,0f,3),(47.6f,-20.4f,90f,2),(47.6f,-49.4f,0f,3),(-11.6f,-38.2f,90f,2),(-11.8f,-17.4f,0f,2),(40.4f,-57.4f,0f,2),(-33.6f,-55.2f,20f,3),(-70,-54.4f,0f,2)})
                {
                    var p=new Vector2(x,z);var foot=At(p);var t=Quaternion.Euler(0,yaw,0);
                    for(int h=0;h<high;h++)
                    {
                        var at=foot+Vector3.up*h*.62f;stacks.Slot(0).Box(at+Vector3.up*.07f,new Vector3(1.2f,.14f,1),t,new Color(.62f,.48f,.32f),new Color(.74f,.58f,.4f),.5f);
                        var load=Paints[(k++*7)%Paints.Length];stacks.Slot(1).Box(at+Vector3.up*.38f,new Vector3(1.1f,.48f,.9f),t*Quaternion.Euler(0,rng.Next(-8,8),0),load,ZoneKit.Shape.Dim(load,1.12f),.5f);
                        stacks.Slot(2).Box(at+Vector3.up*.38f,new Vector3(.06f,.5f,.94f),t,Soot);
                    }
                    ZoneKit.Block(held,foot+Vector3.up*high*.31f,new Vector3(1.3f,high*.62f,1.1f),yaw,"Pallet stack");
                }
                Shade(kit.Built("Pallet stacks",stacks,Wood,Panel,Panel),true).transform.SetParent(held.transform,true);
                // Two parked loaders: a chassis, a cab with its lamp, forks lowered.
                var loaders=new ZoneKit.Shape();var parked=new GameObject("Parked loaders");
                foreach(var (x,z,yaw,c) in new[]{(44.4f,-57.2f,-90f,Hazard),(-29.6f,-55.6f,70f,new Color(.36f,.6f,.66f))})
                {
                    var foot=At(x,z);var t=Quaternion.Euler(0,yaw,0);
                    loaders.Slot(0).Box(foot+t*new Vector3(0,.6f,0),new Vector3(1.4f,.7f,2.4f),t,c,ZoneKit.Shape.Dim(c,1.1f),.5f).Box(foot+t*new Vector3(0,1.5f,-.4f),new Vector3(1.3f,1.1f,1.2f),t,ZoneKit.Shape.Dim(c,.9f));
                    loaders.Slot(1).Box(foot+t*new Vector3(0,1.5f,.22f),new Vector3(1.1f,.8f,.04f),t,new Color(.2f,.26f,.3f));
                    foreach(float side in new[]{-.4f,.4f})loaders.Slot(1).Box(foot+t*new Vector3(side,.12f,1.9f),new Vector3(.14f,.06f,1.3f),t,Soot).Box(foot+t*new Vector3(side,1.2f,1.25f),new Vector3(.1f,2.2f,.1f),t,Soot);
                    foreach(float wx in new[]{-.72f,.72f})foreach(float wz in new[]{-.8f,.8f}){var w=foot+t*new Vector3(wx,.3f,wz);loaders.Slot(1).Tube(new[]{w-t*Vector3.right*.12f,w+t*Vector3.right*.12f},new[]{.3f,.3f},12,Soot,Soot,1,1);}
                    loaders.Slot(2).Lump(foot+t*new Vector3(0,2.15f,-.4f),new Vector3(.12f,.08f,.12f),3,.02f,Color.white,Color.white,6,4);
                    ZoneKit.Block(parked,foot+Vector3.up*1,new Vector3(1.6f,2,3.2f),yaw,"Loader");
                }
                Shade(kit.Built("Parked loaders",loaders,Panel,Panel,Lamp),true).transform.SetParent(parked.transform,true);
            }

            // ---------------------------------------------------------------- the spine's lanes, as a working deck carries them (D370)
            /// <summary>Between the spine's centre line and its kerbs, down to its foot inside the dock door, the deck was wide bare plating from the play camera (nov20-whole
            /// s44/092, s55/014; the dock's own plate beyond the door has its painted bays, rings and cargo, and the five spots tried for hatches there all had something solid at a body's height, d370-b): flush hatches to the holds under the deck in a striped border, hinged, slotted and numbered; an arrow down each lane; tie-down points along both lanes'
            /// edges; lashing lockers on the kerbs with straps coiled on them. The hatches and the paint are flat, off the ways by the ways' own margin and off every spot and
            /// sign; the lockers are solid, off the ways as anything put down is, where no route of the whole-game capture walked (nov22b-whole s27 to s62).</summary>
            static void WorkingDeck(ZoneKit kit)
            {
                var flat=new ZoneKit.Shape();int placed=0,refused=0;string refusedAt="";
                bool Open(Vector2 c,float half)
                {
                    for(int i=0;i<9;i++){var p=c+new Vector2((i%3-1)*half,(i/3-1)*half);if(FromWays(p)<1.8f||OnASign(p)||p.y<ZMin+2.5f)return false;}
                    foreach(var spot in Spots)if(Vector2.Distance(c,spot)<half+3.4f)return false;
                    return true;
                }
                void Patch(Vector2 c,float x0,float x1,float z0,float z1,float up,Color tone)=>flat.Quad(At(c.x+x0,c.y+z0,up),At(c.x+x1,c.y+z0,up),At(c.x+x1,c.y+z1,up),At(c.x+x0,c.y+z1,up),Vector3.up,tone,.5f);
                Color cover=new(.54f,.56f,.58f),frame=new(.26f,.26f,.28f);
                void Hatch(Vector2 c)
                {
                    const float h=.72f,rim=.12f,outer=h+rim+.16f;
                    if(!Open(c,outer)||!Clear(c)){refused++;refusedAt+=$" hatch({c.x:0.#},{c.y:0.#}){(Open(c,outer)?" not clear":"")}";return;}
                    // The border in hazard stripes as the rails' kick plates are, the dark frame, the cover a cooler grey than the plate round it.
                    for(int k=0;k<8;k++){float a=-outer+k*outer/4,b=a+outer/4;var tone=k%2==0?Hazard:Soot;Patch(c,a,b,-outer,-h-rim,.021f,tone);Patch(c,a,b,h+rim,outer,.021f,tone);}
                    for(int k=0;k<6;k++){float a=-h-rim+k*(h+rim)/3,b=a+(h+rim)/3;var tone=k%2==0?Soot:Hazard;Patch(c,-outer,-h-rim,a,b,.021f,tone);Patch(c,h+rim,outer,a,b,.021f,tone);}
                    Patch(c,-h-rim,h+rim,-h-rim,-h,.022f,frame);Patch(c,-h-rim,h+rim,h,h+rim,.022f,frame);Patch(c,-h-rim,-h,-h,h,.022f,frame);Patch(c,h,h+rim,-h,h,.022f,frame);
                    Patch(c,-h,h,-h,h,.022f,cover);
                    // Its two hinges at the back, the lifting slot at the front, and the hold's number in the stencilled blocks the cargo bays are numbered in.
                    foreach(float x in new[]{-.42f,.42f})flat.Box(At(c.x+x,c.y+h-.07f,.045f),new Vector3(.34f,.05f,.12f),Quaternion.identity,Soot,frame);
                    Patch(c,-.24f,.24f,-h+.1f,-h+.2f,.024f,Soot);
                    for(int k=0;k<1+placed%3;k++)Patch(c,-.36f+k*.26f,-.2f+k*.26f,-.28f,.3f,.024f,Cream);
                    placed++;
                }
                void Arrow(Vector2 c,float north)
                {
                    if(!Open(c,.9f)){refused++;return;}
                    Patch(c,-.11f,.11f,north>0?-.9f:-.2f,north>0?.2f:.9f,.021f,Cream);
                    var tip=At(c.x,c.y+.9f*north,.021f);flat.Triangle(At(c.x-.42f,c.y+.2f*north,.021f),At(c.x+.42f,c.y+.2f*north,.021f),tip,Vector3.up,Cream);
                }
                // The spine: a hatch every nine metres or so, side to side; between them each lane's arrow, the east lane up to the arcade and the west lane down to the dock.
                foreach(var at in new[]{new Vector2(3.4f,-49.5f),new Vector2(-3.4f,-40.5f),new Vector2(3.4f,-35),new Vector2(-3.4f,-30.5f),new Vector2(3.4f,-27),new Vector2(-3.4f,-18.5f),new Vector2(3.4f,-15)})Hatch(at);
                foreach(float z in new[]{-44.5f,-31f,-21f})Arrow(new Vector2(3.4f,z),1);
                foreach(float z in new[]{-35.5f,-27.3f,-14.2f})Arrow(new Vector2(-3.4f,z),-1);
                // Tie-down points down both lanes' edges, inside the green lines, a pace apart from each other and from the hatches.
                foreach(float x in new[]{-4.75f,4.75f})for(float z=-51;z<=-13;z+=3)
                {
                    var p=new Vector2(x,z);if(FromWays(p)<1.8f||OnASign(p))continue;bool byHatch=false;
                    foreach(var hz in x<0?new[]{-40.5f,-30.5f,-18.5f}:new[]{-49.5f,-35f,-27f,-15f})if(Mathf.Abs(z-hz)<1.4f)byHatch=true;if(byHatch)continue;
                    var c=At(p,.02f);flat.Box(c+Vector3.up*.01f,new Vector3(.36f,.02f,.36f),Quaternion.identity,frame).Disc(c+Vector3.up*.022f,Vector3.up,.13f,10,new Color(.62f,.62f,.6f),.08f,new Color(.7f,.7f,.68f));
                }
                Shade(kit.Built("Floor detail the spine's and the dock's hatches, lane arrows and tie-downs",flat,Panel),false);
                // Lashing lockers on the kerbs, each in a hull's paint with its lid, hasps and a strap coiled on it: solid, knee-high, between the lane's edge and the kerb's stripes.
                var lockers=new ZoneKit.Shape();var held=new GameObject("Lashing lockers");int n=0;
                foreach(var (x,z) in new[]{(-6.05f,-33.5f),(6.05f,-38f),(6.05f,-25f),(6.05f,-14.5f)})
                {
                    var p=new Vector2(x,z);if(KeepsOff(p)||KeepsOff(p+Vector2.up*.9f)||KeepsOff(p-Vector2.up*.9f)||!Clear(p)){refused++;continue;}
                    var paint=Paints[(n*5+4)%Paints.Length];var foot=At(p);float side=x<0?1:-1;n++;
                    lockers.Slot(0).Box(foot+Vector3.up*.27f,new Vector3(.7f,.54f,1.8f),Quaternion.identity,paint,ZoneKit.Shape.Dim(paint,1.1f)).Box(foot+Vector3.up*.57f,new Vector3(.76f,.08f,1.86f),Quaternion.identity,ZoneKit.Shape.Dim(paint,.8f),ZoneKit.Shape.Dim(paint,1.15f));
                    lockers.Slot(1).Box(foot+Vector3.up*.03f,new Vector3(.74f,.06f,1.84f),Quaternion.identity,Soot);
                    foreach(float dz in new[]{-.5f,.5f})lockers.Slot(1).Box(foot+new Vector3(side*.37f,.44f,dz),new Vector3(.04f,.14f,.1f),Quaternion.identity,new Color(.8f,.78f,.7f));
                    for(int k=0;k<1+n%3;k++)lockers.Slot(1).Box(foot+new Vector3(side*.36f,.24f,-.2f+k*.16f),new Vector3(.02f,.22f,.08f),Quaternion.identity,Cream);
                    // The strap, coiled flat on the lid: three turns, a tail hanging over the lane side.
                    var lid=foot+Vector3.up*.63f+Vector3.forward*.3f;Color strap=n%2==0?new Color(.94f,.6f,.2f):Hazard;
                    for(int turn=0;turn<3;turn++){var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12,r=.26f-turn*.06f;coil[k]=lid+new Vector3(Mathf.Cos(a)*r,turn*.025f,Mathf.Sin(a)*r);thick[k]=.025f;}lockers.Slot(2).Tube(coil,thick,4,strap,strap,1,1,false);}
                    lockers.Slot(2).Tube(new[]{lid+new Vector3(side*.26f,0,0),foot+new Vector3(side*.4f,.6f,.3f),foot+new Vector3(side*.42f,.3f,.36f)},new[]{.022f,.022f,.022f},4,strap,strap,1,1,false);
                    ZoneKit.Block(held,foot+Vector3.up*.31f,new Vector3(.76f,.62f,1.86f),0,"Lashing locker");
                }
                if(!lockers.Empty)Shade(kit.Built("Lashing lockers",lockers,Panel,Panel,Cloth),true).transform.SetParent(held.transform,true);
                Debug.Log($"MANYHANDS_WORKING_DECK hatches={placed} lockers={n} refused={refused}{refusedAt}");
            }

            // ---------------------------------------------------------------- the Ilu's goods along the north quay
            static void QuayGoods(ZoneKit kit)
            {
                var goods=new ZoneKit.Shape();var held=new GameObject("Quay goods");var rng=new System.Random(79);int kept=0;
                for(int tries=0;tries<260&&kept<34;tries++)
                {
                    var p=new Vector2(-77+(float)rng.NextDouble()*40,-9+(float)rng.NextDouble()*24);int kind=rng.Next(5);float size=.55f+(float)rng.NextDouble()*.4f,yaw=(float)rng.NextDouble()*360;
                    if(Hold(p)<RimWide+.8f||KeepsOff(p)||kind==4&&FromWays(p)<2.1f||!Clear(p)||Height(p)>.05f)continue;
                    kept++;var foot=At(p);var turn=Quaternion.Euler(0,yaw,0);
                    if(kind==0){goods.Slot(0).Tube(new[]{foot,foot+Vector3.up*size*.75f},new[]{size*.45f,size*.52f},11,new Color(.78f,.66f,.44f),new Color(.95f,.84f,.58f),2,1,false);
                        goods.Slot(1).Lump(foot+Vector3.up*size*.72f,new Vector3(size*.46f,size*.2f,size*.46f),kept,.25f,Dyes[kept%Dyes.Length],ZoneKit.Shape.Dim(Dyes[kept%Dyes.Length],.6f),8,4);}
                    else if(kind==1){goods.Slot(2).Lump(foot+Vector3.up*size*.48f,new Vector3(size*.4f,size*.5f,size*.4f),kept,.04f,new Color(.74f,.5f,.36f),new Color(.5f,.32f,.24f),12,7);}
                    else if(kind==2){for(int k=0;k<9;k++){var lean=turn*new Vector3((k%3-1)*.12f,0,(k/3-1)*.12f);goods.Slot(0).Tube(new[]{foot+lean,foot+lean*1.6f+Vector3.up*(1.3f+.1f*(k%2))},new[]{.035f,.025f},4,new Color(.66f,.62f,.4f),new Color(.86f,.8f,.5f),1,.8f);}}
                    else if(kind==3){for(int turnNo=0;turnNo<3;turnNo++){var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;coil[k]=foot+new Vector3(Mathf.Cos(a)*(.5f-turnNo*.1f),.06f+turnNo*.09f,Mathf.Sin(a)*(.5f-turnNo*.1f));thick[k]=.05f;}goods.Slot(1).Tube(coil,thick,5,Straw,Straw,1,1,false);}}
                    else{var tone=Dyes[kept%Dyes.Length];goods.Slot(1).Box(foot+Vector3.up*.3f,new Vector3(2.2f,.6f,.7f),turn,tone,ZoneKit.Shape.Dim(tone,1.1f),.6f);}
                    ZoneKit.Block(held,foot+Vector3.up*.35f,new Vector3(size*.9f+(kind==4?1.4f:0),.7f,size*.9f),yaw,"Quay goods");
                }
                Shade(kit.Built("Quay goods",goods,Weave,Cloth,Soil),true).transform.SetParent(held.transform,true);
            }

            // ---------------------------------------------------------------- Driftreed's school raft, here once its bell is home (chapter 4: DovBellReturn, dov.bellReported)
            static void SchoolRaftAboard(ZoneKit kit)
            {
                var raft=new GameObject("Driftreed's school raft");var rng=new System.Random(83);float up=-Height(SchoolRaft);
                var deck=new ZoneKit.Shape();var c=new Vector3(SchoolRaft.x,0,SchoolRaft.y);
                deck.Slot(0).Deck(c+new Vector3(0,0,-2.4f),c+new Vector3(0,0,2.4f),6.4f,.18f,Straw,rng,.5f,.04f);
                for(int k=0;k<9;k++)deck.Slot(1).Tube(new[]{c+new Vector3(-3.2f+k*.8f,-.3f,-2.6f),c+new Vector3(-3.2f+k*.8f,-.3f,2.6f)},new[]{.22f,.22f},7,new Color(.8f,.72f,.5f),new Color(.9f,.82f,.56f),1,.4f);
                // The bell frame with its bell back in it, rung true.
                deck.Slot(2).Post(c+new Vector3(2.2f,0,1.4f),2.2f,.07f,.06f,6,new Color(.5f,.4f,.3f),new Color(.7f,.56f,.4f)).Post(c+new Vector3(3,0,1.4f),2.2f,.07f,.06f,6,new Color(.5f,.4f,.3f),new Color(.7f,.56f,.4f))
                    .Beam(c+new Vector3(2.1f,2.15f,1.4f),c+new Vector3(3.1f,2.15f,1.4f),.08f,.08f,new Color(.5f,.4f,.3f)).Slot(3).Lump(c+new Vector3(2.6f,1.8f,1.4f),new Vector3(.24f,.3f,.24f),3,.02f,new Color(.92f,.74f,.42f),new Color(.7f,.5f,.28f),12,6);
                var view=kit.Built("The school raft",deck,Weave,Weave,Wood,Brass);view.transform.SetParent(raft.transform,true);
                ZoneKit.Block(view,c+Vector3.down*.1f,new Vector3(6.4f,.2f,4.8f),0,"School raft deck");
                // Iri and three children of the raft, facing her, each in a different coat.
                var iri=kit.Townsperson("OnaBody",new Vector3(SchoolRaft.x+1,up,SchoolRaft.y+1),200,new Color(.9f,1,1.08f),"Neve");iri.name="Townsperson, Iri teaching on the school raft";iri.transform.SetParent(raft.transform,true);
                foreach(var (body,dx,dz,yaw,tint) in new[]{("PimBody",-1.2f,-.8f,20f,new Color(1.08f,.96f,.9f)),("MaruBody",-.2f,-1.2f,30f,new Color(.92f,1.06f,1f)),("DovBody",-2,-.2f,15f,new Color(1,.94f,1.1f))})
                {var child=kit.Townsperson(body,new Vector3(SchoolRaft.x+dx,up,SchoolRaft.y+dz),yaw,tint,"Neve");child.transform.localScale=Vector3.one*.74f;child.name="A child on the school raft";child.transform.SetParent(raft.transform,true);}
                Gate("The school raft came with the convoy",new[]{raft},"dov.bellReported",true);
            }
        }
    }
}
