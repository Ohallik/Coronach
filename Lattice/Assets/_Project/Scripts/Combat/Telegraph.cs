using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    /// <summary>
    /// A marked area on the ground (D198): the exact shape a boss attack will strike, filling from its origin as the tell
    /// runs, then flashing as it lands. The same shape answers <see cref="Contains"/>, so what the player sees is what is
    /// hit: a hero standing outside the marking is never struck by it. Shapes follow the ground they lie on (sampled
    /// once, or again when the marking moves); in flight they lie flat under the ships.
    /// </summary>
    public sealed class Telegraph:MonoBehaviour
    {
        public enum Kind{Circle,Ring,Cone,Line}
        public static readonly List<Telegraph> Live=new();
        public static readonly Color Threat=new(1f,.42f,.1f,1),Heavy=new(1f,.16f,.14f,1),Stagger=new(.72f,.4f,1f,1),Trail=new(.86f,.72f,.5f,1);
        /// <summary>Generous by a body's radius: a hero whose capsule overlaps the edge is inside.</summary>
        public const float BodyAllowance=.25f;
        const float Lift=.06f,FlightHeight=.15f;
        /// <summary>The steepest ground a marking follows, rise over run, and the most any one step may rise. Sorrel's floor ends in a wall that rises 5 m for every metre: following
        /// the foot of it broke a marking into spikes. Under this it's a slope anyone could walk; past it the marking stops at the wall's foot and goes on, flat, under the hill.</summary>
        const float FollowedGrade=.7f,FollowedStep=2.2f;
        /// <summary>How far under the ground a lane's vertex can be pulled to stay with its neighbours before that side of the lane counts as having left the floor, and how many rows off it in a row
        /// end that column. Fewer is a rock or a bump it goes under, with floor past it that's struck all the same.</summary>
        const float LeftTheFloor=.2f;const int RowsOffTheFloor=2;
        /// <summary>Sides a whole circle needs per square root of its radius so its straight edges stay under about 1.2 cm: a pixel from the ground camera.</summary>
        const float RoundSides=20.3f;

        public Kind Shape{get;private set;}
        public Vector3 Origin{get;private set;}
        public Vector3 Forward{get;private set;}=Vector3.forward;
        public float Radius{get;private set;}
        public float Inner{get;private set;}
        public float Angle{get;private set;}=360;
        public float Length{get;private set;}
        public float Width{get;private set;}
        public float Duration{get;private set;}
        public float Elapsed{get;private set;}
        public float Remaining=>Mathf.Max(0,Duration-Elapsed);
        public bool Charged=>Elapsed>=Duration;
        public bool Released{get;private set;}
        /// <summary>A marking held at full charge (an expanding band, a burrow trail) instead of filling.</summary>
        public bool Steady;

        Mesh mesh;MeshRenderer view;MaterialPropertyBlock block;Color color;
        float releasedAt=-1,cancelledAt=-1;bool ownsMesh;
        /// <summary>Each marking is drawn twice from one mesh: its area (wash, hatch, the charge's glow) a step earlier in the queue than its outline. Every area goes down before any outline, so
        /// where markings overlap the area is laid once, not stacked, and an outline isn't drawn across another marking's inside: three lanes landing together read as one shape with one edge,
        /// where they were a lattice of rims and crossing hatch with bright bars over the hero. A faint boundary (a ward) lays its light without taking part.</summary>
        static Material shared,sharedFill,sharedFaint;
        static readonly RaycastHit[] Hits=new RaycastHit[8];
        static readonly List<Vector3> Vertices=new(),Scales=new();static readonly List<Vector2> Uvs=new();static readonly List<int> Triangles=new();static readonly List<float> OnFloor=new();
        /// <summary>The ground samples of a whole marking, row by row: where each is, how high it was laid, whether it's floor, and how far it is from where the floor ends (D217's cut).</summary>
        static readonly List<Vector3> SampleAt=new();static readonly List<float> SampleLaid=new(),SampleCut=new(),SampleAway=new();static readonly List<bool> SampleOn=new(),SampleReached=new();static readonly Stack<int> Flood=new();
        /// <summary>A sample's distance from a cut when it has none near it: a marking on open floor.</summary>
        const float Uncut=99;
        /// <summary>The row being laid, and the one inside it on the way out from the origin.</summary>
        static List<float> Heights=new(),Before=new();
        /// <summary>A lane's heights as they were found and as they were laid, whether each vertex is on the floor, and the row each of its columns ends at.</summary>
        static readonly List<float> Raised=new(),Laid=new();static readonly List<bool> Floored=new();static readonly List<int> Ends=new();
        static readonly int FillId=Shader.PropertyToID("_Fill"),FlashId=Shader.PropertyToID("_Flash"),FadeId=Shader.PropertyToID("_Fade"),
            ColorId=Shader.PropertyToID("_BaseColor"),SidesId=Shader.PropertyToID("_SideEdges"),InnerId=Shader.PropertyToID("_InnerEdge"),ThroughId=Shader.PropertyToID("_Through"),HatchId=Shader.PropertyToID("_Hatch");

        /// <summary>A light marking uses fewer ground samples: a swarm of ordinary attacks must not cost a frame.</summary>
        public static Telegraph Circle(Vector3 center,float radius,float duration,Color color,bool light=false)=>
            Create(Kind.Circle,center,Vector3.forward,radius,0,360,0,0,duration,color,light);
        public static Telegraph Ring(Vector3 center,float inner,float outer,float duration,Color color)=>
            Create(Kind.Ring,center,Vector3.forward,outer,inner,360,0,0,duration,color);
        public static Telegraph Cone(Vector3 origin,Vector3 forward,float radius,float angle,float duration,Color color,bool light=false)=>
            Create(Kind.Cone,origin,forward,radius,0,Mathf.Clamp(angle,5,360),0,0,duration,color,light);
        public static Telegraph Line(Vector3 origin,Vector3 direction,float length,float width,float duration,Color color,bool light=false)=>
            Create(Kind.Line,origin,direction,0,0,0,length,width,duration,color,light);

        bool light;
        static Telegraph Create(Kind kind,Vector3 origin,Vector3 forward,float radius,float inner,float angle,float length,float width,float duration,Color color,bool light=false)
        {
            var go=new GameObject("Telegraph "+kind);
            var marking=go.AddComponent<Telegraph>();marking.light=light;
            forward.y=0;if(forward.sqrMagnitude<1e-4f)forward=Vector3.forward;
            marking.Shape=kind;marking.Origin=origin;marking.Forward=forward.normalized;marking.Radius=radius;marking.Inner=inner;marking.Angle=angle;
            marking.Length=length;marking.Width=width;marking.Duration=Mathf.Max(.01f,duration);marking.color=color;
            marking.Build();Live.Add(marking);
            return marking;
        }

        void OnDestroy(){Live.Remove(this);if(ownsMesh&&mesh!=null)Destroy(mesh);}

        /// <summary>Planar test against the marked shape, including <see cref="BodyAllowance"/>.</summary>
        public bool Contains(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;float distance=offset.magnitude;
            switch(Shape)
            {
                case Kind.Circle:return distance<=Radius+BodyAllowance;
                case Kind.Ring:return distance>=Inner-BodyAllowance&&distance<=Radius+BodyAllowance;
                case Kind.Cone:
                    if(distance>Radius+BodyAllowance)return false;
                    if(Angle>=359||distance<.5f)return true;
                    return Vector3.Angle(Forward,offset)<=Angle*.5f+Mathf.Rad2Deg*BodyAllowance/Mathf.Max(.5f,distance);
                default:
                    float along=Vector3.Dot(offset,Forward);
                    float across=Mathf.Abs(Vector3.Dot(offset,Vector3.Cross(Vector3.up,Forward)));
                    return along>=-BodyAllowance&&along<=Length+BodyAllowance&&across<=Width*.5f+BodyAllowance;
            }
        }

        /// <summary>The shortest way out of the marking from a point inside it.</summary>
        public Vector3 WayOut(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;
            if(Shape==Kind.Line)
            {
                var across=Vector3.Cross(Vector3.up,Forward);float side=Vector3.Dot(offset,across);
                return side>=0?across:-across;
            }
            if(Shape==Kind.Ring&&offset.magnitude-Inner<Radius-offset.magnitude&&offset.sqrMagnitude>.01f)return -offset.normalized;
            return offset.sqrMagnitude>.01f?offset.normalized:Vector3.right;
        }
        /// <summary>How far this marking reaches: what makes it big enough that the way out can be off screen.</summary>
        public float Span=>Shape==Kind.Line?Length:Radius;
        /// <summary>The nearest point out of the marking from a point inside it: what the player has to be able to see to.</summary>
        public Vector3 EdgeFrom(Vector3 point)
        {
            var offset=point-Origin;offset.y=0;float distance=offset.magnitude;
            var outward=distance>.01f?offset/distance:Forward;
            switch(Shape)
            {
                case Kind.Line:
                {
                    var across=Vector3.Cross(Vector3.up,Forward);float side=Vector3.Dot(offset,across);
                    return point+(side>=0?across:-across)*Mathf.Max(0,Width*.5f-Mathf.Abs(side));
                }
                case Kind.Ring:return distance-Inner<Radius-distance?point-outward*Mathf.Max(0,distance-Inner):point+outward*Mathf.Max(0,Radius-distance);
                case Kind.Cone when Angle<359:
                {
                    // A side of the fan can be nearer than its far edge: half a chamber's nearest way out is the line across it. Its far edge is 24 m out, in the rock past the wall for a hero
                    // at an organ, so it has to be well nearer to count. On even terms the view drew back to frame rock, and swung from one to the other as he stepped about at the organ.
                    float off=Vector3.SignedAngle(Forward,offset,Vector3.up),sign=off<0?-1:1,inside=Angle*.5f-Mathf.Abs(off);
                    float toSide=inside<90?distance*Mathf.Sin(inside*Mathf.Deg2Rad):distance;
                    if(toSide<(Radius-distance)*2)return point+Vector3.Cross(Vector3.up,Quaternion.Euler(0,sign*Angle*.5f,0)*Forward)*(sign*Mathf.Max(0,toSide));
                    return point+outward*Mathf.Max(0,Radius-distance);
                }
                default:return point+outward*Mathf.Max(0,Radius-distance);
            }
        }
        /// <summary>Move the marking (a trail following a hero) and lay it on the ground again.</summary>
        public void MoveTo(Vector3 origin){Origin=origin;Build();}
        /// <summary>Reshape an expanding band.</summary>
        public void SetRing(float inner,float outer){Inner=Mathf.Max(0,inner);Radius=Mathf.Max(Inner+.05f,outer);Build();}
        /// <summary>The strike lands: a short flash, then the marking fades away. In real time: through a Flash the world runs at a quarter, and a spent ring stayed on the floor for over a
        /// second with the bar already saying OPENING.</summary>
        public void Release(){if(Released||cancelledAt>=0)return;Released=true;releasedAt=Time.unscaledTime;}
        /// <summary>The attack was interrupted (a break, a death): the marking fades without striking.</summary>
        public void Cancel(){if(cancelledAt>=0)return;cancelledAt=Time.unscaledTime;}
        /// <summary>Gone in this frame: a fight that has ended doesn't fade its threats out across the screen that pays out the win.</summary>
        public void Clear(){cancelledAt=Time.unscaledTime;if(view!=null)view.enabled=false;Destroy(gameObject);}
        /// <summary>Not drawn, and still everything else it is: a marking lying exactly on another.</summary>
        public void Hide(bool hidden){if(view!=null)view.enabled=!hidden;}

        public static void CancelAll(Object owner)
        {
            for(int i=Live.Count-1;i>=0;i--)if(Live[i]!=null&&Live[i].owner==owner)Live[i].Cancel();
        }
        public static void ClearAll(Object owner)
        {
            for(int i=Live.Count-1;i>=0;i--)if(Live[i]!=null&&Live[i].owner==owner)Live[i].Clear();
        }
        public Object owner;

        // ---------- a shot in the air ----------
        Projectile[] carried;Vector3[] carriedVelocity;Vector3 carriedOrigin;float carriedLength,carriedFrom;
        /// <summary>The shot is away and the marking goes with it: it stays charged ahead of the shot until the shot lands or burns out at its tip, and a lane is eaten from behind as the shot
        /// runs down it. Released as the shot left, a lane was gone in a third of a second, and the Bellows' comets crossed a second and more of floor with nothing marked ahead of them: a hot
        /// ball heading for the hero that read as Sela's cyan shot in another colour (D207: what strikes is marked where it strikes).</summary>
        public void Carry(Projectile[] shots)
        {
            if(Released||cancelledAt>=0)return;
            int live=0;foreach(var shot in shots)if(shot!=null)live++;
            if(live==0){Release();return;}
            carried=shots;carriedVelocity=new Vector3[shots.Length];for(int i=0;i<shots.Length;i++)if(shots[i]!=null)carriedVelocity[i]=shots[i].Velocity;
            carriedOrigin=Origin;carriedLength=Length;carriedFrom=0;Steady=true;Elapsed=Mathf.Max(Elapsed,Duration);
        }
        /// <summary>Whether a carried shot is still the one fired: a pooled shot that landed can be fired again by someone else in the same frame, with another heading.</summary>
        bool Flying(int i)=>carried[i]!=null&&carried[i].isActiveAndEnabled&&Projectile.Live.Contains(carried[i])&&carried[i].Velocity==carriedVelocity[i];
        void FollowShots()
        {
            float behind=float.PositiveInfinity;bool any=false;
            for(int i=0;i<carried.Length;i++)if(Flying(i)){any=true;behind=Mathf.Min(behind,Vector3.Dot(carried[i].transform.position-carriedOrigin,Forward));}
            if(!any){carried=null;Cancel();return;}
            if(Shape!=Kind.Line)return;
            // The lane starts where the last of its shots is: what's behind them is spent. Laid again only every quarter metre, since each lay asks the ground again.
            behind=Mathf.Clamp(behind,0,carriedLength);
            if(carriedLength-behind<.3f){carried=null;Cancel();return;}
            if(behind-carriedFrom<.25f)return;
            carriedFrom=behind;Origin=carriedOrigin+Forward*behind;Length=carriedLength-behind;Build();
        }

        void Update()
        {
            // A marking can't outlive what made it: a body dismissed or removed mid-tell never got to cancel its own, and it stayed on the floor for good.
            if(cancelledAt<0&&!Released&&!ReferenceEquals(owner,null)&&owner==null)Cancel();
            if(carried!=null&&cancelledAt<0&&!Released)FollowShots();
            if(!GameTime.CombatHeld)Elapsed+=Time.deltaTime;
            float fill=Steady?1:Mathf.Clamp01(Elapsed/Duration),flash=0,fade=1;
            if(releasedAt>=0)
            {
                float age=Time.unscaledTime-releasedAt;flash=Mathf.Clamp01(1-age/.12f);fade=Mathf.Clamp01(1-(age-.08f)/.22f);fill=1;
                if(age>.32f){Destroy(gameObject);return;}
            }
            else if(cancelledAt>=0)
            {
                float age=Time.unscaledTime-cancelledAt;fade=Mathf.Clamp01(1-age/.15f);
                if(age>.16f){Destroy(gameObject);return;}
            }
            else fade=Mathf.Clamp01(Elapsed/.06f);
            block.SetFloat(FillId,fill);block.SetFloat(FlashId,flash);block.SetFloat(FadeId,fade);view.SetPropertyBlock(block);
        }

        void Build()
        {
            if(mesh==null){mesh=new Mesh{name="telegraph"};mesh.MarkDynamic();ownsMesh=true;}
            if(view==null)
            {
                gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
                view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.receiveShadows=false;
                if(shared==null)
                {
                    var shader=Resources.Load<Shader>("Effects/Telegraph");var fill=Resources.Load<Shader>("Effects/TelegraphFill");
                    shared=new Material(shader!=null?shader:Shader.Find("Sprites/Default")){name="telegraph (runtime)"};
                    sharedFill=new Material(fill!=null?fill:shared.shader){name="telegraph fill (runtime)"};
                    // A faint boundary lays its light under whatever else is marked there and keeps nobody's outline out: a danger crossing a ward still reads whole.
                    sharedFaint=new Material(sharedFill){name="telegraph faint (runtime)"};sharedFaint.SetFloat("_FillMask",0);sharedFaint.SetFloat("_FillComp",(float)UnityEngine.Rendering.CompareFunction.Always);
                }
                view.sharedMaterials=new[]{shared,color.a<.99f?sharedFaint:sharedFill};block=new MaterialPropertyBlock();
                block.SetColor(ColorId,color);
                block.SetFloat(SidesId,Shape==Kind.Line||Shape==Kind.Cone&&Angle<359?1:0);
                // A lane is closed at both ends. Open where it starts, a charge's lane ended under the Burrower in a straight cut across its hatch, with no rim, as if the floor had sliced it.
                block.SetFloat(InnerId,Shape==Kind.Ring||Shape==Kind.Line?1:0);
                block.SetFloat(ThroughId,ZoneController.Current!=null&&ZoneController.Current.Flight?0:1);
                block.SetVector(HatchId,Hatch());
            }
            // Moving markings rebuild every frame: reuse the buffers instead of allocating.
            var vertices=Vertices;var uv=Uvs;var scale=Scales;var triangles=Triangles;vertices.Clear();uv.Clear();scale.Clear();triangles.Clear();
            bool flight=ZoneController.Current!=null&&ZoneController.Current.Flight;
            int columns,rows;
            switch(Shape)
            {
                case Kind.Line:
                {
                    // Finer along than it was: its sides are what a lane is read by, where a ring's rim has a sample every .6 m.
                    rows=light?Mathf.Clamp(Mathf.CeilToInt(Length/1.5f),2,14):Mathf.Clamp(Mathf.CeilToInt(Length),2,40);columns=light?1:3;
                    var right=Vector3.Cross(Vector3.up,Forward);float run=Length/rows;
                    Laid.Clear();Floored.Clear();
                    for(int r=0;r<=rows;r++)
                    {
                        float u=r/(float)rows;
                        NewRow();
                        for(int c=0;c<=columns;c++)Ground(Origin+Forward*(u*Length)+right*((c/(float)columns-.5f)*Width),c,r==0,run,flight);
                        Raised.Clear();Raised.AddRange(Heights);
                        Settle(Width/columns,false);
                        for(int c=0;c<=columns;c++){Laid.Add(Heights[c]);Floored.Add(OnFloor[c]>=1&&Raised[c]-Heights[c]<=LeftTheFloor);}
                    }
                    // Its paint ends in a rim on the last floor it has, column by column. Run on flat under the hill it stopped with no edge at all: the hatch's lines ending in a saw-tooth at the
                    // terrain's contour, and a thin rim beyond on whichever side got further.
                    Ends.Clear();
                    for(int c=0;c<=columns;c++)
                    {
                        int last=0,off=0;
                        for(int r=1;r<=rows&&off<=RowsOffTheFloor;r++){if(Floored[r*(columns+1)+c]){last=r;off=0;}else off++;}
                        Ends.Add(last);
                    }
                    // One column running on past every neighbour is a spike. A column that ends early takes nothing with it: one side of a lane caught on a wall's foot can't end the floor the rest of it
                    // is still on, where the boss still charges. Dragging its neighbours down with it, a 20 m lane on open floor could be painted for 6 m.
                    int reach=Mathf.CeilToInt(Width/columns/Mathf.Max(run,.1f))+1;
                    for(int c=0,inside=-1;c<=columns;c++){int own=Ends[c];Ends[c]=Mathf.Min(own,Mathf.Max(inside,c<columns?Ends[c+1]:-1)+reach);inside=own;}
                    // Past its end a column is off the floor too, for the cut: its paint has stopped there either way.
                    SampleAt.Clear();SampleLaid.Clear();SampleOn.Clear();
                    for(int r=0;r<=rows;r++)for(int c=0;c<=columns;c++)
                    {
                        int laid=r*(columns+1)+c;var point=Origin+Forward*(r/(float)rows*Length)+right*((c/(float)columns-.5f)*Width);
                        SampleAt.Add(point);SampleLaid.Add(Laid[laid]);SampleOn.Add(Floored[laid]&&r<=Ends[c]);
                    }
                    Cut(rows,columns,false,flight,true);
                    for(int r=0;r<=rows;r++)for(int c=0;c<=columns;c++)
                    {
                        float v=c/(float)columns;int end=Mathf.Max(1,Ends[c]),laid=r*(columns+1)+c;
                        var point=SampleAt[laid];point.y=Laid[laid]+Lift;
                        vertices.Add(point);uv.Add(new Vector2(r/(float)end,v));scale.Add(new Vector3(end*run,Width,SampleCut[laid]));
                    }
                    break;
                }
                default:
                {
                    float sweep=Shape==Kind.Cone?Angle:360;
                    float outer=Radius,inner=Shape==Kind.Ring?Inner:0;
                    // About 0.6 m of arc per ground sample: a small trail stays light, a wide breath follows the floor.
                    int samples=light?Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*outer/1.2f),Shape==Kind.Cone?4:12,24):Mathf.Clamp(Mathf.CeilToInt(sweep*Mathf.Deg2Rad*outer/.6f),Shape==Kind.Cone?6:16,64);
                    rows=light?1:Mathf.Clamp(Mathf.CeilToInt((outer-inner)/1.4f),1,12);
                    // The rim is finer than the ground it reads: heights carry between the samples, so a circle is round for no more raycasts than it took to be a polygon.
                    int every=Mathf.Max(1,Mathf.CeilToInt(RoundSides*Mathf.Sqrt(outer)*sweep/360/samples));columns=samples*every;
                    float start=-sweep*.5f;bool closed=sweep>=359.9f;int width=samples+1;
                    SampleAt.Clear();SampleLaid.Clear();SampleOn.Clear();
                    for(int r=0;r<=rows;r++)
                    {
                        float radius=Mathf.Lerp(inner,outer,r/(float)rows);
                        NewRow();
                        for(int s=0;s<=samples;s++){var at=Origin+Quaternion.Euler(0,start+sweep*s/samples,0)*Forward*radius;SampleAt.Add(at);Ground(at,s,r==0,(outer-inner)/rows,flight);}
                        Raised.Clear();Raised.AddRange(Heights);
                        Settle(radius*sweep*Mathf.Deg2Rad/samples,closed);
                        // Settled more than a little under the ground that raised it, a sample is under the hill as surely as one that found no floor: its paint can't be seen from there.
                        for(int s=0;s<=samples;s++){SampleLaid.Add(Heights[s]);SampleOn.Add(OnFloor[s]>=1&&Raised[s]-Heights[s]<=LeftTheFloor);}
                    }
                    Cut(rows,samples,closed,flight,false);
                    for(int r=0;r<=rows;r++)
                    {
                        float u=r/(float)rows,radius=Mathf.Lerp(inner,outer,u);
                        for(int c=0;c<=columns;c++)
                        {
                            float v=c/(float)columns;var point=Origin+Quaternion.Euler(0,start+v*sweep,0)*Forward*radius;
                            int sample=c/every,at=r*width+sample;float between=(c%every)/(float)every;
                            point.y=(sample<samples?Mathf.Lerp(SampleLaid[at],SampleLaid[at+1],between):SampleLaid[at])+Lift;
                            float cut=sample<samples?Mathf.Lerp(SampleCut[at],SampleCut[at+1],between):SampleCut[at];
                            vertices.Add(point);uv.Add(new Vector2(u,v));
                            // The arc's real length at this radius: a fan's side line keeps its width all the way to the point.
                            scale.Add(new Vector3(Mathf.Max(.01f,outer-inner),Mathf.Max(.01f,radius*sweep*Mathf.Deg2Rad),cut));
                        }
                    }
                    break;
                }
            }
            for(int r=0;r<rows;r++)for(int c=0;c<columns;c++)
            {
                int a=r*(columns+1)+c,b=a+1,d=a+columns+1,e=d+1;
                triangles.Add(a);triangles.Add(d);triangles.Add(b);triangles.Add(b);triangles.Add(d);triangles.Add(e);
            }
            // The same triangles twice: the outline's material draws the first, the area's the second.
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,scale);mesh.subMeshCount=2;mesh.SetTriangles(triangles,0);mesh.SetTriangles(triangles,1);mesh.RecalculateBounds();
        }

        /// <summary>Where the ground ends a marking (D217: it follows a rise of .7 a metre and stops at a wall's foot), the paint ends in a rim, as at any other edge. Laid on flat under the hill
        /// instead, a ring behind the Burrower had no rim on its slope side and the hero's own rings lost half their round: the shape was never closed where the player could see it.
        /// Each sample gets its distance in metres from where the floor ends, positive on it and negative past it, so the line between two samples that disagree falls where the floor really
        /// stops, found by halving between them. Only floor lost from the edge inward counts: a bump the marking goes under in its middle has floor all round it, and stays painted.</summary>
        void Cut(int rows,int across,bool closed,bool flight,bool lane)
        {
            int width=across+1,count=(rows+1)*width;
            SampleCut.Clear();SampleAway.Clear();SampleReached.Clear();
            for(int i=0;i<count;i++){SampleCut.Add(Uncut);SampleAway.Add(Uncut);SampleReached.Add(false);}
            if(flight)return;
            bool Edge(int r,int s)=>r==rows||lane&&(r==0||s==0||s==across)||!lane&&(Shape==Kind.Ring&&r==0||!closed&&(s==0||s==across));
            Flood.Clear();
            for(int r=0;r<=rows;r++)for(int s=0;s<=across;s++){int i=r*width+s;if(!SampleOn[i]&&Edge(r,s)){SampleReached[i]=true;Flood.Push(i);}}
            while(Flood.Count>0)
            {
                int i=Flood.Pop(),r=i/width,s=i%width;
                void Visit(int rr,int ss)
                {
                    if(closed){if(ss<0)ss=across-1;else if(ss>across)ss=1;}
                    if(rr<0||rr>rows||ss<0||ss>across)return;int j=rr*width+ss;
                    if(!SampleOn[j]&&!SampleReached[j]){SampleReached[j]=true;Flood.Push(j);}
                }
                Visit(r-1,s);Visit(r+1,s);Visit(r,s-1);Visit(r,s+1);
                // The first and last samples of a whole circle are one point.
                if(closed&&(s==0||s==across))Visit(r,s==0?across:0);
            }
            bool Off(int i)=>SampleReached[i];
            bool any=false;for(int i=0;i<count&&!any;i++)any=Off(i);
            if(!any)return;
            int steps=light?3:4;
            void Pair(int a,int b)
            {
                bool offA=Off(a),offB=Off(b);if(offA==offB)return;
                int on=offA?b:a,off=offA?a:b;var flat=SampleAt[off]-SampleAt[on];flat.y=0;float length=flat.magnitude;if(length<.01f)return;
                float foot=Foot(SampleAt[on],SampleLaid[on],SampleLaid[off],flat,length,steps);
                SampleCut[on]=Mathf.Min(SampleCut[on],foot);SampleAway[off]=Mathf.Min(SampleAway[off],length-foot);
            }
            for(int r=0;r<=rows;r++)for(int s=0;s<=across;s++)
            {
                int i=r*width+s;
                if(s<across)Pair(i,i+1);
                if(r<rows){Pair(i,i+width);if(s<across)Pair(i+1,i+width);}
            }
            for(int i=0;i<count;i++)if(Off(i))SampleCut[i]=-SampleAway[i];
            if(closed)for(int r=0;r<=rows;r++){int a=r*width,b=a+across;float both=Mathf.Abs(SampleCut[a])<Mathf.Abs(SampleCut[b])?SampleCut[a]:SampleCut[b];SampleCut[a]=SampleCut[b]=both;}
        }
        /// <summary>How far the paint is drawn on the floor between a sample on it and one past it: halved a few times, each half asking whether the ground there still lies under the paint
        /// as drawn, a straight run between the two samples' laid heights. Asked by the rule that makes a sample floor (a rise of .7 a metre), the cut walked up the low foot of Sorrel's wall
        /// while the paint between the two stayed flat beneath it, and the closing rim lay 0.8 m under the hill.</summary>
        static float Foot(Vector3 from,float levelFrom,float levelTo,Vector3 flat,float length,int steps)
        {
            float lo=0,hi=1;
            for(int i=0;i<steps;i++){float mid=(lo+hi)*.5f;if(FloorAt(from+flat*mid,Mathf.Lerp(levelFrom,levelTo,mid),PaintClear))lo=mid;else hi=mid;}
            return (lo+hi)*.5f*length;
        }
        /// <summary>How far the ground may stand over a laid point and the paint, lifted over it, still show: under the lift and a hair more.</summary>
        const float PaintClear=Lift+.04f;
        static bool FloorAt(Vector3 point,float level,float most)
        {
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,level+3f,point.z),Vector3.down,Hits,8f,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider is CharacterController||collider.GetComponentInParent<Health>()!=null)continue;
                if(Hits[i].point.y<=level+most)return true;
            }
            return false;
        }

        /// <summary>The hatch says marked and the rim says where it ends. Square across a lane its lines were the rungs of a ladder, so there they cross it at a slant; on anything else they run
        /// corner to corner on the fixed camera's screen, where lines at 45 degrees in the world lay almost flat and read as planks. The bigger the area the wider and fainter, until on a mark past
        /// the size of the screen they've all but gone: a rim and a faint wash, where even thinned they were a floor of planks with no safe side. A shot's line has no room for a hatch either:
        /// two rails and the rungs between them, and where three land together a lattice over the hero.</summary>
        Vector4 Hatch()
        {
            var across=Shape==Kind.Line?Quaternion.Euler(0,45,0)*Forward:new Vector3(-.607f,0,.795f);
            float size=Shape==Kind.Line?Mathf.Sqrt(Length*Width):Shape==Kind.Ring?10:Mathf.Sqrt(Mathf.PI*Radius*Radius*Angle/360);
            float thin=Mathf.InverseLerp(8,22,size),strength=Mathf.Lerp(1,.08f,thin);
            if(Shape==Kind.Line&&Width<1.5f)strength*=.3f;
            return new Vector4(across.x,across.z,Mathf.Lerp(.62f,1.6f,thin),strength);
        }
        static void NewRow(){var swap=Before;Before=Heights;Heights=swap;Heights.Clear();OnFloor.Clear();}
        /// <summary>The ground under one point of a row. Each asks from the height of the point inside it on the way out from the origin, so a marking climbs a real slope however high it goes,
        /// while a crate's top stays further off than the floor under it. Asked from the origin's own level, a slope past 3 m above it was missed and that vertex stayed flat under the hill, beside
        /// another on the wall's foot: the spikes. What's left under a hill is marked, so its rim doesn't show through from there.</summary>
        void Ground(Vector3 point,int index,bool first,float run,bool flight)
        {
            if(flight){Heights.Add(FlightHeight-Lift);OnFloor.Add(1);return;}
            float level=first?Origin.y:Before[index],most=first?FollowedStep:Mathf.Min(FollowedStep,run*FollowedGrade+.15f);
            int count=Physics.RaycastNonAlloc(new Vector3(point.x,level+3f,point.z),Vector3.down,Hits,8f,~0,QueryTriggerInteraction.Ignore);
            float best=float.NegativeInfinity,nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var collider=Hits[i].collider;
                if(collider is CharacterController||collider.GetComponentInParent<Health>()!=null)continue;
                // The surface nearest the marking's own level (a floor, not a ceiling above it). The highest used to win:
                // a marking crossing a sealed gate or a crate climbed it, and its outline broke into shards over the top.
                float off=Mathf.Abs(Hits[i].point.y-level);
                if(Hits[i].point.y<=level+most&&off<nearest){nearest=off;best=Hits[i].point.y;}
            }
            bool found=best>float.NegativeInfinity;Heights.Add(found?best:level);OnFloor.Add(found?1:0);
        }
        /// <summary>Along a row nothing stands proud of its neighbours by more than a walkable rise: a vertex that caught the foot of a wall between two that didn't was a spike on the rim.
        /// It only ever lowers, and what it lowers sinks under the ground that raised it. A whole circle's first and last samples are one point: they settle together, or the ring tears open there.</summary>
        static void Settle(float spacing,bool closed)
        {
            float most=spacing*FollowedGrade+.1f;int last=Heights.Count-1;
            for(int pass=closed?2:1;pass>0;pass--)
            {
                for(int i=1;i<=last;i++)Heights[i]=Mathf.Min(Heights[i],Heights[i-1]+most);
                for(int i=last-1;i>=0;i--)Heights[i]=Mathf.Min(Heights[i],Heights[i+1]+most);
                if(closed)Heights[0]=Heights[last]=Mathf.Min(Heights[0],Heights[last]);
            }
        }
    }
}
