using UnityEngine;

namespace Lattice.Combat
{
    // A tiny visibility graph around the measured head/body envelopes. It
    // guides ordinary motor input; it never relocates a ship or hides anatomy.
    sealed class FlightSeparation
    {
        const int SectionsPerBody=8,Directions=8;
        readonly int sections,capacity,measuredSections,departureSection;
        readonly CantorRelease departure;
        readonly Renderer[][] meshes;
        readonly Vector3[] centers,nodes;
        readonly float[] radii,cost;
        readonly int[] previous;
        readonly bool[] valid,visited;
        readonly RaycastHit[] shotHits=new RaycastHit[32];
        readonly float hull;
        public SerpentSegments Body {get;}
        public SerpentSegments OtherBody {get;}
        public SerpentSegments DepartingBody {get;}
        public bool EscapingDeparture {get;private set;}
        float nextPlan;
        Vector3 waypoint,lastGoal,forecastExit;
        bool escapingForecast;
        bool detour,adjustedGoal;
        Health firingTarget;
        public FlightSeparation(SerpentSegments body,float hull,SerpentSegments otherBody=null)
        {
            Body=body;OtherBody=otherBody;this.hull=hull;
            // At most one selected animal and one departing animal. A new
            // live target cannot silently discard the nearby release hazard.
            measuredSections=SectionsPerBody*(otherBody!=null?2:1);
            departure=body.GetComponent<CantorRelease>();
            if(departure==null||!departure.Departing){departure=otherBody!=null?otherBody.GetComponent<CantorRelease>():null;departureSection=SectionsPerBody;}
            if(departure!=null&&!departure.Departing)departure=null;
            DepartingBody=departure!=null?departure.GetComponent<SerpentSegments>():null;
            sections=measuredSections+(departure!=null?5:0);capacity=2+sections*Directions;
            meshes=new Renderer[sections][];centers=new Vector3[sections];radii=new float[sections];
            nodes=new Vector3[capacity];cost=new float[capacity];previous=new int[capacity];
            valid=new bool[capacity];visited=new bool[capacity];
            Capture(body,0);if(otherBody!=null)Capture(otherBody,SectionsPerBody);
        }
        void Capture(SerpentSegments body,int offset)
        {
            meshes[offset]=body.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>();
            for(int i=1;i<SectionsPerBody;i++)meshes[offset+i]=body.Parts[i-1].GetComponentsInChildren<Renderer>();
        }
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
        void Measure()
        {
            for(int i=0;i<measuredSections;i++)
            {
                var bounds=meshes[i][0].bounds;
                for(int j=1;j<meshes[i].Length;j++)bounds.Encapsulate(meshes[i][j].bounds);
                centers[i]=Flat(bounds.center);
                radii[i]=new Vector2(bounds.extents.x,bounds.extents.z).magnitude+hull+.8f;
            }
            // Static obstacles alone route across the animal's accelerating
            // nose, then brake too late as it catches the ship. Reserve the
            // next 1.5 seconds of its real path with five bounded envelopes.
            if(departure!=null)
                for(int i=0;i<5;i++)
                {
                    centers[measuredSections+i]=Flat(departure.PredictPosition((i+1)*.3f));
                    radii[measuredSections+i]=radii[departureSection]+.4f;
                }
        }
        float Clearance(Vector3 p,int count=-1)
        {float best=float.PositiveInfinity;if(count<0)count=sections;for(int i=0;i<count;i++)best=Mathf.Min(best,(p-centers[i]).magnitude-radii[i]);return best;}
        bool Clear(Vector3 a,Vector3 b,bool escapeForecast=false,bool currentOnly=false)
        {
            var d=b-a;float length=d.sqrMagnitude;
            int count=currentOnly?measuredSections:sections;
            for(int i=0;i<count;i++)
            {
                var away=a-centers[i];float radius=radii[i];
                // Permit an outward escape if a moving body has already
                // entered the safety margin, never a path deeper through it.
                if(away.sqrMagnitude<radius*radius)
                {
                    // A ship can start inside several future head envelopes.
                    // Those are alternative times, not simultaneous anatomy:
                    // demanding outward travel from each can leave no route.
                    // The escape starts toward an endpoint outside the union.
                    // Ordinary routes keep the full forecast checks. Current body
                    // margins and dodge paths retain their outward constraint.
                    if((i<measuredSections||!escapeForecast)&&Vector3.Dot(d,away)<0)return false;
                    continue;
                }
                float t=length>.0001f?Mathf.Clamp01(Vector3.Dot(centers[i]-a,d)/length):0;
                if((a+d*t-centers[i]).sqrMagnitude<radius*radius)return false;
            }
            return true;
        }
        public Vector3 Guide(Vector3 position,Vector3 goal,Health target,out bool avoiding,out bool brake)
        {
            bool changed=target!=firingTarget;firingTarget=target;
            float height=position.y;position=Flat(position);goal=Flat(goal);
            if(changed||Time.unscaledTime>=nextPlan||(goal-lastGoal).sqrMagnitude>4||(waypoint-position).sqrMagnitude<.5f)
            {
                nextPlan=Time.unscaledTime+.1f;lastGoal=goal;Measure();Plan(position,goal);
            }
            // Forecasts reserve a path; they are not already touching the
            // ship. Braking for those future envelopes caps an early escape
            // near 2 m/s, letting the accelerating departure catch it.
            var result=detour||adjustedGoal?waypoint:goal;
            avoiding=detour;brake=Clearance(position,measuredSections)<3.5f;
            // A nearby departing head can catch a correctly routed ship if
            // proximity braking caps its outward thrust near 2 m/s. Let that
            // escape accelerate only when it opens every nearby body margin.
            // A final formation leg can still be an urgent outward escape;
            // requiring an intermediate detour would brake it to walking speed.
            // Inward/tangent approaches and ordinary live combat still brake.
            EscapingDeparture=(brake||escapingForecast)&&departure!=null&&OutwardEscape(position,result);
            if(EscapingDeparture)brake=false;
            result.y=height;return result;
        }
        bool OutwardEscape(Vector3 position,Vector3 goal)
        {
            var direction=goal-position;if(direction.sqrMagnitude<.01f)return false;
            for(int i=0;i<measuredSections;i++)
            {
                var away=position-centers[i];
                if(away.magnitude-radii[i]<3.5f&&Vector3.Dot(direction,away)<=0)return false;
            }
            return true;
        }
        void Plan(Vector3 start,Vector3 goal)
        {
            // Keep the escape point through the bounded departure. A clear
            // forecast around the ship does not yet make crossing back toward
            // formation safe: the long trailing body may still be passing.
            // Removal/recovery releases this guide through its existing owner.
            if(escapingForecast)
            {
                if(departure!=null&&departure.Departing)
                {
                    // The forecast moves and changes width while the head turns.
                    // Keep leaving its corridor; do not stop at an old boundary
                    // or reverse toward formation when that boundary advances.
                    var away=forecastExit-start;
                    if(Clearance(start)<1&&away.sqrMagnitude<9&&away.sqrMagnitude>.01f)
                        forecastExit+=away.normalized*4;
                    if(Clear(start,forecastExit,currentOnly:true))
                    {waypoint=forecastExit;detour=true;adjustedGoal=false;return;}
                }
                escapingForecast=false;
            }
            nodes[0]=start;nodes[1]=goal;valid[0]=true;valid[1]=Clearance(goal)>=0&&CanFire(goal);adjustedGoal=!valid[1];
            for(int i=0;i<sections;i++)for(int j=0;j<Directions;j++)
            {
                int n=2+i*Directions+j;float angle=j*Mathf.PI*2/Directions;
                nodes[n]=centers[i]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radii[i]+1.1f);
                valid[n]=Clearance(nodes[n])>=0;
            }
            // Escape an approaching departure before trying to regain formation.
            // Prefer the nearest clear exit, not the exit closest to the leader:
            // that preference can steer ahead of the accelerating nose.
            if(departure!=null&&Clearance(start)<1&&Clearance(start,measuredSections)<3.5f)
            {
                float nearest=float.PositiveInfinity;
                for(int n=2;n<capacity;n++)
                {
                    float distance=(nodes[n]-start).sqrMagnitude;
                    if(!valid[n]||distance>=nearest||!Clear(start,nodes[n],escapeForecast:true)||!OutwardEscape(start,nodes[n]))continue;
                    nearest=distance;forecastExit=nodes[n];
                }
                if(!float.IsPositiveInfinity(nearest))
                {waypoint=forecastExit;escapingForecast=true;detour=true;adjustedGoal=false;return;}
            }
            // A chosen segment's flank can lie inside another bend. Use the
            // nearest clear perimeter point instead of crossing that anatomy.
            if(!valid[1])
            {
                float nearest=float.PositiveInfinity;
                for(int n=2;n<capacity;n++)if(valid[n]&&(nodes[n]-goal).sqrMagnitude<nearest&&CanFire(nodes[n]))
                {nearest=(nodes[n]-goal).sqrMagnitude;nodes[1]=nodes[n];valid[1]=true;}
            }
            for(int i=0;i<capacity;i++){cost[i]=float.PositiveInfinity;previous[i]=-1;visited[i]=false;}
            cost[0]=0;
            for(int pass=0;pass<capacity;pass++)
            {
                int current=-1;float cheapest=float.PositiveInfinity;
                for(int i=0;i<capacity;i++)if(valid[i]&&!visited[i]&&cost[i]<cheapest){current=i;cheapest=cost[i];}
                if(current<0||current==1)break;visited[current]=true;
                for(int next=1;next<capacity;next++)
                {
                    if(!valid[next]||visited[next])continue;
                    float proposed=cheapest+Vector3.Distance(nodes[current],nodes[next]);
                    if(proposed<cost[next]&&Clear(nodes[current],nodes[next])){cost[next]=proposed;previous[next]=current;}
                }
            }
            int step=1;
            if(previous[step]>=0)
            {
                while(previous[step]>0)step=previous[step];
                // A safe replacement for the final flank is still a firing
                // position. Only intermediate corners suppress final aiming.
                waypoint=nodes[step];detour=step!=1;return;
            }
            // No connected perimeter yet: move toward the clearest escape
            // that heads outward from all currently intersected margins.
            float best=float.NegativeInfinity;waypoint=start;
            for(int n=2;n<capacity;n++)if(valid[n]&&Clear(start,nodes[n]))
            {float value=-(nodes[n]-goal).magnitude;if(value>best){best=value;waypoint=nodes[n];}}
            detour=true;
        }
        bool CanFire(Vector3 from)
        {
            if(firingTarget==null)return true;
            from.y=firingTarget.transform.position.y;
            var delta=firingTarget.transform.position-from;
            int count=Physics.SphereCastNonAlloc(from,.15f,delta.normalized,shotHits,delta.magnitude,~0,QueryTriggerInteraction.Collide);
            if(count==shotHits.Length)return false;
            float nearest=float.PositiveInfinity;Health first=null;
            for(int i=0;i<count;i++)
            {
                var hit=shotHits[i];if(hit.distance>=nearest)continue;
                if(CombatCover.Opaque(hit.collider)){nearest=hit.distance;first=null;continue;}
                if(!hit.collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||box.owner.friendly)continue;
                nearest=hit.distance;first=box.owner;
            }
            return first==firingTarget;
        }
        public Vector3 Dodge(Vector3 position,Vector3 wanted)
        {
            Measure();position=Flat(position);wanted=Flat(wanted).normalized;
            if(Clear(position,position+wanted*4))return wanted;
            Vector3 best=Vector3.zero;float score=float.NegativeInfinity;
            for(int i=1;i<Directions;i++)
            {
                var direction=Quaternion.Euler(0,i*45,0)*wanted;
                if(!Clear(position,position+direction*4))continue;
                float value=Vector3.Dot(direction,wanted);
                if(value>score){score=value;best=direction;}
            }
            return best;
        }
    }
}
