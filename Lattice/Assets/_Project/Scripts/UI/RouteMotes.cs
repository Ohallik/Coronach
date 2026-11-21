using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>Sela's chart moves: a mote of light runs along each lit route, from the place the party came from to the place it led to, so the routes read as living
    /// things and not as ruled lines. It keeps the menu's clock, not the game's, which is stopped while the menu is up; and where a mote stands comes from that clock
    /// alone, so a page redrawn under it (every press redraws the page) carries on from where it was.</summary>
    public sealed class RouteMotes:MonoBehaviour
    {
        /// <summary>How long one crossing takes.</summary>
        public const float Seconds=3.4f;
        readonly List<(Image mote,Vector2 from,Vector2 to,float lead)> runs=new();
        public void Add(Image mote,Vector2 from,Vector2 to){runs.Add((mote,from,to,runs.Count*.37f));Place(runs.Count-1);}
        void Update(){for(int i=0;i<runs.Count;i++)Place(i);}
        void Place(int index)
        {
            var (mote,from,to,lead)=runs[index];if(mote==null)return;
            float along=Mathf.Repeat(Time.unscaledTime/Seconds+lead,1);
            mote.rectTransform.anchoredPosition=Vector2.Lerp(from,to,along);
            // It rises out of one lantern and sinks into the next.
            var colour=mote.color;colour.a=Mathf.Sin(along*Mathf.PI)*.9f;mote.color=colour;
        }
    }
}
