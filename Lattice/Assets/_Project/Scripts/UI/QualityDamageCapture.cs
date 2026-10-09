using System;
using System.Collections.Generic;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.UI
{
    // Capture-only diagnostic. Snapshot the event while it happens: a later
    // health sample cannot identify the source or its state at impact.
    public sealed class QualityDamageCapture:IDisposable
    {
        [Serializable] public sealed class Hit
        {
            public double seconds;
            public int step;
            public string scene,victim,source,type,tag;
            public bool sourceAvailable,sourceAlive,deathAttack;
            public float requested,received,remaining;
            public Vector3 position;
            public bool projectile,provenanceAvailable;
            public string releaseCapture,releaseSource;
            public long volleyId,shotId;
            public int sourceInstanceId,impactSourceInstanceId,projectileInstanceId;
            public double releasedAt;
            public Vector3 releasePosition,releaseVelocity;
        }
        [Serializable] public sealed class Report
        {
            public int version=2;
            public string captureId;
            public long projectileLaunches,projectileVolleys;
            public bool complete;
            public int dropped;
            public string scope="Observed party damage events during a recorded replay; absence before subscription is not proof of no damage.";
            public Hit[] hits;
        }
        readonly string folder;
        readonly Func<double> seconds;
        readonly Func<int> step;
        readonly int capacity;
        readonly ProjectileProvenance.Capture provenance;
        readonly HashSet<Health> watched=new();
        readonly List<Hit> hits=new();
        int dropped;bool ended;
        public QualityDamageCapture(string folder,Func<double> seconds,Func<int> step,int capacity=4096)
        {this.folder=folder;this.seconds=seconds;this.step=step;this.capacity=Math.Max(1,capacity);provenance=ProjectileProvenance.Begin(seconds);}
        public void Watch(Health health)
        {if(!ended&&health!=null&&watched.Add(health))health.Damaged+=Record;}
        void Record(Health victim,DamagePacket packet,float amount)
        {
            if(hits.Count>=capacity){dropped++;return;}
            var origin=packet.projectileOrigin;
            hits.Add(new Hit{seconds=seconds(),step=step(),scene=SceneFlow.Current!=null?SceneFlow.Current.Zone:"UNAVAILABLE",
                victim=victim.id,source=packet.source!=null?packet.source.id:"UNAVAILABLE",type=packet.type.ToString(),tag=packet.tag,
                sourceAvailable=packet.source!=null,sourceAlive=packet.source!=null&&packet.source.Alive,deathAttack=packet.deathAttack,
                requested=packet.amount,received=amount,remaining=victim.integrity,position=victim.transform.position,
                projectile=origin.projectile,provenanceAvailable=origin.projectile&&origin.captureId==provenance.Id&&origin.shotId>0,
                releaseCapture=origin.captureId,releaseSource=origin.sourceId,volleyId=origin.volleyId,shotId=origin.shotId,
                sourceInstanceId=origin.sourceInstanceId,impactSourceInstanceId=packet.source!=null?packet.source.GetInstanceID():0,
                projectileInstanceId=origin.projectileInstanceId,releasedAt=origin.releasedAt,
                releasePosition=origin.position,releaseVelocity=origin.velocity});
        }
        public void Finish(){End(true);}
        void End(bool complete)
        {
            if(ended)return;ended=true;
            try
            {
                foreach(var health in watched)if(health!=null)health.Damaged-=Record;
                watched.Clear();
                File.WriteAllText(Path.Combine(folder,"damage.json"),JsonUtility.ToJson(new Report{
                    captureId=provenance.Id,projectileLaunches=provenance.Launches,projectileVolleys=provenance.Volleys,
                    complete=complete&&dropped==0,dropped=dropped,hits=hits.ToArray()},true));
            }
            finally{provenance.Dispose();}
        }
        public void Dispose(){End(false);}
    }
}
