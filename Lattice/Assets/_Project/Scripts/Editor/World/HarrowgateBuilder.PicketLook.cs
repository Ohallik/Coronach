using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>What the Picket of Teeth is drawn as, after the lead's second look (November 14: "the rocks are squared brown blocks and the backdrop is mostly empty dark blue"). Rock is
    /// made here in code, every face flat, in builds that differ in outline (a jagged lump, a lump with spurs, a split boulder, a slab, a lump with a blade through it, a heap; and the
    /// teeth: a lone fang, a pair, a crown, a broken stump with a shard against it, a leaning blade), where the generated ridge rocks were one stratified block turned many ways. Scoria
    /// lies large under the field. Nothing here is solid: the lane's rocks keep their sphere bodies and shadows, the wall its two boxes, the lane its walls.</summary>
    public static partial class HarrowgateBuilder
    {
        static Material RockLook,RockDeep;
        /// <summary>Basalt for every rock of the Vaskar Reach (the Picket's and the harbour's shore): fine dark grain, vesicles, pale flecks and faint bands, painted near neutral and toned in the
        /// vertices, lighter where a face looks up.</summary>
        static void RockMaterials()
        {
            var basalt=ZoneKit.Texture("Vaskar/picket-basalt",512,(u,v)=>
            {
                float grain=ZoneKit.Tile(u*160,v*160,160,71),coarse=ZoneKit.Cloud(u,v,8,72),band=Mathf.Sin((v*9+coarse*1.4f)*Mathf.PI*2)*.5f+.5f;
                float pit=ZoneKit.Tile(u*96,v*96,96,73)>.86f?-.18f:0,fleck=ZoneKit.Tile(u*220,v*220,220,74)>.94f?.14f:0;
                // Fissures: a ridged level of noise, cut thin and dark, so a face reads as cracked stone and not a painted plane.
                float fissure=ZoneKit.Smooth(.9f,.97f,1-Mathf.Abs(2*ZoneKit.Cloud(u+.2f*coarse,v,6,75)-1));
                float tone=.6f+.2f*(grain-.5f)+.2f*(coarse-.5f)+.07f*(band-.5f)+pit+fleck-.3f*fissure;return new Color(tone,tone*.99f,tone*1.02f,1);
            });
            RockLook=ZoneKit.Toon("vaskar-rock",basalt,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.4f,.42f,.56f));m.SetFloat("_LightFloor",.3f);m.SetColor("_RimColor",new Color(.6f,.66f,.8f));});
            // Debris further down: the same stone a little dimmer and with no rim, so it stays behind the lane's own rocks.
            RockDeep=ZoneKit.Toon("vaskar-rock-deep",basalt,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",new Color(.8f,.8f,.84f));m.SetColor("_ShadowTint",new Color(.36f,.38f,.52f));m.SetFloat("_LightFloor",.24f);m.SetColor("_RimColor",Color.black);});
        }

        // ---------------------------------------------------------------- rock in code
        /// <summary>A rock whose every face is flat: rings of points about a middle, each pushed in or out by up to <paramref name="jag"/> of its radius and about one in nine drawn out
        /// into a point. A face is lighter the more it looks up and each is a little apart in tone, so the facets read from the flight camera as broken stone, never a smooth lump.</summary>
        internal static void Crag(ZoneKit.Shape shape,Vector3 centre,Vector3 radii,Quaternion turn,int seed,float jag,Color top,Color under,int around=8,int rings=5)
        {
            var rng=new System.Random(seed);
            float Push()=>1+((float)rng.NextDouble()-.5f)*2*jag+(rng.Next(9)==0?jag*1.3f:0);
            Vector3 At(float lat,float lon,float push)=>centre+turn*Vector3.Scale(new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon)),radii)*push;
            var north=At(0,0,Push());var south=At(Mathf.PI,0,Push());var ring=new Vector3[rings-1,around];
            for(int j=1;j<rings;j++)for(int k=0;k<around;k++)
                ring[j-1,k]=At((j+((float)rng.NextDouble()-.5f)*.3f)*Mathf.PI/rings,(k+(j%2)*.5f+((float)rng.NextDouble()-.5f)*.4f)*Mathf.PI*2/around,Push());
            void Face(Vector3 a,Vector3 b,Vector3 c)
            {
                var mid=(a+b+c)/3;var n=Vector3.Cross(b-a,c-a);if(n.sqrMagnitude<1e-10f)return;if(Vector3.Dot(n,mid-centre)<0)n=-n;
                float up=n.normalized.y*.5f+.5f;shape.Triangle(a,b,c,n,ZoneKit.Shape.Dim(Color.Lerp(under,top,up*up),.72f+.38f*(float)rng.NextDouble()),.3f);
            }
            for(int k=0;k<around;k++){int n=(k+1)%around;Face(north,ring[0,k],ring[0,n]);Face(south,ring[rings-2,n],ring[rings-2,k]);}
            for(int j=0;j<rings-2;j++)for(int k=0;k<around;k++){int n=(k+1)%around;Face(ring[j,k],ring[j,n],ring[j+1,k]);Face(ring[j,n],ring[j+1,n],ring[j+1,k]);}
        }
        /// <summary>A fang of rock from its foot along <paramref name="axis"/>: rings of a few points narrowing toward a crooked point, every face flat and its foot capped. <paramref name="flat"/>
        /// under 1 makes it a blade; <paramref name="broken"/> stops it short in a jagged crown.</summary>
        internal static void Fang(ZoneKit.Shape shape,Vector3 foot,Vector3 axis,float tall,float wide,float flat,int sides,int seed,Color top,Color under,bool broken=false)
        {
            var rng=new System.Random(seed);axis.Normalize();
            var across=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;var other=Vector3.Cross(axis,across);
            float spin=(float)rng.NextDouble()*6.283f,reach=broken?.5f+.2f*(float)rng.NextDouble():.8f;
            var bend=(across*((float)rng.NextDouble()-.5f)+other*((float)rng.NextDouble()-.5f))*tall*.2f;
            var spread=new float[sides];for(int k=0;k<sides;k++)spread[k]=.75f+.5f*(float)rng.NextDouble();
            const int Levels=4;var rings=new Vector3[Levels+1,sides];var middles=new Vector3[Levels+1];
            for(int i=0;i<=Levels;i++)
            {
                float t=i/(float)Levels*reach,w=wide*Mathf.Pow(Mathf.Max(.02f,1-t),.8f);var c=foot+axis*tall*t+bend*t*t;middles[i]=c;
                for(int k=0;k<sides;k++)
                {
                    float a=spin+k*6.283f/sides+((float)rng.NextDouble()-.5f)*.3f+i*.08f,r=w*spread[k]*(.88f+.24f*(float)rng.NextDouble());
                    rings[i,k]=c+(across*Mathf.Cos(a)+other*Mathf.Sin(a)*flat)*r;
                }
            }
            // A broken top: every other point of the last ring raised, round a hollow.
            if(broken)for(int k=0;k<sides;k+=2)rings[Levels,k]+=axis*wide*(.3f+.5f*(float)rng.NextDouble());
            float Shade()=>.84f+.24f*(float)rng.NextDouble();
            for(int i=0;i<Levels;i++)for(int k=0;k<sides;k++)
            {
                int n=(k+1)%sides;Vector3 a=rings[i,k],b=rings[i,n],c=rings[i+1,n],d=rings[i+1,k];var outward=(a+b+c+d)*.25f-(middles[i]+middles[i+1])*.5f;
                var tone=ZoneKit.Shape.Dim(Color.Lerp(under,top,(i+.5f)/Levels*reach+.15f),Shade());
                shape.Triangle(a,b,c,outward,tone,.3f);shape.Triangle(a,c,d,outward,ZoneKit.Shape.Dim(tone,.93f),.3f);
            }
            var last=middles[Levels];
            var tip=broken?last-axis*wide*.12f:foot+axis*tall+bend+(across*((float)rng.NextDouble()-.5f)+other*((float)rng.NextDouble()-.5f))*wide*.3f;
            for(int k=0;k<sides;k++){int n=(k+1)%sides;Vector3 a=rings[Levels,k],b=rings[Levels,n];shape.Triangle(a,b,tip,broken?axis:(a+b+tip)/3-last,ZoneKit.Shape.Dim(top,Shade()),.3f);}
            var root=foot-axis*wide*.25f;
            for(int k=0;k<sides;k++){int n=(k+1)%sides;shape.Triangle(rings[0,k],rings[0,n],root,-axis,ZoneKit.Shape.Dim(under,Shade()),.3f);}
        }
        /// <summary>A rock filling a circle of <paramref name="radius"/> about its middle, in one of six builds: a jagged lump, a lump with spurs, a boulder split in two, a slab, a lump with a
        /// blade through it, a heap of five. Each fills its circle to within a few tenths, so a lane rock's shadow and body are the stone a player sees.</summary>
        static void Rock(ZoneKit.Shape shape,Vector3 centre,float radius,Quaternion turned,int build,int seed,Color tone)
        {
            var under=ZoneKit.Shape.Dim(tone,.38f);var top=ZoneKit.Shape.Dim(tone,1.32f);var rng=new System.Random(seed);
            switch(build%6)
            {
                case 0:Crag(shape,centre,new Vector3(1,.8f,.92f)*radius,turned,seed,.22f,top,under,12,7);break;
                case 1:
                    Crag(shape,centre,new Vector3(.84f,.74f,.8f)*radius,turned,seed,.2f,top,under,10,6);
                    for(int s=0;s<3;s++){var d=turned*Quaternion.Euler(rng.Next(360),rng.Next(360),0)*Vector3.up;Fang(shape,centre+d*radius*.35f,d,radius*.72f,radius*.3f,1,5,seed+s+1,top,under);}
                    break;
                case 2:
                {
                    var split=turned*Vector3.right;
                    Crag(shape,centre-split*radius*.38f,new Vector3(.62f,.84f,.8f)*radius,turned,seed,.18f,top,under,8,5);
                    Crag(shape,centre+split*radius*.42f,new Vector3(.56f,.76f,.82f)*radius,turned*Quaternion.Euler(0,0,12),seed+1,.18f,top,under,8,5);
                    break;
                }
                case 3:Crag(shape,centre,new Vector3(1,.46f,.9f)*radius,turned,seed,.24f,top,under,11,5);break;
                case 4:
                {
                    Crag(shape,centre,new Vector3(.86f,.7f,.78f)*radius,turned,seed,.2f,top,under,10,6);
                    var d=turned*Vector3.forward;Fang(shape,centre-d*radius*.9f,d,radius*2,radius*.36f,.55f,5,seed+3,top,under);
                    break;
                }
                default:
                    Crag(shape,centre,Vector3.one*.62f*radius,turned,seed,.2f,top,under,7,5);
                    for(int s=0;s<4;s++)
                    {
                        float a=s*1.57f+(float)rng.NextDouble();var d=turned*new Vector3(Mathf.Cos(a),((float)rng.NextDouble()-.5f)*.8f,Mathf.Sin(a)).normalized;
                        Crag(shape,centre+d*radius*.6f,Vector3.one*radius*(.3f+.12f*(float)rng.NextDouble()),turned*Quaternion.Euler(0,s*70,0),seed+5+s,.22f,top,under,6,4);
                    }
                    break;
            }
        }
        /// <summary>A tooth of the picket standing through the plane: a jagged foot and on it one of five builds (a lone fang, a pair, a crown of three or four, a broken stump with a shard
        /// fallen against it, a leaning blade). Its middle is a little under the plane, so the camera looks down on its upper half and a hull meets its waist.</summary>
        static void Tooth(ZoneKit.Shape shape,Vector3 at,float tall,float wide,int seed,float footing=1)
        {
            var rng=new System.Random(seed);int build=rng.Next(5);float shade=.86f+.24f*(float)rng.NextDouble();
            var top=ZoneKit.Shape.Dim(new Color(.5f,.49f,.52f),shade);var under=ZoneKit.Shape.Dim(new Color(.15f,.15f,.19f),shade);
            var foot=at+Vector3.down*tall*.55f;
            Vector3 Lean(float by)=>(Vector3.up+new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f)*by).normalized;
            Crag(shape,foot+Vector3.up*wide*.3f*footing,new Vector3(wide*1.4f,wide*.9f,wide*1.3f)*footing,Quaternion.Euler(0,rng.Next(360),0),seed+1,.24f,Color.Lerp(under,top,.45f),under,8,4);
            switch(build)
            {
                case 0:Fang(shape,foot,Lean(.3f),tall,wide,1,6,seed+2,top,under);break;
                case 1:
                {
                    Fang(shape,foot,Lean(.25f),tall,wide,1,6,seed+2,top,under);
                    float a=(float)rng.NextDouble()*6.283f;var off=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));Fang(shape,foot+off*wide*.7f,(Vector3.up+off*.5f).normalized,tall*.66f,wide*.7f,1,5,seed+3,top,under);
                    break;
                }
                case 2:
                {
                    int count=3+rng.Next(2);
                    for(int c=0;c<count;c++)
                    {
                        float a=c*6.283f/count+(float)rng.NextDouble()*.6f;var off=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                        Fang(shape,foot+off*wide*.55f,(Vector3.up+off*.45f).normalized,tall*(.5f+.45f*(float)rng.NextDouble()),wide*.62f,1,5,seed+4+c,top,under);
                    }
                    break;
                }
                case 3:
                {
                    Fang(shape,foot,Lean(.2f),tall*.85f,wide*1.15f,1,7,seed+2,top,under,true);
                    // A shard broken off it, fallen and leaning back against the stump.
                    float a=(float)rng.NextDouble()*6.283f;var off=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var from=foot+off*wide*1.6f+Vector3.up*tall*.08f;var to=foot+Vector3.up*tall*.42f;
                    Fang(shape,from,to-from,Vector3.Distance(from,to)*1.1f,wide*.5f,.7f,5,seed+3,top,under);
                    break;
                }
                default:Fang(shape,foot,Lean(.6f),tall,wide*1.2f,.42f,5,seed+2,top,under);break;
            }
        }
        /// <summary>A built shape stood in the scene with the zone's stone, casting its shadow or not, with a check that no point of it went to NaN (a NaN takes the whole mesh's bounds with it,
        /// and the mesh is culled from every frame).</summary>
        static GameObject Stone(ZoneKit kit,string name,ZoneKit.Shape shape,Material look,bool shadows,Transform parent)=>Stone(kit,name,shape,new[]{look},shadows,parent);
        static GameObject Stone(ZoneKit kit,string name,ZoneKit.Shape shape,Material[] looks,bool shadows,Transform parent)
        {
            var part=kit.Built(name,shape,looks);var view=part.GetComponent<MeshRenderer>();view.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;
            var bounds=part.GetComponent<MeshFilter>().sharedMesh.bounds;if(float.IsNaN(bounds.size.x)||float.IsInfinity(bounds.size.x))throw new InvalidOperationException(kit.zone+": "+name+" has a point that isn't a number");
            if(parent!=null)part.transform.SetParent(parent,true);return part;
        }

        // ---------------------------------------------------------------- Scoria, and the haze between
        /// <summary>Scoria from orbit, for the half of the ball the camera sees: old crust in broad tones, obsidian lowlands nearly black, a few long lava rivers with finer branches, the hot
        /// rings of its calderas, and ash blown off them that dims what lies under it. Every line of melt is thin and hard-edged so it holds at the screen's pixel; deep red, not orange,
        /// because orange on the plane is a danger marking's colour and this lies behind them. <paramref name="heat"/> is the glow.</summary>
        static Color ScoriaFrom(float u,float v,(float u,float v,float r)[] calderas,out float heat)
        {
            float wu=u+.04f*(ZoneKit.Cloud(u,v,4,51)-.5f),wv=v+.04f*(ZoneKit.Cloud(u,v,4,52)-.5f);
            float region=ZoneKit.Cloud(wu,wv,4,53),low=ZoneKit.Smooth(.57f,.61f,ZoneKit.Cloud(wu,wv,3,54));
            float rock=.19f+.1f*(region-.5f)+.06f*(ZoneKit.Cloud(u,v,20,55)-.5f)+.05f*(ZoneKit.Tile(u*384,v*384,384,56)-.5f);rock=Mathf.Lerp(rock,.07f,low*.85f);
            float ridge=1-Mathf.Abs(2*ZoneKit.Cloud(wu,wv,5,57)-1),branch=1-Mathf.Abs(2*ZoneKit.Cloud(wu,wv,11,58)-1);
            float river=ZoneKit.Smooth(.972f,.988f,ridge)*ZoneKit.Smooth(.38f,.5f,ZoneKit.Cloud(u,v,3,59));
            float twig=ZoneKit.Smooth(.982f,.993f,branch)*ZoneKit.Smooth(.44f,.56f,ZoneKit.Cloud(u,v,4,60))*.7f;
            // Calderas: dark pits. Rings of melt round them read, beside a ship, as marked circles, and so did an ember at the bottom of each.
            float pit=0,ember=0;
            foreach(var (cu,cv,r) in calderas)
            {
                float du=Mathf.Repeat(u-cu+.5f,1)-.5f,dv=v-cv,d=Mathf.Sqrt(du*du+dv*dv)/r;if(d>1.4f)continue;
                pit=Mathf.Max(pit,1-ZoneKit.Smooth(.75f,1.05f,d));ember=Mathf.Max(ember,1-ZoneKit.Smooth(.12f,.3f,d));
            }
            // Ash: broad pale plumes blown off the calderas, the face's brightest stuff, dimming the melt under them.
            float ash=ZoneKit.Smooth(.58f,.78f,ZoneKit.Cloud(u+.15f*v,v,7,62))*.6f;
            heat=Mathf.Clamp01(Mathf.Max(river,twig)*(1-ash*.85f));_=ember;
            var ground=new Color(rock*1.04f,rock*.97f,rock*.93f);ground=Color.Lerp(ground,ground*.45f,pit);ground=Color.Lerp(ground,new Color(.38f,.35f,.33f),ash);
            return Color.Lerp(ground,new Color(.55f,.18f,.08f),heat);
        }
        /// <summary>Scoria, large under the field: a whole ball 96 m in radius, 420 m down, its middle where the camera's middle looks with the lead ship at z 450, 170 m west of the lane, so
        /// it lies in the left of the frame wherever in the lane the ship is, never under it (its melt is no marking, but nothing red should sit beside a ship: at 30 m west it did). So deep, and no deeper, because the game's camera draws nothing past 1000 m: the disc comes into the top of the frame inside that, and is whole from the first tooth to
        /// the wall. At that depth its face (the whole sheet spent on the half the camera sees) is as dense as the screen's pixels. The first Scoria, 70 m and set far north, showed
        /// only past the wall, and the field's backdrop was empty dark blue.</summary>
        static void ScoriaBelow(ZoneKit kit)
        {
            const float Depth=420,Radius=96;var onPlane=new Vector3(-170,ZoneKit.FlightPlane,450);
            var at=new Vector3(onPlane.x,ZoneKit.FlightPlane-Depth,onPlane.z+Depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
            var ball=kit.Built("Scoria",new ZoneKit.Shape().Lump(at,Vector3.one*Radius,7,.004f,Color.white,new Color(.6f,.5f,.48f),96,48,1/(Radius*3),Quaternion.Euler(0,0,90)),ScoriaFace);
            var view=ball.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
        /// <summary>Scoria's moon over the approach, east of the lane: the first frames have a world in them before Scoria itself rises into the top of the frame. A cratered face
        /// (the Spore Veil's crater sheet, in the Reach's ash grey) lit a little from itself, 300 m down and 30 m across its radius, so its face is as dense as the screen there.</summary>
        static void ScoriaMoon(ZoneKit kit)
        {
            var face=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Wendmire/wend-moon-c.png");var tone=new Color(.74f,.7f,.68f);
            var look=ZoneKit.Toon("vaskar-moon",face,m=>{m.SetColor("_BaseColor",tone*.62f);if(face!=null)m.SetTexture("_EmissionMap",face);m.SetColor("_EmissionColor",tone*.3f);m.SetFloat("_ReceiveShadows",0);
                m.SetFloat("_RampSteps",6);m.SetColor("_RimColor",Color.black);ZoneKit.With(m,"_DETAIL_ON","_Detail");m.SetColor("_DetailTintA",new Color(.8f,.8f,.84f));m.SetColor("_DetailTintB",new Color(1.15f,1.12f,1.1f));
                m.SetVector("_DetailParams",new Vector4(.05f,.9f,.2f,.35f));});
            const float Depth=300,Radius=30;var onPlane=new Vector3(64,ZoneKit.FlightPlane,64);
            var at=new Vector3(onPlane.x,ZoneKit.FlightPlane-Depth,onPlane.z+Depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
            var ball=kit.Built("Scorias moon",new ZoneKit.Shape().Lump(at,Vector3.one*Radius,9,.008f,Color.white,new Color(.5f,.48f,.52f),64,32,1/(Radius*3),Quaternion.Euler(0,0,90)),look);
            var view=ball.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
        /// <summary>Dead hulls of the picket under the approach, where the first frames were bare stars (what the party is flying into, before it is met), and one between the two ways
        /// down, where the frame was empty.</summary>
        static void ApproachWrecks(ZoneKit kit,List<(Vector2 at,float clear)> keep)
        {
            var root=new GameObject("Wrecks under the approach");
            foreach(var (key,at,length,turn) in new[]{("Hauler",new Vector3(13,-15,32),22f,new Vector3(18,35,64)),("PatrolCutter",new Vector3(-17,-10,90),11f,new Vector3(-28,200,36)),
                ("Hauler",new Vector3(4,-14,797),20f,new Vector3(-24,140,-50))})
            {
                if(!ZoneKit.Landed(key)||!ZoneKit.ClearOnScreen(at,length*.3f,keep))continue;
                var hull=kit.Loose(key,at,length,Quaternion.Euler(turn),new Color(.3f,.29f,.31f));hull.name="Dead "+key;hull.transform.SetParent(root.transform,true);
                foreach(var r in hull.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        /// <summary>The ways down: under each landing a glide path of lamps stepping down and away toward Scoria, pale and steady, so a landing reads as a way down from the frame it is in.</summary>
        static void WaysDown(ZoneKit kit)
        {
            var lamp=ZoneKit.Toon("vaskar-way-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.95f,.95f,.9f));m.SetColor("_EmissionColor",new Color(1.6f,1.55f,1.4f));});
            var floats=new ZoneKit.Shape();var heads=new ZoneKit.Shape();var root=new GameObject("The ways down");
            foreach(var way in new[]{Picket.ScoriaWay,Picket.TownWay})for(int k=1;k<=6;k++)foreach(int s in new[]{-1,1})
            {
                var at=new Vector3(way.x+s*(3.5f+k*.4f),ZoneKit.FlightPlane-3-k*4.5f,way.z-4+k*7);
                floats.Lump(at,new Vector3(.9f,.5f,.9f),k*7+s,.08f,new Color(.4f,.4f,.44f),new Color(.2f,.2f,.22f),8,5);heads.Lump(at+Vector3.up*.6f,new Vector3(.3f,.36f,.3f),k*9+s,.05f,Color.white,Color.white,8,5);
                var glow=GameObject.CreatePrimitive(PrimitiveType.Quad);glow.name="Way lamp";UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());glow.transform.SetParent(root.transform,false);
                glow.transform.position=at+Vector3.up*.6f;glow.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);glow.transform.localScale=Vector3.one*2.2f;
                var view=glow.GetComponent<Renderer>();view.sharedMaterial=HaloGlow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }
            Stone(kit,"Way floats",floats,RockDeep,false,root.transform);Stone(kit,"Way lamps",heads,lamp,false,root.transform);
        }
        /// <summary>Ash and dust far under the lane, lying the way it runs and faded in at both ends, so the dark between the teeth is never bare: ash grey, steel, and a dull ember, faint. Haze
        /// behind the rule, never a marking's colour on the plane.</summary>
        static void AshBands(ZoneKit kit)
        {
            var across=ZoneKit.Texture("Vaskar/picket-band",512,(u,v)=>
            {
                float edge=Mathf.Pow(Mathf.Sin(v*Mathf.PI),1.6f),w=v+.2f*(ZoneKit.Cloud(u,v,3,81)-.5f);
                float threads=1-Mathf.Abs(2*ZoneKit.Tile(u*5,w*20,20,83)-1),fine=1-Mathf.Abs(2*ZoneKit.Tile(u*9,w*44,44,87)-1),body=ZoneKit.Cloud(u,v,4,91);
                float lane=ZoneKit.Smooth(.54f,.68f,ZoneKit.Tile(u*3.2f,w*9,9,93));
                return new Color(1,1,1,Mathf.Clamp01(edge*(.38f*body+.5f*Mathf.Pow(threads,4)+.3f*Mathf.Pow(fine,8))*(1-.8f*lane)*1.5f));
            },false,true);
            var soft=kit.Glow("Ash band",false,Color.white);soft.SetTexture("_BaseMap",across);var root=new GameObject("Ash bands");
            foreach(var (z,x,deep,wide,span,tone) in new[]{(60f,-28f,70f,90f,300f,new Color(.62f,.58f,.56f,.3f)),(250f,34f,95f,110f,340f,new Color(.44f,.54f,.7f,.3f)),(440f,-44f,80f,100f,320f,new Color(.66f,.42f,.36f,.22f)),
                (620f,30f,100f,110f,340f,new Color(.6f,.57f,.55f,.28f)),(800f,-24f,85f,100f,300f,new Color(.44f,.54f,.7f,.28f))})
            {
                float shift=deep/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad);var from=new Vector3(x,ZoneKit.FlightPlane-deep,z-span*.5f+shift);var to=new Vector3(x+((z%3)-1)*20,ZoneKit.FlightPlane-deep,z+span*.5f+shift);
                var band=ZoneKit.Strand(root.transform,from,to,soft,wide,tone,tone);var fade=new Gradient();
                fade.SetKeys(new[]{new GradientColorKey(tone,0),new GradientColorKey(tone,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(tone.a,.35f),new GradientAlphaKey(tone.a,.65f),new GradientAlphaKey(0,1)});
                band.colorGradient=fade;band.alignment=LineAlignment.TransformZ;band.transform.rotation=Quaternion.Euler(90,0,0);band.textureMode=LineTextureMode.Tile;
                band.positionCount=9;for(int k=0;k<9;k++)band.SetPosition(k,band.transform.InverseTransformPoint(Vector3.Lerp(from,to,k/8f)+Vector3.right*Mathf.Sin(k*.8f+z)*10));
            }
        }
    }
}
