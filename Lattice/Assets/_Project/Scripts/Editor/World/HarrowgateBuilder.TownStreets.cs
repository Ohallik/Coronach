using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    public static partial class HarrowgateBuilder
    {
        internal static partial class Town
        {
            // ---------------------------------------------------------------- the streets the party walks (D371)
            /// <summary>The streets as a war camp's capital lives in them, along the ways the replay walks (the quay road, the cross street, the avenue up to the forecourt, the east
            /// gallery): market stalls under striped awnings in the houses' colours with their keepers and customers, provision carts, casks and crates, gutters with their grates,
            /// and people standing together. The whole-game capture of November 22 read the town as bare grey paving with lamp discs on it (s50/087, s51/136, s53/016). Every piece is
            /// checked as placed: its whole footprint off the ways, off the prompts and the camera's line to them, the signs, the errands, the drill, the field and its kerbs, and
            /// anything solid; what can't stand is left out and logged (HARROWGATE_STREETS_SKIP), never forced.</summary>
            static void Streets(ZoneKit kit)
            {
                Physics.SyncTransforms();
                Color wood=new(.58f,.46f,.34f),pale=new(.72f,.6f,.44f),iron=new(.3f,.3f,.32f);
                var shape=new ZoneKit.Shape();var holder=new GameObject("Town streets");int stalls=0,carts=0,piles=0,people=0;
                var standing=new List<(Vector3 at,Quaternion turn,Vector3 half)>();
                // Where a piece can't stand, it is tried a step and two along the street, and a step back from it, before it is left out.
                bool Find(ref Vector2 at,Quaternion turn,Vector2 half,string what)
                {
                    foreach(var o in new[]{Vector3.zero,new Vector3(1.4f,0,0),new Vector3(-1.4f,0,0),new Vector3(0,0,1),new Vector3(2.8f,0,0),new Vector3(-2.8f,0,0),new Vector3(1.4f,0,1),new Vector3(-1.4f,0,1)})
                    {var w=turn*o;var p=at+new Vector2(w.x,w.z);if(Clear(p,turn,half,what,false)){at=p;return true;}}
                    return Clear(at,turn,half,what,true);
                }
                bool Clear(Vector2 at,Quaternion turn,Vector2 half,string what,bool log)
                {
                    string why=null;float reach=Mathf.Max(half.x,half.y),y=kit.Y(at.x,at.y);
                    var corners=new[]{new Vector2(-half.x,-half.y),new Vector2(half.x,-half.y),new Vector2(half.x,half.y),new Vector2(-half.x,half.y)}.Select(c=>{var w=turn*new Vector3(c.x,0,c.y);return at+new Vector2(w.x,w.z);}).ToArray();
                    if(Land(at)<.6f+reach||corners.Any(c=>Mathf.Abs(kit.Y(c.x,c.y)-y)>.12f)||y>CliffTop)why="not on level floor";
                    else if(corners.Append(at).Any(c=>FromWays(c)<2.2f)||FromWays(at)<2.2f+Mathf.Min(half.x,half.y))why="on a way";
                    else if(Mathf.Abs(at.x)<FieldHalfX+4+reach&&at.y>FieldSouth-2-reach&&at.y<FieldNorth+9+reach)why="on the field, its kerbs or its stands";
                    else if(Stands.Any(s=>Vector2.Distance(s,at)<3+reach||(Mathf.Abs(at.x-s.x)<2+reach&&at.y<s.y+1&&at.y>s.y-7-reach)))why="by a prompt, or between the camera and one";
                    else if(Signs.Any(s=>Vector2.Distance(s.at,at)<s.half+1+reach))why="on a sign";
                    else if(Errands.Any(e=>Enumerable.Range(1,e.loop.Length-1).Any(i=>ToSegment(at,e.loop[i-1],e.loop[i])<1.8f+reach)))why="on an errand";
                    else if(Drill.Any(d=>ToSegment(at,d.a,d.b)<1.4f+reach))why="in the drill";
                    else if(Placed.Any(q=>Vector2.Distance(q,at)<1.2f+reach))why="on someone";
                    else if(Physics.OverlapBox(new Vector3(at.x,y+1.3f,at.y),new Vector3(half.x+.1f,1.1f,half.y+.1f),turn,~0,QueryTriggerInteraction.Ignore).FirstOrDefault() is Collider hit)
                        why="in something solid ("+(hit.transform.parent!=null?hit.transform.parent.name+"/":"")+hit.name+")";
                    else if(standing.Any(s=>Vector2.Distance(new Vector2(s.at.x,s.at.z),at)<Mathf.Max(s.half.x,s.half.z)+reach+.6f))why="on another piece";
                    if(why!=null){if(log)Debug.Log($"HARROWGATE_STREETS_SKIP {what} at ({at.x:0.0}, {at.y:0.0}): {why}");return false;}
                    Placed.Add(at);return true;
                }
                void Solid(Vector2 at,Quaternion turn,Vector3 size,string name)
                {
                    var c=kit.On(at.x,at.y,size.y*.5f);ZoneKit.Block(holder,c,size,turn.eulerAngles.y,name);standing.Add((c,turn,size*.5f));
                }
                void Person(string body,Vector3 at,float yaw,Color tint,string name,string doing="")
                {
                    var one=Body(kit,body,new Vector3(at.x,0,at.z),yaw,tint);one.name=name;Carry(kit,one,doing);one.transform.SetParent(holder.transform,true);people++;
                }
                // A market stall under a striped awning: four posts, the awning sloping to the street in two colours with a scalloped valance, a counter with the stall's wares on
                // it and its stock behind, its keeper behind the counter and, where there's room off the ways, a customer before it. The front faces the stall's turn's back.
                void Stall(Vector2 at,float yaw,Color one,Color two,string wares,string keeper,string customer)
                {
                    var turn=Quaternion.Euler(0,yaw,0);if(!Find(ref at,turn,new Vector2(1.75f,1.25f),"a stall"))return;
                    var foot=kit.On(at.x,at.y);Vector3 P(float x,float yy,float z)=>foot+turn*new Vector3(x,yy,z);int seed=(int)(at.x*13+at.y*7);
                    foreach(var (x,z) in new[]{(-1.55f,-1f),(1.55f,-1f),(-1.55f,.95f),(1.55f,.95f)})shape.Slot(0).Post(P(x,0,z),z<0?2.2f:2.62f,.06f,.05f,6,wood,pale);
                    shape.Slot(0).Beam(P(-1.62f,2.18f,-1f),P(1.62f,2.18f,-1f),.07f,.07f,wood).Beam(P(-1.62f,2.6f,.95f),P(1.62f,2.6f,.95f),.07f,.07f,wood);
                    float Roof(float x,float z,float x0){float t=Mathf.Clamp01((1.05f-z)/2.35f);return Mathf.Lerp(2.66f,2.22f,t)-.07f*Mathf.Sin(Mathf.Clamp01((x-x0)/.43f)*Mathf.PI)*Mathf.Sin(t*Mathf.PI);}
                    for(int k=0;k<8;k++)
                    {
                        float x0=-1.72f+k*.43f,x1=x0+.43f;var c=k%2==0?one:two;
                        for(int i=0;i<4;i++)
                        {
                            float z0=1.05f-i*.5875f,z1=z0-.5875f,xm=(x0+x1)*.5f;var tone=ZoneKit.Shape.Dim(c,.9f+.05f*((i+k+seed)%3));
                            shape.Slot(1).Quad(P(x0,Roof(x0,z0,x0),z0),P(xm,Roof(xm,z0,x0),z0),P(xm,Roof(xm,z1,x0),z1),P(x0,Roof(x0,z1,x0),z1),turn*Vector3.up,tone,.5f)
                                .Quad(P(xm,Roof(xm,z0,x0),z0),P(x1,Roof(x1,z0,x0),z0),P(x1,Roof(x1,z1,x0),z1),P(xm,Roof(xm,z1,x0),z1),turn*Vector3.up,tone,.5f);
                        }
                        shape.Slot(1).Triangle(P(x0,2.21f,-1.31f),P(x1,2.21f,-1.31f),P(x0+.215f,1.96f,-1.33f),turn*Vector3.back,ZoneKit.Shape.Dim(c,.86f),.5f);
                    }
                    // The counter: boards on trestles, a front board, the wares on top.
                    shape.Slot(0).Box(P(0,.92f,-.55f),new Vector3(3,.08f,.72f),turn,wood,pale,.5f).Box(P(0,.5f,-.9f),new Vector3(2.9f,.74f,.05f),turn,ZoneKit.Shape.Dim(wood,.82f),null,.5f);
                    foreach(float x in new[]{-1.3f,1.3f})shape.Slot(0).Box(P(x,.45f,-.55f),new Vector3(.08f,.9f,.6f),turn,ZoneKit.Shape.Dim(wood,.75f));
                    var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                    for(int k=0;k<5;k++)
                    {
                        float x=-1.15f+k*.58f;var top=P(x,.96f,-.55f);
                        switch(wares)
                        {
                            case "produce":{Color[] crop={new(.5f,.62f,.3f),new(.78f,.52f,.28f),new(.84f,.78f,.56f),new(.62f,.34f,.22f),new(.44f,.54f,.26f)};
                                shape.Slot(0).Tube(new[]{top,top+Vector3.up*.18f},new[]{.22f,.26f},10,ZoneKit.Shape.Dim(pale,.85f),pale,1,1);
                                shape.Slot(1).Lump(top+Vector3.up*.2f,new Vector3(.22f,.1f,.22f),seed+k,.45f,crop[k],ZoneKit.Shape.Dim(crop[k],.7f),8,3);break;}
                            case "cloth":{Color[] bolts={new(.74f,.5f,.18f),new(.16f,.42f,.44f),new(.86f,.82f,.72f),new(.3f,.44f,.22f),new(.24f,.3f,.52f)};
                                for(int b=0;b<2;b++)shape.Slot(1).Box(top+Vector3.up*(.06f+b*.11f),new Vector3(.44f,.1f,.34f),turn*Quaternion.Euler(0,(R()-.5f)*16,0),ZoneKit.Shape.Dim(bolts[(k+b*2)%5],.94f+.1f*b));break;}
                            case "iron":{if(k%2==0)CookPot(shape.Slot(3),top,.36f);else for(int b=0;b<3;b++)shape.Slot(3).Box(top+turn*new Vector3(-.12f+b*.12f,.03f,0),new Vector3(.06f,.04f,.56f),turn*Quaternion.Euler(0,(R()-.5f)*10,0),new Color(.62f,.62f,.64f),new Color(.78f,.78f,.8f));break;}
                            case "bread":{for(int b=0;b<3;b++)shape.Slot(1).Lump(top+turn*new Vector3(-.12f+b*.12f,.07f,(R()-.5f)*.2f),new Vector3(.13f,.08f,.2f),seed+k*3+b,.2f,new Color(.78f,.56f,.32f),new Color(.56f,.36f,.2f),8,3);break;}
                            default:{shape.Slot(0).Box(top+Vector3.up*.02f,new Vector3(.5f,.04f,.4f),turn,ZoneKit.Shape.Dim(wood,.9f));
                                for(int b=0;b<3;b++)shape.Slot(1).Lump(top+turn*new Vector3(-.12f+b*.12f,.08f,(R()-.5f)*.16f),new Vector3(.07f,.05f,.2f),seed+k*5+b,.2f,new Color(.62f,.68f,.74f),new Color(.42f,.46f,.52f),8,3);break;}
                        }
                    }
                    // Stock behind: sacks and a crate.
                    Sack(shape.Slot(2),P(-1.1f,0,.6f),.8f,new Color(.84f,.76f,.6f),seed+1);Sack(shape.Slot(2),P(-.55f,0,.7f),.7f,new Color(.8f,.72f,.56f),seed+2);
                    shape.Slot(0).Box(P(1f,.3f,.6f),new Vector3(.7f,.6f,.6f),turn*Quaternion.Euler(0,8,0),ZoneKit.Shape.Dim(wood,.9f),pale,.5f);
                    // Solid in two: the counter and the stock, with the keeper's place between them (one block round all of it held the keeper inside it, facing nothing a check
                    // could see).
                    ZoneKit.Block(holder,P(0,.55f,-.7f),new Vector3(3.3f,1.1f,.95f),yaw,"Town street stall");ZoneKit.Block(holder,P(0,.55f,.95f),new Vector3(3.3f,1.1f,.5f),yaw,"Town street stall");
                    standing.Add((kit.On(at.x,at.y,1.25f),turn,new Vector3(1.65f,1.25f,1.15f)));stalls++;
                    var face=yaw+180;Person(keeper,P(.2f,0,.36f),face,new Color(.96f+.04f*(seed%3),.96f,1f-.03f*(seed%2)),"Townsperson, keeping a stall");
                    if(!string.IsNullOrEmpty(customer)){var p=P(.5f,0,-2.05f);var flat=new Vector2(p.x,p.z);if(Free(kit,flat,2.4f,"a customer"))Person(customer,p,yaw,new Color(.94f,.96f,1f),"Townsperson, at a stall","a basket");}
                }
                // A provision cart: bed, sides, two wheels, shafts down, a load of sacks and a cask.
                void Cart(Vector2 at,float yaw,Color load)
                {
                    var turn=Quaternion.Euler(0,yaw,0);if(!Find(ref at,turn,new Vector2(.95f,1.5f),"a cart"))return;var f=kit.On(at.x,at.y);Vector3 P(float x,float yy,float z)=>f+turn*new Vector3(x,yy,z);
                    shape.Slot(0).Box(P(0,.76f,0),new Vector3(1.3f,.12f,2.1f),turn,wood,pale,.5f);
                    foreach(int side in new[]{-1,1})
                    {
                        shape.Slot(0).Box(P(side*.62f,1f,0),new Vector3(.06f,.38f,2.1f),turn,wood);
                        shape.Slot(3).Tube(new[]{P(side*.72f,.48f,-.15f),P(side*.84f,.48f,-.15f)},new[]{.48f,.48f},16,iron,ZoneKit.Shape.Dim(wood,.8f),1,1);
                        shape.Slot(0).Beam(P(side*.4f,.72f,1.05f),P(side*.32f,.08f,2.1f),.08f,.08f,wood);
                    }
                    int seed=(int)(at.x*11+at.y*3);for(int k=0;k<4;k++)Sack(shape.Slot(2),P(-.3f+(k%2)*.6f,.82f,-.6f+(k/2)*.55f),.62f,ZoneKit.Shape.Dim(load,.92f+.05f*k),seed+k);
                    shape.Slot(0).Tube(new[]{P(0,.82f,.65f),P(0,1.12f,.65f),P(0,1.42f,.65f)},new[]{.24f,.29f,.24f},12,wood,pale,2,1);
                    Solid(at,turn,new Vector3(1.8f,1.5f,2.9f),"Town street cart");carts++;
                }
                // Casks and crates stacked against a wall or a stall's side: what the street is supplied from.
                void Pile(Vector2 at,float yaw)
                {
                    var turn=Quaternion.Euler(0,yaw,0);if(!Find(ref at,turn,new Vector2(1f,.7f),"casks and crates"))return;var f=kit.On(at.x,at.y);Vector3 P(float x,float yy,float z)=>f+turn*new Vector3(x,yy,z);
                    foreach(var (x,z,tall) in new[]{(-.55f,.1f,.9f),(.05f,.2f,.84f)}){var b=P(x,0,z);shape.Slot(0).Tube(new[]{b,b+Vector3.up*tall*.5f,b+Vector3.up*tall},new[]{.3f,.36f,.3f},12,wood,pale,2,1);
                        foreach(float h in new[]{.14f,.86f})shape.Slot(3).Tube(new[]{b+Vector3.up*(tall*h-.03f),b+Vector3.up*(tall*h+.03f)},new[]{.33f,.33f},12,iron,iron,1,1,false);}
                    shape.Slot(0).Box(P(.66f,.32f,-.1f),new Vector3(.66f,.64f,.66f),turn*Quaternion.Euler(0,12,0),ZoneKit.Shape.Dim(wood,.86f),pale,.5f).Box(P(.62f,.86f,-.08f),new Vector3(.5f,.44f,.5f),turn*Quaternion.Euler(0,-6,0),ZoneKit.Shape.Dim(wood,.94f),pale,.5f);
                    Solid(at,turn,new Vector3(2f,1.2f,1.4f),"Town street stores");piles++;
                }
                // People standing together in the street, off the ways: a word on the day of a duel.
                void Group(Vector2 at,string a,string b,string doing)
                {
                    var p1=at+new Vector2(-.66f,0);var p2=at+new Vector2(.66f,.1f);
                    if(Free(kit,p1,2.4f,"a townsperson talking")&&Free(kit,p2,2.4f,"a townsperson talking"))
                    {Person(a,new Vector3(p1.x,0,p1.y),90,new Color(.96f,.94f,1f),"Townsperson, talking in the street",doing);Person(b,new Vector3(p2.x,0,p2.y),270,new Color(1f,.96f,.92f),"Townsperson, talking in the street");}
                }
                Color amber=new(.74f,.5f,.18f),teal=new(.16f,.42f,.44f),indigo=new(.24f,.3f,.52f),moss=new(.3f,.44f,.22f),rust=new(.6f,.26f,.14f),cream=new(.9f,.86f,.74f),crimson=new(.46f,.12f,.12f);
                // ---- the quay road, north side, fronts to the road (s53/016)
                Stall(new Vector2(-14.6f,14.4f),0,amber,cream,"fish","TovraBody","OdrunBody");
                Stall(new Vector2(-9.4f,15f),0,teal,cream,"bread","OdrunBody","BrekkBody");
                Stall(new Vector2(-4.4f,15.4f),0,crimson,cream,"produce","BrekkBody","");
                // and its south side, by the water, fronts to the road (the sentry at the gate's foot keeps the camera's line to him clear)
                Stall(new Vector2(2.4f,4.4f),180,moss,cream,"fish","DraskBody","TovraBody");Pile(new Vector2(7.4f,3.2f),0);
                Cart(new Vector2(-20.4f,13.2f),80,new Color(.82f,.76f,.6f));Pile(new Vector2(24.4f,13.4f),0);Group(new Vector2(-27.6f,13f),"DraskBody","TovraBody","a sack");
                // ---- the cross street on the lower terrace, south side, fronts to the street
                Stall(new Vector2(-8f,31.2f),180,indigo,cream,"cloth","TovraBody","OdrunBody");
                Stall(new Vector2(-14.4f,31.2f),180,moss,cream,"produce","DraskBody","");
                Stall(new Vector2(23.4f,31f),180,rust,cream,"iron","BrekkBody","TovraBody");
                Pile(new Vector2(-1.8f,31.4f),0);Cart(new Vector2(33.4f,31.4f),95,new Color(.6f,.5f,.36f));Group(new Vector2(-26.4f,31.6f),"OdrunBody","BrekkBody","a basket");
                // ---- the avenue up to the forecourt, both sides, fronts to it
                Stall(new Vector2(5f,46.6f),90,amber,new Color(.14f,.12f,.1f),"bread","OdrunBody","DraskBody");
                Stall(new Vector2(5f,51.8f),90,teal,cream,"iron","DraskBody","");
                Stall(new Vector2(-5.2f,50.4f),270,rust,cream,"produce","TovraBody","BrekkBody");
                Pile(new Vector2(5f,40.6f),90);Cart(new Vector2(12.4f,60.4f),90,new Color(.66f,.6f,.5f));Pile(new Vector2(16.6f,61.2f),0);Group(new Vector2(-5.6f,54.6f),"BrekkBody","OdrunBody","");
                // ---- the east gallery's square (s51/136)
                Stall(new Vector2(36f,97.6f),0,indigo,cream,"cloth","OdrunBody","TovraBody");
                Stall(new Vector2(28.4f,97f),0,amber,cream,"bread","BrekkBody","OdrunBody");Pile(new Vector2(25.4f,100.6f),0);Pile(new Vector2(39.6f,94.4f),0);Group(new Vector2(33.6f,95.4f),"TovraBody","DraskBody","");
                if(stalls+carts+piles>0)
                {
                    var drawn=kit.Built("Town streets drawn",shape,Timber,Cloth,Burlap,Iron);drawn.transform.SetParent(holder.transform,true);
                    foreach(var block in holder.GetComponentsInChildren<BoxCollider>().Where(b=>b.name.StartsWith("Town street")))block.transform.SetParent(drawn.transform,true);
                }
                Gutters(kit);
                Debug.Log($"HARROWGATE_STREETS stalls={stalls} carts={carts} piles={piles} people={people}");
            }
            /// <summary>Gutters along the streets the party walks, laid flush: a dark iron channel at the street's edge with a grate across it every three metres (the wall walk's own
            /// drain, FloorMarks), where the rain off the terraces runs to the harbour. Drawn only, "Floor detail" so the checks read them as flat.</summary>
            static void Gutters(ZoneKit kit)
            {
                var iron=new ZoneKit.Shape();Color ink=new(.12f,.12f,.14f),rail=new(.24f,.24f,.26f);
                void Band(Vector2 a,Vector2 b,float wide,Color colour,float lift)
                {
                    var along=(b-a).normalized;var side=new Vector2(-along.y,along.x)*wide*.5f;int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/1.5f));
                    for(int k=0;k<n;k++){var p0=Vector2.Lerp(a,b,k/(float)n);var p1=Vector2.Lerp(a,b,(k+1)/(float)n);
                        iron.Quad(kit.On(p0.x-side.x,p0.y-side.y,lift),kit.On(p1.x-side.x,p1.y-side.y,lift),kit.On(p1.x+side.x,p1.y+side.y,lift),kit.On(p0.x+side.x,p0.y+side.y,lift),Vector3.up,colour,.25f);}
                }
                void Gutter(Vector2 a,Vector2 b)
                {
                    Band(a,b,.34f,ink,.012f);var along=(b-a).normalized;var side=new Vector2(-along.y,along.x);
                    for(float s=1.2f;s<Vector2.Distance(a,b);s+=3){var p=a+along*s;Band(p-side*.22f,p+side*.22f,.09f,rail,.018f);}
                }
                // The cross street's south edge (round the gate's head), the avenue's two edges below the ramp, and the quay road's north edge.
                Gutter(new Vector2(-36,33.2f),new Vector2(4.6f,33.2f));Gutter(new Vector2(11.4f,33.2f),new Vector2(40,33.2f));
                Gutter(new Vector2(2.3f,37.2f),new Vector2(2.3f,55.2f));Gutter(new Vector2(-2.3f,37.2f),new Vector2(-2.3f,55.2f));
                Gutter(new Vector2(-38,11.8f),new Vector2(4.6f,11.8f));Gutter(new Vector2(11.4f,11.8f),new Vector2(30,11.8f));
                kit.Built("Floor detail street gutters",iron,Iron).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
    }
}
