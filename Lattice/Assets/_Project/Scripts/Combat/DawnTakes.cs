using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The dawn takes what it reaches (chapter 9's Terminator; <see cref="Dawn"/>): any ordinary enemy the line has passed by <see cref="Dawn.Takes"/> flares white-gold where it stands and
    /// falls there, in sight, the way the day would take anything out on that ground. It pays nothing (the tag a mine that went off by itself carries: no victory, so no XP and no loot)
    /// and logs <c>DAWN_TAKES &lt;id&gt;</c>. A body under ground (an ambusher hidden, which can't be reached) comes up first, since a hide behind the line is one it flees; a boss is never
    /// taken. Made by <see cref="Dawn.Register"/> and gone with <see cref="Dawn.Unregister"/>: nothing a builder places.
    /// </summary>
    public sealed class DawnTakes:MonoBehaviour
    {
        static readonly Color Flare=new(2.2f,1.8f,1.2f,1);
        readonly List<Health> taking=new();
        void Update()
        {
            if(!Dawn.Here||GameTime.CombatHeld)return;
            taking.Clear();
            foreach(var body in Health.All)
            {
                if(body==null||body.friendly||!body.Alive||!body.Damageable||!body.TryGetComponent<EnemyBrain>(out var brain)||brain.definition==null||brain.definition.boss)continue;
                if(Dawn.Reached(body.transform.position))taking.Add(body);
            }
            foreach(var body in taking)Take(body);
        }
        static void Take(Health body)
        {
            var at=body.transform.position;
            CombatVfx.Burst(at+Vector3.up,Flare,"kill");
            AudioManager.PlayAt("forceField_000",null,at+Vector3.up,.22f,AudioBus.SFX,90,.06f,.55f);
            Debug.Log("DAWN_TAKES "+body.id);
            // The place's, not anyone's: no source. Four times its bar, so no resistance or plate keeps it standing.
            body.Receive(new DamagePacket{amount=body.maximum*4,type=DamageType.Plasma,breakPower=0,weight=HitWeight.Heavy,tag=EnemyBrain.SelfDetonation});
        }
    }
}
