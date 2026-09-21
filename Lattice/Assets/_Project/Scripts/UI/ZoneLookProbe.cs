using System.Collections;
using System.IO;
using Lattice.Core;
using Lattice.Combat;
using Lattice.Data;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>Graphics-only inspection. Teleports to fixed viewpoints; never counts as route evidence.</summary>
    public sealed class ZoneLookProbe:MonoBehaviour
    {
        IEnumerator Start()
        {
            string folder=DevArgs.Value("-look");Directory.CreateDirectory(folder);
            while(PartyController.Current==null||SceneFlow.Current.Loading)yield return null;
            string[] zones={"Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowApproach","TallowDrift"};
            Vector3[][] points={new[]{new Vector3(-20,1,0),new Vector3(0,1,30),new Vector3(25,1,25)},new[]{new Vector3(-20,0,0),new Vector3(0,0,0),new Vector3(20,0,0)},
                new[]{new Vector3(0,0,18),new Vector3(0,0,75),new Vector3(0,0,164)},new[]{new Vector3(0,1,8),new Vector3(Mathf.Sin(320f/900*Mathf.PI*4)*12,1,320),new Vector3(Mathf.Sin(805f/900*Mathf.PI*4)*12,1,805)},
                new[]{new Vector3(0,1,-8),new Vector3(10,1,6),new Vector3(-10,1,16)},new[]{new Vector3(0,0,-3),new Vector3(-4,0,5),new Vector3(6,0,3)}};
            int captured=0;
            for(int z=0;z<zones.Length;z++)
            {
                if(SceneFlow.Current.Zone!=zones[z]){SceneFlow.Current.LoadZone(zones[z]);while(SceneFlow.Current.Loading)yield return null;}
                yield return new WaitForSecondsRealtime(.8f);
                var party=PartyController.Current;foreach(var member in party.members){member.GetComponent<PlayerBrain>().AutoPilot=true;member.GetComponent<PartnerBrain>().enabled=false;member.Health.InvulnerableUntil=float.PositiveInfinity;}
                var rig=FindFirstObjectByType<CameraRig>();var original=rig.profile;var profile=Instantiate(original);
                for(int i=0;i<3;i++)
                {
                    for(int m=0;m<party.members.Length;m++){var actor=party.members[m];var cc=actor.GetComponent<CharacterController>();cc.enabled=false;actor.transform.position=points[z][i]+Vector3.right*m*2;cc.enabled=true;actor.motor.Move(Vector2.zero,false,true);}
                    profile.yaw=original.yaw+(i-1)*12;rig.Apply(party.Active.transform,profile);
                    for(float t=0;t<1.5f;t+=Time.unscaledDeltaTime){foreach(var enemy in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;yield return null;}
                    string path=Path.Combine(folder,zones[z]+"-"+i+".png");ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(.6f);captured++;
                    Debug.Log("LOOK_CAPTURE "+path);
                }
                rig.Apply(party.Active.transform,original);Destroy(profile);
            }
            Debug.Log("ZONE_LOOK_OK count="+captured);Application.Quit();
        }
    }
}
