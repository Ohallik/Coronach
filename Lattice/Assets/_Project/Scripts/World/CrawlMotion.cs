using System;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// A town that is always moving, with the deck under the party standing still (chapter 9's Meridian Crawl: <c>HomeBuilder.Crawl.cs</c>, docs/maps/MeridianCrawl.md). The town
    /// walks west and the world goes past it east, so nothing in frame that the party stands on moves and nobody is made queasy: the plain's texture slides under it
    /// (<see cref="plain"/>), its rocks and scrub go by and come round again out of sight (<see cref="drift"/>), the treads' plates run forward on their top runs
    /// (<see cref="belts"/>), and everything that hangs swings together (<see cref="hanging"/>), each hall a beat off the next, with a jolt now and then as a link goes over a
    /// stone. Its look only: nothing reads it, nothing is saved, and it runs on the game's clock (<see cref="GameTime.Now"/>), so a pause stops it and a conversation doesn't.
    /// Scrolled renderers are moved by property block, never their material, which a scene doesn't save (guide 4.3); an editor still shows the builder's own pose.
    /// </summary>
    public sealed class CrawlMotion:MonoBehaviour
    {
        /// <summary>Something that hangs: turned about <see cref="axis"/> (world space) through its pivot by the shared swing, from <see cref="rest"/> (its local rotation unswung).
        /// <see cref="reach"/> scales the swing (a long chain swings more than a stiff sign); <see cref="lag"/> is the seconds its hall's lurch comes after the head's.</summary>
        [Serializable] public struct Hanging{public Transform pivot;public Quaternion rest;public Vector3 axis;public float reach,lag;}
        /// <summary>Something loose on the deck that rolls a little each way as the hall lurches: a melon, a pot. Moved along <see cref="along"/> from <see cref="rest"/> (world),
        /// spun by the distance over its <see cref="radius"/>.</summary>
        [Serializable] public struct Rolling{public Transform body;public Vector3 rest,along;public float reach,radius,lag;}

        /// <summary>Metres a second the world goes past, the way it goes (world space; the town's own heading is the other way).</summary>
        public float speed=1.4f;public Vector3 heading=Vector3.right;
        /// <summary>The plain: renderers whose base map slides along x by the speed. <see cref="plainTile"/> is the metres one texture repeat covers along the mesh's u.</summary>
        public Renderer[] plain=Array.Empty<Renderer>();public float plainTile=24;
        /// <summary>What lies on the plain: carried along the heading and put back at <see cref="driftFrom"/> once past <see cref="driftFrom"/>+<see cref="driftSpan"/> (along x), out of sight.</summary>
        public Transform[] drift=Array.Empty<Transform>();public float driftFrom=-210,driftSpan=420;
        /// <summary>The treads' belts: their u runs along the belt the way its plates go, one unit a plate (<see cref="beltPitch"/> metres).</summary>
        public Renderer[] belts=Array.Empty<Renderer>();public float beltPitch=.9f;
        public Hanging[] hanging=Array.Empty<Hanging>();
        /// <summary>The shared swing, in degrees: a lean into the pace, a slow swing about it, a jolt every so often.</summary>
        public float lean=3,swing=5,period=3.6f,jolt=4,joltEvery=9.5f;
        public Rolling[] rolling=Array.Empty<Rolling>();
        public static CrawlMotion Current{get;private set;}

        static readonly int BaseST=Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock block;Vector4[] plainST,beltST;float[] driftAt;

        void OnEnable()=>Current=this;
        void OnDisable(){if(Current==this)Current=null;}
        void Start()
        {
            block=new MaterialPropertyBlock();
            plainST=Array.ConvertAll(plain,ST);beltST=Array.ConvertAll(belts,ST);
            driftAt=Array.ConvertAll(drift,t=>t!=null?Vector3.Dot(t.position,heading.normalized):0);
        }
        static Vector4 ST(Renderer r)
        {
            var m=r!=null?r.sharedMaterial:null;if(m==null||!m.HasProperty("_BaseMap"))return new Vector4(1,1,0,0);
            var s=m.GetTextureScale("_BaseMap");var o=m.GetTextureOffset("_BaseMap");return new Vector4(s.x,s.y,o.x,o.y);
        }
        /// <summary>The swing every hanging thing shares at a moment, in degrees about its axis, for a hall <paramref name="lag"/> seconds behind the head.</summary>
        public float Swing(float time,float lag)
        {
            float t=time-lag;float s=Mathf.Repeat(t,Mathf.Max(.5f,joltEvery));
            float bump=s<1.6f?Mathf.Exp(-s*2.4f)*Mathf.Sin(s*9):0;
            return lean+swing*Mathf.Sin(t*Mathf.PI*2/Mathf.Max(.2f,period))+jolt*bump;
        }
        /// <summary>How far the world has gone past since the zone opened: what every scrolling and drifting part reads, so they keep step with each other.</summary>
        public float Travelled(float time)=>speed*time;
        void Update()
        {
            if(block==null)Start();
            float now=GameTime.Now,gone=Travelled(now);
            for(int i=0;i<plain.Length;i++)
            {
                var r=plain[i];if(r==null)continue;var st=plainST[i];
                r.GetPropertyBlock(block);block.SetVector(BaseST,new Vector4(st.x,st.y,st.z-gone/Mathf.Max(.01f,plainTile)*st.x,st.w));r.SetPropertyBlock(block);
            }
            for(int i=0;i<belts.Length;i++)
            {
                var r=belts[i];if(r==null)continue;var st=beltST[i];
                r.GetPropertyBlock(block);block.SetVector(BaseST,new Vector4(st.x,st.y,st.z-gone/Mathf.Max(.01f,beltPitch)*st.x,st.w));r.SetPropertyBlock(block);
            }
            var way=heading.normalized;
            for(int i=0;i<drift.Length;i++)
            {
                var t=drift[i];if(t==null)continue;
                float along=driftFrom+Mathf.Repeat(driftAt[i]-driftFrom+gone,Mathf.Max(1,driftSpan));
                var p=t.position;p+=way*(along-Vector3.Dot(p,way));t.position=p;
            }
            foreach(var h in hanging)
            {
                if(h.pivot==null)continue;
                var axis=h.pivot.parent!=null?h.pivot.parent.InverseTransformDirection(h.axis):h.axis;
                h.pivot.localRotation=Quaternion.AngleAxis(Swing(now,h.lag)*(h.reach<=0?1:h.reach),axis)*h.rest;
            }
            foreach(var r in rolling)
            {
                if(r.body==null)continue;float lurch=Swing(now,r.lag)-lean;
                float shift=r.reach*lurch/Mathf.Max(.01f,swing+jolt);var dir=r.along.normalized;
                r.body.position=r.rest+dir*shift;
                r.body.rotation=Quaternion.AngleAxis(-shift/Mathf.Max(.02f,r.radius)*Mathf.Rad2Deg,Vector3.Cross(Vector3.up,dir));
            }
        }
    }
}
