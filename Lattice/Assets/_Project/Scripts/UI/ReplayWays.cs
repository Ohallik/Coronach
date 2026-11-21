using System.Collections.Generic;
using System.Globalization;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>
    /// Whether the way ahead is clear of what a room does on a clock (chapter 8), as a player who has watched it a while knows: a buoy's beam sweeping the Picket of Teeth, the Scoria
    /// Flows' crust breaking open, a ladle's pour across a Foundry floor, a drop press; in flight, a wreck riding a track (chapter 9: Nacre Grave's tow line and berth ring, the
    /// Ring's channel; ContinuousReview.WreckOn). A decision only, from what is on the screen and its own timetable; the driver waits with the pad
    /// held still until it is true (<c>"until":"clear:x,z;x,z"</c>, ContinuousReview.Rooms.cs) and then walks or flies the way by the stick, and the room does to the party whatever
    /// it would do to a person who misjudged it. A route can't time these from a load: a buoy's clock starts from the game's, and a route's walks take as long as they take.
    /// <para>The way is the lead's own position, then each point in turn. Setting off now at the lead's own pace (a ship from rest at its motor's thrust and drag, a body at its
    /// walk), every sample of it must be clear for the whole spell the lead might be there: from <see cref="Margin"/> before the time it should get there to <see cref="Slack"/> times
    /// that and <see cref="Margin"/> after, because a turn, a shove or the partner in the way makes a walk slower than its sum.</para>
    /// </summary>
    public static class ReplayWays
    {
        /// <summary>Seconds of room either side of the moment a sample is reached, how much slower than the sum a real walk may be, and the step the spell is looked at in.</summary>
        public const float Margin=.4f,Slack=1.15f,Beat=.1f;
        /// <summary>The spacing of samples along a way: a ship covers a metre in a twelfth of a second, a body half a metre in a thirteenth.</summary>
        const float FlightSpacing=1,FootSpacing=.5f;
        /// <summary>"x,z;x,z" (invariant culture) as plan points; an empty or unreadable list is no way at all.</summary>
        public static List<Vector2> Points(string text)
        {
            var points=new List<Vector2>();if(string.IsNullOrEmpty(text))return points;
            foreach(string pair in text.Split(';'))
            {
                var parts=pair.Split(',');
                if(parts.Length!=2||!float.TryParse(parts[0],NumberStyles.Float,CultureInfo.InvariantCulture,out float x)||!float.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out float z))
                    return new List<Vector2>();
                points.Add(new Vector2(x,z));
            }
            return points;
        }
        /// <summary>Clear: nothing the room times will be on the way while the lead is on it, setting off now.</summary>
        public static bool Clear(CombatActor lead,IReadOnlyList<Vector2> way)=>Blocked(lead,way)==null;
        /// <summary>What would be on the way (a buoy's name, a plate's, a pour's), or null when nothing would: for the log and a test.</summary>
        public static string Blocked(CombatActor lead,IReadOnlyList<Vector2> way)
        {
            if(lead==null||way==null||way.Count==0)return "no way";
            var at=lead.transform.position;var from=new Vector2(at.x,at.z);
            var samples=Samples(from,way,lead.flight?FlightSpacing:FootSpacing);
            var times=Arrivals(lead,samples);
            var presses=lead.flight?null:Presses();
            for(int i=0;i<samples.Count;i++)
            {
                float t=times[i];
                for(float spell=Mathf.Max(0,t-Margin);spell<=t*Slack+Margin;spell+=Beat)
                {
                    string what=lead.flight?Beam(samples[i].at,spell)??ContinuousReview.WreckOn(samples[i].at,spell,ContinuousReview.WreckRoom(lead)):Underfoot(new Vector3(samples[i].at.x,at.y,samples[i].at.y),spell,presses);
                    if(what!=null)return what;
                }
            }
            return null;
        }
        /// <summary>Points along the way at a spacing, with how far along each is, the end of each leg included.</summary>
        static List<(Vector2 at,float s)> Samples(Vector2 from,IReadOnlyList<Vector2> way,float spacing)
        {
            var samples=new List<(Vector2,float)>{(from,0)};float s=0;var a=from;
            foreach(var b in way)
            {
                float leg=Vector2.Distance(a,b);int n=Mathf.Max(1,Mathf.CeilToInt(leg/spacing));
                for(int k=1;k<=n;k++)samples.Add((Vector2.Lerp(a,b,k/(float)n),s+leg*k/n));
                s+=leg;a=b;
            }
            return samples;
        }
        /// <summary>When the lead gets to each sample, setting off now: a ship from the speed it has, under full thrust, by its own motor's numbers (FlightMotor.Integrate); a body
        /// at its walk.</summary>
        static float[] Arrivals(CombatActor lead,List<(Vector2 at,float s)> samples)
        {
            var times=new float[samples.Count];
            if(lead.motor is FlightMotor ship)
            {
                float speed=ship.Velocity.magnitude,travelled=0,time=0;const float dt=.02f;int i=0;
                while(i<samples.Count&&time<60)
                {
                    while(i<samples.Count&&samples[i].s<=travelled+1e-4f){times[i]=time;i++;}
                    float next=Mathf.Min(ship.maxSpeed,speed+(ship.acceleration-ship.drag*speed)*dt);travelled+=(speed+next)*.5f*dt;speed=next;time+=dt;
                }
                for(;i<samples.Count;i++)times[i]=time;
                return times;
            }
            // At its walk, and whatever slows it now (an ash squall, a burning patch: Health.Pace).
            float pace=Mathf.Max(.5f,(lead.motor is GroundMotor ground?ground.speed:5)*lead.Health.Pace);
            for(int i=0;i<samples.Count;i++)times[i]=samples[i].s/pace+.15f;
            return times;
        }
        /// <summary>A live buoy's beam on this point that many seconds from now, at the pace it sweeps now (SensorSweep.YawAt on its own clock), cut by the rocks as it always is.</summary>
        static string Beam(Vector2 p,float ahead)
        {
            foreach(var sweep in SensorSweep.All)
            {
                if(sweep==null||sweep.Dark||!sweep.isActiveAndEnabled)continue;
                float yaw=SensorSweep.YawAt(sweep.from,sweep.to,sweep.period,sweep.phase,sweep.Clock+sweep.Pace*ahead);
                if(SensorSweep.Sees(sweep.Origin,yaw,sweep.half,sweep.reach,p))return sweep.name;
            }
            return null;
        }
        static string pressZone;static ForgePress[] pressList;
        static ForgePress[] Presses()
        {
            string zone=SceneFlow.Current!=null?SceneFlow.Current.Zone:"";
            if(pressList==null||zone!=pressZone||System.Array.Exists(pressList,p=>p==null)){pressZone=zone;pressList=Object.FindObjectsByType<ForgePress>(FindObjectsSortMode.None);}
            return pressList;
        }
        /// <summary>Ground that will hurt a body standing on it that many seconds from now: crust open (or still rising) under it, a ladle's strip marked or pouring, a tap run
        /// still running, a press's square marked or coming down.</summary>
        static string Underfoot(Vector3 p,float ahead,ForgePress[] presses)
        {
            var field=CrustField.Current;
            if(field!=null)
            {
                var under=field.At(p);
                if(under!=null&&field.Burns(under,field.Clock+ahead)){var drawn=field.Drawn(p);if(drawn==null||drawn==under||field.Burns(drawn,field.Clock+ahead))return under.name;}
            }
            foreach(var pour in PourChannel.All)
            {
                if(pour==null||!pour.isActiveAndEnabled||!pour.Armed||!pour.Contains(p))continue;
                if(pour.period<=0)return pour.name;
                var stage=pour.StageAt(pour.Clock+ahead);if(stage==PourChannel.Stage.Warning||stage==PourChannel.Stage.Pouring)return pour.name;
            }
            if(presses!=null)foreach(var press in presses)
            {
                if(press==null||!press.isActiveAndEnabled||!press.Armed||!press.Contains(p))continue;
                if(Mathf.Repeat(press.Clock+ahead,press.period)<press.warn+press.hold)return press.name;
            }
            return null;
        }
    }
}
