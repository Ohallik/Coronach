using Lattice.Combat;
using Lattice.Core;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class BarkPanel:MonoBehaviour
    {
        TMP_Text text;float until;Canvas canvas;PickupFeed feed;
        static readonly string NameColor=ColorUtility.ToHtmlStringRGB(UiKit.GoldColor);
        /// <summary>A line isn't worth hiding the party for, or what it's about: Orrin's box lay solid over the tender he was saying was free. So a lighter plate, which steps aside like the rest. At .66 it went a muddy tan on Sorrel's floor with its pale words losing out, and the slab showed through it in the Bellows' chamber.</summary>
        const float RestingPlate=.85f;
        /// <summary>Where the words rest. Under the vitals and the pickup feed's five lines, in a column of its own. Across the middle at y 285 it cut the ship in half, in the very band a fight flies in.</summary>
        static readonly Vector2 Rest=new(292,-575);
        /// <summary>A hero under the column, or what a line's about, and the plate slides up or down it to clear them: a plate that only faded left 'Taren: Just a little late' lying across Sela. How clear, how quick, how far over the hint bar it stops, how tall a ship stands on the screen for its width, and how wide a thing known by one point is taken to be.</summary>
        const float Air=14,SlidePerSecond=1100,FootRoom=112,ShipTall=.75f,ThingHalf=70;
        float slide,plateLow,plateHigh,plateLeft,plateRight;
        /// <summary>The stretches of the column the plate can't stand in this frame, as from and to of its slide.</summary>
        readonly float[] shut=new float[48];int shutCount;
        public PanelFade Fade{get;private set;}
        void Start()
        {
            canvas=UiKit.CreateCanvas("Barks",8,transform);feed=GetComponent<PickupFeed>();text=UiKit.Legible(UiKit.Text(canvas.transform,"Bark","",25,UiKit.LooseTextColor,TextAlignmentOptions.TopLeft));
            UiKit.Rect(text.gameObject,new(0,1),new(0,1),Rest,new(460,170));var plate=TextPlate.Attach(text,"BarkPlate",34,16);BarkService.Spoken+=Show;
            plate.Frame.color=new Color(1,1,1,RestingPlate);Fade=new PanelFade(plate.Frame.rectTransform,plate.Frame);
        }
        void Show(string speaker,string line)
        {
            text.text=$"<color=#{NameColor}>{DisplayNames.Speaker(speaker)}</color>\n{line}";until=Time.unscaledTime+3.5f;
            // Measured once a line, as its plate is. It lands clear: there's nothing to slide past when it has only just come up.
            var ink=UiKit.Ink(text);plateHigh=ink.yMax+16;plateLow=ink.yMin-16;plateLeft=ink.xMin-34;plateRight=ink.xMax+34;
            slide=Seat(0);text.rectTransform.anchoredPosition=Rest+new Vector2(0,slide);
        }
        /// <summary>A menu or a conversation hides the line and holds its time. Wiped, it was gone for good, and a hail counts as heard from its first line: Orrin's warning before a one-way warp could be lost to a pause.</summary>
        void Update()
        {
            if(text==null)return;
            bool blocked=GameInput.Current.Blocked;if(canvas.enabled==blocked)canvas.enabled=!blocked;
            if(blocked){until+=Time.unscaledDeltaTime;return;}
            if(Time.unscaledTime>until&&text.text.Length>0)text.text="";
            if(text.text.Length==0)return;
            float wanted=Seat(slide);if(wanted==slide)return;
            slide=Mathf.MoveTowards(slide,wanted,Time.unscaledDeltaTime*SlidePerSecond);text.rectTransform.anchoredPosition=Rest+new Vector2(0,slide);
        }
        /// <summary>The nearest place in the column clear of everyone. It stays where it's got to while that still works, and goes home only with room to spare, so nobody walking along an edge makes it twitch. With nowhere clear it stays put and fades, as any plate does.</summary>
        float Seat(float now)
        {
            var camera=Camera.main;var party=PartyController.Current;if(camera==null||party==null||party.members==null)return now;
            float height=((RectTransform)canvas.transform).rect.height,top=height+Rest.y+plateHigh,bottom=height+Rest.y+plateLow;
            Measure(camera,party,top,bottom);
            if(Clear(0,Air*2))return 0;
            float most=Mathf.Max(0,height-(feed!=null?feed.Reach:311)-6-top),least=Mathf.Min(0,FootRoom-bottom);
            if(now>=least&&now<=most&&Clear(now,Air))return now;
            float best=now,bestFar=float.MaxValue;
            for(int i=0;i<shutCount;i++)
            {
                float spot=shut[i]+(i%2==0?-Air-1:Air+1);
                if(spot<least||spot>most||!Clear(spot,Air))continue;
                float far=Mathf.Abs(spot);if(far<bestFar){bestFar=far;best=spot;}
            }
            return best;
        }
        bool Clear(float spot,float air)
        {
            for(int i=0;i<shutCount;i+=2)if(spot>shut[i]-air&&spot<shut[i+1]+air)return false;
            return true;
        }
        void Measure(Camera camera,PartyController party,float top,float bottom)
        {
            shutCount=0;float scale=canvas.scaleFactor,left=Rest.x+plateLeft,right=Rest.x+plateRight;var across=camera.transform.right;
            foreach(var hero in party.members)
            {
                if(hero==null)continue;
                var at=hero.transform.position;Vector3 feet=camera.WorldToScreenPoint(at);if(feet.z<=0)continue;
                float half=Mathf.Abs(camera.WorldToScreenPoint(at+across*(hero.flight?HeroCollision.HullRadius(hero.character):.6f)).x-feet.x)/scale,low,high;
                if(hero.flight){low=feet.y/scale-half*ShipTall;high=feet.y/scale+half*ShipTall;}
                else{low=feet.y/scale;high=camera.WorldToScreenPoint(at+Vector3.up*(hero.Health.Alive?1.9f:.5f)).y/scale;}
                Shut(feet.x/scale,half,low,high,left,right,top,bottom);
            }
            // Orrin's box sat on the tender he was saying was free.
            foreach(var thing in InteractionPrompt.Active)
            {
                var at=camera.WorldToScreenPoint(thing.transform.position);float x=at.x/scale;
                if(at.z<=0||x+ThingHalf<left||x-ThingHalf>right||!thing.Available)continue;
                Shut(x,ThingHalf,at.y/scale-ThingHalf,at.y/scale+ThingHalf,left,right,top,bottom);
            }
        }
        void Shut(float x,float half,float low,float high,float left,float right,float top,float bottom)
        {
            if(x+half<left||x-half>right||shutCount+2>shut.Length)return;
            shut[shutCount++]=low-top;shut[shutCount++]=high-bottom;
        }
        void OnDestroy(){BarkService.Spoken-=Show;}
    }
}
