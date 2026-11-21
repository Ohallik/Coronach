using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    /// <summary>
    /// A crate standing loose on a fighting floor (chapter 5's freight hall): cover by the ordinary rule while it stands (a solid box with no integrity, which shots and marked strikes stop
    /// at and bodies walk round: it carves the paths), and something a fight can move. Dessik Varn's magnet pulse drags every crate in its reach along the floor to the suit, and in his
    /// second half he throws them back out onto marked circles, where each lands as cover again. While a crate is on the move it is neither cover nor an obstacle (<see cref="Moving"/>),
    /// so it never shoves a hero aside or leaves a hole in the paths where it was. A builder stands one on an object with a solid <c>BoxCollider</c> and the crate's drawn piece under it;
    /// nothing here is in the save, and a reload puts every crate back where it was built. The room it needs: docs/maps/ManyHandsHull.md, "Freight hall contract".
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class LooseCargo:MonoBehaviour
    {
        public static readonly List<LooseCargo> All=new();
        BoxCollider box;NavMeshObstacle hole;
        /// <summary>Being dragged or in the air: not cover, not in the paths, and not to be taken by another move.</summary>
        public bool Moving{get;private set;}
        /// <summary>Half its width on the floor, the larger way: how far from its middle a lane it slides down must reach, and how close it stops against what it meets.</summary>
        public float Half{get{if(box==null)box=GetComponent<BoxCollider>();var size=Vector3.Scale(box.size,transform.lossyScale);return Mathf.Max(Mathf.Abs(size.x),Mathf.Abs(size.z))*.5f;}}
        void Awake()
        {
            box=GetComponent<BoxCollider>();box.isTrigger=false;
            hole=GetComponent<NavMeshObstacle>();
            if(hole==null){hole=gameObject.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Box;hole.size=box.size;hole.center=box.center;}
            hole.carving=true;hole.carveOnlyStationary=true;
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        /// <summary>The middle of its box in the world, whether or not it is solid just now: where a slide is swept from, at the crate's own height and not the floor's.</summary>
        public Vector3 Middle{get{if(box==null)box=GetComponent<BoxCollider>();return transform.TransformPoint(box.center);}}
        /// <summary>How far its pivot stood over the floor it was taken from: a built crate's pivot is at its foot, a made one's at its middle.</summary>
        float rest;
        /// <summary>Taken off the floor by a move: from now until <see cref="Place"/> it is the move's.</summary>
        public void Lift(){Moving=true;Solid(false);rest=transform.position.y-CombatCover.Ground(Middle).y;}
        /// <summary>Carried by a move, a frame at a time (its pivot).</summary>
        public void Hold(Vector3 at)=>transform.position=at;
        /// <summary>Where its pivot stands set down at a place on the floor: the floor under that place, and its pivot as high over it as it was where it was taken from.</summary>
        public Vector3 Resting(Vector3 spot){var floor=CombatCover.Ground(new Vector3(spot.x,Mathf.Max(spot.y,transform.position.y)+2,spot.z));return new Vector3(spot.x,floor.y+rest,spot.z);}
        /// <summary>Set down on the floor at a place: cover again, and in the paths again.</summary>
        public void Place(Vector3 spot){transform.position=Resting(spot);Solid(true);Moving=false;Physics.SyncTransforms();}
        void Solid(bool solid){if(box==null)box=GetComponent<BoxCollider>();box.enabled=solid;if(hole!=null)hole.enabled=solid;}
        /// <summary>The crate standing nearest a point within a distance on the plane, not on the move, or none.</summary>
        public static LooseCargo Nearest(Vector3 point,float within)
        {
            LooseCargo best=null;float least=within*within;
            foreach(var crate in All)
            {
                if(crate==null||crate.Moving||!crate.isActiveAndEnabled)continue;
                var offset=crate.transform.position-point;offset.y=0;if(offset.sqrMagnitude>=least)continue;
                least=offset.sqrMagnitude;best=crate;
            }
            return best;
        }
        /// <summary>A plain crate made where a fight needs one and the floor has none to give (the proving yard): a 1.6 m box in freight paint, standing on the floor point given.
        /// A built hall's crates are the builder's. Moves keep a crate's height as they carry it, so a crate's floor is level with where it is set down.</summary>
        public static LooseCargo Make(Vector3 floor)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Loose crate (made)";go.transform.position=floor+Vector3.up*.7f;go.transform.localScale=new Vector3(1.6f,1.4f,1.6f);
            var view=go.GetComponent<Renderer>();if(view!=null&&view.material!=null)view.material.color=new Color(.62f,.48f,.26f);
            return go.AddComponent<LooseCargo>();
        }
    }
}
