using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Compact Warden's riot shield, drawn here: its body was rigged with the shield cut away (Meshy's rigger refused a box gripped at the hand; tools/c17_intake.py). A tall slab of the
    /// Compact's white with a cyan stripe, held before its off arm while its guard is up (the guarded front the plate arc marks), dropped to hang at its side while the guard is down (after a
    /// shield drive, broken, or giving ground), and gone as it stands aside. Made by the Guardian that gives ground (<see cref="EnemyGuardian"/>); it stands where the off hand is each frame,
    /// so it follows the body's own animation without being parented to a bone whose axes nobody knows.
    /// </summary>
    public sealed class RiotShield:MonoBehaviour
    {
        /// <summary>The slab: wide, tall, thick, in metres; and how far before the hand it is held.</summary>
        public const float Wide=.66f,Tall=1.1f,Thick=.07f,Before=.32f;
        static readonly Color White=new(.9f,.92f,.95f,1),Stripe=new(.45f,1.3f,1.5f,1);
        EnemyGuardian guardian;Transform hand,slab;float lowered;
        /// <summary>Raised before it now (its guard up): what a check reads.</summary>
        public bool Raised=>lowered<.5f;
        public static RiotShield On(Transform body,EnemyGuardian guardian)
        {
            var shield=body.gameObject.AddComponent<RiotShield>();shield.guardian=guardian;return shield;
        }
        void Start()
        {
            var animator=GetComponentInChildren<Animator>();
            if(animator!=null&&animator.isHuman)hand=animator.GetBoneTransform(HumanBodyBones.LeftHand)??animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            // Its own body's material, re-coloured: the same shader and keywords the body was built with, so nothing here is a variant the build strips.
            Material made=null;
            foreach(var view in GetComponentsInChildren<Renderer>())if(view.sharedMaterial!=null&&view.sharedMaterial.HasProperty("_BaseColor")){made=new Material(view.sharedMaterial);break;}
            slab=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;slab.name="Riot shield";Destroy(slab.GetComponent<Collider>());
            slab.SetParent(transform,false);slab.localScale=new Vector3(Wide,Tall,Thick);
            var face=slab.GetComponent<Renderer>();face.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            if(made!=null){if(made.HasProperty("_BaseMap"))made.SetTexture("_BaseMap",Texture2D.whiteTexture);made.SetColor("_BaseColor",White);face.sharedMaterial=made;}
            var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;stripe.name="Riot shield stripe";Destroy(stripe.GetComponent<Collider>());
            stripe.SetParent(slab,false);stripe.localPosition=new Vector3(0,.18f,.6f);stripe.localScale=new Vector3(.9f,.07f,.3f);
            var glow=stripe.GetComponent<Renderer>();glow.sharedMaterial=ActorFactory.Energy(Stripe);glow.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Place();
        }
        void LateUpdate()=>Place();
        void Place()
        {
            if(slab==null)return;
            var health=guardian!=null?GetComponent<Health>():null;
            if(health!=null&&!health.Alive){slab.gameObject.SetActive(false);return;}
            bool up=guardian==null||guardian.Guarding;
            lowered=Mathf.MoveTowards(lowered,up?0:1,Time.deltaTime*4);
            var forward=transform.forward;forward.y=0;forward=forward.sqrMagnitude>.01f?forward.normalized:Vector3.forward;var right=Vector3.Cross(Vector3.up,forward);
            // Raised: square before the off side at chest height. Down: hanging at the hand, its face turned out and its foot toward the floor.
            var grip=hand!=null?hand.position:transform.position+Vector3.up*1.1f-right*.35f;
            var raisedAt=transform.position+Vector3.up*1.15f-right*.22f+forward*Before;var downAt=grip-right*.12f+Vector3.down*.15f;
            slab.position=Vector3.Lerp(raisedAt,downAt,lowered);
            slab.rotation=Quaternion.Slerp(Quaternion.LookRotation(forward),Quaternion.LookRotation(-right)*Quaternion.Euler(18,0,0),lowered);
        }
    }
}
