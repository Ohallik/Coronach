using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Dialogue;
using Lattice.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>
    /// Sela's chart: the settlements the party has stood in, as lantern points joined by route lines, and travel between them from anywhere. It is the pause menu's
    /// Chart page (PauseMenu.ChartPage), which exists once chapter 3's first scene has set <see cref="ChartedFlag"/>: until then the menu is the seven pages it always was.
    /// The stops are data (ChartStop), one per settlement, added by each chapter's own builder. The rules are here and static, so a test or the replay driver asks the
    /// same questions the page does; the page itself is built by <see cref="Build"/> into the pause menu's panel each time that redraws.
    /// </summary>
    public sealed class RouteChart
    {
        public const string ChartedFlag="xylos.charted",StopPrefix="Stop_",ArtPath="UI/Generated/route-chart";
        /// <summary>From this chapter on a zone marks itself visited when the party arrives (<see cref="Arrive"/>). Chapters 1 and 2's zones write nothing: their saves hold exactly what they always did.</summary>
        public const int FirstVisitedChapter=3;
        /// <summary>How a stop stands for the party right now.</summary>
        public enum Standing{Uncharted,Here,Closed,Open}
        public static bool Available=>GameServices.Current!=null&&Set(GameServices.Current.State,ChartedFlag);
        /// <summary>The same text as <see cref="ChartStop.VisitedPrefix"/>, written here so the declaration above sits beside a literal of it.</summary>
        const string Visited="visited.";
        static bool Set(GameState state,string flag)=>!string.IsNullOrEmpty(flag)&&state.flags.TryGetValue(flag,out bool on)&&on;
        /// <summary>Every stop, in the order the story reaches them.</summary>
        public static ChartStop[] Stops
        {
            get
            {
                var all=GameCatalog.All<ChartStop>();
                Array.Sort(all,(a,b)=>a.chapter!=b.chapter?a.chapter.CompareTo(b.chapter):string.CompareOrdinal(a.name,b.name));
                return all;
            }
        }
        public static ChartStop Find(string id)=>string.IsNullOrEmpty(id)?null:GameCatalog.Find<ChartStop>(id);
        public static string Name(ChartStop stop)=>string.IsNullOrWhiteSpace(stop.displayName)?DisplayNames.Zone(stop.zone):stop.displayName;
        /// <summary>Standing in the stop's own zone is here. Otherwise it's uncharted until the party has stood there, and closed while a story beat holds it shut.</summary>
        public static Standing Of(ChartStop stop,GameState state)=>stop.zone==state.zone?Standing.Here:!Set(state,stop.UnlockFlag)?Standing.Uncharted:Set(state,stop.lockedFlag)?Standing.Closed:Standing.Open;
        /// <summary>The stop a zone belongs to: the one that arrives there, or failing that the one it is reached from (ChartStop.reaches).</summary>
        public static ChartStop At(string scene,ChartStop[] stops=null)
        {
            if(string.IsNullOrEmpty(scene))return null;
            stops??=Stops;
            foreach(var stop in stops)if(stop.zone==scene)return stop;
            foreach(var stop in stops)if(stop.reaches!=null&&Array.IndexOf(stop.reaches,scene)>=0)return stop;
            return null;
        }
        /// <summary>The stop where the open story quest's next step lies: its target's scene (QuestTarget.scene), or the zone a Reach step names. A step that says neither marks nothing.</summary>
        public static ChartStop Objective(GameState state,ChartStop[] stops=null)
        {
            var quest=StoryQuests.Open(state,out int step);
            if(quest==null)
            {
                // Between chapters: where the next begins (QuestDef.next), or while that place hasn't been stood in, the nearest place on the way there that has.
                if(!StoryQuests.Between(state,out var next))return null;
                stops??=Stops;var there=At(next.scene,stops);
                if(there!=null&&Of(there,state)==Standing.Uncharted&&next.via!=null)
                    foreach(string between in next.via){var stop=At(between,stops);if(stop!=null&&Of(stop,state)!=Standing.Uncharted)return stop;}
                return there;
            }
            string scene=quest.stepTargets!=null&&step<quest.stepTargets.Length?quest.stepTargets[step].scene:null;
            if(string.IsNullOrEmpty(scene)&&quest.steps[step].kind==ObjectiveKind.Reach)scene=quest.steps[step].target;
            return At(scene,stops);
        }
        /// <summary>Why nobody travels right now, in the words the page shows; null when the way is clear. These are a way out's own conditions (WarpBeacon, DockingPad, RepairBay): no leaving a boss
        /// fight (D198), and nothing awake nearby. Nearby is a quiet interaction's reach from the hero (InteractionCalm), not a dock's: a pad is a place the party has to get to, and the chart is in a pocket.
        /// A flight lane's fight is a whole lane wide, so there any encounter begun and not cleared holds it.</summary>
        public static string Blocked()
        {
            var flow=SceneFlow.Current;if(flow==null||flow.Loading)return "Not while you're already under way.";
            if(DialogueSystem.Current!=null&&DialogueSystem.Current.Running)return "Not in the middle of a conversation.";
            if(BossController.AnyFightUnderway)return "No leaving a boss fight: the chart waits until it's over.";
            var party=PartyController.Current;
            if(party!=null&&party.Active!=null&&EnemyBrain.AwakeNear(party.Active.transform.position,EnemyBrain.InteractionCalm))return "An enemy is awake nearby: the chart waits until it's quiet.";
            var zone=ZoneController.Current;
            if(zone!=null&&zone.definition!=null&&zone.definition.kind==ZoneKind.SpaceCombat)
                foreach(var fight in UnityEngine.Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
                    if(fight.Started&&fight.Remaining>0)return "There's a fight under way in flight: clear the lane and the chart will take you.";
            return null;
        }
        /// <summary>Why the party can't travel to this stop, or null when it can.</summary>
        public static string Refusal(ChartStop stop)
        {
            if(stop==null)return "That place isn't on the chart.";
            switch(Of(stop,GameServices.Current.State))
            {
                case Standing.Here:return $"You're already at {Name(stop)}.";
                case Standing.Uncharted:return "Uncharted: the chart only holds places you've stood in.";
                case Standing.Closed:return string.IsNullOrWhiteSpace(stop.lockedReason)?$"{Name(stop)} is closed to you for now.":stop.lockedReason;
            }
            // A stop defined ahead of its scene: a load of a scene that isn't in the build never finishes.
            if(!Application.CanStreamedLevelBeLoaded(stop.zone))return $"The way to {Name(stop)} isn't open yet.";
            return Blocked();
        }
        /// <summary>Goes there, by the same load a warp uses: the arrival autosaves, and the zone gives the party its form. False, and nothing happens, if it's refused.</summary>
        public static bool Travel(ChartStop stop)
        {
            if(Refusal(stop)!=null)return false;
            Debug.Log($"CHART_TRAVEL {stop.id} {stop.zone} {stop.spawn}");
            SceneFlow.Current.LoadZone(stop.zone,string.IsNullOrEmpty(stop.spawn)?"Arrival":stop.spawn);return true;
        }
        /// <summary>The party has arrived in a zone (ArenaRuntime, behind the load's black, so the arrival's autosave holds it): from chapter 3 on it is marked visited, which is what charts a stop
        /// with no flag of its own. Every such zone, not only a stop's, so a place that becomes a stop later is already charted for whoever has stood there.</summary>
        [SetsFlag("visited.",Prefix=true)] // visited.<zone>
        public static void Arrive(string scene)
        {
            var services=GameServices.Current;var zone=GameCatalog.Find<ZoneDef>(scene);
            if(services==null||zone==null||zone.chapter<FirstVisitedChapter||Set(services.State,Visited+scene))return;
            services.State.flags[Visited+scene]=true;Debug.Log("CHART_VISITED "+scene);
        }

        // ------------------------------------------------------------ where lanterns stand, and how the d-pad walks them
        static readonly Vector2 Mid=new(.5f,.5f),FieldCentre=new(170,44),FieldSize=new(1220,612);
        /// <summary>The part of the field lanterns stand in, in the panel's own coordinates: inset so the easternmost name stays on the vellum, and above the band the selected stop is described in.</summary>
        static readonly Rect Lanterns=new(-320,-24,980,300);
        /// <summary>Two stops nearer than this both ways have their names in each other's.</summary>
        public const float ApartX=220,ApartY=86;
        /// <summary>What a name keeps between its letters and the field's edge: the art's own ruled border is drawn about 18 inside it, and a name runs no nearer than 12 inside that.</summary>
        public const float NameMargin=30;
        /// <summary>How far a stop's name is set in from under its lantern so that its letters stay on the field: nothing for nearly every stop. Neighbourhood Afloat stands in the eastern
        /// column and its name ran off the vellum's edge. The name is set at the type floor already (24: smaller is under it), and a second line would hang into the lantern of
        /// Harrowgate below; so, as the boss card measures a name against its card (GameHud.CardSizeFor), the chart measures it against the field and sets it in, whole and on one line,
        /// still over its lantern. Measured by the place's name whether or not it is charted yet, so the label stands where the name will.</summary>
        static float Inward(TMP_Text label,string name,float x)
        {
            float half=label.GetPreferredValues(name).x*.5f,west=FieldCentre.x-FieldSize.x*.5f+NameMargin,east=FieldCentre.x+FieldSize.x*.5f-NameMargin;
            return x+half>east?east-(x+half):x-half<west?west-(x-half):0;
        }
        public static Vector2 Place(Vector2 position)=>new(Lanterns.x+Mathf.Clamp01(position.x)*Lanterns.width,Lanterns.y+Mathf.Clamp01(position.y)*Lanterns.height);
        /// <summary>The stop a d-pad press moves to from stop <paramref name="from"/>: the nearest in the pressed direction, or -1. A stop counts as that way if it's no more than twice as far
        /// to the side as ahead, and one dead ahead is preferred to a nearer one off to the side, so a row of stops walks as a row.</summary>
        public static int Neighbour(IReadOnlyList<Vector2> at,int from,Vector2 direction)
        {
            int best=-1;float least=float.MaxValue;
            for(int i=0;i<at.Count;i++)
            {
                if(i==from)continue;
                var gap=at[i]-at[from];float ahead=Vector2.Dot(gap,direction),aside=Mathf.Abs(gap.x*direction.y-gap.y*direction.x);
                if(ahead<1||aside>ahead*2)continue;
                float score=ahead+aside*1.5f;if(score<least){least=score;best=i;}
            }
            return best;
        }
        static readonly Vector2[] Ways={Vector2.up,Vector2.right,Vector2.down,Vector2.left};
        /// <summary>What's wrong with where the stops stand, as sentences: one off the field, two in each other's names, one the d-pad can't bring the cursor to from another. The editor's lint
        /// (ContentScaffold.Check) and the tests both ask this.</summary>
        public static List<string> LayoutFaults(IReadOnlyList<ChartStop> stops)
        {
            var faults=new List<string>();var at=new List<Vector2>();
            foreach(var stop in stops)
            {
                if(stop.position.x<0||stop.position.x>1||stop.position.y<0||stop.position.y>1)faults.Add($"ChartStop {stop.name} stands off the chart at {stop.position}: each of x and y is 0 to 1");
                at.Add(Place(stop.position));
            }
            for(int i=0;i<at.Count;i++)for(int j=i+1;j<at.Count;j++)
                if(Mathf.Abs(at[i].x-at[j].x)<ApartX&&Mathf.Abs(at[i].y-at[j].y)<ApartY)
                    faults.Add($"ChartStops {stops[i].name} and {stops[j].name} stand in each other's names: keep them {ApartX/Lanterns.width:0.00} apart in x or {ApartY/Lanterns.height:0.00} in y");
            for(int from=0;from<at.Count;from++)
            {
                var seen=new HashSet<int>{from};var queue=new Queue<int>();queue.Enqueue(from);
                while(queue.Count>0){int here=queue.Dequeue();foreach(var way in Ways){int next=Neighbour(at,here,way);if(next>=0&&seen.Add(next))queue.Enqueue(next);}}
                for(int to=0;to<at.Count;to++)if(!seen.Contains(to))faults.Add($"the d-pad can't bring the cursor from ChartStop {stops[from].name} to {stops[to].name}: move one of them");
            }
            return faults;
        }
        /// <summary>For the replay driver's pickRow: which way a thumb presses next to bring the cursor to this stop ("Stop_Lanternwick"), by the links the cursor itself would follow. Null when the
        /// row asked for isn't a stop or the cursor isn't on the chart's page, and the driver presses down as it always has.</summary>
        public static GamepadButton? Toward(string want)
        {
            if(string.IsNullOrEmpty(want)||!IsStop(want))return null;
            var system=EventSystem.current;var selected=system!=null?system.currentSelectedGameObject:null;
            var from=selected!=null?selected.GetComponent<Selectable>():null;if(from==null)return null;
            // Beside the chart, on its page: right from the tab column and up from Back both lead onto it.
            if(!IsStop(from.name))return OnChart(from.navigation.selectOnRight)?GamepadButton.DpadRight:OnChart(from.navigation.selectOnUp)?GamepadButton.DpadUp:(GamepadButton?)null;
            var first=new Dictionary<Selectable,GamepadButton>();var seen=new HashSet<Selectable>{from};var queue=new Queue<Selectable>();queue.Enqueue(from);
            while(queue.Count>0)
            {
                var here=queue.Dequeue();var links=here.navigation;
                foreach(var (next,button) in new[]{(links.selectOnUp,GamepadButton.DpadUp),(links.selectOnRight,GamepadButton.DpadRight),(links.selectOnDown,GamepadButton.DpadDown),(links.selectOnLeft,GamepadButton.DpadLeft)})
                {
                    if(!OnChart(next)||!seen.Add(next))continue;
                    var press=here==from?button:first[here];first[next]=press;
                    if(next.name==want)return press;
                    queue.Enqueue(next);
                }
            }
            return null;
        }
        static bool IsStop(string name)=>name.StartsWith(StopPrefix,StringComparison.Ordinal);
        static bool OnChart(Selectable control)=>control!=null&&IsStop(control.name);

        // ------------------------------------------------------------ the marks, drawn here
        static readonly Dictionary<string,Sprite> glyphs=new();
        /// <summary>A mark drawn in code, not art: its alpha at each point of -1 to 1.</summary>
        static Sprite Glyph(string id,Func<Vector2,float> alpha)
        {
            if(glyphs.TryGetValue(id,out var known)&&known!=null)return known;
            const int size=64;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                pixels[y*size+x]=new Color32(255,255,255,(byte)(255*Mathf.Clamp01(alpha(new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1)))));
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=id,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontUnloadUnusedAsset};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),Mid,100,0,SpriteMeshType.FullRect);sprite.name=id;sprite.hideFlags=HideFlags.DontUnloadUnusedAsset;
            return glyphs[id]=sprite;
        }
        /// <summary>1 inside <paramref name="edge"/>, 0 past it by <paramref name="soft"/>.</summary>
        static float Within(float value,float edge,float soft)=>Mathf.SmoothStep(1,0,Mathf.InverseLerp(edge,edge+soft,value));
        /// <summary>A lit lantern: a hard bright core in a soft halo. An unlit one: its outline. The cursor: a circle round it. The story's next step: a diamond round it.</summary>
        static Sprite Lit=>Glyph("chart-lantern",p=>Mathf.Max(Within(p.magnitude,.24f,.1f),Mathf.Pow(Mathf.Clamp01(1-p.magnitude),2)*.55f));
        static Sprite Unlit=>Glyph("chart-unlit",p=>Within(Mathf.Abs(p.magnitude-.3f),.045f,.06f));
        static Sprite Cursor=>Glyph("chart-cursor",p=>Within(Mathf.Abs(p.magnitude-.84f),.04f,.07f));
        static Sprite Diamond=>Glyph("chart-diamond",p=>Within(Mathf.Abs(Mathf.Abs(p.x)+Mathf.Abs(p.y)-.86f),.045f,.07f));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget()=>glyphs.Clear();
        static readonly Color OpenLight=new(1,.74f,.3f),HereLight=new(1,.94f,.74f),ClosedLight=new(.66f,.47f,.4f),UnlitLine=new(.56f,.64f,.76f,.8f),
            LitRoute=new(1,.78f,.4f,.72f),DimRoute=new(.6f,.7f,.86f,.3f),ObjectiveLine=new(.82f,.92f,1),FlatField=new(.03f,.05f,.09f);

        // ------------------------------------------------------------ the page
        sealed class Mark{public ChartStop stop;public Standing standing;public Vector2 at;public Button button;public TMP_Text label;public Color ink;}
        readonly List<Mark> marks=new();
        ChartStop party,objective;
        /// <summary>The stop asked about, while the page waits for a yes; the last stop the cursor stood on; what the page last said no to.</summary>
        string asked,shown,said="",focus;
        float askedAt=-9;
        Action<string> rebuild;Action close;
        Image cursor;TMP_Text title,status,locale,blurb;
        Button go,stay;
        /// <summary>B steps out of the question before it steps out of the menu: true if there was one, and the stop the cursor goes back to.</summary>
        public bool Back(out string stop){stop=asked!=null?StopPrefix+asked:null;if(asked==null)return false;asked=null;said="";return true;}
        /// <summary>Draws the page into the pause menu's panel, and returns the words for the note under it. <paramref name="wanted"/> is the control that should hold the cursor after this
        /// redraw, as the menu passes it; none means the page has just been turned to, and it starts over on the stop the party is at.</summary>
        public string Build(Transform panel,string wanted,Action<string> redraw,Action shut)
        {
            rebuild=redraw;close=shut;focus=wanted;marks.Clear();go=stay=null;
            if(wanted==null){asked=shown=null;said="";}
            var state=GameServices.Current.State;var stops=Stops;
            party=At(state.zone,stops);objective=Objective(state,stops);
            var asking=asked!=null?Array.Find(stops,s=>s.id==asked):null;if(asking==null)asked=null;
            var art=Resources.Load<Texture2D>(ArtPath);
            if(art!=null)
            {
                var field=new GameObject("ChartField",typeof(RawImage)).GetComponent<RawImage>();field.transform.SetParent(panel,false);field.texture=art;field.raycastTarget=false;
                UiKit.Rect(field.gameObject,Mid,Mid,FieldCentre,FieldSize);
            }
            else{var flat=UiKit.Panel(panel,"ChartField",FlatField);flat.raycastTarget=false;UiKit.Rect(flat.gameObject,Mid,Mid,FieldCentre,FieldSize);}
            // Routes under the lanterns: lit between two places the party has stood in, faint toward one it hasn't.
            var drawn=new HashSet<string>();var motes=panel.gameObject.AddComponent<RouteMotes>();
            foreach(var stop in stops)foreach(string id in stop.routes??Array.Empty<string>())
            {
                var other=Array.Find(stops,s=>s.id==id);if(other==null||!drawn.Add(string.CompareOrdinal(stop.id,id)<0?stop.id+"|"+id:id+"|"+stop.id))continue;
                bool lit=Of(stop,state)!=Standing.Uncharted&&Of(other,state)!=Standing.Uncharted;
                Vector2 a=Place(stop.position),b=Place(other.position),gap=b-a;
                var line=UiKit.SoftBand(panel,"Route",lit?LitRoute:DimRoute);
                UiKit.Rect(line.gameObject,Mid,Mid,(a+b)*.5f,new Vector2(gap.magnitude,lit?8:6)).localRotation=Quaternion.Euler(0,0,Mathf.Atan2(gap.y,gap.x)*Mathf.Rad2Deg);
                // A stop names the place the party first came to it from, so its mote of light runs the way the story did (RouteMotes).
                if(!lit)continue;
                var mote=UiKit.Panel(panel,"RouteMote",HereLight);mote.sprite=Lit;mote.raycastTarget=false;UiKit.Rect(mote.gameObject,Mid,Mid,b,Vector2.one*22);motes.Add(mote,b,a);
            }
            foreach(var stop in stops)
            {
                var mark=new Mark{stop=stop,standing=Of(stop,state),at=Place(stop.position)};bool known=mark.standing!=Standing.Uncharted;
                // The lantern and its name are one thing to point at or stand the cursor on.
                // Nothing of it is drawn, and a pointer must still find it: a mesh culled for being clear takes no hover and no click.
                var holder=UiKit.Panel(panel,StopPrefix+stop.id,Color.clear);holder.canvasRenderer.cullTransparentMesh=false;UiKit.Rect(holder.gameObject,Mid,Mid,mark.at+new Vector2(0,-14),new Vector2(190,84));
                mark.button=holder.gameObject.AddComponent<Button>();mark.button.targetGraphic=holder;mark.button.transition=Selectable.Transition.None;mark.button.interactable=asking==null;HoverSelect.Attach(mark.button);
                var lantern=UiKit.Panel(holder.transform,"Lantern",mark.standing switch{Standing.Here=>HereLight,Standing.Open=>OpenLight,Standing.Closed=>ClosedLight,_=>UnlitLine});
                lantern.sprite=known?Lit:Unlit;lantern.raycastTarget=false;UiKit.Rect(lantern.gameObject,Mid,Mid,new Vector2(0,14),Vector2.one*(mark.standing==Standing.Here?56:46));
                mark.ink=mark.standing==Standing.Here||mark.standing==Standing.Open?UiKit.LooseTextColor:UiKit.DimTextColor;
                mark.label=UiKit.Legible(UiKit.Text(holder.transform,"Label",known?Name(stop):"Uncharted",24,mark.ink));mark.label.textWrappingMode=TextWrappingModes.NoWrap;
                UiKit.Rect(mark.label.gameObject,Mid,Mid,new Vector2(Inward(mark.label,Name(stop),mark.at.x),-24),new Vector2(320,32));
                mark.button.onClick.AddListener(()=>UiSounds.Run(()=>Choose(mark)));
                holder.gameObject.AddComponent<SelectNote>().selected=()=>Show(mark);
                marks.Add(mark);
            }
            foreach(var mark in marks)
            {
                if(mark.stop==objective){var line=UiKit.Panel(panel,"ChartObjective",ObjectiveLine);line.sprite=Diamond;line.raycastTarget=false;UiKit.Rect(line.gameObject,Mid,Mid,mark.at,Vector2.one*70);}
                if(mark.stop!=party)continue;
                var arrow=UiKit.Panel(panel,"ChartParty",UiKit.GoldColor);arrow.sprite=UiSkin.Pointer();arrow.preserveAspect=true;arrow.raycastTarget=false;
                UiKit.Rect(arrow.gameObject,Mid,Mid,mark.at+new Vector2(0,44),Vector2.one*26).localRotation=Quaternion.Euler(0,0,180);
            }
            cursor=UiKit.Panel(panel,"ChartCursor",UiKit.GoldColor);cursor.sprite=Cursor;cursor.raycastTarget=false;cursor.enabled=false;UiKit.Rect(cursor.gameObject,Mid,Mid,Vector2.zero,Vector2.one*60);
            // The stop under the cursor, in the field's quiet lower band.
            title=UiKit.Heading(panel,"ChartStopName","",34,UiKit.GoldColor,TextAlignmentOptions.Left);UiKit.Rect(title.gameObject,Mid,Mid,new Vector2(0,-124),new Vector2(800,46));
            status=UiKit.Text(panel,"ChartStopStatus","",24,UiKit.FrostColor,TextAlignmentOptions.Right);UiKit.Rect(status.gameObject,Mid,Mid,new Vector2(540,-124),new Vector2(400,34));
            locale=UiKit.Text(panel,"ChartStopWhere","",24,UiKit.FrostColor,TextAlignmentOptions.Left);UiKit.Rect(locale.gameObject,Mid,Mid,new Vector2(170,-164),new Vector2(1140,32));
            blurb=UiKit.Text(panel,"ChartStopBlurb","",24,UiKit.TextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(blurb.gameObject,Mid,Mid,new Vector2(170,-212),new Vector2(1140,62));
            // A refusal stands where every page's does, under the page and over its note.
            var refusal=UiKit.Text(panel,"ChartSaid",said,24,UiKit.TextColor,TextAlignmentOptions.TopLeft);UiKit.Rect(refusal.gameObject,Mid,Mid,new Vector2(170,-300),new Vector2(1220,30));
            var home=Home();if(home!=null)Show(home);
            int charted=marks.FindAll(m=>m.standing!=Standing.Uncharted).Count;
            string standing=(party==null?$"You're in {DisplayNames.Zone(state.zone)}":party.zone==state.zone?$"You're at {Name(party)}":$"You're in {DisplayNames.Zone(state.zone)}, out from {Name(party)}")+$"  ·  {charted} of {marks.Count} places charted";
            if(asking==null)return marks.Count==0?"The chart is blank.":standing+"\nSelect a lit place to travel there. The arrow is the party, the diamond the story's next step.";
            // The question, in the field's quiet lower band where the place under the cursor is described: one press asks and the next goes, and the stops don't answer while it's up.
            // It was a frame over the middle of the chart, and covered the names it was asking among ("Sorrel Out…", "…ncharted"). The band names the place asked about already.
            var about=marks.Find(m=>m.stop==asking);if(about!=null)Show(about);
            blurb.text="";status.text="TRAVEL HERE?";
            go=UiKit.Row(panel,"Travel","Travel — "+Name(asking),()=>Go(asking),26,TextAlignmentOptions.Center);UiKit.Rect(go.gameObject,Mid,Mid,new Vector2(-125,-212),new Vector2(560,56));
            stay=UiKit.Row(panel,"Stay","Stay in "+DisplayNames.Zone(state.zone),()=>{asked=null;rebuild(StopPrefix+asking.id);},26,TextAlignmentOptions.Center);UiKit.Rect(stay.gameObject,Mid,Mid,new Vector2(465,-212),new Vector2(560,56));
            return standing+$"\nTravelling leaves {DisplayNames.Zone(state.zone)} as it stands. The game saves when you arrive at {Name(asking)}.";
        }
        /// <summary>After the menu has linked its own controls: the stops' links, the ways on and off the chart, and the cursor. Left of the westernmost stop is the page's tab, as on every
        /// page; below the southernmost is Back.</summary>
        public void Link(IList<Selectable> tabs,int own,Selectable back)
        {
            var home=Home();Selectable first=go!=null?go:home?.button;if(first==null)return;
            if(go!=null)
            {
                // Two rows, and any direction from one is the other.
                foreach(var (row,other) in new[]{(go,stay),(stay,go)})row.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=other,selectOnDown=other,selectOnLeft=other,selectOnRight=other};
            }
            else
            {
                var at=marks.ConvertAll(m=>m.at);
                Selectable Next(int from,Vector2 way){int next=Neighbour(at,from,way);return next<0?null:marks[next].button;}
                for(int i=0;i<marks.Count;i++)
                    marks[i].button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=Next(i,Vector2.up),selectOnRight=Next(i,Vector2.right),selectOnDown=Next(i,Vector2.down)??back,selectOnLeft=Next(i,Vector2.left)??tabs[own]};
            }
            foreach(var tab in tabs){var links=tab.navigation;links.selectOnRight=first;tab.navigation=links;}
            var under=back.navigation;under.selectOnUp=stay!=null?stay:first;back.navigation=under;
            Selectable want=null;
            if(focus!=null)
            {
                if(go!=null)want=focus==stay.name?stay:focus==go.name?go:null;
                else foreach(var mark in marks)if(mark.button.name==focus)want=mark.button;
            }
            // The cursor was on a tab or on Back and the menu has put it back there: leave it. Otherwise it belongs on the chart.
            bool beside=focus!=null&&(focus==back.name||focus.StartsWith("Tab_",StringComparison.Ordinal));
            if(want==null&&!beside)want=first;
            if(want!=null)EventSystem.current.SetSelectedGameObject(want.gameObject);
        }
        /// <summary>Where the cursor starts: the stop it was last on, else where the party is, else where the story goes next, else the first place charted.</summary>
        Mark Home()=>marks.Find(m=>m.stop.id==shown)??marks.Find(m=>m.stop==party)??marks.Find(m=>m.stop==objective)??marks.Find(m=>m.standing!=Standing.Uncharted)??(marks.Count>0?marks[0]:null);
        static string Services(ChartStop stop)
        {
            var has=new List<string>();if(stop.shop)has.Add("Shop");if(stop.bench)has.Add("Bench");if(stop.repair)has.Add("Repair");
            return has.Count==0?"No services":string.Join(", ",has);
        }
        void Show(Mark mark)
        {
            if(title==null)return;
            shown=mark.stop.id;var stop=mark.stop;bool known=mark.standing!=Standing.Uncharted;
            foreach(var other in marks)if(other.label!=null)other.label.color=other==mark?UiKit.GoldColor:other.ink;
            if(cursor!=null){cursor.enabled=true;cursor.rectTransform.anchoredPosition=mark.at;}
            title.text=known?Name(stop):"Uncharted";
            locale.text=known?stop.region+"  ·  "+Services(stop):"The chart only holds places you've stood in.";
            blurb.text=!known?"":mark.standing==Standing.Closed?"<color=#FFB08A>"+Refusal(stop)+"</color>":stop.blurb;
            var says=new List<string>();
            if(mark.standing==Standing.Here)says.Add("YOU ARE HERE");else if(stop==party)says.Add("THE PARTY IS NEAR");
            if(mark.standing==Standing.Closed)says.Add("CLOSED");
            if(stop==objective)says.Add("NEXT STEP");
            status.text=string.Join("  ·  ",says);
        }
        /// <summary>A on a stop: a refusal says why, and anything else asks first.</summary>
        void Choose(Mark mark)
        {
            string why=Refusal(mark.stop);
            if(why!=null){said=why;UiSounds.Error();rebuild(mark.button.name);return;}
            asked=mark.stop.id;askedAt=Time.unscaledTime;said="";rebuild("Travel");
        }
        void Go(ChartStop stop)
        {
            // The press that asked is often pressed twice: the answer isn't taken until a thumb has had time to read the question.
            if(Time.unscaledTime-askedAt<FirstPress.Hold)return;
            asked=null;string why=Refusal(stop);
            if(why!=null){said=why;UiSounds.Error();rebuild(StopPrefix+stop.id);return;}
            close();Travel(stop);
        }
    }
}
