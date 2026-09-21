using System.Collections;
using System.IO;
using Lattice.Core;
using Lattice.Data;
using Lattice.Combat;
using Lattice.Dialogue;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class PortraitSmoke:MonoBehaviour
    {
        IEnumerator Start()
        {
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            yield return new WaitForSecondsRealtime(1);
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            var system=DialogueSystem.Current;system.AutoAdvance=false;system.StartNode("ArenaGuide","Hal");
            yield return new WaitForSecondsRealtime(2);
            int count=0;
            foreach(string name in new[]{"Taren","Sela","Orrin","Mira","Hal","Neve"})
                foreach(var form in name=="Taren"||name=="Sela"?new[]{BodyForm.Natural,BodyForm.Shaped}:new[]{BodyForm.Natural})
                    foreach(var emotion in new[]{PortraitEmotion.Neutral,PortraitEmotion.Shocked})
                    {
                        var sprite=PortraitLookup.Get(name,form,emotion);
                        if(sprite==null||sprite.rect.width!=576||sprite.rect.height!=576){Debug.LogError("FAILED: portrait missing/full-size rect "+name+" "+form+" "+emotion);Application.Quit(1);yield break;}
                        string line=emotion==PortraitEmotion.Neutral?"The Lattice holds us together. Stay close; we will find a way through.":"That signal is coming from inside the wall!";
                        system.View.Show(name,emotion,sprite,line);system.View.SetVisibleCharacters(int.MaxValue);
                        yield return new WaitForSecondsRealtime(.15f);
                        string path=Path.Combine(DevArgs.Value("-portraitsmoke"),name+"_"+form+"_"+emotion+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));
                        ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.4f);
                        Debug.Log("PORTRAIT_DIALOGUE_RENDER "+path);count++;
                    }
            Debug.Log("PORTRAIT_SMOKE_OK count="+count);Application.Quit();
        }
    }
}
