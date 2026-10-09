using System;
using System.Collections.Generic;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.UI
{
    // Recording-only, called by ContinuousReview after presentation LateUpdate.
    // Bounds are conservative evidence, not mesh intersection or visual acceptance.
    public sealed class ReleaseGeometryCapture:IDisposable
    {
        public const int FrameLimit=4096,SourceLimit=16;
        [Serializable] public sealed class Shape
        {
            public string role,hero;
            public int instance,meshes;
            public bool available;
            public float hullRadius;
            public Vector3 position,velocity,minimum,maximum,viewportMinimum,viewportMaximum;
            public Quaternion rotation;
            public Vector3[] corners;
        }
        [Serializable] public sealed class Frame
        {
            public int sample,frame,step,source,episode;
            public double elapsed;
            public float gameTime,releaseAge;
            public bool paused;
            public Vector3 sourcePosition,cameraPosition;
            public Quaternion sourceRotation,cameraRotation;
            public float[] viewProjection;
            public Shape[] anatomy,heroes;
        }
        [Serializable] public sealed class Span
        {
            public int source,episode,firstSample,lastSample,frames;
            public string end;
        }
        [Serializable] public sealed class Report
        {
            public int schema=1,samples,frames,dropped,frameLimit=FrameLimit,sourceLimit=SourceLimit;
            public bool complete;
            public string failure;
            public string scope="Evaluated release geometry after presentation. World AABBs and projected renderer-local bounds are conservative; overlap does not prove mesh collision or accept visual quality. Recording-only overhead; excluded from clean performance.";
            public Span[] spans;
        }
        sealed class Body
        {
            public CantorRelease release;
            public Transform[] parts;
            public Renderer[][] meshes;
            public Span span;
        }
        readonly string folder;
        readonly StreamWriter writer;
        readonly List<Body> bodies=new();
        readonly Dictionary<CombatActor,Renderer[]> heroMeshes=new();
        readonly int frameLimit;
        int samples,frames,dropped,lastFrame=-1;
        double lastElapsed=-1;
        bool finished;
        public string Failure {get;private set;}
        public ReleaseGeometryCapture(string output,int limit=FrameLimit)
        {
            if(limit<1||limit>FrameLimit)throw new ArgumentOutOfRangeException(nameof(limit));
            folder=output;frameLimit=limit;
            writer=new StreamWriter(Path.Combine(folder,"release-geometry.jsonl"),false,new System.Text.UTF8Encoding(false),65536);
        }
        static Renderer[] Meshes(Transform root)
        {
            if(root==null)return Array.Empty<Renderer>();
            var result=new List<Renderer>();
            foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                if(renderer is MeshRenderer||renderer is SkinnedMeshRenderer)result.Add(renderer);
            return result.ToArray();
        }
        public void Record(int sample,int frame,double elapsed,int step,CombatActor active,CombatActor partner,Camera camera)
        {
            if(finished)throw new InvalidOperationException("Release geometry already finished");
            if(sample!=samples||frame<=lastFrame||!double.IsFinite(elapsed)||elapsed<0||elapsed<=lastElapsed)
                Failure??="Release geometry sample/clock sequence mismatch";
            samples++;lastFrame=frame;lastElapsed=elapsed;
            foreach(var body in bodies)
                if(body.span.end==null&&(body.release==null||!body.release.Departing))
                    body.span.end=body.release==null?"removed":"recovered";
            foreach(var collar in CantorCollar.All)
            {
                if(collar==null)continue;
                var release=collar.GetComponent<CantorRelease>();
                if(release==null||!release.Departing)continue;
                bool known=false;
                foreach(var body in bodies)if(body.span.end==null&&body.release==release){known=true;break;}
                if(known)continue;
                if(bodies.Count>=SourceLimit){Failure??="Release geometry source limit exceeded";dropped++;continue;}
                var serpent=collar.GetComponent<SerpentSegments>();
                var parts=new Transform[8];parts[0]=collar.GetComponent<DefeatPresentation>()?.visual;
                for(int i=1;i<parts.Length;i++)parts[i]=serpent!=null&&serpent.Parts.Count>=i?serpent.Parts[i-1]:null;
                var meshes=new Renderer[8][];for(int i=0;i<8;i++)meshes[i]=Meshes(parts[i]);
                bodies.Add(new Body{release=release,parts=parts,meshes=meshes,
                    span=new Span{source=release.GetInstanceID(),episode=bodies.Count+1,firstSample=sample,lastSample=sample}});
            }
            foreach(var body in bodies)
            {
                if(body.span.end!=null)continue;
                if(frames>=frameLimit){Failure??="Release geometry frame limit exceeded";dropped++;continue;}
                if(camera==null){Failure??="Release geometry camera unavailable";dropped++;continue;}
                var record=new Frame{sample=sample,frame=frame,elapsed=elapsed,step=step,
                    source=body.span.source,episode=body.span.episode,gameTime=GameTime.Now,paused=GameTime.Paused,releaseAge=body.release.Elapsed,
                    sourcePosition=body.release.transform.position,sourceRotation=body.release.transform.rotation,
                    cameraPosition=camera.transform.position,cameraRotation=camera.transform.rotation,
                    viewProjection=Matrix(camera),anatomy=new Shape[8],heroes=new Shape[2]};
                for(int i=0;i<8;i++)record.anatomy[i]=Measure(i==0?"head":"part"+i,"",body.parts[i],body.meshes[i],camera,Vector3.zero,0);
                record.heroes[0]=Hero("active",active,camera);record.heroes[1]=Hero("partner",partner,camera);
                writer.WriteLine(JsonUtility.ToJson(record));frames++;body.span.frames++;body.span.lastSample=sample;
            }
        }
        static float[] Matrix(Camera camera)
        {
            var matrix=camera.projectionMatrix*camera.worldToCameraMatrix;var values=new float[16];
            for(int row=0;row<4;row++)for(int column=0;column<4;column++)values[row*4+column]=matrix[row,column];
            return values;
        }
        Shape Hero(string role,CombatActor actor,Camera camera)
        {
            if(actor!=null&&!heroMeshes.ContainsKey(actor))
            {
                if(heroMeshes.Count>=SourceLimit*2){Failure??="Release geometry hero cache limit exceeded";return new Shape{role=role};}
                heroMeshes.Add(actor,Meshes(actor.GetComponent<FormController>()?.flight?.transform));
            }
            var shape=Measure(role,actor!=null?actor.character:"",actor!=null?actor.transform:null,
                actor!=null?heroMeshes[actor]:Array.Empty<Renderer>(),camera,actor?.motor!=null?actor.motor.Velocity:Vector3.zero,
                actor!=null?HeroCollision.HullRadius(actor.character):0);
            if(actor==null||!actor.flight){shape.available=false;Failure??="Release geometry hero is not in flight";}
            return shape;
        }
        Shape Measure(string role,string hero,Transform root,Renderer[] meshes,Camera camera,Vector3 velocity,float radius)
        {
            var shape=new Shape{role=role,hero=hero,instance=root!=null?root.GetInstanceID():0,
                position=root!=null?root.position:Vector3.zero,rotation=root!=null?root.rotation:Quaternion.identity,
                velocity=velocity,hullRadius=radius};
            var corners=new List<Vector3>(meshes.Length*8);
            foreach(var mesh in meshes)
            {
                if(mesh==null||!mesh.enabled||!mesh.gameObject.activeInHierarchy)continue;
                var box=mesh.bounds;
                if(shape.meshes==0){shape.minimum=box.min;shape.maximum=box.max;}
                else{shape.minimum=Vector3.Min(shape.minimum,box.min);shape.maximum=Vector3.Max(shape.maximum,box.max);}
                // Transform local bounds before projection: projecting the world
                // AABB adds unnecessary space when a ship banks or animal turns.
                var local=mesh.localBounds;
                for(int corner=0;corner<8;corner++)
                {
                    var p=local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var world=mesh.transform.TransformPoint(p);corners.Add(world);
                    var viewport=camera.WorldToViewportPoint(world);
                    if(shape.meshes==0&&corner==0)shape.viewportMinimum=shape.viewportMaximum=viewport;
                    else{shape.viewportMinimum=Vector3.Min(shape.viewportMinimum,viewport);shape.viewportMaximum=Vector3.Max(shape.viewportMaximum,viewport);}
                }
                shape.meshes++;
            }
            shape.corners=corners.ToArray();shape.available=root!=null&&shape.meshes>0;
            if(!shape.available)Failure??="Release geometry unavailable for "+role;
            return shape;
        }
        public void Finish()
        {
            if(finished)return;
            if(samples==0)Failure??="Empty release geometry capture";
            var spans=new Span[bodies.Count];
            for(int i=0;i<bodies.Count;i++)
            {
                spans[i]=bodies[i].span;
                if(spans[i].end==null){spans[i].end="capture-ended-mid-release";Failure??="Release geometry ended during departure";}
            }
            writer.Dispose();finished=true;
            File.WriteAllText(Path.Combine(folder,"release-geometry.json"),JsonUtility.ToJson(new Report{
                samples=samples,frames=frames,dropped=dropped,frameLimit=frameLimit,spans=spans,complete=Failure==null,failure=Failure},true));
        }
        public void Dispose()
        {
            if(finished)return;
            Failure??="Release geometry capture interrupted";Finish();
        }
    }
}
