using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// What an attack leaves on the floor for a few seconds: a spore cloud that slows, a puddle, a patch of burning slag, a blinding haze. One rule each: a hero standing in a Slow
    /// wades at a share of his pace (<see cref="Health.Hinder"/>: a roll and a dash keep their speed, so there is always a way out), a Burn takes a little integrity twice a second,
    /// a Blind stands between the eye and the floor: every marking laid inside it is drawn as nothing while it lasts (<see cref="Telegraph.Hide"/>), still everything else it is, so
    /// what strikes in a spore cloud strikes unseen and the answer is to keep out of the cloud (chapter 4's Spore Pod: visibility is the resource). Its edge is a marking like any other, and what kind says which: a Slow and a Blind are boundaries (a pale ring
    /// under alpha .99, D222), since nothing there strikes; a Burn damages, so it is drawn as the danger it is (D217). It is nobody's attack once laid: its marking belongs to the
    /// area, not to the body that made it, so neither the partner nor a test reads a cloud as a blow about to land. Laid by a lobber, a charger's end, or a boss's move.
    /// </summary>
    public sealed class LingeringArea:MonoBehaviour
    {
        public static readonly List<LingeringArea> All=new();
        /// <summary>A cloud's and a puddle's ring: moss and water, neither the party's cream and cyan nor a danger's orange, red or purple.</summary>
        public static readonly Color Slowing=new(.56f,.84f,.5f,.38f),Blinding=new(.82f,.84f,.7f,.38f),Wet=new(.5f,.78f,.86f,.38f);
        public const float BurnEvery=.5f;
        public AreaEffect Effect{get;private set;}
        public float Radius{get;private set;}
        public float Pace{get;private set;}
        public float Remaining=>Mathf.Max(0,until-Time.time);
        Telegraph ring;float until,nextBurn,nextPuff;DamagePacket burn;Color tone;
        /// <param name="tick">A Burn's damage each second, before the zone's threat; nothing for the others.</param>
        /// <param name="tint">The ring's own colour for a Slow (a puddle is water, a cloud is spore); unset, a cloud's.</param>
        public static LingeringArea Lay(Vector3 at,float radius,float seconds,AreaEffect effect,float pace,float tick,Health source,DamageType type,Color? tint=null)
        {
            var area=new GameObject("Lingering "+effect).AddComponent<LingeringArea>();area.transform.position=at;
            area.Effect=effect;area.Radius=radius;area.Pace=Mathf.Clamp(pace<=0?.6f:pace,.1f,1);area.until=Time.time+Mathf.Max(.5f,seconds);
            float scale=source!=null&&source.TryGetComponent<EnemyBrain>(out var brain)?brain.DamageScale:1;
            area.burn=new DamagePacket{source=source,amount=Mathf.Max(0,tick)*BurnEvery*scale,type=type,weight=HitWeight.Tick,deathAttack=true};
            area.tone=effect==AreaEffect.Burn?Telegraph.Threat:tint??(effect==AreaEffect.Blind?Blinding:Slowing);
            area.ring=Telegraph.Circle(at,radius,seconds,area.tone);area.ring.Steady=true;area.ring.owner=area;
            // The first bite of a burn waits a beat: whoever was standing where it landed has just been hit by the thing that laid it.
            area.nextBurn=Time.time+BurnEvery;
            return area;
        }
        /// <summary>The same, laid as a strip instead of a circle (chapter 5: the burning line a cutting torch leaves on a deck). Its middle is the area's place and its puffs run along it.</summary>
        public static LingeringArea LayLine(Vector3 origin,Vector3 direction,float length,float width,float seconds,AreaEffect effect,float pace,float tick,Health source,DamageType type,Color? tint=null)
        {
            direction.y=0;direction=direction.sqrMagnitude>.0001f?direction.normalized:Vector3.forward;
            var area=Lay(origin+direction*(length*.5f),width*.5f,seconds,effect,pace,tick,source,type,tint);
            Destroy(area.ring.gameObject);area.ring=Telegraph.Line(origin,direction,length,width,seconds,area.tone);area.ring.Steady=true;area.ring.owner=area;
            area.along=direction*(length*.5f);return area;
        }
        /// <summary>A strip's half-length along its way, from its middle; nothing for a circle.</summary>
        Vector3 along;
        public bool Contains(Vector3 point)=>ring!=null&&ring.Contains(point);
        public static LingeringArea Over(Vector3 point){foreach(var area in All)if(area!=null&&area.Contains(point))return area;return null;}
        /// <summary>Gone now, quietly: the fight it belonged to is over.</summary>
        public void Dismiss(){if(ring!=null)ring.Cancel();Destroy(gameObject);}
        public static void DismissAll(){for(int i=All.Count-1;i>=0;i--)if(All[i]!=null)All[i].Dismiss();}
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);Unveil();}
        /// <summary>The markings a Blind has drawn as nothing, to be drawn again as it goes.</summary>
        readonly List<Telegraph> veiled=new();
        /// <summary>Whether a marking is drawn as nothing by a Blind standing over it now.</summary>
        public static bool Veils(Telegraph marking){foreach(var area in All)if(area!=null&&area.veiled.Contains(marking))return true;return false;}
        void Veil()
        {
            foreach(var marking in Telegraph.Live)
            {
                if(marking==null||marking==ring||marking.owner is LingeringArea||veiled.Contains(marking))continue;
                var offset=marking.Origin-transform.position;offset.y=0;if(offset.sqrMagnitude>Radius*Radius)continue;
                marking.Hide(true);veiled.Add(marking);
            }
        }
        void Unveil(){foreach(var marking in veiled)if(marking!=null)marking.Hide(false);veiled.Clear();}
        void Update()
        {
            if(Effect==AreaEffect.Blind&&ring!=null&&Time.time<until)Veil();
            if(GameTime.CombatHeld)return;
            if(Time.time>=until||ring==null){Dismiss();return;}
            var party=PartyController.Current;if(party==null)return;
            bool bite=Effect==AreaEffect.Burn&&Time.time>=nextBurn;if(bite)nextBurn=Time.time+BurnEvery;
            foreach(var hero in party.members)
            {
                if(!hero.Health.Alive||!ring.Contains(hero.transform.position))continue;
                if(Effect==AreaEffect.Slow)hero.Health.Hinder(Pace);
                else if(bite&&burn.amount>0)hero.Health.Receive(burn);
            }
            // The cloud itself, over its ring: a slow one hangs low, a blinding one stands tall. On its own clock and its own numbers: no fight roll is drawn for it.
            if(Time.time<nextPuff)return;nextPuff=Time.time+(Effect==AreaEffect.Blind?.18f:.45f);
            float turn=Time.time*2.4f,off=Radius*(.25f+.5f*Mathf.Abs(Mathf.Sin(Time.time*1.7f)));
            var puff=along.sqrMagnitude>0?transform.position+along*Mathf.Sin(turn*1.3f)+Vector3.up*.5f:transform.position+new Vector3(Mathf.Cos(turn)*off,Effect==AreaEffect.Blind?1.4f:.5f,Mathf.Sin(turn)*off);
            CombatVfx.Burst(puff,new Color(tone.r,tone.g,tone.b,Effect==AreaEffect.Blind?.8f:.5f),Effect==AreaEffect.Burn?"failure":"steam");
        }
    }
}
