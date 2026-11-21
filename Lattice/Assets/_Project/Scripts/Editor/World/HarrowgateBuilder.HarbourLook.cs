using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>What the War Harbour is drawn as, after the lead's second look (November 14: "a blockout: bare grey water and brick-textured slabs"). The harbour of a fortress town on a
    /// volcanic world, all of it under the plane but the cranes: the town's sea wall below the field to the west with its towers, braziers and the houses crowding its walk; the quay the
    /// party lifts off, under the launch; the flak mole with its two gun pits and a light on its head; finger piers; the east shore of black basalt in columns, with the shore batteries'
    /// pits and a seep of melt steaming into the sea; the breakwater; the slip up to the carrier's stern; moored hulls with their lines, lighters, channel buoys; and water that reads as
    /// water at the flight camera's scale (wavelets, glints, foam at every edge and round every hull, the lamps lying on it). Built in the town's own stone (its masonry, flags, iron,
    /// timber and lamps), so the harbour and the town above it are one place. Nothing here is solid: the cranes' legs keep their boxes (<see cref="Crane"/>).</summary>
    public static partial class HarrowgateBuilder
    {
        static Material HWater,HBed,HMasonry,HFlags,HIron,HTimber,HLamp,HMelt,HCloth,HRope,HGlow,HHull,HDeck,HSlip,HArc,HArcGlow,HGlint,HPool;
        /// <summary>Every light whose light lies on the water (its head, its colour, how wide its streak runs, how bright), gathered while the harbour is built and laid on the water by
        /// <see cref="HarbourGlints"/> once everything stands.</summary>
        static readonly List<(Vector3 head,Color tone,float spread,float strength,float shiftX)> Reflections=new();
        /// <summary>Light a lamp throws onto the water near it (where, how wide, its colour and strength in alpha): painted, since a water sheet takes six lights at most.</summary>
        static readonly List<(Vector3 at,float radius,Color tone)> Pools=new();
        /// <summary>A lamp's halo: a soft flare turned to the flight camera, so a head of light reads from thirty metres up.</summary>
        static void Halo(Transform parent,Vector3 head,float size,Material look)
        {
            var glow=GameObject.CreatePrimitive(PrimitiveType.Quad);glow.name="Lamp halo";UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());glow.transform.SetParent(parent,false);
            glow.transform.position=head;glow.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);glow.transform.localScale=Vector3.one*size;
            var view=glow.GetComponent<Renderer>();view.sharedMaterial=look;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
        /// <summary>The town's materials (made by its builder and saved), and the harbour's own water.</summary>
        static void HarbourMaterials(ZoneKit kit)
        {
            Reflections.Clear();Pools.Clear();
            Material Town(string name,Func<Material> made)=>ZoneKit.WorldMaterial(name)??made();
            HMasonry=Town("harrow-masonry",()=>ZoneKit.Toon("vaskar-harbour-masonry",null,m=>ZoneKit.Tinted(m)));HFlags=Town("harrow-flags",()=>ZoneKit.Toon("vaskar-harbour-flags",null,m=>ZoneKit.Tinted(m)));
            HIron=Town("harrow-iron",()=>ZoneKit.Toon("vaskar-harbour-iron",null,m=>ZoneKit.Tinted(m)));HTimber=Town("harrow-timber",()=>ZoneKit.Toon("vaskar-harbour-timber",null,m=>ZoneKit.Tinted(m)));
            HLamp=Town("harrow-lamp",()=>ZoneKit.Toon("vaskar-harbour-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.82f,.5f));m.SetColor("_EmissionColor",new Color(2.1f,1.4f,.55f));}));
            HCloth=Town("harrow-cloth",()=>ZoneKit.Toon("vaskar-harbour-cloth",null,m=>ZoneKit.Tinted(m)));HRope=Town("harrow-rope",()=>ZoneKit.Toon("vaskar-harbour-rope",null,m=>m.SetColor("_BaseColor",new Color(.78f,.72f,.6f))));
            // Melt where it meets the sea: a deep red, not a marking's orange.
            HMelt=ZoneKit.Toon("vaskar-harbour-melt",null,m=>{m.SetColor("_BaseColor",new Color(.4f,.1f,.06f));m.SetColor("_EmissionColor",new Color(.8f,.14f,.05f));});
            // Plain plate for the lighters' hulls, toned in the vertices.
            HHull=ZoneKit.Toon("vaskar-harbour-hull",null,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);});
            // The water seen from thirty metres up, as the town's painters would show it, in four kinds laid by where the harbour puts them (HarbourSurface): the open stream's
            // chop, glassy water in the lee, the current's streaks behind whatever is moored in it, and lace where it breaks on stone. The first harbour's water was one soft swell
            // and read as grey fog; the second (harrow-look2-04) was one crest pattern repeated every ten metres from wall to wall, like wallpaper, with the toon water's own glints
            // lying on it as camouflage. The glints are off; every crest is painted.
            // The chop: short bright crests, each an arch opening toward the camera, a darker trough under it, over broad swells. Twenty metres a repeat, and inside it the crests
            // come in gusts: big and crowded in one patch, small and few in the next, so no two frames of the stream are alike. Fifty texels a metre, crisp at the screen's pixel.
            var chop=ZoneKit.Texture("Vaskar/harbour-chop",1024,(u,v)=>
            {
                const int N=24;float gx=u*N,gy=v*N;int cx=Mathf.FloorToInt(gx),cy=Mathf.FloorToInt(gy);float crest=0,trough=0;
                for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
                {
                    int ix=cx+ox,iy=cy+oy,wx=((ix%N)+N)%N,wy=((iy%N)+N)%N;
                    float gust=ZoneKit.Smooth(.36f,.64f,ZoneKit.Cloud((wx+.5f)/N,(wy+.5f)/N,3,150));if(ZoneKit.Tile(wx,wy,N,121)>.22f+.78f*gust)continue;
                    float px=ix+.15f+.7f*ZoneKit.Tile(wx,wy,N,122),py=iy+.15f+.7f*ZoneKit.Tile(wx,wy,N,123),size=ZoneKit.Tile(wx,wy,N,124)*(.55f+.45f*gust);
                    float r=.16f+.34f*size,thick=.028f+.03f*size,open=.12f+.38f*ZoneKit.Tile(wx,wy,N,125),bright=.36f+.3f*ZoneKit.Tile(wx,wy,N,126);
                    float dx=gx-px,dy=gy-py,d=Mathf.Sqrt(dx*dx+dy*dy);if(d<.001f)continue;float up=dy/d;
                    float arc=ZoneKit.Smooth(open,open+.3f,up)*(1-ZoneKit.Smooth(thick,thick*2,Mathf.Abs(d-r)));crest=Mathf.Max(crest,arc*bright);
                    trough=Mathf.Max(trough,ZoneKit.Smooth(.2f,.5f,up)*(1-ZoneKit.Smooth(0,.12f,Mathf.Abs(d-r*.62f)))*(.5f+size));
                }
                float swell=ZoneKit.Cloud(u,v,4,101),grain=ZoneKit.Tile(u*256,v*256,256,103);
                float tone=.36f+.08f*(swell-.5f)+.025f*(grain-.5f)-.06f*trough+crest;return new Color(tone,tone,tone,1);
            });
            // Glassy water in the lee: nearly still, a little darker, long faint ripple lines lying across the current in patches, a rare small crest. Sixteen metres a repeat.
            var calm=ZoneKit.Texture("Vaskar/harbour-calm",1024,(u,v)=>
            {
                float y=v*40+(ZoneKit.Cloud(u,v,2,162)-.5f)*5;float off=Mathf.Abs(y-Mathf.Round(y));
                float line=(1-ZoneKit.Smooth(.025f,.06f,off))*ZoneKit.Smooth(.48f,.66f,ZoneKit.Cloud(u,v,4,163))*ZoneKit.Smooth(.35f,.6f,ZoneKit.Tile(u*40,Mathf.Repeat(Mathf.Round(y),40),40,164));
                const int N=32;float gx=u*N,gy=v*N;int cx=Mathf.FloorToInt(gx),cy=Mathf.FloorToInt(gy);float dot=0;
                for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++)
                {
                    int ix=cx+ox,iy=cy+oy,wx=((ix%N)+N)%N,wy=((iy%N)+N)%N;if(ZoneKit.Tile(wx,wy,N,166)>.07f)continue;
                    float dx=gx-(ix+.5f),dy=gy-(iy+.5f),d=Mathf.Sqrt(dx*dx+dy*dy);if(d<.001f)continue;
                    dot=Mathf.Max(dot,ZoneKit.Smooth(.3f,.6f,dy/d)*(1-ZoneKit.Smooth(.035f,.07f,Mathf.Abs(d-.18f))));
                }
                float tone=.31f+.05f*(ZoneKit.Cloud(u,v,3,161)-.5f)+.08f*line+.3f*dot;
                return new Color(tone,tone,tone,ZoneKit.Cloud(u,v,5,165));
            },true,true);
            // The current's streaks behind a hull or a buoy: flecks of broken water drawn out north, a few hand-widths across and a metre or two long, over churned water. Eight
            // metres a repeat. (Thin lines a lane apart, the first try, read as scratches on the glass.)
            var wake=ZoneKit.Texture("Vaskar/harbour-wake",512,(u,v)=>
            {
                float n=.62f*Stretched(u*24,v*4,24,4,171)+.38f*Stretched(u*48,v*8,48,8,172);
                float fleck=ZoneKit.Smooth(.6f,.64f,n),churn=ZoneKit.Smooth(.5f,.7f,ZoneKit.Cloud(u,v,8,175));
                float tone=.37f+.04f*churn+.36f*fleck;return new Color(tone,tone,tone,.25f+.75f*fleck);
            },true,true);
            // Foam where the water breaks on stone: thresholded noise in a band, so it lies in irregular lace with holes in it, and a scatter of loose flecks. Six metres a repeat.
            // (The borders of a cell pattern, the first try, read as crazed tiles laid round every pier.)
            var surf=ZoneKit.Texture("Vaskar/harbour-surf",512,(u,v)=>
            {
                float n=.66f*ZoneKit.Cloud(u,v,6,191)+.34f*ZoneKit.Cloud(u,v,20,192);
                float lace=ZoneKit.Smooth(.47f,.5f,n)*(1-ZoneKit.Smooth(.6f,.63f,n)),fleck=ZoneKit.Smooth(.84f,.88f,ZoneKit.Tile(u*120,v*120,120,184))*ZoneKit.Smooth(.42f,.5f,n);
                float foam=Mathf.Max(lace,fleck);float tone=.38f+.46f*foam;return new Color(tone,tone,tone,.2f+.8f*foam);
            },true,true);
            HWater=ZoneKit.Toon("vaskar-harbour-water",chop,m=>
            {
                ZoneKit.WaterLook(m,new Color(.46f,.7f,.8f,.9f),new Color(.24f,.42f,.52f,.95f),new Color(.9f,.92f,.92f,1),30,2,0,.4f);m.SetFloat("_BaseMapStrength",1);
                // Shade on the water goes blue, not violet: at dusk the stream is the sky's colour where the sun doesn't reach it.
                m.SetColor("_ShadowTint",new Color(.4f,.5f,.66f));
                ZoneKit.With(m,"_SPLAT_ON","_Splat");m.SetTexture("_SplatMap1",calm);m.SetTexture("_SplatMap2",wake);m.SetTexture("_SplatMap3",surf);
                m.SetColor("_SplatTint1",new Color(.42f,.64f,.76f,1));m.SetColor("_SplatTint2",new Color(.52f,.74f,.82f,.9f));m.SetColor("_SplatTint3",new Color(.8f,.88f,.92f,.92f));
                // No ink under a layer's edge: water's kinds run into each other, they aren't laid on it.
                m.SetVector("_SplatScale",new Vector4(1/16f,1/8f,1/6f,.35f));m.SetVector("_SplatEdge",new Vector4(.07f,.4f,.55f,0));
            });
            // A carrier's flight deck: plates two metres by four with dark seams, rivet lines along them, scuffed paler down the middle where hulls land.
            var deck=ZoneKit.Texture("Vaskar/carrier-deck",512,(u,v)=>
            {
                float x=u*2,y=v*4,fx=Mathf.Repeat(x,1),fy=Mathf.Repeat(y+(Mathf.Floor(x)%2)*.5f,1);int px=Mathf.FloorToInt(x),py=Mathf.FloorToInt(y+(Mathf.Floor(x)%2)*.5f);
                float seam=Mathf.Min(Mathf.Min(fx,1-fx)*2,Mathf.Min(fy,1-fy)*4),rivet=(Mathf.Min(fx,1-fx)<.05f||Mathf.Min(fy,1-fy)<.025f)&&Mathf.Repeat((fx+fy)*24,1)<.3f?.06f:0;
                float tone=.56f+.08f*(ZoneKit.Tile(px*1.7f,py*2.3f,64,131)-.5f)+.07f*(ZoneKit.Cloud(u,v,8,132)-.5f)+.05f*(ZoneKit.Tile(u*200,v*200,200,133)-.5f)+rivet;
                return new Color(tone,tone,tone*1.02f,1)*(seam<.035f?.55f:1);
            });
            HDeck=ZoneKit.Toon("vaskar-carrier-deck",deck,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);});
            // The slip's concrete: poured slabs four metres a side with dark joints, wet and darker toward the water, a few weed-dark stains.
            var slip=ZoneKit.Texture("Vaskar/slip-concrete",512,(u,v)=>
            {
                float fx=Mathf.Repeat(u*2,1),fy=Mathf.Repeat(v*2,1),joint=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                float tone=.6f+.08f*(ZoneKit.Cloud(u,v,6,141)-.5f)+.06f*(ZoneKit.Tile(u*240,v*240,240,142)-.5f)-.12f*ZoneKit.Smooth(.6f,.8f,ZoneKit.Cloud(u,v,4,143));
                return new Color(tone,tone*.99f,tone*.97f,1)*(joint<.012f?.5f:1);
            });
            HSlip=ZoneKit.Toon("vaskar-harbour-slip",slip,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);});
            HBed=ZoneKit.Toon("vaskar-harbour-bed",null,m=>m.SetColor("_BaseColor",new Color(.07f,.08f,.08f)));
            HGlow=kit.Glow("Lamp glow",false,new Color(1.5f,1.15f,.7f,1));
            // The Muster's arc lamps, come on at dusk over the cranes and the carrier: a hard blue-white against the town's warm lamps.
            HArc=ZoneKit.Toon("vaskar-harbour-arc",null,m=>{m.SetColor("_BaseColor",new Color(.9f,.95f,1));m.SetColor("_EmissionColor",new Color(2.2f,2.5f,3f));});
            HArcGlow=kit.Glow("Arc glow",false,new Color(1.1f,1.3f,1.7f,1));
            // A lamp's light on the water is a column of short bright dashes toward the camera, broken by the chop: one dash, rounded at its ends, laid many times by HarbourGlints.
            var dash=ZoneKit.Texture("Vaskar/glint-dash",128,(u,v)=>
            {
                float dx=Mathf.Max(0,Mathf.Abs(u-.5f)-.3f)/.2f,dy=Mathf.Abs(v-.5f)/.5f,d=Mathf.Sqrt(dx*dx+dy*dy);
                return new Color(1,1,1,1-ZoneKit.Smooth(.7f,1,d));
            },false,true);
            HGlint=kit.Glow("Lamp glints",false,new Color(1.2f,1.2f,1.2f,1));HGlint.SetTexture("_BaseMap",dash);
            // A pool of a lamp's light where it falls, its colour the lamp's (in the mesh).
            HPool=kit.Glow("Lamp pools",false,new Color(1.25f,1.25f,1.25f,1));
            // Both under the danger markings (Transparent-10), over the water (Transparent-30): a shell's circle always lies over a lamp's light.
            HGlint.renderQueue=HPool.renderQueue=(int)RenderQueue.Transparent-20;UnityEditor.EditorUtility.SetDirty(HGlint);UnityEditor.EditorUtility.SetDirty(HPool);
        }

        /// <summary>Value noise repeating every <paramref name="px"/> cells across and <paramref name="py"/> along: drawn out one way (ZoneKit.Tile has one period for both).</summary>
        static float Stretched(float x,float y,int px,int py,int seed)
        {
            int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y);float fx=x-x0,fy=y-y0;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            float At(int ix,int iy){ix=(ix%px+px)%px;iy=(iy%py+py)%py;uint h=(uint)(ix*374761393+iy*668265263+seed*1442695041);h=(h^(h>>13))*1274126177;h^=h>>16;return (h&0xffff)/65535f;}
            return Mathf.Lerp(Mathf.Lerp(At(x0,y0),At(x0+1,y0),fx),Mathf.Lerp(At(x0,y0+1),At(x0+1,y0+1),fx),fy);
        }

        // ---------------------------------------------------------------- what is dry, for the water's foam
        /// <summary>The harbour's edges as the water knows them: the stone (boxes by centre, half sizes and yaw; discs; the east shore's line), which the water stops at, and what floats
        /// (<see cref="Floater"/>s, each by the line where it meets the water), which the water runs under. A point's distance from the nearest of either is its second uv, the foam's.</summary>
        sealed class Dry
        {
            readonly List<(Vector2 at,Vector2 half,float yaw)> boxes=new();readonly List<(Vector2 at,Vector2 radii,float yaw)> rounds=new();
            public readonly List<Floater> floats=new();
            public Func<float,float> shore;
            public void Box(Vector2 at,Vector2 half,float yaw=0)=>boxes.Add((at,half,yaw));
            public void Round(Vector2 at,float radius)=>rounds.Add((at,new Vector2(radius,radius),0));
            public void Ellipse(Vector2 at,Vector2 radii,float yaw)=>rounds.Add((at,radii,yaw));
            /// <summary>Something floating, by its waterline; <paramref name="wake"/> is how strongly the current streams behind it (0 for none).</summary>
            public void Float(List<Vector2> line,float wake=1){if(line.Count>2)floats.Add(new Floater(line,wake));}
            /// <summary>A floating round (a buoy) or an oval (a lighter, a hull whose waterline couldn't be read), as points round it.</summary>
            public void Float(Vector2 at,Vector2 radii,float yaw,float wake=1)
            {
                var line=new List<Vector2>();var turn=Quaternion.Euler(0,yaw,0);int n=Mathf.Clamp(Mathf.RoundToInt((radii.x+radii.y)*6),12,64);
                for(int k=0;k<n;k++){float a=k*Mathf.PI*2/n;var q=turn*new Vector3(Mathf.Cos(a)*radii.x,0,Mathf.Sin(a)*radii.y);line.Add(at+new Vector2(q.x,q.z));}
                Float(line,wake);
            }
            static Vector2 Local(Vector2 p,Vector2 at,float yaw){var d=p-at;float a=yaw*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(d.x*c-d.y*s,d.x*s+d.y*c);}
            /// <summary>Metres to the nearest stone: the water is drawn only where this is more than nothing.</summary>
            public float Stone(Vector2 p)
            {
                float best=shore!=null?Mathf.Max(0,shore(p.y)-p.x):float.PositiveInfinity;
                foreach(var (at,half,yaw) in boxes){var q=Local(p,at,yaw);var o=new Vector2(Mathf.Max(0,Mathf.Abs(q.x)-half.x),Mathf.Max(0,Mathf.Abs(q.y)-half.y));best=Mathf.Min(best,o.magnitude);}
                foreach(var (at,radii,yaw) in rounds){var q=Local(p,at,yaw);float k=new Vector2(q.x/radii.x,q.y/radii.y).magnitude;best=Mathf.Min(best,Mathf.Max(0,(k-1)*Mathf.Min(radii.x,radii.y)));}
                return best;
            }
            /// <summary>Metres to the nearest waterline of anything floating.</summary>
            public float Floating(Vector2 p){float best=float.PositiveInfinity;foreach(var f in floats)best=Mathf.Min(best,f.Distance(p));return best;}
            public float From(Vector2 p)=>Mathf.Min(Stone(p),Floating(p));
        }
        /// <summary>A floating thing's waterline, and the current's wake behind it: the harbour ebbs north, out past the carrier, so each hull and buoy trails a tongue of broken water
        /// north of itself as wide as it lies across the stream, drawn out and thinning.</summary>
        sealed class Floater
        {
            readonly List<Vector2> line;readonly float minX,maxX,minZ,maxZ,wake;readonly float[] top;const float Bin=.5f;
            public Floater(List<Vector2> points,float wake)
            {
                // A tenth of a metre apart at most: a hull's mesh crosses the water in many small pieces.
                var seen=new HashSet<(int,int)>();line=new List<Vector2>();foreach(var p in points)if(seen.Add((Mathf.RoundToInt(p.x*10),Mathf.RoundToInt(p.y*10))))line.Add(p);
                this.wake=wake;minX=line.Min(p=>p.x);maxX=line.Max(p=>p.x);minZ=line.Min(p=>p.y);maxZ=line.Max(p=>p.y);
                top=new float[Mathf.CeilToInt((maxX-minX)/Bin)+1];for(int k=0;k<top.Length;k++)top[k]=float.NegativeInfinity;
                foreach(var p in line){int k=Mathf.Clamp(Mathf.FloorToInt((p.x-minX)/Bin),0,top.Length-1);top[k]=Mathf.Max(top[k],p.y);}
                for(int k=0;k<top.Length;k++)if(float.IsNegativeInfinity(top[k]))top[k]=k>0?top[k-1]:maxZ;
            }
            public float Distance(Vector2 p)
            {
                if(p.x<minX-12||p.x>maxX+12||p.y<minZ-12||p.y>maxZ+12)return float.PositiveInfinity;
                float best=float.PositiveInfinity;foreach(var q in line){float d=(p-q).sqrMagnitude;if(d<best)best=d;}return Mathf.Sqrt(best);
            }
            /// <summary>How much of the current's broken water lies at <paramref name="p"/> behind this (0 to 1).</summary>
            public float Wake(Vector2 p)
            {
                if(wake<=0)return 0;float across=maxX-minX,length=Mathf.Clamp(6+across*1.2f,5,24);
                float edge=Mathf.Max(minX-p.x,p.x-maxX);float zTop=top[Mathf.Clamp(Mathf.FloorToInt((Mathf.Clamp(p.x,minX,maxX)-minX)/Bin),0,top.Length-1)];
                float along=p.y-zTop;if(along<-.2f||along>length)return 0;along=Mathf.Max(0,along);
                float spread=.6f+2*along/length,inside=1-ZoneKit.Smooth(-Mathf.Min(1.2f,across*.2f),spread,edge);
                return wake*inside*Mathf.Pow(1-along/length,.8f);
            }
        }

        // ---------------------------------------------------------------- built stone
        /// <summary>A block of the town's masonry: its sides in coursed basalt (slot 0, the courses level), its top in flags (slot 1).</summary>
        static void Block(ZoneKit.Shape shape,Vector3 centre,Vector3 size,float yaw,Color face,Color top)
        {
            var turn=Quaternion.Euler(0,yaw,0);Vector3 x=turn*Vector3.right*size.x*.5f,z=turn*Vector3.forward*size.z*.5f;float low=centre.y-size.y*.5f,high=centre.y+size.y*.5f;
            var c=new Vector3(centre.x,0,centre.z);
            shape.Slot(1).Quad(c-x-z+Vector3.up*high,c-x+z+Vector3.up*high,c+x+z+Vector3.up*high,c+x-z+Vector3.up*high,Vector3.up,top,1/8f);
            foreach(var (a,b) in new[]{(c-x-z,c+x-z),(c+x-z,c+x+z),(c+x+z,c-x+z),(c-x+z,c-x-z)})Side(shape,a,b,low,high,face);
        }
        /// <summary>One upright face from <paramref name="a"/> to <paramref name="b"/> on the plan, its courses level: laid so the texture's up is the world's.</summary>
        static void Side(ZoneKit.Shape shape,Vector3 a,Vector3 b,float low,float high,Color face)
        {
            var mid=(a+b)*.5f;var along=b-a;along.y=0;var outward=Vector3.Cross(along,Vector3.up);
            // Quad's texture runs along its first edge and up across it: the first edge level, turned so that up is up.
            if(Vector3.Dot(Vector3.Cross(outward.normalized,along.normalized),Vector3.up)<0){var t=a;a=b;b=t;}
            a.y=b.y=0;shape.Slot(0).Quad(a+Vector3.up*low,b+Vector3.up*low,b+Vector3.up*high,a+Vector3.up*high,outward,face,.2f);
        }
        /// <summary>A round of masonry (a tower, a gun pit's footing, a mole's head): its wall coursed round it, its top flags.</summary>
        static void Round(ZoneKit.Shape shape,Vector3 foot,float radius,float high,Color face,Color top,int sides=20)
        {
            for(int k=0;k<sides;k++)
            {
                float a0=k*Mathf.PI*2/sides,a1=(k+1)*Mathf.PI*2/sides;
                Side(shape,foot+new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1))*radius,foot+new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0))*radius,foot.y,high,face);
            }
            shape.Slot(1).Disc(new Vector3(foot.x,high,foot.z),Vector3.up,radius,sides,top,0,null,1/8f);
        }
        /// <summary>Merlons along a line: a parapet's teeth, so a wall's top reads as a fortress's from above.</summary>
        static void Parapet(ZoneKit.Shape shape,Vector3 from,Vector3 to,float walk,float low,float high,float thick,Color face,Color top)
        {
            var run=to-from;run.y=0;float length=run.magnitude;var along=run/length;float yaw=Mathf.Atan2(along.x,along.z)*Mathf.Rad2Deg;
            Block(shape,new Vector3((from.x+to.x)*.5f,(walk+low)*.5f,(from.z+to.z)*.5f),new Vector3(thick,low-walk,length),yaw,face,top);
            for(float d=1.2f;d<length-1;d+=2.6f){var c=from+along*d;Block(shape,new Vector3(c.x,(low+high)*.5f,c.z),new Vector3(thick,high-low,1.3f),yaw,face,top);}
        }

        // ---------------------------------------------------------------- the place
        /// <summary>The wall below the field, the field and the houses on the walk, the quay, the mole, the piers, the gun pits, the slip, the cranes' rail piers: the town's stone.</summary>
        static void HarbourStone(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water;var shape=new ZoneKit.Shape();var face=new Color(.62f,.6f,.6f);var top=new Color(.74f,.72f,.7f);var old=new Color(.5f,.49f,.5f);
            // The sea wall below the muster field, its walk a metre under the plane and its parapet's teeth just under that: the skiff goes down over it, onto the field.
            Block(shape,new Vector3(-64,(W-5+WallWalk)*.5f,95),new Vector3(16,WallWalk-(W-5),290),0,face,top);dry.Box(new Vector2(-64,95),new Vector2(8,145));
            Parapet(shape,new Vector3(-56.6f,0,-50),new Vector3(-56.6f,0,240),WallWalk,WallWalk+.7f,WallWalk+1.4f,1.2f,old,top);
            foreach(float z in WallTowers){Round(shape,new Vector3(-56,W-5,z),6,WallWalk+.3f,face,top);dry.Round(new Vector2(-56,z),6);}
            // The muster field over it, west, and the town's terraces beyond: the wide still's.
            Block(shape,new Vector3(-106,(W-5+WallWalk)*.5f,95),new Vector3(52,WallWalk-(W-5),290),0,old,top);
            Block(shape,new Vector3(-142,WallWalk+2,95),new Vector3(20,6,290),0,face,top);Block(shape,new Vector3(-162,WallWalk+5,95),new Vector3(20,12,290),0,face,top);
            // The quay the party lifts off, under the launch: its face four metres over the water, steps down to it.
            Block(shape,new Vector3(-5,(W-5+QuayTop)*.5f,-25),new Vector3(102,QuayTop-(W-5),40),0,face,top);dry.Box(new Vector2(-5,-25),new Vector2(51,20));
            foreach(float x in new[]{-36f,-4f,28f})
            {
                for(int s=0;s<4;s++){float high=QuayTop-.8f*(s+1);Block(shape,new Vector3(x+s,(W-1+high)*.5f,-4.3f),new Vector3(1,high-(W-1),1.4f),0,old,top);}
                dry.Box(new Vector2(x+1.5f,-4.3f),new Vector2(2,.7f));
            }
            // The flak mole out from the wall toward leg 2, its two gun pits and its head.
            Block(shape,new Vector3(-36,(W-5+MoleTop)*.5f,128),new Vector3(42,MoleTop-(W-5),12),-6,face,top);dry.Box(new Vector2(-36,128),new Vector2(21,6),-6);
            // Its parapet along the north side, broken for the gun pit there.
            foreach(var (d0,d1) in new[]{(1f,23f),(36f,40f)})Parapet(shape,MoleAt(d0,5.4f),MoleAt(d1,5.4f),MoleTop,MoleTop+.6f,MoleTop+1.2f,.9f,old,top);
            Round(shape,new Vector3(MoleHead.x,W-5,MoleHead.y),6.5f,MoleTop,face,top,24);dry.Round(MoleHead,6.5f);
            for(int i=0;i<Harbour.Batteries.Length;i++)
            {
                var b=Harbour.Batteries[i];Round(shape,new Vector3(b.x,W-5,b.z),5,GunPit-.02f,face,top,20);dry.Round(new Vector2(b.x,b.z),5);
                // The pit's own parapet: a ring of low teeth round the guns.
                for(int k=0;k<10;k++){float a=k*Mathf.PI*2/10;var p=new Vector3(b.x,0,b.z)+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*4.4f;Block(shape,new Vector3(p.x,GunPit+.4f,p.z),new Vector3(1.5f,.8f,.7f),90-a*Mathf.Rad2Deg,old,top);}
            }
            // Finger piers off the wall, where the Muster's hulls lie.
            foreach(var (z,reach) in Piers){float x0=-56,x1=-56+reach;Block(shape,new Vector3((x0+x1)*.5f,(W-4+PierTop)*.5f,z),new Vector3(reach,PierTop-(W-4),7),0,face,top);dry.Box(new Vector2((x0+x1)*.5f,z),new Vector2(reach*.5f,3.5f));}
            // The slip, from the water up to the carrier's stern; its rails and its kerbs.
            float slipFrom=SlipFoot,slipTo=SlipHead,rise=SlipTop-(W-1);float pitch=-Mathf.Atan2(rise,slipTo-slipFrom)*Mathf.Rad2Deg;
            var slipMid=new Vector3(14,(W-1+SlipTop)*.5f-1.2f,(slipFrom+slipTo)*.5f);
            var slipway=new ZoneKit.Shape();var slipTurn=Quaternion.Euler(pitch,0,0);float slipLength=Mathf.Sqrt(rise*rise+(slipTo-slipFrom)*(slipTo-slipFrom));
            slipway.Box(slipMid,new Vector3(26,2.4f,slipLength),slipTurn,new Color(.52f,.5f,.48f),new Color(.6f,.58f,.56f),.125f);
            // Two pairs of rails up it, on sleepers, for the cradles a skiff is hauled up on; a kerb along each side.
            var surface=slipMid+slipTurn*Vector3.up*1.2f;var along=slipTurn*Vector3.forward;var rails=new ZoneKit.Shape();
            foreach(float rx in new[]{-7.5f,-5.5f,5.5f,7.5f})rails.Beam(surface+Vector3.right*rx-along*slipLength*.5f+Vector3.up*.12f,surface+Vector3.right*rx+along*slipLength*.5f+Vector3.up*.12f,.18f,.22f,new Color(.36f,.34f,.34f),new Color(.62f,.6f,.58f));
            for(float d=-slipLength*.5f+.8f;d<slipLength*.5f;d+=1.6f)foreach(float rx in new[]{-6.5f,6.5f})rails.Box(surface+Vector3.right*rx+along*d+Vector3.up*.04f,new Vector3(3.2f,.1f,.32f),slipTurn,new Color(.3f,.27f,.24f),new Color(.36f,.31f,.27f),.5f);
            foreach(float kx in new[]{-12.6f,12.6f})slipway.Box(surface+Vector3.right*kx+Vector3.up*.3f,new Vector3(.8f,.6f,slipLength),slipTurn,new Color(.46f,.44f,.42f),new Color(.56f,.54f,.52f),.25f);
            Stone(kit,"The slip",slipway,HSlip,true,parent);Stone(kit,"The slip's rails",rails,HIron,true,parent);dry.Box(new Vector2(14,(slipFrom+slipTo)*.5f+2),new Vector2(13,(slipTo-slipFrom)*.5f+2));
            // The cranes' rail piers, under each pair of legs: a gantry stands on its rails.
            foreach(var (name,at,hx,hz,tall) in Harbour.Cranes)foreach(int sz in new[]{-1,1})
            {
                Block(shape,new Vector3(at.x,(W-4+W+1.6f)*.5f,at.y+sz*hz),new Vector3(hx*2+5,5.6f,2.6f),0,old,top);dry.Box(new Vector2(at.x,at.y+sz*hz),new Vector2(hx+2.5f,1.3f));
            }
            Stone(kit,"Harbour stone",shape.Slot(1),new[]{HMasonry,HFlags},true,parent);
            // The bed under the water, dark basalt silt: what the shallows show through.
            var bed=new ZoneKit.Shape();bed.Quad(new Vector3(-170,W-3,-70),new Vector3(-170,W-3,320),new Vector3(180,W-3,320),new Vector3(180,W-3,-70),Vector3.up,Color.white,1/20f);
            Stone(kit,"Harbour bed",bed,HBed,false,parent).GetComponent<MeshRenderer>().receiveShadows=false;
        }
        internal const float WallWalk=-1,QuayTop=-3,MoleTop=-2.2f,GunPit=-2.2f,PierTop=-4,SlipFoot=180,SlipHead=198,SlipTop=-3;
        static readonly float[] WallTowers={-14,58,176};
        static readonly Vector2 MoleHead=new(-15.1f,130.2f);
        /// <summary>A point along the flak mole from its root at the wall (<paramref name="along"/> metres), <paramref name="north"/> metres off its middle line: the mole lies 6 degrees north of east.</summary>
        static Vector3 MoleAt(float along,float north){float a=6*Mathf.Deg2Rad;return new Vector3(-56.9f+along*Mathf.Cos(a)-north*Mathf.Sin(a),0,125.8f+along*Mathf.Sin(a)+north*Mathf.Cos(a));}
        static readonly (float z,float reach)[] Piers={(40,22),(92,20)};

        /// <summary>The east shore: black basalt from the water to a low rough top, its edge broken into clusters of columns (the Reach's own coast), the shore batteries' pits on it, and a
        /// seep of melt running down to the sea, steaming where it meets it.</summary>
        static void EastShore(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water;
            float Line(float z)=>50+4*Mathf.Sin(z*.045f+1)+3*(Mathf.PerlinNoise(z*.03f,4.1f)-.5f)*2;
            dry.shore=Line;
            float Height(float x,float z){float inland=x-Line(z);if(inland<0)return W-2+inland*.2f;float rough=Mathf.PerlinNoise(x*.12f,z*.12f)-.5f;return Mathf.Lerp(W-1.2f,-3.8f+rough*1.4f,ZoneKit.Smooth(0,5,inland));}
            var land=new ZoneKit.Shape();const float Cell=2;
            for(float x=36;x<150;x+=Cell)for(float z=-60;z<250;z+=Cell)
            {
                Vector3 P(float px,float pz)=>new(px,Height(px,pz),pz);
                Vector3 a=P(x,z),b=P(x,z+Cell),c=P(x+Cell,z+Cell),d=P(x+Cell,z);if(a.y<W-1.5f&&b.y<W-1.5f&&c.y<W-1.5f&&d.y<W-1.5f)continue;
                float t=Mathf.Clamp01((Mathf.Max(a.y,c.y)-W)/4);var tone=ZoneKit.Shape.Dim(Color.Lerp(new Color(.24f,.24f,.27f),new Color(.44f,.43f,.45f),t),.9f+.2f*Mathf.PerlinNoise(x*.4f,z*.4f));
                land.Triangle(a,b,c,Vector3.up,tone,1/6f).Triangle(a,c,d,Vector3.up,ZoneKit.Shape.Dim(tone,.95f),1/6f);
            }
            // Columns along the edge: tight clusters of six-sided basalt, stepping down into the water.
            var rng=new System.Random(911);
            for(float z=-50;z<240;z+=14+(float)rng.NextDouble()*8)
            {
                float edge=Line(z);int count=8+rng.Next(9);
                for(int c=0;c<count;c++)
                {
                    float r=.75f+.25f*(float)rng.NextDouble(),dx=((float)rng.NextDouble()-.35f)*6,dz=((float)rng.NextDouble()-.5f)*7;var foot=new Vector3(edge+dx,W-1.5f,z+dz);
                    float topY=Mathf.Lerp(W+.4f,-2.6f,ZoneKit.Smooth(-3,3,dx))+((float)rng.NextDouble()-.5f)*.8f;if(topY<=W-.2f)continue;
                    var tone=ZoneKit.Shape.Dim(new Color(.3f,.3f,.33f),.85f+.3f*(float)rng.NextDouble());
                    land.Tube(new[]{foot,new Vector3(foot.x,topY,foot.z)},new[]{r,r},6,tone,ZoneKit.Shape.Dim(tone,1.15f),1,.3f);
                }
            }
            Stone(kit,"East shore",land,RockLook,true,parent);
            // Fumaroles: cracks in the shore breathing steam, a little dull red at the bottom of each: the ground under the town is still warm. (A seep of melt down to the sea, the
            // first try, was an orange line of beads across the frame: a marking's colour and shape.)
            var vents=new ZoneKit.Shape();var embers=new ZoneKit.Shape();var steamGlow=kit.Glow("Steam",false,new Color(.9f,.9f,.92f,1));
            foreach(var (vx,vz) in Fumaroles)
            {
                var at=new Vector3(vx,Height(vx,vz),vz);
                for(int k=0;k<6;k++){float a=k*1.05f;vents.Lump(at+new Vector3(Mathf.Cos(a)*1.2f,.15f,Mathf.Sin(a)*1.2f),new Vector3(.75f,.45f,.7f),k+(int)vx,.28f,new Color(.44f,.42f,.4f),new Color(.2f,.19f,.19f),7,4);}
                embers.Lump(at+Vector3.up*.02f,new Vector3(.3f,.08f,.26f),(int)vz,.2f,new Color(.5f,.5f,.5f),new Color(.5f,.5f,.5f),7,3);
                var steam=kit.Drift("Steam off the shore",at+Vector3.up*1.6f,new Vector3(2,1,2),steamGlow,new Color(.86f,.86f,.88f,.38f),70,.8f,2.2f,.9f,5);
                var lift=steam.velocityOverLifetime;lift.enabled=true;lift.x=new ParticleSystem.MinMaxCurve(0);lift.y=new ParticleSystem.MinMaxCurve(1.2f);lift.z=new ParticleSystem.MinMaxCurve(0);steam.transform.SetParent(parent,true);
            }
            Stone(kit,"Fumaroles",vents,RockLook,false,parent);Stone(kit,"Fumarole embers",embers,HMelt,false,parent);
        }
        static readonly (float x,float z)[] Fumaroles={(66,96),(76,40),(68,176)};

        /// <summary>The houses of the town crowding the wall's walk and the terraces beyond the field, as the town builds them (its own basalt house), with cook-smoke.</summary>
        static void HarbourTown(ZoneKit kit,Transform parent)
        {
            if(!ZoneKit.Landed("HarrowgateHouse"))return;var rng=new System.Random(77);var root=new GameObject("The town over the wall");root.transform.SetParent(parent,true);
            void House(Vector3 at,float size,float yaw){var house=kit.Prop("HarrowgateHouse",at,size,yaw,false,false);house.AddComponent<PropTint>().tint=Color.Lerp(new Color(.82f,.8f,.78f),Color.white,(float)rng.NextDouble()*.4f);house.transform.SetParent(root.transform,true);}
            for(float z=-40;z<235;z+=10+(float)rng.NextDouble()*6){if(WallTowers.Any(t=>Mathf.Abs(t-z)<8))continue;House(new Vector3(-66.5f-(float)rng.NextDouble()*2,WallWalk,z),5+(float)rng.NextDouble()*1.5f,90);}
            foreach(var (x,y) in new[]{(-142f,WallWalk+5),(-162f,WallWalk+11)})for(float z=-40;z<235;z+=12+(float)rng.NextDouble()*6)House(new Vector3(x-(float)rng.NextDouble()*3,y,z),6+(float)rng.NextDouble()*2,90);
            var smoke=kit.Glow("Cook smoke",false,new Color(.6f,.6f,.64f,1));
            foreach(float z in new[]{8f,74f,130f,196f}){var drift=kit.Drift("Cook smoke",new Vector3(-67,WallWalk+7,z),new Vector3(2,1,2),smoke,new Color(.66f,.64f,.66f,.3f),40,1,2.2f,.6f,8);drift.transform.SetParent(root.transform,true);}
        }

        /// <summary>Lamps: on the quay's edge, along the wall's walk (braziers), the mole and its head, round the gun pits, both sides of the slip; each a head of light with a halo the flight
        /// camera reads from its height, and its light lying on the water toward the camera. No light sources: past six a renderer they are painted, not lit.</summary>
        static void HarbourLamps(ZoneKit kit,Transform parent)
        {
            float W=Harbour.Water;var heads=new ZoneKit.Shape();var posts=new ZoneKit.Shape();var root=new GameObject("Harbour lamps");root.transform.SetParent(parent,true);
            var warm=new Color(1,.84f,.56f);
            // A lamp; overWater lays its reflection toward the camera (shifted out to the water when it stands back from a wall's face), and pool lays its light on the water in
            // front of it, toward pool's direction.
            void Lamp(Vector3 foot,float tall,bool overWater,float halo=1.4f,Color? colour=null,float shiftX=0,Vector2? pool=null)
            {
                posts.Post(foot,tall,.12f,.1f,6,new Color(.22f,.21f,.22f),new Color(.3f,.29f,.3f),.5f);
                var head=foot+Vector3.up*(tall+.25f);heads.Lump(head,new Vector3(.32f,.4f,.32f),Mathf.RoundToInt(foot.x*7+foot.z),.05f,colour??Color.white,colour??Color.white,8,5);
                Halo(root.transform,head,halo,HGlow);
                if(overWater)Reflections.Add((head,colour??warm,.9f,1,shiftX));
                if(pool is Vector2 toward)Pools.Add((new Vector3(foot.x+toward.x,W+.05f,foot.z+toward.y),4.4f,new Color(1,.8f,.5f,.55f)));
            }
            for(float x=-50;x<44;x+=12)Lamp(new Vector3(x,QuayTop,-6.2f),2.2f,false,pool:new Vector2(0,4.2f));
            for(float z=-46;z<236;z+=16)if(!WallTowers.Any(t=>Mathf.Abs(t-z)<7))Lamp(new Vector3(-57.6f,WallWalk,z),1.6f,true,shiftX:3.2f,pool:new Vector2(4.4f,0));
            foreach(float z in WallTowers)Lamp(new Vector3(-56,WallWalk+.3f,z),1.4f,true,2.2f,shiftX:7.4f);
            for(float d=4;d<40;d+=8){var p=MoleAt(d,-5.3f);if(Harbour.Batteries.Any(b=>Vector2.Distance(new Vector2(b.x,b.z),new Vector2(p.x,p.z))<6.5f))continue;Lamp(new Vector3(p.x,MoleTop,p.z),1.8f,true,pool:new Vector2(0,-3.4f));}
            // Lamps along the finger piers' south edges, where the Muster's hulls lie: their light on the water between the piers.
            foreach(var (z,reach) in Piers)for(float x=-52;x<-56+reach-2;x+=7.5f)Lamp(new Vector3(x,PierTop,z-3.1f),1.6f,true,pool:new Vector2(0,-3.2f));
            // The mole's head light, white, and the breakwater's, green: the harbour's two marks.
            Lamp(new Vector3(MoleHead.x,MoleTop,MoleHead.y),1.4f,true,3,new Color(.9f,.95f,1));
            foreach(var b in Harbour.Batteries)for(int k=0;k<2;k++){float a=(k*180+45)*Mathf.Deg2Rad;Lamp(new Vector3(b.x+Mathf.Cos(a)*4.4f,GunPit+.8f,b.z+Mathf.Sin(a)*4.4f),.8f,false);}
            foreach(int s in new[]{0,1})for(float z=SlipFoot+2;z<SlipHead;z+=5){float y=Mathf.Lerp(W-1,SlipTop,(z-SlipFoot)/(SlipHead-SlipFoot));Lamp(new Vector3(s==0?1.6f:26.4f,y,z),1.2f,false,pool:new Vector2(s==0?-3.4f:3.4f,0));}
            Lamp(new Vector3(BreakwaterHead.x,W+2.6f,BreakwaterHead.y),1.4f,true,3,new Color(.5f,1,.62f));
            Stone(kit,"Lamp posts",posts,HIron,false,root.transform);Stone(kit,"Lamp heads",heads,HLamp,false,root.transform);
            // Braziers on the walk, between the houses and the parapet: the wall is manned.
            var coal=ZoneKit.WorldMaterial("harrow-coal")??HMelt;var bowls=new ZoneKit.Shape();var fires=new ZoneKit.Shape();
            for(float z=-38;z<236;z+=24){if(WallTowers.Any(t=>Mathf.Abs(t-z)<8))continue;var at=new Vector3(-59.5f,WallWalk,z);bowls.Post(at,.7f,.18f,.5f,8,new Color(.2f,.19f,.2f),new Color(.26f,.25f,.26f),.5f);fires.Lump(at+Vector3.up*.75f,new Vector3(.4f,.22f,.4f),Mathf.RoundToInt(z),.2f,Color.white,Color.white,7,4);}
            Stone(kit,"Braziers",bowls,HIron,false,root.transform);Stone(kit,"Brazier fire",fires,coal,false,root.transform);
        }
        static readonly Vector2 BreakwaterHead=new(26,214);

        /// <summary>The breakwater off the east shore, round the carrier's berth: basalt heaped, its head lit green.</summary>
        static void Breakwater(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water;var shape=new ZoneKit.Shape();var rng=new System.Random(213);var from=new Vector2(54,186);
            for(float t=0;t<=1.001f;t+=.04f)
            {
                var p=Vector2.Lerp(from,BreakwaterHead,t);float r=2.2f+1.4f*(float)rng.NextDouble();int seed=rng.Next(99999);
                Crag(shape,new Vector3(p.x+((float)rng.NextDouble()-.5f)*3,W+.4f,p.y+((float)rng.NextDouble()-.5f)*3),new Vector3(r,r*.8f,r*1.1f),Quaternion.Euler(0,seed%360,0),seed,.22f,new Color(.42f,.41f,.43f),new Color(.16f,.16f,.18f),8,5);
                dry.Round(p,4.2f);
            }
            Stone(kit,"Breakwater",shape,RockLook,true,parent);
        }

        /// <summary>What floats: the Muster's hulls in its gunmetal along the quay and the piers and at buoys in the stream, each lying low with its lines to a bollard or a buoy; lighters
        /// with cargo; and the channel's buoys. Every footprint goes to <paramref name="dry"/>, so each lies in its own ring of foam.</summary>
        static void Moored(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water;var root=new GameObject("Moored");root.transform.SetParent(parent,true);var ropes=new ZoneKit.Shape();var gear=new ZoneKit.Shape();var rng=new System.Random(812);
            var lamps=new ZoneKit.Shape();var lit=new ZoneKit.Shape();
            foreach(var (key,x,z,length,yaw,line) in Hulls)
            {
                if(!ZoneKit.Landed(key))continue;
                var hull=kit.Prop(key,new Vector3(x,0,z),length,0,false,false);var box=ZoneKit.Drawn(hull);float beam=box.size.x,deep=box.size.y;
                hull.transform.RotateAround(box.center,Vector3.up,yaw);hull.transform.position+=Vector3.up*(W-deep*.32f-box.min.y);
                hull.AddComponent<PropTint>().tint=new Color(.5f+.08f*(float)rng.NextDouble(),.47f,.45f);hull.transform.SetParent(root.transform,true);hull.isStatic=true;
                foreach(var r in hull.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
                // The water runs under the hull to the line where its plates go in: foam hugs that, and the current streams on north of it. (Cut out by an oval, the water left
                // a dark square of harbour bed showing beside every hull, and lay as a pale disc round it.)
                var surface=Surface(hull);var waterline=Waterline(surface,W);
                if(waterline.Count>8)dry.Float(waterline);else dry.Float(new Vector2(x,z),new Vector2(beam*.45f,box.size.z*.45f),yaw);
                var along=Quaternion.Euler(0,yaw,0)*Vector3.forward;var deck=new Vector3(x,W+deep*.5f,z);
                foreach(int end in new[]{-1,1})
                {
                    var from=deck+along*end*box.size.z*.42f;var to=line+along*end*Mathf.Min(6,box.size.z*.3f);to.y=line.y;
                    ropes.Rope(from,to,.6f,.06f,new Color(.76f,.7f,.58f),4,8);
                    if(to.y<=W+.5f){gear.Lump(to,new Vector3(.7f,.55f,.7f),Mathf.RoundToInt(to.x*3+to.z),.06f,new Color(.86f,.8f,.6f),new Color(.24f,.24f,.26f),10,5);dry.Float(new Vector2(to.x,to.z),new Vector2(.75f,.75f),0,.5f);}
                    else gear.Post(new Vector3(to.x,to.y-.6f,to.z),.7f,.22f,.28f,8,new Color(.18f,.18f,.2f),new Color(.26f,.26f,.28f));
                }
                HullLights(surface,new Vector3(x,0,z),along,box.size.z,lamps,lit,gear,root.transform);
            }
            // Lighters: open barges of timber and plate, low in the water with crates on them.
            foreach(var (x,z,yaw) in Lighters)
            {
                var turn=Quaternion.Euler(0,yaw,0);var c=new Vector3(x,W+.35f,z);var hull=new ZoneKit.Shape();
                hull.Slot(0).Box(c,new Vector3(3,1.3f,7),turn,new Color(.26f,.25f,.26f),new Color(.3f,.29f,.3f),.5f);
                hull.Slot(1).Box(c+turn*new Vector3(0,.7f,0),new Vector3(2.5f,.12f,6.4f),turn,new Color(.72f,.62f,.5f),new Color(.8f,.68f,.54f),.4f).Slot(0);
                var bow=c+turn*new Vector3(0,0,3.5f);hull.Triangle(bow+turn*new Vector3(-1.5f,.65f,0),bow+turn*new Vector3(1.5f,.65f,0),bow+turn*new Vector3(0,.4f,1.4f),turn*Vector3.up,new Color(.34f,.32f,.32f),.5f);
                for(int k=0;k<3;k++)hull.Slot(1).Box(c+turn*new Vector3(((k%2)-.5f)*1.1f,1.3f,-1.8f+k*1.6f),new Vector3(1,1,1.2f),turn*Quaternion.Euler(0,k*12,0),new Color(.62f,.52f,.42f),new Color(.7f,.6f,.48f),.5f);
                Stone(kit,"Lighter "+Mathf.RoundToInt(x)+" "+Mathf.RoundToInt(z),hull,new[]{HHull,HTimber},true,root.transform);
                // Its waterline: the box and its bow.
                var outline=new List<Vector2>();var b0=turn*new Vector3(-1.5f,0,-3.5f);var b1=turn*new Vector3(1.5f,0,-3.5f);var b2=turn*new Vector3(1.5f,0,3.5f);var b3=turn*new Vector3(0,0,4.9f);var b4=turn*new Vector3(-1.5f,0,3.5f);
                var corners=new[]{b0,b1,b2,b3,b4};for(int k=0;k<corners.Length;k++){var a=corners[k];var e=corners[(k+1)%corners.Length];for(float t=0;t<1;t+=.1f){var q=Vector3.Lerp(a,e,t);outline.Add(new Vector2(x+q.x,z+q.z));}}
                dry.Float(outline,.8f);
                // A lantern on its cargo.
                var hang=c+turn*new Vector3(.9f,2.2f,1.6f);lamps.Lump(hang,new Vector3(.18f,.24f,.18f),Mathf.RoundToInt(x*5+z),.05f,Color.white,Color.white,8,5);Halo(root.transform,hang,1.1f,HGlow);
                Reflections.Add((hang,new Color(1,.84f,.56f),.7f,.8f,0));
            }
            // The channel: pale buoys either side of the run, each in its ring of foam, the stream drawn out behind it.
            foreach(var (x,z) in ChannelBuoys)
            {
                var at=new Vector3(x,W,z);gear.Post(at+Vector3.down*.4f,1.6f,.55f,.18f,8,new Color(.86f,.84f,.76f),new Color(.86f,.84f,.76f));
                gear.Post(at+Vector3.up*.5f,.4f,.47f,.4f,8,new Color(.18f,.18f,.2f),new Color(.18f,.18f,.2f));dry.Float(new Vector2(x,z),new Vector2(.62f,.62f),0,.7f);
            }
            Stone(kit,"Mooring lines",ropes,HRope,false,root.transform);Stone(kit,"Moorings",gear,HHull,false,root.transform);
            if(lamps.Vertices>0)Stone(kit,"Riding lights",lamps,HLamp,false,root.transform);if(lit.Vertices>0)Stone(kit,"Lit ports",lit,HLamp,false,root.transform);
        }
        /// <summary>A body's drawn surfaces as world triangles: what its waterline and its lamps are read from.</summary>
        static List<(Vector3 a,Vector3 b,Vector3 c)> Surface(GameObject body)
        {
            var all=new List<(Vector3,Vector3,Vector3)>();
            foreach(var filter in body.GetComponentsInChildren<MeshFilter>())
            {
                var mesh=filter.sharedMesh;if(mesh==null)continue;var m=filter.transform.localToWorldMatrix;var v=mesh.vertices;if(v.Length==0)continue;var t=mesh.triangles;
                for(int k=0;k+2<t.Length;k+=3)all.Add((m.MultiplyPoint3x4(v[t[k]]),m.MultiplyPoint3x4(v[t[k+1]]),m.MultiplyPoint3x4(v[t[k+2]])));
            }
            return all;
        }
        /// <summary>Where a body's drawn surfaces cross the water: points every quarter metre along each triangle's crossing.</summary>
        static List<Vector2> Waterline(List<(Vector3 a,Vector3 b,Vector3 c)> surface,float level)
        {
            var line=new List<Vector2>();var cut=new List<Vector3>(3);
            foreach(var (a,b,c) in surface)
            {
                cut.Clear();
                foreach(var (p,q) in new[]{(a,b),(b,c),(c,a)}){if((p.y-level)*(q.y-level)>0||Mathf.Approximately(p.y,q.y))continue;cut.Add(Vector3.Lerp(p,q,(level-p.y)/(q.y-p.y)));}
                if(cut.Count<2)continue;float length=Vector3.Distance(cut[0],cut[1]);int n=Mathf.Max(1,Mathf.CeilToInt(length/.25f));
                for(int k=0;k<=n;k++){var x=Vector3.Lerp(cut[0],cut[1],k/(float)n);line.Add(new Vector2(x.x,x.z));}
            }
            return line;
        }
        /// <summary>The highest drawn point of a body within <paramref name="reach"/> of a plan point, or null where it has none.</summary>
        static float? TopAt(List<(Vector3 a,Vector3 b,Vector3 c)> surface,Vector2 at,float reach)
        {
            float best=float.NegativeInfinity;
            foreach(var (a,b,c) in surface)foreach(var p in new[]{a,b,c,(a+b+c)/3})if((new Vector2(p.x,p.z)-at).sqrMagnitude<reach*reach&&p.y>best)best=p.y;
            return float.IsNegativeInfinity(best)?null:best;
        }
        /// <summary>A moored hull's lights, read off its own surfaces: a white light at the bow, a warm one at the stern and one on a short mast amidships, each with its halo and its
        /// light on the water; and a row of lit ports along its back, where the crew are aboard.</summary>
        static void HullLights(List<(Vector3 a,Vector3 b,Vector3 c)> surface,Vector3 centre,Vector3 along,float length,ZoneKit.Shape lamps,ZoneKit.Shape lit,ZoneKit.Shape gear,Transform parent)
        {
            if(surface.Count==0)return;var side=Vector3.Cross(Vector3.up,along);
            void Light(Vector3 at,float raise,Color tone,float halo)
            {
                var top=TopAt(surface,new Vector2(at.x,at.z),.7f);if(top==null)return;var head=new Vector3(at.x,top.Value+raise+.12f,at.z);
                if(raise>.3f)gear.Post(new Vector3(at.x,top.Value-.05f,at.z),raise,.06f,.05f,6,new Color(.2f,.2f,.22f),new Color(.26f,.26f,.28f));
                lamps.Lump(head,new Vector3(.16f,.2f,.16f),Mathf.RoundToInt(at.x*13+at.z*7),.05f,Color.white,Color.white,8,5);
                Halo(parent,head,halo,HGlow);Reflections.Add((head,tone,.6f,.75f,0));
            }
            Light(centre+along*length*.4f,0,new Color(.95f,.95f,1),1);Light(centre-along*length*.42f,0,new Color(1,.8f,.52f),1);Light(centre+along*length*.04f,1.4f,new Color(1,.84f,.56f),1.3f);
            for(int k=0;k<5;k++)
            {
                var at=centre+along*length*(-.22f+k*.09f);
                foreach(int s in new[]{-1,1})
                {
                    var port=at+side*s*.55f;var top=TopAt(surface,new Vector2(port.x,port.z),.45f);if(top==null)continue;
                    var p=new Vector3(port.x,top.Value+.03f,port.z);var u=along*.16f;var w=side*.11f;lit.Quad(p-u-w,p+u-w,p+u+w,p-u+w,Vector3.up,Color.white);
                }
            }
        }
        /// <summary>The hulls in harbour: kit key, where, length, yaw, and the point (with its height) its lines run to. None nearer a leg of the run than 18 m but those against a wall.</summary>
        static readonly (string key,float x,float z,float length,float yaw,Vector3 line)[] Hulls=
        {
            ("Skiff",-44,-.4f,9,90,new Vector3(-44,QuayTop+.3f,-5.6f)),("PatrolCutter",-26,0,11,90,new Vector3(-26,QuayTop+.3f,-5.6f)),("Skiff",22,-.4f,9,270,new Vector3(22,QuayTop+.3f,-5.6f)),
            ("PatrolCutter",37,0,11,270,new Vector3(37,QuayTop+.3f,-5.6f)),
            ("PatrolCutter",-45,47.5f,11,90,new Vector3(-45,PierTop+.3f,43.6f)),("Skiff",-42,32.8f,9,270,new Vector3(-42,PierTop+.3f,36.4f)),
            ("Hauler",-46,101,16,90,new Vector3(-46,PierTop+.3f,95.6f)),("Skiff",-44,84.8f,9,90,new Vector3(-44,PierTop+.3f,88.4f)),
            ("Hauler",44,30,16,15,new Vector3(44,Harbour.Water,46)),("PatrolCutter",40,102,11,-20,new Vector3(36,Harbour.Water,114)),("Hauler",-34,160,16,80,new Vector3(-48,Harbour.Water,162)),
            ("PatrolCutter",-14,54,11,30,new Vector3(-20,Harbour.Water,66)),("Skiff",16,12,9,100,new Vector3(28,Harbour.Water,10)),
            ("Skiff",-51,150,9,0,new Vector3(-56.2f,Harbour.Water+2.2f,150)),("PatrolCutter",-51,196,11,180,new Vector3(-56.2f,Harbour.Water+2.2f,196)),
        };
        static readonly (float x,float z,float yaw)[] Lighters={(-12,-1,92),(8,-1.4f,88),(30,140,30),(-30,108,85),(4,54,20)};
        static readonly (float x,float z)[] ChannelBuoys={(-18,14),(36,14),(-16,36),(14,40),(-22,64),(44,64),(-28,104),(44,84),(-30,150),(42,160),(0,170),(36,184)};

        /// <summary>The Long Patience at her berth, her stern at the slip's head and her bow out past the harbour's line: a carrier's hull made here, gunmetal sides flaring from the water
        /// to a flight deck three and a half metres under the plane, the deck plated and marked, lit along its edges, two of her craft on it, her launch well open and lit at the stern
        /// where the slip comes up to her, and her island to starboard. (The Hauler at 110 m, the first try, was a pale flat floor the size of the frame from her stern: its texture
        /// made for a hull a tenth the size.)</summary>
        internal const float CarrierX=14,CarrierHalf=17,CarrierDeck=-2.5f;
        /// <summary>Whether a point of the plan is over the carrier's deck (her stern half, square: what the harbour line crosses).</summary>
        internal static bool OnTheCarrier(Vector2 p)=>p.y>=SlipHead+1&&Mathf.Abs(p.x-CarrierX)<CarrierHalf-.5f;
        static void Carrier(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water,Deck=CarrierDeck;const float X=CarrierX,Half=CarrierHalf,Waist=282,Bow=334;float Stern=SlipHead+1;
            float HalfAt(float z)=>z<=Waist?Half:Half*Mathf.Max(.05f,1-Mathf.Pow((z-Waist)/(Bow-Waist),1.6f));
            const float Well=11;var stations=new List<float>{Stern};for(float z=Stern+Well;z<Waist;z+=10)stations.Add(z);for(float z=Waist;z<Bow;z+=6.5f)stations.Add(z);stations.Add(Bow);
            var hull=new ZoneKit.Shape();var plate=new ZoneKit.Shape();var lit=new ZoneKit.Shape();var gear=new ZoneKit.Shape();var arcHeads=new ZoneKit.Shape();
            // Painted as the Muster paints its ships (harrow-look2-04 was grey on grey): a deck of dark blue-grey plate, its lanes, rings and crest in bone; the island and a band
            // round the stern in Muster crimson, both upright, never on the deck (red on a floor is a danger marking's colour, D217).
            // (Warm bone lines went orange in the low sun: the deck's paint is a cool white, which the dusk turns cream.)
            Color side=new(.24f,.25f,.28f),deck=new(.28f,.33f,.4f),line=new(.78f,.84f,.88f),crimson=new(.32f,.08f,.08f),bone=new(.72f,.72f,.68f);var root=new GameObject("The Long Patience");root.transform.SetParent(parent,true);
            for(int i=1;i<stations.Count;i++)
            {
                float z0=stations[i-1],z1=stations[i],h0=HalfAt(z0),h1=HalfAt(z1);bool well=i==1;
                if(!well)plate.Quad(new Vector3(X-h0,Deck,z0),new Vector3(X-h1,Deck,z1),new Vector3(X+h1,Deck,z1),new Vector3(X+h0,Deck,z0),Vector3.up,ZoneKit.Shape.Dim(deck,.92f+.1f*(i%3)),.2f);
                else foreach(int s in new[]{-1,1})plate.Quad(new Vector3(X+s*7,Deck,z0),new Vector3(X+s*7,Deck,z1),new Vector3(X+s*h1,Deck,z1),new Vector3(X+s*h0,Deck,z0),Vector3.up,deck,.2f);
                foreach(int s in new[]{-1,1})hull.Quad(new Vector3(X+s*h0*.9f,W-.6f,z0),new Vector3(X+s*h1*.9f,W-.6f,z1),new Vector3(X+s*h1,Deck,z1),new Vector3(X+s*h0,Deck,z0),new Vector3(s,0,0),side,.2f);
            }
            hull.Quad(new Vector3(X-Half*.9f,W-.6f,Stern),new Vector3(X+Half*.9f,W-.6f,Stern),new Vector3(X+Half,Deck,Stern),new Vector3(X-Half,Deck,Stern),Vector3.back,ZoneKit.Shape.Dim(side,.9f),.2f);
            // The launch well at the stern, where the slip's skiffs go in: a dark plated floor two metres down, lit by bars along its walls and a row of lamps up its middle. (Lit
            // over its whole floor it was the brightest thing in the harbour, a blank page under the fight at the slip.)
            plate.Quad(new Vector3(X-7,Deck-2.2f,Stern),new Vector3(X-7,Deck-2.2f,Stern+Well),new Vector3(X+7,Deck-2.2f,Stern+Well),new Vector3(X+7,Deck-2.2f,Stern),Vector3.up,new Color(.26f,.26f,.28f),.25f);
            foreach(int s in new[]{-1,1})
            {
                hull.Quad(new Vector3(X+s*7,Deck-2.2f,Stern),new Vector3(X+s*7,Deck-2.2f,Stern+Well),new Vector3(X+s*7,Deck,Stern+Well),new Vector3(X+s*7,Deck,Stern),new Vector3(-s,0,0),side,.2f);
                lit.Quad(new Vector3(X+s*6.95f,Deck-1.1f,Stern+.6f),new Vector3(X+s*6.95f,Deck-1.1f,Stern+Well-.6f),new Vector3(X+s*6.95f,Deck-.8f,Stern+Well-.6f),new Vector3(X+s*6.95f,Deck-.8f,Stern+.6f),new Vector3(-s,0,0),Color.white,.25f);
            }
            hull.Quad(new Vector3(X-7,Deck-2.2f,Stern+Well),new Vector3(X+7,Deck-2.2f,Stern+Well),new Vector3(X+7,Deck,Stern+Well),new Vector3(X-7,Deck,Stern+Well),Vector3.back,side,.2f);
            // Deck marks: a dashed middle line and a line down each side; landing rings down the deck and the Muster's crest-sail; lights along both edges.
            const float CrestFrom=232,CrestTo=240;
            for(float z=Stern+14;z<Waist+30;z+=6)if(z+3.2f<CrestFrom-1||z>CrestTo+1)gear.Quad(new Vector3(X-.3f,Deck+.03f,z),new Vector3(X-.3f,Deck+.03f,z+3.2f),new Vector3(X+.3f,Deck+.03f,z+3.2f),new Vector3(X+.3f,Deck+.03f,z),Vector3.up,line,.5f);
            foreach(int s in new[]{-1,1})gear.Quad(new Vector3(X+s*12.8f,Deck+.03f,Stern+13),new Vector3(X+s*12.8f,Deck+.03f,Waist),new Vector3(X+s*13.2f,Deck+.03f,Waist),new Vector3(X+s*13.2f,Deck+.03f,Stern+13),Vector3.up,line,.5f);
            foreach(float rz in new[]{224f,256f,288f})gear.Disc(new Vector3(X-4,Deck+.035f,rz),Vector3.up,5,40,line,4.45f);
            gear.Triangle(new Vector3(X-1.4f,Deck+.04f,CrestFrom),new Vector3(X+2.4f,Deck+.04f,CrestFrom+1),new Vector3(X-1.4f,Deck+.04f,CrestTo),Vector3.up,line,.5f);
            gear.Quad(new Vector3(X-2.1f,Deck+.04f,CrestFrom-1),new Vector3(X-2.1f,Deck+.04f,CrestTo+1),new Vector3(X-1.7f,Deck+.04f,CrestTo+1),new Vector3(X-1.7f,Deck+.04f,CrestFrom-1),Vector3.up,line,.5f);
            // The launch well's rim striped bone and black, where a skiff drops in.
            for(float d=0;d<Well+14;d+=1.2f)
            {
                var tone=Mathf.RoundToInt(d/1.2f)%2==0?bone:new Color(.12f,.12f,.13f);
                if(d<Well)foreach(int s in new[]{-1,1})gear.Quad(new Vector3(X+s*7.05f,Deck+.03f,Stern+d),new Vector3(X+s*7.05f,Deck+.03f,Stern+d+1.2f),new Vector3(X+s*7.75f,Deck+.03f,Stern+d+1.2f),new Vector3(X+s*7.75f,Deck+.03f,Stern+d),Vector3.up,tone,.5f);
                else{float x0=X-7+(d-Well);gear.Quad(new Vector3(x0,Deck+.03f,Stern+Well+.05f),new Vector3(x0,Deck+.03f,Stern+Well+.75f),new Vector3(x0+1.2f,Deck+.03f,Stern+Well+.75f),new Vector3(x0+1.2f,Deck+.03f,Stern+Well+.05f),Vector3.up,tone,.5f);}
            }
            // The stern's band of crimson, toward the slip.
            hull.Quad(new Vector3(X-Half*.97f,Deck-1.5f,Stern-.03f),new Vector3(X+Half*.97f,Deck-1.5f,Stern-.03f),new Vector3(X+Half*.99f,Deck-.5f,Stern-.03f),new Vector3(X-Half*.99f,Deck-.5f,Stern-.03f),Vector3.back,crimson,.2f);
            var heads=new ZoneKit.Shape();for(float z=Stern+1.5f;z<Stern+Well;z+=2.4f)heads.Lump(new Vector3(X,Deck-2.1f,z),new Vector3(.2f,.08f,.2f),Mathf.RoundToInt(z),.05f,Color.white,Color.white,6,3);
            for(float z=Stern+2;z<Bow-10;z+=9)foreach(int s in new[]{-1,1}){float h=HalfAt(z)-.7f;heads.Lump(new Vector3(X+s*h,Deck+.2f,z),new Vector3(.22f,.18f,.22f),Mathf.RoundToInt(z)+s,.05f,Color.white,Color.white,6,4);}
            // Her guns at the deck's corners: a turret and its two barrels, laid fore and aft.
            foreach(var (gx,gz) in new[]{(X-13f,Stern+16),(X+13f,Stern+16),(X-13f,Waist-20),(X+13f,Waist-20)})
            {
                gear.Post(new Vector3(gx,Deck,gz),.9f,1.3f,1.1f,10,new Color(.18f,.18f,.2f),new Color(.24f,.24f,.26f));
                foreach(int b in new[]{-1,1})gear.Beam(new Vector3(gx+b*.35f,Deck+.6f,gz+.6f),new Vector3(gx+b*.35f,Deck+.8f,gz+4.2f),.22f,.22f,new Color(.22f,.22f,.24f));
            }
            // Her island, to starboard amidships, in Muster crimson with a bone bridge, its windows lit: two rows on the face toward the slip and the bridge's.
            gear.Box(new Vector3(X+12.5f,Deck+2.5f,252),new Vector3(5,5,30),Quaternion.identity,crimson,bone,.25f);
            gear.Box(new Vector3(X+12.6f,Deck+6.2f,250),new Vector3(3.6f,2.4f,14),Quaternion.identity,bone,crimson,.25f);
            lit.Quad(new Vector3(X+11,Deck+5.6f,242.96f),new Vector3(X+14.2f,Deck+5.6f,242.96f),new Vector3(X+14.2f,Deck+6.6f,242.96f),new Vector3(X+11,Deck+6.6f,242.96f),Vector3.back,Color.white,.25f);
            foreach(float wy in new[]{1.2f,3.1f})for(float wx=-2;wx<2;wx+=1.05f)
                lit.Quad(new Vector3(X+12.5f+wx,Deck+wy,236.97f),new Vector3(X+12.5f+wx+.6f,Deck+wy,236.97f),new Vector3(X+12.5f+wx+.6f,Deck+wy+.7f,236.97f),new Vector3(X+12.5f+wx,Deck+wy+.7f,236.97f),Vector3.back,Color.white,.25f);
            gear.Post(new Vector3(X+12.6f,Deck+7.4f,252),5,.18f,.1f,6,new Color(.24f,.24f,.26f),new Color(.3f,.3f,.32f));
            // Arc lamps on short standards round the deck's edge, on since dusk: a hard blue-white head, its light on the deck round it and on the water off her side. The heads stay
            // under the flight plane, so nothing of hers stands in the slip fight's air.
            var arcs=new ZoneKit.Shape();
            foreach(float az in new[]{Stern+2.5f,Stern+28,Stern+56,Stern+84})foreach(int s in new[]{-1,1})
            {
                var foot=new Vector3(X+s*(Half-.8f),Deck,az);arcs.Post(foot,2.9f,.09f,.07f,6,new Color(.2f,.2f,.22f),new Color(.26f,.26f,.28f));
                var head=foot+new Vector3(-s*.45f,3.05f,0);gear.Box(head+Vector3.up*.18f,new Vector3(.8f,.12f,.5f),Quaternion.identity,new Color(.2f,.2f,.22f),new Color(.24f,.24f,.26f),.5f);
                arcHeads.Lump(head,new Vector3(.3f,.16f,.22f),Mathf.RoundToInt(az*3)+s,.04f,Color.white,Color.white,8,4);Halo(root.transform,head,2.4f,HArcGlow);
                Pools.Add((new Vector3(foot.x-s*5,Deck+.06f,az),6.5f,new Color(.7f,.82f,1,.22f)));Reflections.Add((head,new Color(.78f,.88f,1),1.6f,.8f,s*3.4f));
            }
            Stone(kit,"Carrier arc standards",arcs,HIron,true,root.transform);
            Stone(kit,"Carrier hull",hull,HHull,true,root.transform);Stone(kit,"Carrier deck",plate,HDeck,true,root.transform);Stone(kit,"Carrier fittings",gear,HHull,true,root.transform);
            Stone(kit,"Carrier lights",lit,HLamp,false,root.transform);Stone(kit,"Carrier deck lights",heads,HLamp,false,root.transform);Stone(kit,"Carrier arc lamps",arcHeads,HArc,false,root.transform);
            // Two of her skiffs on the deck by the well, in the Muster's gunmetal.
            if(ZoneKit.Landed("Skiff"))foreach(var (sx,sz,yaw) in new[]{(X-10.5f,Stern+15,8f),(X+9.5f,Stern+20,-6f)})
            {
                var craft=kit.Prop("Skiff",new Vector3(sx,Deck+.02f,sz),9,yaw,false,false);craft.AddComponent<PropTint>().tint=new Color(.5f,.47f,.45f);craft.transform.SetParent(root.transform,true);
            }
            dry.Box(new Vector2(X,(Stern+Waist)*.5f),new Vector2(Half*.9f,(Waist-Stern)*.5f));dry.Ellipse(new Vector2(X,Waist),new Vector2(Half*.88f,Bow-Waist),0);
        }

        /// <summary>The water over the whole harbour and past its edges: a grid at the water line, each point's second uv how far it lies from anything dry, so the shader lays its foam at every
        /// wall, pier, pit, crane footing, buoy and hull, and its deep colour out in the stream.</summary>
        static void HarbourSurface(ZoneKit kit,Transform parent,Dry dry)
        {
            const float Cell=1,X0=-150,X1=170,Z0=-60,Z1=310;float W=Harbour.Water;
            int nx=Mathf.CeilToInt((X1-X0)/Cell)+1,nz=Mathf.CeilToInt((Z1-Z0)/Cell)+1;var points=new Vector3[nx*nz];var uv=new Vector2[points.Length];var shore=new Vector2[points.Length];var up=new Vector3[points.Length];
            var kinds=new List<Vector4>(points.Length);var stone=new float[points.Length];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;var p=new Vector2(X0+x*Cell,Z0+z*Cell);points[i]=new Vector3(p.x,W,p.y);uv[i]=p/20;up[i]=Vector3.up;
                stone[i]=dry.Stone(p);float floating=dry.Floating(p);shore[i]=new Vector2(0,Mathf.Min(12,Mathf.Min(stone[i],floating)));
                kinds.Add(WaterKinds(p,stone[i],floating,dry));
            }
            var faces=new List<int>();
            // A cell wholly inside stone has no water: under a quay it is hidden anyway, and where the stone is low it would be a sheet of foam. What floats has water under it.
            bool Wet(int i)=>stone[i]>.01f;
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++){int i=z*nx+x;if(!Wet(i)&&!Wet(i+1)&&!Wet(i+nx)&&!Wet(i+nx+1))continue;faces.AddRange(new[]{i,i+nx,i+nx+1,i,i+nx+1,i+1});}
            var mesh=new Mesh{name="Harbour water",indexFormat=IndexFormat.UInt32};mesh.vertices=points;mesh.normals=up;mesh.uv=uv;mesh.uv2=shore;mesh.SetUVs(2,kinds);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();
            mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Harbour-water.asset");
            var go=new GameObject("Harbour water",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.transform.SetParent(parent,true);
            var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=HWater;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=true;
        }

        /// <summary>Which of the water's kinds lies at a point (the water material's three layers and a shade, HarbourMaterials): glassy in the lee of the sea wall and the quay and in
        /// patches where the evening's wind drops; the current's streaks behind whatever floats, stronger the further out from the quay the stream runs; lace on the water for a few
        /// metres off stone and round each hull's waterline; and long swells a little darker, so the open water has a grain larger than a frame.</summary>
        static Vector4 WaterKinds(Vector2 p,float stone,float floating,Dry dry)
        {
            float lee=ZoneKit.Smooth(30,3,p.x+56),quay=ZoneKit.Smooth(24,2,p.y+5),gust=Mathf.PerlinNoise(p.x/46+3.1f,p.y/46+7.7f);
            float calm=Mathf.Clamp01((gust-.5f)*3.2f+.12f+.5f*lee+.6f*quay);
            float stream=Mathf.Lerp(.3f,1,ZoneKit.Smooth(6,40,p.y)),wake=0;foreach(var f in dry.floats)wake=Mathf.Max(wake,f.Wake(p));wake*=stream;
            float ragged=(Mathf.PerlinNoise(p.x/3.3f+40,p.y/3.3f+12)-.5f)*2.4f;
            float surf=Mathf.Max(1-ZoneKit.Smooth(.5f,2.1f,stone+ragged*.6f),(1-ZoneKit.Smooth(.2f,.95f,floating+ragged*.3f))*.85f);
            float swell=.09f*ZoneKit.Smooth(.42f,.72f,Mathf.PerlinNoise(p.x/24+11,p.y/14+5));
            return new Vector4(calm,Mathf.Clamp01(wake),Mathf.Clamp01(surf),swell);
        }
        /// <summary>Lays every lamp's light on the water once the harbour stands: a column of short dashes toward the camera from where its image lies (about half its height south of
        /// it, the camera being south and above), wider in the middle and thinning away, each dash on water only; and its pool on whatever is under it.</summary>
        static void HarbourGlints(ZoneKit kit,Transform parent,Dry dry)
        {
            float W=Harbour.Water;var points=new List<Vector3>();var uv=new List<Vector2>();var tones=new List<Color>();var tris=new List<int>();
            void Flat(List<Vector3> pts,List<Vector2> uvs,List<Color> cols,List<int> idx,Vector3 c,float hx,float hz,Color tone)
            {
                int i=pts.Count;pts.Add(c+new Vector3(-hx,0,-hz));pts.Add(c+new Vector3(-hx,0,hz));pts.Add(c+new Vector3(hx,0,hz));pts.Add(c+new Vector3(hx,0,-hz));
                uvs.Add(new Vector2(0,0));uvs.Add(new Vector2(0,1));uvs.Add(new Vector2(1,1));uvs.Add(new Vector2(1,0));for(int k=0;k<4;k++)cols.Add(tone);idx.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
            }
            var rng=new System.Random(1414);
            foreach(var (head,tone,spread,strength,shiftX) in Reflections)
            {
                // Broken by the chop: dashes crowd and run wide near where the image lies and thin out away from it, never evenly spaced (a column of even dashes, the first try,
                // read as a lane painted on the water).
                float high=Mathf.Max(1,head.y-W),from=head.z-.12f*Mathf.Min(high,12),to=head.z-Mathf.Min(.8f*high,10)-1.2f;
                float z=from;int laid=0;
                while(z>to&&laid<40)
                {
                    float t=(from-z)/Mathf.Max(.1f,from-to),r1=(float)rng.NextDouble(),r2=(float)rng.NextDouble(),r3=(float)rng.NextDouble();
                    z-=.12f+.5f*t*t+.3f*r3;
                    float profile=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t*1.4f+.1f)),half=spread*(.2f+.8f*profile)*(.35f+1.1f*r1)*.5f;
                    float x=head.x+shiftX+(r2-.5f)*spread*(.4f+.8f*t);if(dry.Stone(new Vector2(x,z))<.4f)continue;
                    var c=new Color(tone.r,tone.g,tone.b,strength*(1-t*.75f)*(.45f+.55f*r3));
                    Flat(points,uv,tones,tris,new Vector3(x,W+.04f,z),half,.045f+.06f*r2,c);laid++;
                    // Now and then a short dash beside it, as the light breaks on two crests.
                    if(r1>.62f){float side=(r2>.5f?1:-1)*(half+.15f+.3f*r3);Flat(points,uv,tones,tris,new Vector3(x+side,W+.04f,z+.06f),half*.45f,.04f,new Color(c.r,c.g,c.b,c.a*.7f));}
                }
            }
            if(points.Count>0)Lay("Lamp light on the water",points,uv,tones,tris,HGlint);
            var poolPoints=new List<Vector3>();var poolUv=new List<Vector2>();var poolTones=new List<Color>();var poolTris=new List<int>();
            foreach(var (at,radius,tone) in Pools)Flat(poolPoints,poolUv,poolTones,poolTris,at,radius,radius*.85f,tone);
            if(poolPoints.Count>0)Lay("Lamp pools",poolPoints,poolUv,poolTones,poolTris,HPool);
            void Lay(string name,List<Vector3> pts,List<Vector2> uvs,List<Color> cols,List<int> idx,Material look)
            {
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(pts);mesh.SetUVs(0,uvs);mesh.SetColors(cols);mesh.SetNormals(pts.Select(_=>Vector3.up).ToList());mesh.SetTriangles(idx,0);mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-')+".asset");
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.transform.SetParent(parent,true);
                var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=look;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }
        }

        /// <summary>A gantry crane drawn as one: lattice legs of four chords braced crosswise (each standing in its leg's box), lattice girders across their tops, the cab over the middle
        /// with its windows lit, the jib run out north over the water and its hook hanging from it. Dark iron, the cab in the Muster's bone and rust.</summary>
        static void CraneDrawn(ZoneKit kit,Transform parent,string name,Vector2 at,float hx,float hz,float top)
        {
            float W=Harbour.Water,foot=W+1.6f;var iron=new ZoneKit.Shape();var cab=new ZoneKit.Shape();var lit=new ZoneKit.Shape();var arcs=new ZoneKit.Shape();
            // Painted, as a yard paints its cranes so they're seen: the Muster's weathered ochre, the bracing a shade darker, the cab bone under a crimson roof, the legs' feet banded
            // black and bone where hulls pass. (Dark iron, they were three black scaffolds over dark water.)
            Color steel=new(.66f,.48f,.2f),dark=new(.5f,.36f,.16f),bone=new(.78f,.74f,.66f),rust=new(.34f,.1f,.09f);
            // A lattice run between two points: four chords and a brace across each face in each bay.
            void Lattice(Vector3 a,Vector3 b,float wide,float bay)
            {
                var run=b-a;float length=run.magnitude;var dir=run/length;var u=Vector3.Cross(dir,Mathf.Abs(dir.y)>.9f?Vector3.right:Vector3.up).normalized*wide*.5f;var v=Vector3.Cross(dir,u.normalized)*wide*.5f;
                var corners=new[]{u+v,u-v,-u-v,-u+v};foreach(var c in corners)iron.Beam(a+c,b+c,.16f,.16f,steel);
                int bays=Mathf.Max(1,Mathf.RoundToInt(length/bay));
                for(int i=0;i<bays;i++)for(int f=0;f<4;f++)
                {
                    var p0=a+dir*(length*i/bays);var p1=a+dir*(length*(i+1)/bays);var c0=corners[f];var c1=corners[(f+1)%4];
                    if((i+f)%2==0)iron.Beam(p0+c0,p1+c1,.1f,.1f,dark);else iron.Beam(p0+c1,p1+c0,.1f,.1f,dark);
                    iron.Beam(p1+c0,p1+c1,.1f,.1f,dark);
                }
            }
            foreach(var (sx,sz) in new[]{(-1,-1),(1,-1),(-1,1),(1,1)})
            {
                Lattice(new Vector3(at.x+sx*hx,foot,at.y+sz*hz),new Vector3(at.x+sx*hx,top,at.y+sz*hz),1.4f,2.2f);
                for(int band=0;band<6;band++)cab.Box(new Vector3(at.x+sx*hx,foot+.25f+band*.5f,at.y+sz*hz),new Vector3(1.5f,.5f,1.5f),Quaternion.identity,band%2==0?new Color(.12f,.12f,.13f):bone,band%2==0?new Color(.12f,.12f,.13f):bone,.4f);
            }
            foreach(int sz in new[]{-1,1})Lattice(new Vector3(at.x-hx-.7f,top+.6f,at.y+sz*hz),new Vector3(at.x+hx+.7f,top+.6f,at.y+sz*hz),1.2f,2.4f);
            foreach(int sx in new[]{-1,1})Lattice(new Vector3(at.x+sx*hx,top+.6f,at.y-hz-.6f),new Vector3(at.x+sx*hx,top+.6f,at.y+hz+.6f),1f,2.4f);
            // The cab and its machinery house over the middle, and the jib north from it, a little up.
            var cabAt=new Vector3(at.x,top+2.2f,at.y+hz*.3f);cab.Box(cabAt,new Vector3(Mathf.Min(6,hx),2.6f,Mathf.Min(5,hz*1.2f)),Quaternion.identity,bone,new Color(.62f,.58f,.52f),.4f);
            cab.Box(cabAt+new Vector3(0,1.5f,0),new Vector3(Mathf.Min(6,hx)+.4f,.3f,Mathf.Min(5,hz*1.2f)+.4f),Quaternion.identity,rust,rust,.4f);
            lit.Quad(cabAt+new Vector3(-Mathf.Min(6,hx)*.42f,.1f,-Mathf.Min(5,hz*1.2f)*.5f-.02f),cabAt+new Vector3(Mathf.Min(6,hx)*.42f,.1f,-Mathf.Min(5,hz*1.2f)*.5f-.02f),
                cabAt+new Vector3(Mathf.Min(6,hx)*.42f,.8f,-Mathf.Min(5,hz*1.2f)*.5f-.02f),cabAt+new Vector3(-Mathf.Min(6,hx)*.42f,.8f,-Mathf.Min(5,hz*1.2f)*.5f-.02f),Vector3.back,Color.white,.5f);
            var jibFrom=new Vector3(at.x,top+1.4f,at.y+hz);var jibTo=new Vector3(at.x,top+3.2f,at.y+hz+18);Lattice(jibFrom,jibTo,1.1f,2.2f);
            cab.Box(new Vector3(at.x,top+1.6f,at.y-hz-1.4f),new Vector3(3,1.8f,2.2f),Quaternion.identity,new Color(.26f,.26f,.28f),rust,.4f);
            // The hook, hanging from the jib's end to just over the water.
            iron.Rope(jibTo,new Vector3(jibTo.x,W+3.4f,jibTo.z),0,.05f,dark,4,2);cab.Box(new Vector3(jibTo.x,W+3,jibTo.z),new Vector3(.9f,.9f,.6f),Quaternion.identity,rust,bone,.4f);
            var holder=parent;
            // Arc lamps slung under the north girder and at the jib's end, lit for the night's work: their light on the water under them, and lying south across it.
            var hung=new List<Vector3>{new(at.x-hx*.5f,top-.2f,at.y+hz),new(at.x+hx*.5f,top-.2f,at.y+hz),jibTo+Vector3.down*.7f};
            foreach(var head in hung)
            {
                cab.Box(head+Vector3.up*.35f,new Vector3(.9f,.25f,.6f),Quaternion.identity,new Color(.2f,.2f,.22f),new Color(.24f,.24f,.26f),.5f);
                arcs.Lump(head,new Vector3(.34f,.18f,.26f),Mathf.RoundToInt(head.x*11+head.z*3),.04f,Color.white,Color.white,8,4);Halo(holder,head,3,HArcGlow);
                Pools.Add((new Vector3(head.x,W+.05f,head.z),7,new Color(.7f,.82f,1,.16f)));Reflections.Add((head,new Color(.78f,.88f,1),1.5f,.6f,0));
            }
            Stone(kit,name+" iron",iron,HIron,true,holder);Stone(kit,name+" cab",cab,HHull,true,holder);Stone(kit,name+" windows",lit,HLamp,false,holder);Stone(kit,name+" arc lamps",arcs,HArc,false,holder);
        }
    }
}
