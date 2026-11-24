using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Many Hands' people (named and not), the chapter's story wiring (manifest section 2: who stands where, when) and the floor's dressing, last.</summary>
    public static partial class ManyHandsBuilder
    {
        internal static partial class Hands
        {
            // ---------------------------------------------------------------- stills
            /// <summary>LookDev's views of the city (its row in LookDev.Views reads this): each district as it is first walked, the places a talk or the fight happens, two wide views.
            /// The gallery's points are at its floor, 3.6 m up. Made on first read, the arrivals' after the city's own: this class's static setup never reads Arrivals, so either
            /// can be read first (a chapter's staging reads Arrivals' places before anything else of the city; ManyHandsArrivalsInitTests).</summary>
            internal static LookDev.View[] Views=>views??=CityViews.Concat(Arrivals.Views).ToArray();
            static LookDev.View[] views;
            static readonly LookDev.View[] CityViews=
            {
                new("arrival",new Vector3(-4,0,-58)),new("dock-door",new Vector3(6,0,-56)),new("dock-west",new Vector3(-22,0,-60)),new("freight-yard",new Vector3(26,0,-60)),
                new("spine-south",new Vector3(0,0,-45)),new("gantry",new Vector3(0,0,-32)),new("spine-north",new Vector3(0,0,-18)),new("cradle",new Vector3(-9,0,-45)),
                new("hall-door",new Vector3(24,0,-48)),new("hall",new Vector3(32,0,-36)),new("hall-north",new Vector3(32,0,-25)),
                new("crossing",new Vector3(0,0,3)),new("arcade-west",new Vector3(-24,0,4)),new("arcade-east",new Vector3(22,0,4)),new("arcade-far-east",new Vector3(42,0,4)),
                new("galley",new Vector3(64,0,3)),new("galley-hatch",new Vector3(66,0,9.5f)),new("galley-table",new Vector3(63,0,-4.5f)),
                new("garden-entrance",new Vector3(20,0,22)),new("garden",new Vector3(44,0,32)),new("garden-north",new Vector3(52,0,48)),new("racks",new Vector3(72,0,46)),
                new("raft-quay",new Vector3(-12,0,-24)),new("ferry-raft",new Vector3(-26,0,-24)),new("weavers-raft",new Vector3(-50,0,-36)),new("mat-raft",new Vector3(-58,0,-20)),
                new("north-quay",new Vector3(-50,0,2)),new("tide-board",new Vector3(-58,0,10)),
                new("cabins-south",new Vector3(-40,0,20)),new("cabins-lane",new Vector3(-60,0,40)),new("breach",new Vector3(-60,0,49)),
                new("forecourt-west",new Vector3(-18,0,28)),new("forecourt-east",new Vector3(16,0,28)),new("stair",new Vector3(0,0,25)),
                new("gallery",new Vector3(0,GalleryTop,44)),new("gallery-east",new Vector3(12,GalleryTop,49)),new("gallery-west",new Vector3(-14,GalleryTop,46)),
                new("overview",new Vector3(0,0,-2),false,4.6f),new("overview-north",new Vector3(18,0,36),false,3),
                // Where the whole-game capture's stills stood (nov20-whole s30 284 and 327: the galley; s30 129: the cabins' breach; s28 197: the raft quay; s31 163: the garden).
                new("galley-s30-284",new Vector3(59.6f,0,3)),new("galley-s30-327",new Vector3(60.8f,0,-5.2f)),new("cabins-s30-129",new Vector3(-57.3f,0,49.3f)),
                new("raft-s28-197",new Vector3(-21.3f,0,-24)),new("garden-s31-163",new Vector3(62.6f,0,24.2f)),
                // November 22's (nov22-whole s28 043: the spine under the gantry; s29 070: the freight hall's arrival; s31 285 and s28 222: the raft bay's water).
                new("spine-s28-043",new Vector3(0,0,-20.75f)),new("freight-s29-070",new Vector3(32,0,-59)),new("raft-s31-285",new Vector3(-43.3f,0,-35)),new("raft-s28-222",new Vector3(-24,0,-11.9f)),
                // November 22b's (nov22b-whole s31 129 and 208: the galley's flues and hatch wall from the garden's walk; s44 092 and s55 014: the spine's lanes and its foot).
                new("garden-s31-129",new Vector3(62,0,24.5f)),new("garden-s31-208",new Vector3(55,0,24.2f)),new("spine-s44-092",new Vector3(0,0,-39.5f)),new("spine-s55-014",new Vector3(-.15f,0,-52.2f)),
            };

            // ---------------------------------------------------------------- named people
            /// <summary>Someone with a name at one of their places. Who says what aboard is chapter 5's beats (SideStories, scoped to this zone): the tiers here are what the wiring test
            /// reads and what a first meeting plays for someone never met before.</summary>
            static Npc Person(string id,Vector2 at,float yaw,string first,string repeat,string post)
            {
                var npc=Kit.Person(id,new Vector3(at.x,0,at.y),yaw,"hands.dinnerDone",!string.IsNullOrEmpty(post),null,null,first);
                npc.firstNode=first;npc.repeatNode=repeat;npc.postNode=post??"";if(string.IsNullOrEmpty(post))npc.postFlag="";
                return npc;
            }
            /// <summary>A holder for someone who leaves in sight (LeavesAfter switches them off) so that whatever decides they are here can't switch them back on.</summary>
            static GameObject Holder(string name,Npc npc){var holder=new GameObject(name);npc.transform.SetParent(holder.transform,true);return holder;}
            static void Where(string name,GameObject target,string from,string awayFrom,string backAt,string goneAt,bool live)
            {
                var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{target};
            }
            static void Leaves(Npc npc,string flag,params Vector2[] path){var leave=npc.gameObject.AddComponent<LeavesAfter>();leave.flag=flag;leave.path=path.Select(p=>At(p)).ToArray();}
            /// <summary>At the dinner from Meret's arrival: shown as she is met (the galley out of sight at the cabins), kept to the next load once the dinner is done, so nobody vanishes
            /// from the table in front of the party; their posts take them back at once, out of sight.</summary>
            static void AtTable(string name,Npc npc,string from="hands.meretMet")
            {
                var outer=new GameObject(name);var inner=new GameObject(name+", seated");inner.transform.SetParent(outer.transform,true);npc.transform.SetParent(inner.transform,true);
                Where(name+" comes to dinner",outer,from,null,null,null,true);Where(name+" has left the table",inner,null,null,null,"hands.dinnerDone",false);
            }
            /// <summary>At their post except for the dinner.</summary>
            static void Post(string name,GameObject who,string from=null)=>Where(name,who,from,"hands.meretMet","hands.dinnerDone",null,true);

            static void People(ZoneKit kit)
            {
                // Keeper Abri: on the dock for the welcome, then she walks up the spine to her desk on the gallery; at the dinner table for its length.
                var abriDock=Person("Keeper",AbriDock,210,"AbriHandsArrival","AbriHandsRepeat","AbriHandsPost");Holder("Keeper Abri on the dock",abriDock);
                Leaves(abriDock,"hands.aboard",new Vector2(3.2f,-52),new Vector2(3.2f,-40),new Vector2(3.2f,-24));
                var abri=Person("Keeper",Abri,180,"AbriHandsArrival","AbriHandsRepeat","AbriHandsPost");Post("Keeper Abri at her desk",abri.gameObject,"hands.aboard");
                AtTable("Keeper Abri at dinner",Person("Keeper",AbriSeat,0,"AbriHandsArrival","AbriHandsRepeat","AbriHandsPost"));
                // Pell on the gallery, Binna at her hatch, Hadda, Corra and Dov in the arcade: at their posts throughout.
                Person("Pell",Pell,200,"PellChartLamps","PellHandsRepeat","PellHandsPost");
                Person("Binna",Binna,180,"BinnaVent","BinnaHandsRepeat","BinnaHandsPost");
                Person("Hadda",Hadda,200,"HaddaHands","HaddaHandsRepeat","HaddaHandsPost");
                Person("Corra",Corra,180,"CorraHands","CorraHandsRepeat","CorraHandsPost");
                Person("Dov",Dov,180,"DovHands","DovHandsRepeat","DovHandsPost");
                // The garden: Ruva and Teo (at the table for the dinner), Garo on Marrow-2's deck, Dabb at the pumps.
                var ruva=Person("Ruva",Ruva,200,"RuvaMooring","RuvaHandsRepeat","RuvaHandsPost");Post("Ruva in the garden",ruva.gameObject);
                AtTable("Ruva at dinner",Person("Ruva",RuvaSeat,180,"RuvaMooring","RuvaHandsRepeat","RuvaHandsPost"));
                var teo=Person("Teo",Teo,220,"TeoHands","TeoHandsRepeat","TeoHandsPost");Post("Teo on the garden deck",teo.gameObject);
                AtTable("Teo at dinner",Person("Teo",TeoSeat,180,"TeoHands","TeoHandsRepeat","TeoHandsPost"));
                Person("Garo",Garo,180,"GaroHands","GaroHandsRepeat","GaroHandsPost");
                Person("Dabb",Dabb,240,"DabbHands","DabbHandsRepeat","DabbHandsPost");
                // The raft bay: Maru on the ferry raft (standing by the galley wall for the dinner), Ona at her bench, Pim on the mat raft.
                var maru=Person("Maru",Maru,200,"MaruMooring","MaruHandsRepeat","MaruHandsPost");Post("Maru in the raft bay",maru.gameObject);
                AtTable("Maru by the galley wall",Person("Maru",MaruWall,260,"MaruMooring","MaruHandsRepeat","MaruHandsPost"));
                Person("Ona",Ona,180,"OnaHands","OnaHandsRepeat","OnaHandsPost");
                Person("Pim",Pim,190,"PimHands","PimHandsRepeat","PimHandsPost");
                // Jask: hidden behind the seed racks until he is found (a body, not someone to talk to); in the racks once found until he is placed; then where Teo puts him; at the table
                // for the dinner if he was found.
                var hiding=kit.Townsperson("JaskBody",new Vector3(JaskHid.x,0,JaskHid.y),250,new Color(1,1,1),"Orrin");hiding.name="Jask, hiding";
                foreach(var c in hiding.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                Where("Jask is hiding",hiding,null,null,null,"hands.jaskFound",true);
                var racks=Person("Jask",JaskHid,250,"JaskFoundRepeat","JaskFoundRepeat","JaskHandsPost");Where("Jask in the racks",racks.gameObject,"hands.jaskFound","hands.meretMet","hands.dinnerDone","hands.jaskPlaced",true);
                foreach(var (at,flag,line) in new[]{(JaskGarden,"jask.worksGarden","JaskGardenHand"),(JaskFerry,"jask.worksFerry","JaskFerryHand"),(JaskLamps,"jask.worksLamps","JaskLampHand")})
                    Post("Jask at work",Person("Jask",at,160,line,line,"JaskHandsPost").gameObject,flag);
                AtTable("Jask at dinner",Person("Jask",JaskSeat,180,"JaskFoundRepeat","JaskFoundRepeat","JaskHandsPost"),"hands.jaskAtTable");
                // Ilo: on the dock from the three jobs' end (he walks up to the gallery once he has been heard), at his desk beside Abri's after, at the table for the dinner.
                var iloDock=Person("Ilo",IloDock,160,"IloArrives","IloHandsRepeat","IloHandsPost");var iloAtDock=Holder("Ilo on the dock",iloDock);
                Where("Ilo comes off the courier",iloAtDock,"hands.jobsDone",null,null,null,true);Leaves(iloDock,"hands.claimRead",new Vector2(-4,-52),new Vector2(-3.4f,-40),new Vector2(-3.4f,-26));
                var ilo=Person("Ilo",Ilo,180,"IloArrives","IloHandsRepeat","IloHandsPost");Post("Ilo at his desk",ilo.gameObject,"hands.iloAboard");
                AtTable("Ilo at dinner",Person("Ilo",IloSeat,0,"IloArrives","IloHandsRepeat","IloHandsPost"));
                // The Steady Hand's captain at the sealed cabins from the yield until the chapter closes; Meret behind him (a body until he has spoken to her, then herself), then at
                // the table, then at the galley door, then by her cutter on the dock.
                var patrol=Person("Patrol",Patrol,190,"PatrolBreachSealed","PatrolRepeat","PatrolRepeat");Where("The captain at the seal",patrol.gameObject,"hands.dessikYielded",null,null,"chapter5.complete",true);
                var behind=kit.Townsperson("MeretBody",new Vector3(MeretCabins.x,0,MeretCabins.y),200,Color.white,"Mira");behind.name="Meret, behind the captain";foreach(var c in behind.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                Where("Meret waits behind the captain",behind,"hands.dessikYielded",null,null,"hands.breachSealed",true);
                var meret=Person("Meret",MeretCabins,200,"MeretArrives","MeretArrives","MeretArrives");Where("Meret at the cabins",Holder("Meret at the cabins",meret),"hands.breachSealed",null,null,null,true);
                Leaves(meret,"hands.meretMet",new Vector2(-58,50.2f),new Vector2(-52,46),new Vector2(-52,34));
                var seated=Person("Meret",MeretSeat,180,"MeretArrives","MeretArrives","MeretArrives");var seat=Holder("Meret at the table",seated);
                // Someone to talk to only when it's her turn at the table (after Binna's meal; after Ruva and Ilo have had theirs); a figure at her place the rest of the dinner, so
                // a talk with her can't come out of turn and she has nothing to repeat.
                Where("Meret's turn at the table",seat,"hands.dinnerSat","hands.stillwaterHeard","hands.footnotesTabled",null,true);Leaves(seated,"hands.sorrelNamed",new Vector2(59,3.6f),GalleyDoor);
                var atPlace=kit.Townsperson("MeretBody",new Vector3(MeretSeat.x,0,MeretSeat.y),180,Color.white,"Mira");atPlace.name="Meret, at her place";
                foreach(var c in atPlace.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
                Where("Meret listens at the table",atPlace,"hands.meretMet","hands.dinnerSat","hands.stillwaterHeard","hands.footnotesTabled",true);
                var standing=Person("Meret",GalleyDoor,100,"MeretArrives","MeretArrives","MeretArrives");var door=Holder("Meret at the galley door",standing);
                var up=new GameObject("Meret gets up and goes to the door",typeof(TurnsUp)).GetComponent<TurnsUp>();up.flag="hands.sorrelNamed";up.delay=4.6f;up.targets=new[]{door};door.SetActive(false);
                Leaves(standing,"hands.dinnerDone",new Vector2(50,6),new Vector2(36,4));
                var parting=Person("Meret",MeretDock,120,"MeretArrives","MeretArrives","MeretArrives");var dock=Holder("Meret by her cutter",parting);
                Where("Meret goes down to her cutter",dock,"hands.dinnerDone",null,null,"chapter5.complete",true);Leaves(parting,"hands.partingHeard",MeretCutter+new Vector2(3.4f,1.2f));

                // Everyone else who lives here.
                foreach(var (body,x,z,yaw,tint,doing) in Folk){var one=kit.Townsperson(body,new Vector3(x,0,z),yaw,tint,"Neve");one.name="Townsperson, "+doing;Carry(one,doing);}
                foreach(var (body,x,z,yaw,tint) in Children){var child=kit.Townsperson(body,new Vector3(x,0,z),yaw,tint,"Neve");child.transform.localScale=Vector3.one*.74f;child.name="A child playing on the quay";}
                foreach(var (body,loop,speed,pause,tint,back,name) in Errands)
                {
                    var walker=kit.Walker(body,loop,speed,pause,tint,"Neve");walker.thereAndBack=back;walker.name=name;
                    if(name.StartsWith("A child"))walker.transform.localScale=Vector3.one*.74f;
                    Carry(walker.gameObject,name);
                }
            }
            /// <summary>What someone is carrying, which is how a still says what they are doing: a form, a tray of seedlings, a mat, a crate, a basket of laundry, a trimmer's pole.</summary>
            static void Carry(GameObject who,string doing)
            {
                ZoneKit.Shape load=null;Material[] stuff=null;
                if(doing.Contains("form")){load=new ZoneKit.Shape().Quad(new Vector3(-.16f,1.05f,.36f),new Vector3(.16f,1.05f,.36f),new Vector3(.16f,1.45f,.3f),new Vector3(-.16f,1.45f,.3f),new Vector3(0,.2f,1),new Color(.96f,.94f,.88f),.5f);stuff=new[]{Cloth};}
                else if(doing.Contains("tray")||doing.Contains("seedlings")){load=new ZoneKit.Shape().Slot(0).Box(new Vector3(0,1.08f,.42f),new Vector3(.7f,.1f,.46f),Quaternion.identity,new Color(.5f,.4f,.3f)).Slot(1).Lump(new Vector3(0,1.2f,.42f),new Vector3(.3f,.1f,.2f),3,.3f,new Color(.5f,.76f,.38f),new Color(.3f,.5f,.24f),8,3);stuff=new[]{Wood,Leaf};}
                else if(doing.Contains("mat")){load=new ZoneKit.Shape().Box(new Vector3(0,1.1f,.45f),new Vector3(1.1f,.9f,.06f),Quaternion.Euler(-12,0,0),new Color(.5f,.66f,.86f));stuff=new[]{Weave};}
                else if(doing.Contains("crate")){load=new ZoneKit.Shape().Box(new Vector3(0,1.15f,.42f),new Vector3(.6f,.5f,.5f),Quaternion.identity,new Color(.78f,.62f,.42f),new Color(.86f,.7f,.48f),.6f);stuff=new[]{Wood};}
                else if(doing.Contains("laundry")){load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.3f,.78f,.1f),new Vector3(.3f,1.08f,.1f)},new[]{.24f,.3f},10,new Color(.8f,.68f,.46f),new Color(.9f,.8f,.56f),2,.8f).Slot(1).Lump(new Vector3(.3f,1.1f,.1f),new Vector3(.26f,.12f,.26f),2,.3f,new Color(.86f,.56f,.36f),new Color(.6f,.66f,.86f),8,3);stuff=new[]{Weave,Cloth};}
                else if(doing.Contains("trimmer")){load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.32f,.4f,.1f),new Vector3(.4f,2.6f,.3f)},new[]{.025f,.02f},5,new Color(.5f,.4f,.3f),new Color(.7f,.58f,.4f),1,.6f).Slot(1).Lump(new Vector3(.4f,2.66f,.3f),new Vector3(.07f,.09f,.07f),1,.02f,Color.white,Color.white,6,4);stuff=new[]{Wood,Lamp};}
                if(load==null)return;
                var carried=Kit.Built(who.name+" carries",load,stuff);carried.transform.SetParent(who.transform,false);carried.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }

            // ---------------------------------------------------------------- the chapter's story, wired into the world (manifest section 2)
            static void Story(ZoneKit kit)
            {
                // Coming aboard: Abri's welcome on a crossing over the dock, a step from the arrival; the dock door to read.
                kit.Crossing("Keeper Abri meets the party at the dock",new Vector3(Arrival.x+2,0,Arrival.y+2.6f),new Vector3(30,5,2.4f),"hands.welcomeHeard","AbriHandsArrival","Keeper");
                kit.Discovery("The dock door",new Vector3(DoorChalk.x,0,DoorChalk.y),"Look at the dock door","hands.doorRead","","SelaDockDoor","Sela",null,0,2.4f);
                // The mooring: the rings are fitted at the hold's north-east wall once both sides have been heard.
                All("Both sides of the mooring are heard","hands.bothHeard","hands.ruvaHeard","hands.maruHeard");
                var rings=kit.Discovery("The hold wall",new Vector3(RingsAt.x,0,RingsAt.y),"Fit the running rings","hands.ringsFitted","MooringRail","SelaMooringRail","Sela",null,0,2.6f);rings.requiredFlag="hands.bothHeard";
                // The chart: Pell heard (hands.pellAboard, SideStories.HeardFlagFor), the three gallery lamps sighted from the scopes round the table, then the table itself.
                for(int i=0;i<3;i++){var scope=kit.Discovery("Gallery lamp "+(i+1),new Vector3(Scopes[i].x,0,Scopes[i].y),"Sight the gallery lamp","hands.lamp"+(i+1),"GalleryLamp","","Sela",null,0,1.3f);scope.requiredFlag="hands.pellAboard";}
                All("The three gallery lamps are sighted","hands.lampsSighted","hands.lamp1","hands.lamp2","hands.lamp3");
                var chart=kit.Discovery("The chart table",new Vector3(ChartTalk.x,0,ChartTalk.y),"Open the route chart","hands.chartSet","GalleryLamp","SelaChartOpen","Sela",null,0,2);chart.requiredFlag="hands.lampsSighted";
                var read=kit.Discovery("The chart after dinner",new Vector3(ChartTalk.x,0,ChartTalk.y),"Read the chart","hands.chartLooked","ChartTable","SelaLampGallery","Sela",null,0,2);read.requiredFlag="hands.dinnerDone";
                // The three jobs done: Ilo comes off the courier, and when he has been heard Dessik's claim comes over the radio.
                All("The three jobs are done","hands.jobsDone","hands.mooringSettled","hands.ventFixed","hands.chartOpen");
                var claim=new GameObject("Dessik Varn reads his claim",typeof(AfterTalk)).GetComponent<AfterTalk>();claim.flag="hands.iloAboard";claim.node="DessikClaim";claim.speaker="Dessik";claim.heardFlag="hands.claimRead";claim.delay=2.5f;claim.range=0;
                // Jask, found behind the racks off Marrow-3; at the table if he was.
                kit.Discovery("Behind the seed racks",new Vector3(JaskLook.x,0,JaskLook.y),"Look behind the seed racks","hands.racksLooked","","JaskFound","Sela",null,0,2.2f);
                All("Jask was found before dinner","hands.jaskAtTable","hands.meretMet","hands.jaskFound");
                FreightHallFight(kit);
            }
            static void All(string name,string result,params string[] flags){var all=new GameObject(name,typeof(FlagAll)).GetComponent<FlagAll>();all.flags=flags;all.result=result;}

            /// <summary>The freight hall, built to the contract (docs/maps/ManyHands.md): the cargo door shut until the frame comes through the roof, the talk just inside, the seal that
            /// shuts behind it, the fight, the room a combat place for its length (FightPocket), and Dessik in custody after.</summary>
            static void FreightHallFight(ZoneKit kit)
            {
                Color leaf=new(.62f,.44f,.32f);
                var shut=WorldBuilder.Membrane("Freight hall cargo door",new Vector3(HallDoor.x,2.6f,-53.25f),8.2f,false,5.2f);
                foreach(var f in shut.GetComponentsInChildren<MeshFilter>())Object.DestroyImmediate(f);foreach(var r in shut.GetComponentsInChildren<Renderer>())Object.DestroyImmediate(r);
                var shutBox=shut.GetComponent<BoxCollider>();var hold=shut.gameObject.AddComponent<NavMeshObstacle>();hold.shape=NavMeshObstacleShape.Box;hold.center=shutBox.center;hold.size=shutBox.size;hold.carving=true;hold.carveOnlyStationary=false;
                var leafShape=new ZoneKit.Shape().Slot(0).Box(new Vector3(HallDoor.x,2.6f,-53.25f),new Vector3(8.2f,5.2f,.3f),Quaternion.identity,leaf,ZoneKit.Shape.Dim(leaf,1.1f),.36f);
                for(int i=0;i<8;i++)leafShape.Slot(1).Box(new Vector3(HallDoor.x-3.5f+i,.3f,-53.42f),new Vector3(.5f,.4f,.02f),Quaternion.identity,i%2==0?Hazard:Soot);
                var leafView=kit.Built("Freight hall cargo door leaf",leafShape,Corrugated,Panel);leafView.transform.SetParent(shut.transform,true);
                shut.SetOpen(false);Gate("The cargo door opens for the frame",new[]{shut.gameObject},"bossdown.Hulljack",false);
                // The seal across the door behind the party: strands of light, solid and carving while shut.
                var seal=WorldBuilder.Membrane("Freight hall seal",new Vector3(HallDoor.x,2,-52.1f),8.4f,true,4);
                foreach(var f in seal.GetComponentsInChildren<MeshFilter>())Object.DestroyImmediate(f);foreach(var r in seal.GetComponentsInChildren<Renderer>())Object.DestroyImmediate(r);
                var door=seal.GetComponent<BoxCollider>();var sealObstacle=seal.gameObject.AddComponent<NavMeshObstacle>();sealObstacle.shape=NavMeshObstacleShape.Box;sealObstacle.center=door.center;sealObstacle.size=door.size;sealObstacle.carving=true;sealObstacle.carveOnlyStationary=false;sealObstacle.enabled=false;
                var beam=kit.Glow("Hands seal",true,new Color(.5f,.9f,1,1));var wave=new System.Random(83);
                for(float x=HallDoor.x-3.8f;x<=HallDoor.x+3.8f;x+=.95f){float rise=3+(float)wave.NextDouble()*.8f,lean=((float)wave.NextDouble()-.5f)*.3f;ZoneKit.Strand(seal.transform,new Vector3(x,0,-52.1f),new Vector3(x+lean,rise,-52.1f),beam,1.5f,new Color(.5f,.86f,1,.6f),new Color(.5f,.86f,1,0));}
                foreach(var strand in seal.GetComponentsInChildren<LineRenderer>())strand.enabled=false;
                var duel=WorldBuilder.Encounter("DessikVarn",new Vector3(F.x,1,F.y),new Vector3(37,5,33),"DessikVarn",1,new[]{seal});
                var post=duel.GetComponentInChildren<Spawner>();post.transform.position=At(F);post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="hands.hallEntered";duel.checkpointSpawn="FreightHall";
                var viewpoint=new GameObject("Dessik Varn on show");viewpoint.transform.position=At(HallTalk);duel.previewPoint=viewpoint.transform;duel.previewRadius=18;
                var talk=kit.Crossing("Dessik Varn in the freight hall",new Vector3(HallTalk.x,0,HallTalk.y),new Vector3(8,5,2),"hands.hallEntered","DessikHall","Dessik","bossdown.Hulljack");
                talk.saveSpawn="FreightHall";talk.previewEncounter=duel;
                // The room is a combat place while its fight is on; the freight crew are at work before and after.
                var room=new GameObject("Freight hall",typeof(BoxCollider));room.transform.position=new Vector3(31.8f,3,-34.8f);var box=room.GetComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(38.4f,8,35.4f);
                var pocket=room.AddComponent<FightPocket>();pocket.from="bossdown.Hulljack";pocket.until="bossdown.DessikVarn";
                GameObject Crew(string name,params (string body,Vector2 at,float yaw,Color tint)[] who)
                {
                    var crew=new GameObject(name);foreach(var (body,at,yaw,tint) in who){var one=kit.Townsperson(body,new Vector3(at.x,0,at.y),yaw,tint,"Neve");one.name="Townsperson, freight crew";Carry(one,"a crate");one.transform.SetParent(crew.transform,true);
                        // Not in the bake: the room's floor has to be whole in every state, and the crew are gone while the fight is on.
                        foreach(var t in one.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;}
                    return crew;
                }
                // The hall's three states (docs/maps/ManyHands.md, "The way in from the hull ... and the staging"): at work, the fight's room, at work again.
                var hoist=At(F,0);Light Hung(GameObject parent,Vector3 at,Color colour,float range,float intensity){var go=new GameObject("Hall light",typeof(Light));go.transform.SetParent(parent.transform,true);go.transform.position=at;
                    var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=colour;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;return l;}
                var before=Crew("The freight crew at work",("Orrin",new Vector2(37.6f,-24.6f),200,new Color(.86f,.82f,.74f)),("OkhariGrower",new Vector2(46.6f,-46.6f),300,new Color(.8f,.9f,.86f)));
                var working=new ZoneKit.Shape();
                working.Slot(0).Box(hoist+new Vector3(0,7.2f,0),new Vector3(2.2f,.9f,1.6f),Quaternion.Euler(0,14,0),new Color(.76f,.6f,.42f),new Color(.86f,.7f,.48f),.6f);
                foreach(var (dx,dz) in new[]{(-.9f,-.6f),(.9f,-.6f),(-.9f,.6f),(.9f,.6f)})working.Slot(1).Rope(hoist+new Vector3(dx,7.6f,dz),hoist+new Vector3(0,11.5f,0),0,.02f,Soot,4,2);
                working.Slot(0).Box(At(39.6f,-27.4f,.5f),new Vector3(.8f,1,.5f),Quaternion.Euler(0,30,0),new Color(.5f,.52f,.56f)).Slot(1).Tube(new[]{At(39.3f,-27.6f,.12f),At(39.9f,-27.2f,.12f)},new[]{.12f,.12f},8,Soot,Soot,1,1);
                Shade(kit.Built("The hall at work",working,Wood,Panel),false).transform.SetParent(before.transform,true);
                Hung(before,hoist+new Vector3(-3,8.5f,2),new Color(1,.84f,.62f),14,1.2f);
                var during=new GameObject("The hall staged for the fight");
                var bell=new ZoneKit.Shape();var rays=new System.Random(91);
                for(int k=0;k<10;k++){float a=k*Mathf.PI/5+(float)rays.NextDouble()*.3f;var top=hoist+new Vector3(Mathf.Cos(a)*1.4f,13,Mathf.Sin(a)*1.4f);var foot=hoist+new Vector3(Mathf.Cos(a)*3.6f,.1f,Mathf.Sin(a)*3.6f);bell.Slot(0).Quad(top,top+Vector3.right*.6f,foot+Vector3.right*1.4f,foot,Vector3.back,new Color(.86f,.92f,1),.3f);}
                Shade(kit.Built("Light through the torn roof",bell,kit.Glow("Roof light",true,new Color(.7f,.8f,.9f,1))),false).transform.SetParent(during.transform,true);
                var torn=new ZoneKit.Shape();
                torn.Slot(0).Quad(At(26,-17.4f,0),At(31,-17.4f,0),At(31.6f,-17.9f,3.6f),At(25.4f,-17.9f,3.4f),Vector3.back,new Color(.62f,.44f,.32f),.36f);
                torn.Slot(0).Quad(At(50.6f,-30,0),At(50.6f,-25,0),At(50.1f,-24.6f,3.2f),At(50.1f,-30.6f,3.6f),Vector3.left,new Color(.62f,.44f,.32f),.36f);
                for(int k=0;k<9;k++){float a=k*Mathf.PI*2/9;var c=At(F.x+Mathf.Cos(a)*7.6f,F.y+Mathf.Sin(a)*7.6f,.02f);torn.Slot(1).Quad(c+new Vector3(-.9f,0,-.5f),c+new Vector3(.9f,0,-.5f),c+new Vector3(.9f,0,.5f),c+new Vector3(-.9f,0,.5f),Vector3.up,new Color(.12f,.11f,.11f),.5f);}
                Shade(kit.Built("Floor detail torn leaves and scorch",torn,Corrugated,Dark),false).transform.SetParent(during.transform,true);
                Hung(during,hoist+Vector3.up*6,new Color(.86f,.92f,1),12,1.6f);
                var after=Crew("The freight crew back at work",("SurvivorNatural",new Vector2(36.4f,-22.6f),190,new Color(.84f,.86f,.92f)),("SennBody",new Vector2(47.4f,-24),240,new Color(.92f,.86f,.76f)));
                var work=new ZoneKit.Shape().Slot(0).Rope(hoist+new Vector3(0,7.2f,0),hoist+new Vector3(0,12,0),0,.02f,Soot,4,2).Slot(1).Lump(hoist+new Vector3(0,7,0),new Vector3(.3f,.36f,.3f),3,.04f,Color.white,Color.white,8,5);
                Shade(kit.Built("Work lamp over the hall",work,Rope,Lamp),false).transform.SetParent(after.transform,true);
                Hung(after,hoist+Vector3.up*6.6f,new Color(1,.86f,.64f),14,1.4f);
                // The cabin alarm at Dessik's half-way shift: blue-white strobes high on the north and east walls, a light for each (never orange, red or purple on the floor: D217).
                var alarm=new GameObject("The cabin alarm in the hall");var strobes=new ZoneKit.Shape();var strobe=ZoneKit.Toon("hands-strobe",null,m=>{m.SetColor("_BaseColor",new Color(.86f,.94f,1));m.SetColor("_EmissionColor",new Color(1.6f,2.2f,3));});
                foreach(var at in new[]{new Vector2(32,-17.6f),new Vector2(50.6f,-34)}){strobes.Lump(At(at,5.8f),new Vector3(.3f,.22f,.3f),(int)at.x,.03f,Color.white,Color.white,10,5);Hung(alarm,At(at,5),new Color(.76f,.88f,1),12,1.4f);}
                Shade(kit.Built("Alarm strobes",strobes,strobe),false).transform.SetParent(alarm.transform,true);
                pocket.before=new[]{before};pocket.during=new[]{during};pocket.after=new[]{after};pocket.alarm=new[]{alarm};pocket.alarmBoss="DessikVarn";pocket.alarmPhase=2;
                during.SetActive(false);after.SetActive(false);alarm.SetActive(false);
                // Dessik in custody: his own person takes over from the frame where it kneels, says his piece, and on later visits stands by the door under Abri's people.
                var dessik=Person("Dessik",DessikHeld,60,"DessikYields","DessikHeld","DessikHeldPost");
                var yielded=new GameObject("Dessik Varn in custody",typeof(YieldedCaptain)).GetComponent<YieldedCaptain>();yielded.npc=dessik;dessik.gameObject.SetActive(false);
                yielded.bossId="DessikVarn";yielded.downFlag="bossdown.DessikVarn";yielded.heardFlag="hands.dessikYielded";yielded.guardedFlag="hands.dessikYielded";yielded.guardedNode="";
                var guards=new[]{kit.Townsperson("Hal",new Vector3(DessikHeld.x-1.6f,0,DessikHeld.y+.8f),80,Color.white,"Neve"),kit.Townsperson("Mira",new Vector3(DessikHeld.x+1.7f,0,DessikHeld.y+1.2f),250,Color.white,"Neve")};
                foreach(var g in guards){g.name="Keeper Abri's watch";g.SetActive(false);}yielded.guards=guards;
            }

            // ---------------------------------------------------------------- the floor, last
            /// <summary>The ground kit's layers (the liner's tile in the arcade and galley, boards in the garden and on the rafts, grime off the ways on the hauler's plate), flat details
            /// (scuffs, scorch at the dock and in the hall, cracks at the joins, litter where people stand about), low cover in the garden and along the bay's quay, lamp pools, the
            /// moving shade of what passes over the ship. Without the kit's textures the floor keeps its vertex tones and says so.</summary>
            static void Ground(ZoneKit kit,ZoneKit.Land land)
            {
                if(!File.Exists(GroundKit.Art+"ground-litter.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png")||!File.Exists(GroundKit.Art+"ground-shade-cloud.png"))
                {Debug.LogWarning("MANYHANDS_GROUND_KIT_PENDING: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,59,CliffTop);
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.1f,new GroundKit.Layer("hands-liner",new Color(.98f,.92f,.8f,1),4),new GroundKit.Layer("hands-boards",new Color(.9f,.74f,.54f,1),4),
                    new GroundKit.Layer("litter",new Color(.64f,.6f,.52f,1),2.6f));
                ground.Paint(0,p=>MakeAt(p)==Make.Liner?1:0);
                ground.Paint(1,p=>{var m=MakeAt(p);return m==Make.Carrier||m==Make.Raft?1:0;});
                float Grimy(Vector2 p){var m=MakeAt(p);return m==Make.Hauler||m==Make.Hall?1:m==Make.Station?.5f:0;}
                ground.PaintPatches(2,3.4f,.32f,p=>Grimy(p)*ZoneKit.Smooth(1.2f,3,FromWays(p)),5);
                ground.Shade(p=>.08f*(1-ZoneKit.Smooth(0,1.4f,FromWays(p)))*-1+.06f*Grimy(p)*ground.Noise(p,4,3));
                Color soot=new(.3f,.28f,.28f,.7f),oil=new(.22f,.22f,.24f,.6f),scuff=new(.5f,.48f,.44f,.7f),litter=new(.6f,.54f,.42f,.8f);
                ground.Decals(new[]{"Stain","Scorch"},11,p=>Grimy(p)*ZoneKit.Smooth(1,2.6f,FromWays(p)),1.2f,3,oil,false,.12f,1);
                // No wheel ruts: each laid at its own turn, short pairs of dark bars read from the fixed camera as the shadow of something that isn't there (Lastlight's s34/010).
                ground.Decals(new[]{"Stain"},5,p=>Grimy(p)*(1-ZoneKit.Smooth(.6f,2,FromWays(p))),.8f,1.8f,scuff,false,.12f,11);
                ground.Decals(new[]{"CracksA","CracksB"},3,p=>Grimy(p),1,2.4f,scuff,true,.1f,2);
                ground.Decals(new[]{"Tracks"},2.4f,p=>MakeAt(p)==Make.Hauler?1:0,2,4,soot,false,.1f,3);
                ground.Decals(new[]{"Litter","Stain"},5,p=>MakeAt(p)==Make.Liner||MakeAt(p)==Make.Station?ZoneKit.Smooth(1,2.4f,FromWays(p)):0,.6f,1.4f,litter,false,.12f,4);
                ground.Decals(new[]{"Litter","Lichen","PebblesA"},7,p=>MakeAt(p)==Make.Carrier?1:0,.6f,1.6f,new Color(.5f,.56f,.36f,.8f),false,.14f,6);
                ground.Decals(new[]{"Scorch"},3,p=>MakeAt(p)==Make.Hall?1:0,1.6f,3.4f,soot,false,.1f,7);
                foreach(var way in Ways.Where(w=>MakeAt(w[0])==Make.Hauler))ground.Strip("Tracks",way,.3f,1,new Color(.36f,.34f,.32f,.45f));
                // The cabins' lanes are walked every day: worn tracks down each.
                foreach(var way in Ways.Where(w=>w.Any(p=>p.x<-28&&p.y>16)))ground.Strip("Tracks",way,.2f,1.4f,new Color(.34f,.4f,.4f,.5f));
                ground.Cover(new[]{"Fern","Sprouts","TuftA","Flowers"},9,p=>MakeAt(p)==Make.Carrier&&Height(p)>-.05f&&!KeepsOff(p)?1:0,.2f,.5f,new Color(.62f,.82f,.48f,1),3,.14f,8);
                ground.Cover(new[]{"Reeds","TuftC"},5,p=>{float h=Hold(p);return h>RimWide+.2f&&h<RimWide+2.4f&&!KeepsOff(p)?1:0;},.24f,.5f,new Color(.66f,.76f,.5f,1),3,.14f,9);
                ground.Cover(new[]{"Twigs","Shards"},2,p=>Grimy(p)*(KeepsOff(p)?0:1),.08f,.2f,new Color(.7f,.66f,.6f,1),2,.14f,10);
                ground.Look("River",null).LampPools(2.4f,new Color(1,.78f,.48f,.45f));
                ground.MovingShade("cloud",16,new Vector2(.6f,.3f),.18f,.5f,.04f);
                ground.Finish();
            }
        }
    }
}
