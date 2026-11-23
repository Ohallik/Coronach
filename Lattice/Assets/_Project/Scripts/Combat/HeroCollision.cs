using UnityEngine;
namespace Lattice.Combat
{
    // Flight encloses the generated geometry through any visual bank/roll.
    // Keep collision stable while the visible hull rotates inside it.
    public sealed class HeroCollision:MonoBehaviour
    {
        CharacterController controller;CapsuleCollider body;
        float groundRadius,groundHeight,groundSkin,groundStep;Vector3 groundCenter;bool flying;
        /// <summary>Each hull's measured reach from its root through any bank (FlightSocketTests). Idra's cutter reaches 2.16 m: in Sela's 1.69 half a metre of it flew
        /// outside her collision.</summary>
        public static float HullRadius(string hero)=>hero=="Taren"?1.78f:hero=="Idra"?2.17f:1.69f;
        public const float ArrivalSpacing=3.7f;
        /// <summary>How far apart two ships arrive: the usual spacing, or more for two hulls that wouldn't clear each other at it (Taren's and Idra's reach 3.95 m between
        /// them). Taren and Sela arrive exactly as they always did.</summary>
        public static float Apart(string a,string b)=>Mathf.Max(ArrivalSpacing,HullRadius(a)+HullRadius(b)+.2f);
        void Awake()
        {
            controller=GetComponent<CharacterController>();body=GetComponent<CapsuleCollider>();
            groundRadius=controller.radius;groundHeight=controller.height;groundCenter=controller.center;
            groundSkin=controller.skinWidth;groundStep=controller.stepOffset;
        }
        /// <summary>No hero is ever a solid to another (D354). A hero's hull, in flight and on foot, passes through the others as if they were not there, and everything else stays
        /// solid to all of them: nothing the partner does can stop, slow or turn the lead, and flying or walking into her is never a scrape. It replaces the evening of November 21's
        /// rules about when she gives way, each written for one narrow place and each followed by the next (a Picket leg, the Tollkeeper's gate motor, a regroup), and then the
        /// Steward's middle gate in the Orrery, where she sat in its mouth and the lead pressed against her hull through five seconds of green (nov22-whole s42c, three plays of three).
        /// Kept by each motor before it moves a hull, and by the party every frame, since a collider that is switched off (as a placing does) forgets what it ignored. Only for a
        /// hull that is one of the party's: a bare body on a motor (a fixture's) is solid to the heroes as anything else is.</summary>
        public static void ThroughParty(CharacterController hull)
        {
            var party=PartyController.Current;
            if(hull==null||party==null||party.members==null||!hull.enabled||!hull.gameObject.activeInHierarchy)return;
            bool member=false;foreach(var hero in party.members)if(hero!=null&&hero.gameObject==hull.gameObject){member=true;break;}
            if(!member)return;
            foreach(var hero in party.members)
            {
                if(hero==null||hero.gameObject==hull.gameObject||!hero.gameObject.activeInHierarchy||!hero.TryGetComponent<CharacterController>(out var other)||!other.enabled)continue;
                if(!Physics.GetIgnoreCollision(hull,other))Physics.IgnoreCollision(hull,other,true);
            }
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
