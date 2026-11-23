using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the two shield relays that feed Meret Venn's dome in the Continuance's flight berth (chapter 10, phase one: <c>BossController.MeretShip.cs</c>): a mast on the hull's back
    /// in front of each shelter wing, its head at the plane, a cyan beam from it to her dome. While her dome is up a lit relay can be locked and shot (a shot at it is never given back:
    /// it stands well outside the dome); shot through, it goes dark, her dome falls for a window and the relay recharges. So the fight under her dome is the read and the shot at a
    /// relay, not holding fire. Logs <c>MERET_RELAY_DOWN</c>, <c>MERET_RELAY_LIT</c>.
    /// <para>What a player sees (the look pass of November 21: in the whole-game capture the relay was a 1.25 m ball on a thin grey pole, less than half the 2.8 m its shots are judged
    /// against, grey on the grey deck): the room's own mast (the Continuance stands both, built in its plating, <see cref="MastName"/>), and on it the relay's head drawn as big as its
    /// target, a ring round it at the plane the flight camera reads from above, and the beam. Lit, all three are the shield's cyan; shot through, the head goes dark and the ring burns
    /// amber until it is lit again. Where no room stands a mast (a test yard), the relay stands a plain one of its own. Nothing of it is solid.</para>
    /// </summary>
    public sealed class MeretRelay:MonoBehaviour
    {
        public static readonly List<MeretRelay> All=new();
        /// <summary>Whose dome it feeds, which it is (0 the west, 1 the east), and its own small bar (not hers: shooting it takes nothing off her bar).</summary>
        public BossController boss;public int index;public Health health;
        /// <summary>It is lit, takes blows and can be locked: her dome stands, and it isn't recharging.</summary>
        public bool Lit{get;private set;}
        public float RechargedAt{get;set;}=-1;
        internal SphereCollider shape;
        /// <summary>What is drawn of it: its head, its beam to her dome, its ring at the plane.</summary>
        public Renderer head;public LineRenderer beam,ring;
        /// <summary>The drawn head's diameter, lit and dark, against the target's 2.8 m (<c>BossController.ShipTune.RelayTarget</c>, its radius): what is drawn is what a shot is judged against.</summary>
        public const float HeadLit=2.8f,HeadDark=2.1f,RingRadius=2.4f;
        static readonly Color LitTone=new(.45f,1.45f,1.65f,.95f),DarkTone=new(.16f,.2f,.24f,.95f),RingLit=new(.5f,1.5f,1.7f,.9f),RingDark=new(1.5f,.7f,.25f,.75f);
        /// <summary>The name the room gives the mast each relay stands on (0 the west, 1 the east): the Continuance's builder stands them, the fight finds them.</summary>
        public static string MastName(int index)=>index==0?"Her west shield relay's mast":"Her east shield relay's mast";
        public void Light(bool lit,Vector3 dome)
        {
            if(health!=null){health.Damageable=lit;}
            if(shape!=null)shape.enabled=lit;
            if(head!=null){head.sharedMaterial=ActorFactory.Energy(lit?LitTone:DarkTone);head.transform.localScale=Vector3.one*(lit?HeadLit*(1+.04f*Mathf.Sin(Time.time*6+index)):HeadDark);}
            if(beam!=null){beam.enabled=lit;if(lit){beam.SetPosition(0,transform.position);beam.SetPosition(1,dome);}}
            if(ring!=null){ring.sharedMaterial=ActorFactory.Energy(lit?RingLit:RingDark);float w=lit?.34f:.22f;ring.startWidth=ring.endWidth=w;}
            if(lit!=Lit){Lit=lit;Debug.Log((lit?"MERET_RELAY_LIT ":"MERET_RELAY_DARK ")+index);}
        }
        /// <summary>Draws a relay on <paramref name="go"/> (stood at its head's middle, at the plane): the head, the ring round it, the beam, and a plain mast down to the hull when the room
        /// stands none of its own (<paramref name="mastDown"/> metres to the hull, 0 for none). Used by the fight and by the zone's stills, so both draw the same relay.</summary>
        public static MeretRelay Draw(GameObject go,int index,float mastDown)
        {
            var relay=go.GetComponent<MeretRelay>();if(relay==null)relay=go.AddComponent<MeretRelay>();relay.index=index;
            static void Drop(Object o){if(Application.isPlaying)Object.Destroy(o);else Object.DestroyImmediate(o);}
            var headGo=GameObject.CreatePrimitive(PrimitiveType.Sphere);headGo.name="Relay head";Drop(headGo.GetComponent<Collider>());headGo.transform.SetParent(go.transform,false);
            var headR=headGo.GetComponent<Renderer>();headR.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;headR.receiveShadows=false;relay.head=headR;
            if(mastDown>0)
            {
                var mast=GameObject.CreatePrimitive(PrimitiveType.Cylinder);mast.name="Relay mast";Drop(mast.GetComponent<Collider>());mast.transform.SetParent(go.transform,false);
                mast.transform.localPosition=Vector3.down*mastDown*.5f;mast.transform.localScale=new Vector3(.6f,mastDown*.5f,.6f);mast.GetComponent<Renderer>().sharedMaterial=ActorFactory.Energy(new Color(.5f,.55f,.6f,1));
            }
            // The ring lies flat at the plane: drawn in its own XY and turned onto the world's XZ, facing up (TransformZ).
            var ringGo=new GameObject("Relay ring");ringGo.transform.SetParent(go.transform,false);ringGo.transform.localRotation=Quaternion.Euler(90,0,0);
            var line=ringGo.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;const int sides=32;line.positionCount=sides;
            for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides;line.SetPosition(k,new Vector3(Mathf.Sin(a)*RingRadius,Mathf.Cos(a)*RingRadius,0));}
            line.alignment=LineAlignment.TransformZ;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;relay.ring=line;
            var beam=go.GetComponent<LineRenderer>();if(beam==null)beam=go.AddComponent<LineRenderer>();beam.positionCount=2;beam.useWorldSpace=true;beam.startWidth=.4f;beam.endWidth=.7f;
            beam.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;relay.beam=beam;
            return relay;
        }
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
    }
}
