using UnityEngine;

namespace Lattice.Combat
{
    // A tiny visibility graph around the measured head/body envelopes. It
    // guides ordinary motor input; it never relocates a ship or hides anatomy.
    sealed class FlightSeparation
    {
        const int Sections=8,Directions=8,Capacity=2+Sections*Directions;
        readonly Renderer[][] meshes=new Renderer[Sections][];
        readonly Vector3[] centers=new Vector3[Sections],nodes=new Vector3[Capacity];
        readonly float[] radii=new float[Sections],cost=new float[Capacity];
        readonly int[] previous=new int[Capacity];
        readonly bool[] valid=new bool[Capacity],visited=new bool[Capacity];
        readonly RaycastHit[] shotHits=new RaycastHit[32];
        readonly float hull;
        public SerpentSegments Body {get;}
        float nextPlan;
        Vector3 waypoint,lastGoal;
        bool detour,adjustedGoal;
        Health firingTarget;
        public FlightSeparation(SerpentSegments body,float hull)
        {
            Body=body;this.hull=hull;
            meshes[0]=body.GetComponent<DefeatPresentation>().visual.GetComponentsInChildren<Renderer>();
            for(int i=1;i<Sections;i++)meshes[i]=body.Parts[i-1].GetComponentsInChildren<Renderer>();
        }
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
        void Measure()
        {
            for(int i=0;i<Sections;i++)
            {
                var bounds=meshes[i][0].bounds;
                for(int j=1;j<meshes[i].Length;j++)bounds.Encapsulate(meshes[i][j].bounds);
                centers[i]=Flat(bounds.center);
                radii[i]=new Vector2(bounds.extents.x,bounds.extents.z).magnitude+hull+.8f;
            }
        }
        float Clearance(Vector3 p)
        {float best=float.PositiveInfinity;for(int i=0;i<Sections;i++)best=Mathf.Min(best,(p-centers[i]).magnitude-radii[i]);return best;}
        bool Clear(Vector3 a,Vector3 b)
        {
            var d=b-a;float length=d.sqrMagnitude;
            for(int i=0;i<Sections;i++)
            {
                var away=a-centers[i];float radius=radii[i];
                // Permit an outward escape if a moving body has already
                // entered the safety margin, never a path deeper through it.
                if(away.sqrMagnitude<radius*radius)
                {if(Vector3.Dot(d,away)<0)return false;continue;}
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
            avoiding=detour;brake=Clearance(position)<3.5f;
            var result=detour||adjustedGoal?waypoint:goal;result.y=height;return result;
        }
        void Plan(Vector3 start,Vector3 goal)
        {
            nodes[0]=start;nodes[1]=goal;valid[0]=true;valid[1]=Clearance(goal)>=0&&CanFire(goal);adjustedGoal=!valid[1];
            for(int i=0;i<Sections;i++)for(int j=0;j<Directions;j++)
            {
                int n=2+i*Directions+j;float angle=j*Mathf.PI*2/Directions;
                nodes[n]=centers[i]+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*(radii[i]+1.1f);
                valid[n]=Clearance(nodes[n])>=0;
            }
            // A chosen segment's flank can lie inside another bend. Use the
            // nearest clear perimeter point instead of crossing that anatomy.
            if(!valid[1])
            {
                float nearest=float.PositiveInfinity;
                for(int n=2;n<Capacity;n++)if(valid[n]&&(nodes[n]-goal).sqrMagnitude<nearest&&CanFire(nodes[n]))
                {nearest=(nodes[n]-goal).sqrMagnitude;nodes[1]=nodes[n];valid[1]=true;}
            }
            for(int i=0;i<Capacity;i++){cost[i]=float.PositiveInfinity;previous[i]=-1;visited[i]=false;}
            cost[0]=0;
            for(int pass=0;pass<Capacity;pass++)
            {
                int current=-1;float cheapest=float.PositiveInfinity;
                for(int i=0;i<Capacity;i++)if(valid[i]&&!visited[i]&&cost[i]<cheapest){current=i;cheapest=cost[i];}
                if(current<0||current==1)break;visited[current]=true;
                for(int next=1;next<Capacity;next++)
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
            for(int n=2;n<Capacity;n++)if(valid[n]&&Clear(start,nodes[n]))
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
