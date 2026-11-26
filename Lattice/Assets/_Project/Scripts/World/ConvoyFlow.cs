using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The convoy running the Returning Passage (chapter 10, docs/maps/ReturningPassage.md): every group of hulls the player has known, each its own root, flowing north along the
    /// passage's line at its own pace, following its bends and squeezed through its narrows by the road's width there, and coming round again from <see cref="to"/> to <see cref="from"/>,
    /// out of every frame. From <see cref="closeFlag"/> (Orrin: "Close up on the lanterns, and leave the middle of the lane empty") every group under the road's middle closes to the
    /// lantern line at its own edge over <see cref="closeSeconds"/>; a zone opened with the flag set opens with them closed. It also scrolls the deep streams of light along the flow.
    /// <para>Everything it moves is drawn only, under the plane: it never pushes or hides a hero. It does nothing while the game is paused, and nothing while a conversation holds
    /// the controls (D275): a talk holds everything that moves. <see cref="heldByTalk"/> is only ever turned off by the test that shows the old way failing first.</para>
    /// </summary>
    public sealed class ConvoyFlow:MonoBehaviour
    {
        /// <summary>The passage's line at the plane, in the way it flows, and the road's half width at each of its points.</summary>
        public Vector3[] path=System.Array.Empty<Vector3>();public float[] halves=System.Array.Empty<float>();
        /// <summary>Where along the line a group comes in, and where it goes out (it comes round to <see cref="from"/> again).</summary>
        public float from=-100,to=1480;
        /// <summary>Each group's root, and its place: metres along the line, metres to starboard of it, the height of its root, its pace and its own turn.</summary>
        public Transform[] groups=System.Array.Empty<Transform>();
        public float[] along=System.Array.Empty<float>(),side=System.Array.Empty<float>(),height=System.Array.Empty<float>(),pace=System.Array.Empty<float>(),turn=System.Array.Empty<float>();
        public string closeFlag="";public float closeSeconds=8;
        /// <summary>The widest the road is, and how far in from its edge a closed group lies: the middle it leaves empty is the road less twice this and the groups' own spread.</summary>
        public float wide=30,edgeIn=6;
        /// <summary>Ribbons of light deep under the convoy, their maps scrolled along the flow at <see cref="streamRate"/> repeats a second.</summary>
        public Renderer[] streams=System.Array.Empty<Renderer>();public float streamRate=.08f;
        public bool heldByTalk=true;
        /// <summary>How closed the convoy is now, nought to one.</summary>
        public float Closed{get;private set;}
        float scroll;MaterialPropertyBlock block;bool said;
        /// <summary>D425: every group's hulls are drawn hulls while it runs, which the companion's place in flight keeps off (nov25-r5-chain s68/023, s68/028: Idra's tug in a canoe).</summary>
        void OnEnable(){foreach(var group in groups)MooredHulls.Take(group,this);}
        void OnDisable()=>DrawnHulls.Remove(this);

        void Start()
        {
            var services=GameServices.Current;
            if(services!=null&&!string.IsNullOrEmpty(closeFlag)&&services.Flags.GetBool(closeFlag)){Closed=1;said=true;}
            Place();
        }
        /// <summary>Whether a conversation (or a load) holds the controls, which holds the convoy.</summary>
        public bool Held=>heldByTalk&&GameServices.Current!=null&&GameServices.Current.Input.Blocked;
        void Update()
        {
            if(GameTime.Paused||Held)return;
            float dt=Time.deltaTime;var services=GameServices.Current;
            bool closing=services!=null&&!string.IsNullOrEmpty(closeFlag)&&services.Flags.GetBool(closeFlag);
            if(closing&&!said){said=true;Debug.Log("CONVOY_CLOSES "+name);}
            Closed=Mathf.MoveTowards(Closed,closing?1:0,dt/Mathf.Max(.1f,closeSeconds));
            float loop=to-from;
            for(int i=0;i<groups.Length&&i<along.Length;i++)
            {
                along[i]+=pace[i]*dt;
                if(along[i]>to)along[i]-=loop;
            }
            Place();
            scroll+=dt*streamRate;
            block??=new MaterialPropertyBlock();
            foreach(var stream in streams)
            {
                if(stream==null)continue;stream.GetPropertyBlock(block);block.SetVector("_BaseMap_ST",new Vector4(1,1,-scroll,0));stream.SetPropertyBlock(block);
            }
        }
        /// <summary>Every group where it is now: on the line at its place, its side closed as far as the convoy is and squeezed by the road's width there.</summary>
        public void Place()
        {
            for(int i=0;i<groups.Length&&i<along.Length;i++)
            {
                if(groups[i]==null)continue;
                var at=At(path,along[i],SideNow(i),out var ahead);at.y=height[i];
                groups[i].SetPositionAndRotation(at,Quaternion.LookRotation(ahead)*Quaternion.Euler(0,turn[i],0));
            }
        }
        /// <summary>Where a group lies across the road now: as laid, or (closed) out by the lantern line at its own edge; in the narrows, as much nearer the middle as the road is narrower.</summary>
        public float SideNow(int i)=>Side(side[i],along[i],Closed);
        public float Side(float laid,float s,float closed)
        {
            float squeeze=Mathf.Clamp(Half(s)/Mathf.Max(1,wide),.4f,1.6f);
            float shut=laid;
            if(Mathf.Abs(laid)<wide){float sign=laid<0?-1:laid>0?1:1;shut=sign*(wide-edgeIn+Mathf.Abs(laid)/wide*(edgeIn-1));}
            return Mathf.Lerp(laid,shut,closed)*squeeze;
        }
        /// <summary>The road's half width at a place along the line.</summary>
        public float Half(float s)
        {
            if(halves==null||halves.Length!=path.Length||path.Length<2)return wide;
            float run=0;
            for(int i=1;i<path.Length;i++)
            {
                float leg=Flat(path[i]-path[i-1]).magnitude;
                if(s<=run+leg||i==path.Length-1)return Mathf.Lerp(halves[i-1],halves[i],Mathf.Clamp01((s-run)/Mathf.Max(.01f,leg)));
                run+=leg;
            }
            return wide;
        }

        // ------------------------------------------------------------------ the passage's line (the builder, the routes and the other parts of the passage read the same)
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        /// <summary>The point <paramref name="s"/> metres along a line (before its start or past its end, on along its first or last leg), <paramref name="side"/> metres to starboard of
        /// it, at the line's height; and which way the line runs there.</summary>
        public static Vector3 At(Vector3[] line,float s,float side,out Vector3 ahead)
        {
            ahead=Vector3.forward;if(line==null||line.Length==0)return Vector3.zero;if(line.Length==1)return line[0]+Vector3.right*side;
            float run=0;int leg=1;float t=0;
            for(int i=1;i<line.Length;i++)
            {
                float length=Flat(line[i]-line[i-1]).magnitude;
                if(s<=run+length||i==line.Length-1){leg=i;t=(s-run)/Mathf.Max(.01f,length);break;}
                run+=length;
            }
            if(s<0){leg=1;t=s/Mathf.Max(.01f,Flat(line[1]-line[0]).magnitude);}
            var a=line[leg-1];var b=line[leg];ahead=Flat(b-a).normalized;if(ahead.sqrMagnitude<.5f)ahead=Vector3.forward;
            var across=Vector3.Cross(Vector3.up,ahead);
            return a+(b-a)*t+across*side;
        }
        public static Vector3 At(Vector3[] line,float s,float side)=>At(line,s,side,out _);
        /// <summary>How far along a line a point is (the nearest point of the line to it), and how far to starboard of it.</summary>
        public static float Along(Vector3[] line,Vector3 point,out float side)
        {
            side=0;if(line==null||line.Length<2)return 0;
            float best=float.PositiveInfinity,run=0,found=0;var p=Flat(point);
            for(int i=1;i<line.Length;i++)
            {
                var a=Flat(line[i-1]);var d=Flat(line[i])-a;float length=d.magnitude;if(length<.01f)continue;
                var dir=d/length;float t=Vector3.Dot(p-a,dir);
                // Before the first leg or past the last, the line runs on.
                float clamped=i==1?Mathf.Min(t,length):i==line.Length-1?Mathf.Max(t,0):Mathf.Clamp(t,0,length);
                var q=a+dir*clamped;float gap=Vector3.Distance(p,q);
                if(gap<best){best=gap;found=run+clamped;side=Vector3.Dot(p-q,Vector3.Cross(Vector3.up,dir));}
                run+=length;
            }
            return found;
        }
        public static float Along(Vector3[] line,Vector3 point)=>Along(line,point,out _);
    }
}
