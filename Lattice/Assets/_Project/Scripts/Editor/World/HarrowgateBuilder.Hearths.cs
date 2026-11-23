using System.Collections.Generic;
using Lattice.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Fire, for the two chapter 8 ground zones that burn things: the Bridle Foundry's smithy and the Flows' cookfires, the Unsworn camp's fire and its forge. The whole-game capture
    /// (November 20) found every one of them a flat orange polygon in a ring of stones (s46/166, s47/117, s49/194), the kettle-stone fires an errand is built round among them. A fire is
    /// now a bed of coals with the fire in the gaps between them (the same picture is their light), flame tongues on crossed sheets leaning back to the fixed camera so they stand up on
    /// screen, flames, embers and smoke going up, and a warm light that breathes with the coals. Its lights are named "Fire light", never "Lamp light": the ground kit lays a cold
    /// work lamp's pool under each of those.
    /// </summary>
    public static partial class HarrowgateBuilder
    {
        internal static class Hearths
        {
            /// <summary>The coals' own light at its middle breath, and what the bed's lumps are.</summary>
            internal static readonly Color CoalGlow=new(1.7f,.66f,.22f);
            static Material coals,flameSolid;static readonly Dictionary<string,(Material flame,Material embers,Material smoke)> glows=new();
            /// <summary>Coke and charcoal in lumps, black, with the fire in the gaps between them and inside a few: one texture for its colour and its light.</summary>
            internal static Material Coals
            {
                get
                {
                    if(coals!=null)return coals;
                    var map=ZoneKit.Texture("Vaskar/fire-coals",256,(u,v)=>
                    {
                        float ridge=1-Mathf.Abs(2*ZoneKit.Cloud(u,v,7,301)-1),fine=1-Mathf.Abs(2*ZoneKit.Cloud(u,v,15,303)-1),heart=ZoneKit.Cloud(u,v,4,305);
                        float gap=Mathf.Max(ZoneKit.Smooth(.82f,.96f,ridge),.7f*ZoneKit.Smooth(.86f,.97f,fine)),lit=Mathf.Clamp01(gap+.35f*ZoneKit.Smooth(.6f,.85f,heart));
                        float dark=.1f+.06f*ZoneKit.Cloud(u,v,20,307);float c=Mathf.Lerp(dark,1,lit);
                        return new Color(c,c*(.36f+.5f*lit),c*(.14f+.4f*lit*lit),1);
                    });
                    coals=ZoneKit.Toon("vaskar-coals",map,m=>{m.SetTexture("_EmissionMap",map);m.SetColor("_EmissionColor",CoalGlow);m.SetColor("_BaseColor",new Color(.32f,.27f,.25f));m.SetFloat("_LightFloor",.18f);});
                    return coals;
                }
            }
            /// <summary>The flame sheet (tongues licking up off a bright root, hot yellow at the core going to orange at the edge, ragged at the tips), the embers and the smoke, made once
            /// a zone: a glow is saved beside the scene.</summary>
            static (Material flame,Material embers,Material smoke) Glows(ZoneKit kit)
            {
                if(glows.TryGetValue(kit.zone,out var made)&&made.flame!=null)return made;
                var tongues=ZoneKit.Texture("Vaskar/fire-flame",128,(u,v)=>
                {
                    float x=u-.5f,sway=.08f*Mathf.Sin(v*9+u*2)*v+.05f*Mathf.Sin(v*17+3)*v*v;x+=sway;
                    float body=.42f*Mathf.Pow(Mathf.Max(0,1-v),.55f);
                    // Three tongues: the middle one tallest, a lick either side.
                    float split=Mathf.Abs(Mathf.Sin((x+.5f)*Mathf.PI*3+v*2.2f));float tongue=Mathf.Lerp(1,split,ZoneKit.Smooth(.25f,.8f,v));
                    float shape=ZoneKit.Smooth(body,body*.55f,Mathf.Abs(x))*ZoneKit.Smooth(0,.07f,v)*Mathf.Clamp01(tongue*1.4f-.25f*v);
                    float rag=ZoneKit.Tile(u*12,v*10,12,311);shape*=1-ZoneKit.Smooth(.55f,.95f,v)*ZoneKit.Smooth(.3f,.7f,rag);
                    float core=ZoneKit.Smooth(body*.75f,body*.15f,Mathf.Abs(x))*(1-v);
                    var c=Color.Lerp(new Color(1,.42f,.12f),new Color(1,.9f,.58f),core);return new Color(c.r,c.g,c.b,Mathf.Clamp01(shape));
                },false,true);
                var flame=kit.Glow("Fire flame",false,new Color(1.6f,1.05f,.6f,1));flame.SetTexture("_BaseMap",tongues);flame.SetFloat("_Cull",0);
                var embers=kit.Glow("Fire embers",false,new Color(1.9f,1.05f,.5f,1));var smoke=kit.Glow("Fire smoke",true,new Color(.7f,.62f,.58f,1));
                glows[kit.zone]=(flame,embers,smoke);return (flame,embers,smoke);
            }
            /// <summary>The zone's flame sheet, for a fire drawn without a bed (a flame licking out of a furnace's door).</summary>
            internal static Material Flame(ZoneKit kit)=>Glows(kit).flame;
            static ParticleSystem Rise(ZoneKit kit,string name,Vector3 at,Vector3 size,Material glow,Color colour,int count,float small,float large,float climb,float life,Transform under)
            {
                var motes=kit.Drift(name,at,size,glow,colour,count,small,large,0,life);motes.transform.SetParent(under,true);
                var velocity=motes.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
                velocity.x=new ParticleSystem.MinMaxCurve(-.12f,.12f);velocity.y=new ParticleSystem.MinMaxCurve(climb*.6f,climb*1.3f);velocity.z=new ParticleSystem.MinMaxCurve(-.12f,.12f);
                var noise=motes.noise;noise.enabled=true;noise.strength=small<.2f?.35f:.12f;noise.frequency=.6f;noise.scrollSpeed=.4f;
                return motes;
            }
            /// <summary>A fire whose bed of coals lies about <paramref name="bed"/> (a world point: the top of whatever it burns on), <paramref name="radius"/> across, its flames standing
            /// <paramref name="tall"/> over it; its light <paramref name="range"/> and <paramref name="intensity"/>. Drawn only: whatever holds it is the caller's, and so is its solid.</summary>
            internal static GameObject Fire(ZoneKit kit,string name,Vector3 bed,float radius,float tall,float range,float intensity,int seed)
            {
                var root=new GameObject(name);root.transform.position=bed;var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                // The bed: a low heap of lumps, highest in its middle, a few spilt at its edge.
                var heap=new ZoneKit.Shape();int lumps=Mathf.Clamp(Mathf.RoundToInt(radius*radius*44),6,40);
                for(int k=0;k<lumps;k++)
                {
                    float a=R()*6.283f,d=radius*Mathf.Sqrt(R())*.92f,s=radius*(.15f+.12f*R());
                    var p=bed+new Vector3(Mathf.Cos(a)*d,(1-d/radius)*radius*.26f+s*.2f,Mathf.Sin(a)*d);
                    heap.Lump(p,new Vector3(s,s*.66f,s*.86f),seed+k,.38f,Color.white,new Color(.74f,.7f,.68f),7,4,.6f,Quaternion.Euler(0,R()*360,0));
                }
                // Tongues of flame standing off the bed, solid in the coals' own light, the tallest at its middle: a fire stands up (and is a thing standing, not a glow on the floor).
                int tongues=Mathf.Clamp(Mathf.RoundToInt(radius*12),4,8);
                for(int k=0;k<tongues;k++)
                {
                    float a=k*6.283f/tongues+R()*.6f,d=radius*(k==0?0:.25f+.4f*R()),h=tall*(k==0?1:.45f+.4f*R()),w=radius*(.18f+.08f*R());
                    var foot=bed+new Vector3(Mathf.Cos(a)*d,radius*.1f,Mathf.Sin(a)*d);var lean=new Vector3((R()-.5f)*.25f,1,(R()-.5f)*.25f).normalized;var bend=new Vector3((R()-.5f)*w,0,(R()-.5f)*w);
                    heap.Slot(1).Tube(new[]{foot,foot+lean*h*.45f+bend*.5f,foot+lean*h*.8f+bend,foot+lean*h+bend*1.4f},new[]{w,w*.8f,w*.42f,.01f},6,Color.white,Color.white,1,.8f,false);
                    heap.Slot(0);
                }
                flameSolid??=ZoneKit.Toon("vaskar-flame",null,m=>{m.SetColor("_BaseColor",new Color(1,.72f,.36f));m.SetColor("_EmissionColor",CoalGlow);m.SetFloat("_LightFloor",.6f);m.SetColor("_RimColor",Color.black);});
                var coalBed=kit.Built(name+" coals",heap,Coals,flameSolid);coalBed.transform.SetParent(root.transform,true);
                var bedView=coalBed.GetComponent<MeshRenderer>();bedView.shadowCastingMode=ShadowCastingMode.Off;
                // The flames: three sheets crossed about the fire's middle, each leaning back toward the camera by half its pitch so a flame stands up on screen and not as a stripe.
                var (flame,embers,smoke)=Glows(kit);
                for(int k=0;k<3;k++)
                {
                    var sheet=GameObject.CreatePrimitive(PrimitiveType.Quad);sheet.name=name+" flame "+k;Object.DestroyImmediate(sheet.GetComponent<Collider>());sheet.transform.SetParent(root.transform,false);
                    float w=radius*(1.9f-.35f*k),h=tall*(1-.2f*k);var off=new Vector3((R()-.5f)*radius*.4f,0,(R()-.5f)*radius*.4f);
                    sheet.transform.rotation=Quaternion.Euler(SightlineAudit.Pitch*.5f,SightlineAudit.Yaw+(k-1)*42+(R()-.5f)*10,0);
                    sheet.transform.position=bed+off+Vector3.up*(radius*.12f)+sheet.transform.up*(h*.5f);sheet.transform.localScale=new Vector3(w,h,1);
                    var view=sheet.GetComponent<MeshRenderer>();view.sharedMaterial=flame;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                }
                // In play the flames move: tongues off the bed, embers above them, a little smoke higher still.
                Rise(kit,name+" flames",bed+Vector3.up*(tall*.25f),new Vector3(radius*1.4f,.1f,radius*1.4f),flame,new Color(1,.72f,.4f,.85f),Mathf.RoundToInt(70*radius+20),radius*.5f,radius*.95f,tall*1.6f,.5f,root.transform);
                Rise(kit,name+" embers",bed+Vector3.up*tall*.7f,new Vector3(radius*1.6f,.3f,radius*1.6f),embers,new Color(1,.62f,.3f,1),Mathf.RoundToInt(24*radius+8),.025f,.06f,1.1f,2.6f,root.transform);
                Rise(kit,name+" smoke",bed+Vector3.up*(tall+.5f),new Vector3(radius*1.5f,.6f,radius*1.5f),smoke,new Color(.55f,.5f,.48f,.08f),10,.7f,1.6f,.45f,6,root.transform);
                var light=kit.Lamp(new Vector3(bed.x,0,bed.z),range,intensity,new Color(1,.64f,.34f));light.transform.position=bed+Vector3.up*(tall*.8f+.25f);light.name="Fire light";light.transform.SetParent(root.transform,true);
                var breath=root.AddComponent<Breathing>();breath.glows=new Renderer[]{bedView};breath.lights=new[]{light};breath.colour=CoalGlow;
                breath.period=1.3f+R()*.8f;breath.low=.78f;breath.high=1.12f;breath.flicker=.22f;
                return root;
            }
            /// <summary>Stones round a fire laid on the ground: rounded, smoke-blackened on their inner faces, set a little into the ash.</summary>
            internal static void Ring(ZoneKit.Shape shape,Vector3 middle,float radius,int count,int seed,Color outside,Color inside)
            {
                var rng=new System.Random(seed);
                for(int k=0;k<count;k++)
                {
                    float a=k*Mathf.PI*2/count+((float)rng.NextDouble()-.5f)*.25f;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float s=.15f+.07f*(float)rng.NextDouble();
                    shape.Lump(middle+d*radius+Vector3.up*s*.45f,new Vector3(s*1.2f,s*.85f,s),seed*7+k,.3f,outside,inside,7,4,.4f,Quaternion.LookRotation(d));
                }
            }
        }
        /// <summary>The nameless people of the Flows' camp and the Foundry's release, who stand in one frame. The generated bodies share one mesh, so two read as copies when they wear
        /// one dye or stand one shape (TownsfolkLookTests): each here wears a body of nobody named in chapter 8 (the camp wore Brekk's twice beside Brekk himself, the smiths five
        /// bodies round eleven, two of them Tovra's and Brekk's at the release they stand at), and each is set apart from the others by height or by what they carry.</summary>
        internal static class Folk
        {
            internal static readonly string[] Bodies={"DabbBody","DessikBody","DorrowBody","DovBody","GaroBody","HaddaBody","IruBody","JaskBody","MaruBody","QuillBody","TernBody",
                "BinnaBody","JunBody","OkhariBody","OnaBody","PimBody","RuvaBody","SennBody"};
            /// <summary>Heights a step of 7% apart, a carried coil or pack over them: twelve people no two alike.</summary>
            static readonly float[] Heights={.985f,1.055f,.92f,1.13f};static readonly string[] Carried={"","coil","pack"};
            internal static void Set(ZoneKit kit,GameObject person,int k)
            {
                person.transform.localScale*=Heights[k%Heights.Length];
                TownLife.Carry(kit,person,Carried[(k/Heights.Length)%Carried.Length]);
            }
        }
    }
}
