using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Data;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The loot and progression layer's definitions (S2, D223): every material, affix, consumable, part, recipe and stall the systems read, written through
    /// <see cref="ArenaBuilder.Asset{T}"/> so a run changes only the fields named here and can be repeated. Entry: <c>Lattice.EditorTools.ProgressionBuilder.Build</c>.
    /// <list type="bullet">
    /// <item>Materials for every region of the ten chapters, each with what a shop pays for it. Two commons and a rare a region; the rare is a boss's or an elite's.</item>
    /// <item>Twenty affixes in five pools by slot. An affix's stats are written for a tier 1 part and scale with the part's tier (Gear.AffixStats).</item>
    /// <item>Thirteen consumables with carry caps: three heals by share of the bar, two Charge, a party tonic, a revive, three draughts and three cooked meals.</item>
    /// <item>Parts: the first two tiers get their pools and drop flags; Prime, Tempered and Choirwrought are written in every slot (a baseline, two typed weapons, a light frame,
    /// a scout drive, a plain module); fourteen modules with a passive; two more trophy parts. Tempered and Choirwrought have no recipe, stall or drop here, so nothing reaches them
    /// until their chapters place them.</item>
    /// <item>Recipes through chapter 5 (Xylos and Wendmire materials), and a stall for every seller so far.</item>
    /// </list>
    /// A later chapter adds its recipes, stalls and drops by calling the helpers here from its own region's builder, or by adding a block to this file.
    /// </summary>
    public static class ProgressionBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{Define();AssetDatabase.SaveAssets();Debug.Log("PROGRESSION_OK");});
        /// <summary>Everything, in the order each kind needs the last: a part's pool names affixes, a recipe names a part.</summary>
        public static void Define(){Materials();Affixes();Consumables();Parts();Recipes();Stalls();Enemies();}

        static T Find<T>(string id)where T:ScriptableObject=>AssetDatabase.LoadAssetAtPath<T>("Assets/_Project/Resources/Definitions/"+typeof(T).Name+"/"+id+".asset");
        static Ingredient In(string id,int count)=>new(){id=id,count=count};
        public static readonly string[] TierNames={"","Scrap","Refined","Prime","Tempered","Choirwrought"};

        // ------------------------------------------------------------------ materials
        /// <summary>A region's two commons and its rare, by the chapter that reaches it. Edges and frames are upgraded with the first common, everything else with the second.</summary>
        public readonly struct Region
        {
            public readonly int chapter;public readonly string first,second,rare;
            public Region(int chapter,string first,string second,string rare){this.chapter=chapter;this.first=first;this.second=second;this.rare=rare;}
            public string Common(GearSlot slot)=>slot==GearSlot.Edge||slot==GearSlot.Frame||string.IsNullOrEmpty(second)?first:second;
        }
        public static readonly Region Xylos=new(3,"Rootfibre","AmberSap","HeartwoodNode"),Wendmire=new(4,"ReedWeave","Glowspore","LureBulb"),Hoarfell=new(6,"RimeGlass","HostAlloy","MarshalsSeal"),
            Oriel=new(7,"FibreThread","SingingGlass","WardenKey"),Vaskar=new(8,"ScoriaGlass","BridleWire","FoundrySeal"),Separation=new(9,"HaloRivet","NacreShard","RingKeystone"),
            Mooring=new(10,"AnchorThread","","StillwaterCore");
        /// <summary>The region a tier's own parts are upgraded with: Prime is Xylos's, Tempered Hoarfell's, Choirwrought the Separation's.</summary>
        static Region RegionOf(int tier)=>tier>=5?Separation:tier==4?Hoarfell:Xylos;
        static void Materials()
        {
            void M(string id,string name,int value,int chapter,bool rare=false)=>ArenaBuilder.Asset<MaterialDef>(id,m=>{m.id=id;m.displayName=name;m.value=value;m.chapter=chapter;m.rare=rare;});
            // What a shop pays is always under what any stall asks for one (ProgressionTests checks every stall), so nothing can be bought and sold round in a circle.
            M("ScrapAlloy","Scrap Alloy",1,1);M("LatticeFilament","Lattice Filament",2,1);M("RidgeCrystal","Ridge Crystal",4,1);
            M("HuskCore","Husk Core",10,1);M("ChoirResin","Choir Resin",6,1);M("CantorPearl","Cantor Pearl",100,1,true);
            M("Rootfibre","Rootfibre",5,3);M("AmberSap","Amber Sap",6,3);M("HeartwoodNode","Heartwood Node",150,3,true);
            M("ReedWeave","Reed Weave",6,4);M("Glowspore","Glowspore",7,4);M("LureBulb","Lure Bulb",170,4,true);
            M("RimeGlass","Rime Glass",9,6);M("HostAlloy","Host Alloy",10,6);M("MarshalsSeal","Marshal's Seal",220,6,true);
            M("FibreThread","Fibre Thread",11,7);M("SingingGlass","Singing Glass",12,7);M("WardenKey","Warden Key",250,7,true);
            M("ScoriaGlass","Scoria Glass",13,8);M("BridleWire","Bridle Wire",14,8);M("FoundrySeal","Foundry Seal",280,8,true);
            M("HaloRivet","Halo Rivet",15,9);M("NacreShard","Nacre Shard",16,9);M("RingKeystone","Ring Keystone",320,9,true);
            M("AnchorThread","Anchor Thread",18,10);M("StillwaterCore","Stillwater Core",400,10,true);
        }

        // ------------------------------------------------------------------ affixes
        static readonly string[] WeaponPool={"Honed","Keen","Vicious","Breaker","Attuned","Siphon","Hunter","Executioner"},FramePool={"Plated","Bulwark","Balanced"},
            DrivePool={"Quick","Fleet","Flashbound","Scavenger"},ModulePool={"Resonant","Charged","Overcharged","Quickcycle","Lucky"};
        static AffixDef[] Pool(GearSlot slot)=>(slot==GearSlot.Frame?FramePool:slot==GearSlot.Drive?DrivePool:slot==GearSlot.Module?ModulePool:WeaponPool).Select(Find<AffixDef>).Where(a=>a!=null).ToArray();
        static void Affixes()
        {
            void A(string id,Stats bonus,Mods mods)=>ArenaBuilder.Asset<AffixDef>(id,a=>{a.id=a.displayName=id;a.bonus=bonus;a.mods=mods;});
            // Stats, for a tier 1 part: each is multiplied by the tier of the part it's on. Resonant was a flat +4 on any part; on a Refined part it still is.
            A("Resonant",new Stats{resonance=2},default);A("Honed",new Stats{output=3},default);A("Plated",new Stats{plating=3},default);A("Quick",new Stats{response=2},default);
            A("Lucky",new Stats{fortune=3},default);A("Balanced",new Stats{output=1,plating=1,response=1,resonance=1,fortune=1},default);
            // Effects, the same on any tier.
            A("Keen",default,new Mods{critChance=.05f});A("Vicious",default,new Mods{critDamage=.25f});A("Breaker",default,new Mods{breakPower=.2f});A("Attuned",default,new Mods{ownType=.15f});
            A("Charged",default,new Mods{chargeGain=.15f});A("Siphon",default,new Mods{lifeOnKill=.02f});A("Overcharged",default,new Mods{skillCost=-2});A("Quickcycle",default,new Mods{cooldown=-.1f});
            A("Bulwark",default,new Mods{guardCost=-.25f});A("Fleet",default,new Mods{mashCost=-.5f,thrustRegen=.2f});A("Flashbound",default,new Mods{flashWindow=.03f,flashCharge=15});
            A("Hunter",default,new Mods{brokenDamage=.15f});A("Executioner",default,new Mods{executeDamage=.2f});A("Scavenger",default,new Mods{dropLuck=.05f});
        }

        // ------------------------------------------------------------------ consumables
        static void Consumables()
        {
            void C(string id,string name,int cap,int value,Action<ConsumableDef> more)=>ArenaBuilder.Asset<ConsumableDef>(id,c=>
            {
                c.id=id;c.displayName=name;c.stack=cap;c.value=value;c.heal=c.charge=c.healPercent=c.seconds=0;c.revive=c.party=c.meal=false;c.buff=default;c.mark="";
                more(c);
            });
            // A heal is a share of the hero's own bar (prog159's other half: a gel was a flat 60 on a bar that reaches 640). The gel keeps its 60 as a floor, for a new hero.
            C("RepairGel","Repair Gel",9,10,c=>{c.heal=60;c.healPercent=.25f;});
            C("RepairSalve","Repair Salve",6,30,c=>c.healPercent=.5f);
            C("RepairCore","Repair Core",3,80,c=>c.healPercent=1);
            C("ChargeCell","Charge Cell",5,12,c=>c.charge=50);
            C("ChargeBank","Charge Bank",3,30,c=>c.charge=100);
            C("SyncTonic","Sync Tonic",3,45,c=>{c.healPercent=.4f;c.charge=40;c.party=true;});
            C("RelinkSpike","Relink Spike",3,40,c=>{c.healPercent=.4f;c.revive=true;});
            // Draughts: a minute, one at a time.
            C("SurgeDraught","Surge Draught",3,25,c=>{c.seconds=60;c.mark="SURGE";c.buff=new Mods{damageDealt=.15f};});
            C("PlatingWax","Plating Wax",3,25,c=>{c.seconds=60;c.mark="WAX";c.buff=new Mods{damageTaken=-.2f};});
            C("Quickening","Quickening",3,25,c=>{c.seconds=60;c.mark="QUICK";c.buff=new Mods{thrustRegen=.5f};});
            // Cooked food (chapter 3's cook): two minutes, milder, beside a draught and not in its place; bought, never fabricated.
            C("RiverSkewer","River Skewer",3,9,c=>{c.meal=true;c.seconds=120;c.mark="SKEWER";c.buff=new Mods{thrustRegen=.2f};});
            C("PodStew","Pod Stew",3,12,c=>{c.meal=true;c.seconds=120;c.mark="STEW";c.buff=new Mods{regen=.0025f};});
            C("SpiceCake","Spice Cake",3,12,c=>{c.meal=true;c.seconds=120;c.mark="CAKE";c.buff=new Mods{chargeGain=.1f};});
        }

        // ------------------------------------------------------------------ parts
        /// <summary>Base stats by tier, index 3 to 5: an edge's and an emitter's Output, a frame's Plating, a drive's Response. A weapon stands at 35 to 55% of the hero's own Output
        /// at the Sync it's found (Prime at 11 to 16, Tempered at 17 to 25, Choirwrought at 26 to 30), as a Refined one does at 10.</summary>
        static readonly int[] EdgeOutput={0,8,30,50,70,95},EmitterOutput={0,8,25,42,60,80},FramePlating={0,8,16,26,38,52},DriveResponse={0,6,12,18,24,30};
        static TechPartDef Part(string id,string name,GearSlot slot,int tier,DamageType type,Stats stats,Mods mods,Region region,bool drops)=>ArenaBuilder.Asset<TechPartDef>(id,p=>
        {
            p.id=id;p.displayName=name;p.slot=slot;p.tier=tier;p.damageType=type;p.baseStats=stats;p.mods=mods;p.drops=drops;p.affixPool=Pool(slot);
            p.upgradeMaterial=tier>=3?region.Common(slot):"";p.upgradeRare=tier>=3?region.rare:"";
            // Broken down, a part gives Scrap by its tier and, from Prime, one of its region's common.
            p.salvage=tier>=3?new[]{In("ScrapAlloy",tier*2),In(region.Common(slot),1)}:new[]{In("ScrapAlloy",tier*2)};
        });
        static void Parts()
        {
            // The first two tiers and chapter 1's trophies keep their stats: they gain a pool for their slot, and the plain ones may drop.
            foreach(string kind in new[]{"Emitters","Edges","Frames","Drives","Modules"})for(int tier=1;tier<=2;tier++)
            {string id=kind+"T"+tier;if(Find<TechPartDef>(id)!=null)ArenaBuilder.Asset<TechPartDef>(id,p=>{p.affixPool=Pool(p.slot);p.drops=true;});}
            foreach(string id in new[]{"FramesHusk","EmittersChoir","ModulesPearl"})if(Find<TechPartDef>(id)!=null)ArenaBuilder.Asset<TechPartDef>(id,p=>{p.affixPool=Pool(p.slot);p.drops=false;});
            for(int tier=3;tier<=5;tier++)
            {
                string of=TierNames[tier];var region=RegionOf(tier);int edge=EdgeOutput[tier],emitter=EmitterOutput[tier],plating=FramePlating[tier],response=DriveResponse[tier];
                // A typed weapon gives up a twelfth of its Output for its type: against a weakness that's half again the damage and twice the Break, against a resistance half.
                Part("EdgesT"+tier,of+" Edge",GearSlot.Edge,tier,DamageType.Kinetic,new Stats{output=edge},default,region,true);
                Part("EdgesT"+tier+"Plasma",of+" Ember Edge",GearSlot.Edge,tier,DamageType.Plasma,new Stats{output=Mathf.Round(edge*.92f)},default,region,true);
                Part("EdgesT"+tier+"Pulse",of+" Pulse Edge",GearSlot.Edge,tier,DamageType.Pulse,new Stats{output=Mathf.Round(edge*.92f)},default,region,true);
                Part("EmittersT"+tier,of+" Emitter",GearSlot.Emitter,tier,DamageType.Beam,new Stats{output=emitter},default,region,true);
                Part("EmittersT"+tier+"Plasma",of+" Ember Emitter",GearSlot.Emitter,tier,DamageType.Plasma,new Stats{output=Mathf.Round(emitter*.92f)},default,region,true);
                Part("EmittersT"+tier+"Pulse",of+" Pulse Emitter",GearSlot.Emitter,tier,DamageType.Pulse,new Stats{output=Mathf.Round(emitter*.92f)},default,region,true);
                // Heavy against light: all Plating, or less of it and some Response; all Response, or less of it and some Fortune.
                Part("FramesT"+tier,of+" Frame",GearSlot.Frame,tier,DamageType.Kinetic,new Stats{plating=plating},default,region,true);
                Part("FramesT"+tier+"Light",of+" Light Frame",GearSlot.Frame,tier,DamageType.Kinetic,new Stats{plating=Mathf.Round(plating*.62f),response=Mathf.Round(response*.45f)},default,region,true);
                Part("DrivesT"+tier,of+" Drive",GearSlot.Drive,tier,DamageType.Kinetic,new Stats{response=response},default,region,true);
                Part("DrivesT"+tier+"Scout",of+" Scout Drive",GearSlot.Drive,tier,DamageType.Kinetic,new Stats{response=Mathf.Round(response*.6f),fortune=Mathf.Round(response*.5f)},default,region,true);
                Part("ModulesT"+tier,of+" Module",GearSlot.Module,tier,DamageType.Kinetic,new Stats{resonance=tier*5,fortune=tier*3},default,region,true);
            }
            // Trophy parts: a boss's rare is the key to one part a little past the Prime one in its slot, as chapter 1's trophies are (prog158). Made, never bought or found.
            Part("EdgesHeartwood","Heartwood Edge",GearSlot.Edge,3,DamageType.Kinetic,new Stats{output=54,plating=6},default,Xylos,false);
            Part("DrivesLure","Lurelight Drive",GearSlot.Drive,3,DamageType.Kinetic,new Stats{response=20,fortune=8},default,Wendmire,false);
            // Modules with a passive: small stats, and one thing that changes how the hero is played. Made or bought, never found at random.
            void Module(string id,string name,int tier,Stats stats,Mods mods,Region region)=>Part("Modules"+id,name,GearSlot.Module,tier,DamageType.Kinetic,stats,mods,region,false);
            // Chapter 3, Xylos.
            Module("GuardCapacitor","Guard Capacitor",3,new Stats{resonance=6},new Mods{guardCost=-.4f,flashCharge=15},Xylos);
            Module("MedicFilament","Medic Filament",3,new Stats{fortune=6},new Mods{sharedHeal=.5f},Xylos);
            Module("Reservoir","Reservoir",3,new Stats{resonance=8},new Mods{chargeHold=1,chargeFloor=30},Xylos);
            Module("TwinThread","Twin Thread",3,new Stats{resonance=4,fortune=4},new Mods{partnerDamage=.25f},Xylos);
            Module("FlashCoil","Flash Coil",3,new Stats{response=6},new Mods{flashDamage=.3f},Xylos);
            // Chapter 4, Wendmire: a prism for each damage type. Every hit of that type counts, a swing or a skill: with a typed weapon and Attuned it's a build.
            Module("PrismBeam","Beam Prism",3,new Stats{resonance=5},new Mods{beam=.15f},Wendmire);
            Module("PrismPlasma","Plasma Prism",3,new Stats{resonance=5},new Mods{plasma=.15f},Wendmire);
            Module("PrismKinetic","Kinetic Prism",3,new Stats{resonance=5},new Mods{kinetic=.15f},Wendmire);
            Module("PrismPulse","Pulse Prism",3,new Stats{resonance=5},new Mods{pulse=.15f},Wendmire);
            // Chapters 6 to 8 and 9 to 10: written now, placed by their chapters.
            Module("GlassLattice","Glass Lattice",4,new Stats{resonance=6},new Mods{damageDealt=.25f,damageTaken=.25f},Hoarfell);
            Module("BreakersKnuckle","Breaker's Knuckle",4,new Stats{fortune=6},new Mods{breakPower=.4f,brokenTime=1},Hoarfell);
            Module("MasteryLens","Mastery Lens",4,new Stats{resonance=10},new Mods{mastery=1},Oriel);
            Module("FortuneCompass","Fortune Compass",4,new Stats{fortune=12},new Mods{luckCap=.1f},Vaskar);
            Module("SecondWind","Second Wind",5,new Stats{plating=8},new Mods{secondWind=1},Separation);
        }

        // ------------------------------------------------------------------ recipes
        static Discipline Ladder(GearSlot slot)=>slot switch{GearSlot.Emitter=>Discipline.Emitters,GearSlot.Edge=>Discipline.Edges,GearSlot.Frame=>Discipline.Frames,GearSlot.Drive=>Discipline.Drives,_=>Discipline.Modules};
        /// <summary>A part's recipe, named as its part. <paramref name="rank"/> is the ladder level it asks for: a trophy's is 1, so it can be made the day it drops.</summary>
        public static void PartRecipe(string id,int rank,params Ingredient[] inputs)
        {
            var part=Find<TechPartDef>(id);if(part==null)throw new InvalidOperationException("No part "+id+" for its recipe");
            ArenaBuilder.Asset<RecipeDef>(id,r=>{r.id=id;r.displayName=part.displayName;r.discipline=Ladder(part.slot);r.tier=r.unlockLevel=rank;r.output=part;r.consumableOutput="";r.inputs=inputs;});
        }
        /// <summary>A consumable's recipe: no ladder, no rank, makeable anywhere out of a fight.</summary>
        public static void ItemRecipe(string id,params Ingredient[] inputs)
        {
            var item=Find<ConsumableDef>(id);if(item==null)throw new InvalidOperationException("No consumable "+id+" for its recipe");
            ArenaBuilder.Asset<RecipeDef>(id,r=>{r.id=id;r.displayName=item.displayName;r.discipline=Discipline.Emitters;r.tier=r.unlockLevel=1;r.output=null;r.consumableOutput=id;r.inputs=inputs;});
        }
        static void Recipes()
        {
            // Chapter 3: Prime, from Xylos. The plain parts are also Hadda's; the typed weapons, the light frame and the scout drive are only made or found.
            PartRecipe("EdgesT3",3,In("ScrapAlloy",10),In("Rootfibre",4));
            PartRecipe("EdgesT3Plasma",3,In("ScrapAlloy",10),In("Rootfibre",3),In("AmberSap",3));
            PartRecipe("EdgesT3Pulse",3,In("ScrapAlloy",10),In("Rootfibre",3),In("LatticeFilament",4));
            PartRecipe("EmittersT3",3,In("ScrapAlloy",10),In("AmberSap",4));
            PartRecipe("EmittersT3Plasma",3,In("ScrapAlloy",10),In("AmberSap",5),In("Rootfibre",1));
            PartRecipe("EmittersT3Pulse",3,In("ScrapAlloy",10),In("AmberSap",3),In("LatticeFilament",4));
            PartRecipe("FramesT3",3,In("ScrapAlloy",12),In("Rootfibre",5));
            PartRecipe("FramesT3Light",3,In("ScrapAlloy",8),In("Rootfibre",3),In("AmberSap",2));
            PartRecipe("DrivesT3",3,In("ScrapAlloy",10),In("AmberSap",4));
            PartRecipe("DrivesT3Scout",3,In("ScrapAlloy",8),In("AmberSap",3),In("Rootfibre",2));
            PartRecipe("ModulesT3",3,In("ScrapAlloy",10),In("AmberSap",3),In("Rootfibre",3));
            PartRecipe("ModulesReservoir",3,In("AmberSap",6),In("Rootfibre",3),In("ScrapAlloy",8));
            PartRecipe("ModulesTwinThread",3,In("Rootfibre",6),In("AmberSap",3),In("ScrapAlloy",8));
            PartRecipe("ModulesFlashCoil",3,In("AmberSap",4),In("Rootfibre",4),In("LatticeFilament",4));
            PartRecipe("EdgesHeartwood",1,In("HeartwoodNode",1),In("Rootfibre",6),In("ScrapAlloy",8));
            // Chapter 4: Wendmire's prisms and its trophy.
            foreach(string type in new[]{"Beam","Plasma","Kinetic","Pulse"})PartRecipe("ModulesPrism"+type,3,In("Glowspore",5),In("ReedWeave",2),In("ScrapAlloy",6));
            PartRecipe("DrivesLure",1,In("LureBulb",1),In("ReedWeave",6),In("ScrapAlloy",8));
            // Consumables. A meal has no recipe; the Repair Core's comes with its chapter.
            ItemRecipe("RepairSalve",In("AmberSap",2),In("ScrapAlloy",2));
            ItemRecipe("RelinkSpike",In("Rootfibre",1),In("LatticeFilament",2));
            ItemRecipe("ChargeBank",In("Glowspore",2),In("LatticeFilament",2));
            ItemRecipe("SyncTonic",In("AmberSap",1),In("Glowspore",1),In("ScrapAlloy",2));
            ItemRecipe("SurgeDraught",In("Glowspore",2),In("Rootfibre",1));
            ItemRecipe("PlatingWax",In("ReedWeave",2),In("AmberSap",1));
            ItemRecipe("Quickening",In("ReedWeave",2),In("Glowspore",1));
        }

        // ------------------------------------------------------------------ stalls
        static ShopOffer Sell(string id,int price,int lot=0,string requires="")=>new(){id=id,price=price,lot=lot,requires=requires};
        /// <summary>A stall: what it sells, each at its own price, in the order it's shown. Named for its zone, or "Zone_Seller" where a zone has more than one (ShopUi.For).</summary>
        public static ShopInventory Stall(string id,string title,params ShopOffer[] offers)=>ArenaBuilder.Asset<ShopInventory>(id,s=>{s.title=title;s.offers=offers;s.buysMaterials=true;});
        /// <summary>Corra's arch market. Chapter 2's builder calls this too, so a rebuild of its definitions writes the same stall.</summary>
        public static ShopInventory Lanternwick()=>Stall("Lanternwick","ARCH MARKET",
            Sell("EmittersT2",240),Sell("EdgesT2",240),Sell("FramesT2",260),Sell("DrivesT2",220),Sell("ModulesT2",280),Sell("RepairGel",22),Sell("ChargeCell",26),
            Sell("ScrapAlloy",14,3),Sell("LatticeFilament",16,3),Sell("RidgeCrystal",30,3));
        static void Stalls()
        {
            // Mira, on the Decks. With no inventory of its own her stall listed every tier 1 part at 60 and every consumable in the catalogue at 20: thirteen consumables would all have been hers.
            Stall("Hub_Decks","",Sell("EmittersT1",60),Sell("EdgesT1",60),Sell("FramesT1",60),Sell("DrivesT1",60),Sell("ModulesT1",60),Sell("ChargeCell",20),Sell("RepairGel",20),
                Sell("ScrapAlloy",12,3),Sell("LatticeFilament",12,3));
            Lanternwick();
            // Chapter 3, Rootmarket: Binna cooks and provisions, Hadda mends and sells parts at her press. Two of Binna's dishes wait for her baskets (the ThreeBaskets side quest).
            Stall("Rootmarket_Binna","BINNA'S KITCHEN",Sell("RiverSkewer",18),Sell("PodStew",24,0,"quest.ThreeBaskets.complete"),Sell("SpiceCake",24,0,"quest.ThreeBaskets.complete"),
                Sell("RepairGel",24),Sell("RepairSalve",60),Sell("ChargeCell",28),Sell("RelinkSpike",90));
            Stall("Rootmarket_Hadda","THE MENDER'S PRESS",Prime().Concat(new[]{Sell("ModulesGuardCapacitor",720),Sell("ModulesMedicFilament",680),Sell("ScrapAlloy",16,3),Sell("Rootfibre",45,3),Sell("AmberSap",54,3)}).ToArray());
            // Chapters 4 and 5, written ahead of their zones so the data is there to place; their builders can rewrite them with Stall(). Driftreed: Dov's smokehouse (his draughts
            // wait on the SmokehouseOrder side quest) and Quill's pump raft. Many Hands, the hub with every shop: Corra's parts, and the provisioner's counter for anyone else who sells.
            Stall("Driftreed_Dov","THE SMOKEHOUSE",Sell("RepairGel",24),Sell("RepairSalve",60),Sell("ChargeCell",28),Sell("ChargeBank",60),Sell("SyncTonic",90),Sell("RelinkSpike",90),
                Sell("SurgeDraught",50,0,"quest.SmokehouseOrder.complete"),Sell("PlatingWax",50,0,"quest.SmokehouseOrder.complete"),Sell("Quickening",50,0,"quest.SmokehouseOrder.complete"));
            Stall("Driftreed_Quill","QUILL'S PUMP RAFT",Prime().Concat(new[]{Sell("ScrapAlloy",16,3),Sell("ReedWeave",54,3),Sell("Glowspore",63,3)}).ToArray());
            // Many Hands' own builder writes Corra's stall as it stands (ManyHandsBuilder.Definitions); this is the same counter, the convoy's Choirwrought and all.
            var corra=Stall("ManyHands_Corra","CORRA'S STALL",Prime().Concat(new[]{Sell("ModulesGuardCapacitor",720),Sell("ModulesMedicFilament",680),
                Sell("ScrapAlloy",16,3),Sell("LatticeFilament",18,3),Sell("RidgeCrystal",32,3),Sell("Rootfibre",45,3),Sell("AmberSap",54,3),Sell("ReedWeave",54,3),Sell("Glowspore",63,3)}).Concat(Choirwrought(ConvoyIn)).ToArray());
            corra.noteFlag=ConvoyIn;corra.note=ConvoyNote;EditorUtility.SetDirty(corra);
            Stall("ManyHands","PROVISIONS",Sell("RepairGel",24),Sell("RepairSalve",60),Sell("ChargeCell",28),Sell("ChargeBank",60),Sell("SyncTonic",90),Sell("RelinkSpike",90),
                Sell("SurgeDraught",50),Sell("PlatingWax",50),Sell("Quickening",50),Sell("RiverSkewer",18),Sell("PodStew",24),Sell("SpiceCake",24));
        }
        /// <summary>The plain Prime part of every slot, at the price every stall asks for it.</summary>
        static IEnumerable<ShopOffer> Prime()=>new[]{Sell("EmittersT3",560),Sell("EdgesT3",600),Sell("FramesT3",620),Sell("DrivesT3",540),Sell("ModulesT3",640)};
        /// <summary>The plain Choirwrought part of every slot, on the counter once <paramref name="from"/> is set. Chapter 10 is sized on it (DevLoadout: Choirwrought from chapter 9's
        /// stalls), and until November 15 only Neighbourhood Afloat sold any, after the last boss: a party reached the finale with 14,000 scrip and nothing to spend it on.
        /// Priced on the ladder: a step up is about 1.7 times from Prime to Tempered and 1.53 from Tempered to Choirwrought (Neighbourhood Afloat's Emitter, Edge and Module, which
        /// these match; the Frame and the Drive by the same step from the Decks' 1,050 and 920).</summary>
        public static ShopOffer[] Choirwrought(string from)=>new[]{Sell("EmittersT5",1480,0,from),Sell("EdgesT5",1560,0,from),Sell("FramesT5",1620,0,from),Sell("DrivesT5",1420,0,from),Sell("ModulesT5",1600,0,from)};
        /// <summary>When the Choirwrought stock reaches Many Hands' stalls, and what Corra says of it: the Decks' convoy is in (chapter 9 ends at the fleet's chart).</summary>
        public const string ConvoyIn="chapter9.complete",ConvoyNote="OFF THE DECKS' CONVOY  ·  CHOIRWROUGHT";

        // ------------------------------------------------------------------ enemies
        /// <summary>What a kill pays, by name now (EnemyDef.scrip and scrap): the first two chapters' enemies keep the 8 and 2, and their bosses the 150 and 12, every enemy used to pay.
        /// A later chapter's builder sets its own, and puts parts in its loot tables with <see cref="AnyPart"/>.</summary>
        static void Enemies()
        {
            foreach(string guid in AssetDatabase.FindAssets("t:EnemyDef",new[]{"Assets/_Project/Resources/Definitions/EnemyDef"}))
            {
                var enemy=AssetDatabase.LoadAssetAtPath<EnemyDef>(AssetDatabase.GUIDToAssetPath(guid));if(enemy==null||enemy.scrip>=0&&enemy.scrap>=0)continue;
                if(enemy.scrip<0)enemy.scrip=enemy.boss?150:8;if(enemy.scrap<0)enemy.scrap=enemy.boss?12:2;EditorUtility.SetDirty(enemy);
            }
        }
        /// <summary>A loot entry for any part of a tier that drops, in one slot: <c>AnyPart(3,GearSlot.Edge,.08f)</c>. It comes rolled.</summary>
        public static LootEntry AnyPart(int tier,GearSlot slot,float chance)=>new(){count=1,chance=chance,partTier=tier,partSlot=slot};
        /// <summary>The same in any slot.</summary>
        public static LootEntry AnyPart(int tier,float chance)=>new(){count=1,chance=chance,partTier=tier,anySlot=true};
        /// <summary>A loot entry for a material, a consumable or a part by name.</summary>
        public static LootEntry Drop(string id,int count,float chance)=>new(){id=id,count=count,chance=chance};
    }
}
