using System;
using UnityEngine;

namespace Lattice.Combat
{
    // Value data belongs to one launch, not to the pooled Projectile object or
    // a source that may be destroyed before impact. No history is retained here.
    public struct ProjectileOrigin
    {
        public bool projectile;
        public string captureId,sourceId;
        public long volleyId,shotId;
        public int sourceInstanceId,projectileInstanceId;
        public double releasedAt;
        public Vector3 position,velocity;
    }

    public static class ProjectileProvenance
    {
        static Capture current;
        public sealed class Capture:IDisposable
        {
            internal readonly Func<double> clock;
            public string Id{get;}=Guid.NewGuid().ToString("N");
            public long Volleys{get;internal set;}
            public long Launches{get;internal set;}
            internal Capture(Func<double> clock){this.clock=clock;}
            public void Dispose(){if(ReferenceEquals(current,this))current=null;}
        }
        public static Capture Begin(Func<double> clock)
        {
            if(clock==null)throw new ArgumentNullException(nameof(clock));
            if(current!=null)throw new InvalidOperationException("A projectile provenance capture already owns this session.");
            return current=new Capture(clock);
        }
        // One stamp before a fan loop gives its distinct launches one volley.
        // Single shots can omit it; StampLaunch supplies a fresh volley then.
        public static DamagePacket Volley(DamagePacket packet)
        {
            if(current!=null)packet.projectileOrigin=new ProjectileOrigin{captureId=current.Id,volleyId=++current.Volleys};
            return packet;
        }
        public static ProjectileOrigin StampLaunch(DamagePacket packet,Vector3 position,Vector3 velocity,int projectileInstanceId)
        {
            var origin=new ProjectileOrigin{projectile=true};
            if(current==null)return origin;
            origin.captureId=current.Id;
            origin.volleyId=packet.projectileOrigin.captureId==current.Id&&packet.projectileOrigin.volleyId>0?
                packet.projectileOrigin.volleyId:++current.Volleys;
            origin.shotId=++current.Launches;
            origin.sourceId=packet.source!=null?packet.source.id:"UNAVAILABLE";
            origin.sourceInstanceId=packet.source!=null?packet.source.GetInstanceID():0;
            origin.projectileInstanceId=projectileInstanceId;
            origin.releasedAt=current.clock();origin.position=position;origin.velocity=velocity;
            return origin;
        }
    }
}
