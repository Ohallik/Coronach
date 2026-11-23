using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Bryn Asta's tug, the <i>Welcome Back</i>, at the edge of the Broken Ring (chapter 9): on the tug's drawn root, at the flight plane, where it waits through the Debris Cutter's fight. Once
    /// <c>bossdown.&lt;boss&gt;</c> is set it comes in, puts a line on the disabled body (dead in space and whole, its crew aboard) and tows it off to <see cref="away"/>, where both are gone. A save with
    /// the flag loads with both gone. With no body to take (it went some other way) it simply leaves. The brief's ending: "Disabled, it is towed off by Bryn Asta's tug; its crew live."
    /// Logs <c>TOW_COMES &lt;boss&gt;</c>, <c>TOW_TAKES &lt;boss&gt;</c>, <c>TOW_GONE &lt;boss&gt;</c>. The Ring's contract: docs/maps/BrokenRing.md, "Cutter arena contract".
    /// <para>It never comes through a ship. Bryn's talk opens as it comes, so the party is held where the Cutter went dead, and the tug used to come in to the body's side toward
    /// <see cref="away"/>, which was where the party stood, and tow it off through them: its boom lay through Idra's tug and the Cutter was dragged through Taren's fighter (whole-game capture,
    /// November 20, s61/086 to 098). It now comes in <see cref="under"/> metres under the plane to a point past its berth on the body's side away from the ships (turned a little if a ship
    /// or a district is there), backs up to it rising as it comes, and tows the body straight away from them, going down as it goes, before it turns for <see cref="away"/>
    /// <see cref="deep"/> metres under everything.</para>
    /// </summary>
    public sealed class TowedOff:MonoBehaviour
    {
        /// <summary>The boss it takes, by EnemyDef id; where it tows it; how fast it flies; how long its line is behind it.</summary>
        public string boss="DebrisCutter";
        public Transform away;
        public float speed=12,lineLength=9;
        /// <summary>How far under the plane it comes in, and tows at once clear; how far it tows straight away from the ships first.</summary>
        public float under=8,deep=14,clearRun=20;
        enum Stage{Waiting,Coming,Towing,Gone}
        Stage stage;Transform taken;LineRenderer line;Vector3 waitedAt,side,legEnd;float plane;bool firstLeg,backing;
        public bool Towing=>stage==Stage.Towing;
        public bool Gone=>stage==Stage.Gone;
        bool Down=>GameServices.Current!=null&&GameServices.Current.Flags.GetBool("bossdown."+boss);
        void Start()
        {
            waitedAt=transform.position;plane=waitedAt.y;
            // Taken already: nothing of either is here.
            if(Down){stage=Stage.Gone;gameObject.SetActive(false);}
        }
        Transform Body()
        {
            foreach(var brain in FindObjectsByType<EnemyBrain>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                if(brain!=null&&brain.definition!=null&&brain.definition.id==boss)return brain.transform;
            return null;
        }
        Vector3 Away=>away!=null?away.position:waitedAt+Flat(transform.forward)*120;
        /// <summary>The way from the body it takes it: straight away from the ships near it (toward where it goes if none is), turned by a step at a time until its berth is clear of every
        /// ship by its line's length and outside every district.</summary>
        Vector3 Side()
        {
            var at=taken.position;var party=PartyController.Current;Vector3 sum=Vector3.zero;int near=0;
            if(party!=null)foreach(var member in party.members)if(member!=null&&member.gameObject.activeInHierarchy&&Flat(member.transform.position-at).magnitude<45){sum+=member.transform.position;near++;}
            var toAway=Flat(Away-at);var from=near>0?Flat(at-sum/near):toAway;
            if(from.sqrMagnitude<.01f)from=toAway.sqrMagnitude>.01f?toAway:Vector3.forward;from.Normalize();
            foreach(float turn in new[]{0f,30,-30,60,-60,90,-90,120,-120})
            {
                var way=Quaternion.Euler(0,turn,0)*from;var berth=at+way*lineLength;berth.y=plane;bool clear=!RingDistrict.Inside(berth,6);
                if(clear&&party!=null)foreach(var member in party.members)if(member!=null&&member.gameObject.activeInHierarchy&&Flat(member.transform.position-berth).magnitude<lineLength*.9f){clear=false;break;}
                if(clear)return way;
            }
            return from;
        }
        void Update()
        {
            if(GameTime.Paused||stage==Stage.Gone)return;
            float dt=Time.deltaTime;
            if(stage==Stage.Waiting)
            {
                if(!Down)return;
                taken=Body();if(taken!=null)side=Side();stage=Stage.Coming;Debug.Log("TOW_COMES "+boss);
            }
            if(stage==Stage.Coming)
            {
                if(taken==null){stage=Stage.Towing;legEnd=Away;legEnd.y=plane-deep;firstLeg=false;return;}
                // To its line's length off the body's side away from the ships, nose that way: under the plane to a point past its berth, then backing up to the berth, rising as it
                // comes, so it is never up at the plane under the body or beside a ship.
                var berth=taken.position+side*lineLength;berth.y=plane;var beyond=berth+side*8;
                var here=transform.position;
                if(!backing&&Flat(beyond-here).magnitude<1)backing=true;
                var target=backing?berth:beyond;var flatTo=Flat(target-here);float off=Flat(berth-here).magnitude;
                float wantY=backing?Mathf.Lerp(plane,plane-under,Mathf.InverseLerp(1,7,off)):plane-under;
                var flat=Vector3.MoveTowards(new Vector3(here.x,0,here.z),new Vector3(target.x,0,target.z),(backing?speed*.5f:speed)*dt);
                transform.position=new Vector3(flat.x,Mathf.MoveTowards(here.y,wantY,speed*.7f*dt),flat.z);
                var facing=backing||Flat(beyond-here).magnitude<10||flatTo.sqrMagnitude<.01f?side:flatTo.normalized;
                transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(facing),90*dt);
                if(off<.6f&&Mathf.Abs(transform.position.y-plane)<.3f)
                {
                    stage=Stage.Towing;Debug.Log("TOW_TAKES "+boss);transform.rotation=Quaternion.LookRotation(side);
                    legEnd=transform.position+side*clearRun;legEnd.y=plane-deep*.6f;firstLeg=true;
                    var go=new GameObject("Tow line");go.transform.SetParent(transform,false);line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;
                    line.startWidth=line.endWidth=.14f;line.sharedMaterial=ActorFactory.Energy(new Color(1.3f,1.05f,.6f,.9f));line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    AudioManager.PlayAt("impactMetal_000",transform,taken.position,.26f,AudioBus.SFX,60,.05f,.8f);
                }
                return;
            }
            // Towing: straight away from the ships and down, then on to away far under everything, the body behind on its line, turning after it.
            if(firstLeg&&(legEnd-transform.position).magnitude<1){firstLeg=false;legEnd=Away;legEnd.y=plane-deep;}
            var to=legEnd;var way=Flat(to-transform.position);
            transform.position=Vector3.MoveTowards(transform.position,to,speed*dt);
            if(way.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(way.normalized),60*dt);
            if(taken!=null)
            {
                var behind=transform.position-Flat(transform.forward)*lineLength;
                taken.position=Vector3.MoveTowards(taken.position,behind,speed*1.2f*dt);
                var look=Flat(transform.position-taken.position);if(look.sqrMagnitude>.01f)taken.rotation=Quaternion.RotateTowards(taken.rotation,Quaternion.LookRotation(look),40*dt);
                if(line!=null){line.SetPosition(0,transform.position);line.SetPosition(1,taken.position);}
            }
            if(!firstLeg&&Flat(Away-transform.position).magnitude<1)
            {
                stage=Stage.Gone;Debug.Log("TOW_GONE "+boss);
                if(taken!=null)Destroy(taken.gameObject);gameObject.SetActive(false);
            }
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
    }
}
