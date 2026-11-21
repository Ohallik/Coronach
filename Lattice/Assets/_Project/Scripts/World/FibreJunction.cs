using System;
using System.Collections;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A junction of the Skyfields: two fibres meet in a frayed collar on an island's rim and the next island hangs off the join, crooked and below the rim, across a gap. Seated
    /// (<see cref="seatFlag"/>), the fibre takes its weight and the island rises flush between the two rims; cut (<see cref="cutFlag"/>), the fibre snaps, the island drops away into the
    /// cloud with whatever was on it, and the freed fibre springs taut across the gap as a road of light. Either one crosses, by the same <see cref="deck"/>: a solid static span the
    /// bake walks (switched off while the island sags), with <see cref="shut"/> (rails across both rims' openings, and a carving obstacle over the span) standing until it is there.
    /// The hands are two <see cref="DiscoveryPoint"/>s the builder stands beside it, one setting each flag, and both go the moment either is worked; when either is set, so is
    /// <see cref="crossedFlag"/>. A save with either flag loads the junction as that left it, with nothing played. Logs <c>JUNCTION_SEATED flag</c>, <c>JUNCTION_CUT flag</c>.
    /// </summary>
    public sealed class FibreJunction:MonoBehaviour
    {
        public enum State{Sagging,Seated,Cut}
        public string seatFlag,cutFlag,crossedFlag;
        /// <summary>The island as it is drawn (its rock, its top, whatever stands on it), and its three poses in the world.</summary>
        public Transform island;
        public Vector3 sagAt,seatAt,fallAt;
        public Vector3 sagTurn,seatTurn,fallTurn;
        /// <summary>The way across, active once seated or cut: solid and static, so the bake (made with it on) knows it.</summary>
        public GameObject deck;
        /// <summary>What stands while the island sags: the rails across each rim's opening and the carving obstacle that keeps the paths off the span.</summary>
        public GameObject[] shut=Array.Empty<GameObject>();
        /// <summary>The two hands, gone once either is worked.</summary>
        public GameObject[] hands=Array.Empty<GameObject>();
        /// <summary>The fibre as it hangs, frayed and slack; the fibre seated and taut; the fibre cut and sprung into a road across the gap.</summary>
        public GameObject slack,taut,road;
        /// <summary>What rides on the island and goes into the cloud with it when it is cut (a garden bed, a chest), and what the cut leaves on the rims (spilt soil, a torn collar).</summary>
        public GameObject[] lost=Array.Empty<GameObject>(),wrecked=Array.Empty<GameObject>();
        /// <summary>The island's shade on the cloud under it: there while it hangs or is seated, gone with it when it is cut.</summary>
        public GameObject shade;
        /// <summary>Prompts on the island (a chest) that can't be reached until it is seated: out of reach (range 0) while it sags, <see cref="seatedRange"/> once seated.</summary>
        public InteractionPrompt[] seatedOnly=Array.Empty<InteractionPrompt>();
        public float seatedRange=2.6f;
        public float rise=3.5f,drop=2.4f;
        public string seatSound="root_groan",cutSound="explosionCrunch_000";
        public static event Action<FibreJunction,State> Worked;
        public State Now{get;private set;}
        public bool Moving{get;private set;}
        bool known;
        State Read()
        {
            var flags=GameServices.Current.Flags;
            return !string.IsNullOrEmpty(cutFlag)&&flags.GetBool(cutFlag)?State.Cut:!string.IsNullOrEmpty(seatFlag)&&flags.GetBool(seatFlag)?State.Seated:State.Sagging;
        }
        void Update()
        {
            if(GameServices.Current==null)return;
            var now=Read();
            if(!known){known=true;Show(now);if(now!=State.Sagging)Crossed();return;}
            if(now==Now||Moving||Now!=State.Sagging)return;
            StartCoroutine(now==State.Seated?Seat():Cut());
        }
        /// <summary>The state with nothing played: a load, or a fixture.</summary>
        void Show(State state)
        {
            Now=state;bool open=state!=State.Sagging;
            if(deck!=null)deck.SetActive(open);foreach(var s in shut)if(s!=null)s.SetActive(!open);foreach(var h in hands)if(h!=null)h.SetActive(!open);
            if(slack!=null)slack.SetActive(state==State.Sagging);if(taut!=null)taut.SetActive(state==State.Seated);if(road!=null)road.SetActive(state==State.Cut);
            foreach(var w in wrecked)if(w!=null)w.SetActive(state==State.Cut);
            if(shade!=null)shade.SetActive(state!=State.Cut);
            foreach(var p in seatedOnly)if(p!=null)p.range=state==State.Seated?seatedRange:0;
            if(island!=null)
            {
                island.gameObject.SetActive(state!=State.Cut);
                island.SetPositionAndRotation(state==State.Seated?seatAt:sagAt,Quaternion.Euler(state==State.Seated?seatTurn:sagTurn));
            }
            foreach(var l in lost)if(l!=null&&state==State.Cut)l.SetActive(false);
        }
        void Crossed(){if(!string.IsNullOrEmpty(crossedFlag)&&!GameServices.Current.Flags.GetBool(crossedFlag))GameServices.Current.Flags.SetBool(crossedFlag,true);}
        IEnumerator Seat()
        {
            Moving=true;Debug.Log("JUNCTION_SEATED "+seatFlag);
            foreach(var h in hands)if(h!=null)h.SetActive(false);
            if(slack!=null)slack.SetActive(false);if(taut!=null)taut.SetActive(true);
            AudioManager.PlayAt(seatSound,transform,seatAt+Vector3.up,.5f,AudioBus.SFX,30);
            var rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(rise*.8f,.05f);
            var from=island!=null?island.position:sagAt;var fromTurn=island!=null?island.rotation:Quaternion.Euler(sagTurn);var to=Quaternion.Euler(seatTurn);
            // The fibre takes the weight: slow to start, a little past the rim, and back.
            for(float t=0;t<rise;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;float s=Mathf.Clamp01(t/rise);float eased=1-Mathf.Pow(1-s,3)+.04f*Mathf.Sin(s*Mathf.PI)*(1-s);
                if(island!=null)island.SetPositionAndRotation(Vector3.LerpUnclamped(from,seatAt,eased),Quaternion.SlerpUnclamped(fromTurn,to,eased));
                yield return null;
            }
            Show(State.Seated);Crossed();
            CombatVfx.Burst(seatAt+Vector3.up*.4f,new Color(.96f,.92f,.7f,.6f),"shape");
            Moving=false;Worked?.Invoke(this,State.Seated);
        }
        IEnumerator Cut()
        {
            Moving=true;Debug.Log("JUNCTION_CUT "+cutFlag);
            foreach(var h in hands)if(h!=null)h.SetActive(false);
            if(slack!=null)slack.SetActive(false);
            AudioManager.PlayAt(cutSound,transform,transform.position+Vector3.up,.5f,AudioBus.SFX,30);
            CombatVfx.Burst(transform.position+Vector3.up*1.2f,new Color(1f,.95f,.75f,.8f),"hit");
            var rig=FindFirstObjectByType<CameraRig>();if(rig!=null){rig.Impulse(Vector3.down,CameraRig.MostShake,.3f);rig.Tremor(.6f,.08f);}
            foreach(var w in wrecked)if(w!=null)w.SetActive(true);
            // The freed fibre springs across at once: the road is there while the island is still falling.
            if(road!=null)road.SetActive(true);
            var from=island!=null?island.position:sagAt;var fromTurn=island!=null?island.rotation:Quaternion.Euler(sagTurn);var to=Quaternion.Euler(fallTurn);
            for(float t=0;t<drop;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;float s=Mathf.Clamp01(t/drop);
                // It falls as a dropped thing does: from rest, faster and faster, turning as one end goes first.
                if(island!=null)island.SetPositionAndRotation(Vector3.LerpUnclamped(from,fallAt,s*s),Quaternion.Slerp(fromTurn,to,s));
                yield return null;
            }
            Show(State.Cut);Crossed();
            Moving=false;Worked?.Invoke(this,State.Cut);
        }
    }
}
