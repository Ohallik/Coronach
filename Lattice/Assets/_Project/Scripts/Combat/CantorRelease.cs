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
        public bool KeepInEncounterFrame {get;private set;}
        public bool Departing {get;private set;}
        Transform head;
        Transform[] parts;
        Vector3[] localPositions,worldPositions;
        Quaternion[] localRotations;
        Vector3 origin,headPosition,firstControl,secondControl,destination;
        Vector3 turnCenter,turnOffset,travelOrigin;
        float turnAngle,turnDuration,age;
        Quaternion heading,headRotation;

        public void Begin(Transform visual,IReadOnlyList<Transform> body)
        {
            age=0;Departing=true;KeepInEncounterFrame=true;
            head=visual;origin=transform.position;heading=transform.rotation;
            headPosition=head.localPosition;headRotation=head.localRotation;
            parts=new Transform[body.Count];localPositions=new Vector3[body.Count];
            localRotations=new Quaternion[body.Count];worldPositions=new Vector3[body.Count];
            for(int i=0;i<body.Count;i++)
            {
                parts[i]=body[i];localPositions[i]=body[i].localPosition;
                localRotations[i]=body[i].localRotation;worldPositions[i]=body[i].position;
            }
            var forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
            bool gullet=ZoneController.Current!=null&&ZoneController.Current.definition.id=="Gullet_Tunnel";
            turnAngle=Mathf.DeltaAngle(Mathf.Atan2(forward.x,forward.z)*Mathf.Rad2Deg,0);
            // A collinear reverse-facing Bezier has a zero tangent and flips
            // 180 degrees in one frame. First turn along a four-metre arc. At
            // the ambiguous half turn, choose the inside of the broad coil.
            if(gullet&&Mathf.Abs(turnAngle)>179.9f)
                turnAngle=origin.x>=GulletProfile.Center(origin.z)?180:-180;
            turnDuration=1.1f*Mathf.Abs(turnAngle)/180;
            turnOffset=Vector3.Cross(Vector3.up,forward)*Mathf.Sign(turnAngle)*4;
            turnCenter=origin+turnOffset;
            travelOrigin=turnCenter-Quaternion.AngleAxis(turnAngle,Vector3.up)*turnOffset;
            firstControl=travelOrigin+Vector3.forward*12+Vector3.up;
            secondControl=travelOrigin+Vector3.forward*38+Vector3.up*3;
            destination=origin+Vector3.forward*72+Vector3.up*6;
            if(gullet)
            {
                // Converge on the real exit, not a parallel line at the lethal
                // X coordinate: the wide coil narrows to a 20 m valve. Leave
                // enough room beyond the end for the entire trailing body.
                secondControl=new Vector3(GulletProfile.Center(GulletProfile.ExitZ-15),origin.y+3,GulletProfile.ExitZ-15);
                destination=new Vector3(GulletProfile.Center(GulletProfile.End),origin.y+6,GulletProfile.End+30);
            }
        }
        // Guidance samples the authored departure without moving the animal.
        // The evaluated presentation clock also holds this forecast on pause.
        public Vector3 PredictPosition(float lead)
        {
            if(!Departing)return transform.position;
            float elapsed=Mathf.Clamp(age+lead,0,Duration);
            if(elapsed<=.35f)return origin;
            if(elapsed<.35f+turnDuration)
            {
                float turn=Mathf.SmoothStep(0,1,(elapsed-.35f)/turnDuration);
                return turnCenter-Quaternion.AngleAxis(turnAngle*turn,Vector3.up)*turnOffset;
            }
            float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.35f-turnDuration)/(Duration-.35f-turnDuration))),u=1-t;
            return u*u*u*travelOrigin+3*u*u*t*firstControl+3*u*t*t*secondControl+t*t*t*destination;
        }
        public void Advance(float elapsed)
        {
            // Give the first turn room to read, then let the animal leave the
            // ordinary view before bounded removal. The owner passes game time.
            age=elapsed;KeepInEncounterFrame=elapsed<1.5f;
            if(elapsed<=.35f)return;
            if(elapsed<.35f+turnDuration)
            {
                float turn=Mathf.SmoothStep(0,1,(elapsed-.35f)/turnDuration);
                var rotation=Quaternion.AngleAxis(turnAngle*turn,Vector3.up);
                transform.SetPositionAndRotation(turnCenter-rotation*turnOffset,rotation*heading);
            }
            else
            {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-.35f-turnDuration)/(Duration-.35f-turnDuration))),u=1-t;
                transform.position=u*u*u*travelOrigin+3*u*u*t*firstControl+3*u*t*t*secondControl+t*t*t*destination;
                var tangent=3*u*u*(firstControl-travelOrigin)+6*u*t*(secondControl-firstControl)+3*t*t*(destination-secondControl);
                tangent.y=0;
                if(tangent.sqrMagnitude>.001f)transform.rotation=Quaternion.LookRotation(tangent);
            }
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
            Departing=false;KeepInEncounterFrame=false;
            transform.SetPositionAndRotation(origin,heading);
            head.SetLocalPositionAndRotation(headPosition,headRotation);
            for(int i=0;i<parts.Length;i++)
                if(parts[i]!=null)parts[i].SetLocalPositionAndRotation(localPositions[i],localRotations[i]);
        }
    }
}
