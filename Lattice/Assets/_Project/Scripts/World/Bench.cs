using System;
using Lattice.Core;
namespace Lattice.World
{
    public sealed class Bench:InteractionPrompt
    {
        public static event Action Requested;
        /// <summary>The attack button also interacts: a bench beside a live fight paused the game mid-swing.</summary>
        public override bool Available=>!Lattice.Combat.BossController.AnyFightUnderway&&!Lattice.Combat.EnemyBrain.AwakeNear(transform.position,Lattice.Combat.EnemyBrain.InteractionCalm);
        public override void Interact()=>Requested?.Invoke();
    }
}
