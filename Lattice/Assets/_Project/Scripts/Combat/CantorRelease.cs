using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // Combat resolution frees the route animal. It leaves at full size instead
    // of inheriting the generic corpse roll and shrink. DefeatPresentation owns
    // the clock, collision shutdown and bounded removal.
    public sealed class CantorRelease : MonoBehaviour
    {
        public const float Duration=3.6f;
        Transform head;
        Transform[] parts;
        Vector3[] localPositions,worldPositions;
        Quaternion[] localRotations;
        Vector3 origin,headPosition,firstControl,secondControl,destination;
        Quaternion heading,headRotation;

        public void Begin(Transform visual,IReadOnlyList<Transform> body)
        {
            head=visual;origin=transform.position;heading=transform.rotation;
            headPosition=head.localPosition;headRotation=head.localRotation;
            parts=new Transform[body.Count];localPositions=new Vector3[body.Count];
            localRotations=new Quaternion[body.Count];worldPositions=new Vector3[body.Count];
            for(int i=0;i<body.Count;i++)
            {
                parts[i]=body[i];localPositions[i]=body[i].localPosition;
                localRotations[i]=body[i].localRotation;worldPositions[i]=body[i].position;
            }
            // The side control gives a reverse-facing animal a curved turn
            // without a zero tangent/pivot in isolated encounter fixtures.
            var forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
            firstControl=origin+forward*8+Vector3.up;
            secondControl=origin+Vector3.forward*38+Vector3.Cross(Vector3.up,forward)*15+Vector3.up*3;
            destination=origin+Vector3.forward*72+Vector3.up*6;
            if(ZoneController.Current!=null&&ZoneController.Current.definition.id=="Gullet_Tunnel")
            {
                // Converge on the real exit, not a parallel line at the lethal
                // X coordinate: the wide coil narrows to a 20 m valve. Leave
                // enough room beyond the end for the entire trailing body.
                firstControl.x=Mathf.Clamp(firstControl.x,GulletProfile.LeftEdge(firstControl.z)+4,GulletProfile.RightEdge(firstControl.z)-4);
                secondControl=new Vector3(GulletProfile.Center(GulletProfile.ExitZ-15),origin.y+3,GulletProfile.ExitZ-15);
                destination=new Vector3(GulletProfile.Center(GulletProfile.End),origin.y+6,GulletProfile.End+30);
            }
        }
        public void Advance(float elapsed)
        {
            if(elapsed<=.35f)return;
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.35f)/(Duration-.35f))),u=1-t;
            transform.position=u*u*u*origin+3*u*u*t*firstControl+3*u*t*t*secondControl+t*t*t*destination;
            var tangent=3*u*u*(firstControl-origin)+6*u*t*(secondControl-firstControl)+3*t*t*(destination-secondControl);
            tangent.y=0;
            if(tangent.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(tangent);
            // Cached world points are independent of the moving parent. Each
            // segment follows its predecessor at the original 2.2 m spacing.
            var previous=transform.position+Vector3.up*SerpentSegments.CenterHeight;
            for(int i=0;i<parts.Length;i++)
            {
                if(parts[i]==null)continue;
                var direction=previous-worldPositions[i];
                direction=direction.sqrMagnitude>.001f?direction.normalized:transform.forward;
                worldPositions[i]=previous-direction*2.2f;
                parts[i].SetPositionAndRotation(worldPositions[i],Quaternion.LookRotation(direction));
                previous=worldPositions[i];
            }
        }
        public void Restore()
        {
            transform.SetPositionAndRotation(origin,heading);
            head.SetLocalPositionAndRotation(headPosition,headRotation);
            for(int i=0;i<parts.Length;i++)
                if(parts[i]!=null)parts[i].SetLocalPositionAndRotation(localPositions[i],localRotations[i]);
        }
    }
}
