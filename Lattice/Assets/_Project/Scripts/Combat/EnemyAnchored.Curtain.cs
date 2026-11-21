using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The anchored body's <b>curtain</b> (<c>tune.count</c> over nought: chapter 8's Flak Gate, in the wall across the Picket of Teeth). A gate in a flight lane whose flak fills the lane in
    /// front of it in rows, on a clock of its own: <c>tune.count</c> rows lie across the way it faces, from its own line out to <c>tune.reach</c> ahead, each <c>tune.width</c> long and centred
    /// on it. Every beat (the attack's tell, then its cooldown) it marks alternate rows, the first, third and fifth, then the second and fourth, and bursts flak in them. A row never follows a
    /// ship, so the quiet rows are a clock to cross on, and a ship working in front of it (the route lock's pieces) waits for its row's quiet beat. It fires while any hero is within its
    /// attack's range. The flak bursts in the row: nothing comes from the gate, so a hull between the gate and a ship is no cover from it. Two curtains that overlap (two gates either side of
    /// one lane) strike a ship once a volley between them.
    /// Reads: attacks[0] (telegraph, range is how near a hero must come for it to fire at all, damage, cooldown is the quiet between beats), tune.count (rows), reach (where the last row ends
    /// ahead, 24 m), width (each row's length across the lane, 52 m).
    /// </summary>
    public sealed partial class EnemyAnchored
    {
        bool Curtained=>tune.count>0;
        int Rows=>Mathf.Max(1,tune.count);float Deep=>Or(tune.reach,24);float Across=>Or(tune.width,52);float RowDepth=>Deep/Rows;
        int beat;readonly List<Telegraph> rows=new();
        static readonly Color FlakTone=new(1.35f,.95f,.55f,.9f);
        /// <summary>The beats fired since it woke: an even beat marks the first, third and fifth row, an odd one the others.</summary>
        public int Beat=>beat;
        /// <summary>One row of its curtain as a lane across the way it faces: where the row starts, the way it runs, how long and how deep.</summary>
        public (Vector3 origin,Vector3 along,float length,float depth) Row(int index)
        {
            var ahead=Flat(body.forward).normalized;var across=Vector3.Cross(Vector3.up,ahead);
            var centre=body.position+ahead*(RowDepth*(index+.5f));
            return (centre-across*(Across*.5f),across,Across,RowDepth);
        }
        /// <summary>When each hero was last struck by any curtain, so two that overlap strike once a volley.</summary>
        static readonly Dictionary<Health,float> struckAt=new();
        const float SameVolley=.3f;
        void Curtain(CombatActor victim)
        {
            if(brain.Telegraphing)
            {
                if(!brain.TellRunOut)return;
                var mark=brain.EndTell();brain.Rest(Cooldown);beat++;
                if(mark!=null&&!rows.Contains(mark))rows.Add(mark);
                var packet=brain.Blow(attack,HitWeight.Medium);int landed=0;
                foreach(var row in rows)
                {
                    if(ReferenceEquals(row,null))continue;
                    landed+=Burst(row,packet);row.Release();
                    for(int i=1;i<=3;i++)CombatVfx.Burst(row.Origin+row.Forward*(row.Length*i/4f)+Vector3.up*.3f,FlakTone,"kill");
                }
                rows.Clear();Sound("explosionCrunch_000",.2f,1.45f);Debug.Log($"CURTAIN_BURST {def.id} beat={beat} struck={landed}");
                return;
            }
            if(!brain.Rested||!AnyNear())return;
            // Every other row, by the beat: the clock, not the ship. What the brain tells (the partner's and the driver's dodge, the wind-up) is the row he is in, if any is.
            Telegraph primary=null;
            for(int i=Rows>1?beat%2:0;i<Rows;i+=2)
            {
                var (origin,along,length,depth)=Row(i);
                var row=Telegraph.Line(origin,along,length,depth,Tell,Telegraph.Threat,true);row.owner=brain;rows.Add(row);
                if(primary==null||row.Contains(victim.transform.position))primary=row;
            }
            if(primary==null)return;
            rows.Remove(primary);
            Mark(primary,Tell,Flat(body.forward),victim,"forceField_000",.9f);
            Debug.Log($"CURTAIN_MARKED {def.id} beat={beat}");
        }
        bool AnyNear()
        {
            if(PartyController.Current==null)return false;
            foreach(var hero in PartyController.Current.members)if(hero.Health.Alive&&Flat(hero.transform.position-body.position).sqrMagnitude<Range*Range)return true;
            return false;
        }
        int Burst(Telegraph row,DamagePacket packet)
        {
            int landed=0;if(PartyController.Current==null)return 0;
            // Only the last volley matters: the heroes of a zone left behind go with it.
            if(struckAt.Count>16)struckAt.Clear();
            foreach(var hero in PartyController.Current.members)
            {
                if(!hero.Health.Alive||!row.Contains(hero.transform.position))continue;
                if(struckAt.TryGetValue(hero.Health,out float at)&&Time.time-at<SameVolley&&Time.time>=at)continue;
                if(hero.Health.Receive(packet)<=0)continue;
                struckAt[hero.Health]=Time.time;landed++;
            }
            return landed;
        }
        /// <summary>Its other rows go with the tell the brain cancels (a break, a lunge mid-tell, its death).</summary>
        void Unmark(){foreach(var row in rows)if(row!=null)row.Cancel();rows.Clear();}
    }
}
