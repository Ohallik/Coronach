using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Rootmarket's arrival (chapter 3, staged as the first town's morning was, D195): growers are hauling a root island, Binna's spice garden, in to its mooring, and Ruva puts
    /// Taren on a rope before she learns his name. The conversation that says so starts on a crossing; this is the rope it ends with. Each press is one pull: the island comes a
    /// third of the way in, Ruva calls the count, and on the last pull it seats and the flag the terms wait for is set and saved. Until then Ruva and Binna answer with a line about
    /// the rope instead of their full conversations (a hold node marks nobody met), and the crew heave on their lines. A later load finds the island moored and the lines slack.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MooringHaul:InteractionPrompt
    {
        public string arrivalFlag="xylos.arrivalSeen",hauledFlag="xylos.ropeHauled",landedFlag="xylos.landed";
        public string caller="Ruva",bark="rope";
        public int pulls=3;
        /// <summary>What is hauled, where it lies before the first pull and where it seats.</summary>
        public Transform island;public Vector3 adrift,moored;
        /// <summary>How far it has turned from its mooring heading while adrift, in degrees.</summary>
        public float swing=24;
        /// <summary>Lines from the island's cleats (first point) to whoever holds them or to the post (second point); each is drawn taut every frame.</summary>
        public LineRenderer[] lines=System.Array.Empty<LineRenderer>();
        public Transform[] cleats=System.Array.Empty<Transform>(),hands=System.Array.Empty<Transform>();
        /// <summary>Who answers with a holding line until the island is in, and the line each gives.</summary>
        public Npc[] held=System.Array.Empty<Npc>();public string[] holdNodes=System.Array.Empty<string>();
        GameState owner;int pulled;float at,wanted;Quaternion heading;bool hauled;
        public override bool Available=>isActiveAndEnabled&&!hauled&&at>=wanted-.01f&&GameServices.Current!=null&&GameServices.Current.Flags.GetBool(arrivalFlag);
        /// <summary>How far in it is, 0 adrift to 1 moored.</summary>
        public float Progress=>at;
        void Start()
        {
            var services=GameServices.Current;if(services==null||island==null){enabled=false;return;}
            owner=services.State;heading=island.rotation;
            if(!string.IsNullOrEmpty(landedFlag))services.Flags.SetBool(landedFlag,true);
            hauled=services.Flags.GetBool(hauledFlag);at=wanted=hauled?1:0;pulled=hauled?pulls:0;
            Place();Hold();
        }
        void Update()
        {
            var services=GameServices.Current;if(services==null||!ReferenceEquals(owner,services.State))return;
            if(!GameTime.Paused&&at<wanted)
            {
                at=Mathf.MoveTowards(at,wanted,Time.deltaTime/(pulls*1.6f));
                if(at>=1&&!hauled)Seat(services);
            }
            Place();Hold();
        }
        public override void Interact()
        {
            if(!Available)return;
            pulled++;wanted=Mathf.Clamp01(pulled/(float)Mathf.Max(1,pulls));
            if(!string.IsNullOrEmpty(caller))BarkService.Play(caller,bark,true);
            AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.1f,AudioBus.SFX,30,.06f,.6f);
        }
        void Seat(GameServices services)
        {
            hauled=true;services.Flags.SetBool(hauledFlag,true);Debug.Log("ROPE_HAULED "+hauledFlag);
            AudioManager.PlayAt("doorOpen_000",transform,island.position,.18f,AudioBus.SFX,40);
            services.Saves.Save("autosave",services.State);
        }
        void Place()
        {
            float t=Mathf.SmoothStep(0,1,at);
            // Adrift it rides the water and swings on its lines; moored it only breathes.
            float ride=Mathf.Sin(Time.time*.9f)*Mathf.Lerp(.07f,.02f,t),sway=Mathf.Sin(Time.time*.47f)*Mathf.Lerp(5,0,t);
            island.SetPositionAndRotation(Vector3.Lerp(adrift,moored,t)+Vector3.up*ride,heading*Quaternion.Euler(0,swing*(1-t)+sway,0));
            for(int i=0;i<lines.Length&&i<cleats.Length&&i<hands.Length;i++)
            {
                if(lines[i]==null||cleats[i]==null||hands[i]==null)continue;
                Vector3 a=cleats[i].position,b=hands[i].position;int points=lines[i].positionCount;
                // Taut while there is hauling to do, hanging once she's seated.
                float sag=Mathf.Lerp(.05f,.5f,t)*Mathf.Min(1,Vector3.Distance(a,b)*.2f);
                for(int k=0;k<points;k++){float s=k/(float)(points-1);lines[i].SetPosition(k,Vector3.Lerp(a,b,s)+Vector3.down*sag*4*s*(1-s));}
            }
        }
        void Hold()
        {
            for(int i=0;i<held.Length;i++)if(held[i]!=null)held[i].holdNode=hauled?null:i<holdNodes.Length?holdNodes[i]:null;
        }
    }
}
