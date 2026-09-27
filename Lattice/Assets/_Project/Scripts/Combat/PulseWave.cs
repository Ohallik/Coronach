using System.Collections.Generic;
using Lattice.Core;
using UnityEngine;

namespace Lattice.Combat
{
    public sealed class PulseWave:MonoBehaviour
    {
        DamagePacket packet;GroundRing ring;float age;
        readonly HashSet<Health> victims=new();
        public float Radius{get;private set;}
        public static PulseWave Release(Vector3 position,DamagePacket packet)
        {
            var go=new GameObject("Pulse wave",typeof(PulseWave));go.transform.position=position;
            var wave=go.GetComponent<PulseWave>();wave.packet=packet;
            wave.ring=go.AddComponent<GroundRing>();wave.ring.Initialize(5);wave.ring.Draw(0,Color.clear);
            return wave;
        }
        void Update()
        {
            if(GameTime.Paused)return;
            age+=Time.deltaTime;Radius=5*Mathf.Clamp01(age/.28f);
            var origin=transform.position+Vector3.up*.8f;
            ring.Draw(Radius,new Color(1.5f,.75f,.2f,Mathf.Clamp01((.4f-age)/.12f)));
            foreach(var health in Health.All.ToArray())
            {
                if(health==null||!health.Alive||health==packet.source||packet.source!=null&&health.friendly==packet.source.friendly||victims.Contains(health))continue;
                var offset=health.transform.position-transform.position;
                if(Mathf.Abs(offset.y)>2.5f)continue;offset.y=0;
                if(offset.sqrMagnitude>Radius*Radius||!CombatCover.Clear(origin,health.transform.position+Vector3.up*.75f))continue;
                victims.Add(health);health.Receive(packet);
            }
            if(age>=.4f)Destroy(gameObject);
        }
    }
}
