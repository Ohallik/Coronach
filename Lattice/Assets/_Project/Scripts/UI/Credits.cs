using System.Collections.Generic;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// The game's end (ch10-wiring section 8, D288): no CHAPTER COMPLETE plate for the last chapter (ChapterCards), but the credits, over the convoy under its new sky, and then the town again,
    /// walkable. In the town the game ends in (<see cref="Zone"/>) they begin by themselves when a conversation closes having put <see cref="Flag"/> up (the last scene, JunLastLine, whose own
    /// last line sets it), or when the town is entered with it up and the roll never seen to its end (a quit in the middle of it). A flag put up any other way, by a fixture or a talk that finds
    /// it already up, rolls nothing. From the first frame the party is held (no step, menu, map, bag, swap or talk: the HUD, the names and the signs leave as they do for a menu), nothing
    /// barks, the title's theme plays from its top (<see cref="Music"/>), and a camera of the roll's own draws back from the party over the town to the frames its builder publishes for it.
    /// Two lines one after the other (CreditsRoll), the roll at a reading pace, the player's own line held at the middle, "The pears keep a schedule." alone, the title; then the camera comes
    /// home to the party, the town's music comes back, and the hold lets go. Holding Interact (A) hurries it five times over; Pause (Start) skips it, once it has been seen to its end on this
    /// machine (<see cref="Seen"/>). At the end the save says so (<see cref="Rolled"/>, written to the autosave at once), and nothing the last scene announced (the last quest's card) comes up
    /// after it. Logs <c>CREDITS_BEGIN</c>, <c>CREDITS_SKIP</c>, <c>CREDITS_RETURN</c>, <c>CREDITS_END</c>.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class Credits:MonoBehaviour
    {
        /// <summary>The flag the wiring names (section 3): the last scene's own last line sets it.</summary>
        public const string Flag="game.complete";
        /// <summary>This save's credits have been played to their end, so entering the town again rolls nothing.</summary>
        public const string Rolled="credits.rolled";
        /// <summary>The town the game ends in (wiring section 2): nowhere else rolls them.</summary>
        public const string Zone="NeighbourhoodAfloat";
        /// <summary>The roll's music, by its title in docs/music/tracks.json: the title's own theme (the chapter brief, "Credits: Title Theme"; docs/music/NEW_TRACKS.md, "Title Theme stays for
        /// the credits"), staged always. A cue of the creator's for the credits is this one line, and its staging (MusicIntake.Stage's list, as this one's).</summary>
        public const string Music="Title Theme";
        /// <summary>What the title says under its name once a save there has been played to its end, in the closing line's own words, where a fresh game says what the game is.</summary>
        public const string TitleLine="THE CONVOY IS THROUGH.";
        public enum Stage{Waiting,Opening,Rolling,LastLine,Title,Returning,Done}
        public static Credits Current{get;private set;}
        public Stage Now{get;private set;}
        /// <summary>Seconds into the roll. The lines, the roll and the camera all read it, so a still can be taken anywhere in it by setting it.</summary>
        public float Clock{get;set;}
        /// <summary>Interact is held: the roll runs <see cref="HurryRate"/> times over.</summary>
        public bool Hurried{get;private set;}
        /// <summary>Canvas units a second (the canvas is 1080 tall): a line crosses the screen in about seventeen seconds, long enough to read a licence twice.</summary>
        public const float Speed=64,HurryRate=5;
        /// <summary>When each part comes, in seconds of <see cref="Clock"/>. The two lines come up 1.5 s and 4.5 s in and go together at 10.5; the roll starts at 12.5, its last section held
        /// at the middle for 4.5 s; the last line alone for 5 s; the title for 5.</summary>
        public const float LineOneAt=1.5f,LineTwoAt=4.5f,LinesGo=10.5f,RollAt=12.5f,LastHold=4.5f,LastLineHold=5,TitleHold=5;
        const float LineFade=1.2f,LinesGoFor=1.5f,RollFade=1.5f,LastLineFade=1.2f,TitleFade=1.6f,Gap=.6f;
        /// <summary>The camera's move out to the town's wide frame, and its way home at the end (real seconds: a hurried roll comes home at the same pace), with the type fading first.</summary>
        const float OutSeconds=10,BackSeconds=3.5f,FadeBack=1.5f;
        public float RollEnd=>RollAt+(lastCentre+half)/Speed;
        public float LastLineAt=>RollEnd+LastHold+RollFade+Gap;
        public float TitleAt=>LastLineAt+LastLineFade*2+LastLineHold+Gap;
        public float TitleEnd=>TitleAt+TitleFade+TitleHold;
        /// <summary>Type sizes at 1080 lines, from a couch: every one well over the 24 floor.</summary>
        public const float LineSize=46,HeadingSize=44,RoleSize=26,NameSize=40,DetailSize=28,TitleSize=30,HintSize=24;
        public const float ColumnWidth=1200,DetailWidth=1100;
        /// <summary>The town's wide frame as its builder publishes it for LookDev (LastMooringBuilder.Neighbourhood.Views, "overview"; the map doc's "Sightlines"): the fixed camera's angle
        /// drawn back over the market, the districts moored together with the new sky's giant in every gap. Then on north past the map's "overview-north" (0, 80, 3.2 back) to the gallery's
        /// rail, a little further back: there the convoy's two big hulls moored off the north rail (Tallow's station hull, the hauler) stand in the top corners with the giant between them,
        /// where from "overview-north" itself each stood just outside the frame. A point, and how many of the camera's own distances back from it.</summary>
        static readonly (Vector3 at,float back) Overview=(new(0,1,40),4.4f),Convoy=(new(0,1,96),4f);
        /// <summary>A dark band behind the column and a light dim over the rest: the type reads over the giant's pastel and the decks' clutter, and the town is still there round it.</summary>
        const float BandAlpha=.68f,BandWidth=1500,DimStrength=.22f;

        Canvas canvas;CanvasGroup group,rollGroup;Image band,dim;RectTransform roll;TMP_Text lineOne,lineTwo,lastLine,hint;RawImage wordmark;
        readonly List<TMP_Text> words=new();
        /// <summary>Every piece of type the roll put up: the lines, the roll's, the last line and the hint.</summary>
        public IReadOnlyList<TMP_Text> Words=>words;
        public RectTransform Roll=>roll;
        /// <summary>Where each heading's middle stands in the roll, down from its top: a still of a heading is taken by moving the roll there.</summary>
        public readonly Dictionary<string,float> Headings=new();
        float lastCentre,half=540,backClock,settledFor,hintUntil;
        bool talking,upAtTalk,owed,onLoad,holding,hushed,cued;
        CinemachineCamera view;Transform home;Vector3 fromAt,outAt,driftAt,backFrom;Quaternion fromAngle,angle,backAngle;

        static bool Has(string key)=>GameServices.Current!=null&&GameServices.Current.Flags.GetBool(key);
        /// <summary>A save that has been played to its end: what the title reads.</summary>
        public static bool Finished(GameState state)=>state!=null&&state.flags.TryGetValue(Flag,out bool up)&&up;
        public bool Running=>Now!=Stage.Waiting&&Now!=Stage.Done;

        void OnEnable()
        {
            if(gameObject.scene.name!=Zone){enabled=false;return;}
            Current=this;DialogueSystem.Settled+=OnSettled;
        }
        void OnDisable(){DialogueSystem.Settled-=OnSettled;Release();if(Current==this)Current=null;}
        void Start()
        {
            // Entered with the game over and the roll never seen to its end (a quit in the middle of it): it rolls again once the town has settled round the party.
            if(Has(Flag)&&!Has(Rolled)){owed=true;onLoad=true;}
        }
        /// <summary>The conversation that has just ended put the flag up (its own last line, as JunLastLine's does): the roll takes the screen there and then, before anything else this
        /// frame can take the press that closed the box, or the card the talk earned can come up.</summary>
        void OnSettled(){if(Now==Stage.Waiting&&talking&&!upAtTalk&&Has(Flag)&&!Has(Rolled)){talking=false;Begin("talk");}}
        bool Ready(GameServices services)
        {
            var flow=SceneFlow.Current;var dialogue=DialogueSystem.Current;var party=PartyController.Current;
            return flow!=null&&!flow.Loading&&dialogue!=null&&dialogue.Prepared&&!dialogue.Running&&!services.Input.Blocked&&party!=null&&party.members!=null&&party.members.Length>0&&party.Active!=null;
        }

        void Update()
        {
            var services=GameServices.Current;if(services==null)return;
            var dialogue=DialogueSystem.Current;bool running=dialogue!=null&&dialogue.Running;
            if(Now==Stage.Waiting)
            {
                // Which talk puts the flag up: one that began with it down. A talk that finds it up already (everyone's line, asked again after the end) rolls nothing.
                if(running&&!talking){talking=true;upAtTalk=Has(Flag);}
                else if(!running&&talking){talking=false;if(!upAtTalk&&Has(Flag)&&!Has(Rolled))owed=true;}
                if(!owed||!Ready(services)){settledFor=0;return;}
                // Entered with it owed: a beat for the town to be drawn and the load's black to lift first.
                settledFor+=Time.unscaledDeltaTime;if(onLoad&&settledFor<1)return;
                Begin(onLoad?"load":"talk");return;
            }
            if(Now==Stage.Done)return;
            // The hold, asked again each frame, as the launch asks it (CoronachLaunch.Hold): whatever let go of it, the roll still has the screen.
            if(!running)services.Input.Blocked=true;
            if(Now==Stage.Returning)
            {
                backClock+=Time.unscaledDeltaTime;Draw();
                if(backClock>=BackSeconds)Finish();
                return;
            }
            var input=GameInput.Current;
            Hurried=input!=null&&input.Find("Interact")?.IsPressed()==true||UiActions.Ready&&UiActions.Submit.IsPressed();
            if(AnyPress())hintUntil=Time.unscaledTime+4;
            // Only a roll seen to its end skips: the first time through, Start does what every other press does, which is nothing.
            if(Seen&&input!=null&&input.Find("Pause")?.WasPressedThisFrame()==true){Debug.Log("CREDITS_SKIP");Return();Draw();return;}
            Clock+=Time.unscaledDeltaTime*(Hurried?HurryRate:1);
            Now=Clock<RollAt?Stage.Opening:Clock<LastLineAt?Stage.Rolling:Clock<TitleAt?Stage.LastLine:Stage.Title;
            if(Clock>=TitleEnd)Return();
            Draw();
        }

        void Begin(string why)
        {
            var services=GameServices.Current;owed=false;onLoad=false;Now=Stage.Opening;Clock=0;
            services.Input.Blocked=true;holding=true;
            BarkService.Hush(this,true);hushed=true;
            MusicDirector.FromTheTop(Music);MusicDirector.Encounter(this,Music,true);cued=true;
            Build();Frame();Draw();
            Debug.Log("CREDITS_BEGIN "+why+" "+Mathf.RoundToInt(TitleEnd)+"s");
        }
        /// <summary>Everything after the title, or a skip: the type fades, the camera comes home, the town's own music comes back.</summary>
        void Return()
        {
            Now=Stage.Returning;backClock=0;
            if(view!=null){backFrom=view.transform.position;backAngle=view.transform.rotation;}
            if(cued){MusicDirector.Encounter(this,null,false);cued=false;}
            Debug.Log("CREDITS_RETURN");
        }
        void Finish()
        {
            var services=GameServices.Current;Now=Stage.Done;
            services.Flags.SetBool(Rolled,true);Seen=true;
            // The last scene finished the main quest, and its card has waited under the roll: it doesn't come up over the town afterwards (manifest: after Taren's line, no toast).
            GetComponent<QuestToast>()?.Forget();
            Release();
            services.Saves.Save("autosave",services.State);
            Debug.Log("CREDITS_END");
        }
        /// <summary>Lets go of everything the roll took: the hold, the hush, the music, its camera and its type. A roll cut short by the scene going lets go the same way.</summary>
        void Release()
        {
            if(hushed){BarkService.Hush(this,false);hushed=false;}
            if(cued){MusicDirector.Encounter(this,null,false);cued=false;}
            if(view!=null){Destroy(view.gameObject);view=null;}
            if(canvas!=null){Destroy(canvas.gameObject);canvas=null;}
            if(holding){holding=false;var services=GameServices.Current;var flow=SceneFlow.Current;if(services!=null&&(flow==null||!flow.Loading))services.Input.Blocked=false;}
        }

        // ---------------------------------------------------------------- the type
        void Build()
        {
            canvas=UiKit.CreateCanvas("Credits",55,transform);
            group=canvas.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
            dim=UiKit.Dim(canvas.transform,DimStrength);dim.name="CreditsDim";dim.raycastTarget=false;
            band=UiKit.SoftBand(canvas.transform,"CreditsBand",new Color(.02f,.05f,.1f,BandAlpha));
            // Its soft edge as wide as a quarter of it, and off the screen top and bottom: drawn with the sprite's own 31-pixel edge the band stood in the town as a hard dark pillar.
            band.pixelsPerUnitMultiplier=.12f;var bandRect=UiKit.Rect(band.gameObject,new(.5f,0),new(.5f,1),Vector2.zero,Vector2.zero);bandRect.sizeDelta=new(BandWidth,640);
            lineOne=Line("CreditsFirstLine",CreditsRoll.FirstLine,new(0,44));lineTwo=Line("CreditsSecondLine",CreditsRoll.SecondLine,new(0,-44));
            roll=new GameObject("CreditsRoll",typeof(RectTransform),typeof(CanvasGroup)).GetComponent<RectTransform>();roll.SetParent(canvas.transform,false);
            roll.anchorMin=roll.anchorMax=new(.5f,.5f);roll.pivot=new(.5f,1);roll.sizeDelta=new(ColumnWidth,0);rollGroup=roll.GetComponent<CanvasGroup>();rollGroup.alpha=0;
            float y=0;Headings.Clear();
            foreach(var section in CreditsRoll.Sections)
            {
                y=Heading(section.heading,section.role,y);
                foreach(var (name,detail) in section.people)
                {
                    y=Place(UiKit.Text(roll,"CreditsName",name,NameSize,UiKit.LooseTextColor,TextAlignmentOptions.Top),0,y,ColumnWidth)+4;
                    if(!string.IsNullOrEmpty(detail))y=Place(UiKit.Text(roll,"CreditsDetail",detail,DetailSize,UiKit.FrostColor,TextAlignmentOptions.Top),0,y,DetailWidth);
                    y+=30;
                }
                // The music's titles, two to a row, each in its own half of the column.
                for(int i=0;i<section.titles.Length;i+=2)
                {
                    float row=0;
                    for(int j=i;j<i+2&&j<section.titles.Length;j++)
                    {
                        float end=Place(UiKit.Text(roll,"CreditsTrack",section.titles[j],TitleSize,UiKit.LooseTextColor,TextAlignmentOptions.Top),(j-i==0?-1:1)*290,y,560);
                        row=Mathf.Max(row,end-y);
                    }
                    y+=row+8;
                }
                y+=130;
            }
            // The player's, held at the middle of the screen when the roll stops.
            float top=y;y=Heading(CreditsRoll.PlayerHeading,null,y);
            y=Place(UiKit.Text(roll,"CreditsPlayerLine",CreditsRoll.PlayerLine,NameSize,UiKit.LooseTextColor,TextAlignmentOptions.Top),0,y,ColumnWidth);
            lastCentre=(top+y)*.5f;
            lastLine=Line("CreditsLastLine",CreditsRoll.LastLine,Vector2.zero);
            var art=Resources.Load<Texture2D>("UI/Title/wordmark");
            wordmark=new GameObject("CreditsWordmark",typeof(RawImage)).GetComponent<RawImage>();wordmark.transform.SetParent(canvas.transform,false);wordmark.texture=art;wordmark.raycastTarget=false;
            UiKit.Rect(wordmark.gameObject,new(.5f,.5f),new(.5f,.5f),new(0,20),new(900,art!=null?900f*art.height/art.width:176));wordmark.color=new Color(1,1,1,0);
            hint=UiKit.Legible(UiKit.Text(canvas.transform,"CreditsHint","",HintSize,UiKit.LooseTextColor,TextAlignmentOptions.MidlineRight));
            UiKit.Rect(hint.gameObject,new(1,0),new(1,0),new(-470,64),new(840,40));hint.alpha=0;words.Add(hint);
            Canvas.ForceUpdateCanvases();half=Mathf.Max(540,((RectTransform)canvas.transform).rect.height*.5f);
        }
        TMP_Text Line(string name,string text,Vector2 at)
        {
            var line=UiKit.Legible(UiKit.Text(canvas.transform,name,text,LineSize,UiKit.LooseTextColor));UiKit.Rect(line.gameObject,new(.5f,.5f),new(.5f,.5f),at,new(1400,70));
            line.alpha=0;words.Add(line);return line;
        }
        /// <summary>A heading in the chapter card's manner: the display face in gold, a short gold rule, and what it is for under it.</summary>
        float Heading(string heading,string role,float y)
        {
            var text=UiKit.Heading(roll,"CreditsHeading",heading,HeadingSize,UiKit.GoldColor,TextAlignmentOptions.Top);
            float start=y;y=Place(text,0,y,ColumnWidth)+8;
            var rule=UiKit.Panel(roll,"CreditsRule",new Color(UiKit.GoldColor.r,UiKit.GoldColor.g,UiKit.GoldColor.b,.85f));rule.raycastTarget=false;
            var rect=rule.rectTransform;rect.anchorMin=rect.anchorMax=new(.5f,1);rect.pivot=new(.5f,1);rect.anchoredPosition=new(0,-y);rect.sizeDelta=new(320,3);y+=3+10;
            Headings[heading]=(start+y)*.5f;
            if(!string.IsNullOrEmpty(role))y=Place(UiKit.Text(roll,"CreditsRole",role,RoleSize,UiKit.FrostColor,TextAlignmentOptions.Top),0,y,ColumnWidth);
            return y+30;
        }
        /// <summary>Stands a piece of type in the roll at <paramref name="y"/> down from its top, as tall as its words are at that width, and says where the next may start.</summary>
        float Place(TMP_Text text,float x,float y,float width)
        {
            UiKit.Legible(text);var rect=text.rectTransform;rect.anchorMin=rect.anchorMax=new(.5f,1);rect.pivot=new(.5f,1);
            float tall=Mathf.Ceil(text.GetPreferredValues(text.text,width,0).y)+2;rect.anchoredPosition=new(x,-y);rect.sizeDelta=new(width,tall);
            words.Add(text);return y+tall;
        }
        static float Fade(float t,float inAt,float inFor,float outAt,float outFor)=>Mathf.Min(Mathf.Clamp01((t-inAt)/inFor),Mathf.Clamp01((outAt+outFor-t)/outFor));
        void Draw()
        {
            if(canvas==null)return;float t=Clock;
            group.alpha=Now==Stage.Returning?1-Mathf.Clamp01(backClock/FadeBack):1;
            float under=Mathf.Clamp01(t/1.5f);band.color=new Color(band.color.r,band.color.g,band.color.b,BandAlpha*under);dim.color=new Color(0,0,0,DimStrength*under);
            lineOne.alpha=Fade(t,LineOneAt,LineFade,LinesGo,LinesGoFor);lineTwo.alpha=Fade(t,LineTwoAt,LineFade,LinesGo,LinesGoFor);
            roll.anchoredPosition=new(0,Mathf.Min(-half+Speed*Mathf.Max(0,t-RollAt),lastCentre));
            rollGroup.alpha=t<RollAt?0:1-Mathf.Clamp01((t-RollEnd-LastHold)/RollFade);
            lastLine.alpha=Fade(t,LastLineAt,LastLineFade,LastLineAt+LastLineFade+LastLineHold,LastLineFade);
            wordmark.color=new Color(1,1,1,Mathf.Clamp01((t-TitleAt)/TitleFade));
            bool hinted=Time.unscaledTime<hintUntil&&Now<Stage.Returning;
            if(hinted)hint.text=PromptService.Tag("Interact")+"  HOLD TO HURRY"+(Seen?"     ·     "+PromptService.Tag("Pause")+"  SKIP":"");
            hint.alpha=Mathf.MoveTowards(hint.alpha,hinted?.9f:0,Time.unscaledDeltaTime*3);
        }
        static bool AnyPress()
        {
            var keys=Keyboard.current;if(keys!=null&&keys.anyKey.wasPressedThisFrame)return true;
            foreach(var pad in Gamepad.all)foreach(var control in pad.allControls)if(control is ButtonControl button&&button.wasPressedThisFrame)return true;
            return false;
        }

        // ---------------------------------------------------------------- the camera
        /// <summary>A camera of the roll's own, over the zone's: it starts where the game's stands, keeps the zone's fixed angle and lens, and goes where the town's wide frames are.</summary>
        void Frame()
        {
            var main=Camera.main;if(main==null)return;
            var zone=ZoneController.Current;var profile=zone!=null&&zone.definition!=null?zone.definition.cameraProfile:null;
            angle=profile!=null?Quaternion.Euler(profile.pitch,profile.yaw,0):main.transform.rotation;float distance=profile!=null?profile.distance:20,fov=profile!=null?profile.fov:main.fieldOfView;
            fromAt=main.transform.position;fromAngle=main.transform.rotation;
            Vector3 Back((Vector3 at,float back) frame)=>frame.at-angle*Vector3.forward*distance*frame.back;
            outAt=Back(Overview);driftAt=Back(Convoy);
            var rig=FindFirstObjectByType<CameraRig>();home=rig!=null?rig.transform.Find("GroundFollow"):null;
            view=new GameObject("CreditsView",typeof(CinemachineCamera)).GetComponent<CinemachineCamera>();view.transform.SetParent(transform,false);
            view.transform.SetPositionAndRotation(fromAt,fromAngle);view.Priority=50;
            var lens=view.Lens;lens.FieldOfView=fov;lens.NearClipPlane=.1f;lens.FarClipPlane=1600;view.Lens=lens;
        }
        static float Smooth(float u){u=Mathf.Clamp01(u);return u*u*(3-2*u);}
        void LateUpdate()
        {
            if(view==null)return;
            if(Now==Stage.Returning)
            {
                // Home to wherever the game's own camera stands now, at the party.
                Vector3 at=home!=null?home.position:fromAt;var turn=home!=null?home.rotation:fromAngle;float u=Smooth(backClock/BackSeconds);
                view.transform.SetPositionAndRotation(Vector3.Lerp(backFrom,at,u),Quaternion.Slerp(backAngle,turn,u));return;
            }
            if(Clock<OutSeconds){float u=Smooth(Clock/OutSeconds);view.transform.SetPositionAndRotation(Vector3.Lerp(fromAt,outAt,u),Quaternion.Slerp(fromAngle,angle,u));return;}
            view.transform.SetPositionAndRotation(Vector3.Lerp(outAt,driftAt,Smooth((Clock-OutSeconds)/Mathf.Max(1,TitleEnd-OutSeconds))),angle);
        }

        // ---------------------------------------------------------------- seen once
        const string SeenKey="credits.seen";
        /// <summary>Kept as the keyboard's rebinds are (KeyBindingStore): beside an isolated run's own settings, in the player's preferences otherwise.</summary>
        static string SeenFile{get{string folder=DevArgs.Value("-audio-settings-path")??DevArgs.Value("-savepath");return string.IsNullOrEmpty(folder)?null:Path.Combine(Path.GetFullPath(folder),"credits-seen.txt");}}
        /// <summary>The roll has been watched to its end on this machine, in any save: from then on Start skips it.</summary>
        public static bool Seen
        {
            get{string file=SeenFile;return file==null?PlayerPrefs.GetInt(SeenKey,0)==1:File.Exists(file);}
            set
            {
                string file=SeenFile;
                if(file==null){PlayerPrefs.SetInt(SeenKey,value?1:0);PlayerPrefs.Save();return;}
                try{if(value){Directory.CreateDirectory(Path.GetDirectoryName(file));File.WriteAllText(file,"seen");}else if(File.Exists(file))File.Delete(file);}
                catch(IOException e){Debug.LogWarning("The credits' mark could not be kept: "+e.Message);}
            }
        }
    }
}
