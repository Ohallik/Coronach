using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>A rigid body's wind-up: it rears back through its tell and snaps forward into the strike, as a boss's own moves do, and a
    /// mine swells. Rigged bodies play their tell on the Attack clip instead (GeneratedAnimator). Runs after ProceduralMotion, whose
    /// skitter writes this same rotation every frame; a boss's moves and a body that's down each own the pose while they last.</summary>
    [DefaultExecutionOrder(650)]
    public sealed class EnemyTellPose:MonoBehaviour
    {
        const float RearBack=-12,FollowThrough=7,TurnRate=120,MineSwell=.22f;
        EnemyBrain brain;BossController boss;Transform visual;
        Quaternion rest;Vector3 restScale;float pitch,swell;bool posed,skitters;
        void Awake(){GetComponent<Health>().Died+=OnDown;}
        void OnDestroy(){if(TryGetComponent<Health>(out var health))health.Died-=OnDown;}
        void OnDown(Health _,DamagePacket __)
        {
            // A mine bursts from the size it has swollen to: back at rest for a frame, it would shrink as it blows.
            var scale=visual!=null?visual.localScale:Vector3.one;Settle();
            if(brain!=null&&brain.Mine&&visual!=null)visual.localScale=scale;
        }
        void Start()
        {
            brain=GetComponent<EnemyBrain>();boss=GetComponent<BossController>();
            var presentation=GetComponent<DefeatPresentation>();visual=presentation!=null?presentation.visual:null;
            // The Bellows has no rig and no pose: its breath is its animation (BossController swells it through a tell).
            if(brain==null||visual==null||GetComponentInChildren<GeneratedAnimator>()!=null||brain.definition.id=="BellowsBelow"){enabled=false;return;}
            rest=visual.localRotation;restScale=visual.localScale;
            skitters=visual.TryGetComponent<ProceduralMotion>(out var motion)&&motion.motion==ProceduralMotion.Motion.Skitter;
        }
        void LateUpdate()
        {
            if(GameTime.CombatHeld)return;
            if(!brain.Health.Alive||boss!=null&&(boss.Busy||boss.Burrowed)){Settle();return;}
            bool mine=brain.Mine;
            float goal=mine?0:brain.Telegraphing?RearBack*brain.TellProgress:brain.Striking?FollowThrough:0;
            pitch=Mathf.MoveTowards(pitch,goal,TurnRate*Time.deltaTime);
            swell=Mathf.MoveTowards(swell,mine?MineSwell*brain.TellProgress:0,2*Time.deltaTime);
            if(pitch==0&&swell==0){Settle();return;}
            posed=true;
            if(mine)visual.localScale=restScale*(1+swell);
            else visual.localRotation=(skitters?visual.localRotation:rest)*Quaternion.Euler(pitch,0,0);
        }
        /// <summary>Hands the body back as it was: called in the same frame a boss's move or a death takes it over.</summary>
        void Settle()
        {
            pitch=swell=0;if(!posed||visual==null)return;posed=false;
            visual.localRotation=rest;visual.localScale=restScale;
        }
    }
}
