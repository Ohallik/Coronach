using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>S6a: Idra Sorn as a hero who can fight. Her definition (stats, the style that makes her guard, hop and hammer hers, the bodies she borrows until her own are
    /// made, what she arrives wearing), her eleven skills (four known as she joins, then Sync 3, 5, 8, 12, 16, 20 and 24, the ladder the first two climb) and her EXTRA,
    /// their icon sheets' import settings, and the barks copied to where the game reads them. It can be run
    /// again at any time: every asset is found by its id and written over, and a body of her own that art intake has since given her is left alone.
    /// <code>Unity -batchmode -quit -projectPath Lattice -executeMethod Lattice.EditorTools.IdraBuilder.Build</code></summary>
    public static class IdraBuilder
    {
        public const string Hero="Idra";
        /// <summary>The generated sheets (tools/meshy/s6_idra_icons.py): her first eight skills on the first, her EXTRA and her Sync 16, 20 and 24 skills on the second.</summary>
        public const string Sheet="skill-icons-4",ExtraSheet="skill-icons-5";
        /// <summary>Whose bodies she stands in until IdraNatural, IdraBraced and IdraFlight are made: his rig has every clip a hero's kit asks for, the hammer's three swings among them.</summary>
        public const string Lender="Taren";
        const string Root="Assets/_Project/Resources/Definitions/";
        public static void Build()=>BatchTools.Run(()=>
        {
            Definitions();
            // Her lines (a shout for Last One Out, the guard's lesson, the rest of her voice) live with the rest of the barks, which the game reads from Resources.
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            File.Copy(Path.Combine(root,"docs/writing/barks.json"),"Assets/_Project/Resources/Audio/barks.json",true);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            // The sheets as the other three are imported (UiArtImporter): whole, clamped and uncompressed. Left as Unity first found them, their cells bleed into each other and blur.
            foreach(string sheet in new[]{Sheet,ExtraSheet})
            {
                var importer=AssetImporter.GetAtPath("Assets/_Project/Resources/UI/Generated/"+sheet+".png") as TextureImporter;
                if(importer==null)throw new System.Exception("the icon sheet "+sheet+" isn't in Resources/UI/Generated: run tools/meshy/s6_idra_icons.py --compose 1");
                importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var def=GameCatalog.Find<CharacterDef>(Hero);
            if(def==null||def.skills.Length!=4||def.skills.Any(s=>s==null)||def.learnable.Length!=Skills.Length-4||def.learnable.Any(s=>s==null)||def.extra==null)throw new System.Exception("Idra's skills didn't take");
            if(def.natural==null||def.shaped==null||def.flight==null)throw new System.Exception("Idra has no body to stand in: "+Lender+"'s weren't found");
            if(def.style==null||def.style.attack!=AttackStyle.Hammer)throw new System.Exception("Idra's style didn't take");
            Debug.Log("IDRA_OK");
        });
        /// <summary>(number, kind, name, the HUD's short label, what the row says, Sync level, Charge, cooldown, damage type, icon cell). The first four are known from the
        /// start and are her loadout as she joins; the others come with Sync 3, 5, 8, 12, 16, 20 and 24, as the first two heroes' do.</summary>
        static readonly (int number,SkillKind kind,string name,string label,string says,int level,float cost,float cooldown,DamageType type,int cell)[] Skills=
        {
            (1,SkillKind.Breach,"Breach","Breach","Burst through a line of foes behind your brace, striking each",1,20,5,DamageType.Kinetic,0),
            (2,SkillKind.TowLine,"Tow Line","Tow","A harpoon: reels a light foe to you, or pulls you to a heavy one",1,16,7,DamageType.Pulse,1),
            (3,SkillKind.Shelter,"Shelter","Shelter","A dome for 5 s that stops enemy shots for you both",1,28,18,DamageType.Pulse,2),
            (4,SkillKind.SlagHammer,"Slag Hammer","Slag","A slow overhead slam of molten weight. Huge Break",1,24,6,DamageType.Plasma,3),
            (5,SkillKind.RescueFlare,"Rescue Flare","Flare","Mends your partner from afar, and raises them if they're down",3,40,40,DamageType.Pulse,4),
            (6,SkillKind.Counterweight,"Counterweight","Counter","Your next three guarded hits each answer with a shockwave",5,18,14,DamageType.Kinetic,5),
            (7,SkillKind.Avalanche,"Avalanche","Avalanche","Three leaping slams, each wider, the last the heaviest",8,34,10,DamageType.Kinetic,6),
            (8,SkillKind.HoldTheLine,"Hold the Line","Hold","Foes turn to you for 6 s and your Plating rises. Bosses don't",12,22,16,DamageType.Kinetic,7),
            // Sync 16, 20 and 24, as the first two heroes' late three (SkillsLate): their icons are the second sheet's after her EXTRA's.
            (9,SkillKind.TakeTheWeight,"Take the Weight","Weight","For 6 s, hits meant for your partner land on you instead",16,26,18,DamageType.Kinetic,1),
            (10,SkillKind.CuttingTorch,"Cutting Torch","Torch","A held Plasma cut with heavy Break. Plates don't stop it",20,30,10,DamageType.Plasma,2),
            (11,SkillKind.Bulkhead,"Bulkhead","Bulkhead","A 6 s wall of light that stops enemy shots and holds foes back",24,36,20,DamageType.Kinetic,3),
        };
        public static void Definitions()
        {
            var made=Skills.Select(s=>ArenaBuilder.Asset<SkillDef>(Hero+"_"+s.number,d=>
            {
                d.id=Hero+"_"+s.number;d.displayName=s.name;d.shortName=s.label;d.description=s.says;d.kind=s.kind;d.unlockLevel=s.level;
                d.chargeCost=s.cost;d.cooldown=s.cooldown;d.damageType=s.type;d.ground=d.flight=true;d.iconSheet=s.number>8?ExtraSheet:Sheet;d.iconCell=s.cell;
            })).ToArray();
            // An EXTRA costs its gauge, not Charge, and has no cooldown: the definition is its name, its words and its icon.
            var extra=ArenaBuilder.Asset<SkillDef>(Hero+"_Extra",d=>
            {
                d.id=Hero+"_Extra";d.displayName=d.shortName="Last One Out";d.description="Untouchable: a breach, a shockwave, and your partner is raised";d.kind=SkillKind.LastOneOut;d.unlockLevel=1;
                d.chargeCost=0;d.cooldown=0;d.damageType=DamageType.Kinetic;d.ground=d.flight=true;d.iconSheet=ExtraSheet;d.iconCell=0;
            });
            var lender=AssetDatabase.LoadAssetAtPath<CharacterDef>(Root+"CharacterDef/"+Lender+".asset");
            ArenaBuilder.Asset<CharacterDef>(Hero,c=>
            {
                c.id=c.speakerId=Hero;c.displayName="Idra Sorn";c.female=true;
                // The sturdy one. Output as the others' (her hammer's weight is in its numbers, not her stats); Plating from 16 by 3 a level where theirs is from 10 by 2
                // (at Sync 16 a bare 61 to their 40: a hit that takes 77% of itself from Taren takes 66% from her); Response from 8 by half a point where theirs is 10 by 1.
                c.baseStats=new Stats{output=50,plating=16,response=8,resonance=10,fortune=5};
                c.growthPerLevel=new Stats{output=5,plating=3,response=.5f,resonance=2,fortune=1};
                // Ember: redder and hotter than Taren's amber (1, .65, .12), so the two warm heroes are still told apart on a bar or a ring.
                c.syncHue=new Color(1,.38f,.1f);
                // Her own bodies are IdraNatural, IdraBraced and IdraFlight (art intake sets them); until each lands she stands in the lender's.
                if(lender!=null)
                {
                    if(c.natural==null)c.natural=lender.natural;if(c.shaped==null)c.shaped=lender.shaped;if(c.flight==null)c.flight=lender.flight;
                }
                c.skills=made.Take(4).ToArray();c.learnable=made.Skip(4).ToArray();c.extra=extra;
                // She joins in chapter 6, where the party wears Prime (tier 3): the edge her hammer strikes with, a frame and a drive. The rest of the chapter's floor (a Refined Emitter,
                // a Prime Module) comes with the rule every (re)joining hero meets (Lattice.Rpg.PartyFloor, D329), not from this list.
                c.startingGear=new[]{"EdgesT3","FramesT3","DrivesT3"}.Select(id=>AssetDatabase.LoadAssetAtPath<TechPartDef>(Root+"TechPartDef/"+id+".asset")).Where(part=>part!=null).ToArray();
                c.style=new HeroStyle
                {
                    attack=AttackStyle.Hammer,
                    // Brace: 85% stopped where a guard stops 65%, and 150 Thrust for a whole bar stopped where a guard pays 250. A hit of a fifth of a bar costs her 25.5
                    // Thrust and leaves 3% of the bar; the same hit costs Taren 32.5 and leaves 7%.
                    guardTakes=.15f,guardCost=150,
                    // The prow: the only guard a ship has, over the 120 degrees ahead of it.
                    flightGuard=true,prowArc=120,
                    // A Flash Guard's shockwave: 2x Output all round her out to 4 m, Break 70 (Taren's finishing cut is 1.5x and 30).
                    counter=2,counterBreak=70,counterReach=4,
                    // A short hop: 2.2 m in .22 s, untouchable for the first .12 s of it (the others roll 3.3 m in .25 s, untouchable throughout). Her ship's roll the same way:
                    // 2.8 m, and .14 s of its .3 s.
                    hop=2.2f,hopSeconds=.22f,hopSafe=.12f,roll=2.8f,rollSeconds=.3f,rollSafe=.14f,
                    // Her swings are .5 to .72 s long where his are .32 to .48: the same half second of grace after one to press the next.
                    chainWindow=1.1f,
                    partnerGuards=true,
                    // Thrusters, a dash's edge and her form change in the warm palette: she's an ember, and cyan is Sela's.
                    warm=true,
                    bodiesFrom=Lender,bodyTint=new Color(1,.66f,.5f),
                };
            });
        }
    }
}
