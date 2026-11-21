using System.Collections.Generic;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Someone with a new conversation waiting shows it: their name label turns gold and grows, to be seen across a square, and
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
        /// <summary>How tall a name stands on a 1080p screen, and how tall when it's marked; and how far over the head it hangs, in metres.</summary>
        const float NamePixels=26,MarkPixels=32,OverHead=.32f;
        /// <summary>A heavy mark, not a tick: thin Cinzel's '!' read as a stray stroke before the name.</summary>
        const string Mark="<size=150%><b>!</b></size>  ";
        sealed class Label{public TextMeshPro text;public string name;public Color color;public float size,markSize;public bool marked;}
        readonly Dictionary<Npc,Label> labels=new();
        readonly List<Label> shown=new();
        float next;
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
            foreach(var prompt in InteractionPrompt.Active)
            {
                if(prompt is not Npc npc)continue;
                var label=LabelOf(npc);if(label==null)continue;
                bool news=HasNews(npc);if(news==label.marked)continue;
                label.marked=news;label.text.text=news?Mark+label.name:label.name;label.text.color=news?UiKit.GoldColor:label.color;
                label.text.fontSize=news?label.markSize:label.size;
            }
        }
        /// <summary>A name reads level whichever way its person faces: as a child it turned with them, so CORRA stood on end.</summary>
        void LateUpdate()
        {
            var view=Camera.main;if(view==null)return;var facing=view.transform.rotation;
            for(int i=shown.Count-1;i>=0;i--)
            {
                var text=shown[i].text;
                if(text==null)shown.RemoveAt(i);else text.transform.rotation=facing;
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
                found.fontSharedMaterial=UiKit.LegibleMaterial(found.font);
                // A name goes with the signs: '! Keeper Abri' printed through the box, and 'ORRIN' was cut in half by the vitals.
                if(!found.TryGetComponent<SignFade>(out _))found.gameObject.AddComponent<SignFade>().bossOnly=true;
                float scale=Mathf.Max(.01f,found.transform.lossyScale.y),size=Mathf.Max(found.fontSize,UiKit.WorldFontSize(NamePixels)/scale);found.fontSize=size;
                if(found.transform.IsChildOf(npc.transform))OverTheHead(found,npc);
                label=new Label{text=found,name=found.text,color=found.color,size=size,markSize=Mathf.Max(size,UiKit.WorldFontSize(MarkPixels)/scale)};shown.Add(label);
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
