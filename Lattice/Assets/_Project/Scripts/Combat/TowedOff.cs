using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Bryn Asta's tug, the <i>Welcome Back</i>, at the edge of the Broken Ring (chapter 9): on the tug's drawn root, at the flight plane, where it waits through the Debris Cutter's fight. Once
    /// <c>bossdown.&lt;boss&gt;</c> is set it comes in, puts a line on the disabled body (dead in space and whole, its crew aboard) and tows it off to <see cref="away"/>, where both are gone. A save with
    /// the flag loads with both gone. With no body to take (it went some other way) it simply leaves. The brief's ending: "Disabled, it is towed off by Bryn Asta's tug; its crew live."
    /// Logs <c>TOW_COMES &lt;boss&gt;</c>, <c>TOW_TAKES &lt;boss&gt;</c>, <c>TOW_GONE &lt;boss&gt;</c>. The Ring's contract: docs/maps/BrokenRing.md, "Cutter arena contract".
    /// </summary>
    public sealed class TowedOff:MonoBehaviour
    {
        /// <summary>The boss it takes, by EnemyDef id; where it tows it; how fast it flies; how long its line is behind it.</summary>
        public string boss="DebrisCutter";
        public Transform away;
        public float speed=12,lineLength=9;
        enum Stage{Waiting,Coming,Towing,Gone}
        Stage stage;Transform taken;LineRenderer line;Vector3 waitedAt;
        public bool Towing=>stage==Stage.Towing;
        public bool Gone=>stage==Stage.Gone;
        bool Down=>GameServices.Current!=null&&GameServices.Current.Flags.GetBool("bossdown."+boss);
        void Start()
        {
            waitedAt=transform.position;
            // Taken already: nothing of either is here.
            if(Down){stage=Stage.Gone;gameObject.SetActive(false);}
        }
        Transform Body()
        {
            foreach(var brain in FindObjectsByType<EnemyBrain>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
                if(brain!=null&&brain.definition!=null&&brain.definition.id==boss)return brain.transform;
            return null;
        }
        void Update()
        {
            if(GameTime.Paused||stage==Stage.Gone)return;
            float dt=Time.deltaTime;
            if(stage==Stage.Waiting)
            {
                if(!Down)return;
                taken=Body();stage=Stage.Coming;Debug.Log("TOW_COMES "+boss);
            }
            if(stage==Stage.Coming)
            {
                if(taken==null){stage=Stage.Towing;return;}
                // Up to its line's length off the body's far side from where it goes, nose toward away.
                var toAway=Flat((away!=null?away.position:waitedAt)-taken.position);var heading=toAway.sqrMagnitude>.01f?toAway.normalized:Vector3.forward;
                var berth=taken.position+heading*lineLength;berth.y=transform.position.y;
                Fly(berth,heading,dt);
                if(Flat(berth-transform.position).magnitude<.6f)
                {
                    stage=Stage.Towing;Debug.Log("TOW_TAKES "+boss);
                    var go=new GameObject("Tow line");go.transform.SetParent(transform,false);line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=2;
                    line.startWidth=line.endWidth=.14f;line.sharedMaterial=ActorFactory.Energy(new Color(1.3f,1.05f,.6f,.9f));line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                    AudioManager.PlayAt("impactMetal_000",transform,taken.position,.26f,AudioBus.SFX,60,.05f,.8f);
                }
                return;
            }
            // Towing: off to away, the body behind on its line, turning after it.
            var to=away!=null?away.position:waitedAt+Flat(transform.forward)*120;to.y=transform.position.y;
            var way=Flat(to-transform.position);Fly(to,way.sqrMagnitude>.01f?way.normalized:transform.forward,dt);
            if(taken!=null)
            {
                var behind=transform.position-Flat(transform.forward)*lineLength;behind.y=taken.position.y;
                taken.position=Vector3.MoveTowards(taken.position,behind,speed*1.2f*dt);
                var look=Flat(transform.position-taken.position);if(look.sqrMagnitude>.01f)taken.rotation=Quaternion.RotateTowards(taken.rotation,Quaternion.LookRotation(look),40*dt);
                if(line!=null){line.SetPosition(0,transform.position);line.SetPosition(1,taken.position);}
            }
            if(way.magnitude<1)
            {
                stage=Stage.Gone;Debug.Log("TOW_GONE "+boss);
                if(taken!=null)Destroy(taken.gameObject);gameObject.SetActive(false);
            }
        }
        void Fly(Vector3 to,Vector3 heading,float dt)
        {
            transform.position=Vector3.MoveTowards(transform.position,to,speed*dt);
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(heading),60*dt);
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
    }
}
