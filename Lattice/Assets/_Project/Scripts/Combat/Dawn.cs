using System;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The dawn line, for a body that has to read it (chapter 9's Terminator: a tidally locked world's lethal day coming on behind the party; docs/maps/Terminator.md, "Run fights contract").
    /// The line itself is the world's (whatever moves it, draws its light and burns a hero it reaches); combat can't see that assembly, so the zone registers two answers here while it is
    /// enabled: how far a point stands <b>ahead</b> of the line (in the twilight, metres; below nought the dawn has it) and whether it is in <b>shade</b> (a rock's, a tread's, a gantry's long
    /// shadow). Anywhere without one there is no dawn and nothing here does anything.
    /// <para>What reads it: every ambusher hunts the <b>warm band</b> just ahead of the line and out of shade (<see cref="Warm"/>; the Heat-scavenger), and flees shade; and the dawn takes any
    /// ordinary enemy it passes (<see cref="DawnTakes"/>, made by <see cref="Register"/>): it flares where it stands and falls, in sight, and pays nothing.</para>
    /// </summary>
    public static class Dawn
    {
        static Func<Vector3,float> ahead;static Func<Vector3,bool> shade;static UnityEngine.Object owner;static DawnTakes taker;
        /// <summary>The warm band: from this far ahead of the line to this far, out of shade. Its middle is where a hunter makes its hide.</summary>
        public const float BandFrom=1.5f,BandTo=14,BandMiddle=7;
        /// <summary>How far past a body the line has to be before the dawn takes it: a body standing on the line's edge isn't taken by the width of a paw.</summary>
        public const float Takes=1.5f;
        /// <summary>The zone's line, while it is enabled. With <paramref name="takes"/> false the zone burns what the line reaches itself, and nothing here does (a rule two places raced
        /// would kill a body twice); a burn of its own should carry <c>EnemyBrain.SelfDetonation</c>, so the dawn's kill pays nothing either way.</summary>
        public static void Register(UnityEngine.Object by,Func<Vector3,float> ahead,Func<Vector3,bool> shade,bool takes=true)
        {
            owner=by;Dawn.ahead=ahead;Dawn.shade=shade;
            if(takes&&taker==null)taker=new GameObject("The dawn takes what it reaches").AddComponent<DawnTakes>();
            else if(!takes&&taker!=null){UnityEngine.Object.Destroy(taker.gameObject);taker=null;}
        }
        public static void Unregister(UnityEngine.Object by)
        {
            if(owner!=by)return;owner=null;ahead=null;shade=null;
            if(taker!=null)UnityEngine.Object.Destroy(taker.gameObject);taker=null;
        }
        /// <summary>A dawn is registered here.</summary>
        public static bool Here=>owner!=null&&ahead!=null;
        /// <summary>Metres a point stands ahead of the line; infinitely far with no dawn.</summary>
        public static float Ahead(Vector3 point)=>Here?ahead(point):float.PositiveInfinity;
        public static bool Shade(Vector3 point)=>Here&&shade!=null&&shade(point);
        /// <summary>In the warm band and out of shade: where a hunter of the dawn line goes under.</summary>
        public static bool Warm(Vector3 point){if(!Here)return false;float a=ahead(point);return a>=BandFrom&&a<=BandTo&&!Shade(point);}
        /// <summary>The line has passed the point by more than <see cref="Takes"/>.</summary>
        public static bool Reached(Vector3 point)=>Here&&ahead(point)<-Takes;
        /// <summary>The way the line comes on at a point, on the floor: the way "ahead" grows.</summary>
        public static Vector3 Forward(Vector3 point)
        {
            if(!Here)return Vector3.forward;
            float dx=ahead(point+Vector3.right)-ahead(point-Vector3.right),dz=ahead(point+Vector3.forward)-ahead(point-Vector3.forward);
            var way=new Vector3(dx,0,dz);return way.sqrMagnitude>1e-6f?way.normalized:Vector3.forward;
        }
        /// <summary>A warm point across from this one: carried along the line's way to the band's middle, then, if that is in shade, a little to either side. False if there is none
        /// within 8 m to either side.</summary>
        public static bool Into(Vector3 near,out Vector3 warm)
        {
            warm=near;if(!Here)return false;
            var forward=Forward(near);var across=Vector3.Cross(Vector3.up,forward);
            var at=near+forward*(BandMiddle-ahead(near));
            for(int i=0;i<=8;i++)
            {
                float side=(i&1)==0?i/2*2:-(i/2+1)*2;var p=at+across*side;
                if(Warm(p)){warm=p;return true;}
            }
            return false;
        }
    }
}
