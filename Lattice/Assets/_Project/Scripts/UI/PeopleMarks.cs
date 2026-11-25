using System.Collections.Generic;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Someone with a new conversation waiting shows it: their name label turns gold behind a gold pip, to be seen across a square, and
    /// the prompt says so up close. Five quests are only ever offered on a second talk, and Corra's offer stands where a player expects her
    /// shop, with nothing to suggest either. World can't see TextMeshPro, so the mark lives here and World doesn't change.</summary>
    public sealed class PeopleMarks:MonoBehaviour
    {
        /// <summary>Beats that stay true after they've been heard: the person is only repeating themselves, and a mark on them would never clear.</summary>
        static readonly HashSet<string> Repeats=new(){"AbriLanternRepeat","AbriLanternWaiting","AbriLanternAfter","PellLampAfter","AumiRelaysLit","MiraRepeat",
            "AbriSeedRoadRepeat","AbriSeedRoadCharted","AbriSeedRoadWaiting","AbriDinnerServed","PellLaneRepeat","PellAfterVote",
            "AbriReedRoadRepeat","AbriReedRoadCharted","AbriReedRoadWaiting","AbriManyHands","PellVeilRepeat","PellManyHands","OnaPimBack","MaruOnaWaits",
            // Many Hands (chapter 5): every person's repeat and after-dinner line aboard, the courier pointer, and the dinner's optional talks, which stay true for their window.
            "AbriHandsRepeat","AbriHandsPost","AbriCourier","AbriDinnerTalk","PellHandsRepeat","PellHandsPost","RuvaHandsRepeat","RuvaHandsPost","MaruHandsRepeat","MaruHandsPost","MaruDinnerTalk",
            "BinnaHandsRepeat","BinnaHandsPost","BinnaDinnerTalk","TeoHandsRepeat","TeoHandsPost","TeoDinnerTalk","JaskFoundRepeat","JaskGardenHand","JaskFerryHand","JaskLampHand","JaskHandsPost",
            "JaskDinnerTalk","HaddaHandsRepeat","HaddaHandsPost","CorraHandsRepeat","CorraHandsPost","DovHandsRepeat","DovHandsPost","OnaHandsRepeat","OnaHandsPost","PimHandsRepeat","PimHandsPost",
            "GaroHandsRepeat","GaroHandsPost","DabbHandsRepeat","DabbHandsPost","IloHandsRepeat","IloHandsPost","PatrolRepeat",
            // The Long Sleep (chapter 6): the lines that hold for the chapter's window aboard, Hask's count while the strays are out, and the herders' words aboard after it.
            "AbriThirtySix","IloLastlightEntry","HaskStraysWaiting","AbriCutters","HaskAboard","IruAboard",
            // The Cage Makers (chapter 7, manifest section 8): Abri's line while the party is away, Ishen's and Lume's lines at the camp and aboard, Ruva's and Pell's for good once their side story is done.
            "AbriTooManyBooks","IshenCamp","IshenOrielPost","LumePost","IshenAboard","RuvaGardener","PellOrielLantern",
            // Harrowgate (chapter 8, manifest section 8.2): the lines that hold for their window at the camp, the gate, the kitchen and in town, and the words aboard after it.
            "AbriTwentySeven","AbriThreeWords","TovraRepeat","BrekkCampRepeat","HessaGateRepeat","OdrunRepeat","OdrunShop","BrekkKitchen","PipOathDay","PipPost",
            "TovraAboard","BrekkAboard","AbriSmiths","DraskAboard","PellAumiAnswers",
            // The House That Can Leave (chapter 9, manifest section 8.2): the lines that hold aboard, on the Crawl and on the Decks for their window.
            "OdaAboard","AbriNamesOnTheWall","DorrowRepeat","TamFirst","JunRepeat","MiraCratedRepeat","HalCrated","NeveFlyingAStreet","SurvivorDecks","OdaDecks","IloDecks","DorrowListens",
            // A Route With Room To Turn Back (chapter 10, manifest section 8.2): the lines that hold aboard and in the epilogue, the four messages, and every visit and extra in the
            // neighbourhood (each is said again when asked again).
            "AbriFortiethDay","DraskBeforeTheField","MeretAccount","DraskWatches","JaskNewSky","PipRota","TamSkyGarden",
            "IruTernsMessage","PellAumisMessage","RuvaSennsMessage","PimQuillsMessage",
            "AbriHomecoming","TeoHomecoming","MaruHomecoming","HaskHomecoming","PellHomecoming","TovraHomecoming","OdaHomecoming","DorrowHomecoming","MiraHomecoming",
            "MiraHomeCrystals","NeveHomeParcels","OdaHomeStamp","SurvivorHomeLamps","PellHomeMothersLamp","IshenHomeShelves","BrekkHomeAwning","BinnaHomeBaskets","RuvaHomePost",
            "JaskHomeThree","TeoHomeTags","MaruHomeHarbourLamp","DovHomeSmokehouse","DovHomeBell","PimHomeKey","CorraHomeKettles","JaskHomeFare","IloHomeBook","HaskHomeStrays",
            "IruHomeLamps","IdraHomeCrew","RuvaHomeShoot","IshenHomeMoons","PellHomeOriel","TovraHomePikes","AbriHomeGlass","BrekkHomeKettle","PipHomeFile","MiraHomeTable",
            "HalHomeBenches","NeveHomeSocks","TamHomeFrame",
            // Ruva's motion nine, her line in the square every time: news until heard once (SideStories.HeardFlagFor).
            "RuvaPassageRules"};
        /// <summary>How tall a name stands on a 1080p screen, marked or not (D388: grown to 32 for its mark, with the mark half as tall again, PELL's tag was 190 by 125 pixels, larger
        /// than his head and against Taren's side, s13 079); and how far over the head it hangs, in metres.</summary>
        const float NamePixels=26,OverHead=.32f;
        /// <summary>The room the mark takes before the name, in the name's own size, so a marked name's tag is the height of any other's and only as much wider as its mark (D396).</summary>
        const string Room=Cut+Gap,Cut="!",Gap="<space=0.5em>";
        /// <summary>How far the pip stands past the capitals above and below, and past the '!' on each side, in em: wide enough that the gold shows round the '!' and its dark
        /// halo (at .06 a side, with a bold '!', the pip was a gold rim round a dark stroke at play size).</summary>
        const float PipOver=.15f,PipSide=.26f;
        /// <summary>The mark (D396): a solid pip in the HUD's gold for what is waiting (the objective's arrow and its 'Here'), a dark '!' cut in it, before the name inside the
        /// name's own tag, from a little under the baseline to a little over the capitals. Size told news until D388 (grown to 32 px with a '!' half as tall again, PELL's tag was
        /// 190 by 125, s13 079); in the name's own size Cinzel's '!' was a stroke three pixels wide at play size. The pip is TextMeshPro's mark like the tag and comes after it
        /// in the text, so SignText's marks pass lays it on the tag, and the letters on both. TextMeshPro writes a mark's colour as it is given, not into linear light as it
        /// does a letter's, so the gold is given in linear light and shows as the HUD's.</summary>
        public static string Pip(TextMeshPro text)
        {
            var face=text!=null&&text.font!=null?text.font.faceInfo:default;
            float per=face.pointSize>0?face.scale/(float)face.pointSize:0,ascent=face.ascentLine*per,descent=face.descentLine*per,cap=face.capLine*per;
            if(per<=0||cap<=0||ascent<=cap){ascent=1;descent=-.3f;cap=.7f;}
            // TextMeshPro's mark runs from the line's descender to its ascender, past them by its padding (a hundredth of an em each): taken in to the capitals' band.
            var invariant=System.Globalization.CultureInfo.InvariantCulture;
            string side=(PipSide*100).ToString("0.#",invariant),top=((cap+PipOver-ascent)*100).ToString("0.#",invariant),bottom=((descent+PipOver)*100).ToString("0.#",invariant);
            var gold=QualitySettings.activeColorSpace==ColorSpace.Linear?UiKit.GoldColor.linear:UiKit.GoldColor;
            return $"<mark=#{ColorUtility.ToHtmlStringRGB(gold)}FF padding=\"{side},{side},{top},{bottom}\"><color=#05080E>{Cut}</color></mark>{Gap}";
        }
        /// <summary>The pip's colour as TextMeshPro writes it into the mesh: what a test finds there.</summary>
        public static Color32 PipColor=>QualitySettings.activeColorSpace==ColorSpace.Linear?UiKit.GoldColor.linear:UiKit.GoldColor;
        sealed class Label{public TextMeshPro text;public string name,shows;public Color color;public float size;public bool marked,placed;public Npc npc;}
        readonly Dictionary<Npc,Label> labels=new();
        readonly List<Label> shown=new();readonly List<TextMeshPro> upright=new();readonly HashSet<string> names=new();
        float next;bool everyone;
        public static bool HasNews(Npc npc)
        {
            var service=GameServices.Current;
            if(npc==null||service==null||!string.IsNullOrEmpty(npc.holdNode))return false;
            string node=SideStories.NodeFor(npc.speaker,service.State);
            // A repeat the story waits behind is news until it has been heard once: without the mark nothing says Abri's last word of chapter 2 leads to chapter 3.
            return node!=null&&(!Repeats.Contains(node)||SideStories.HeardFlagFor(node) is string heard&&!service.Flags.GetBool(heard));
        }
        /// <summary>What the prompt adds up close. A hail reaches the same person as a talk.</summary>
        public static string PromptNote(InteractionPrompt prompt)
        {
            var npc=prompt as Npc;if(prompt is HailPoint hail)npc=hail.contact;
            return HasNews(npc)?"  ·  has something to say":"";
        }
        void Update()
        {
            if(Time.unscaledTime<next||GameServices.Current==null)return;next=Time.unscaledTime+.25f;
            // Everyone's name from the first frame, their talk open or not: at the meeting Jun, Dace and Oda waited their turn with their talk shut, and their names lay on the
            // floor as the builder had tilted them, turned with their bodies, DACE upside down by the grate (s57/055). One whose body isn't out yet is put over its head when it is.
            if(!everyone)
            {
                everyone=true;
                foreach(var npc in FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    LabelOf(npc);
                    // And every name hanging over a person is held upright, shown yet or not: a second Jun at the meeting, hidden as the zone loaded, kept the builder's tilt.
                    foreach(var name in npc.GetComponentsInChildren<TextMeshPro>(true))if(name.name.StartsWith("Sign_")&&!upright.Contains(name))upright.Add(name);
                    names.Add("Sign_"+DisplayNames.Speaker(npc.speaker));
                }
                // And a sign named for someone and hung apart from them (Jun's at her stall on the day): it reads as their name, so it faces the camera as their name does.
                foreach(var text in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(names.Contains(text.name)&&!upright.Contains(text))upright.Add(text);
            }
            foreach(var label in shown)
                if(!label.placed&&label.text!=null&&label.npc!=null&&label.npc.gameObject.activeInHierarchy){label.placed=true;if(label.text.transform.IsChildOf(label.npc.transform))OverTheHead(label.text,label.npc);}
            foreach(var prompt in InteractionPrompt.Active)
            {
                if(prompt is not Npc npc)continue;
                var label=LabelOf(npc);if(label==null)continue;
                bool news=HasNews(npc);if(news==label.marked)continue;
                // The mark stands before the name (backed or not), the name's words as they were (QaUiFlowTests: the mark doesn't take the name's place); a backed name's one tag
                // runs under both.
                label.marked=news;label.text.fontSize=label.size;
                label.text.text=Marked(label.text,label.name,label.shows,news);label.text.color=news?UiKit.GoldColor:label.color;
            }
        }
        /// <summary>A name's words with its mark or without: <paramref name="shows"/> is the name as drawn (backed by its tag, or as it stands), <paramref name="name"/> its bare words.</summary>
        public static string Marked(TextMeshPro text,string name,string shows,bool news)=>!news?shows:shows!=name?SignStyle.Backing(text,Room+name)+Pip(text)+name:Pip(text)+shows;
        /// <summary>A name reads level whichever way its person faces: as a child it turned with them, so CORRA stood on end.</summary>
        void LateUpdate()
        {
            var view=Camera.main;if(view==null)return;var facing=view.transform.rotation;
            for(int i=shown.Count-1;i>=0;i--)
            {
                var text=shown[i].text;
                if(text==null)shown.RemoveAt(i);else text.transform.rotation=facing;
            }
            for(int i=upright.Count-1;i>=0;i--)
            {
                var text=upright[i];
                if(text==null)upright.RemoveAt(i);else if(text.isActiveAndEnabled)text.transform.rotation=facing;
            }
        }
        /// <summary>The label the camera can see: a hailed person's own is switched off with their body, and a sign named for them hangs beside their ship.</summary>
        Label LabelOf(Npc npc)
        {
            if(labels.TryGetValue(npc,out var known))return known;
            TextMeshPro found=null;
            foreach(var own in npc.GetComponentsInChildren<TextMeshPro>(true))if(Shown(own)){found=own;break;}
            if(found==null)
            {
                string sign="Sign_"+DisplayNames.Speaker(npc.speaker);
                foreach(var other in FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(other.name==sign&&Shown(other)){found=other;break;}
            }
            Label label=null;
            if(found!=null)
            {
                // Drawn nearer the camera than it hangs, like a sign (SignStyle.Reach): a post across MOTHER ODRUN, an awning over Ishen's name. In a full dark halo, so it reads
                // over a notice board's lines and on snow.
                found.fontSharedMaterial=SignStyle.NameFace(found.font)??UiKit.LegibleMaterial(found.font);
                float scale=Mathf.Max(.01f,found.transform.lossyScale.y),size=Mathf.Max(found.fontSize,UiKit.WorldFontSize(NamePixels)/scale);found.fontSize=size;
                // Backed by a dark tag in its own text (SignStyle.Backed): ORRIN read as part of the notice board behind him, OLD TERN's gold was faint on snow. Only a name hanging
                // over its own person: a sign named for someone and hung elsewhere (beside a hailed ship) keeps its words as they are, since a sign is known for a name by its words.
                string words=found.text,shows=found.transform.IsChildOf(npc.transform)?SignStyle.Backed(found,words):words;found.text=shows;
                // A name goes with the signs: '! Keeper Abri' printed through the box, and 'ORRIN' was cut in half by the vitals.
                if(!found.TryGetComponent<SignFade>(out var fade)){fade=found.gameObject.AddComponent<SignFade>();fade.bossOnly=true;}
                fade.keepsTurn=false;fade.nameTag=true;
                bool placed=npc.gameObject.activeInHierarchy;
                if(placed&&found.transform.IsChildOf(npc.transform))OverTheHead(found,npc);
                label=new Label{text=found,name=words,shows=shows,color=found.color,size=size,npc=npc,placed=placed};shown.Add(label);
            }
            labels[npc]=label;return label;
        }
        /// <summary>Over this body's own head. A fixed 2.8 m left a name floating against the wall behind a short person, and in the shoulder of a tall one.</summary>
        static void OverTheHead(TextMeshPro label,Npc npc)
        {
            float top=float.MinValue;
            foreach(var body in npc.GetComponentsInChildren<Renderer>())
                if(!body.transform.IsChildOf(label.transform)&&body is MeshRenderer or SkinnedMeshRenderer)top=Mathf.Max(top,body.bounds.max.y);
            if(top>float.MinValue){var at=npc.transform.position;label.transform.position=new Vector3(at.x,top+OverHead,at.z);}
        }
        static bool Shown(TextMeshPro text){var view=text.GetComponent<Renderer>();return view!=null&&view.enabled;}
    }
}
