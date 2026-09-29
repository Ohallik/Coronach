using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
namespace Lattice.EditorTools
{
    // Editor-only bridge to Unity's mixer asset authoring API. Runtime playback
    // uses only the public AudioMixer/AudioMixerGroup API.
    public static class AudioMixSetup
    {
        const string PathName="Assets/_Project/Resources/Audio/Coronach.mixer";
        const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        static Type MixerType()=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController",false)).First(t=>t!=null);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethods(Flags).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(target,args);
        public static void Create()=>BatchTools.Run(()=>
        {
            if(AssetDatabase.LoadAssetAtPath<AudioMixer>(PathName)!=null)throw new InvalidOperationException("Preserve existing mix; inspect or update it explicitly");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName));
            var type=MixerType();var mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",Flags).Invoke(null,new object[]{PathName});
            var master=type.GetProperty("masterGroup",Flags).GetValue(mixer);
            var groups=new object[5];groups[0]=master;
            var names=new[]{"Master","Music","SFX","Ambience","UI"};
            for(int i=1;i<names.Length;i++)
            {groups[i]=Call(mixer,"CreateNewGroup",names[i],false);Call(mixer,"AddChildToParent",groups[i],master);}
            var exposed=type.GetProperty("exposedParameters",Flags);var parameterType=exposed.PropertyType.GetElementType();
            var parameters=Array.CreateInstance(parameterType,names.Length);
            for(int i=0;i<names.Length;i++)
            {
                var parameter=Activator.CreateInstance(parameterType);
                parameterType.GetField("name",Flags).SetValue(parameter,names[i]+"Volume");
                parameterType.GetField("guid",Flags).SetValue(parameter,Call(groups[i],"GetGUIDForVolume"));parameters.SetValue(parameter,i);
            }
            exposed.SetValue(mixer,parameters);foreach(var group in groups)EditorUtility.SetDirty((UnityEngine.Object)group);EditorUtility.SetDirty(mixer);AssetDatabase.SaveAssets();
            foreach(string name in names)
            {
                if(mixer.FindMatchingGroups(name).Count(g=>g.name==name)!=1)throw new InvalidOperationException("Mixer group missing or ambiguous: "+name);
                if(!mixer.GetFloat(name+"Volume",out _))throw new InvalidOperationException("Mixer volume was not exposed: "+name);
            }
            Debug.Log("AUDIO_MIXER_CREATED_OK");
        });
    }
}
