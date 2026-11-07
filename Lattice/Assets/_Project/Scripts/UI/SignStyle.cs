using System.Collections.Generic;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>One look for every sign in the world, set as a zone loads so every builder's signs agree and no rebuild shrinks them.
    /// Readers measured floor signs at 8 to 12 px of thin Cinzel that vanished on a light floor, slanted by the ground camera's yaw, and half sunk
    /// because each stood on end through its own middle. A floor sign now lies almost flat on the real ground, reads level to the camera at a
    /// size made for this zone's camera, in a heavier face over a soft dark painted plate. Every sign shares one text and one plate material.
    /// A person's name is PeopleMarks' business, and only takes the face.</summary>
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
        /// <summary>Ships fly here: anything below it in a flight zone is lying on the floor under them.</summary>
        const float FlightPlane=1,Feather=.22f,MostOverLaid=.45f;
        static Material face,paint;
        static readonly Dictionary<long,Mesh> plates=new();
        static readonly RaycastHit[] hits=new RaycastHit[16];

        static Material Face
        {
            get
            {
                if(face==null)face=Resources.Load<Material>("Fonts & Materials/Cinzel SDF Sign");
                return face!=null?face:UiSkin.Display.material;
            }
        }
        static Material Paint
        {
            get
            {
                if(paint!=null)return paint;
                var shader=Resources.Load<Shader>("Effects/SignPlate");
                paint=new Material(shader!=null?shader:Shader.Find("Sprites/Default")){name="sign plate (runtime)"};
                return paint;
            }
        }
        public static void Apply()
        {
            var zone=ZoneController.Current;bool flight=zone!=null&&zone.Flight;
            var view=zone!=null&&zone.definition!=null?zone.definition.cameraProfile:null;
            float yaw=view!=null?view.yaw:0;
            float slide=view!=null?Mathf.Sin(view.pitch*Mathf.Deg2Rad)/Mathf.Max(1,view.distance):0;
            var people=new HashSet<string>();
            foreach(var npc in Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None))people.Add(DisplayNames.Speaker(npc.speaker));
            var warps=Object.FindObjectsByType<WarpBeacon>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var sign in Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!sign.name.StartsWith("Sign_"))continue;
                sign.font=UiSkin.Display;sign.fontSharedMaterial=Face;
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
                AddPlate(sign,size);
                var fade=sign.gameObject.AddComponent<SignFade>();fade.bossOnly=!lying||flight;
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
        static void AddPlate(TextMeshPro sign,Vector2 size)
        {
            var plate=new GameObject(PlateName,typeof(MeshFilter),typeof(MeshRenderer));plate.layer=sign.gameObject.layer;
            plate.transform.SetParent(sign.transform,false);plate.transform.localPosition=Vector3.forward*.01f;
            plate.GetComponent<MeshFilter>().sharedMesh=PlateMesh(size);
            var view=plate.GetComponent<MeshRenderer>();view.sharedMaterial=Paint;view.receiveShadows=false;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        /// <summary>A flat plate whose paint feathers out to nothing at its rim. Plates of one size share a mesh for the life of the game.</summary>
        static Mesh PlateMesh(Vector2 size)
        {
            long key=Mathf.RoundToInt(size.x*10)*100000L+Mathf.RoundToInt(size.y*10);
            if(plates.TryGetValue(key,out var mesh)&&mesh!=null)return mesh;
            float x=size.x*.5f,y=size.y*.5f;float[] xs={-x-Feather,-x,x,x+Feather},ys={-y-Feather,-y,y,y+Feather};
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
