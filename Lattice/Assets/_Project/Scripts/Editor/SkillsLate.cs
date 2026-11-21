using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>S3: the definitions of the six skills learned at Sync 16, 20 and 24 and of the two EXTRA moves, each hero's <c>learnable</c> list and <c>extra</c>, and the
    /// barks copied to where the game reads them. It can be run again at any time: every asset is found by its id and written over, and a hero's list is its first four
    /// learned skills followed by these three, whatever it held before.
    /// <code>Unity -batchmode -quit -projectPath Lattice -executeMethod Lattice.EditorTools.SkillsLate.Build</code></summary>
    public static class SkillsLate
    {
        /// <summary>The generated sheet (tools/meshy/s3_skill_icons.py): Taren on top, Sela below, the fourth of each row the hero's EXTRA.</summary>
        public const string Sheet="skill-icons-3";
        public static void Build()=>BatchTools.Run(()=>
        {
            Definitions();
            // The lines these need (a shout for each EXTRA, the lesson) live with the rest of the barks, which the game reads from Resources.
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            File.Copy(Path.Combine(root,"docs/writing/barks.json"),"Assets/_Project/Resources/Audio/barks.json",true);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            // The sheet as the other two are imported (UiArtImporter): whole, clamped and uncompressed. Left as Unity first found it, its cells bleed into each other and blur.
            var importer=AssetImporter.GetAtPath("Assets/_Project/Resources/UI/Generated/"+Sheet+".png") as TextureImporter;
            if(importer==null)throw new System.Exception("the icon sheet "+Sheet+" isn't in Resources/UI/Generated: run tools/meshy/s3_skill_icons.py --compose 1");
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=4096;importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            foreach(string hero in new[]{"Taren","Sela"})
            {
                var def=GameCatalog.Find<CharacterDef>(hero);
                if(def.learnable.Length!=7||def.learnable.Any(s=>s==null)||def.extra==null)throw new System.Exception(hero+"'s late skills didn't take");
            }
            Debug.Log("SKILLS_LATE_OK");
        });
        /// <summary>(asset id, kind, name, the HUD's short label, what the row says, Sync level, Charge, cooldown, damage type, icon cell).</summary>
        static readonly (string hero,int number,SkillKind kind,string name,string label,string says,int level,float cost,float cooldown,DamageType type,int cell)[] Skills=
        {
            ("Taren",9,SkillKind.BulwarkRush,"Bulwark Rush","Bulwark","Ram ahead behind your plating: hits on the way barely land",16,26,8,DamageType.Kinetic,0),
            ("Taren",10,SkillKind.CinderBrand,"Cinder Brand","Brand","Brand a foe for 8 s: it takes 20% more from everyone",20,22,12,DamageType.Plasma,1),
            ("Taren",11,SkillKind.Severance,"Severance","Sever","A slow, huge cut. Harder on Broken foes; a kill refunds half its cost",24,40,12,DamageType.Kinetic,2),
            ("Sela",9,SkillKind.MendLattice,"Mend Lattice","Mend","Lay a lattice that mends you both for 5 s. It can't revive",16,35,22,DamageType.Pulse,4),
            ("Sela",10,SkillKind.PrismRelay,"Prism Relay","Relay","A beam that jumps between up to five foes",20,26,7,DamageType.Beam,5),
            ("Sela",11,SkillKind.LongExposure,"Long Exposure","Exposure","Hold still for a 2.5 s piercing beam; a dodge or roll ends it",24,40,14,DamageType.Beam,6),
        };
        static readonly (string hero,SkillKind kind,string name,string says,DamageType type,int cell)[] Extras=
        {
            // One line of the Skills page's feedback each, with the move's name before it.
            ("Taren",SkillKind.AnvilChorus,"Anvil Chorus","Untouchable: eight blows, then one that breaks almost anything",DamageType.Kinetic,3),
            ("Sela",SkillKind.LatticeAria,"Lattice Aria","Untouchable: beams on every foe in sight, then you're both mended",DamageType.Beam,7),
        };
        public static void Definitions()
        {
            foreach(string hero in new[]{"Taren","Sela"})
            {
                var late=Skills.Where(s=>s.hero==hero).Select(s=>ArenaBuilder.Asset<SkillDef>(hero+"_"+s.number,d=>
                {
                    d.id=hero+"_"+s.number;d.displayName=s.name;d.shortName=s.label;d.description=s.says;d.kind=s.kind;d.unlockLevel=s.level;
                    d.chargeCost=s.cost;d.cooldown=s.cooldown;d.damageType=s.type;d.ground=d.flight=true;d.iconSheet=Sheet;d.iconCell=s.cell;
                })).ToArray();
                var move=Extras.First(e=>e.hero==hero);
                // An EXTRA costs its gauge, not Charge, and has no cooldown: the definition is its name, its words and its icon.
                var extra=ArenaBuilder.Asset<SkillDef>(hero+"_Extra",d=>
                {
                    d.id=hero+"_Extra";d.displayName=d.shortName=move.name;d.description=move.says;d.kind=move.kind;d.unlockLevel=1;
                    d.chargeCost=0;d.cooldown=0;d.damageType=move.type;d.ground=d.flight=true;d.iconSheet=Sheet;d.iconCell=move.cell;
                });
                ArenaBuilder.Asset<CharacterDef>(hero,c=>
                {
                    // The four learned at Sync 3 to 12 stay as they are and in their order; these follow them.
                    var first=(c.learnable??new SkillDef[0]).Where(s=>s!=null&&s.kind<SkillKind.BulwarkRush).ToArray();
                    c.learnable=first.Concat(late).ToArray();c.extra=extra;
                });
            }
        }
    }
}
