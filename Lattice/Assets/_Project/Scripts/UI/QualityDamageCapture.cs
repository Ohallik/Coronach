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
        }
        [Serializable] public sealed class Report
        {
            public bool complete;
            public int dropped;
            public string scope="Observed party damage events during a recorded replay; absence before subscription is not proof of no damage.";
            public Hit[] hits;
        }
        readonly string folder;
        readonly Func<double> seconds;
        readonly Func<int> step;
        readonly int capacity;
        readonly HashSet<Health> watched=new();
        readonly List<Hit> hits=new();
        int dropped;bool ended;
        public QualityDamageCapture(string folder,Func<double> seconds,Func<int> step,int capacity=4096)
        {this.folder=folder;this.seconds=seconds;this.step=step;this.capacity=Math.Max(1,capacity);}
        public void Watch(Health health)
        {if(!ended&&health!=null&&watched.Add(health))health.Damaged+=Record;}
        void Record(Health victim,DamagePacket packet,float amount)
        {
            if(hits.Count>=capacity){dropped++;return;}
            hits.Add(new Hit{seconds=seconds(),step=step(),scene=SceneFlow.Current!=null?SceneFlow.Current.Zone:"UNAVAILABLE",
                victim=victim.id,source=packet.source!=null?packet.source.id:"UNAVAILABLE",type=packet.type.ToString(),tag=packet.tag,
                sourceAvailable=packet.source!=null,sourceAlive=packet.source!=null&&packet.source.Alive,deathAttack=packet.deathAttack,
                requested=packet.amount,received=amount,remaining=victim.integrity,position=victim.transform.position});
        }
        public void Finish(){End(true);}
        void End(bool complete)
        {
            if(ended)return;ended=true;
            foreach(var health in watched)if(health!=null)health.Damaged-=Record;
            watched.Clear();
            File.WriteAllText(Path.Combine(folder,"damage.json"),JsonUtility.ToJson(new Report{complete=complete&&dropped==0,dropped=dropped,hits=hits.ToArray()},true));
        }
        public void Dispose(){End(false);}
    }
}
