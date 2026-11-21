using System;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.World
{
    /// <summary>
    /// Someone who walks with the party without being in it: the Skyfields' gardener, twelve paces behind from the landing, and Ishen, at the party's heels from the landing to the
    /// record. It goes by <see cref="stages"/>: the latest whose <see cref="Stage.when"/> holds is where it is. A stage that <see cref="Stage.follows"/> keeps <see cref="gap"/> metres of
    /// path behind the lead, stops when the party stops and walks on when they do, and holds still while anything is awake within <see cref="calm"/> of it or of the lead (never in a
    /// fight); one that doesn't goes to its <see cref="Stage.point"/> and waits there facing <see cref="Stage.faces"/>, and one that <see cref="Stage.leads"/> goes there in front of the
    /// party, waiting for them whenever they fall <see cref="leadWait"/> behind and never turning round to look. Its feet follow the baked paths (the roads of the lifts are in them).
    /// Out of the camera's view and far off its place, it is put there, as a guide is. At load it stands where its stage puts it, so nobody sees it walk a road twice.
    /// Its light (<see cref="lamps"/>, by property block, and <see cref="lampLight"/>) is each stage's <see cref="Stage.glow"/>: the gardeners' low and steady light is a request.
    /// An <see cref="Npc"/> on the same object (Ishen) can be talked to wherever it is. Logs <c>KEEPS_PACE name stage</c> as a stage begins.
    /// </summary>
    public sealed class KeepsPace:MonoBehaviour
    {
        [Serializable] public struct Stage
        {
            /// <summary>When this stage begins: a flag, or several as <c>a|b</c> (any of them) or <c>a&amp;b</c> (all of them). Empty is where it starts.</summary>
            public string when;
            public bool follows;
            public Vector3 point,faces;
            public bool leads;
            /// <summary>Its light in this stage, as a share of its own: 1 is bright and level (at rest), about .35 low and steady (asking). Zero leaves it as it is.</summary>
            public float glow;
        }
        public Stage[] stages=Array.Empty<Stage>();
        /// <summary>Metres of path kept behind the lead (twelve paces is about nine), how fast it walks, and how fast it hurries when it has fallen well behind.</summary>
        public float gap=9,walk=3.2f,hurry=6.6f,turnSpeed=300;
        /// <summary>Anything awake this near it or the lead and it holds where it is.</summary>
        public float calm=14;
        /// <summary>Leading, it waits while the lead is further behind it than this.</summary>
        public float leadWait=14;
        /// <summary>Further than this from its place (by path, or with no path at all) and out of view, it is put there.</summary>
        public float tooFar=40;
        public string goneFlag;
        public Renderer[] lamps=Array.Empty<Renderer>();
        public Light lampLight;
        public Color lampColour=new(1,.86f,.6f);
        public int Current{get;private set;}=-1;
        public bool Holding{get;private set;}
        public float PathToLead{get;private set;}=float.PositiveInfinity;
        NavMeshPath path;Vector3[] corners=Array.Empty<Vector3>();int corner;float nextPath,lampFull=-1;bool placed;MaterialPropertyBlock block;
        static bool Has(string when)
        {
            if(string.IsNullOrEmpty(when))return true;var flags=GameServices.Current.Flags;
            if(when.Contains("&")){foreach(var one in when.Split('&'))if(one.Length>0&&!flags.GetBool(one))return false;return true;}
            foreach(var one in when.Split('|'))if(one.Length>0&&flags.GetBool(one))return true;
            return false;
        }
        int Reached(){int last=-1;for(int i=0;i<stages.Length;i++)if(Has(stages[i].when))last=i;return last;}
        void Awake(){path=new NavMeshPath();block=new MaterialPropertyBlock();if(lampLight!=null)lampFull=lampLight.intensity;}
        static Transform Lead=>PartyController.Current!=null&&PartyController.Current.Active!=null?PartyController.Current.Active.transform:null;
        void Update()
        {
            var services=GameServices.Current;if(services==null||stages.Length==0)return;
            if(!string.IsNullOrEmpty(goneFlag)&&services.Flags.GetBool(goneFlag)){gameObject.SetActive(false);return;}
            int now=Reached();
            if(now!=Current){Current=now;corners=Array.Empty<Vector3>();nextPath=0;if(placed)Debug.Log("KEEPS_PACE "+name+" "+now);Light(now);}
            if(now<0)return;
            var stage=stages[now];var lead=Lead;
            if(!placed)
            {
                if(stage.follows&&lead==null)return;
                placed=true;Put(stage.follows?Behind(lead):stage.point);if(!stage.follows)Face(stage.faces-transform.position,true);return;
            }
            if(GameTime.Paused||services.Input.Blocked)return;
            Vector3 goal=stage.follows?(lead!=null?lead.position:transform.position):stage.point;
            if(Time.time>=nextPath)
            {
                nextPath=Time.time+.35f;
                corners=Route(transform.position,goal,out float length);corner=1;
                if(lead!=null){var toLead=Route(transform.position,lead.position,out float lengthToLead);PathToLead=toLead.Length>1?lengthToLead:float.PositiveInfinity;}
                // Far from its place, by path or with none, and nobody looking at it: put it there. Following, its place is behind a lead who is always in view, so only
                // where it stands now has to be out of view (and Behind prefers a spot out of view too); going to a point, the point must be out of view as well.
                bool far=corners.Length<2?Vector3.Distance(transform.position,goal)>3:length>tooFar;
                if(far&&!Seen(transform.position)&&(stage.follows||!Seen(goal))){Put(stage.follows?Behind(lead):stage.point);corners=Array.Empty<Vector3>();return;}
            }
            Holding=false;
            float remaining=Remaining();
            if(stage.follows)
            {
                // Never in a fight: anything awake near it or the lead and it stands where it is.
                if(EnemyBrain.AwakeNear(transform.position,calm)||lead!=null&&EnemyBrain.AwakeNear(lead.position,calm)){Holding=true;Face(goal-transform.position,false);return;}
                if(remaining<=gap){Face(goal-transform.position,false);return;}
                Walk(remaining-gap,remaining>gap+8?hurry:walk);
                return;
            }
            if(stage.leads&&lead!=null&&PathToLead>leadWait&&remaining>.3f){Holding=true;return;}
            if(remaining<=.25f){Face(stage.faces-transform.position,false);return;}
            Walk(remaining,remaining>16&&!stage.leads?hurry:walk);
        }
        Vector3[] Route(Vector3 from,Vector3 to,out float length)
        {
            length=float.PositiveInfinity;
            if(!NavMesh.SamplePosition(from,out var a,2,NavMesh.AllAreas)||!NavMesh.SamplePosition(to,out var b,2.5f,NavMesh.AllAreas))return Array.Empty<Vector3>();
            if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return Array.Empty<Vector3>();
            var found=path.corners;length=0;for(int i=1;i<found.Length;i++)length+=Vector3.Distance(found[i-1],found[i]);return found;
        }
        float Remaining()
        {
            if(corners.Length<2)return 0;
            float total=Flat(corners[Mathf.Min(corner,corners.Length-1)]-transform.position).magnitude;
            for(int i=corner+1;i<corners.Length;i++)total+=Vector3.Distance(corners[i-1],corners[i]);
            return total;
        }
        /// <summary>Along the corners for this much path at most, at this pace, its feet on the paths' height.</summary>
        void Walk(float most,float pace)
        {
            float step=Mathf.Min(most,pace*Time.deltaTime);
            while(step>0&&corner<corners.Length)
            {
                var to=corners[corner];var flat=Flat(to-transform.position);float d=flat.magnitude;
                if(d<.05f){corner++;continue;}
                float move=Mathf.Min(step,d);var at=transform.position+flat/d*move;
                at.y=Mathf.MoveTowards(transform.position.y,to.y,Mathf.Max(move*1.2f,Mathf.Abs(to.y-transform.position.y)*move/Mathf.Max(d,.01f)));
                if(NavMesh.SamplePosition(at,out var on,.6f,NavMesh.AllAreas))at.y=on.position.y;
                transform.position=at;Face(flat,false);step-=move;if(move>=d)corner++;
            }
        }
        /// <summary>Where it stands to keep pace with a lead it has just been put beside: on the paths, a gap behind them, or beside them where there is no floor behind.</summary>
        Vector3 Behind(Transform lead)
        {
            if(lead==null)return transform.position;
            Vector3? seen=null;
            foreach(var back in new[]{-lead.forward*gap,-lead.forward*(gap+4),-lead.forward*gap*.5f,lead.right*2.5f,-lead.right*2.5f})
                if(NavMesh.SamplePosition(lead.position+back,out var on,1.5f,NavMesh.AllAreas)&&Route(on.position,lead.position,out float length).Length>1&&length<gap*1.8f+4)
                {if(!Seen(on.position))return on.position;seen??=on.position;}
            return seen??lead.position+lead.right*2;
        }
        /// <summary>Exactly where it is put: a stage's point is on the ground already, and the bake leaves a hole round a named person's post, so the nearest path is a stride off it.</summary>
        void Put(Vector3 at)=>transform.position=at;
        void Face(Vector3 look,bool snap)
        {
            look.y=0;if(look.sqrMagnitude<.01f)return;var goal=Quaternion.LookRotation(look);
            transform.rotation=snap?goal:Quaternion.RotateTowards(transform.rotation,goal,turnSpeed*Time.deltaTime);
        }
        static bool Seen(Vector3 point)
        {
            var camera=Camera.main;if(camera==null)return false;var v=camera.WorldToViewportPoint(point+Vector3.up);
            return v.z>0&&v.x>-.1f&&v.x<1.1f&&v.y>-.1f&&v.y<1.1f;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        void Light(int stage)
        {
            if(stage<0)return;float glow=stages[stage].glow;if(glow<=0)return;
            foreach(var lamp in lamps){if(lamp==null)continue;lamp.GetPropertyBlock(block);block.SetColor("_EmissionColor",lampColour*glow*1.6f);lamp.SetPropertyBlock(block);}
            if(lampLight!=null&&lampFull>0)lampLight.intensity=lampFull*glow;
        }
    }
}
