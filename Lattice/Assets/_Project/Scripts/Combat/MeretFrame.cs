using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Meret Venn's heavy frame as the player reads it (chapter 10, phase two: <c>BossController.MeretGantry.cs</c>): its two arms and the couplings at their shoulders, laid over the rig's own clip
    /// after the animator has posed it. An arm <b>reaching</b> is thrown out along its strike and down to the deck (a slam planted, a weld at full stretch); an arm whose coupling is open
    /// <b>hangs</b> dead from its shoulder. Each shoulder carries its coupling's ring of light: cyan while it holds, amber and breathing while its arm is overreached (the seat beside it is
    /// lit), gone once it is open. Nothing here decides anything: the boss says what each arm is doing.
    /// </summary>
    [DefaultExecutionOrder(650)]
    public sealed class MeretFrame:MonoBehaviour
    {
        public enum Arm{Raised,Reaching,Hanging}
        static readonly Color Holds=new(.45f,1.45f,1.6f,.95f),Offered=new(1.7f,.95f,.35f,.95f);
        Transform rightUpper,rightFore,leftUpper,leftFore;Renderer rightRing,leftRing;
        Arm right,left;Vector3 rightWay,leftWay;float rightWeight,leftWeight;bool rightOffered,leftOffered;
        public Transform RightShoulder=>rightUpper;
        public Transform LeftShoulder=>leftUpper;
        public bool Rigged=>rightUpper!=null&&leftUpper!=null;
        public Arm Right=>right;public Arm Left=>left;
        /// <summary>The rig's arms, and a coupling ring at each shoulder.</summary>
        public void Bind(Animator rig)
        {
            if(rig!=null&&rig.isHuman)
            {
                rightUpper=rig.GetBoneTransform(HumanBodyBones.RightUpperArm);rightFore=rig.GetBoneTransform(HumanBodyBones.RightLowerArm);
                leftUpper=rig.GetBoneTransform(HumanBodyBones.LeftUpperArm);leftFore=rig.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            }
            // The welder is her right arm, the press her left.
            rightRing=Ring("Welder coupling",rightUpper,rightFore);leftRing=Ring("Press coupling",leftUpper,leftFore);
        }
        /// <summary>A ring of light round the arm where it meets the frame, its axis along the arm.</summary>
        Renderer Ring(string name,Transform at,Transform toward)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=RingMesh;
            // A quarter of the way down the upper arm: at the joint itself it is inside the frame's shoulder plate.
            go.transform.SetParent(at!=null?at:transform,false);go.transform.localPosition=at!=null&&toward!=null?toward.localPosition*.3f:Vector3.up*2.4f;
            if(at!=null&&toward!=null&&toward.localPosition.sqrMagnitude>1e-6f)go.transform.localRotation=Quaternion.FromToRotation(Vector3.forward,toward.localPosition.normalized);
            // Sized in the world whatever the rig's scale: a ring round a 3.2 m frame's shoulder.
            float world=Mathf.Max(.0001f,go.transform.lossyScale.x);go.transform.localScale=Vector3.one*(1/world);
            var ring=go.GetComponent<Renderer>();ring.sharedMaterial=ActorFactory.Energy(Holds);ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
            return ring;
        }
        static Mesh ringMesh;
        /// <summary>A torus .5 m round and .09 thick, its axis along local +z.</summary>
        static Mesh RingMesh
        {
            get
            {
                if(ringMesh!=null)return ringMesh;
                const int round=28,tube=8;const float r=.5f,t=.09f;var vertices=new Vector3[(round+1)*(tube+1)];var normals=new Vector3[vertices.Length];var triangles=new int[round*tube*6];
                for(int i=0;i<=round;i++)for(int j=0;j<=tube;j++)
                {
                    float a=i*Mathf.PI*2/round,b=j*Mathf.PI*2/tube;var centre=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*r;
                    var normal=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));
                    vertices[i*(tube+1)+j]=centre+normal*t;normals[i*(tube+1)+j]=normal;
                }
                int n=0;for(int i=0;i<round;i++)for(int j=0;j<tube;j++)
                {
                    int a=i*(tube+1)+j,b=a+tube+1;
                    triangles[n++]=a;triangles[n++]=b;triangles[n++]=a+1;triangles[n++]=a+1;triangles[n++]=b;triangles[n++]=b+1;
                }
                ringMesh=new Mesh{name="Meret coupling ring",vertices=vertices,normals=normals,triangles=triangles};ringMesh.RecalculateBounds();
                return ringMesh;
            }
        }
        /// <summary>What an arm is doing now: raised (the clip's own), thrown out along <paramref name="way"/> (world, flat), or hanging dead; and whether its coupling is offered.</summary>
        public void Set(bool isRight,Arm state,Vector3 way=default,bool offered=false)
        {
            way.y=0;
            if(isRight){if(state!=right)rightWeight=0;right=state;if(way.sqrMagnitude>.01f)rightWay=way.normalized;rightOffered=offered;}
            else{if(state!=left)leftWeight=0;left=state;if(way.sqrMagnitude>.01f)leftWay=way.normalized;leftOffered=offered;}
        }
        void LateUpdate()
        {
            float dt=Time.deltaTime;
            rightWeight=Mathf.MoveTowards(rightWeight,right==Arm.Raised?0:1,dt/.18f);leftWeight=Mathf.MoveTowards(leftWeight,left==Arm.Raised?0:1,dt/.18f);
            Pose(rightUpper,rightFore,right,rightWay,rightWeight,1);Pose(leftUpper,leftFore,left,leftWay,leftWeight,-1);
            Look(rightRing,right,rightOffered);Look(leftRing,left,leftOffered);
        }
        void Pose(Transform upper,Transform fore,Arm state,Vector3 way,float weight,float side)
        {
            if(upper==null||fore==null||weight<=0||state==Arm.Raised)return;
            var along=fore.position-upper.position;if(along.sqrMagnitude<1e-6f)return;
            // Hanging: straight down and a little out from the frame's side. Reaching: out along the strike and down onto the deck.
            var want=state==Arm.Hanging?Vector3.down+transform.right*(side*.18f):(way+Vector3.down*.55f).normalized;
            upper.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(along,want),weight)*upper.rotation;
            if(state==Arm.Reaching)
            {
                // The forearm straightened along the same line: the arm at full stretch.
                var hand=fore.childCount>0?fore.GetChild(0):null;if(hand==null)return;var reach=hand.position-fore.position;if(reach.sqrMagnitude<1e-6f)return;
                fore.rotation=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(reach,want),weight)*fore.rotation;
            }
        }
        void Look(Renderer ring,Arm state,bool offered)
        {
            if(ring==null)return;ring.enabled=state!=Arm.Hanging;if(!ring.enabled)return;
            ring.sharedMaterial=ActorFactory.Energy(offered?Offered:Holds);
            var parent=ring.transform.parent;float world=parent!=null?Mathf.Max(.0001f,parent.lossyScale.x):1;
            ring.transform.localScale=Vector3.one*(1/world)*(offered?1+.15f*Mathf.Sin(Time.time*9):1);
        }
        /// <summary>The frame is gone from the fight: both rings out.</summary>
        public void Dark(){if(rightRing!=null)rightRing.enabled=false;if(leftRing!=null)leftRing.enabled=false;enabled=false;}
    }
}
