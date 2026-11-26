using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A word lying in the world keeps out of the way of what's on the screen. It goes for a conversation, a menu or the chapter's card, which printed it through the box and under the panels. It goes
    /// for a boss bar, which sits over the lower middle of the screen. It goes while it's cut, by a fixed plate or by the screen's edge: half a word under the hint bar read as a fault.
    /// And a floor sign thins out under the hero who's standing on it, and only then: it was a ghost 2 m off, at the very moment the party had reached the place it names.
    /// <para>Since the November 20 capture: it goes at once, not over a tenth of a second, for a conversation, a menu, the card or a boss's bar (a still caught GREAT LANTERN between the
    /// bar and the hints, and ILO half gone through Ilo's own talk); it keeps a margin from the screen's edge and from a plate (KEEPER ABRI eight pixels from the edge, PIM against the
    /// zone's tag); a place's name steps aside while a fight is on round the party (CUT CORRIDOR under a scavenger's telegraphs); a body over a floor word's letters takes it down to a
    /// trace, whoever's body it is and wherever they stand (LAST LANTERN through Taren and Idra, at a third); a word too small on the screen to read isn't drawn (the Orrery's gate
    /// names as specks in the wide view); and a sign keeps reading level whatever turns what it hangs on.</para>
    /// <para>Since the November 22 capture (D356): a word is read or gone, never between. It goes at once and comes back at once, after a quarter second clear of whatever took it
    /// (a plate, the screen's edge, a speck's size, a fight, a body over its letters), so a word on the edge of one doesn't flicker. Fading in a tenth of a second and back over a
    /// third, the capture caught names and floor words at a fifth to a half of their strength again and again: RUVA OKH, KEEPER ABRI and ILO the frame a conversation closed
    /// (s30 277), TEO MARROW (s31 125), FIRST ARCH, STILT MARKET, LAMP GALLERY, PILOTS' STAGE and LOWER GALLERY as the party ran past a panel's edge (s20 019, s25 090, s30 118,
    /// s27 013, s26 512), HADDA by the objective (s22 014). The one level between is the trace a body over a floor word's letters leaves. And a hanging sign steps aside for a
    /// hero's body over its letters as a floor word does (PRESSURE LIFT across Sela in the lift's door, s16 144); a person's name doesn't.</para>
    /// <para>Since the November 22 night capture (D365): a body over a word's letters takes it away too, no trace. The trace was .08 of the word's alpha, and the checks counted
    /// anything under a tenth as gone; but a word is blended in linear light, so .08 of white over a dark floor or open space lifts it by about .07, which the screen shows as
    /// a third of the way to the letters: THE BASIN beside Sela (s21 327), LOWER GALLERY under Taren (s26 513), MARROW-3 (s20 346), THE WEIGHER under the ship (s42 216),
    /// ARRIVALS / DOCK OFFICE under a walker (s60 189) were all that trace, measured in the stills at .06 to .09 over their ground. Why a word is gone is kept
    /// (<see cref="GoneFor"/>), which is how a test tells a word gone for a body from one cut by a plate.</para></summary>
    [DefaultExecutionOrder(910)]
    public sealed class SignFade:MonoBehaviour
    {
        /// <summary>A tenth of a second. At a fifth, slower than the plates' own fade, a sign that had started to go was still there at half strength under the skill icons ('E RY ALL') and through a plate that had stepped aside.
        /// Now only how far ahead a moving word is looked for: it goes at once.</summary>
        const float AsidePerSecond=10;
        /// <summary>How long a word stays gone, or a trace, after what took it has let it be.</summary>
        const float BackAfter=.25f;
        /// <summary>A person's name over their head (PeopleMarks): it keeps to its person, and no body takes it away.</summary>
        [System.NonSerialized] public bool nameTag;
        /// <summary>What has taken it, while it's gone: "away" (a conversation, a menu, the card, a boss's bar), "cut" (a plate or the screen's edge), "speck" (under the type
        /// floor), "fight", or "body" (a body over its letters). Empty while it's read.</summary>
        public string GoneFor{get;private set;}="";
        float goneUntil=-1;
        /// <summary>How far ahead a moving sign is looked for, as a share of the screen's height: enough for a run and the camera catching up, and no more, since a jump isn't a heading.</summary>
        const float MostAhead=.12f;
        static readonly int FadeId=Shader.PropertyToID("_Fade");
        static MaterialPropertyBlock block;
        /// <summary>A body over a floor word's letters: legs through the middle of a word read 'LO R CHAMBER' (E4-hushwell-03), and two heroes standing on LAST LANTERN left it at a third,
        /// through both of them (s43/044). It goes at once, and comes back at once a moment after the body has gone (a trace of .08 read as a ghost in the player, D365). A body
        /// counts when it covers a quarter of the letters' height: one standing behind the word, its feet above it on the screen, or past its end, is beside it.</summary>
        const float BodyTall=1.8f,BodyHalfWide=.4f,LeastOver=.25f;
        /// <summary>A sign below this lies on the floor under the ships, and how much bigger than made a held sign may grow at most. In a flight zone a lying sign grows only as far as its
        /// letters stay over the floor: grown three times at the farthest pull, SALVAGE EDDY's outer letters went into the Gullet's curving bed (QA-037).</summary>
        const float LyingBelow=1,MostLying=3.4f,GrowthStep=.1f;
        /// <summary>The room a word keeps from the screen's edge and from a plate, in 1080p pixels.</summary>
        const float Margin=16;
        /// <summary>Under this share of the type floor (an em of <see cref="SignStyle.TypeFloorPixels"/> at 1080p) a word isn't drawn: drawn back fourteen times for the Orrery's wide view,
        /// a held gate name grown its most was an em of ten pixels.</summary>
        const float LeastOfTheFloor=.75f;
        /// <summary>A sign that hangs over the party, a person's name, or a sign lying below the ships: nobody stands on it, so no hero moves it, and a fight doesn't take it away.</summary>
        public bool bossOnly;
        /// <summary>Whether it keeps reading level as it was laid, whatever turns what it hangs on. A person's name is turned to the camera by PeopleMarks instead, which says so.</summary>
        [System.NonSerialized] public bool keepsTurn;
        TextMeshPro text;Renderer plate;float full,halfWide,halfHigh,held=1,mostGrowth=-1;string measured;
        bool holdsSize,tracked;Vector3 sized;Vector2 lastAt;Quaternion reading;
        static readonly RaycastHit[] floors=new RaycastHit[8];
        /// <summary>How far this sign may grow to hold its size: as far as the camera asks, unless it lies on a floor.</summary>
        public float MostGrowth{get{if(mostGrowth<0)mostGrowth=MeasureGrowth();return mostGrowth;}}
        void Awake()
        {
            text=GetComponent<TextMeshPro>();full=text.alpha;sized=transform.localScale;reading=transform.rotation;
            FindPlate();
            var zone=ZoneController.Current;bool named=GetComponentInParent<Npc>(true)!=null;
            holdsSize=zone!=null&&zone.Flight&&zone.definition!=null&&zone.definition.cameraProfile!=null&&!named;
            // A person's name faces the camera by PeopleMarks; a sign reads level as SignStyle laid it, whatever it hangs on turns (a district flying out, a seated crowd).
            keepsTurn=!named;nameTag|=named;
        }
        /// <summary>Its plate, when one was put behind it after it woke.</summary>
        public void FindPlate(){var backing=transform.Find(SignStyle.PlateName);plate=backing!=null?backing.GetComponent<Renderer>():null;}
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var lead=party.Active.transform.position;var input=GameInput.Current;var view=Camera.main;
            if(keepsTurn&&transform.rotation!=reading)transform.rotation=reading;
            if(holdsSize&&view!=null)HoldSize(view,lead.y);
            bool away=input!=null&&input.Blocked||QuestToast.CardUp||BossController.Focus(lead)!=null;
            // D416: the prompt is saying its words. 'LAND — THE SKYFIELDS' stood on its plate at the top while 'A  Land — The Skyfields' stood in the prompt in the same frame
            // (nov24-whole s38 110): the place's name once, where the button is.
            bool said=!away&&!nameTag&&SaidByThePrompt(text.text);
            // Read or gone. Away (a conversation, a menu, the card, a boss's bar) it's gone for that frame and back the frame it ends; set aside by where it stands, or by a body
            // over its letters, it stays gone a moment after, so a word riding the edge of a plate or a body doesn't flicker.
            float now=Time.unscaledTime;
            if(away){tracked=false;GoneFor="away";}
            // D424: and a body's or a plate's hold from before goes with it, so the frame the prompt stops saying it the sign is read again or taken by what takes it now;
            // a hold that outlived the prompt left it gone, still marked "said" (QaNov24LabelTests, d424a).
            else if(said){tracked=false;GoneFor="said";goneUntil=-1;}
            else
            {
                string took=Cut(view)?"cut":Speck(view)?"speck":!bossOnly&&Fighting(lead)?"fight":null;
                if(took==null&&now>=goneUntil&&!nameTag&&Covered(view,bossOnly))took="body";
                if(took!=null){goneUntil=now+BackAfter;GoneFor=took;}
                else if(now>=goneUntil)GoneFor="";
            }
            float shown=away||said||now<goneUntil?0:1;
            float alpha=full*shown;
            if(Mathf.Abs(text.alpha-alpha)<=.01f&&alpha>0)return;
            if(text.alpha==alpha)return;
            text.alpha=alpha;
            if(plate==null)return;
            block??=new MaterialPropertyBlock();
            plate.GetPropertyBlock(block);block.SetFloat(FadeId,shown);plate.SetPropertyBlock(block);
        }
        /// <summary>A flight's camera draws back far past what a zone's signs were sized for: 60 m on the Tallow line against the 26 they stand at, and the Halo's warp sign went to 12 px letters just as the player looked for the way out. A sign holds its size on the screen. How far back goes by the camera's height over the hero, so a sign up the screen isn't taken for one further off.</summary>
        void HoldSize(Camera view,float plane)
        {
            var zone=ZoneController.Current;if(zone==null)return;var look=zone.definition.cameraProfile;
            float wanted=Mathf.Max(1,(view.transform.position.y-plane)/(look.distance*Mathf.Sin(look.pitch*Mathf.Deg2Rad)));
            // At rest it's exactly the size it was made: the camera's height settles a hair over, and the step rule below would have left it up to 3% large for good.
            if(wanted<1.01f)wanted=1;
            wanted=Mathf.Min(wanted,MostGrowth);
            if(Mathf.Abs(wanted-held)<=held*.03f&&(wanted>1||held==1)&&wanted!=MostGrowth)return;
            if(wanted==held)return;
            held=wanted;transform.localScale=sized*held;
        }
        /// <summary>The most a held sign grows before its letters go into the floor it lies on: grown about its middle, a tilted sign's lower letters come down toward the floor, and on a
        /// curving bed its ends meet the floor first. A sign standing clear of the floor, a warp's or a hanging one, grows as far as the camera asks.</summary>
        float MeasureGrowth()
        {
            if(!holdsSize||transform.position.y>=LyingBelow||text==null)return float.PositiveInfinity;
            var size=text.GetPreferredValues(text.text);float wide=size.x*.5f,high=size.y*.5f;
            float most=1;
            for(float grown=1+GrowthStep;grown<=MostLying+.001f;grown+=GrowthStep){if(!AboveTheFloor(grown,wide,high))break;most=grown;}
            return most;
        }
        /// <summary>Whether the letters, grown this much about the sign's middle, all stand over the floor below them: across the word, along its middle and its letters' lower half.</summary>
        bool AboveTheFloor(float grown,float wide,float high)
        {
            for(int i=0;i<=8;i++)for(int j=-1;j<=1;j++)
            {
                var point=transform.position+transform.TransformVector(new Vector3((i/4f-1)*wide,j*.5f*high,0))*grown;
                int count=Physics.RaycastNonAlloc(point+Vector3.up*3,Vector3.down,floors,6,~0,QueryTriggerInteraction.Ignore);
                for(int k=0;k<count;k++)
                {
                    var hit=floors[k].collider;
                    if(hit is CharacterController||hit.attachedRigidbody!=null||hit.GetComponentInParent<Membrane>()!=null)continue;
                    if(floors[k].point.y>point.y)return false;
                }
            }
            return true;
        }

        // ---------- bodies and fights, read once a frame for every sign ----------
        static int bodiesAt=-1,foughtAt=-1,heroes;static bool fought;
        /// <summary>The party's bodies first (<see cref="heroes"/> of them), then everyone else's.</summary>
        static readonly List<Rect> bodies=new();
        static ZoneController peopleOf;static Transform[] people=System.Array.Empty<Transform>();
        /// <summary>Everyone who can stand over a word, as the screen has them this frame: the party on foot, the named, and the townsfolk who walk.</summary>
        static List<Rect> Bodies(Camera view)
        {
            if(bodiesAt==Time.frameCount)return bodies;bodiesAt=Time.frameCount;bodies.Clear();
            var zone=ZoneController.Current;
            if(zone!=peopleOf)
            {
                peopleOf=zone;var found=new HashSet<Transform>();
                foreach(var npc in FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None))found.Add(npc.transform);
                foreach(var walk in FindObjectsByType<WalkLoop>(FindObjectsInactive.Include,FindObjectsSortMode.None))found.Add(walk.transform);
                foreach(var walk in FindObjectsByType<ScriptedWalk>(FindObjectsInactive.Include,FindObjectsSortMode.None))found.Add(walk.transform);
                people=new Transform[found.Count];found.CopyTo(people);
            }
            var party=PartyController.Current;
            // A ship counts by its hull as the screen has it: a word lying under the plane (a gate's name, a pad's) was drawn over the ship flying across it, since it is drawn
            // nearer the camera than it lies (D356).
            if(party!=null&&party.members!=null)foreach(var member in party.members)
            {
                if(member==null||!member.gameObject.activeInHierarchy)continue;
                if(!member.flight)Add(view,member.transform.position);
                else{float hull=HeroCollision.HullRadius(member.character);if(PanelFade.ScreenBounds(view,new Bounds(member.transform.position,new Vector3(hull*2,hull*.5f,hull*2)),out var ship))bodies.Add(ship);}
            }
            heroes=bodies.Count;
            foreach(var person in people)if(person!=null&&person.gameObject.activeInHierarchy)Add(view,person.position);
            return bodies;
        }
        /// <summary>How far in front of the lens a body has to stand to count. A steep camera has ground just in front of its lens under it, and a body there projects to a rect larger
        /// than the screen: a grower walking his rope line 21 m short of THE PRESS stood .07 m in front of the lens and took the word to its trace in half the frames, with nobody on
        /// it (s18 091, D356).</summary>
        const float LensClear=1;
        static void Add(Camera view,Vector3 at)
        {
            var feet=view.WorldToScreenPoint(at);if(feet.z<view.nearClipPlane+LensClear)return;
            var seen=view.WorldToViewportPoint(at);if(seen.x<-.5f||seen.x>1.5f||seen.y<-.5f||seen.y>1.5f)return;
            var head=view.WorldToScreenPoint(at+Vector3.up*BodyTall);float half=Mathf.Abs(view.WorldToScreenPoint(at+view.transform.right*BodyHalfWide).x-feet.x);
            bodies.Add(Rect.MinMaxRect(feet.x-half,Mathf.Min(feet.y,head.y),feet.x+half,Mathf.Max(feet.y,head.y)));
        }
        /// <summary>A fight round the party: a boss's under way, or an awake enemy as near as quiet interactions wait for.</summary>
        static bool Fighting(Vector3 lead)
        {
            if(foughtAt!=Time.frameCount){foughtAt=Time.frameCount;fought=BossController.AnyFightUnderway||EnemyBrain.AwakeNear(lead,EnemyBrain.InteractionCalm);}
            return fought;
        }
        /// <summary>Whether a body stands over the word's letters on the screen, by a quarter of their height or more: anyone's over a floor word, a hero's over a hanging sign.</summary>
        bool Covered(Camera view,bool heroesOnly)
        {
            if(view==null||!Box(view,out var box,out _))return false;
            float least=box.height*LeastOver;
            var all=Bodies(view);int count=heroesOnly?heroes:all.Count;
            for(int i=0;i<count;i++)
            {
                var body=all[i];
                if(!body.Overlaps(box))continue;
                float over=Mathf.Min(body.yMax,box.yMax)-Mathf.Max(body.yMin,box.yMin);
                if(over>=least)return true;
            }
            return false;
        }
        /// <summary>Its letters' rect on the screen, and how far from the camera the near edge of its letters lies.</summary>
        bool Box(Camera view,out Rect box,out float front)
        {
            box=default;front=0;
            if(!ReferenceEquals(measured,text.text))
            {
                measured=text.text;var size=text.GetPreferredValues();halfWide=size.x*.5f;halfHigh=size.y*.5f;
                // A name's dark tag stands nine tenths of an em past its letters (SignStyle.Backed): the tag is what a plate or the edge cuts first, and HADDA's lay half under the
                // objective while its letters kept their margin (s22 014).
                if(text.text.Contains("<mark")){float tag=text.fontSize*.1f*.9f;halfWide+=tag;halfHigh+=tag;}
            }
            var scale=transform.lossyScale;
            Vector3 at=transform.position,across=transform.right*(halfWide*scale.x),up=transform.up*(halfHigh*scale.y);
            Vector3 one=view.WorldToScreenPoint(at-across-up),two=view.WorldToScreenPoint(at+across+up);
            if(one.z<=0||two.z<=0)return false;
            box=Rect.MinMaxRect(Mathf.Min(one.x,two.x),Mathf.Min(one.y,two.y),Mathf.Max(one.x,two.x),Mathf.Max(one.y,two.y));front=view.WorldToScreenPoint(at-up).z;
            return true;
        }
        /// <summary>Its letters' rect on the screen against the plates, with room kept round it: two points a frame, for the few signs in a zone. A sign reads level to the camera, so two corners
        /// are its rect. A name's words change with its mark, so they're measured when they do. Where it's heading counts as well as where it is, by as far as it'll go while it fades,
        /// so at a run it has gone by the time it gets there.</summary>
        bool Cut(Camera view)
        {
            if(view==null||!Box(view,out var box,out _))return false;
            float width=view.pixelWidth,height=view.pixelHeight;var middle=box.center;
            float room=Margin*height/1080f;box=Rect.MinMaxRect(box.xMin-room,box.yMin-room,box.xMax+room,box.yMax+room);
            var ahead=tracked?(middle-lastAt)/(Mathf.Max(Time.unscaledDeltaTime,.001f)*AsidePerSecond):Vector2.zero;lastAt=middle;tracked=true;
            if(PanelFade.Cuts(box,width,height))return true;
            if(ahead.sqrMagnitude<1)return false;
            float most=height*MostAhead;if(ahead.sqrMagnitude>most*most)ahead=ahead.normalized*most;
            box.position+=ahead;return PanelFade.Cuts(box,width,height);
        }
        /// <summary>Whether the interaction the prompt offers begins with these words, as a warp's prompt begins with its sign's ('Land — The Skyfields'): a sign and its button.
        /// What the prompt shows is GameHud's to say; while it shows nothing the sign is already away (a conversation, a menu, the card, a boss's bar).</summary>
        public static bool SaidByThePrompt(string words)
        {
            if(string.IsNullOrEmpty(words)||words.Length<4)return false;
            var offered=PromptService.Interaction;
            return offered!=null&&offered.isActiveAndEnabled&&!string.IsNullOrEmpty(offered.prompt)&&offered.prompt.StartsWith(words.Trim(),System.StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>Whether it's on the screen at an em too small to read.</summary>
        bool Speck(Camera view)
        {
            if(view==null)return false;
            var at=view.WorldToScreenPoint(transform.position);if(at.z<=0||at.x<0||at.y<0||at.x>view.pixelWidth||at.y>view.pixelHeight)return false;
            float em=text.fontSize*.1f*transform.lossyScale.y;var top=view.WorldToScreenPoint(transform.position+view.transform.up*em);
            return Vector2.Distance(at,top)/view.pixelHeight*1080<SignStyle.TypeFloorPixels*LeastOfTheFloor;
        }
    }
}
