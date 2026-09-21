using System.Collections.Generic;
using UnityEngine;
namespace Lattice.World
{
    public sealed class TrafficShip:MonoBehaviour
    {
        static readonly List<TrafficShip> Ships=new();
        public TrafficLane lane;
        public float phase;
        float length;
        void OnEnable()=>Ships.Add(this);
        void OnDisable()=>Ships.Remove(this);
        void Start(){length=lane.Length;}
        void Update()
        {
            if(lane==null||length<=0)return;
            float pace=1;
            foreach(var ship in Ships)if(ship!=this&&ship.lane==lane){float gap=Mathf.Repeat(ship.phase-phase,1)*length;if(gap<7)pace=Mathf.Min(pace,gap/7);}
            phase=Mathf.Repeat(phase+lane.speed*pace*Time.deltaTime/length,1);
            var tangent=lane.Tangent(phase);transform.position=lane.Position(phase);if(tangent.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(tangent);
        }
    }
}
