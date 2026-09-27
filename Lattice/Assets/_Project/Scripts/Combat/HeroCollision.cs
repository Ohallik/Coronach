using UnityEngine;
namespace Lattice.Combat
{
    // Flight encloses the generated geometry through any visual bank/roll.
    // Keep collision stable while the visible hull rotates inside it.
    public sealed class HeroCollision:MonoBehaviour
    {
        CharacterController controller;CapsuleCollider body;
        float groundRadius,groundHeight,groundSkin,groundStep;Vector3 groundCenter;bool flying;
        public static float HullRadius(string hero)=>hero=="Taren"?1.78f:1.69f;
        public const float ArrivalSpacing=3.7f;
        void Awake()
        {
            controller=GetComponent<CharacterController>();body=GetComponent<CapsuleCollider>();
            groundRadius=controller.radius;groundHeight=controller.height;groundCenter=controller.center;
            groundSkin=controller.skinWidth;groundStep=controller.stepOffset;
        }
        public void SetFlight(bool value)
        {
            if(value==flying)return;flying=value;
            float radius=value?HullRadius(GetComponent<CombatActor>().character):groundRadius;
            controller.stepOffset=value?0:groundStep;
            controller.radius=radius;controller.height=value?radius*2:groundHeight;
            controller.center=value?Vector3.up*.8f:groundCenter;controller.skinWidth=value?.005f:groundSkin;
            body.radius=radius;body.height=controller.height;body.center=controller.center;
        }
    }
}
