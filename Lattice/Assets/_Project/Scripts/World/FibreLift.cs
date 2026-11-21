using System;
using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A lift node on Oriel's Skyfields: where a severed fibre of the old road still runs taut across a gap, weight follows it, and the fibre carries the party to the next island.
    /// The hand is this prompt (<see cref="InteractionPrompt.prompt"/> "Ride the lift node"), standing on the node; pressed, both heroes out are drawn onto the node, lifted a hand off
    /// the road and carried along <see cref="path"/> to its far end, eased like a fall and a catch, and set down side by side. Every lift is used both ways, so each end has its own
    /// (the path reversed). A lift's road is solid and walkable too (the builder's, under a ribbon of light), so the companion and the bake know the way; the long fibre has no road and
    /// is one way. Refused, as a warp is, while a boss fight is under way or an enemy is awake near the node. Logs <c>FIBRE_LIFT name</c> as it carries and <c>FIBRE_LIFT_DOWN name</c>
    /// as it sets down; the lead's bark is <see cref="situation"/>.
    /// </summary>
    public sealed class FibreLift:InteractionPrompt
    {
        /// <summary>The ride in the world, from this node (the first point) to the far one (the last), along the fibre.</summary>
        public Vector3[] path=Array.Empty<Vector3>();
        public float seconds=3;
        /// <summary>How high off the road the party is carried at the middle of the ride: a hand, on a short lift; on the long fibre, whatever the arc between its points is.</summary>
        public float lift=.45f;
        /// <summary>The party's word as the fibre takes them (barks.json "lift", three in turn).</summary>
        public string situation="lift";
        public string sound="forceField_000";
        public float volume=.35f;
        /// <summary>Something has just been carried: for a test, the long fibre's arrival talk, or anything else that wants the moment.</summary>
        public static event Action<FibreLift> Carried;
        public bool Riding{get;private set;}
        public override bool Available=>!Riding&&path!=null&&path.Length>1&&!BossController.AnyFightUnderway&&!EnemyBrain.AwakeNear(transform.position,EnemyBrain.ExitCalm);
        public override void Interact(){if(Available)StartCoroutine(Ride());}
        /// <summary>The fibre's length, and the point that far along it.</summary>
        public float Length{get{float total=0;for(int i=1;i<path.Length;i++)total+=Vector3.Distance(path[i-1],path[i]);return total;}}
        public Vector3 At(float metres,out Vector3 along)
        {
            along=path.Length>1?(path[1]-path[0]).normalized:Vector3.forward;
            for(int i=1;i<path.Length;i++)
            {
                float leg=Vector3.Distance(path[i-1],path[i]);along=leg>0?(path[i]-path[i-1])/leg:along;
                if(metres<=leg||i==path.Length-1)return Vector3.Lerp(path[i-1],path[i],leg>0?Mathf.Clamp01(metres/leg):1);
                metres-=leg;
            }
            return path.Length>0?path[^1]:transform.position;
        }
        IEnumerator Ride()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)yield break;
            var services=GameServices.Current;Riding=true;bool wasBlocked=services.Input.Blocked;services.Input.Blocked=true;
            var lead=party.Active;var bodies=(CombatActor[])party.members.Clone();var controllers=new CharacterController[bodies.Length];var from=new Vector3[bodies.Length];
            for(int i=0;i<bodies.Length;i++){controllers[i]=bodies[i].GetComponent<CharacterController>();if(controllers[i]!=null)controllers[i].enabled=false;from[i]=bodies[i].transform.position;(bodies[i].motor as GroundMotor)?.Halt();}
            Debug.Log("FIBRE_LIFT "+name);
            BarkService.Play(lead.character,situation,true);
            AudioManager.PlayAt(sound,transform,path[0]+Vector3.up,volume,AudioBus.SFX,40);
            CombatVfx.Burst(path[0]+Vector3.up*.6f,new Color(.96f,.92f,.7f,.7f),"shape");
            float total=Length;
            // Each hero's place on the ride: the lead at the front, whoever else a stride and a half behind and a little aside, as two people step onto a road together.
            Vector3 Place(int i,float metres,out Vector3 along)
            {
                bool first=bodies[i]==lead;var on=At(Mathf.Clamp(metres-(first?0:1.6f),0,total),out along);
                var side=Vector3.Cross(Vector3.up,along).normalized;return first?on:on+side*.9f;
            }
            // Drawn onto the node first, so nobody is carried from where they stood.
            for(float t=0;t<.35f;)
            {
                if(!GameTime.Paused)t+=Time.unscaledDeltaTime;
                for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){var to=Place(i,0,out var along);bodies[i].transform.position=Vector3.Lerp(from[i],to,Mathf.SmoothStep(0,1,t/.35f));Face(bodies[i].transform,along);}
                yield return null;
            }
            // Then along the fibre: slow off the node, quick in the middle, caught at the far end. Weight follows the fibre, so the ride is a fall that is slowed at the bottom.
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.unscaledDeltaTime;
                float s=Mathf.Clamp01(t/seconds);float eased=s*s*(3-2*s);float metres=eased*total;float up=lift*Mathf.Sin(Mathf.PI*s);
                for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null){var at=Place(i,metres,out var along);bodies[i].transform.position=at+Vector3.up*up;Face(bodies[i].transform,along);}
                yield return null;
            }
            // Set down side by side at the far node, the companion to the lead's right.
            var end=At(total,out var last);var right=Vector3.Cross(Vector3.up,last).normalized;
            for(int i=0;i<bodies.Length;i++)if(bodies[i]!=null)bodies[i].transform.position=end+(bodies[i]==lead?Vector3.zero:right*1.8f)+Vector3.up*.05f;
            Physics.SyncTransforms();
            for(int i=0;i<bodies.Length;i++){if(controllers[i]!=null)controllers[i].enabled=true;if(bodies[i]!=null)(bodies[i].motor as GroundMotor)?.Halt();}
            CombatVfx.Burst(end+Vector3.up*.6f,new Color(.96f,.92f,.7f,.6f),"shape");
            services.Input.Blocked=wasBlocked;Riding=false;Debug.Log("FIBRE_LIFT_DOWN "+name);
            Carried?.Invoke(this);
        }
        static void Face(Transform body,Vector3 along){along.y=0;if(along.sqrMagnitude>.001f)body.rotation=Quaternion.LookRotation(along);}
        protected override void OnDisable()
        {
            base.OnDisable();
            // A ride cut off by a load gives the party back their feet and their hands.
            if(!Riding)return;Riding=false;
            if(GameServices.Current!=null)GameServices.Current.Input.Blocked=false;
            if(PartyController.Current!=null)foreach(var body in PartyController.Current.members){var cc=body!=null?body.GetComponent<CharacterController>():null;if(cc!=null)cc.enabled=true;}
        }
    }
}
