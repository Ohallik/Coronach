using UnityEngine;
namespace Lattice.Combat
{
    public interface IMotor{void Move(Vector2 input,bool boost,bool brake);void Dash(Vector3 direction,float distance);Vector3 Facing{get;}Vector3 Velocity{get;}}
}
