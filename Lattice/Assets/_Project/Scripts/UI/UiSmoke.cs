using System.Collections;
using System.IO;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Dialogue;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class UiSmoke:MonoBehaviour
    {
        IEnumerator Start()
        {
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;yield return new WaitForSecondsRealtime(1);
            foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            var dialogue=DialogueSystem.Current;dialogue.AutoAdvance=false;dialogue.StartNode("ArenaGuide","Hal");yield return new WaitForSecondsRealtime(1.5f);
            yield return Shot("dialogue");dialogue.AutoAdvance=true;float until=Time.realtimeSinceStartup+10;while(dialogue.Running&&Time.realtimeSinceStartup<until)yield return null;
            if(dialogue.Running){Debug.LogError("FAILED: UI dialogue did not finish");Application.Quit(1);yield break;}
            var menu=FindFirstObjectByType<PauseMenu>();if(menu.IsOpen)menu.Close();
            for(int i=0;i<6;i++){menu.Open(i,true);yield return new WaitForSecondsRealtime(.15f);yield return Shot("menu-"+i);menu.Close();}
            var shop=FindFirstObjectByType<ShopUi>();shop.Open(null);yield return Shot("shop");shop.Close();
            Debug.Log("UI_SMOKE_OK");Application.Quit();
        }
        IEnumerator Shot(string name)
        {
            string path=Path.Combine(DevArgs.Value("-uismoke"),name+".png");Directory.CreateDirectory(Path.GetDirectoryName(path));
            ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.25f);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.4f);
            Debug.Log("UI_SCREENSHOT_OK "+path);
        }
    }
}
