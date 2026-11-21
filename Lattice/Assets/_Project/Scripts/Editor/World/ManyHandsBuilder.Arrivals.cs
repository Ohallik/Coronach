using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    public static partial class ManyHandsBuilder
    {
        /// <summary>Stills of the city at each stage of the story, nothing saved (<see cref="Hands.Arrivals.Stills"/>): <c>-arrivals-run name</c>, <c>-arrivals-stages a,b</c>.</summary>
        public static void ArrivalStills()=>BatchTools.Run(()=>{Hands.Arrivals.Stills();Debug.Log("MANYHANDS_ARRIVAL_STILLS_OK");});

        internal static partial class Hands
        {
            /// <summary>
            /// Who comes to live aboard after chapter 5, each where the map brief's "Room for later arrivals" left room (docs/maps/ManyHands.md, "Arrivals as built"): a place, not a
            /// person standing in a gap. Every arrival is one root (named "Arrivals: ...") under a <see cref="Whereabouts"/> decided at load from the flag of the talk that brings it, so
            /// it is there when the party next docks and never appears in view or on anyone. Saved switched off, so nothing of it is in the navigation bake; what is solid carries a
            /// carving obstacle, so the paths change with it. What stands in the freight hall is away for the window the hall is a fight (D252) whatever the flags say.
            /// The named people who come aboard are each chapter's own staging (HoarfellBuilder, OrielBuilder, HarrowgateBuilder.StageManyHands): the places here publish where they
            /// stand (<see cref="HaskAt"/> and the rest). Chapter 9's people wait for its script to be live (docs/writing/pending): until then they are bodies, said so in the log.
            /// Built from <c>Detail</c> (where the school raft is); <see cref="Settle"/> runs once the city's people and story stand, then switches everything off before the bake.
            /// </summary>
            internal static class Arrivals
            {
                internal const string Prefix="Arrivals: ";
                internal const string SixDone="chapter6.complete",SevenDone="chapter7.complete",EightDone="chapter8.complete",NineDone="chapter9.complete",
                    CuttersHome="hoar.sleepLearned",FrameIn="bossdown.Hulljack",FightOver="bossdown.DessikVarn",LanternLit="quest.LumesLantern.complete",GardenerHome="quest.EmptyGardens.complete",
                    MoreSmiths="quest.QuietHands.complete",OdaFound="home.odaFound",TugFound="home.tugFound",NavigatorsFound="home.navigatorsFound",NoticeHeard="home.noticeHeard",
                    HomeReturned="home.returned",Launched="home.launched",BenchesCarried="quest.PortableBenches.complete";
                /// <summary>Flags arrivals here come with that no builder or live script sets yet. None since chapter 9's definitions (HomeBuilder: HouseLeaves sets home.odaFound to
                /// chapter9.complete, CoronachLaunch home.launched, the PortableBenches quest its own); a later arrival whose flag nothing sets is named here.</summary>
                internal static readonly string[] Unset={};

                // ---------------------------------------------------------------- where each arrival stands (plan x, z; the gallery's are at its floor, 3.6 m up)
                // Chapter 6: Idra's cutters, the herd, Iru on the gallery, Idra's crew in the galley.
                // Literals in these initialisers. Hands.Views is made on first read, so neither class reads the other while setting up and either can be read first
                // (when this class read Hands.MeretCutter and Hands' setup took in these Views, reading this class first threw; ManyHandsArrivalsInitTests).
                internal static readonly Vector2 PadCutter=new(-29,-62),CrewAtPad=new(-33.8f,-61.6f),RescueGear=new(-34.6f,-58.9f);
                internal static readonly Vector2[] Lashed={new(-51,-69.4f),new(-41,-69.8f)};
                /// <summary>The pen's fence, west wall to west wall round the hall's west half, leaving Dessik's custody corner out; the gate is the gap at x 27 between z -48.2 and -45.8.
                /// The south run keeps to the gate's south post (z -48.2): the 3.8 m along the south wall (the seal's line, z -52.1) is the way into his corner. At z -50.2 it left under
                /// two metres there, which a body can't pass, and his corner was shut in from chapter 6.</summary>
                internal static readonly Vector2[][] PenSides={new Vector2[]{new(12.9f,-22.2f),new(27,-22.2f),new(27,-45.8f)},new Vector2[]{new(27,-48.2f),new(20,-48.2f),new(20,-44.6f),new(12.9f,-44.6f)}};
                internal static readonly Vector2 PenGate=new(27,-47),HaskAt=new(28.5f,-48),Trough=new(13.4f,-34.8f),PenLamp=new(18.2f,-33.2f);
                /// <summary>The beasts' walk round the pen, closed: up the west side by the trough, along the north fence, down between the cargo stacks and the gate side, round the notch.</summary>
                internal static readonly Vector2[] HerdLine={new(15.4f,-26),new(16.6f,-24.3f),new(24.4f,-24.3f),new(25.8f,-25.8f),new(25.8f,-45.2f),new(24.6f,-46.4f),new(22.4f,-46.4f),new(21.6f,-45.4f),
                    new(21.2f,-43.9f),new(16.6f,-43.6f),new(15.4f,-42.2f),new(15.4f,-26)};
                internal static readonly Vector2 IruAt=new(15.4f,46.4f),IruLamps=new(17.8f,48.8f);
                internal static readonly Vector2[] CrewAt={new(75.9f,-10.4f),new(75.9f,-7.8f),new(76.1f,-5.4f),new(75.9f,3.2f),new(75.9f,6.6f)};
                internal static readonly Vector2 KottAt=new(64.4f,-7.4f),SibbAt=new(67.4f,-7.4f);
                // Chapter 7: Ishen's archive, the gardener's bed, the release in the galley, Oriel's lantern.
                internal static readonly Vector2 IshenAt=new(-33.2f,42.3f),ArchiveDesk=new(-31.3f,40.6f),Reader=new(-30.3f,41.9f);
                /// <summary>Row C's two east cabins, made the archive's: the middle of each (their fronts at z 43.6).</summary>
                internal static readonly float[] ArchiveCabins={-34.75f,-31.58f};
                internal static readonly Vector2 GardenerPlanter=new(40.7f,51.7f),GardenerAt=new(41.8f,49.7f),GardenerLook=new(42.7f,50.5f);
                internal static readonly Vector2 ReleaseNotice=new(57.2f,16.1f),ReleaseReader=new(57.4f,14.2f),OrielLantern=new(-17.6f,56.3f);
                // Chapter 8: Tovra's forge and its queue, Drask's cell.
                internal static readonly Vector2 Hearth=new(49.6f,-30),Anvil=new(46.2f,-29.6f),Quench=new(46.9f,-34.6f),Mending=new(43,-34.2f),Racks=new(50.2f,-36.6f),Gear=new(43.4f,-23.4f),
                    TovraAt=new(46.2f,-27.9f),BrekkAt=new(48.6f,-26.2f);
                internal static readonly Vector2[] QueueAt={new(40.9f,-34.1f),new(39.1f,-34.5f),new(37.3f,-34.1f),new(35.5f,-34.6f)};
                internal static readonly Vector2 CellMin=new(39.6f,-60.85f),CellMax=new(45,-58.25f),DraskAt=new(42.2f,-58.8f),CellGuard=new(38.2f,-59.3f),CellLamp=new(39.1f,-57.9f);
                // Chapter 9 (as far as its manifest goes): Oda's cabin, Bryn and her navigators, the tug, the wall of names, Nacre's families, Orrin's board, Ket's bench.
                internal static readonly Vector2 OdaAt=new(-41.4f,42.4f),OdaMother=new(-39.7f,43),OdaBundles=new(-42.4f,43.05f);
                internal const float OdaCabinMid=-41.08f;
                internal static readonly Vector2 BrynAt=new(4.4f,47.2f);
                internal static readonly Vector2[] NavigatorsAt={new(-5.6f,46.4f),new(-2.6f,50.6f),new(0,52),new(3,50.3f),new(1.8f,53.6f)};
                internal static readonly Vector2 TugAt=new(-46,-71.6f),Writer=new(43.6f,-55.1f);
                // All three readers on the west stretch of names: east of the hall door the yard is a lane between the wall and the cargo, the only way to Drask's bars,
                // and a reader there (at (38.4, -55.6), then (36.9, -55.4)) shut it from chapter 9.
                internal static readonly Vector2[] NameReaders={new(23.4f,-55.4f),new(26.8f,-55.6f),new(25.1f,-55.2f)},NacreAt={new(16.8f,-62.5f),new(22.4f,-62),new(27.2f,-62.7f)};
                internal static readonly Vector2 BoardAt=new(14.8f,37.62f),OrrinAt=new(14.8f,36.1f),HerbStallMoved=new(21.6f,33.6f),KetBench=new(40,10.2f),KetAt=new(40,11.5f);
                /// <summary>Where the chapters' own stagings stand their people today (HoarfellBuilder/OrielBuilder/HarrowgateBuilder.StageManyHands): nothing here is put on them.</summary>
                static readonly Vector2[] Staged={new(28.5f,-48),new(15.4f,46.4f),new(-8.4f,-50.6f),new(-20.4f,51.6f),new(50.4f,38.6f),new(48.2f,37),new(6.4f,-46.6f),new(28,-37),new(35.5f,-31),new(70,-1.6f)};

                // ---------------------------------------------------------------- the build's own state
                sealed class Place{public string what,from;public GameObject root;public Whereabouts where;}
                static readonly List<Place> places=new();static readonly List<(string what,BoxCollider box)> solids=new();static readonly List<string> faults=new();
                static readonly List<(string body,Vector2 at,string what)> bodies=new();static readonly List<GameObject> hidden=new();static readonly Dictionary<GameObject,string> madeRoom=new();
                static Material Straw,Hide,Bone,Books,Iron,Ember,OrielGlass,Slate,Brick;static AudioMixerGroup ambience;

                /// <summary>One arrival: its root, switched on at load from <paramref name="from"/> (and off at <paramref name="goneAt"/>); in the freight hall, away for the fight's window.</summary>
                static Place Arrive(string what,string from,string goneAt=null,bool hall=false)
                {
                    var root=new GameObject(Prefix+what);
                    var where=new GameObject(Prefix+what+", from "+from,typeof(Whereabouts)).GetComponent<Whereabouts>();
                    where.from=from;where.goneAt=goneAt??"";where.awayFrom=hall?FrameIn:"";where.backAt=hall?FightOver:"";where.live=false;where.targets=new[]{root};
                    var place=new Place{what=what,from=from,root=root,where=where};places.Add(place);return place;
                }
                /// <summary>Something of an arrival's that hangs elsewhere in the tree (under the stars), shown and hidden with it.</summary>
                static void With(Place place,GameObject other){place.where.targets=place.where.targets.Append(other).ToArray();hidden.Add(other);}
                /// <summary>A part of an arrival that has a flag of its own as well (Oriel's lantern needs the far end lit, not only the chapter over).</summary>
                static GameObject Also(Place place,string what,string from)
                {
                    var part=new GameObject(Prefix+what);part.transform.SetParent(place.root.transform,false);
                    var where=new GameObject(Prefix+what+", from "+from,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from;where.goneAt=where.awayFrom=where.backAt="";where.live=false;where.targets=new[]{part};
                    hidden.Add(part);return part;
                }
                static GameObject Piece(Place place,string name,ZoneKit.Shape shape,bool casts,params Material[] materials)
                {var go=Shade(Kit.Built("Arrivals "+name,shape,materials),casts);go.transform.SetParent(place.root.transform,true);return go;}
                static BoxCollider Block(Place place,GameObject view,Vector3 centre,Vector3 size,float yaw=0,string name="Block")
                {var box=ZoneKit.Block(view,centre,size,yaw,name);solids.Add((place.what+": "+name,box));return box;}
                /// <summary>Someone who lives here now, with no name: a townsperson in a static box, as the city's own.</summary>
                static GameObject Folk(Place place,string body,Vector2 at,float yaw,Color tint,string doing,float scale=1,Transform under=null)
                {
                    var one=Kit.Townsperson(body,new Vector3(at.x,0,at.y),yaw,tint,"Neve");one.name="Townsperson, "+doing;one.transform.localScale=Vector3.one*scale;
                    one.transform.SetParent(under??place.root.transform,true);bodies.Add((body,at,place.what+": "+doing));solids.Add((place.what+": "+doing,one.GetComponent<BoxCollider>()));return one;
                }
                /// <summary>A named person whose talk aboard waits on a script that isn't live (or who has nothing to say yet): their body where they will stand, said so in the log.</summary>
                static GameObject StandIn(Place place,string id,string body,Vector2 at,float yaw,string waits)
                {
                    // Themselves, untinted, and not taken for a townsperson by any check: the named stand by the ways' ends, as the city's own do.
                    var one=Kit.Townsperson(body,new Vector3(at.x,0,at.y),yaw,Color.white,"Neve");one.name=DisplayName(id)+", aboard";one.transform.SetParent(place.root.transform,true);UnityEngine.Object.DestroyImmediate(one.GetComponent<PropTint>());
                    solids.Add((place.what+": "+id,one.GetComponent<BoxCollider>()));Debug.Log("MANYHANDS_ARRIVAL_STANDIN "+id+" at "+at+": "+waits);return one;
                }
                static string DisplayName(string id){var def=ZoneKit.Find<CharacterDef>(id);return def!=null&&!string.IsNullOrEmpty(def.displayName)?def.displayName:id;}
                /// <summary>Chalk on a wall, a board or a door: letters standing upright facing the camera's side (not a floor sign: SignStyle lays those flat).</summary>
                static GameObject Words(Place place,string text,Vector3 at,float size,Color colour,float wide,string name,Transform under=null)
                {
                    var label=WorldBuilder.Label(text,at,size);label.name=name;label.transform.rotation=Quaternion.identity;label.transform.position=at;
                    var tmp=label.GetComponent<TMPro.TextMeshPro>();tmp.color=colour;tmp.rectTransform.sizeDelta=new Vector2(wide,size*.16f+.2f);
                    label.transform.SetParent(under??place.root.transform,true);return label;
                }
                static void Sound(Place place,string clip,Vector2 at,float up,float volume,float near,float far)
                {
                    string path="Assets/_Project/Audio/Ambience/"+clip+".ogg";var audio=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if(audio==null){Debug.LogWarning("MANYHANDS_ARRIVAL_SOUND_PENDING "+clip+": run tools/manyhands_arrivals_ambience.py");return;}
                    if(AssetImporter.GetAtPath(path) is AudioImporter importer&&(!importer.forceToMono||importer.loadInBackground)){importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();audio=AssetDatabase.LoadAssetAtPath<AudioClip>(path);}
                    var go=new GameObject(place.what+" heard",typeof(AudioSource));go.transform.SetParent(place.root.transform,false);go.transform.position=At(at,up);
                    var source=go.GetComponent<AudioSource>();source.clip=audio;source.loop=true;source.playOnAwake=true;source.volume=volume;source.spatialBlend=1;source.dopplerLevel=0;
                    source.rolloffMode=AudioRolloffMode.Linear;source.minDistance=near;source.maxDistance=far;source.priority=170;source.outputAudioMixerGroup=ambience;
                }

                // ---------------------------------------------------------------- what the arrivals are made of
                static void Materials()
                {
                    var straw=ZoneKit.Texture("ManyHands/hands-straw",256,(u,v)=>
                    {
                        float stalk=ZoneKit.Tile(u*48+ZoneKit.Tile(u*5,v*5,5,3)*4,v*5,48,5),clump=ZoneKit.Cloud(u,v,8,9),fleck=ZoneKit.Tile(u*96,v*96,96,2);
                        float tone=.6f+.28f*(stalk-.5f)+.14f*(clump-.5f)+(fleck>.92f?.1f:fleck<.06f?-.16f:0);return new Color(tone*1.04f,tone,tone*.76f,1);
                    });
                    var hide=ZoneKit.Texture("ManyHands/hands-hide",256,(u,v)=>
                    {
                        float mottle=ZoneKit.Cloud(u,v,6,31),hair=ZoneKit.Tile(u*12,v*90,12,7);float tone=.6f+.2f*(mottle-.5f)+.07f*(hair-.5f);
                        if(Mathf.Abs(Mathf.Repeat(u*2,1)-.5f)<.008f&&Mathf.Repeat(v*28,1)<.5f)tone-=.2f;
                        return new Color(tone,tone*.94f,tone*.86f,1);
                    });
                    var bone=ZoneKit.Texture("ManyHands/hands-bone",256,(u,v)=>
                    {
                        float ring=Mathf.Abs(Mathf.Sin((v*7+ZoneKit.Cloud(u,v,4,5)*1.6f)*Mathf.PI)),fine=ZoneKit.Cloud(u,v,16,2);float tone=.8f+.07f*(ring-.5f)+.06f*(fine-.5f);
                        // Carved bands every so often: what a herder cuts into a post to know it again.
                        if(Mathf.Abs(Mathf.Repeat(v*3,1)-.5f)<.025f)tone-=.18f;if(ZoneKit.Tile(u*40,v*40,40,9)>.94f)tone-=.1f;
                        return new Color(tone,tone*.97f,tone*.9f,1);
                    });
                    // A shelf's row of spines, eight books to the width and one row high: each its own cloth and height, gilt bands at head and foot, the dark above the shorter ones.
                    Color[] cloths={new(.46f,.18f,.16f),new(.2f,.3f,.42f),new(.24f,.36f,.22f),new(.56f,.44f,.26f),new(.36f,.22f,.36f),new(.62f,.58f,.5f),new(.18f,.2f,.22f),new(.5f,.32f,.18f)};
                    var books=ZoneKit.Texture("ManyHands/hands-books",256,(u,v)=>
                    {
                        float x=u*8;int book=Mathf.FloorToInt(x);float fx=x-book,own=ZoneKit.Tile(book*2.7f,.5f,8,13),tall=.66f+.3f*ZoneKit.Tile(book*1.9f,.3f,8,5);
                        if(v>tall||fx<.04f||fx>.96f)return new Color(.1f,.08f,.07f,1);
                        var cloth=cloths[Mathf.Abs(book*5+(int)(own*7))%cloths.Length];float shade=.85f+.25f*own-.1f*Mathf.Abs(fx-.5f);
                        if(Mathf.Abs(v-tall*.12f)<.025f||Mathf.Abs(v-tall*.86f)<.025f)return new Color(.86f,.7f,.38f,1);
                        if(Mathf.Abs(v-tall*.55f)<.06f&&Mathf.Abs(fx-.5f)<.3f)shade*=1.25f;
                        return new Color(cloth.r*shade,cloth.g*shade,cloth.b*shade,1);
                    });
                    var iron=ZoneKit.Texture("ManyHands/hands-iron",256,(u,v)=>
                    {
                        float m=ZoneKit.Cloud(u,v,8,41),dent=ZoneKit.Tile(u*30,v*30,30,4),scale=ZoneKit.Cloud(u,v,20,17);float tone=.44f+.12f*(m-.5f)+.1f*(dent-.5f)+(scale>.66f?.06f:0);
                        return new Color(tone,tone*.98f,tone*1.02f,1);
                    });
                    var ember=ZoneKit.Texture("ManyHands/hands-ember",256,(u,v)=>
                    {
                        float c=ZoneKit.Cloud(u,v,6,19),crack=ZoneKit.Tile(u*24,v*24,24,8);float tone=.3f+.7f*Mathf.Clamp01((c-.3f)*2)*(crack>.28f?1:.45f);return new Color(tone,tone,tone,1);
                    });
                    // Fire brick, the hearth's: courses a quarter-metre long, each brick its own burn, the mortar dark.
                    var brick=ZoneKit.Texture("ManyHands/hands-brick",256,(u,v)=>
                    {
                        float row=v*8;int r=Mathf.FloorToInt(row);float fy=row-r,x=u*4+(r%2)*.5f;int c=Mathf.FloorToInt(x);float fx=x-c;
                        float own=ZoneKit.Tile(c*3.3f+r*5.9f,.5f,32,21),tone=.6f+.22f*(own-.5f)+.06f*(ZoneKit.Cloud(u,v,8,4)-.5f);
                        if(fy<.08f||fx<.04f)tone=.28f;return new Color(tone,tone*.96f,tone*.92f,1);
                    });
                    Straw=ZoneKit.Toon("hands-straw",straw,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.54f,.52f,.64f));});
                    Hide=ZoneKit.Toon("hands-hide",hide,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.62f));});
                    Bone=ZoneKit.Toon("hands-bone",bone,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.56f,.56f,.68f));});
                    Books=ZoneKit.Toon("hands-books",books,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.62f));});
                    Iron=ZoneKit.Toon("hands-iron",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_RimColor",new Color(.7f,.66f,.6f));m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.44f,.46f,.6f));});
                    Ember=ZoneKit.Toon("hands-ember",ember,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(2.1f,.9f,.3f));});
                    OrielGlass=ZoneKit.Toon("hands-oriel-glass",null,m=>{m.SetColor("_BaseColor",new Color(.8f,.96f,1));m.SetColor("_EmissionColor",new Color(.9f,1.5f,1.7f));});
                    Slate=ZoneKit.Toon("hands-slate",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.24f,.28f,.27f));m.SetFloat("_RampSteps",3);});
                    Brick=ZoneKit.Toon("hands-brick",brick,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.46f,.6f));});
                    var mixer=AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Resources/Audio/Coronach.mixer");
                    ambience=mixer!=null?mixer.FindMatchingGroups("Ambience").FirstOrDefault(g=>g.name=="Ambience"):null;
                    if(ambience==null)Debug.LogWarning("MANYHANDS_ARRIVAL_SOUND the Ambience bus wasn't found: the arrivals' sounds play unrouted");
                }

                // ---------------------------------------------------------------- the build
                internal static void Build(ZoneKit kit)
                {
                    places.Clear();solids.Clear();faults.Clear();bodies.Clear();hidden.Clear();madeRoom.Clear();
                    Materials();
                    Cutters(kit);Herd(kit);IruNook(kit);Crew(kit);
                    Archive(kit);GardenerBed(kit);Release(kit);Lantern(kit);
                    Forge(kit);Cell(kit);
                    OdaCabin(kit);Navigators(kit);Tug(kit);Names(kit);Nacre(kit);Board(kit);KetsBench(kit);
                    Plan();
                    if(faults.Count>0)throw new InvalidOperationException("Many Hands arrivals: the plan puts something where it can't stand:\n"+string.Join("\n",faults.Distinct()));
                    Debug.Log("MANYHANDS_ARRIVALS_BUILT places="+places.Count+" bodies="+bodies.Count+" unset-flags="+string.Join(",",Unset));
                }

                // ---------------------------------------------------------------- chapter 6: Idra's cutters come home
                static void Cutters(ZoneKit kit)
                {
                    // Her own cutter set down on the visitors' pad where Meret's stood, the stripes up: what a still of the dock names at once.
                    var pad=Arrive("Idra's cutter on the visitors' pad",CuttersHome);
                    var hull=ZoneKit.Prefab("Characters","IdraFlight");
                    // Chocks under its skids and a boarding step at its side: what it's tied down with, and what its solid stands under (a box goes under something drawn
                    // that isn't scaled: the hull is fitted to a length, and a box under it would be scaled with it).
                    var chocks=new ZoneKit.Shape();var pc=At(PadCutter,.07f);
                    foreach(var (x,z) in new[]{(-1.3f,-.9f),(1.3f,-.9f),(-1.3f,.9f),(1.3f,.9f)})chocks.Slot(0).Box(pc+Quaternion.Euler(0,112,0)*new Vector3(x,.1f,z),new Vector3(.5f,.2f,.3f),Quaternion.Euler(0,112,0),Hazard,ZoneKit.Shape.Dim(Hazard,1.1f));
                    chocks.Slot(0).Box(pc+Quaternion.Euler(0,112,0)*new Vector3(0,.2f,-1.9f),new Vector3(1,.4f,.6f),Quaternion.Euler(0,112,0),new Color(.5f,.52f,.56f),new Color(.6f,.62f,.66f));
                    var footing=Piece(pad,"cutter chocks",chocks,true,Panel);
                    if(hull!=null)
                    {
                        var ship=Ship(hull,"Idra's cutter",4.8f,At(PadCutter,.12f),112);ship.transform.SetParent(pad.root.transform,true);
                        var b=ZoneKit.Drawn(ship);Block(pad,footing,new Vector3(b.center.x,At(PadCutter).y+.9f,b.center.z),new Vector3(3.2f,1.8f,3.2f),0,"Cutter");
                    }
                    else{Debug.LogWarning("MANYHANDS_ARRIVAL_STANDIN Idra's cutter: IdraFlight isn't taken in, so the pad holds its chocks only");Block(pad,footing,pc+Vector3.up*.2f,new Vector3(3,.4f,2.2f),112,"Chocks");}
                    // Beside it the rescue gear she never lands without: a winch drum on its cradle, a stretcher cage folded flat, coils of line, a crate in hazard stripes.
                    var g=new ZoneKit.Shape();var foot=At(RescueGear);var t=Quaternion.Euler(0,14,0);Color orange=new(.92f,.46f,.16f),steel=new(.5f,.54f,.58f);
                    g.Slot(0).Box(foot+t*new Vector3(0,.42f,0),new Vector3(1.6f,.84f,.9f),t,steel,ZoneKit.Shape.Dim(steel,1.1f));
                    for(int k=0;k<6;k++)g.Slot(0).Box(foot+t*new Vector3(-.65f+k*.26f,.86f,-.46f),new Vector3(.13f,.06f,.02f),t,k%2==0?Hazard:Soot);
                    g.Slot(1).Tube(new[]{foot+t*new Vector3(-.5f,1.05f,0),foot+t*new Vector3(.5f,1.05f,0)},new[]{.24f,.24f},14,orange,ZoneKit.Shape.Dim(orange,.8f),1,1);
                    foreach(int side in new[]{-1,1})g.Slot(0).Box(foot+t*new Vector3(side*.62f,1.02f,0),new Vector3(.08f,.36f,.5f),t,steel);
                    var cage=foot+t*new Vector3(0,0,1.2f);
                    for(int k=0;k<3;k++)g.Slot(1).Box(cage+t*new Vector3(0,.06f+k*.09f,0),new Vector3(1.9f,.06f,.62f),t*Quaternion.Euler(0,k*4-4,0),orange,ZoneKit.Shape.Dim(orange,1.15f));
                    for(int k=0;k<2;k++){var coil=foot+t*new Vector3(1.1f+k*.7f,0,.9f-k*.3f);for(int turn=0;turn<3;turn++){var ring=new Vector3[13];var r=new float[13];for(int i=0;i<13;i++){float a=i*Mathf.PI*2/12;ring[i]=coil+new Vector3(Mathf.Cos(a)*(.36f-turn*.07f),.05f+turn*.07f,Mathf.Sin(a)*(.36f-turn*.07f));r[i]=.045f;}g.Slot(2).Tube(ring,r,5,orange,orange,1,1,false);}}
                    var gear=Piece(pad,"rescue gear",g,true,Panel,Cloth,Weave);
                    Block(pad,gear,foot+t*new Vector3(0,.6f,.35f),new Vector3(2.1f,1.2f,2),14,"Rescue gear");
                    Folk(pad,"IdraNatural",CrewAtPad,40,new Color(.84f,.86f,.94f),"a rescue hand coiling line");
                    // The other two lashed off the stern beyond the south rail, drawn only and hung under the stars as the patrol's cutters are, their lines over the rail to bollards.
                    var lashed=Arrive("Idra's cutters lashed off the stern",CuttersHome,TugFound);
                    var stars=GameObject.Find("Stars below");var lines=new ZoneKit.Shape();
                    for(int i=0;i<Lashed.Length;i++)
                    {
                        var at=Lashed[i];
                        if(hull!=null){var ship=Ship(hull,"Idra's lashed cutter "+(i+1),5.4f,new Vector3(at.x,-2.2f,at.y),170+i*16);if(stars!=null){ship.transform.SetParent(stars.transform,true);With(lashed,ship);}else ship.transform.SetParent(lashed.root.transform,true);}
                        var post=new Vector3(at.x,0,-64.9f);
                        lines.Slot(0).Post(post,.46f,.18f,.15f,8,Soot,new Color(.5f,.52f,.56f)).Slot(1).Rope(post+Vector3.up*.4f,new Vector3(at.x+.2f,1.2f,ZMin+.25f),.05f,.04f,new Color(.92f,.5f,.2f))
                            .Rope(new Vector3(at.x+.2f,1.2f,ZMin+.25f),new Vector3(at.x+.6f,-1.1f,at.y+2.2f),.5f,.04f,new Color(.92f,.5f,.2f));
                    }
                    Piece(lashed,"mooring lines",lines,false,Panel,Rope);
                }
                /// <summary>A ship's body (a hero's flight hull) as scenery: its colliders and behaviours taken off, fitted to a length, set down with its keel at a point.</summary>
                static GameObject Ship(GameObject prefab,string name,float length,Vector3 keel,float yaw)
                {
                    var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);body.name=name;
                    foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                    foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                    foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                    body.transform.rotation=Quaternion.Euler(0,yaw,0);var bounds=ModelGeometry.BoundsOf(body);float longest=Mathf.Max(bounds.size.x,bounds.size.z);
                    if(longest>.01f)body.transform.localScale*=length/longest;bounds=ModelGeometry.BoundsOf(body);
                    body.transform.position+=keel-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    foreach(var r in body.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.On;
                    return body;
                }

                // ---------------------------------------------------------------- chapter 6: the herd, in the freight hall's west half
                static void Herd(ZoneKit kit)
                {
                    var pen=Arrive("the herd in the freight hall",SixDone,null,true);
                    Color bone=new(.92f,.88f,.78f),boneDark=new(.62f,.58f,.52f),hide=new(.62f,.46f,.32f),hideDark=new(.42f,.32f,.24f);var rng=new System.Random(601);
                    // The fence the herders brought: carved bone posts, hide ropes, a hide hung over every other span against the draught, a block rail no beast steps over.
                    var fence=new ZoneKit.Shape();var rails=new List<(Vector3 a,Vector3 b)>();int span=0;
                    foreach(var side in PenSides)for(int s=1;s<side.Length;s++)
                    {
                        var a=side[s-1];var b=side[s];int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/2.3f));Vector3? last=null;
                        for(int k=0;k<=n;k++)
                        {
                            var p=Vector2.Lerp(a,b,k/(float)n);var post=At(p);
                            fence.Slot(0).Post(post,1.45f,.11f,.08f,6,boneDark,bone).Lump(post+Vector3.up*1.5f,new Vector3(.12f,.1f,.12f),k+s,.15f,bone,boneDark,6,4);
                            if(last is Vector3 l)
                            {
                                fence.Slot(1).Rope(l+Vector3.up*1.18f,post+Vector3.up*1.18f,.1f,.035f,hideDark).Rope(l+Vector3.up*.62f,post+Vector3.up*.62f,.08f,.035f,hideDark);
                                // A hide over the rope on every other span, hanging on the camera's side so the pen reads as the herders' own and not the hall's.
                                if(span++%2==0)
                                {
                                    var face=Vector3.Cross(Vector3.up,(post-l).normalized);if(Vector3.Dot(face,new Vector3(-.34f,0,-.94f))<0)face=-face;var o=face*.07f;
                                    fence.Slot(2).Quad(l+Vector3.up*1.16f+o,post+Vector3.up*1.16f+o,post+Vector3.up*.36f+o,l+Vector3.up*.42f+o,face,(span/2)%2==0?hide:new Color(.78f,.66f,.5f),.6f);
                                }
                            }
                            last=post;
                        }
                        rails.Add((At(a,.6f),At(b,.6f)));
                    }
                    // The gate: two leaves of lashed bone poles, shut, under a crossbar hung with charms; horn lamps on its posts.
                    var g0=At(new Vector2(27,-45.8f));var g1=At(new Vector2(27,-48.2f));
                    foreach(var gp in new[]{g0,g1})fence.Slot(0).Post(gp,2.3f,.14f,.11f,7,boneDark,bone).Slot(3).Lump(gp+new Vector3(.25f,2.05f,0),new Vector3(.12f,.17f,.12f),(int)gp.z,.04f,Color.white,Color.white,7,4);
                    fence.Slot(0).Beam(g0+Vector3.up*2.25f,g1+Vector3.up*2.25f,.14f,.12f,bone);
                    for(int k=0;k<7;k++){var y=.25f+k*.16f;fence.Slot(0).Beam(Vector3.Lerp(g0,g1,.04f)+Vector3.up*y,Vector3.Lerp(g0,g1,.96f)+Vector3.up*y,.06f,.05f,k%3==0?boneDark:bone);}
                    for(int k=0;k<4;k++){var hang=Vector3.Lerp(g0,g1,.2f+k*.2f)+Vector3.up*2.18f;fence.Slot(1).Rope(hang,hang+Vector3.down*.5f,.01f,.015f,hideDark).Slot(0).Lump(hang+Vector3.down*.56f,new Vector3(.06f,.1f,.06f),k,.1f,bone,boneDark,6,4);}
                    var view=Piece(pen,"pen fence",fence,true,Bone,Hide,Hide,Lamp);
                    foreach(var (a,b) in rails)solids.Add((pen.what+": rail",ZoneKit.BlockAlong(view,a,b,.22f,1.2f,"Pen rail")));
                    solids.Add((pen.what+": gate",ZoneKit.BlockAlong(view,g0+Vector3.up*.65f,g1+Vector3.up*.65f,.24f,1.3f,"Pen gate")));
                    // Bedding: straw spread in drifts over the pen's floor, darker where it's trodden down along the beasts' walk.
                    // Thin slabs of straw strewn at every angle and overlapping, so the floor reads as forked-out bedding and never as discs (a lump's texture runs round its pole).
                    var bed=new ZoneKit.Shape();
                    for(int k=0;k<420;k++)
                    {
                        var p=new Vector2(13.4f+(float)rng.NextDouble()*13.4f,-47.8f+(float)rng.NextDouble()*25.4f);float wide=.35f+(float)rng.NextDouble()*.6f,deep=.2f+(float)rng.NextDouble()*.35f,yaw=(float)rng.NextDouble()*180,tilt=((float)rng.NextDouble()-.5f)*8;
                        if(!InPen(p,.35f)||NearStack(p,.15f)||Mathf.Abs(p.x-Trough.x)<.7f&&Mathf.Abs(p.y-Trough.y)<3.7f)continue;
                        float trod=ZoneKit.FromPath(p,HerdLine)<1.1f?.66f:.92f+.16f*(float)rng.NextDouble();var tone=new Color(.88f*trod,.76f*trod,.5f*trod);
                        bed.Box(At(p,.02f+(k%4)*.012f),new Vector3(wide,.025f,deep),Quaternion.Euler(tilt,yaw,0),ZoneKit.Shape.Dim(tone,.8f),tone,1.6f);
                    }
                    Piece(pen,"bedding",bed,false,Straw);
                    // The trough along the west wall, carved from a rib, water in it; a hay rack of dried moss beside it; bales of fodder lashed on the two cargo stacks under hides.
                    var tr=new ZoneKit.Shape();var tf=At(Trough);
                    tr.Slot(0).Box(tf+Vector3.up*.3f,new Vector3(.8f,.6f,7),Quaternion.identity,boneDark,bone,.6f).Slot(1).Box(tf+Vector3.up*.58f,new Vector3(.56f,.04f,6.7f),Quaternion.identity,new Color(.5f,.72f,.76f));
                    foreach(float z in new[]{-3.2f,0,3.2f})tr.Slot(0).Box(tf+new Vector3(0,.3f,z),new Vector3(.86f,.66f,.12f),Quaternion.identity,boneDark);
                    var rack=At(new Vector2(13.5f,-27.6f));
                    foreach(float z in new[]{-1.2f,1.2f})tr.Slot(0).Post(rack+new Vector3(0,0,z),1.7f,.07f,.06f,6,boneDark,bone);
                    tr.Slot(0).Beam(rack+new Vector3(0,1.65f,-1.25f),rack+new Vector3(0,1.65f,1.25f),.08f,.08f,bone);
                    for(int k=0;k<5;k++)tr.Slot(2).Lump(rack+new Vector3(.05f,1.15f,-1+k*.5f),new Vector3(.3f,.45f,.28f),k,.3f,new Color(.58f,.62f,.36f),new Color(.38f,.4f,.24f),9,5);
                    foreach(var stack in new[]{HallStacks[1],HallStacks[2]})
                    {
                        // Bales of dried moss and straw, tied with hide cord, two layers on the stack's top; a hide thrown over the back of them against the hall's draught.
                        var top=At(stack,2.82f);
                        for(int k=0;k<5;k++)
                        {
                            var o=new Vector3((k%3-1)*1.02f,(k/3)*.46f+.23f,k/3==0?-.3f:.25f);var turn=Quaternion.Euler(0,(k*37)%11-5,0);var tone=k%2==0?new Color(.82f,.74f,.48f):new Color(.66f,.68f,.42f);
                            tr.Slot(2).Box(top+o,new Vector3(.94f,.44f,.62f),turn,ZoneKit.Shape.Dim(tone,.82f),tone,1.4f);
                            foreach(float dx in new[]{-.24f,.24f})tr.Slot(3).Box(top+o+turn*new Vector3(dx,0,0),new Vector3(.05f,.46f,.64f),turn,new Color(.42f,.32f,.24f));
                        }
                        tr.Slot(3).Quad(top+new Vector3(-1.5f,.5f,.62f),top+new Vector3(1.5f,.5f,.62f),top+new Vector3(1.5f,.94f,-.1f),top+new Vector3(-1.5f,.94f,-.1f),new Vector3(0,1,-.4f),hide,.6f)
                            .Quad(top+new Vector3(-1.5f,.5f,.62f),top+new Vector3(1.5f,.5f,.62f),top+new Vector3(1.55f,-.1f,1.62f),top+new Vector3(-1.55f,-.1f,1.62f),new Vector3(0,.6f,1),hide,.6f);
                    }
                    var trough=Piece(pen,"trough and fodder",tr,true,Bone,Slate,Straw,Hide);
                    Block(pen,trough,tf+Vector3.up*.35f,new Vector3(.86f,.7f,7),0,"Trough");Block(pen,trough,rack+Vector3.up*.85f,new Vector3(.5f,1.7f,2.6f),0,"Hay rack");
                    // The herders' horn lamp on its crook in the pen's middle: warm light on the straw, and the hall's west half lit as a place people keep.
                    var lp=new ZoneKit.Shape();var lf=At(PenLamp);
                    lp.Slot(0).Post(lf,2.7f,.1f,.08f,6,boneDark,bone).Tube(new[]{lf+Vector3.up*2.6f,lf+new Vector3(.25f,2.9f,0),lf+new Vector3(.6f,2.85f,0)},new[]{.06f,.05f,.045f},5,bone,bone,1,.5f).Slot(1).Lump(lf+new Vector3(.62f,2.5f,0),new Vector3(.16f,.22f,.16f),3,.05f,Color.white,Color.white,8,5);
                    var lamp=Piece(pen,"pen lamp",lp,true,Bone,Lamp);Block(pen,lamp,lf+Vector3.up*1.35f,new Vector3(.3f,2.7f,.3f),0,"Lamp post");
                    Kit.Lamp(new Vector3(PenLamp.x+.6f,2.3f,PenLamp.y),10,1.2f,new Color(1,.8f,.54f)).transform.SetParent(pen.root.transform,true);
                    // The beasts: rime-beasts walking their pen, each a kit beast on a kinematic body, the walk's nod a child. Nothing startles them here: the pen is their own.
                    var line=new List<Vector3>();
                    for(int i=1;i<HerdLine.Length;i++){var a=HerdLine[i-1];var b=HerdLine[i];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));for(int k=0;k<steps;k++){var p=Vector2.Lerp(a,b,k/(float)steps);line.Add(At(p));}}
                    line.Add(At(HerdLine[^1]));
                    var herd=new GameObject("The herd",typeof(RimeHerd)).GetComponent<RimeHerd>();herd.transform.SetParent(pen.root.transform,false);herd.line=line.ToArray();
                    herd.speed=.42f;herd.startle=0;herd.trample=0;herd.knock=0;
                    float length=0;for(int i=1;i<line.Count;i++)length+=Vector3.Distance(line[i-1],line[i]);
                    var beasts=new List<Transform>();var across=new List<float>();var along=new List<float>();int count=6;
                    for(int i=0;i<count;i++)
                    {
                        var root=new GameObject("Rime-beast "+i);root.transform.SetParent(herd.transform,true);
                        var body=Kit.Prop(ZoneKit.Kit("RimeBeast","DressBoulders"),Vector3.zero,1.5f+(float)rng.NextDouble()*.3f,0,false,false);body.transform.SetParent(root.transform,true);
                        if(i%3==1)body.AddComponent<PropTint>().tint=new Color(.9f,.92f,1);else if(i%3==2)body.AddComponent<PropTint>().tint=new Color(1.04f,1,.94f);
                        var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.8f,0);box.size=new Vector3(1.2f,1.5f,2.4f);var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                        float side=((float)rng.NextDouble()-.5f)*.5f,at=length*i/count+(float)rng.NextDouble()*1.5f;beasts.Add(root.transform);across.Add(side);along.Add(at);
                        float walked=0;for(int k=1;k<line.Count;k++){float leg=Vector3.Distance(line[k-1],line[k]);if(walked+leg>=at){var p=Vector3.Lerp(line[k-1],line[k],(at-walked)/leg);var ahead=line[k]-line[k-1];ahead.y=0;ahead.Normalize();
                            root.transform.SetPositionAndRotation(p+new Vector3(ahead.z,0,-ahead.x)*side,Quaternion.LookRotation(ahead));break;}walked+=leg;}
                    }
                    herd.beasts=beasts.ToArray();herd.across=across.ToArray();herd.along=along.ToArray();
                    // The herders: one forking bedding between the stacks, one leaning on the fence watching the walk, the third at the hay rack. Two coats of the Kept's.
                    Hold(Folk(pen,"KeptHerder",new Vector2(17.6f,-35.6f),250,new Color(.96f,.94f,1.04f),"a herder forking bedding"),"fork");
                    Folk(pen,"TernBody",new Vector2(29.2f,-24.8f),250,new Color(.9f,.96f,1.06f),"a herder watching the herd");
                    Hold(Folk(pen,"KeptHerder",new Vector2(13.7f,-30.1f),80,new Color(1.06f,.98f,.9f),"a herder at the hay rack"),"moss");
                    Sound(pen,"AMB_HandsHerd",new Vector2(19.5f,-34),1.4f,.55f,5,26);
                }
                static bool InPen(Vector2 p,float margin)=>p.x>12.9f+margin&&p.x<27-margin&&p.y<-22.2f-margin&&(p.y>-44.6f+margin||p.x>20+margin&&p.y>-48.2f+margin);
                static bool NearStack(Vector2 p,float margin){foreach(var s in HallStacks)if(Vector2.Distance(p,s)<2.3f+margin)return true;return false;}

                // ---------------------------------------------------------------- chapter 6: Iru on the lamp gallery, with Pell
                static void IruNook(ZoneKit kit)
                {
                    var nook=Arrive("Iru's lamps on the gallery",SixDone);var s=new ZoneKit.Shape();var c=At(IruLamps);Color bone=new(.92f,.88f,.8f),dark=new(.66f,.62f,.56f);
                    // Lastlight's shrine lamps, the ones she carried: small horn lamps on bone stands in an arc, lit low, a low bowl in the middle; two hide mats to kneel on.
                    for(int k=0;k<5;k++)
                    {
                        float a=(25+k*32.5f)*Mathf.Deg2Rad;var stand=c+new Vector3(Mathf.Cos(a)*1.5f,0,Mathf.Sin(a)*1.4f);float tall=.75f+.18f*(k%2);
                        s.Slot(0).Post(stand,tall,.06f,.05f,6,dark,bone).Tube(new[]{stand+Vector3.up*tall,stand+Vector3.up*(tall+.12f)},new[]{.1f,.15f},9,bone,bone,1,1,false).Slot(1).Lump(stand+Vector3.up*(tall+.14f),new Vector3(.07f,.09f,.07f),k,.05f,Color.white,Color.white,6,4);
                    }
                    s.Slot(0).Post(c,.7f,.16f,.1f,8,dark,bone).Tube(new[]{c+Vector3.up*.7f,c+Vector3.up*.9f},new[]{.26f,.42f},12,bone,bone,1,1,false).Slot(1).Lump(c+Vector3.up*.93f,new Vector3(.26f,.12f,.26f),3,.3f,Color.white,Color.white,9,4);
                    foreach(var (x,z,tone) in new[]{(-.7f,-1.1f,new Color(.84f,.8f,.72f)),(.8f,-1f,new Color(.62f,.48f,.36f))})s.Slot(2).Disc(c+new Vector3(x,.03f,z),Vector3.up,.7f,9,tone,0,ZoneKit.Shape.Dim(tone,.8f),.6f);
                    // A string of bone lamp-hooks on a carved post at the nook's back, the way the shrine kept them.
                    var post=c+new Vector3(1.9f,0,.9f);s.Slot(0).Post(post,2.3f,.12f,.08f,7,dark,bone);
                    for(int k=0;k<5;k++){var hang=post+new Vector3(.12f*Mathf.Cos(k*1.3f),2.15f,.12f*Mathf.Sin(k*1.3f));s.Slot(3).Rope(hang,hang+Vector3.down*(.5f+.12f*(k%3)),.01f,.012f,new Color(.5f,.4f,.3f)).Slot(0).Lump(hang+Vector3.down*(.55f+.12f*(k%3)),new Vector3(.04f,.08f,.04f),k,.1f,bone,dark,6,4);}
                    var view=Piece(nook,"Iru's lamps",s,true,Bone,Lamp,Hide,Rope);
                    Block(nook,view,c+new Vector3(0,.5f,.65f),new Vector3(3.2f,1,1.6f),0,"Lamp stands");Block(nook,view,post+Vector3.up*1.15f,new Vector3(.3f,2.3f,.3f),0,"Hook post");
                    Kit.Lamp(new Vector3(IruLamps.x,1.5f,IruLamps.y+.4f),6,.9f,new Color(1,.76f,.46f)).transform.SetParent(nook.root.transform,true);
                }

                // ---------------------------------------------------------------- chapter 6: Idra's crew in the galley, eating standing with their backs to the window wall
                static void Crew(ZoneKit kit)
                {
                    var crew=Arrive("Idra's crew in the galley",SixDone);
                    // The Rauk have one body in the kit, Idra's (a crew body is owed: docs/maps/ManyHands.md): her off-duty coat in the crew's own greys and her frame for the two still on watch.
                    Color[] greys={new(.82f,.84f,.9f),new(.96f,.92f,.88f),new(.74f,.76f,.8f),new(.9f,.86f,.84f),new(.86f,.92f,.94f)};
                    for(int i=0;i<CrewAt.Length;i++){var one=Folk(crew,i==1||i==3?"IdraBraced":"IdraNatural",CrewAt[i],268+i*3,greys[i],i==1||i==3?"a rescue hand still in her frame":"a rescue hand eating at the wall",i%2==0?1.03f:.98f);Hold(one,"bowl");}
                    // Kott and Sibb at the south table: the winch hand big, with the four bowls of eel he complained about; the pilot smaller, knocking three and two on the table.
                    var kott=Folk(crew,"IdraNatural",KottAt,182,new Color(.7f,.72f,.76f),"Kott, Idra's winch hand",1.1f);kott.name="Kott, at the south table";Hold(kott,"bowl");
                    var sibb=Folk(crew,"IdraNatural",SibbAt,176,new Color(1.02f,.96f,.92f),"Sibb, Idra's pilot",.94f);sibb.name="Sibb, at the south table";
                    var bowls=new ZoneKit.Shape();var table=At(KottAt.x,-9.05f,.84f);for(int k=0;k<4;k++)bowls.Lump(table+Vector3.up*k*.07f,new Vector3(.17f,.06f,.17f),k,.02f,Cream,new Color(.6f,.56f,.5f),10,3);
                    // Their gear stacked at the wall's foot: helmets, line, a lamp cell.
                    var g=At(76.6f,-1.2f);for(int k=0;k<3;k++)bowls.Slot(1).Lump(g+new Vector3((k-1)*.5f,.18f,0),new Vector3(.2f,.18f,.22f),k+9,.04f,new Color(.92f,.46f,.16f),new Color(.5f,.3f,.16f),9,5);
                    bowls.Slot(1).Box(g+new Vector3(0,.25f,.6f),new Vector3(.5f,.5f,.4f),Quaternion.Euler(0,8,0),new Color(.5f,.54f,.58f));
                    var gear=Piece(crew,"crew gear",bowls,true,Panel,Panel);Block(crew,gear,g+new Vector3(0,.3f,.25f),new Vector3(1.6f,.6f,1.2f),0,"Crew gear");
                }

                // ---------------------------------------------------------------- chapter 7: Ishen's archive in the cabins' lane
                static void Archive(ZoneKit kit)
                {
                    var archive=Arrive("Ishen's archive in the cabins",SevenDone);var s=new ZoneKit.Shape();Color wood=new(.56f,.42f,.3f),wood2=new(.7f,.56f,.4f);
                    // Bookcases against the fronts of row C's two east cabins, over their windows: fifty-two books and the rest of what Lanternwick sent after them.
                    var boxes=new List<(Vector3 at,Vector3 size,string name)>();
                    foreach(float mid in ArchiveCabins)
                    {
                        float x0=mid-.02f,x1=mid+1.5f,face=43.58f;var c=At((x0+x1)*.5f,face-.24f);float w=x1-x0;
                        s.Slot(0).Box(c+Vector3.up*1.05f,new Vector3(w,2.1f,.46f),Quaternion.identity,wood,wood2,.6f);
                        for(int r=0;r<4;r++){float y=.22f+r*.5f;s.Slot(1).Box(c+new Vector3(0,y+.18f,-.12f),new Vector3(w-.12f,.36f,.3f),Quaternion.identity,Color.white,Color.white,2.78f);
                            s.Slot(0).Box(c+new Vector3(0,y-.03f,-.12f),new Vector3(w-.06f,.05f,.36f),Quaternion.identity,wood2);}
                        // Books lying across the tops, the overflow.
                        for(int k=0;k<3;k++)s.Slot(1).Box(c+new Vector3(-.4f+k*.36f,2.16f+k%2*.05f,-.05f),new Vector3(.3f,.08f,.22f),Quaternion.Euler(0,k*17,0),Color.white,Color.white,2.78f);
                        boxes.Add((c+Vector3.up*1.05f,new Vector3(w,2.1f,.46f),"Bookcase"));
                    }
                    // Crates of them still to be shelved at the lane's end, books stood on top.
                    var crates=At(-29.4f,38.6f);for(int k=0;k<4;k++)s.Slot(2).Box(crates+new Vector3((k%2)*.62f-.31f,.26f+(k/2)*.5f,(k%2)*.1f),new Vector3(.58f,.5f,.5f),Quaternion.Euler(0,k*9,0),new Color(.76f,.6f,.42f),new Color(.84f,.68f,.48f),.6f);
                    for(int k=0;k<5;k++)s.Slot(1).Box(crates+new Vector3(-.2f+k*.1f,1.06f,0),new Vector3(.08f,.3f,.24f),Quaternion.Euler(0,0,(k-2)*6),Color.white,Color.white,2.78f);
                    boxes.Add((crates+Vector3.up*.5f,new Vector3(1.3f,1,.7f),"Book crates"));
                    // The reading desk: a slanted top, an open book, a lamp; a stool; someone reading.
                    var d=At(ArchiveDesk);s.Slot(0).Box(d+Vector3.up*.38f,new Vector3(1.6f,.76f,.7f),Quaternion.identity,wood,wood2,.6f).Box(d+new Vector3(0,.82f,0),new Vector3(1.7f,.06f,.8f),Quaternion.Euler(-10,0,0),wood2);
                    s.Slot(3).Quad(d+new Vector3(-.4f,.88f,-.2f),d+new Vector3(.36f,.88f,-.2f),d+new Vector3(.36f,.94f,.16f),d+new Vector3(-.4f,.94f,.16f),Vector3.up,new Color(.96f,.94f,.86f),.5f);
                    s.Slot(4).Lump(d+new Vector3(.62f,1.08f,.2f),new Vector3(.1f,.13f,.1f),2,.04f,Color.white,Color.white,7,4).Slot(0).Post(d+new Vector3(.62f,.85f,.2f),.18f,.03f,.03f,5,Bronze,Bronze);
                    s.Slot(0).Post(At(ArchiveDesk+new Vector2(-.5f,.9f)),.46f,.18f,.16f,8,wood,wood2);
                    boxes.Add((d+Vector3.up*.45f,new Vector3(1.7f,.9f,.8f),"Reading desk"));
                    // Boxes under what is drawn, made after it (Lastlight's carving yard found the other order hangs them under an empty holder, which reads as an invisible wall).
                    var view=Piece(archive,"archive",s,true,Wood,Books,Wood,Cloth,Lamp);
                    foreach(var (at,size,name) in boxes)Block(archive,view,at,size,0,name);
                    Kit.Lamp(new Vector3(ArchiveDesk.x+.6f,1.5f,ArchiveDesk.y+.2f),6,1,new Color(1,.86f,.62f)).transform.SetParent(archive.root.transform,true);
                    // Its name on a board along the roof's edge over the two doors, as the cabins' households mark theirs.
                    var board=new ZoneKit.Shape().Box(At(-33.2f,43.66f,3.04f),new Vector3(3.4f,.58f,.08f),Quaternion.identity,new Color(.3f,.24f,.2f));
                    Piece(archive,"archive board",board,false,Wood);
                    Words(archive,"ARCHIVE",At(-33.2f,43.58f,3.04f),3.2f,new Color(.96f,.92f,.8f),3.4f,"Painted on the archive's board");
                    Hold(Folk(archive,"AumiBody",Reader,205,new Color(.86f,.92f,1),"reading in the archive"),"book");
                }

                // ---------------------------------------------------------------- chapter 7: the gardener's bed in the seed garden
                static void GardenerBed(ZoneKit kit)
                {
                    var bed=Arrive("the gardener's bed by Marrow-1",SevenDone);var planted=Also(bed,"the gardener's bed, planted",GardenerHome);
                    // A planter of the Ancelane's pale stone at the aisle's north end, the jar of thousand-year-old seed set in it, its soil raked in rings as the gardens on Oriel are,
                    // and the gardener's own low light on it all night. Whether anything comes up is chapter 10's.
                    var s=new ZoneKit.Shape();var c=At(GardenerPlanter);Color stone=new(.84f,.82f,.76f),stoneDark=new(.6f,.6f,.56f);
                    s.Slot(0).Box(c+Vector3.up*.22f,new Vector3(1.5f,.44f,1.5f),Quaternion.identity,stoneDark,stone,.6f).Slot(1).Box(c+Vector3.up*.45f,new Vector3(1.26f,.03f,1.26f),Quaternion.identity,new Color(.5f,.4f,.3f));
                    for(int r=0;r<4;r++){var ring=new Vector3[25];var rad=new float[25];for(int k=0;k<25;k++){float a=k*Mathf.PI*2/24;ring[k]=c+new Vector3(Mathf.Cos(a)*(.16f+r*.13f),.47f,Mathf.Sin(a)*(.16f+r*.13f));rad[k]=.012f;}s.Slot(1).Tube(ring,rad,4,new Color(.36f,.28f,.2f),new Color(.36f,.28f,.2f),1,1,false);}
                    s.Slot(2).Lump(c+Vector3.up*.52f,new Vector3(.11f,.14f,.11f),3,.02f,new Color(.56f,.7f,.72f),new Color(.32f,.42f,.44f),10,5);
                    s.Slot(0).Post(c+new Vector3(.62f,.44f,.62f),.5f,.03f,.025f,5,Bronze,Bronze).Slot(3).Lump(c+new Vector3(.62f,.98f,.62f),new Vector3(.07f,.07f,.07f),1,.02f,Color.white,Color.white,6,4);
                    var view=Piece(bed,"gardener's planter",s,true,Panel,Soil,Glass,OrielGlass);view.transform.SetParent(planted.transform,true);
                    Block(bed,view,c+Vector3.up*.25f,new Vector3(1.5f,.5f,1.5f),0,"Planter");
                    var glow=Kit.Lamp(new Vector3(GardenerPlanter.x+.6f,.95f,GardenerPlanter.y+.6f),3.4f,.5f,new Color(.82f,.94f,1));glow.transform.SetParent(planted.transform,true);
                }

                // ---------------------------------------------------------------- chapter 7: the release, posted in the galley
                static void Release(ZoneKit kit)
                {
                    var notice=Arrive("the release posted in the galley",SevenDone);var s=new ZoneKit.Shape();var c=At(ReleaseNotice,1.7f)+Vector3.back*.12f;
                    s.Slot(0).Box(c,new Vector3(3.5f,1.5f,.06f),Quaternion.identity,new Color(.5f,.38f,.26f)).Slot(1).Box(c+Vector3.back*.04f,new Vector3(3.3f,1.3f,.02f),Quaternion.identity,new Color(.24f,.28f,.27f));
                    foreach(var dx in new[]{-1.55f,1.55f})s.Slot(0).Lump(c+new Vector3(dx,.62f,-.06f),new Vector3(.04f,.04f,.03f),(int)(dx*10),.02f,Bronze,Bronze,6,4);
                    Piece(notice,"release board",s,false,Wood,Slate);
                    // Ishen's words, in the order the maker cut them, and the line under them that isn't a step. One of his four copies.
                    var face=c+Vector3.back*.07f;var chalk=new Color(.95f,.95f,.9f);
                    Words(notice,"TAKE ITS WEIGHT.",face+Vector3.up*.42f,2.6f,chalk,3.3f,"The release, posted");Words(notice,"WAIT FOR THE SLACK.",face+Vector3.up*.08f,2.6f,chalk,3.3f,"The release, posted");
                    Words(notice,"LET GO TOGETHER.",face+Vector3.down*.26f,2.6f,chalk,3.3f,"The release, posted");Words(notice,"Then give it room.",face+Vector3.down*.56f,2.4f,new Color(.86f,.86f,.8f),3.3f,"The release, posted");
                    Folk(notice,"SennBody",ReleaseReader,0,new Color(.92f,.96f,1.02f),"reading the release");
                }

                // ---------------------------------------------------------------- chapter 7: a lantern on the gallery lit from Oriel
                static void Lantern(ZoneKit kit)
                {
                    var place=Arrive("Oriel's lantern on the gallery",SevenDone);var lit=Also(place,"Oriel's lantern, lit",LanternLit);
                    // The fourth in the row of Pell's gallery lamps, at the west end: a Lanternwick post with the far end's glass in it, its light colder than the convoy's.
                    var s=new ZoneKit.Shape();var foot=At(OrielLantern);
                    s.Slot(0).Post(foot,3.3f,.1f,.07f,8,ZoneKit.Shape.Dim(Bronze,.7f),Bronze).Beam(foot+new Vector3(0,3.2f,0),foot+new Vector3(.62f,3.2f,0),.06f,.06f,Bronze);
                    s.Slot(0).Tube(new[]{foot+new Vector3(.62f,2.48f,0),foot+new Vector3(.62f,2.52f,0)},new[]{.24f,.24f},10,Bronze,Bronze,1,1).Tube(new[]{foot+new Vector3(.62f,3.08f,0),foot+new Vector3(.62f,3.16f,0)},new[]{.18f,.08f},10,Bronze,Bronze,1,1);
                    s.Slot(1).Tube(new[]{foot+new Vector3(.62f,2.52f,0),foot+new Vector3(.62f,2.8f,0),foot+new Vector3(.62f,3.08f,0)},new[]{.2f,.22f,.18f},10,Color.white,Color.white,1,1,false);
                    for(int k=0;k<6;k++){float a=k*Mathf.PI/3;s.Slot(0).Beam(foot+new Vector3(.62f+Mathf.Cos(a)*.21f,2.5f,Mathf.Sin(a)*.21f),foot+new Vector3(.62f+Mathf.Cos(a)*.19f,3.1f,Mathf.Sin(a)*.19f),.025f,.025f,Bronze);}
                    var view=Piece(place,"Oriel's lantern",s,true,Brass,OrielGlass);view.transform.SetParent(lit.transform,true);
                    Block(place,view,foot+Vector3.up*1.65f,new Vector3(.3f,3.3f,.3f),0,"Lantern post");
                    Kit.Lamp(new Vector3(OrielLantern.x+.62f,2.5f,OrielLantern.y),8,1.2f,new Color(.78f,.94f,1)).transform.SetParent(lit.transform,true);
                }

                // ---------------------------------------------------------------- chapter 8: Tovra's forge on the freight hall's east wall
                static void Forge(ZoneKit kit)
                {
                    var forge=Arrive("Tovra's forge in the freight hall",EightDone,null,true);var s=new ZoneKit.Shape();Color brick=new(.5f,.3f,.24f),brickDark=new(.32f,.2f,.18f),steel=new(.42f,.44f,.48f);
                    // The hearth against the wall: a brick bed with its coals, a hood over it and a flue up the hall's wall, the smoke going out where the roof was torn.
                    var h=At(Hearth);s.Slot(0).Box(h+Vector3.up*.5f,new Vector3(1.6f,1,3.4f),Quaternion.identity,brickDark,brick,.8f);
                    s.Slot(0).Box(h+new Vector3(.62f,1.2f,0),new Vector3(.36f,.4f,3.4f),Quaternion.identity,brickDark,brick,.8f);
                    var coals=new ZoneKit.Shape();var rng=new System.Random(801);
                    for(int k=0;k<22;k++){var p=h+new Vector3(-.5f+(float)rng.NextDouble()*1,1.02f,-1.4f+(float)rng.NextDouble()*2.8f);coals.Lump(p,new Vector3(.18f,.1f,.16f),k,.4f,new Color(1,.62f,.3f,1),new Color(.6f,.2f,.08f,.6f),7,4);}
                    var embers=Piece(forge,"forge coals",coals,false,Ember);
                    // The hood and its flue up the wall, a piece of their own: tall and narrow, faded by the camera like every tall thing in the town, and kept out of the bench's box.
                    var flue=new ZoneKit.Shape().Box(h+new Vector3(.1f,2.7f,0),new Vector3(1.9f,.9f,3.7f),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.1f),.6f)
                        .Box(h+new Vector3(.62f,4.3f,0),new Vector3(.6f,2.4f,.8f),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.1f),.6f);
                    foreach(float dz in new[]{-1.6f,1.6f})flue.Beam(h+new Vector3(-.75f,1.1f,dz),h+new Vector3(-.75f,2.3f,dz),.08f,.08f,Soot);
                    var flueView=Piece(forge,"forge hood and flue",flue,true,Iron);ZoneKit.Fadeable(flueView);
                    // The bellows at the hearth's north end, its board and its lever.
                    var b=At(Hearth+new Vector2(-.1f,3.2f));s.Slot(2).Lump(b+Vector3.up*.75f,new Vector3(.45f,.22f,.6f),7,.1f,new Color(.5f,.36f,.26f),new Color(.32f,.22f,.16f),10,5).Slot(6).Box(b+Vector3.up*.95f,new Vector3(.9f,.05f,1.2f),Quaternion.Euler(8,0,0),new Color(.52f,.4f,.3f));
                    s.Slot(6).Box(b+Vector3.up*.3f,new Vector3(.8f,.6f,1),Quaternion.identity,new Color(.44f,.34f,.26f)).Beam(b+new Vector3(0,1.1f,.6f),b+new Vector3(-.9f,1.5f,1.2f),.06f,.06f,new Color(.5f,.4f,.3f));
                    // The anvil on its stump, tongs and a hammer on it; the bar of cage metal being made a release clamp.
                    var a=At(Anvil);s.Slot(6).Post(a,.55f,.36f,.32f,10,new Color(.46f,.34f,.24f),new Color(.6f,.46f,.32f));
                    s.Slot(3).Box(a+Vector3.up*.66f,new Vector3(.5f,.22f,.9f),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.2f)).Box(a+Vector3.up*.86f,new Vector3(.6f,.18f,1.1f),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.25f));
                    s.Slot(3).Tube(new[]{a+new Vector3(0,.9f,.55f),a+new Vector3(0,.9f,.95f)},new[]{.1f,.01f},8,steel,steel,1,1);
                    s.Slot(3).Box(a+new Vector3(.08f,.99f,-.15f),new Vector3(.08f,.06f,.6f),Quaternion.Euler(0,12,0),new Color(.36f,.3f,.28f)).Box(a+new Vector3(-.2f,1,.2f),new Vector3(.32f,.08f,.1f),Quaternion.Euler(0,40,0),Soot);
                    var hot=new ZoneKit.Shape().Box(a+new Vector3(.12f,1.02f,-.1f),new Vector3(.07f,.05f,.4f),Quaternion.Euler(0,12,0),new Color(1,.7f,.36f,1));Piece(forge,"hot bar",hot,false,Ember);
                    // The quench trough, steaming; racks of the Bridle's bars and cage ribs along the wall; finished release gear on a pallet: clamp rings and yokes in bright metal.
                    var q=At(Quench);s.Slot(3).Box(q+Vector3.up*.35f,new Vector3(2.2f,.7f,.8f),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.1f)).Slot(4).Box(q+Vector3.up*.66f,new Vector3(2,.03f,.62f),Quaternion.identity,new Color(.42f,.56f,.6f));
                    var r=At(Racks);for(int k=0;k<3;k++)s.Slot(3).Post(r+new Vector3(0,0,-2+k*2),1.9f,.07f,.06f,6,Soot,steel);
                    s.Slot(3).Beam(r+new Vector3(0,1.85f,-2.1f),r+new Vector3(0,1.85f,2.1f),.08f,.08f,steel).Beam(r+new Vector3(0,.9f,-2.1f),r+new Vector3(0,.9f,2.1f),.07f,.07f,steel);
                    for(int k=0;k<11;k++){float z=-1.8f+k*.36f;s.Slot(3).Beam(r+new Vector3(-.45f,0,z),r+new Vector3(.1f,1.95f,z+.05f),.07f,.07f,new Color(.38f,.3f,.3f));}
                    for(int k=0;k<3;k++){var arc=new Vector3[9];var rad=new float[9];for(int i=0;i<9;i++){float t2=i/8f;arc[i]=r+new Vector3(-.8f,.3f+Mathf.Sin(t2*Mathf.PI)*1.3f,-1.4f+k*1.2f+(t2-.5f)*.9f);rad[i]=.07f;}s.Slot(3).Tube(arc,rad,6,new Color(.36f,.28f,.28f),new Color(.4f,.32f,.3f),1,1);}
                    var gp=At(Gear);s.Slot(6).Box(gp+Vector3.up*.07f,new Vector3(1.3f,.14f,1.1f),Quaternion.identity,new Color(.62f,.48f,.32f),new Color(.74f,.58f,.4f),.5f);
                    for(int k=0;k<5;k++){var ring=new Vector3[13];var rad=new float[13];var rc=gp+new Vector3(-.35f+(k%3)*.35f,.24f+(k/3)*.16f,-.2f+(k%2)*.3f);for(int i=0;i<13;i++){float an=i*Mathf.PI*2/12;ring[i]=rc+new Vector3(Mathf.Cos(an)*.22f,0,Mathf.Sin(an)*.22f);rad[i]=.05f;}s.Slot(5).Tube(ring,rad,6,Color.white,Color.white,1,1,false);}
                    // The mending table the queue comes to: what the convoy brings Tovra's smiths, a kettle, a pump's valve, a pot with its handle off.
                    var m=At(Mending);s.Slot(6).Box(m+Vector3.up*.45f,new Vector3(.9f,.9f,2.4f),Quaternion.identity,new Color(.5f,.4f,.3f),new Color(.66f,.52f,.38f),.5f);
                    s.Slot(5).Lump(m+new Vector3(0,1.02f,-.7f),new Vector3(.17f,.14f,.17f),5,.04f,Color.white,Color.white,9,5).Slot(3).Lump(m+new Vector3(.05f,1.0f,.1f),new Vector3(.2f,.12f,.2f),6,.06f,steel,Soot,9,4).Slot(5).Tube(new[]{m+new Vector3(0,.92f,.6f),m+new Vector3(0,1.1f,.75f)},new[]{.07f,.06f},8,Color.white,Color.white,1,1);
                    var view=Piece(forge,"forge",s,true,Brick,Iron,Hide,Iron,Slate,Brass,Wood);
                    Block(forge,view,h+Vector3.up*.7f,new Vector3(1.6f,1.4f,3.4f),0,"Hearth");Block(forge,view,b+Vector3.up*.5f,new Vector3(.9f,1,1.2f),0,"Bellows");
                    Block(forge,view,a+Vector3.up*.5f,new Vector3(.8f,1,1.2f),0,"Anvil");Block(forge,view,q+Vector3.up*.35f,new Vector3(2.2f,.7f,.8f),0,"Quench trough");
                    Block(forge,view,r+new Vector3(-.3f,.95f,0),new Vector3(1.2f,1.9f,4.4f),0,"Bar rack");Block(forge,view,gp+Vector3.up*.3f,new Vector3(1.3f,.6f,1.1f),0,"Release gear");
                    Block(forge,view,m+Vector3.up*.5f,new Vector3(.9f,1,2.4f),0,"Mending table");
                    // Hot and lit: the hearth's light breathing with the coals (yellow-white on the floor, never a marking's orange: D217), sparks off the anvil, smoke up the flue.
                    var light=Kit.Lamp(new Vector3(Hearth.x-.9f,1.8f,Hearth.y),10,1.6f,new Color(1,.82f,.58f));light.transform.SetParent(forge.root.transform,true);
                    var breathe=embers.AddComponent<LampRhythm>();breathe.count=breathe.retuned=LampRhythm.Count.Flicker;breathe.litFlag=breathe.retunedFlag=breathe.darkFlag="";breathe.rest=.78f;
                    breathe.lights=new[]{light};breathe.intensity=1.6f;breathe.lamps=new Renderer[]{embers.GetComponent<Renderer>()};breathe.colour=new Color(1,.62f,.32f);breathe.emission=2;
                    var sparks=Kit.Drift("Forge sparks",a+Vector3.up*1.2f,new Vector3(.4f,.2f,.4f),Kit.Glow("Arrivals sparks",false,new Color(1.8f,1.1f,.5f,1)),new Color(1,.78f,.42f,.9f),24,.025f,.06f,1.6f,.8f);
                    var main=sparks.main;main.gravityModifier=.6f;var shape=sparks.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=40;sparks.transform.rotation=Quaternion.Euler(-90,0,0);sparks.transform.SetParent(forge.root.transform,true);
                    var smoke=Kit.Drift("Forge smoke",h+new Vector3(.62f,5.6f,0),new Vector3(.5f,.2f,.6f),Kit.Glow("Arrivals smoke",false,new Color(.5f,.5f,.52f,1)),new Color(.55f,.55f,.58f,.28f),40,.8f,2.2f,.7f,5);
                    smoke.transform.SetParent(forge.root.transform,true);
                    var steam=Kit.Drift("Quench steam",q+Vector3.up*.8f,new Vector3(1.8f,.2f,.5f),Kit.Glow("Arrivals steam",false,new Color(.9f,.92f,.94f,1)),new Color(.92f,.93f,.95f,.22f),24,.4f,1.1f,.4f,3);
                    steam.transform.SetParent(forge.root.transform,true);
                    // The smiths at work (Odrun's, Pip's and the Warmaster's coats, worn by the Unsworn: the faces aboard are the named ones), and the queue with things to mend.
                    Hold(Folk(forge,"OdrunBody",new Vector2(48.6f,-34.8f),250,new Color(.9f,.92f,.98f),"a smith at the bar rack"),"tongs");
                    Hold(Folk(forge,"PipBody",new Vector2(45.4f,-36.2f),20,new Color(1.02f,.98f,.94f),"an apprentice smith at the quench",.9f),"tongs");
                    Hold(Folk(forge,"DraskBody",new Vector2(45.3f,-24.5f),160,new Color(.76f,.74f,.8f),"a smith carrying release gear"),"rings");
                    string[] wants={"a kettle","a pot","a valve","a kettle"};string[] queueBodies={"QuillBody","OkhariGrower","Neve","SennBody"};
                    Color[] queueTints={new(1.02f,.94f,.88f),new(.88f,.98f,.86f),new(.96f,.88f,.9f),new(.86f,.84f,.76f)};
                    for(int i=0;i<QueueAt.Length;i++)Hold(Folk(forge,queueBodies[i],QueueAt[i],88+i*4,queueTints[i],"in the forge's queue with "+wants[i]),wants[i].Substring(2));
                    var more=Also(forge,"more smiths aboard",MoreSmiths);
                    Hold(Folk(forge,"OdrunBody",new Vector2(42.4f,-21.4f),200,new Color(1.04f,.96f,.92f),"a smith come aboard with her family",1,more.transform),"tongs");
                    Folk(forge,"PipBody",new Vector2(40.6f,-21.6f),190,new Color(.92f,.98f,1.04f),"a smith's child come aboard",.74f,more.transform);
                    Sound(forge,"AMB_HandsForge",Anvil+new Vector2(.8f,0),1.6f,.6f,5,30);
                }

                // ---------------------------------------------------------------- chapter 8: Drask in a cell on the freight yard, apart from Dessik in the hall
                static void Cell(ZoneKit kit)
                {
                    var cell=Arrive("Drask's cell on the freight yard",EightDone);var s=new ZoneKit.Shape();Color bar=new(.36f,.36f,.4f),barLit=new(.5f,.5f,.54f);
                    // Bars round three sides of the strip along the container's north face (the container is the fourth wall), a barred roof, the door in the north side locked.
                    float x0=CellMin.x,x1=CellMax.x,z0=CellMin.y,z1=CellMax.y,top=2.3f;
                    void Bars(Vector2 a,Vector2 b){int n=Mathf.Max(1,Mathf.RoundToInt(Vector2.Distance(a,b)/.24f));for(int k=0;k<=n;k++){var p=At(Vector2.Lerp(a,b,k/(float)n));s.Slot(0).Post(p,top,.03f,.03f,5,bar,barLit);}
                        s.Slot(0).Beam(At(a,top),At(b,top),.08f,.06f,bar).Beam(At(a,1.1f),At(b,1.1f),.06f,.04f,bar).Beam(At(a,.08f),At(b,.08f),.08f,.06f,bar);}
                    Bars(new Vector2(x0,z0+.1f),new Vector2(x0,z1));Bars(new Vector2(x0,z1),new Vector2(x1,z1));Bars(new Vector2(x1,z1),new Vector2(x1,z0+.1f));
                    for(float x=x0+.4f;x<x1;x+=.6f)s.Slot(0).Beam(At(x,z0+.1f,top),At(x,z1,top),.03f,.03f,bar);
                    s.Slot(1).Box(At(41.2f,z1,1.1f)+Vector3.back*.04f,new Vector3(.18f,.24f,.1f),Quaternion.identity,Bronze);
                    // Inside: a cot along the container, a stool, a bowl on it. He has what his soldiers had.
                    var cot=At(43.4f,-60.35f);s.Slot(2).Box(cot+Vector3.up*.25f,new Vector3(2,.12f,.72f),Quaternion.identity,new Color(.5f,.4f,.3f)).Slot(3).Box(cot+Vector3.up*.34f,new Vector3(1.9f,.08f,.66f),Quaternion.identity,new Color(.46f,.5f,.42f));
                    foreach(float dx in new[]{-.9f,.9f})s.Slot(2).Box(cot+new Vector3(dx,.12f,0),new Vector3(.06f,.24f,.66f),Quaternion.identity,Soot);
                    var stool=At(40.6f,-59.8f);s.Slot(2).Post(stool,.44f,.18f,.16f,8,new Color(.5f,.4f,.3f),new Color(.62f,.5f,.36f)).Slot(4).Lump(stool+Vector3.up*.5f,new Vector3(.12f,.05f,.12f),2,.02f,Cream,new Color(.6f,.56f,.5f),9,3);
                    var view=Piece(cell,"cell",s,true,Iron,Brass,Wood,Cloth,Panel);
                    Block(cell,view,At(x0,(z0+z1)*.5f+.05f,1.15f),new Vector3(.12f,2.3f,z1-z0-.1f),0,"Cell bars");Block(cell,view,At((x0+x1)*.5f,z1,1.15f),new Vector3(x1-x0,2.3f,.12f),0,"Cell bars");
                    Block(cell,view,At(x1,(z0+z1)*.5f+.05f,1.15f),new Vector3(.12f,2.3f,z1-z0-.1f),0,"Cell bars");Block(cell,view,cot+Vector3.up*.2f,new Vector3(2,.4f,.72f),0,"Cot");
                    // A lamp outside, and one of Abri's watch keeping it.
                    var lp=new ZoneKit.Shape();var lf=At(CellLamp);lp.Slot(0).Post(lf,2.4f,.06f,.05f,6,Soot,new Color(.5f,.52f,.56f)).Beam(lf+Vector3.up*2.35f,lf+new Vector3(.5f,2.35f,-.3f),.05f,.05f,Soot).Slot(1).Lump(lf+new Vector3(.5f,2.1f,-.3f),new Vector3(.12f,.16f,.12f),2,.04f,Color.white,Color.white,7,4);
                    var lamp=Piece(cell,"cell lamp",lp,true,Panel,Lamp);Block(cell,lamp,lf+Vector3.up*1.2f,new Vector3(.24f,2.4f,.24f),0,"Lamp post");
                    Kit.Lamp(new Vector3(CellLamp.x+.5f,1.9f,CellLamp.y-.3f),7,1,new Color(1,.82f,.58f)).transform.SetParent(cell.root.transform,true);
                    Hold(Folk(cell,"Mira",CellGuard,80,new Color(.78f,.84f,.96f),"one of Keeper Abri's watch at the cell"),"pike");
                }

                // ---------------------------------------------------------------- chapter 9: Oda's cabin with a wall
                static void OdaCabin(ZoneKit kit)
                {
                    var cabin=Arrive("Oda's cabin",OdaFound);var s=new ZoneKit.Shape();float face=43.56f;
                    // The frame on the wall, over the cabin's window: line nine hundred and four, the crack straight through the nine.
                    var fc=At(OdaCabinMid+.75f,face-.24f,1.8f);
                    s.Slot(0).Box(fc,new Vector3(.98f,.74f,.06f),Quaternion.identity,new Color(.42f,.3f,.2f)).Slot(1).Box(fc+Vector3.back*.035f,new Vector3(.84f,.6f,.01f),Quaternion.identity,new Color(.2f,.24f,.28f));
                    s.Slot(2).Quad(fc+new Vector3(-.3f,.28f,-.045f),fc+new Vector3(-.26f,.28f,-.045f),fc+new Vector3(-.16f,-.29f,-.045f),fc+new Vector3(-.2f,-.29f,-.045f),Vector3.back,new Color(.86f,.88f,.9f),.5f);
                    // What a hauler family carries: bundles strapped as they were strapped to the outside, a trunk, a rolled mattress.
                    var b=At(OdaBundles);var rng=new System.Random(904);
                    for(int k=0;k<4;k++){var p=b+new Vector3((k%2)*.5f-.25f,.22f+(k/2)*.38f,(k%2)*.12f);var tone=Dyes[(k*2+1)%Dyes.Length];s.Slot(3).Lump(p,new Vector3(.32f,.2f,.26f),k,.25f,tone,ZoneKit.Shape.Dim(tone,.6f),9,4);
                        s.Slot(4).Tube(new[]{p+new Vector3(-.05f,.2f,-.27f),p+new Vector3(-.05f,.22f,0),p+new Vector3(-.05f,.2f,.27f)},new[]{.025f,.025f,.025f},4,Soot,Soot,1,1,false);}
                    s.Slot(0).Box(b+new Vector3(.9f,.25f,0),new Vector3(.7f,.5f,.45f),Quaternion.Euler(0,10,0),new Color(.36f,.4f,.46f),new Color(.44f,.5f,.56f));
                    var view=Piece(cabin,"Oda's cabin",s,true,Wood,Slate,Chalk,Cloth,Panel);
                    Block(cabin,view,b+new Vector3(.3f,.4f,0),new Vector3(1.7f,.8f,.7f),0,"Bundles");
                    Words(cabin,"904",fc+Vector3.back*.05f,2.6f,new Color(.92f,.9f,.84f),.8f,"Line nine hundred and four, framed");
                    // Oda in the lane and her mother at the door until they go home for the Halo's last day (the frame and the bundles stay: the cabin is theirs). Oda's talk aboard
                    // (OdaAboard) is chapter 9's, its script still pending, so she is her body until it is live.
                    var home=Arrive("Oda and her mother at their cabin",OdaFound,HomeReturned);
                    StandIn(home,"Oda","OdaBody",OdaAt,190,"OdaAboard is in docs/writing/pending/Home.yarn: her Npc and its beat wait for it");
                    Folk(home,"Mira",OdaMother,200,new Color(.8f,.78f,.84f),"Oda's mother at their door",.98f);
                }

                // ---------------------------------------------------------------- chapter 9: Bryn Asta and her navigators round the compass rose
                static void Navigators(ZoneKit kit)
                {
                    var rose=Arrive("Bryn's navigators at the chart table",NavigatorsFound);
                    string[] coats={"Neve","Hal","SurvivorNatural","Mira","Hal"};Color[] tints={new(.9f,.96f,1.04f),new(1.02f,.94f,.88f),new(.86f,.92f,.96f),new(.96f,.9f,1),new(.84f,.88f,.98f)};
                    for(int i=0;i<NavigatorsAt.Length;i++){var at=NavigatorsAt[i];float yaw=Mathf.Atan2(ChartTable.x-at.x,ChartTable.y-at.y)*Mathf.Rad2Deg;Hold(Folk(rose,coats[i],at,yaw,tints[i],"a Nacre navigator at the chart"),"chart");}
                    var captain=Also(rose,"Bryn at the chart table",TugFound);
                    var bryn=StandIn(rose,"Bryn","BrynBody",BrynAt,270,"BrynChartLesson is in docs/writing/pending/Home.yarn: chapter 9's staging wires the lesson");bryn.transform.SetParent(captain.transform,true);
                }

                // ---------------------------------------------------------------- chapter 9: the tug Welcome Back, lashed where the cutters were
                static void Tug(ZoneKit kit)
                {
                    var tug=Arrive("the tug Welcome Back off the stern",TugFound);var s=new ZoneKit.Shape();var c=new Vector3(TugAt.x,-2.6f,TugAt.y);var turn=Quaternion.Euler(0,96,0);
                    Color hull=new(.26f,.42f,.4f),trim=new(.86f,.8f,.6f);
                    // A squat working hull in the Nacre approach's green, scuffed by nineteen years of other people's hulls: fenders all round, a tall bridge, a tow boom, the line still on it.
                    s.Slot(0).Box(c,new Vector3(4.6f,2.4f,8.6f),turn,hull,ZoneKit.Shape.Dim(hull,1.15f),.4f).Box(c+turn*new Vector3(0,1.6f,-1.2f),new Vector3(3,1.6f,3.2f),turn,trim,ZoneKit.Shape.Dim(trim,1.1f),.4f);
                    s.Slot(1).Box(c+turn*new Vector3(0,1.9f,.42f),new Vector3(2.6f,.5f,.06f),turn,new Color(.2f,.26f,.3f));
                    for(int k=0;k<8;k++){var f=c+turn*new Vector3((k%2==0?-1:1)*2.4f,-.3f,-3.4f+(k/2)*2.2f);s.Slot(2).Tube(new[]{f+Vector3.up*.3f,f+Vector3.down*.3f},new[]{.34f,.34f},10,Soot,Soot,1,1);}
                    s.Slot(0).Beam(c+turn*new Vector3(0,1.4f,2.6f),c+turn*new Vector3(0,3.6f,5.6f),.24f,.24f,trim);
                    s.Slot(3).Rope(c+turn*new Vector3(0,3.6f,5.6f),new Vector3(TugAt.x+4,1.2f,ZMin+.25f),.6f,.05f,new Color(.92f,.82f,.56f));
                    var view=Shade(Kit.Built("Arrivals the tug Welcome Back",s,Panel,Glass,Panel,Rope),true);
                    var stars=GameObject.Find("Stars below");if(stars!=null){view.transform.SetParent(stars.transform,true);With(tug,view);}else view.transform.SetParent(tug.root.transform,true);
                    var post=new ZoneKit.Shape().Post(new Vector3(TugAt.x+4,0,-64.9f),.46f,.18f,.15f,8,Soot,new Color(.5f,.52f,.56f));Piece(tug,"tug bollard",post,false,Panel);
                }

                // ---------------------------------------------------------------- chapter 9: Abri's wall of names ("start at the door and go round")
                static readonly string[] NamesOnTheWall=
                {
                    "Tamsin Orrow","Halvard Pike","Juna Reil","Corse Abel","Ennet Vair","Mabry Sorn","Pell Haddon","Oskar Lume","Wren Tallis","Dagny Fell","Ruel Okho","Saba Kirr",
                    "Ivo Marsh","Tessaly Brand","Nim Arrow","Gant Selwyn","Aud Merrin","Coll Ashby","Fennick Dray","Lio Barrow","Hesk Tannet","Mora Quill","Sivert Rook","Ama Feld",
                    "Bryce Tolland","Kaya Wend","Orla Penn","Dacey Hulme","Tuva Ostry","Rafe Kinnock","Imma Sorrel","Jory Vance","Lenke Hale","Pardo Grey","Ysolde Vey","Kell Marrin",
                };
                static void Names(ZoneKit kit)
                {
                    var wall=Arrive("Keeper Abri's wall of names",NoticeHeard);var chalk=new Color(.96f,.96f,.92f);int n=0;
                    // The dock door's leaf first, under and over what Pell chalked on it, then round the yard along the freight hall's south wall: each name where its people stand.
                    foreach(var (x,y) in new[]{(9.3f,.55f),(12,.55f),(9.3f,.98f),(12,.98f),(9.6f,4.5f),(13,4.5f),(9.6f,4.95f),(13,4.95f),(11.4f,5.4f)})Words(wall,NamesOnTheWall[n++%NamesOnTheWall.Length],new Vector3(x,y,-55.34f),2.6f,chalk,2.6f,"A name on the wall");
                    foreach(var (x0,x1) in new[]{(16.8f,27.6f),(36.4f,46.4f)})
                        for(float x=x0+1.1f;x<x1;x+=2.5f)foreach(float y in new[]{.36f,.78f})Words(wall,NamesOnTheWall[n++%NamesOnTheWall.Length],new Vector3(x+(y>.5f?.6f:0),y,-54.1f),2.6f,chalk,2.4f,"A name on the wall");
                    // A bucket of chalk by the door; someone writing; people standing by theirs.
                    var s=new ZoneKit.Shape();var bk=At(8.9f,-55.85f);s.Slot(0).Tube(new[]{bk,bk+Vector3.up*.34f},new[]{.16f,.19f},10,new Color(.5f,.52f,.56f),new Color(.6f,.62f,.66f),1,1,false).Slot(1).Disc(bk+Vector3.up*.3f,Vector3.up,.16f,10,new Color(.92f,.92f,.88f));
                    var view=Piece(wall,"chalk bucket",s,true,Panel,Chalk);Block(wall,view,bk+Vector3.up*.17f,new Vector3(.4f,.34f,.4f),0,"Chalk bucket");
                    Hold(Folk(wall,"Neve",Writer,8,new Color(.84f,.9f,1),"writing a name on the wall"),"chalk");
                    string[] who={"SennBody","OkhariGrower","Hal"};Color[] tint={new(1.04f,.96f,.9f),new(.9f,1,.9f),new(.92f,.86f,.82f)};
                    for(int i=0;i<NameReaders.Length;i++)Folk(wall,who[i],NameReaders[i],176+i*8,tint[i],"standing by a name on the wall");
                }

                // ---------------------------------------------------------------- chapter 9: Nacre's families in the freight yard ("four hundred and eleven more aboard")
                static void Nacre(ZoneKit kit)
                {
                    var yard=Arrive("Nacre's families in the freight yard",NoticeHeard);var s=new ZoneKit.Shape();var rng=new System.Random(411);var kept=new List<(Vector2 at,int k)>();
                    for(int k=0;k<14;k++)
                    {
                        var p=new Vector2(14.6f+(float)rng.NextDouble()*15.4f,-63.2f+(float)rng.NextDouble()*1.6f);var tone=Paints[rng.Next(Paints.Length)];
                        if(NacreAt.Any(q=>Vector2.Distance(p,q)<1.3f)||kept.Any(o=>Vector2.Distance(p,o.at)<1.1f))continue;kept.Add((p,k));
                        if(k%3==0)s.Slot(0).Tube(new[]{At(p,.14f)+Vector3.left*.6f,At(p,.14f)+Vector3.right*.6f},new[]{.15f,.15f},10,tone,ZoneKit.Shape.Dim(tone,.8f),1,1);
                        else s.Slot(0).Lump(At(p,.2f),new Vector3(.3f,.2f,.24f),k,.25f,tone,ZoneKit.Shape.Dim(tone,.6f),9,4);
                    }
                    var view=Piece(yard,"Nacre's bundles",s,true,Cloth);
                    foreach(var (p,_) in kept)Block(yard,view,At(p,.2f),new Vector3(.9f,.4f,.6f),0,"Bundle");
                    string[] who={"Hal","SurvivorNatural","QuillBody"};Color[] tint={new(.8f,.86f,.94f),new(.96f,.9f,.86f),new(.9f,.94f,1.06f)};
                    for(int i=0;i<NacreAt.Length;i++)Hold(Folk(yard,who[i],NacreAt[i],190+i*10,tint[i],"a family from Nacre with their bundles"),"bundle");
                }

                // ---------------------------------------------------------------- chapter 9 into 10: Orrin's board on the gallery's front, the herb stall moved for it
                static void Board(ZoneKit kit)
                {
                    var board=Arrive("Orrin's board under the gallery",Launched);var s=new ZoneKit.Shape();var c=At(BoardAt,1.9f);
                    s.Slot(0).Box(c,new Vector3(2.8f,2,.08f),Quaternion.identity,new Color(.44f,.34f,.24f)).Slot(1).Box(c+Vector3.back*.05f,new Vector3(2.6f,1.8f,.02f),Quaternion.identity,new Color(.22f,.26f,.24f));
                    s.Slot(0).Box(At(BoardAt.x,BoardAt.y-.12f,.86f),new Vector3(2.8f,.06f,.18f),Quaternion.identity,new Color(.52f,.4f,.28f));
                    Piece(board,"Orrin's board",s,false,Wood,Slate);
                    var face=c+Vector3.back*.08f;var chalk=new Color(.95f,.95f,.9f);
                    Words(board,"CINDER HALO",face+Vector3.up*.6f,2.6f,chalk,2.6f,"Chalk on Orrin's board");Words(board,"DEPARTED,",face+Vector3.up*.26f,2.4f,chalk,2.6f,"Chalk on Orrin's board");
                    Words(board,"ALL SECTIONS",face+Vector3.down*.06f,2.4f,chalk,2.6f,"Chalk on Orrin's board");
                    var sorrel=Also(board,"Sorrel on Orrin's board",NineDone);Words(board,"SORREL",face+Vector3.down*.56f,3,new Color(1,.92f,.7f),2.6f,"Chalk on Orrin's board",sorrel.transform);
                    StandIn(board,"Orrin","Orrin",OrrinAt,190,"he is aboard from the launch and has no node until chapter 10 (OrrinRoll, pending)");
                    // The herb stall it took the place of, set up again across the forecourt.
                    Stall("Arrivals herb stall moved",HerbStallMoved,10,new Color(.46f,.62f,.34f),new Color(.86f,.62f,.3f),new Color(.5f,.74f,.38f),Leaf).transform.SetParent(board.root.transform,true);
                    solids.Add((board.what+": herb stall",board.root.GetComponentsInChildren<BoxCollider>().Last()));
                }

                // ---------------------------------------------------------------- chapter 9: Ket's folding bench in the arcade
                static void KetsBench(ZoneKit kit)
                {
                    var bench=Arrive("Ket's bench in the arcade",BenchesCarried);var s=new ZoneKit.Shape();var c=At(KetBench);Color wood=new(.68f,.52f,.36f);
                    // Hal's folding bench, opened out in front of an empty frontage: its legs crossed, a vice, tools laid along it, a small lamp on a crook; Ket behind it.
                    s.Slot(0).Box(c+Vector3.up*.86f,new Vector3(2,.08f,.8f),Quaternion.identity,wood,ZoneKit.Shape.Dim(wood,1.15f));
                    foreach(int side in new[]{-1,1}){s.Slot(0).Beam(c+new Vector3(side*.8f,0,-.35f),c+new Vector3(side*.8f,.84f,.35f),.06f,.06f,ZoneKit.Shape.Dim(wood,.8f)).Beam(c+new Vector3(side*.8f,0,.35f),c+new Vector3(side*.8f,.84f,-.35f),.06f,.06f,ZoneKit.Shape.Dim(wood,.8f));}
                    s.Slot(1).Box(c+new Vector3(-.7f,1,-.2f),new Vector3(.24f,.2f,.18f),Quaternion.identity,new Color(.4f,.42f,.46f));
                    for(int k=0;k<6;k++)s.Slot(1).Box(c+new Vector3(-.3f+k*.17f,.92f,.1f),new Vector3(.05f,.03f,.32f),Quaternion.Euler(0,k*11-20,0),k%2==0?new Color(.5f,.52f,.56f):new Color(.6f,.42f,.3f));
                    s.Slot(1).Post(c+new Vector3(.85f,.9f,.25f),.5f,.02f,.02f,5,Bronze,Bronze).Slot(2).Lump(c+new Vector3(.85f,1.42f,.25f),new Vector3(.07f,.09f,.07f),1,.03f,Color.white,Color.white,6,4);
                    var view=Piece(bench,"Ket's bench",s,true,Wood,Panel,Lamp);Block(bench,view,c+Vector3.up*.45f,new Vector3(2,.9f,.8f),0,"Folding bench");
                    StandIn(bench,"Ket","Hal",KetAt,180,"Hal's apprentice, no lines; the bench's working Bench and the delivery point wait on chapter 9's PortableBenches").transform.localScale=Vector3.one*.92f;
                }

                // ---------------------------------------------------------------- what people hold (a still says what they're doing)
                static void Hold(GameObject who,string what)
                {
                    ZoneKit.Shape load=null;Material[] stuff=null;Color steel=new(.5f,.52f,.56f);
                    switch(what)
                    {
                        case "bowl":load=new ZoneKit.Shape().Lump(new Vector3(.05f,1.08f,.34f),new Vector3(.15f,.07f,.15f),2,.02f,Cream,new Color(.6f,.56f,.5f),10,3);stuff=new[]{Panel};break;
                        case "fork":load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.3f,.1f,.45f),new Vector3(.34f,1.7f,.2f)},new[]{.025f,.022f},5,new Color(.86f,.82f,.72f),new Color(.86f,.82f,.72f),1,.6f).Slot(1).Lump(new Vector3(.3f,.12f,.55f),new Vector3(.25f,.08f,.2f),1,.3f,new Color(.86f,.76f,.5f),new Color(.6f,.5f,.34f),8,3);stuff=new[]{Bone,Straw};break;
                        case "moss":load=new ZoneKit.Shape().Lump(new Vector3(0,1.1f,.4f),new Vector3(.35f,.22f,.25f),4,.3f,new Color(.58f,.62f,.36f),new Color(.38f,.4f,.24f),9,4);stuff=new[]{Straw};break;
                        case "book":load=new ZoneKit.Shape().Box(new Vector3(0,1.15f,.32f),new Vector3(.3f,.04f,.22f),Quaternion.Euler(-30,0,0),Color.white,Color.white,2.78f);stuff=new[]{Books};break;
                        case "chart":load=new ZoneKit.Shape().Box(new Vector3(0,1.12f,.36f),new Vector3(.62f,.02f,.44f),Quaternion.Euler(-40,0,0),new Color(.92f,.88f,.74f));stuff=new[]{Cloth};break;
                        case "tongs":load=new ZoneKit.Shape().Beam(new Vector3(.32f,.9f,.25f),new Vector3(.36f,.9f,.95f),.04f,.04f,Soot);stuff=new[]{Iron};break;
                        case "rings":load=new ZoneKit.Shape().Tube(Enumerable.Range(0,13).Select(i=>new Vector3(Mathf.Cos(i*Mathf.PI/6)*.24f,1.05f,.42f+Mathf.Sin(i*Mathf.PI/6)*.24f)).ToArray(),Enumerable.Repeat(.05f,13).ToArray(),6,Color.white,Color.white,1,1,false);stuff=new[]{Brass};break;
                        case "pike":load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.3f,0,.2f),new Vector3(.3f,2.4f,.25f)},new[]{.03f,.025f},5,new Color(.5f,.4f,.3f),new Color(.5f,.4f,.3f),1,.5f).Slot(1).Tube(new[]{new Vector3(.3f,2.4f,.25f),new Vector3(.3f,2.75f,.26f)},new[]{.05f,.005f},5,steel,steel,1,1);stuff=new[]{Wood,Iron};break;
                        case "kettle":load=new ZoneKit.Shape().Lump(new Vector3(.05f,1,.38f),new Vector3(.16f,.14f,.16f),5,.04f,Color.white,Color.white,9,5);stuff=new[]{Brass};break;
                        case "pot":load=new ZoneKit.Shape().Tube(new[]{new Vector3(0,.92f,.4f),new Vector3(0,1.12f,.4f)},new[]{.2f,.22f},10,Soot,steel,1,1,false);stuff=new[]{Iron};break;
                        case "valve":load=new ZoneKit.Shape().Disc(new Vector3(0,1.05f,.4f),Vector3.back,.18f,10,Color.white,.08f);stuff=new[]{Brass};break;
                        case "chalk":load=new ZoneKit.Shape().Box(new Vector3(.3f,1.55f,.35f),new Vector3(.04f,.04f,.12f),Quaternion.Euler(-60,0,0),new Color(.95f,.95f,.9f));stuff=new[]{Chalk};break;
                        case "bundle":load=new ZoneKit.Shape().Lump(new Vector3(0,1.25f,-.28f),new Vector3(.32f,.36f,.22f),7,.2f,new Color(.6f,.5f,.4f),new Color(.4f,.32f,.26f),9,4);stuff=new[]{Cloth};break;
                    }
                    if(load==null)return;
                    var carried=Kit.Built(who.name+" holds "+what,load,stuff);carried.transform.SetParent(who.transform,false);carried.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                }

                // ---------------------------------------------------------------- the plan, checked before anything is switched off
                /// <summary>Everyone and everything an arrival puts down, against the city's own rules (City.cs KeepsOff and Refuse): on level floor, nobody with no name within two
                /// metres of a way, nothing on a sign or a spot a player stands at, nothing between the camera and someone with a name, nobody on a walker's loop, no two neighbours alike.</summary>
                static void Plan()
                {
                    float Level(Vector2 p)=>p.y>37.5f&&p.x>-24.5f&&p.x<24.5f&&p.y<58?GalleryTop:0;
                    var named=Named.Concat(Staged).Concat(new[]{IruAt,HaskAt,TovraAt,BrekkAt,DraskAt,IshenAt,GardenerAt,OdaAt,BrynAt,OrrinAt}).ToArray();
                    foreach(var (body,at,what) in bodies)
                    {
                        float y=Height(at);if(Mathf.Abs(y-Level(at))>.12f)faults.Add($"{what} at {at} stands at {y:0.00}, not on the floor");
                        if(FromWays(at)<2)faults.Add($"{what} at {at} is {FromWays(at):0.0} m from a way people walk");
                        if(OnASign(at))faults.Add($"{what} at {at} stands on a floor sign");
                        foreach(var spot in Spots)if(Vector2.Distance(at,spot)<2.2f)faults.Add($"{what} at {at} stands on {spot}, where a player stands to do something");
                        foreach(var p in named)if(Vector2.Distance(at,p)<1.4f&&Vector2.Distance(at,p)>.01f)faults.Add($"{what} at {at} stands on someone with a name at {p}");
                        foreach(var (_,loop,_,_,_,back,name) in Errands)for(int k=0;k<loop.Length-(back?1:0);k++)if(ZoneKit.Segment(at,loop[k],loop[(k+1)%loop.Length])<1.6f)faults.Add($"{what} at {at} stands on the walk of {name}");
                        foreach(var f in Hands.Folk)if(f.body==body&&Vector2.Distance(at,new Vector2(f.x,f.z))<3.4f)faults.Add($"{what} at {at} wears the same body as the townsperson beside it ({f.doing})");
                        // Idra's crew share the one Rauk body there is (a crew body is owed), so they stand apart by coat tint and scale instead.
                        foreach(var (other,there,whatElse) in bodies)if(other==body&&there!=at&&Vector2.Distance(at,there)<3.2f&&!body.StartsWith("Idra"))faults.Add($"{what} at {at} and {whatElse} at {there} wear one body side by side");
                        foreach(var person in Named)if(ZoneKit.InSightOf(at,person,2.4f,4.5f,1.6f)&&Vector2.Distance(at,person)>.01f)faults.Add($"{what} at {at} stands between the camera and someone with a name at {person}");
                    }
                }

                // ---------------------------------------------------------------- once the city's people and story stand
                /// <summary>Runs after the city's People and Story (one line in City.cs's Build): what arrives takes over the bodies and the floor of what it replaces, every arrival is
                /// checked against everything that now stands, and then everything is switched off, so the bake that follows never sees any of it.</summary>
                internal static void Settle(ZoneKit kit)
                {
                    Borrowed(kit);
                    MakeRoom();
                    Physics.SyncTransforms();
                    var mine=new HashSet<Transform>(places.Select(p=>p.root.transform).Concat(hidden.Select(h=>h.transform)));
                    bool Mine(Component c){for(var t=c.transform;t!=null;t=t.parent)if(mine.Contains(t))return true;return false;}
                    // What each arrival stands in: a scattered piece is moved off for it (hidden from its flag on); anything else is a fault.
                    var moved=new Dictionary<GameObject,string>();
                    foreach(var (what,box) in solids)
                    {
                        if(box==null)continue;var centre=box.transform.TransformPoint(box.center);var half=Vector3.Scale(box.size,box.transform.lossyScale)*.48f;half=new Vector3(Mathf.Abs(half.x),Mathf.Abs(half.y),Mathf.Abs(half.z));
                        foreach(var other in Physics.OverlapBox(centre,half,box.transform.rotation,~0,QueryTriggerInteraction.Ignore))
                        {
                            if(Mine(other)||other.name=="Many Hands deck"||other.attachedRigidbody!=null||Cleared(other,what))continue;
                            var top=other.transform.root.gameObject;
                            if(Scattered.Contains(top.name)){var place=places.First(p=>what.StartsWith(p.what));if(!moved.ContainsKey(top))moved[top]=place.from;continue;}
                            faults.Add($"{what} stands in {other.name} ({top.name}) at ({other.transform.position.x:0.0}, {other.transform.position.z:0.0})");
                        }
                    }
                    foreach(var (piece,from) in moved){Where("Arrivals: moved for what came with "+from,piece,null,null,null,from,false);Debug.Log($"MANYHANDS_ARRIVAL_MOVED {piece.name} at ({piece.transform.position.x:0.0}, {piece.transform.position.z:0.0}) from {from}");}
                    // Every walker's loop clear of what arrives: a walker stands still for good at whatever is in its way (the city's own check, which runs after these are switched off).
                    foreach(var (_,loop,_,_,_,back,name) in Errands)
                        for(int k=0;k<loop.Length-(back?1:0);k++)
                        {
                            var a=At(loop[k],.95f);var next=loop[(k+1)%loop.Length];var b=At(next,.95f);var run=b-a;run.y=0;
                            foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))if(Mine(hit.collider)&&hit.collider.GetComponentInParent<WalkLoop>()==null)faults.Add($"{name}'s walk runs into {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                        }
                    // Nothing wide and tall of theirs between the fixed camera and a talk, a spot or the hall's floor.
                    var floor=Named.Concat(Spots).Concat(Staged).Concat(new[]{F,IruAt,HaskAt,TovraAt,BrekkAt,DraskAt,IshenAt,GardenerAt,OdaAt,BrynAt,OrrinAt}).Select(p=>At(p)).ToList();
                    foreach(var place in places)foreach(var part in place.root.GetComponentsInChildren<Renderer>(true))
                    {
                        if(part is ParticleSystemRenderer||part.GetComponentInParent<TMPro.TMP_Text>()!=null)continue;var bounds=part.bounds;
                        if(Mathf.Max(bounds.size.x,bounds.size.z)>=3&&ZoneKit.HidesFloor(bounds,floor))faults.Add($"{place.what}: {part.name} stands between the camera and a place a talk happens");
                    }
                    if(faults.Count>0)throw new InvalidOperationException("Many Hands arrivals: the city as it stands refuses them:\n"+string.Join("\n",faults.Distinct()));
                    NothingStandsOnTheWays();
                    SwitchOff();
                    Debug.Log("MANYHANDS_ARRIVALS_OK places="+places.Count+" "+string.Join("; ",places.Select(p=>p.what+" from "+p.from)));
                }
                /// <summary>How near a way's line the edge of anything solid may come (the bake's agent is .55 m round, its cells .15: nearer, a straight walk along the way is cut), how
                /// near someone with no name may stand, and a walker's stop (XylosBuilder.Rootmarket's rule).</summary>
                const float WayClear=.9f,FolkClear=2,LoopClear=1.6f;
                /// <summary>The city's declared ways against the finished scene, whatever stood each thing there, with every arrival standing (so every stage at once) and every state of
                /// the city's own: a solid's footprint (a box's own turned rectangle, otherwise its bounds) within <see cref="WayClear"/> of a way's line, a townsperson within
                /// <see cref="FolkClear"/>, a walker's stop within <see cref="LoopClear"/>. A scattered kit piece is measured by its footprint, not its centre, and dropped where it
                /// reaches a way (the draws stay in step); anything else fails the build by name. The named stand at the ways' ends and seals close across them by design, so
                /// neither counts; the landform is the only thing wider than forty metres, and what stands under a third of a metre is floor detail.</summary>
                static void NothingStandsOnTheWays()
                {
                    Physics.SyncTransforms();var found=new List<string>();var dropped=new List<GameObject>();
                    float Outside(Vector2 p,Vector2 centre,float yaw,Vector2 half)
                    {
                        var d=p-centre;float r=yaw*Mathf.Deg2Rad,x=d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),z=d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r);
                        float ox=Mathf.Abs(x)-half.x,oz=Mathf.Abs(z)-half.y;return new Vector2(Mathf.Max(ox,0),Mathf.Max(oz,0)).magnitude+Mathf.Min(Mathf.Max(ox,oz),0);
                    }
                    (float gap,Vector2 from,Vector2 to) Nearest(Vector2 centre,float yaw,Vector2 half)
                    {
                        var best=(float.PositiveInfinity,Vector2.zero,Vector2.zero);float reach=Mathf.Max(half.x,half.y)+4;
                        foreach(var way in Ways)for(int i=1;i<way.Length;i++)
                        {
                            if(ZoneKit.Segment(centre,way[i-1],way[i])>reach)continue;
                            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(way[i-1],way[i])/.1f));
                            for(int k=0;k<=steps;k++){float gap=Outside(Vector2.Lerp(way[i-1],way[i],k/(float)steps),centre,yaw,half);if(gap<best.Item1)best=(gap,way[i-1],way[i]);}
                        }
                        return best;
                    }
                    foreach(var solid in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    {
                        if(solid==null||!solid.enabled||solid.isTrigger||solid.attachedRigidbody!=null||solid.name.IndexOf("deck",StringComparison.OrdinalIgnoreCase)>=0||solid.GetComponentInParent<Npc>(true)!=null||solid.GetComponentInParent<Membrane>(true)!=null||solid.GetComponentInParent<WalkLoop>(true)!=null)continue;
                        Vector2 at,half;float yaw=0,top;
                        if(solid is BoxCollider box&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.x,0))<.5f&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.z,0))<.5f)
                        {
                            var c=solid.transform.TransformPoint(box.center);var size=Vector3.Scale(box.size,solid.transform.lossyScale);size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
                            at=new Vector2(c.x,c.z);half=new Vector2(size.x,size.z)*.5f;yaw=solid.transform.eulerAngles.y;top=c.y+size.y*.5f;
                        }
                        else{if(!solid.gameObject.activeInHierarchy)continue;var b=solid.bounds;at=new Vector2(b.center.x,b.center.z);half=new Vector2(b.extents.x,b.extents.z);top=b.max.y;}
                        if(half.x>20||half.y>20||top<Height(at)+.32f)continue;
                        bool folk=solid.GetComponentInParent<PropTint>(true)!=null&&solid.GetComponent<BoxCollider>()!=null&&solid.transform.GetComponentInChildren<SkinnedMeshRenderer>(true)!=null;
                        if(folk){half=Vector2.zero;yaw=0;}
                        var (gap,from,to)=Nearest(at,yaw,half);float need=folk?FolkClear:WayClear;if(gap>=need-.01f)continue;
                        var top0=solid.transform.root.gameObject;
                        if(Scattered.Contains(top0.name)){if(!dropped.Contains(top0))dropped.Add(top0);continue;}
                        found.Add($"{(solid.transform.parent!=null?solid.transform.parent.name+"/":"")}{solid.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from the way from {from} to {to} (needs {need})");
                    }
                    foreach(var walk in UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var p in walk.path)
                    {var (gap,from,to)=Nearest(new Vector2(p.x,p.z),0,Vector2.zero);if(gap<LoopClear)found.Add($"{walk.name}'s stop at ({p.x:0.0}, {p.z:0.0}) is {gap:0.00} m from the way from {from} to {to}");}
                    foreach(var piece in dropped){Debug.Log($"MANYHANDS_WAYS_DROPPED {piece.name} at ({piece.transform.position.x:0.0}, {piece.transform.position.z:0.0})");UnityEngine.Object.DestroyImmediate(piece);}
                    if(found.Count>0)throw new InvalidOperationException("Many Hands: something stands on the ways:\n"+string.Join("\n",found.Distinct()));
                    Debug.Log("MANYHANDS_WAYS_CLEAR dropped="+dropped.Count);
                }
                /// <summary>The kit pieces the city scatters (Dock, Spine, Garden, RaftBay, Cabins, LampGallery, Forecourts): what may be moved off for an arrival.</summary>
                static readonly HashSet<string> Scattered=new(){"DressCargoPallet","DressCableDrum","DeckCrate","MusterCrate","DressFuelTank","DressPowerCell","HushwellCrates","DressCoreRack","DressWorkLamp",
                    "DressRailBuffer","DressPlanter","DressKiosk","DeckBench","DressWorkbench","DressFanShrub","DressSucculent","SpicePodPlant","OrchardTree","DressLightBollard","DressSolarArray","DressShelfRack"};
                /// <summary>The bodies of people who have come aboard, worn by townsfolk until they did (City.cs: "Ishen and Brekk until their chapters"; Orrin is aboard from the launch):
                /// each of those goes at the chapter that brings its person, and someone of the peoples now aboard takes the job over, in the same place.</summary>
                static void Borrowed(ZoneKit kit)
                {
                    var swaps=new Dictionary<string,(string flag,string[] bodies)>{["IshenBody"]=(SevenDone,new[]{"KeptHerder"}),["BrekkBody"]=(EightDone,new[]{"OdrunBody","TernBody"}),["Orrin"]=(Launched,new[]{"Neve","Hal","Hal"})};
                    var people=UnityEngine.Object.FindObjectsByType<PropTint>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    var used=new Dictionary<string,int>();
                    GameObject Find(string name,Vector2 at)=>people.Select(p=>p.gameObject).Where(g=>g.name==name).OrderBy(g=>Vector2.Distance(new Vector2(g.transform.position.x,g.transform.position.z),at)).FirstOrDefault(g=>Vector2.Distance(new Vector2(g.transform.position.x,g.transform.position.z),at)<.6f);
                    string Next(string body){used.TryGetValue(body,out int k);used[body]=k+1;var choices=swaps[body].bodies;return choices[k%choices.Length];}
                    foreach(var (body,x,z,yaw,tint,doing) in Hands.Folk)
                    {
                        if(!swaps.TryGetValue(body,out var swap))continue;
                        var was=Find("Townsperson, "+doing,new Vector2(x,z));if(was==null){faults.Add($"the townsperson {doing} at ({x}, {z}) in {body}'s body wasn't found to stand down");continue;}
                        if(was.transform.parent!=null&&was.transform.parent.GetComponent<FightPocket>()!=null)continue;
                        var place=Arrive("someone else "+doing+" (was in "+body+"'s body)",swap.flag);
                        var now=Kit.Townsperson(Next(body),new Vector3(x,0,z),yaw,new Color(tint.b,tint.g,tint.r),"Neve");now.name=was.name;now.transform.SetParent(place.root.transform,true);Carry(now,doing);
                        Where(Prefix+"stands down for "+body+"'s own: "+doing,was,null,null,null,swap.flag,false);
                    }
                    foreach(var (body,loop,speed,pause,tint,back,name) in Errands)
                    {
                        if(!swaps.TryGetValue(body,out var swap))continue;
                        var was=UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(w=>w.name==name);if(was==null){faults.Add($"the walker {name} wasn't found to stand down");continue;}
                        var place=Arrive("someone else: "+name,swap.flag);
                        var now=Kit.Walker(Next(body),loop,speed,pause,new Color(tint.b,tint.g,tint.r),"Neve");now.thereAndBack=back;now.name=name;now.transform.SetParent(place.root.transform,true);Carry(now.gameObject,name);
                        Where(Prefix+"stands down for "+body+"'s own: "+name,was.gameObject,null,null,null,swap.flag,false);
                    }
                }
                /// <summary>What the arrivals take the floor of: the loose crates in the pen's half and on the forge's floor (the hall is the fight's only while it is on: they are back for
                /// it, and a reload puts them where they were built), the freight crew's hand by the forge, the herb stall under Orrin's board.</summary>
                static void MakeRoom()
                {
                    foreach(var crate in UnityEngine.Object.FindObjectsByType<Lattice.Combat.LooseCargo>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    {
                        var at=new Vector2(crate.transform.position.x,crate.transform.position.z);
                        if(InPen(at,-1.2f))Gone(Prefix+"a loose crate moved for the herd",crate.gameObject,SixDone);
                        else if(at.x>40&&at.y>-40&&at.y<-20)Gone(Prefix+"a loose crate moved for the forge",crate.gameObject,EightDone);
                    }
                    foreach(var crew in UnityEngine.Object.FindObjectsByType<PropTint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.name=="Townsperson, freight crew"))
                    {var at=new Vector2(crew.transform.position.x,crew.transform.position.z);if(at.x>40&&at.y>-40&&at.y<-20)Gone(Prefix+"the freight crew's hand makes room for the forge",crew.gameObject,EightDone);}
                    var stall=GameObject.Find("Herb stall");if(stall!=null)Gone(Prefix+"the herb stall moved for Orrin's board",stall,Launched);else faults.Add("the herb stall wasn't found to move for Orrin's board");
                }
                /// <summary>Something of the city's that goes from a flag on, to make room for what comes with it: what an arrival with that flag may stand in.</summary>
                static void Gone(string name,GameObject target,string flag){Where(name,target,null,null,null,flag,false);madeRoom[target]=flag;}
                static bool Cleared(Collider other,string what){var from=places.First(p=>what.StartsWith(p.what)).from;for(var t=other.transform;t!=null;t=t.parent)if(madeRoom.TryGetValue(t.gameObject,out var flag)&&flag==from)return true;return false;}
                /// <summary>Every arrival switched off as saved, so the bake never reads it; what is solid in one carves the paths once it is switched on.</summary>
                static void SwitchOff()
                {
                    foreach(var place in places)
                    {
                        foreach(var box in place.root.GetComponentsInChildren<BoxCollider>(true))
                        {
                            if(box.isTrigger||box.attachedRigidbody!=null||box.GetComponent<NavMeshObstacle>()!=null)continue;
                            var hole=box.gameObject.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Box;hole.center=box.center;hole.size=box.size;hole.carving=true;hole.carveOnlyStationary=true;
                        }
                        place.root.SetActive(false);
                    }
                    foreach(var other in hidden)other.SetActive(false);
                }

                // ---------------------------------------------------------------- views
                /// <summary>LookDev's views of the arrivals (appended to the city's own): each as it is first seen, and the hall wide.</summary>
                internal static readonly LookDev.View[] Views=
                {
                    new("arrivals-pad",new Vector3(-27,0,-59)),new("arrivals-pen",new Vector3(22,0,-37)),new("arrivals-pen-gate",new Vector3(29.4f,0,-50f)),new("arrivals-iru",new Vector3(15.6f,GalleryTop,45.6f)),
                    new("arrivals-crew",new Vector3(72.4f,0,-4)),new("arrivals-archive",new Vector3(-33,0,40.2f)),new("arrivals-gardener",new Vector3(42.6f,0,48.4f)),new("arrivals-release",new Vector3(57.6f,0,12.6f)),
                    new("arrivals-lantern",new Vector3(-16,GalleryTop,53)),new("arrivals-forge",new Vector3(44.6f,0,-31.6f)),new("arrivals-forge-queue",new Vector3(38.6f,0,-32)),new("arrivals-cell",new Vector3(41.8f,0,-57.2f)),
                    new("arrivals-names",new Vector3(21,0,-57.4f)),new("arrivals-names-east",new Vector3(40.6f,0,-56.6f)),new("arrivals-oda",new Vector3(-40.6f,0,40.8f)),new("arrivals-navigators",new Vector3(.6f,GalleryTop,45.6f)),
                    new("arrivals-board",new Vector3(15.4f,0,33.4f)),new("arrivals-ket",new Vector3(40,0,7.6f)),new("arrivals-hall-wide",new Vector3(32,0,-36),false,2.2f),
                };

                // ---------------------------------------------------------------- stills of the city at each stage of the story
                /// <summary>Opens the saved city, stands every Whereabouts, FlagGate, FightPocket, TurnsUp and YieldedCaptain as a stage's flags would at load, and takes the city's views and
                /// the arrivals' (LookDev's camera, heroes and light) into <c>Builds/quality/look/&lt;run&gt;/&lt;stage&gt;</c>. Nothing is saved. Stages: ch5 (first docking), ch6 to ch9 (as the
                /// party docks after that chapter: every quest of every earlier chapter done), ch9-nacre (chapter 9 after Nacre, before the Halo), ch10 (the city chapter 10 starts in).</summary>
                internal static void Stills()
                {
                    string run=Lattice.Core.DevArgs.Value("-arrivals-run")??throw new ArgumentException("-arrivals-run <name> is required");
                    string[] stages=(Lattice.Core.DevArgs.Value("-arrivals-stages")??"ch5,ch6,ch7,ch8,ch9-nacre,ch10").Split(',',StringSplitOptions.RemoveEmptyEntries);
                    string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));if(Directory.Exists(root))throw new InvalidOperationException("Preserve earlier look run: "+root);
                    var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
                    var def=AssetDatabase.LoadAssetAtPath<ZoneDef>("Assets/_Project/Resources/Definitions/ZoneDef/"+City+".asset");int count=0;
                    foreach(string stage in stages)
                    {
                        var flags=StageFlags(stage);string folder=Path.Combine(root,stage);Directory.CreateDirectory(folder);
                        EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+City+".unity");Stand(flags);
                        foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))if(text.name.StartsWith("Sign_"))text.gameObject.SetActive(false);
                        foreach(var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                        {var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);}
                        if(def!=null)RenderSettings.skybox=def.skybox;
                        var camera=new GameObject("Look camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                        camera.clearFlags=CameraClearFlags.Skybox;camera.nearClipPlane=.3f;camera.farClipPlane=1500;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
                        foreach(var view in Hands.Views.Concat(Views).GroupBy(v=>v.name).Select(g=>g.First()))
                        {
                            var heroes=Heroes(view.point);var rotation=Quaternion.Euler(ground.pitch,ground.yaw,0);camera.fieldOfView=ground.fov;
                            camera.transform.SetPositionAndRotation(view.point+Vector3.up-rotation*Vector3.forward*ground.distance*Mathf.Max(1,view.zoom),rotation);
                            Shot(camera,Path.Combine(folder,City+"-"+view.name+".png"));count++;foreach(var h in heroes)UnityEngine.Object.DestroyImmediate(h);
                        }
                        UnityEngine.Object.DestroyImmediate(camera.gameObject);Debug.Log("MANYHANDS_ARRIVAL_STILLS "+stage+" flags="+flags.Count+" folder="+folder);
                    }
                    Debug.Log($"MANYHANDS_ARRIVAL_STILLS_TAKEN count={count} root={root}");
                }
                /// <summary>A stage's flags, as DevLoadout arrives at a chapter (every earlier chapter's quests told, with what they set), and every side story of those chapters done too,
                /// which is the fullest the city can be; chapter 9's own flags from its manifest, which nothing defines yet.</summary>
                internal static HashSet<string> StageFlags(string stage)
                {
                    int chapter=stage=="ch9-nacre"?9:int.Parse(stage.Substring(2));var flags=new HashSet<string>();
                    void Told(QuestDef quest)
                    {
                        flags.Add("quest."+quest.id+".complete");foreach(string f in quest.flagsOnComplete??Array.Empty<string>())flags.Add(f);foreach(string f in quest.storyFlags??Array.Empty<string>())flags.Add(f);
                        if(!string.IsNullOrWhiteSpace(quest.cardFlag))flags.Add(quest.cardFlag);
                        foreach(var step in quest.steps??Array.Empty<Objective>())
                        {
                            if(step.kind==ObjectiveKind.Flag)flags.Add(step.target);else if(step.kind==ObjectiveKind.TalkTo)flags.Add("met."+step.target);
                            else if(step.kind==ObjectiveKind.Kill){var enemy=ZoneKit.Find<EnemyDef>(step.target);if(enemy!=null&&enemy.boss)flags.Add("bossdown."+step.target);}
                        }
                    }
                    int told=stage=="ch9-nacre"?9:chapter+1;
                    for(int k=1;k<told;k++)flags.Add("chapter"+k+".complete");
                    foreach(var guid in AssetDatabase.FindAssets("t:QuestDef")){var quest=AssetDatabase.LoadAssetAtPath<QuestDef>(AssetDatabase.GUIDToAssetPath(guid));if(quest!=null&&quest.chapter>0&&quest.chapter<told)Told(quest);}
                    if(chapter>=9)foreach(string f in new[]{"home.nacreReached",OdaFound,TugFound,NavigatorsFound,"home.nacreSpoken",NoticeHeard,"home.chartLent","home.crawlNamed"})flags.Add(f);
                    if(stage=="ch9"||chapter>=10)foreach(string f in new[]{HomeReturned,"home.meetingHeld","home.berthRefused","home.saintReleased","home.halChose","home.coronachWalked",Launched,"home.cutterDisabled","home.chartRead",NineDone,BenchesCarried})flags.Add(f);
                    return flags;
                }
                /// <summary>The open scene as the flags would stand it at load.</summary>
                internal static void Stand(HashSet<string> flags)
                {
                    bool Has(string f)=>!string.IsNullOrEmpty(f)&&flags.Contains(f);
                    T[] All<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    foreach(var w in All<Whereabouts>())
                    {
                        bool here=(string.IsNullOrEmpty(w.from)||Has(w.from))&&!Has(w.goneAt)&&(string.IsNullOrEmpty(w.awayFrom)||!Has(w.awayFrom)||Has(w.backAt));
                        foreach(var t in w.targets)if(t!=null)t.SetActive(here);
                    }
                    foreach(var g in All<FlagGate>()){if(string.IsNullOrEmpty(g.flag))continue;bool show=g.showWhenSet?Has(g.flag):!Has(g.flag);foreach(var t in g.targets)if(t!=null)t.SetActive(show);}
                    foreach(var p in All<FightPocket>()){int state=Has(p.until)?2:string.IsNullOrEmpty(p.from)||Has(p.from)?1:0;foreach(var t in p.before)if(t!=null)t.SetActive(state==0);foreach(var t in p.during)if(t!=null)t.SetActive(state==1);foreach(var t in p.after)if(t!=null)t.SetActive(state==2);foreach(var t in p.alarm)if(t!=null)t.SetActive(false);}
                    foreach(var u in All<TurnsUp>())if(Has(u.flag))foreach(var t in u.targets)if(t!=null)t.SetActive(true);
                    foreach(var y in All<YieldedCaptain>()){if(y.npc!=null)y.npc.gameObject.SetActive(Has(y.downFlag));foreach(var g in y.guards)if(g!=null)g.SetActive(Has(y.heardFlag)&&Has(y.guardedFlag));}
                    foreach(var l in All<LeavesAfter>())if(Has(l.flag))l.gameObject.SetActive(false);
                }
                static GameObject[] Heroes(Vector3 point)
                {
                    var made=new List<GameObject>();
                    foreach(var (name,i) in new[]{("TarenNatural",0),("SelaNatural",1)})
                    {
                        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+name+".prefab");if(prefab==null)continue;
                        var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var at=point+new Vector3(i*2.2f-1.1f,0,0);
                        if(Physics.Raycast(at+Vector3.up*6,Vector3.down,out var hit,20,~0,QueryTriggerInteraction.Ignore))at=hit.point;
                        body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,160,0));foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                        foreach(var animator in body.GetComponentsInChildren<Animator>()){var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];var idle=clips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);}
                        made.Add(body);
                    }
                    return made.ToArray();
                }
                static void Shot(Camera camera,string path)
                {
                    var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                    var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                    File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }
    }
}
