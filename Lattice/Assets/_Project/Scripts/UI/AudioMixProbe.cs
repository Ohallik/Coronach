#if UNITY_EDITOR || LATTICE_DEV
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Lattice.Core;
using UnityEngine;
namespace Lattice.UI
{
    // Opt-in DSP witness. This tone fixture measures signal routing, not sound quality.
    public sealed class AudioMixProbe:MonoBehaviour
    {
        [Serializable] sealed class Sample {public string channel,setting;public float rms,ratio;public bool accepted;}
        [Serializable] sealed class Report
        {
            public string kind="diagnostic PCM gain/routing fixture; not ordinary play or listening",fault;
            public int outputSampleRate;public float startupSeconds,startupRms;public bool valid;public List<string> failures=new();public List<Sample> samples=new();
        }
        readonly Report report=new();readonly float[] buffer=new float[2048];
        float measured;AudioSource source;AudioClip tone;string output;
        IEnumerator Start()
        {
            output=DevArgs.Value("-audio-mix-probe");
            if(string.IsNullOrEmpty(output)||string.IsNullOrEmpty(DevArgs.Value("-savepath")))
            {Debug.LogError("AUDIO_MIX_PROBE_REQUIRES_ISOLATED_OUTPUT_AND_SAVE");Application.Quit(1);yield break;}
            output=Path.GetFullPath(output);Directory.CreateDirectory(output);report.fault=DevArgs.Value("-audio-mix-fault");
            report.outputSampleRate=AudioSettings.outputSampleRate;
            yield return new WaitForSecondsRealtime(1.5f);
            if(AudioMix.Current==null){report.failures.Add("mixer did not initialize");Finish();yield break;}
            var listeners=FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            if(listeners.Length!=1){report.failures.Add("expected one output listener, got "+listeners.Length);Finish();yield break;}
            if(MusicDirector.Current!=null)MusicDirector.Current.enabled=false;
            if(AudioManager.Current!=null)AudioManager.Current.enabled=false;
            foreach(var existing in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))existing.Stop();
            foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            AudioMix.Current.SetCueDucking(false);GameTime.Paused=false;AudioListener.pause=false;
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=true;source.spatialBlend=0;source.volume=1;source.priority=0;
            int rate=AudioSettings.outputSampleRate;var data=new float[rate];
            for(int i=0;i<data.Length;i++)data[i]=.05f*Mathf.Sin(2*Mathf.PI*1000*i/rate);
            tone=AudioClip.Create("Diagnostic 1kHz routing witness",rate,1,rate,false);tone.SetData(data,0);source.clip=tone;
            // Prime the listener's output sampling before taking a channel
            // reference. The first cold query can return zeros even though
            // later gain measurements contain audio. This independent master
            // tone must become measurable within a bounded startup interval.
            source.outputAudioMixerGroup=AudioMix.Group(AudioBus.Master);source.Play();
            float started=Time.realtimeSinceStartup;int stable=0;
            while(stable<3&&Time.realtimeSinceStartup-started<2)
            {
                AudioListener.GetOutputData(buffer,0);double energy=0;foreach(float value in buffer)energy+=value*value;
                report.startupRms=(float)Math.Sqrt(energy/buffer.Length);
                stable=report.startupRms>.015f&&report.startupRms<.05f?stable+1:0;yield return null;
            }
            report.startupSeconds=Time.realtimeSinceStartup-started;
            if(stable<3){report.failures.Add("master PCM startup witness was silent or outside its amplitude range");source.Stop();Finish();yield break;}
            foreach(AudioBus bus in new[]{AudioBus.Music,AudioBus.SFX,AudioBus.Ambience,AudioBus.UI})
            {
                source.outputAudioMixerGroup=report.fault=="bypass-sfx"&&bus==AudioBus.SFX?null:AudioMix.Group(bus);source.Play();
                foreach(AudioBus item in Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(item,1,false);
                yield return Measure();float baseline=measured;
                Record(bus,"unity",baseline,baseline,baseline>.015f&&baseline<.05f);
                AudioMix.Current.SetLevel(bus,.5f,false);yield return Measure();Record(bus,"half",measured,baseline,Ratio(measured,baseline,.5f,.04f));
                AudioMix.Current.SetLevel(bus,0,false);yield return Measure();Record(bus,"channel mute",measured,baseline,baseline>.015f&&measured<baseline*.001f);
                AudioMix.Current.SetLevel(bus,1,false);
                foreach(AudioBus other in new[]{AudioBus.Music,AudioBus.SFX,AudioBus.Ambience,AudioBus.UI})if(other!=bus)AudioMix.Current.SetLevel(other,0,false);
                yield return Measure();Record(bus,"other channels muted",measured,baseline,Ratio(measured,baseline,1,.04f));
                AudioMix.Current.SetLevel(AudioBus.Master,.5f,false);yield return Measure();Record(bus,"master half",measured,baseline,Ratio(measured,baseline,.5f,.04f));
                AudioMix.Current.SetLevel(AudioBus.Master,0,false);yield return Measure();Record(bus,"master mute",measured,baseline,baseline>.015f&&measured<baseline*.001f);
            }
            source.Stop();foreach(AudioBus bus in Enum.GetValues(typeof(AudioBus)))AudioMix.Current.SetLevel(bus,1,false);
            Destroy(tone);Finish();
        }
        IEnumerator Measure()
        {
            yield return new WaitForSecondsRealtime(.3f);double energy=0;int count=0;
            for(int frame=0;frame<12;frame++)
            {
                AudioListener.GetOutputData(buffer,0);
                foreach(float value in buffer){energy+=value*value;count++;}
                yield return null;
            }
            measured=(float)Math.Sqrt(energy/count);
        }
        static bool Ratio(float value,float baseline,float target,float tolerance)=>baseline>.015f&&Mathf.Abs(value/baseline-target)<=tolerance;
        void Record(AudioBus bus,string setting,float rms,float baseline,bool accepted)
        {
            report.samples.Add(new Sample{channel=bus.ToString(),setting=setting,rms=rms,ratio=baseline>0?rms/baseline:-1,accepted=accepted});
            if(!accepted)report.failures.Add(bus+" "+setting+": RMS="+rms+" baseline="+baseline);
        }
        void Finish()
        {
            report.valid=report.failures.Count==0;File.WriteAllText(Path.Combine(output,"audio-mix.json"),JsonUtility.ToJson(report,true));
            Debug.Log(report.valid?"AUDIO_MIX_PCM_OK":"AUDIO_MIX_PCM_REJECTED "+string.Join("; ",report.failures));Application.Quit(report.valid?0:1);
        }
    }
}
#endif
