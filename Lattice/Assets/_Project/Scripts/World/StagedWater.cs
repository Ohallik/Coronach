using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Water that goes down in steps, one for each switch that is worked: the Drowned Waystation's flood, which pump one takes off the entry hall and pump two off the lower gallery's
    /// floor. One <see cref="WaterVolume"/> does the wading and the shore; this sets its level. Stage nought is the volume's own level; stage i is <see cref="levels"/>[i-1] and is reached
    /// once <see cref="flags"/>[i-1] is set (the highest stage whose flag is set wins, so a save loads at the stage it was left at, with nothing played).
    /// <see cref="blockers"/>[i] is what makes water deep at stage i and not at the next: it stands while the water is at stage i or above it, and goes once the move to stage i+1 has
    /// finished (each a holder of WaterKit.Deep's carving boxes, which is a Membrane, so whatever opens every seal to read the baked floor opens it too). Leave the volume's own
    /// drainFlag empty: two things moving one water is a fight. Logs <c>WATER_STAGE flag level</c> as the water starts down and <c>WATER_STAGED flag</c> when it is there.
    /// </summary>
    public sealed class StagedWater:MonoBehaviour
    {
        public WaterVolume water;
        public string[] flags=System.Array.Empty<string>();
        public float[] levels=System.Array.Empty<float>();
        public GameObject[] blockers=System.Array.Empty<GameObject>();
        /// <summary>How long a step takes to go down.</summary>
        public float seconds=3.5f;
        /// <summary>The stage the water is at or on its way to.</summary>
        public int Stage{get;private set;}=-1;
        float settleAt=-1;
        public float LevelOf(int stage)=>stage<=0||water==null?(water!=null?water.level:0):levels[Mathf.Min(stage,levels.Length)-1];
        int Wanted()
        {
            var services=GameServices.Current;if(services==null)return 0;
            for(int i=flags.Length;i>0;i--)if(!string.IsNullOrEmpty(flags[i-1])&&services.Flags.GetBool(flags[i-1]))return i;
            return 0;
        }
        void Show(int stage){for(int i=0;i<blockers.Length;i++)if(blockers[i]!=null&&blockers[i].activeSelf!=(i>=stage))blockers[i].SetActive(i>=stage);}
        void Update()
        {
            if(water==null||GameServices.Current==null)return;
            int wanted=Wanted();
            if(Stage<0){Stage=wanted;water.MoveTo(LevelOf(wanted),0);Show(wanted);return;}
            if(wanted>Stage)
            {
                Stage=wanted;water.MoveTo(LevelOf(wanted),seconds);settleAt=seconds;
                Debug.Log("WATER_STAGE "+flags[wanted-1]+" "+LevelOf(wanted).ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));
            }
            if(settleAt>0&&!GameTime.Paused)
            {
                settleAt-=Time.deltaTime;
                if(settleAt<=0){settleAt=-1;Show(Stage);Debug.Log("WATER_STAGED "+flags[Stage-1]);}
            }
        }
    }
}
