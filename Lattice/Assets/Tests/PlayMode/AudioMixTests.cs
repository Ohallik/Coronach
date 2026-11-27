using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Records the listener's mix: the loudest sample and how many blocks the audio thread delivered.</summary>
    sealed class MixTap:MonoBehaviour
    {
        volatile bool armed;float peak;int blocks;readonly object gate=new();
        public void Arm(){lock(gate){peak=0;blocks=0;}armed=true;}
        public (float peak,int blocks) Disarm(){armed=false;lock(gate)return(peak,blocks);}
        void OnAudioFilterRead(float[] data,int channels)
        {
            if(!armed)return;float loudest=0;foreach(float x in data)loudest=Mathf.Max(loudest,Mathf.Abs(x));
            lock(gate){peak=Mathf.Max(peak,loudest);blocks++;}
        }
    }

    /// <summary>Round ten (November 27): the game's mix as it reaches the listener with every slider at 1. Nathan heard captured play and
    /// no sound effects: a hit peaked about -25 dBFS under music at -22 RMS. Each case first plays the state as it was
    /// (CueLevels.AsRoundNine: the clips' old levels, gain 1, every voice heard from the lens, the beeping room hum) and requires it to fail,
    /// then the game as it is. The sheet each prints is the measure: a clip's own peak (AudioClip.GetData) times the source volume
    /// AudioManager gives it, times the spatial part at the distance the voice is heard from, times the buses (1). Whether the engine sums
    /// it the same way is <see cref="TheEngineSumsWhatTheSheetReckons"/>'s to say, from the listener's own output.</summary>
    public sealed class AudioMixTests
    {
        [UnitySetUp] public IEnumerator Boot()
        {
            CueLevels.AsRoundNine=false;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.5f);DevLoadout.Apply("starter");
            SceneFlow.Current.LoadZone("Arena_Ground");float deadline=Time.unscaledTime+8;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading);
            foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members)
            {actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            yield return new WaitForSecondsRealtime(.5f);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            CueLevels.AsRoundNine=false;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            yield return new WaitForSecondsRealtime(.3f);
        }

        static float Db(double x)=>x<=1e-9?-120:(float)(20*Math.Log10(x));
        static readonly Dictionary<string,float[]> data=new();
        static IEnumerator Load(AudioClip clip)
        {
            if(data.ContainsKey(clip.name+clip.GetInstanceID()))yield break;
            clip.LoadAudioData();float deadline=Time.realtimeSinceStartup+10;
            while(clip.loadState==AudioDataLoadState.Loading&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual(AudioDataLoadState.Loaded,clip.loadState,clip.name+" never loaded");
            var samples=new float[clip.samples*clip.channels];Assert.IsTrue(clip.GetData(samples,0),clip.name+": no sample data");
            data[clip.name+clip.GetInstanceID()]=samples;
        }
        static float[] Samples(AudioClip clip)=>data[clip.name+clip.GetInstanceID()];
        static float Peak(AudioClip clip){float peak=0;foreach(float x in Samples(clip))peak=Mathf.Max(peak,Mathf.Abs(x));return peak;}
        /// <summary>The louder channel's RMS: what one speaker plays.</summary>
        static float ChannelRms(AudioClip clip)
        {
            var s=Samples(clip);int c=clip.channels;double best=0;
            for(int ch=0;ch<c;ch++){double sum=0;for(int i=ch;i<s.Length;i+=c)sum+=s[i]*(double)s[i];best=Math.Max(best,sum/(s.Length/c));}
            return (float)Math.Sqrt(best);
        }
        /// <summary>25 ms windows standing 8 dB or more over the clip's median window: a drip, a clank, a beep's onset.</summary>
        static int Events(AudioClip clip)
        {
            var s=Samples(clip);int c=clip.channels,w=Mathf.Max(1,clip.frequency/40)*c;var env=new List<float>();
            for(int i=0;i+w<=s.Length;i+=w){double sum=0;for(int j=i;j<i+w;j++)sum+=s[j]*(double)s[j];env.Add(Db(Math.Sqrt(sum/w)));}
            var sorted=env.OrderBy(x=>x).ToList();float floor=sorted[sorted.Count/2];int events=0;bool hot=false;
            foreach(float e in env){bool now=e>floor+8;if(now&&!hot)events++;hot=now;}
            return events;
        }
        /// <summary>Unity's sum for one of AudioManager's voices: blend .35, so .65 of it is heard flat and .35 through a logarithmic rolloff
        /// from 8 m (min distance over distance, held past 80).</summary>
        static float Spatial(float distance)=>.65f+.35f*8f/Mathf.Clamp(distance,8,80);
        /// <summary>A mono voice reaches each channel at -3 dB (CueLevels.PanLawDb, read back by the witness below); a stereo one goes as it is.</summary>
        static float Pan(AudioClip clip)=>clip.channels==1?Mathf.Pow(10,CueLevels.PanLawDb/20):1;
        static (float low,float high) Band(CueLevels.Kind kind)=>kind switch
        {
            CueLevels.Kind.Impact=>(-9,-5),CueLevels.Kind.Motion=>(-14,-10),CueLevels.Kind.Step=>(-20,-16),
            CueLevels.Kind.UiMove=>(-17,-15),CueLevels.Kind.UiConfirm=>(-13,-11),_=>(-16,-6),
        };
        static IEnumerable<AudioClip> OneShots=>Resources.LoadAll<AudioClip>("Audio/Palette").Concat(Resources.LoadAll<AudioClip>("Audio/SFX"));
        static List<AudioClip> Of(CueLevels.Family family)=>OneShots.Where(c=>CueLevels.TryFamily(c.name,out var f)&&f.name==family.name).ToList();
        static float MusicLevel=>(float)typeof(MusicDirector).GetField("Level",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).GetRawConstantValue();

        // ------------------------------------------------------------------ one reference on disk
        [UnityTest] public IEnumerator EveryOneShotSitsAtTheReference()
        {
            // As it was: the palette at -10 and the made clips from -1.6 to -14 (the families' recorded old peaks).
            var before=CueLevels.Families.Where(f=>Mathf.Abs(f.oldPeakDb-CueLevels.ReferencePeakDb)>1).Select(f=>f.name).ToList();
            Assert.Greater(before.Count,30,"fixture: the old levels sit on the reference, so this case can't tell");
            var off=new List<string>();var sheet=new StringBuilder("AUDIO_CLIP_SHEET (sample peak dBFS on import)\n");
            foreach(var clip in OneShots)
            {
                yield return Load(clip);float peak=Db(Peak(clip));sheet.AppendLine($"  {clip.name,-22} {peak,6:0.0}  {clip.channels} channel(s)");
                if(Mathf.Abs(peak-CueLevels.ReferencePeakDb)>1)off.Add($"{clip.name} {peak:0.0}");
                if(clip.channels!=1)off.Add(clip.name+" is not mono: the table's pan law doesn't hold for it");
            }
            Debug.Log(sheet);
            Assert.IsEmpty(off,"off the reference of "+CueLevels.ReferencePeakDb+" dBFS: "+string.Join(", ",off));
        }

        // ------------------------------------------------------------------ one table
        static readonly Regex Call=new(@"(AudioManager\.(?:Play|PlayUi|PlayAt|Family|StartLoop|Sample)|CombatAudio\.Accent)\(");
        static string FirstArgument(string text,int open)
        {
            int depth=0;bool quoted=false;
            for(int i=open;i<text.Length;i++)
            {
                char ch=text[i];if(ch=='"')quoted=!quoted;if(quoted)continue;
                if(ch=='('||ch=='[')depth++;else if(ch==')'||ch==']'){if(depth==0)return text.Substring(open,i-open);depth--;}
                else if(ch==','&&depth==0)return text.Substring(open,i-open);
            }
            return "";
        }
        [UnityTest] public IEnumerator EveryClipHasAFamilyAndNoCallSiteIsOutsideTheTable()
        {
            yield return null;
            var clips=OneShots.Select(c=>c.name).ToList();
            var orphans=clips.Where(c=>!CueLevels.TryFamily(c,out _)).ToList();
            Assert.IsEmpty(orphans,"clips no family's level covers: "+string.Join(", ",orphans));
            var empty=CueLevels.Families.Where(f=>!clips.Any(c=>CueLevels.TryFamily(c,out var g)&&g.name==f.name)).Select(f=>f.name).ToList();
            Assert.IsEmpty(empty,"families with no clip on disk: "+string.Join(", ",empty));
            // Every key a call names in the scripts is a clip with a family, and nothing plays a sound past AudioManager.
            var scripts=Path.Combine(Application.dataPath,"_Project","Scripts");var outside=new List<string>();var unknown=new List<string>();int calls=0;
            var own=new HashSet<string>{"AudioManager.cs","MusicDirector.cs","AudioMixProbe.cs","CueLevels.cs"};
            foreach(var file in Directory.GetFiles(scripts,"*.cs",SearchOption.AllDirectories))
            {
                if(file.Replace('\\','/').Contains("/Editor/"))continue;
                var text=File.ReadAllText(file);var name=Path.GetFileName(file);
                if(!own.Contains(name)&&Regex.IsMatch(text,@"\.PlayOneShot\(|AddComponent<AudioSource>|typeof\(AudioSource\)|PlayClipAtPoint"))outside.Add(name);
                foreach(Match m in Call.Matches(text))
                {
                    calls++;
                    string argument=FirstArgument(text,m.Index+m.Length);
                    foreach(Match key in Regex.Matches(argument,@"""([^""]*)"""))
                    {
                        string k=key.Groups[1].Value,before=argument.Substring(0,key.Index).TrimEnd();
                        if(k.EndsWith("_")||before.EndsWith("==")||before.EndsWith("!="))continue; // a family's stem, completed by Family(); a name compared, not played
                        if(!CueLevels.TryFamily(k,out _)&&!CueLevels.TryFamily(k+"_0",out _))unknown.Add(name+": "+k);
                    }
                }
            }
            Debug.Log($"AUDIO_CALL_SITES {calls} calls read");
            Assert.Greater(calls,200,"fixture: the scan found too few calls to mean anything");
            Assert.IsEmpty(outside,"scripts that play sound past AudioManager's table: "+string.Join(", ",outside));
            Assert.IsEmpty(unknown,"keys with no family: "+string.Join(", ",unknown));
        }

        // ------------------------------------------------------------------ the sheet: each family at the listener
        IEnumerator Sheet(bool before,List<string> outside,Action<float,float> read)
        {
            CueLevels.AsRoundNine=before;yield return null;yield return null;
            var hero=PartyController.Current.Active;var point=hero.transform.position+Vector3.up*.9f;
            float distance=Vector3.Distance(before?Camera.main.transform.position:AudioManager.Current.Ears,point);
            float music=Db(MusicLevel)+CueLevels.MusicFileRmsDb;
            var sheet=new StringBuilder($"AUDIO_MIX_SHEET {(before?"as round nine left it":"round ten")}: party heard from {distance:0.0} m (spatial {Db(Spatial(distance)):0.0} dB; a boss's framing at 45 m would be {Db(Spatial(45)):0.0}), music RMS {music:0.0}\n");
            if(music<CueLevels.MusicLowDb||music>CueLevels.MusicHighDb)outside.Add($"music {music:0.0}");
            float hit=0;
            foreach(var family in CueLevels.Families)
            {
                var clips=Of(family);foreach(var clip in clips)yield return Load(clip);
                float peak=clips.Max(Peak);bool flat=family.kind==CueLevels.Kind.UiMove||family.kind==CueLevels.Kind.UiConfirm;
                float spatial=(flat?1:Spatial(distance))*Pan(clips[0]);var (low,high)=Band(family.kind);
                foreach(float call in family.kind==CueLevels.Kind.Step?new[]{.15f,family.nominal,.22f}:new[]{family.nominal})
                {
                    float level=Db(peak*CueLevels.Volume(clips[0].name,call)*spatial);bool inside=level>=low-.05f&&level<=high+.05f;
                    sheet.AppendLine($"  {family.name,-20} {family.kind,-9} call {call:0.00} -> source {CueLevels.Volume(clips[0].name,call):0.000}  peak at the ears {level,6:0.0}  band {low} to {high}{(inside?"":"  OUTSIDE")}");
                    if(!inside)outside.Add($"{family.name}@{call:0.00} {level:0.0}");
                    if(family.name=="hit_soft"&&call==family.nominal)hit=level;
                }
            }
            sheet.AppendLine($"  a soft hit stands {hit-music:0.0} dB over the music's RMS a channel");
            Debug.Log(sheet);TestContext.WriteLine(sheet.ToString());read(hit,music);
        }
        [UnityTest] public IEnumerator EachFamilyReachesTheListenerInsideItsBand()
        {
            var outside=new List<string>();float hit=0,music=0;
            yield return Sheet(true,outside,(h,m)=>{hit=h;music=m;});
            Assert.IsNotEmpty(outside,"fixture: round nine's mix reads inside the bands, so this case can't tell");
            Assert.Less(hit-music,10,"fixture: round nine's soft hit already stood over the music");
            outside.Clear();
            yield return Sheet(false,outside,(h,m)=>{hit=h;music=m;});
            Assert.IsEmpty(outside,"outside their bands at the ears: "+string.Join(", ",outside));
            // Reckoned, not a fight: a soft hit some 16 dB over a channel of a track of median loudness (-18.6); the loudest (-16.8) takes 2 dB of it.
            Assert.GreaterOrEqual(hit-music,10,"a soft hit doesn't stand clear of the music");
        }

        // ------------------------------------------------------------------ the engine's own sum
        [UnityTest] public IEnumerator TheEngineSumsWhatTheSheetReckons()
        {
            foreach(var bus in new[]{AudioBus.Music,AudioBus.Ambience,AudioBus.UI})AudioMix.Current.SetLevel(bus,0,false);
            var clip=Resources.Load<AudioClip>("Audio/Palette/hit_soft_0");yield return Load(clip);
            var tap=Camera.main.gameObject.AddComponent<MixTap>();var read=new List<string>();
            try
            {
                foreach(bool before in new[]{true,false})
                {
                    CueLevels.AsRoundNine=before;yield return new WaitForSecondsRealtime(.4f);
                    var hero=PartyController.Current.Active;var point=hero.transform.position+Vector3.up*.9f;
                    float distance=Vector3.Distance(before?Camera.main.transform.position:AudioManager.Current.Ears,point);
                    float reckoned=Db(Peak(clip)*CueLevels.Volume(clip.name,.22f)*Spatial(distance)*Pan(clip));
                    tap.Arm();AudioManager.PlayAt(clip.name,hero.transform,point,.22f,AudioBus.SFX,90,0);
                    yield return new WaitForSecondsRealtime(.8f);var (peak,blocks)=tap.Disarm();
                    if(blocks==0)Assert.Inconclusive("the listener's filter never ran: no audio output in this run, so the sheet stands unchecked");
                    read.Add($"{(before?"round nine":"round ten")}: {distance:0.0} m, reckoned {reckoned:0.0}, heard {Db(peak):0.0} dBFS over {blocks} blocks");
                    Debug.Log("AUDIO_DSP_WITNESS "+read[^1]);
                    Assert.AreEqual(reckoned,Db(peak),2f,"the engine's sum isn't the sheet's: "+string.Join("; ",read));
                    if(before)Assert.Less(Db(peak),-9,"fixture: round nine's hit was already loud");
                    else Assert.GreaterOrEqual(Db(peak),-9.5f,"a soft hit is still under the impacts' band at the ears");
                }
            }
            finally{UnityEngine.Object.Destroy(tap);}
        }

        // ------------------------------------------------------------------ every place's air
        static readonly string[] Metronomes={"computerNoise_000"};
        IEnumerator Air(bool before,List<string> faults,StringBuilder sheet)
        {
            CueLevels.AsRoundNine=before;
            foreach(var zone in Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").OrderBy(z=>z.name))
            {
                bool flight=zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe;
                // What AudioManager.Zone plays there.
                AudioManager.Zone(flight,zone.ambientAudio,zone.ambientVolume);yield return null;
                var bed=GameServices.Current.GetComponents<AudioSource>().Single(s=>s.loop&&s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name=="Ambience");
                if(bed.clip==null){faults.Add(zone.name+" is silent");continue;}
                yield return Load(bed.clip);
                float level=Db(ChannelRms(bed.clip)*bed.volume*Pan(bed.clip));int events=Events(bed.clip);float length=bed.clip.length;
                sheet.AppendLine($"  {zone.name,-20} {(flight?"flight":"ground"),-6} {bed.clip.name,-26} {length,5:0.0} s  volume {bed.volume:0.000}  RMS a channel {level,6:0.0}  events {events}");
                if(Metronomes.Contains(bed.clip.name))faults.Add(zone.name+" loops "+bed.clip.name);
                if(length<20)faults.Add($"{zone.name}: {bed.clip.name} loops every {length:0.0} s");
                if(events>0&&length<30)faults.Add($"{zone.name}: {bed.clip.name} brings its {events} events round every {length:0.0} s");
                if(level<CueLevels.BedLowDb-.05f||level>CueLevels.BedHighDb+.05f)faults.Add($"{zone.name}: {bed.clip.name} at {level:0.0}");
            }
        }
        [UnityTest] public IEnumerator EveryPlaceHasAirAndNoneIsAMetronome()
        {
            var faults=new List<string>();var sheet=new StringBuilder("AUDIO_BED_SHEET as round nine left it\n");
            yield return Air(true,faults,sheet);Debug.Log(sheet);
            Assert.IsTrue(faults.Any(f=>f.Contains("computerNoise_000")),"fixture: round nine's beeping hum isn't found: "+string.Join("; ",faults));
            faults.Clear();sheet=new StringBuilder("AUDIO_BED_SHEET round ten\n");
            yield return Air(false,faults,sheet);Debug.Log(sheet);TestContext.WriteLine(sheet.ToString());
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
    }
}
