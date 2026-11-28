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
            CueLevels.AsRoundNine=CueLevels.BedsAsRoundTen=CueLevels.CuesAsRoundTen=false;GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
            AudioManager.CueStarted-=Started;AudioManager.CueStarted+=Started;started.Clear();
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
            CueLevels.AsRoundNine=CueLevels.BedsAsRoundTen=CueLevels.CuesAsRoundTen=false;AudioManager.CueStarted-=Started;
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;
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
        static readonly Regex Call=new(@"(AudioManager\.(?:Play|PlayUi|PlayAt|PlayThrough|Family|StartLoop|Sample)|CombatAudio\.Accent)\(");
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
        /// <summary>One RBJ biquad (Butterworth, Q .7071), in place: tools' measure (r11sound/measure.py) splits the bands with the same two
        /// stages a band edge (scipy's lfilter), so the sheet here and the one in the round's decision read alike.</summary>
        static void Biquad(double[] x,bool high,float hz,int rate)
        {
            double w=2*Math.PI*hz/rate,c=Math.Cos(w),q=Math.Sin(w)/(2*.70710678),a0=1+q;
            double b0=(high?(1+c)/2:(1-c)/2)/a0,b1=(high?-(1+c):1-c)/a0,b2=b0,a1=-2*c/a0,a2=(1-q)/a0,x1=0,x2=0,y1=0,y2=0;
            for(int i=0;i<x.Length;i++){double y=b0*x[i]+b1*x1+b2*x2-a1*y1-a2*y2;x2=x1;x1=x[i];y2=y1;y1=y;x[i]=y;}
        }
        /// <summary>The share of a clip's energy (both channels) that a cascade of edges keeps: each (high-pass?, Hz) applied twice.</summary>
        static float Share(AudioClip clip,params (bool high,float hz)[] edges)
        {
            var s=Samples(clip);int c=clip.channels,n=s.Length/c;double kept=0,total=0;
            for(int ch=0;ch<c;ch++)
            {
                var x=new double[n];for(int i=0;i<n;i++){x[i]=s[i*c+ch];total+=x[i]*x[i];}
                foreach(var (high,hz) in edges){Biquad(x,high,hz,clip.frequency);Biquad(x,high,hz,clip.frequency);}
                foreach(double v in x)kept+=v*v;
            }
            return (float)(kept/Math.Max(total,1e-20));
        }
        static readonly Dictionary<string,(float presence,float top)> shares=new();
        static (float presence,float top) Shares(AudioClip clip)
        {
            if(!shares.TryGetValue(clip.name,out var read))
                shares[clip.name]=read=(Share(clip,(true,CueLevels.PresenceLowHz),(false,CueLevels.PresenceHighHz)),Share(clip,(true,CueLevels.FallbackTopHz)));
            return read;
        }
        static bool Fallback(AudioClip clip)=>clip.name==CueLevels.RoomTone||clip.name==CueLevels.FlightDrone;
        /// <summary>Every zone's bed as AudioManager.Zone plays it: round nine (<paramref name="before"/>), round ten (<paramref name="morning"/>:
        /// CueLevels.BedsAsRoundTen) or as it is. A bed's RMS a channel at the ears, less its own cut, lies in CueLevels' band, a fallback bed at
        /// -46; it puts no more than a tenth of its energy into the music's presence (1-6 kHz) at more than -48 there; it never peaks over -20.</summary>
        IEnumerator Air(bool before,bool morning,List<string> faults,StringBuilder sheet)
        {
            CueLevels.AsRoundNine=before;CueLevels.BedsAsRoundTen=morning;
            foreach(var zone in Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").OrderBy(z=>z.name))
            {
                bool flight=zone.kind==ZoneKind.SpaceCombat||zone.kind==ZoneKind.SpaceSafe;
                // What AudioManager.Zone plays there.
                AudioManager.Zone(flight,zone.ambientAudio,zone.ambientVolume);yield return null;
                var bed=GameServices.Current.GetComponents<AudioSource>().Single(s=>s.loop&&s.outputAudioMixerGroup!=null&&s.outputAudioMixerGroup.name=="Ambience");
                if(bed.clip==null){faults.Add(zone.name+" is silent");continue;}
                yield return Load(bed.clip);
                float level=Db(ChannelRms(bed.clip)*bed.volume*Pan(bed.clip)),peak=Db(Peak(bed.clip)*bed.volume*Pan(bed.clip));int events=Events(bed.clip);float length=bed.clip.length;
                var (share,top)=before?(0,0):Shares(bed.clip);float presence=level+10*Mathf.Log10(Mathf.Max(share,1e-12f)),cut=CueLevels.BedCutDb(bed.clip.name);
                sheet.AppendLine($"  {zone.name,-20} {(flight?"flight":"ground"),-6} {bed.clip.name,-26} {length,5:0.0} s  volume {bed.volume:0.000}  RMS a channel {level,6:0.0} (own cut {cut,5:0.0})  1-6 kHz {share,4:0.00} of it, {presence,6:0.0}  peak {peak,6:0.0}  over 800 Hz {top:0.000}  events {events}");
                if(Metronomes.Contains(bed.clip.name))faults.Add(zone.name+" loops "+bed.clip.name);
                if(length<20)faults.Add($"{zone.name}: {bed.clip.name} loops every {length:0.0} s");
                if(events>0&&length<30)faults.Add($"{zone.name}: {bed.clip.name} brings its {events} events round every {length:0.0} s");
                if(before)continue;
                if(Fallback(bed.clip)?Mathf.Abs(level-CueLevels.FallbackBedDb)>1:level-cut<CueLevels.BedLowDb-.05f||level-cut>CueLevels.BedHighDb+.05f)
                    faults.Add($"{zone.name}: {bed.clip.name} at {level:0.0}{(cut!=0?$" (its own cut {cut})":"")}");
                if(share>CueLevels.BedPresenceShare&&presence>CueLevels.BedPresenceDb)faults.Add($"{zone.name}: {bed.clip.name} puts {share:0.00} of itself at 1-6 kHz, {presence:0.0} there");
                if(peak>CueLevels.BedPeakDb)faults.Add($"{zone.name}: {bed.clip.name} peaks at {peak:0.0}");
                if(Fallback(bed.clip)&&top>.01f)faults.Add($"{zone.name}: {bed.clip.name} has {top:0.000} of its energy over {CueLevels.FallbackTopHz} Hz");
            }
        }
        [UnityTest] public IEnumerator EveryPlaceHasAirAndNoneIsAMetronome()
        {
            var faults=new List<string>();var sheet=new StringBuilder("AUDIO_BED_SHEET as round nine left it\n");
            yield return Air(true,false,faults,sheet);Debug.Log(sheet);
            Assert.IsTrue(faults.Any(f=>f.Contains("computerNoise_000")),"fixture: round nine's beeping hum isn't found: "+string.Join("; ",faults));
            faults.Clear();sheet=new StringBuilder("AUDIO_BED_SHEET as it is\n");
            yield return Air(false,false,faults,sheet);Debug.Log(sheet);TestContext.WriteLine(sheet.ToString());
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }
        /// <summary>Round eleven (November 27, night): Nathan heard round ten's beds as "constant background kind of air noise" that made the music
        /// seem muffled. As round ten left them every bed stood at -32 to -28 RMS a channel, six decibels under the music; Hushwell's drips put 40%
        /// of their energy at 1-6 kHz and peaked at -1.3 at the ears, over every effect. The measure first plays round ten's levels and requires the
        /// faults it was built for (every bed over the band, the drips over the presence bar and the peak), then the beds as they are.</summary>
        [UnityTest] public IEnumerator NoBedCoversTheMusic()
        {
            var faults=new List<string>();var sheet=new StringBuilder("AUDIO_BED_SHEET as round ten left it\n");
            yield return Air(false,true,faults,sheet);Debug.Log(sheet);TestContext.WriteLine(sheet.ToString());
            Assert.Greater(faults.Count(f=>f.Contains(" at -")),40,"fixture: round ten's beds already sit in the new band: "+string.Join("; ",faults));
            Assert.IsTrue(faults.Any(f=>f.Contains("AMB_Drips puts")),"fixture: round ten's drips aren't found over the music's presence: "+string.Join("; ",faults));
            Assert.IsTrue(faults.Any(f=>f.Contains("AMB_Drips peaks")),"fixture: round ten's drips aren't found peaking over a step: "+string.Join("; ",faults));
            // The fallback beds' top: the measure can tell a bed with one (the drips, most of whose energy is over 800 Hz) from one without.
            var drips=Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").Single(z=>z.name=="Hushwell").ambientAudio;yield return Load(drips);
            Assert.Greater(Shares(drips).top,.1f,"fixture: the drips read as having nothing over 800 Hz, so the top's measure can't tell");
            faults.Clear();sheet=new StringBuilder("AUDIO_BED_SHEET round eleven\n");
            yield return Air(false,false,faults,sheet);Debug.Log(sheet);TestContext.WriteLine(sheet.ToString());
            Assert.IsEmpty(faults,string.Join("\n",faults));
        }

        // ------------------------------------------------------------------ round eleven: what round ten left silent
        static readonly List<string> started=new();
        static void Started(string key,Transform _)=>started.Add(key);
        static List<string> Heard(){started.Clear();return started;}
        /// <summary>A warp: the zone's load cut every sound playing when its scene unloaded, so a warp's crossing was never heard. It is now a cue
        /// that outlives the unload (AudioManager.PlayThrough), heard as the old zone goes. As it was (CueLevels.CuesAsRoundTen) nothing plays.</summary>
        [UnityTest] public IEnumerator AWarpsCrossingIsHeardThroughTheLoad()
        {
            foreach(bool before in new[]{true,false})
            {
                CueLevels.CuesAsRoundTen=before;
                var beacon=new GameObject("A warp for the test").AddComponent<Lattice.World.WarpBeacon>();beacon.requiredFlag="";beacon.scene="Arena_Ground";
                bool heardAtUnload=false;
                void Unloaded(Scene _)=>heardAtUnload|=AudioManager.Current.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying&&s.clip!=null&&s.clip.name=="forceField_003");
                SceneManager.sceneUnloaded+=Unloaded;
                try
                {
                    beacon.Interact();yield return null;float deadline=Time.unscaledTime+20;
                    while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
                }
                finally{SceneManager.sceneUnloaded-=Unloaded;}
                Assert.IsFalse(SceneFlow.Current.Loading,"fixture: the warp's load never finished");
                Debug.Log($"AUDIO_WARP {(before?"as round ten":"round eleven")}: the crossing heard as the old zone unloaded: {heardAtUnload}");
                if(before)Assert.IsFalse(heardAtUnload,"fixture: as round ten left it, something of the warp was heard through the load");
                else Assert.IsTrue(heardAtUnload,"a warp's crossing is cut by the load: nothing is heard as the old zone goes");
                yield return new WaitForSecondsRealtime(.3f);
            }
        }
        /// <summary>The fabrication bench opened and closed in silence (D453). It opens and closes as a hatch does, at a menu's confirm.</summary>
        [UnityTest] public IEnumerator TheBenchOpensAndClosesWithASound()
        {
            var menu=UnityEngine.Object.FindFirstObjectByType<PauseMenu>();Assert.IsNotNull(menu,"fixture: no pause menu");
            foreach(bool before in new[]{true,false})
            {
                CueLevels.CuesAsRoundTen=before;var keys=Heard();
                menu.Open(PauseMenu.FabricatePage,true);yield return null;yield return null;menu.Close();yield return null;
                bool opened=keys.Contains("doorOpen_001"),closed=keys.Contains("doorClose_001");
                Debug.Log($"AUDIO_BENCH {(before?"as round ten":"round eleven")}: opened {opened}, closed {closed}: {string.Join(", ",keys)}");
                if(before)Assert.IsFalse(opened||closed,"fixture: as round ten left it, the bench made a sound");
                else{Assert.IsTrue(opened,"the bench opens in silence");Assert.IsTrue(closed,"the bench closes in silence");}
            }
        }
        /// <summary>A boss who walks out (Vessa Korl, BossDeparture with no sound of her own) went in silence. She is heard going out of sight,
        /// through the door she leaves by, once, at a world cue's level.</summary>
        [UnityTest] public IEnumerator ABossWhoWalksOutIsHeardGoing()
        {
            foreach(bool before in new[]{true,false})
            {
                CueLevels.CuesAsRoundTen=before;var keys=Heard();
                var body=new GameObject("A boss leaving");body.transform.position=PartyController.Current.Active.transform.position+new Vector3(0,0,6);
                body.AddComponent<Health>().id="TestLeaver";var visual=new GameObject("Body").transform;visual.SetParent(body.transform,false);
                var leaving=body.AddComponent<BossDeparture>();leaving.sound="";leaving.outSound="doorOpen_000";leaving.keepsColours=true;leaving.speed=6;
                leaving.Begin(visual,default);
                for(float t=0;t<=leaving.Duration;t+=.1f){leaving.Advance(t);yield return null;}
                int count=keys.Count(k=>k=="doorOpen_000");UnityEngine.Object.Destroy(body);
                Debug.Log($"AUDIO_BOSS_OUT {(before?"as round ten":"round eleven")}: heard going {count} time(s)");
                if(before)Assert.AreEqual(0,count,"fixture: as round ten left it, a boss walking out was heard");
                else Assert.AreEqual(1,count,"a boss walking out is heard going once");
            }
            var vessa=File.ReadAllText(Path.Combine(Application.dataPath,"_Project","Scripts","Combat","BossController.VessaKorl.cs"));
            Assert.IsTrue(Regex.IsMatch(vessa,@"outSound=""doorOpen_000"""),"Vessa Korl walks out with no sound of her going");
        }
    }
}
