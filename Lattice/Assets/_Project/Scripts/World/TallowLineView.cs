using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    // Local story interest, not a combat target. The marker keeps its original
    // motion while the actual assembly and nearby craft share readable space.
    public sealed class TallowLineView:MonoBehaviour
    {
        public TallowMarkerDrift drift;
        public Transform[] subjects;
        public float enterDistance=10,exitDistance=12;
        public Rect safeViewport=new Rect(.055f,.445f,.89f,.49f);
        CameraRig cameraRig;Renderer[] sceneMeshes;CombatActor[] partyMembers;
        Renderer[][] shipMeshes;GameState owner;bool holding;
        void Start()
        {
            cameraRig=Object.FindFirstObjectByType<CameraRig>();owner=GameServices.Current?.State;
            sceneMeshes=subjects.Where(t=>t!=null).SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).ToArray();
        }
        void Update()
        {
            // ArenaRuntime creates the rig in Start; scene Start order is not
            // guaranteed. Retry until the runtime camera has been created.
            if(cameraRig==null)cameraRig=Object.FindFirstObjectByType<CameraRig>();
            if(cameraRig==null||drift==null||drift.observation==null||GameServices.Current==null)return;
            var party=PartyController.Current;
            if(party==null||!party.Active.flight||!ReferenceEquals(owner,GameServices.Current.State)||(owner.flags.TryGetValue("tallow.lineObserved",out bool observed)&&observed))
            {Clear();return;}
            float distance=Vector3.Distance(party.Active.transform.position,drift.observation.position);
            if(distance>(holding?exitDistance:enterDistance)){Clear();return;}
            holding=true;
            if(partyMembers!=party.members)
            {
                partyMembers=party.members;shipMeshes=partyMembers.Select(h=>h!=null?h.GetComponent<FormController>().flight.GetComponentsInChildren<Renderer>(true):new Renderer[0]).ToArray();
            }
            var bounds=new Bounds(party.Active.transform.position,Vector3.zero);Include(ref bounds,sceneMeshes);
            for(int i=0;i<partyMembers.Length;i++)
                if(partyMembers[i]!=null&&partyMembers[i].flight&&partyMembers[i].gameObject.activeInHierarchy&&Vector3.Distance(partyMembers[i].transform.position,party.Active.transform.position)<=30)
                    Include(ref bounds,shipMeshes[i]);
            // Renew even during pause so the camera lease remains held; marker
            // and pose clocks already pause, and CameraRig freezes its easing.
            cameraRig.FrameSceneInterest(this,bounds,safeViewport);
        }
        static void Include(ref Bounds bounds,Renderer[] meshes)
        {foreach(var mesh in meshes)if(mesh!=null&&mesh.enabled&&mesh.gameObject.activeInHierarchy)bounds.Encapsulate(mesh.bounds);}
        void Clear(){holding=false;if(cameraRig!=null)cameraRig.FrameSceneInterest(this,null,safeViewport);}
        void OnDisable(){Clear();}
    }
}
