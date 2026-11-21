using System;
using Lattice.Core;

namespace Lattice.World
{
    /// <summary>
    /// Side stories (D199): who says what when a side quest is at a given point. A speaker's ordinary tiers (first, repeat,
    /// post, final) stay as authored; when one of these beats applies, its conversation plays instead. Steps advance on
    /// flags the side conversations set themselves (<c>ObjectiveKind.Flag</c>), never on "talked to X", so the main
    /// story's own conversation with the same person can never tick a side step by accident. A beat never preempts a
    /// first meeting: each one requires its speaker to have been met already.
    /// </summary>
    public static class SideStories
    {
        readonly struct Beat
        {
            public readonly string speaker,node;public readonly Func<GameState,bool> when;
            public Beat(string speaker,string node,Func<GameState,bool> when){this.speaker=speaker;this.node=node;this.when=when;}
        }
        static int Step(GameState state,string quest)=>state.questSteps.TryGetValue(quest,out var step)?step:-1;
        static bool Flag(GameState state,string flag)=>state.flags.TryGetValue(flag,out var value)&&value;
        static bool Met(GameState state,string speaker)=>Flag(state,"met."+speaker);
        /// <summary>Set once Abri's last line of chapter 2 (<c>AbriLanternAfter</c>) has been heard: chapter 3's offer waits behind it.</summary>
        public const string AfterwordHeard="chapter2.afterword";
        /// <summary>The flag to set when this beat has been heard to its end, for a beat the story waits behind; null for every other. Whoever plays the beat sets it (Npc).</summary>
        public static string HeardFlagFor(string node)=>node=="AbriLanternAfter"?AfterwordHeard:node=="AbriDinnerServed"?ChapterThreeAfterword:HeardAboard.TryGetValue(node,out var aboard)?aboard:null;
        /// <summary>Chapter 5: a first talk aboard Many Hands that sets no flag of its own is marked heard by this, so the person's next talk is their repeat line.</summary>
        static readonly System.Collections.Generic.Dictionary<string,string> HeardAboard=new()
        {
            ["PellChartLamps"]="hands.pellAboard",["TeoHands"]="hands.teoAboard",["HaddaHands"]="hands.haddaAboard",["CorraHands"]="hands.corraAboard",["DovHands"]="hands.dovAboard",
            ["OnaHands"]="hands.onaAboard",["PimHands"]="hands.pimAboard",["GaroHands"]="hands.garoAboard",["DabbHands"]="hands.dabbAboard",
            // Chapter 6: the words aboard once Idra's cutters are home, each heard once, after which the person's chapter 5 line holds again (ch06 manifest, section 2).
            ["IloHoarfellPost"]="hoar.iloPostHeard",["BinnaKottSibb"]="hoar.kottSibbHeard",["BinnaCutterCrews"]="hoar.crewsHeard",["MaruCutters"]="hoar.maruCuttersHeard",
            ["RuvaBeasts"]="hoar.ruvaBeastsHeard",["TeoLiftFigures"]="hoar.teoFiguresHeard",
            // Chapter 10: Ruva's motion nine is her line in the square every time, and news only until it has been heard once (it gives out Homecomings).
            ["RuvaPassageRules"]="end.rulesHeard",
        };
        /// <summary>Chapter 5's beats are Many Hands' own: people who also stand in Rootmarket, Driftreed or Tallow say there what their own chapter gave them.</summary>
        static bool Aboard(GameState state)=>state.zone=="ManyHands";
        /// <summary>Chapter 9's Decks on separation day: a second scene of the Decks (DecksLeaving), whose people say only the day's words there.</summary>
        static bool OnTheDay(GameState state)=>state.zone=="DecksLeaving";
        /// <summary>Chapter 10's epilogue town (NeighbourhoodAfloat): the people there say only their epilogue words there.</summary>
        static bool Afloat(GameState state)=>state.zone=="NeighbourhoodAfloat";
        static bool Done(GameState state,string quest)=>Flag(state,"quest."+quest+".complete");
        /// <summary>Set once Abri's last line of chapter 3 (<c>AbriDinnerServed</c>) has been heard: chapter 4's offer waits behind it, as chapter 3's waits behind chapter 2's.</summary>
        public const string ChapterThreeAfterword="chapter3.afterword";

        static readonly Beat[] Beats=
        {
            // A Route With Room To Turn Back (chapter 10; routing from docs/writing/ch10-manifest.md, section 2, "Suggested routing"; who places each: docs/chapters/ch10-wiring.md).
            // Ahead of chapter 9's, because Abri's wall of names and Drask's count hold aboard from chapter 9 and the first match wins. Every beat asks for a flag only chapter 10 sets
            // (end.*, its own quests). Each zone's engineer appends their own people's beats inside this block, guarded by their zone or such a flag, each with its speaker met.
            // Many Hands (the first engineer's StageApproach): Abri notes the fortieth day until the party has stood on Sorrel; Drask, in custody, until the epilogue (he is off the ship then).
            new("Keeper","AbriFortiethDay",s=>Aboard(s)&&Flag(s,"end.planSaid")&&!Flag(s,"visited.SorrelSecondSky")),
            new("Drask","DraskBeforeTheField",s=>Aboard(s)&&Flag(s,"end.planSaid")&&Met(s,"Drask")),
            // Neighbourhood Afloat (LastMooringBuilder.Neighbourhood.cs; docs/maps/NeighbourhoodAfloat.md, "People"): everyone there says their epilogue line every time, ahead of
            // every older beat of theirs (an old offer won't play seven weeks on); someone whose only lines are extras opens with the first story they finished. Homecoming asks
            // their other extras to follow in the same box. Orrin's offer holds until the handrail is his; after it he is at the board and offers nothing.
            new("Orrin","OrrinDirectorship",s=>Afloat(s)&&!Flag(s,"end.handrail")&&Met(s,"Orrin")),
            new("Ruva","RuvaPassageRules",s=>Afloat(s)&&Met(s,"Ruva")),new("Meret","MeretAccount",s=>Afloat(s)&&Met(s,"Meret")),new("Drask","DraskWatches",s=>Afloat(s)&&Met(s,"Drask")),
            new("Jask","JaskNewSky",s=>Afloat(s)&&Met(s,"Jask")),new("Pip","PipRota",s=>Afloat(s)&&Met(s,"Pip")),
            new("Tam","TamHomeFrame",s=>Afloat(s)&&Done(s,"StillGround")&&Met(s,"Tam")),new("Tam","TamSkyGarden",s=>Afloat(s)&&Met(s,"Tam")),
            new("Keeper","AbriHomecoming",s=>Afloat(s)&&Met(s,"Keeper")),new("Teo","TeoHomecoming",s=>Afloat(s)&&Met(s,"Teo")),new("Maru","MaruHomecoming",s=>Afloat(s)&&Met(s,"Maru")),
            new("Hask","HaskHomecoming",s=>Afloat(s)&&Met(s,"Hask")),new("Pell","PellHomecoming",s=>Afloat(s)&&Met(s,"Pell")),new("Tovra","TovraHomecoming",s=>Afloat(s)&&Met(s,"Tovra")),
            new("Oda","OdaHomecoming",s=>Afloat(s)&&Met(s,"Oda")),new("Dorrow","DorrowHomecoming",s=>Afloat(s)&&Met(s,"Dorrow")),new("Mira","MiraHomecoming",s=>Afloat(s)&&Met(s,"Mira")),
            new("Neve","NeveHomeParcels",s=>Afloat(s)&&Done(s,"NeveParcel")&&Met(s,"Neve")),new("Neve","NeveHomeSocks",s=>Afloat(s)&&Done(s,"ReturnAddress")&&Met(s,"Neve")),
            new("Survivor","SurvivorHomeLamps",s=>Afloat(s)&&Done(s,"SorrelBeacons")&&Met(s,"Survivor")),
            new("Ishen","IshenHomeShelves",s=>Afloat(s)&&Done(s,"IshensRecord")&&Met(s,"Ishen")),new("Ishen","IshenHomeMoons",s=>Afloat(s)&&Done(s,"MoonNames")&&Met(s,"Ishen")),
            new("Brekk","BrekkHomeAwning",s=>Afloat(s)&&Done(s,"UnswornRations")&&Met(s,"Brekk")),new("Brekk","BrekkHomeKettle",s=>Afloat(s)&&Done(s,"OdrunsKitchen")&&Met(s,"Brekk")),
            new("Binna","BinnaHomeBaskets",s=>Afloat(s)&&Done(s,"ThreeBaskets")&&Met(s,"Binna")),
            new("Dov","DovHomeSmokehouse",s=>Afloat(s)&&Done(s,"SmokehouseOrder")&&Met(s,"Dov")),new("Dov","DovHomeBell",s=>Afloat(s)&&Done(s,"SchoolBell")&&Met(s,"Dov")),
            new("Pim","PimHomeKey",s=>Afloat(s)&&Done(s,"FourthPump")&&Met(s,"Pim")),new("Corra","CorraHomeKettles",s=>Afloat(s)&&Done(s,"KettleTrade")&&Met(s,"Corra")),
            new("Ilo","IloHomeBook",s=>Afloat(s)&&Done(s,"ClaimsLedger")&&Met(s,"Ilo")),new("Iru","IruHomeLamps",s=>Afloat(s)&&Done(s,"ShrineLamps")&&Met(s,"Iru")),
            new("Hal","HalHomeBenches",s=>Afloat(s)&&Done(s,"PortableBenches")&&Met(s,"Hal")),
            // The House That Can Leave (chapter 9; routing from docs/writing/ch09-manifest.md, section 2, "Suggested routing"; who places each: docs/chapters/ch09-wiring.md). Ahead of
            // chapter 8's, because Abri's AbriSmiths holds aboard for good once chapter 8 is done and the first match wins. Every beat asks for a flag only chapter 9 sets (home.*, its
            // people's, its own quests). Each zone's engineer appends their own people's beats inside this block, guarded by their zone or such a flag, each with its speaker met.
            // Many Hands (the first engineer's StageApproach): Abri's wall of names holds aboard from Meret's notice; Drask asks his count once.
            new("Keeper","AbriNamesOnTheWall",s=>Aboard(s)&&Flag(s,"home.noticeHeard")),
            new("Drask","DraskCount",s=>Aboard(s)&&Flag(s,"home.odaFound")&&!Flag(s,"drask.asked")&&Met(s,"Drask")),
            // The Decks on separation day (zone DecksLeaving, HomeBuilder.Decks.cs; docs/maps/DecksLeaving.md). Every beat is that zone's alone (OnTheDay), so none can win in an earlier
            // chapter or anywhere else. Chapter 1's own beats for these people (Orrin's token, Neve's and Dace's parcel, Oda's berth, Mira's thanks) are still true for a save that
            // never did them, and the manifest holds their chapter 1 tiers on the day (8.3), so each person here ends on a line of the day's that holds (PeopleMarks.Repeats has them).
            // The meeting is eight talks in turn (TurnToSpeak keeps each person quiet outside theirs); Orrin's are first because the first match wins. The Coronach's go ahead of these.
            new("Orrin","MeretWithdraws",s=>OnTheDay(s)&&Flag(s,"home.orrinSaid")&&!Flag(s,"home.meetingHeld")),
            new("Orrin","OrrinPostponed",s=>OnTheDay(s)&&Flag(s,"home.odaStood")&&!Flag(s,"home.orrinSaid")),
            new("Orrin","OrrinMeetingOpens",s=>OnTheDay(s)&&Flag(s,"home.meetingGathered")&&!Flag(s,"home.meetingOpened")),
            new("Orrin","OrrinHomecoming",s=>OnTheDay(s)&&!Flag(s,"home.returned")),
            new("Meret","MeretBerthOffer",s=>OnTheDay(s)&&Flag(s,"home.meetingOpened")&&!Flag(s,"home.offerMade")),
            new("Hal","HalLastMachine",s=>OnTheDay(s)&&Flag(s,"home.saintReleased")&&!Flag(s,"home.halChose")),
            new("Hal","HalApprentices",s=>OnTheDay(s)&&Flag(s,"home.offerMade")&&!Flag(s,"home.halAsked")),
            new("Hal","HalBenchesReturn",s=>OnTheDay(s)&&Step(s,"PortableBenches")==1),
            new("Hal","HalBenchesOffer",s=>OnTheDay(s)&&Flag(s,"home.returned")&&Step(s,"PortableBenches")<0&&!Done(s,"PortableBenches")),
            new("Mira","MiraManifest",s=>OnTheDay(s)&&Flag(s,"home.halAsked")&&!Flag(s,"home.manifestShown")),
            new("Mira","MiraInventoryReturn",s=>OnTheDay(s)&&Step(s,"SecondInventory")==1),
            new("Mira","MiraInventoryOffer",s=>OnTheDay(s)&&Flag(s,"home.returned")&&Step(s,"SecondInventory")<0&&!Done(s,"SecondInventory")),
            new("Mira","MiraCratedRepeat",s=>OnTheDay(s)),
            new("Ilo","IloReadsNames",s=>OnTheDay(s)&&Flag(s,"home.manifestShown")&&!Flag(s,"home.namesRead")),
            new("Ilo","IloDecks",s=>OnTheDay(s)),
            new("Oda","OdaStands",s=>OnTheDay(s)&&Flag(s,"home.namesRead")&&!Flag(s,"home.odaStood")),
            new("Oda","OdaDecks",s=>OnTheDay(s)),
            new("Neve","NeveReturnAnswer",s=>OnTheDay(s)&&Step(s,"ReturnAddress")==3),
            new("Neve","NeveFlyingAStreet",s=>OnTheDay(s)),
            new("Survivor","SurvivorNevesLetter",s=>OnTheDay(s)&&Step(s,"ReturnAddress")==2),
            new("Survivor","SurvivorDecks",s=>OnTheDay(s)),
            new("Tam","TamGoesBack",s=>OnTheDay(s)&&Step(s,"StillGround")==1&&Met(s,"Tam")),
            new("Tam","TamStillOffer",s=>OnTheDay(s)&&Flag(s,"home.returned")&&Step(s,"StillGround")<0&&!Done(s,"StillGround")&&Met(s,"Tam")),
            new("Dorrow","DorrowListens",s=>OnTheDay(s)&&Met(s,"Dorrow")),
            // Harrowgate (chapter 8; routing from docs/writing/ch08-manifest.md, section 2, "Suggested routing"). Ahead of chapter 7's and 6's, because Abri's and Pell's lines of those
            // chapters hold for good aboard and the first match wins. Every beat asks for a flag only chapter 8 sets (vask.*, its people's, chapter8.complete, its own quests) or for
            // chapter 7 to be over, so none can win in an earlier chapter. Tovra's, Odrun's and Pip's ask for their speaker met; Hessa's, met since chapter 2, for the Warmaster met.
            // The people who come aboard at the end say only their aboard lines there (their Npc's own), so the camp's and the town's beats keep to off the ship.
            new("Keeper","AbriSmiths",s=>Aboard(s)&&Flag(s,"chapter8.complete")),
            new("Keeper","AbriThreeWords",s=>Aboard(s)&&Flag(s,"vask.lockAdmitted")&&!Flag(s,"vask.picketCrossed")),
            new("Keeper","AbriTwentySeven",s=>Aboard(s)&&Flag(s,"chapter7.complete")&&!Flag(s,"vask.lockAdmitted")),
            new("Pell","PellAumiAnswers",s=>Aboard(s)&&Flag(s,"chapter8.complete")&&Done(s,"RelayGlass")),
            new("Tovra","TovraHarrowgateRoad",s=>!Aboard(s)&&Flag(s,"vask.juvenileFreed")&&!Flag(s,"vask.roadSaid")&&Met(s,"Tovra")),
            new("Tovra","TovraQuietReturn",s=>!Aboard(s)&&Step(s,"QuietHands")==1&&Met(s,"Tovra")),
            new("Tovra","TovraCages",s=>!Aboard(s)&&Flag(s,"vask.unswornMet")&&!Flag(s,"tovra.cagesHeard")&&Met(s,"Tovra")),
            new("Tovra","TovraQuietOffer",s=>!Aboard(s)&&Flag(s,"vask.unswornMet")&&Step(s,"QuietHands")<0&&!Done(s,"QuietHands")&&Met(s,"Tovra")),
            new("Hessa","HessaWarmaster",s=>!Aboard(s)&&Flag(s,"vask.draskMet")&&!Flag(s,"vask.duelWon")&&!Flag(s,"hessa.warmasterHeard")),
            // Hessa was met in chapter 2 (met.Hessa), so her first tier would never play: the parley is hers at the gate until it is vouched, whatever she said in the relay.
            new("Hessa","HessaParley",s=>!Aboard(s)&&Flag(s,"vask.roadSaid")&&!Flag(s,"vask.parley")),
            new("Pip","PipAfterDuel",s=>!Aboard(s)&&Flag(s,"vask.deposed")&&Step(s,"PipsOath")==2&&Met(s,"Pip")),
            new("Odrun","OdrunKitchenReturn",s=>!Aboard(s)&&Step(s,"OdrunsKitchen")==1&&Met(s,"Odrun")),
            new("Odrun","OdrunSleepless",s=>!Aboard(s)&&Flag(s,"vask.odrunMet")&&!Flag(s,"odrun.sleeplessHeard")&&Met(s,"Odrun")),
            new("Odrun","OdrunKitchenOffer",s=>!Aboard(s)&&Flag(s,"vask.odrunMet")&&Step(s,"OdrunsKitchen")<0&&!Done(s,"OdrunsKitchen")&&Met(s,"Odrun")),
            // The Cage Makers (chapter 7; routing from docs/writing/ch07-manifest.md, section 2, "Suggested routing"). Ahead of chapter 6's, because every one of chapter 6's
            // aboard holds once its cutters are home (AbriCutters is true for good) and the first match wins. Every beat here asks for a flag only chapter 7 sets (oriel.*,
            // chapter7.complete, its own quests), so none can win in an earlier chapter. Lume's chapter-end reading is compulsory and asks for no meeting; her offer and return do.
            // Ishen speaks the same at the camp and aboard; Ruva's and Pell's lines take the place of their earlier ones for good once their side story is done.
            new("Keeper","AbriTooManyBooks",s=>Aboard(s)&&Flag(s,"oriel.charted")&&!Flag(s,"oriel.landed")),
            new("Ishen","IshenMoonsReturn",s=>Step(s,"MoonNames")==1&&Met(s,"Ishen")),
            new("Ishen","IshenMoonsOffer",s=>Flag(s,"oriel.ruleSeen")&&Step(s,"MoonNames")<0&&!Done(s,"MoonNames")&&Met(s,"Ishen")),
            new("Lume","LumeBrekksWord",s=>Flag(s,"oriel.meretHeard")&&!Flag(s,"chapter7.complete")),
            new("Lume","LumeLanternReturn",s=>Step(s,"LumesLantern")==1&&Met(s,"Lume")),
            new("Lume","LumeLanternOffer",s=>Met(s,"Lume")&&Step(s,"LumesLantern")<0&&!Done(s,"LumesLantern")),
            new("Ruva","RuvaGardener",s=>Aboard(s)&&Flag(s,"chapter7.complete")&&Done(s,"EmptyGardens")),
            new("Pell","PellOrielLantern",s=>Aboard(s)&&Flag(s,"chapter7.complete")&&Done(s,"LumesLantern")),
            // The Long Sleep (chapter 6; routing from docs/writing/ch06-manifest.md, section 2, "Suggested routing"). Ahead of chapter 5's, because the first match wins and every
            // person aboard ends on a line of chapter 5's that holds there: Abri's and Ilo's chapter 6 words stand for the chapter's window, and the words aboard once the cutters are
            // home are heard once each (HeardAboard). Lastlight's people are new; each beat of theirs asks for its speaker to have been met, and none can be true before
            // chapter 5's chart is read (hoar.charted is the first flag of the chapter).
            new("Keeper","AbriCutters",s=>Aboard(s)&&Flag(s,"chapter6.complete")),
            new("Keeper","AbriThirtySix",s=>Aboard(s)&&Flag(s,"hoar.charted")&&!Flag(s,"hoar.idraHailed")),
            new("Ilo","IloHoarfellPost",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&!Flag(s,"hoar.iloPostHeard")),
            new("Ilo","IloLastlightEntry",s=>Aboard(s)&&Flag(s,"hoar.charted")&&!Flag(s,"chapter6.complete")),
            new("Binna","BinnaKottSibb",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&Done(s,"CutterDown")&&!Flag(s,"hoar.kottSibbHeard")),
            new("Binna","BinnaCutterCrews",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&!Flag(s,"hoar.crewsHeard")),
            new("Maru","MaruCutters",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&!Flag(s,"hoar.maruCuttersHeard")),
            new("Ruva","RuvaBeasts",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&!Flag(s,"hoar.ruvaBeastsHeard")),
            new("Teo","TeoLiftFigures",s=>Aboard(s)&&Flag(s,"chapter6.complete")&&!Flag(s,"hoar.teoFiguresHeard")),
            new("Tern","TernLongSleep",s=>Flag(s,"hoar.marshalAsleep")&&!Flag(s,"hoar.sleepLearned")&&Met(s,"Tern")),
            new("Hask","HaskStraysReturn",s=>Step(s,"StrayBeasts")==1&&Met(s,"Hask")),
            new("Hask","HaskStraysWaiting",s=>Step(s,"StrayBeasts")==0&&Met(s,"Hask")),
            new("Hask","HaskOwes",s=>Flag(s,"hoar.marshalAsleep")&&!Flag(s,"hask.owesHeard")&&Met(s,"Hask")),
            new("Hask","HaskStraysOffer",s=>Met(s,"Hask")&&Flag(s,"hoar.planTaken")&&Step(s,"StrayBeasts")<0&&!Done(s,"StrayBeasts")),
            new("Hask","HaskAboard",s=>Aboard(s)&&Flag(s,"chapter6.complete")),
            // Iru speaks up in Tern's hall (where she stands for it: manifest 8.7), never aboard.
            new("Iru","IruSpeaksUp",s=>s.zone=="Lastlight"&&Done(s,"ShrineLamps")&&Flag(s,"hoar.sleepLearned")&&!Flag(s,"iru.spokeUp")&&Met(s,"Iru")),
            new("Iru","IruLampsReturn",s=>Step(s,"ShrineLamps")==1&&Met(s,"Iru")),
            new("Iru","IruLampsOffer",s=>Met(s,"Iru")&&Flag(s,"hoar.ternHeard")&&Step(s,"ShrineLamps")<0&&!Done(s,"ShrineLamps")),
            new("Iru","IruAboard",s=>Aboard(s)&&Flag(s,"chapter6.complete")),
            // Many Hands (chapter 5; routing from docs/writing/ch05-manifest.md, section 2). Every one is scoped to the ship (Aboard), and every person who speaks aboard ends on a
            // line that holds there, because chapters 3 and 4's beats for the same people (Dov's smokehouse order, Maru's beacons, Abri's call) are still true and would otherwise
            // play aboard. A compulsory talk asks for no earlier meeting; first talks aboard that set nothing are marked heard by HeardAboard.
            new("Keeper","AbriHandsArrival",s=>Aboard(s)&&!Flag(s,"hands.aboard")),
            new("Keeper","AbriLedger",s=>Aboard(s)&&Step(s,"ClaimsLedger")>=0&&!Flag(s,"ledger.abri")&&!Done(s,"ClaimsLedger")),
            new("Keeper","AbriKettle",s=>Aboard(s)&&Step(s,"KettleTrade")>=0&&!Flag(s,"kettle.cabins")&&!Done(s,"KettleTrade")),
            new("Keeper","AbriDinnerTalk",s=>Aboard(s)&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.dinnerDone")),
            new("Keeper","AbriCourier",s=>Aboard(s)&&Flag(s,"hands.jobsDone")&&!Flag(s,"hands.iloAboard")),
            new("Keeper","AbriHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Keeper","AbriHandsRepeat",s=>Aboard(s)),
            new("Pell","PellHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Pell","PellChartLamps",s=>Aboard(s)&&!Flag(s,"hands.pellAboard")),
            new("Pell","PellHandsRepeat",s=>Aboard(s)),
            new("Ruva","RuvaDinnerNumber",s=>Aboard(s)&&Flag(s,"hands.stillwaterHeard")&&!Flag(s,"hands.numberHeard")),
            new("Ruva","RuvaStowaway",s=>Aboard(s)&&Step(s,"StowawayFare")==0),
            new("Ruva","RuvaMooringLost",s=>Aboard(s)&&Flag(s,"hands.mooringForTides")&&!Flag(s,"ruva.mooringGrumbled")),
            new("Ruva","RuvaMooring",s=>Aboard(s)&&!Flag(s,"hands.ruvaHeard")),
            new("Ruva","RuvaHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Ruva","RuvaHandsRepeat",s=>Aboard(s)),
            new("Maru","MaruDinnerTalk",s=>Aboard(s)&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.dinnerDone")),
            new("Maru","MaruLedger",s=>Aboard(s)&&Step(s,"ClaimsLedger")>=0&&!Flag(s,"ledger.maru")&&!Done(s,"ClaimsLedger")),
            new("Maru","MaruMooringLost",s=>Aboard(s)&&Flag(s,"hands.mooringForRoots")&&!Flag(s,"maru.mooringGrumbled")),
            new("Maru","MaruMooring",s=>Aboard(s)&&!Flag(s,"hands.maruHeard")),
            new("Maru","MaruHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Maru","MaruHandsRepeat",s=>Aboard(s)),
            new("Binna","BinnaDinner",s=>Aboard(s)&&Flag(s,"hands.meretMet")&&!Flag(s,"hands.dinnerSat")),
            new("Binna","BinnaDinnerTalk",s=>Aboard(s)&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.dinnerDone")),
            new("Binna","BinnaVent",s=>Aboard(s)&&!Flag(s,"hands.ventFixed")),
            new("Binna","BinnaHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Binna","BinnaHandsRepeat",s=>Aboard(s)),
            new("Teo","TeoDinnerTalk",s=>Aboard(s)&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.dinnerDone")),
            new("Teo","TeoLedger",s=>Aboard(s)&&Step(s,"ClaimsLedger")>=0&&!Flag(s,"ledger.teo")&&!Done(s,"ClaimsLedger")),
            new("Teo","TeoStowawayDecide",s=>Aboard(s)&&Step(s,"StowawayFare")==1),
            new("Teo","TeoStowawayOffer",s=>Aboard(s)&&Flag(s,"hands.jaskFound")&&Step(s,"StowawayFare")<0&&!Done(s,"StowawayFare")),
            new("Teo","TeoHands",s=>Aboard(s)&&!Flag(s,"hands.teoAboard")&&!Flag(s,"hands.jaskFound")),
            new("Teo","TeoHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Teo","TeoHandsRepeat",s=>Aboard(s)),
            new("Jask","JaskDinnerTalk",s=>Aboard(s)&&Flag(s,"hands.jaskFound")&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.dinnerDone")),
            new("Jask","JaskThanksGarden",s=>Aboard(s)&&Flag(s,"jask.worksGarden")&&!Flag(s,"jask.thanked")),
            new("Jask","JaskThanksFerry",s=>Aboard(s)&&Flag(s,"jask.worksFerry")&&!Flag(s,"jask.thanked")),
            new("Jask","JaskThanksLamps",s=>Aboard(s)&&Flag(s,"jask.worksLamps")&&!Flag(s,"jask.thanked")),
            new("Jask","JaskHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Jask","JaskGardenHand",s=>Aboard(s)&&Flag(s,"jask.worksGarden")),
            new("Jask","JaskFerryHand",s=>Aboard(s)&&Flag(s,"jask.worksFerry")),
            new("Jask","JaskLampHand",s=>Aboard(s)&&Flag(s,"jask.worksLamps")),
            new("Jask","JaskFoundRepeat",s=>Aboard(s)),
            new("Hadda","HaddaHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Hadda","HaddaHands",s=>Aboard(s)&&!Flag(s,"hands.haddaAboard")),
            new("Hadda","HaddaHandsRepeat",s=>Aboard(s)),
            new("Corra","CorraKettleReturn",s=>Aboard(s)&&Step(s,"KettleTrade")==3),
            new("Corra","CorraKettleOffer",s=>Aboard(s)&&Flag(s,"hands.corraAboard")&&Flag(s,"hands.jobsDone")&&Step(s,"KettleTrade")<0&&!Done(s,"KettleTrade")),
            // Her first talk aboard before her post line: the kettles' offer waits on it (hands.corraAboard), so a party that first meets her after the dinner would otherwise
            // never be offered them.
            new("Corra","CorraHands",s=>Aboard(s)&&!Flag(s,"hands.corraAboard")),
            new("Corra","CorraHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Corra","CorraHandsRepeat",s=>Aboard(s)),
            new("Dov","DovHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Dov","DovHands",s=>Aboard(s)&&!Flag(s,"hands.dovAboard")),
            new("Dov","DovHandsRepeat",s=>Aboard(s)),
            new("Ona","OnaKettle",s=>Aboard(s)&&Step(s,"KettleTrade")>=0&&!Flag(s,"kettle.bay")&&!Done(s,"KettleTrade")),
            new("Ona","OnaHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Ona","OnaHands",s=>Aboard(s)&&!Flag(s,"hands.onaAboard")),
            new("Ona","OnaHandsRepeat",s=>Aboard(s)),
            new("Pim","PimHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Pim","PimHands",s=>Aboard(s)&&!Flag(s,"hands.pimAboard")),
            new("Pim","PimHandsRepeat",s=>Aboard(s)),
            new("Garo","GaroKettle",s=>Aboard(s)&&Step(s,"KettleTrade")>=0&&!Flag(s,"kettle.garden")&&!Done(s,"KettleTrade")),
            new("Garo","GaroHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Garo","GaroHands",s=>Aboard(s)&&!Flag(s,"hands.garoAboard")),
            new("Garo","GaroHandsRepeat",s=>Aboard(s)),
            new("Dabb","DabbHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Dabb","DabbHands",s=>Aboard(s)&&!Flag(s,"hands.dabbAboard")),
            new("Dabb","DabbHandsRepeat",s=>Aboard(s)),
            new("Ilo","IloFootnotes",s=>Aboard(s)&&Flag(s,"hands.numberHeard")&&!Flag(s,"hands.footnotesTabled")),
            new("Ilo","IloLedgerReturn",s=>Aboard(s)&&Step(s,"ClaimsLedger")==3),
            new("Ilo","IloLedgerOffer",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")&&Step(s,"ClaimsLedger")<0&&!Done(s,"ClaimsLedger")),
            new("Ilo","IloArrives",s=>Aboard(s)&&!Flag(s,"hands.iloAboard")),
            new("Ilo","IloHandsPost",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")),
            new("Ilo","IloHandsRepeat",s=>Aboard(s)),
            new("Patrol","PatrolBreachSealed",s=>Aboard(s)&&Flag(s,"hands.dessikYielded")&&!Flag(s,"hands.breachSealed")),
            new("Patrol","PatrolRepeat",s=>Aboard(s)),
            // Meret's first talk is her own first meeting (MeretArrives, behind the captain); after it, each of her talks waits for the one before it at the table.
            new("Meret","MeretParting",s=>Aboard(s)&&Flag(s,"hands.dinnerDone")&&!Flag(s,"hands.partingHeard")),
            new("Meret","MeretDrawings",s=>Aboard(s)&&Flag(s,"hands.sorrelNamed")&&!Flag(s,"hands.dinnerDone")),
            new("Meret","MeretSorrel",s=>Aboard(s)&&Flag(s,"hands.footnotesTabled")&&!Flag(s,"hands.sorrelNamed")),
            new("Meret","MeretStillwater",s=>Aboard(s)&&Flag(s,"hands.dinnerSat")&&!Flag(s,"hands.stillwaterHeard")),
            // Breathing Room (chapter 4; routing from docs/writing/ch04-manifest.md, section 2). Ahead of chapter 3's for the reason chapter 3's lead chapter 2's: once chapter 3 is
            // over, what Abri and Pell say is chapter 4's. Maru is in two places over the chapter (the pilots' stage, the mire), so the beats that belong to one of them name its zone.
            new("Keeper","AbriManyHands",s=>Flag(s,"chapter4.complete")),
            new("Keeper","AbriReedRoadWaiting",s=>Flag(s,"wend.onaHeard")),
            new("Keeper","AbriReedRoadCharted",s=>Flag(s,"wend.charted")),
            new("Keeper","AbriReedRoadRepeat",s=>Step(s,"ReedRoad")>=0),
            // Chapter 3 ends on its own last word: Abri's offer waits until her line after the dinner (AbriDinnerServed) has been heard to its end.
            new("Keeper","AbriReedRoad",s=>Flag(s,ChapterThreeAfterword)&&Step(s,"ReedRoad")<0&&!Flag(s,"quest.ReedRoad.complete")),
            new("Pell","PellManyHands",s=>Flag(s,"chapter4.complete")),
            new("Pell","PellVeilRepeat",s=>Flag(s,"wend.charted")),
            new("Pell","PellVeilLantern",s=>Step(s,"ReedRoad")>=0),
            new("Maru","MaruBeaconsReturn",s=>s.zone=="Driftreed"&&Step(s,"BeaconKeeper")==1&&Met(s,"Maru")),
            new("Maru","MaruTide",s=>Flag(s,"wend.onaHeard")&&!Flag(s,"wend.tideTurned")&&Met(s,"Maru")),
            new("Maru","MaruTideCord",s=>s.zone=="DrownedWaystation"&&Flag(s,"wend.tideTurned")&&!Flag(s,"maru.cordShown")&&!Flag(s,"wend.rotbloomDead")&&Met(s,"Maru")),
            new("Maru","MaruOnaWaits",s=>s.zone=="Driftreed"&&Flag(s,"wend.rotbloomDead")&&!Flag(s,"wend.decided")),
            new("Maru","MaruComes",s=>Flag(s,"wend.decided")&&!Flag(s,"maru.comesHeard")),
            new("Maru","MaruBeaconsOffer",s=>Flag(s,"maru.comesHeard")&&Step(s,"BeaconKeeper")<0&&!Flag(s,"quest.BeaconKeeper.complete")),
            new("Ona","OnaDecides",s=>Flag(s,"wend.rotbloomDead")&&!Flag(s,"wend.decided")&&Met(s,"Ona")),
            new("Ona","OnaPimBack",s=>Flag(s,"wend.pimFound")&&!Flag(s,"wend.rotbloomDead")&&Met(s,"Ona")),
            new("Dov","DovOrderReturn",s=>Step(s,"SmokehouseOrder")==1),
            new("Dov","DovBellReturn",s=>Step(s,"SchoolBell")==1),
            new("Dov","DovOrderOffer",s=>Met(s,"Dov")&&Flag(s,"wend.tideTurned")&&Step(s,"SmokehouseOrder")<0&&!Flag(s,"quest.SmokehouseOrder.complete")),
            new("Dov","DovBellOffer",s=>Met(s,"Dov")&&Flag(s,"wend.tideTurned")&&(Step(s,"SmokehouseOrder")>=0||Flag(s,"quest.SmokehouseOrder.complete"))&&Step(s,"SchoolBell")<0&&!Flag(s,"quest.SchoolBell.complete")),
            new("Quill","QuillPumpReturn",s=>Step(s,"FourthPump")==2),
            new("Quill","QuillPumpOffer",s=>Met(s,"Quill")&&Flag(s,"wend.onaHeard")&&Step(s,"FourthPump")<0&&!Flag(s,"quest.FourthPump.complete")),
            // Carrying a Place (chapter 3; routing from docs/writing/ch03-manifest.md, section 2). Ahead of chapter 2's, because the first match wins and none of these can be
            // true before chapter 2 is home. A compulsory talk asks for no earlier meeting; an offer, a return and an afterword do. Chapter 2 still ends on its own last word:
            // Abri's offer waits until her line after the homecoming has been heard (AfterwordHeard), so the homecoming is followed by that line and the next talk opens chapter 3.
            // It took that line's place the moment the homecoming was told, which changed how chapter 2 closed.
            new("Keeper","AbriDinnerServed",s=>Flag(s,"chapter3.complete")),
            new("Keeper","AbriSeedRoadWaiting",s=>Flag(s,"xylos.termsHeard")),
            new("Keeper","AbriSeedRoadCharted",s=>Flag(s,"xylos.charted")),
            new("Keeper","AbriSeedRoadRepeat",s=>Step(s,"SeedRoad")>=0),
            new("Keeper","AbriSeedRoad",s=>Flag(s,AfterwordHeard)&&Step(s,"SeedRoad")<0&&!Flag(s,"quest.SeedRoad.complete")),
            new("Pell","PellAfterVote",s=>Flag(s,"chapter3.complete")),
            new("Pell","PellLaneRepeat",s=>Flag(s,"xylos.charted")),
            new("Pell","PellLaneLantern",s=>Step(s,"SeedRoad")>=0),
            new("Ruva","RuvaVote",s=>Flag(s,"xylos.floodcrownGone")&&!Flag(s,"xylos.voteHeard")),
            new("Ruva","RuvaCrewsHome",s=>Flag(s,"xylos.termsHeard")&&Flag(s,"xylos.marrow1")&&Flag(s,"xylos.marrow2")&&!Flag(s,"xylos.crewsThanked")&&!Flag(s,"xylos.floodcrownGone")),
            new("Teo","TeoSeedfall",s=>Flag(s,"bossdown.Seedthane")&&!Flag(s,"xylos.seedthaneGone")),
            new("Teo","TeoTagsReturn",s=>Step(s,"ClaimTags")==1&&Met(s,"Teo")),
            new("Binna","BinnaBasketsReturn",s=>Step(s,"ThreeBaskets")==1),
            new("Binna","BinnaBasketsOffer",s=>Step(s,"ThreeBaskets")<0&&!Flag(s,"quest.ThreeBaskets.complete")&&Met(s,"Binna")&&Flag(s,"xylos.termsHeard")),
            new("Binna","BinnaAfterBaskets",s=>Flag(s,"quest.ThreeBaskets.complete")&&!Flag(s,"binna.afterBaskets")&&!Flag(s,"xylos.voteHeard")),
            // On the mid-river Jask only: the town's has left by the time the middle sluice is open.
            new("Jask","JaskFerry",s=>Step(s,"Questions")==0&&Met(s,"Jask")&&Flag(s,"xylos.sluiceA")),
            new("Senn","SennLastWord",s=>Flag(s,"quest.MooringPost.complete")&&Flag(s,"xylos.voteHeard")&&!Flag(s,"senn.lastWord")),
            new("Senn","SennPostReturn",s=>Step(s,"MooringPost")==1),
            new("Senn","SennPostOffer",s=>Step(s,"MooringPost")<0&&!Flag(s,"quest.MooringPost.complete")&&Met(s,"Senn")&&Flag(s,"xylos.termsHeard")),
            // An overdue parcel (NeveParcel): Neve has held one of Sorrel's missed deliveries; the Survivor answers it.
            new("Survivor","SurvivorParcel",s=>Step(s,"NeveParcel")==0&&Met(s,"Survivor")),
            new("Neve","NeveParcelReturn",s=>Step(s,"NeveParcel")==1),
            new("Neve","NeveParcelOffer",s=>Step(s,"NeveParcel")<0&&Met(s,"Neve")),
            new("Neve","NeveAfterParcel",s=>Flag(s,"quest.NeveParcel.complete")&&Flag(s,"bossdown.Burrower")&&!Flag(s,"neve.afterParcel")),
            // Quiet ridges (SorrelBeacons): the survey crew's evacuation beacons still call machinery toward the outpost.
            new("Survivor","SurvivorBeaconsDone",s=>Step(s,"SorrelBeacons")==1),
            new("Survivor","SurvivorBeaconsOffer",s=>Step(s,"SorrelBeacons")<0&&Flag(s,"bossdown.Burrower")&&Met(s,"Survivor")&&(!Flag(s,"hushwell.nursery")||Flag(s,"sorrel.nurseryHeard"))),
            new("Survivor","SurvivorAfterParcel",s=>Flag(s,"quest.NeveParcel.complete")&&Flag(s,"bossdown.Burrower")&&!Flag(s,"sorrel.afterParcel")&&(!Flag(s,"hushwell.nursery")||Flag(s,"sorrel.nurseryHeard"))),
            // A berth for two (OdaBerth): Oda's family filed for Nacre; their berth token waits at Orrin's office.
            new("Orrin","OrrinBerth",s=>Step(s,"OdaBerth")==0&&Met(s,"Orrin")),
            new("Oda","OdaBerthReturn",s=>Step(s,"OdaBerth")==1),
            new("Oda","OdaBerthOffer",s=>Step(s,"OdaBerth")<0&&Met(s,"Oda")&&Met(s,"Orrin")),
            new("Oda","OdaAfterBerth",s=>Flag(s,"quest.OdaBerth.complete")&&!Flag(s,"oda.afterBerth")),
            // The chapter's last beat: once Tallow knows it is moving, the Compact calls on the beacon band (Act II's hook).
            new("Keeper","CompactHail",s=>Flag(s,"tallow.driftDiscussed")&&!Flag(s,"compact.hailHeard")),
            // The Lantern Road (chapter 2, D202): Abri remembers the canyon town whose lanterns guided moving stations.
            new("Keeper","AbriLanternHome",s=>Flag(s,"lanternwick.chartGiven")&&!Flag(s,"chapter2.homecoming")),
            new("Keeper","AbriLanternTale",s=>Flag(s,"compact.hailHeard")&&Step(s,"LanternRoad")<0&&!Flag(s,"lanternwick.chartGiven")),
            new("Keeper","AbriLanternRepeat",s=>Step(s,"LanternRoad")>=0&&Step(s,"LanternRoad")<2),
            new("Keeper","AbriLanternWaiting",s=>Step(s,"LanternRoad")>=2&&!Flag(s,"lanternwick.chartGiven")),
            new("Keeper","AbriLanternAfter",s=>Flag(s,"chapter2.homecoming")),
            // Pell's lamp (PellsLamp): the apprentice lost her mother's lamp to a grazer in the gorge.
            new("Pell","PellLampReturn",s=>Step(s,"PellsLamp")==1),
            new("Pell","PellLampOffer",s=>Step(s,"PellsLamp")<0&&Met(s,"Pell")&&Step(s,"LanternRoad")>=3),
            new("Pell","PellLampAfter",s=>Flag(s,"pell.lampReturned")&&!Flag(s,"lanternwick.chartGiven")),
            // Ishen's record (IshensRecord): three record plates from the Sunken Relay.
            new("Ishen","IshenRecordReturn",s=>Step(s,"IshensRecord")==1),
            // Unsworn rations (UnswornRations): Corra feeds a Muster deserter hiding in the orchard shed.
            new("Brekk","BrekkRations",s=>Step(s,"UnswornRations")==1),
            // Aumi can see them from the stair. Her ordinary line went on asking for relays already burning.
            new("Aumi","AumiRelaysLit",s=>Met(s,"Aumi")&&Flag(s,"gorge.relaysLit")&&!Flag(s,"bossdown.HessaKorl")),
            // Mira thanks the party once for the six crystals (her post tier), then goes back to selling.
            new("Mira","MiraRepeat",s=>Flag(s,"mira.thanked")),
            // The captain's surrender: heard once, wherever she is standing (D202). Once the chart is given the fight is long over,
            // and she can't still be shouting as if it had just ended.
            new("Hessa","HessaYieldsLate",s=>Flag(s,"bossdown.HessaKorl")&&!Flag(s,"relay.hessaYielded")&&Flag(s,"lanternwick.chartGiven")),
            new("Hessa","HessaYields",s=>Flag(s,"bossdown.HessaKorl")&&!Flag(s,"relay.hessaYielded")),
            new("Corra","CorraRationsOffer",s=>Step(s,"UnswornRations")<0&&Met(s,"Corra")&&Step(s,"LanternRoad")>=3),
        };

        /// <summary>First meetings the story has overtaken. Dace's introduction has the Burrower still hunting and sends the party to a key
        /// they may hold, and a parcel in hand used to wait for a second talk nothing asks for. The meeting keeps its place and marks him
        /// met as any other does; only its words change. The parcel nodes play the meeting, then the delivery, as one talk.</summary>
        static readonly Beat[] LateMeetings=
        {
            // Nobody spoke to Maru after the stern line and Ona has sent them back to her: the tide talk is the meeting, and reads right as one (ch04 manifest).
            new("Maru","MaruTide",s=>Flag(s,"wend.onaHeard")&&!Flag(s,"wend.tideTurned")),
            new("Survivor","SurvivorLateParcel",s=>Flag(s,"bossdown.Burrower")&&Step(s,"NeveParcel")==0),
            new("Survivor","SurvivorLate",s=>Flag(s,"bossdown.Burrower")),
            new("Survivor","SurvivorFirstParcel",s=>Step(s,"NeveParcel")==0),
        };
        /// <summary>The conversation that takes the place of this speaker's first node, or null for the one as written.</summary>
        public static string FirstMeetingFor(string speaker,GameState state)
        {
            if(state==null||string.IsNullOrEmpty(speaker))return null;
            foreach(var beat in LateMeetings)if(beat.speaker==speaker&&beat.when(state))return beat.node;
            return null;
        }
        /// <summary>The side-story conversation for this speaker now, or null for their ordinary tiers.</summary>
        /// <summary>Keeper Abri's next talk is one a chapter waits on: her last word on the chapter just told, or the offer that opens the next (chapters 3 and 4 begin
        /// aboard Tallow this way). The objective's arrow points at her while this holds; nothing else in the game says she has more to say.</summary>
        public static bool KeeperOpensAChapter(GameState state)
        {
            string node=NodeFor("Keeper",state);
            return node=="AbriLanternAfter"||node=="AbriSeedRoad"||node=="AbriDinnerServed"&&!Flag(state,ChapterThreeAfterword)||node=="AbriReedRoad";
        }
        public static string NodeFor(string speaker,GameState state)
        {
            if(state==null||string.IsNullOrEmpty(speaker))return null;
            foreach(var beat in Beats)if(beat.speaker==speaker&&beat.when(state))return beat.node;
            return null;
        }
    }
}
