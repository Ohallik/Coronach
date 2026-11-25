using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// D407: a hero left inside a beaten boss's body is eased out of it. A small body that falls is thrown clear of him (DefeatPresentation.FallClear, D386); a boss's body stays
    /// where it ends (shut down, destroyed, or reeling before it leaves), and cannot be. A hero that a charge or his own lunge had carried into it stood inside it for the talk that
    /// followed: Taren's ship in the Warmaster's darkened skiff under the quest card and his own line (nov24-recon-h-52 s52/177 to 183). For a moment after the end, a hero of its
    /// own form inside its capsule on the plan (what stopped him while it lived) is slid out along the way from its middle through him, over <see cref="Over"/> s, by his own
    /// controller: a wall stops him, and on foot he only goes over floor at his own height, so never off a deck or a shelf; where that way is shut, the next way round. It is added
    /// to his own moving, never put in its place: the stick still moves him, and nothing holds him once he is clear.
    /// </summary>
    public sealed class WreckClearance:MonoBehaviour
    {
        /// <summary>How long after the end a hero found inside it begins to be eased (on the heroes' clock, which the kill's stop holds), how long one easing may run, the time the
        /// slide takes and its least pace, and how far past touching it he is left.</summary>
        public const float Window=1.5f,MostEase=1.5f,Over=.4f,LeastPace=3,Margin=.15f;
        /// <summary>On foot: how far above or below the body's root he may stand and still be in it (higher, he stands on it: the Foundry Saint's back), and the most the floor may
        /// fall away under the next step of the slide.</summary>
        const float SameFloor=1.2f,FlightLevel=3,MostDrop=.4f;
        static readonly float[] Ways={0,45,-45,90,-90,135,-135};
        CharacterController capsule;Health body;float began;
        readonly Dictionary<CombatActor,(float pace,float since)> easing=new();
        /// <summary>Begun on a boss's end (BossController), for a body with a capsule.</summary>
        public static void Begin(GameObject boss)
        {
            if(boss==null||!boss.TryGetComponent<CharacterController>(out _)||boss.GetComponent<WreckClearance>()!=null)return;
            boss.AddComponent<WreckClearance>();
        }
        void Awake(){capsule=GetComponent<CharacterController>();body=GetComponent<Health>();began=GameTime.Now;}
        void Update()
        {
            float now=GameTime.Now-began,dt=GameTime.Delta;
            if(now>Window+MostEase){Destroy(this);return;}
            var party=PartyController.Current;if(party==null||party.members==null||capsule==null||dt<=0)return;
            var middle=transform.TransformPoint(capsule.center);var scale=transform.lossyScale;float reach=capsule.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z));
            foreach(var hero in party.members)
            {
                if(hero==null||!hero.gameObject.activeInHierarchy||hero.Health==null||!hero.Health.Alive||!hero.TryGetComponent<CharacterController>(out var own)||!own.enabled)continue;
                if(body!=null&&EnemyBrain.OtherForm(hero,body))continue;
                var at=hero.transform.position;if(Mathf.Abs(at.y-transform.position.y)>(hero.flight?FlightLevel:SameFloor))continue;
                var gap=at-middle;gap.y=0;float apart=gap.magnitude,clear=reach+own.radius+Margin;
                bool known=easing.TryGetValue(hero,out var ease);
                if(apart>=clear)
                {
                    if(known){easing.Remove(hero);Debug.Log($"WRECK_CLEAR {(body!=null?body.id:name)} {hero.character} {apart:0.00} m");}
                    continue;
                }
                if(!known)
                {
                    if(now>Window)continue;
                    ease=(Mathf.Max(LeastPace,(clear-apart)/Over),now);easing[hero]=ease;
                    Debug.Log($"WRECK_EASE {(body!=null?body.id:name)} {hero.character} {apart:0.00} of {clear:0.00} m");
                }
                else if(now-ease.since>MostEase)continue;
                var way=apart>.05f?gap/apart:Level(-hero.transform.forward);
                float step=Mathf.Min(clear-apart,ease.pace*dt);
                if(!Open(hero,own,way,step,out var toward))continue;
                HeroCollision.ThroughParty(own);own.Move(toward*step);
            }
        }
        static Vector3 Level(Vector3 v){v.y=0;return v.sqrMagnitude>.0001f?v.normalized:Vector3.back;}
        /// <summary>The first way out, from straight out round to either side, that nothing solid shuts within the step and (on foot) that has floor at his height under it.</summary>
        bool Open(CombatActor hero,CharacterController own,Vector3 way,float step,out Vector3 toward)
        {
            var at=hero.transform.position;var centre=at+own.center;float r=own.radius*.9f,half=Mathf.Max(0,own.height*.5f-own.radius);
            var low=centre-Vector3.up*(half-(hero.flight?0:own.stepOffset*.5f));var high=centre+Vector3.up*half;
            foreach(float angle in Ways)
            {
                toward=Quaternion.Euler(0,angle,0)*way;bool shut=false;
                foreach(var hit in Physics.CapsuleCastAll(low,high,r,toward,step+.05f,~0,QueryTriggerInteraction.Ignore))
                {
                    if(hit.distance<=0||hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<CombatActor>()!=null)continue;
                    shut=true;break;
                }
                if(shut)continue;
                if(!hero.flight&&!Floored(at+toward*(step+own.radius*.5f),at.y))continue;
                return true;
            }
            toward=way;return false;
        }
        static bool Floored(Vector3 at,float height)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(at.x,height+1,at.z),Vector3.down,1+MostDrop+.05f,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.GetComponentInParent<CombatActor>()!=null||hit.collider.GetComponentInParent<EnemyBrain>()!=null)continue;
                if(hit.point.y>=height-MostDrop)return true;
            }
            return false;
        }
    }
}
