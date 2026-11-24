using Lattice.Combat;
using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// A fight staged in a safe place (CHAPTER_GUIDE 15.11): Many Hands' freight hall, where Hulljack's second part is fought inside the town. On an always-active object whose trigger
    /// <see cref="BoxCollider"/> is the room. Between <see cref="from"/> (the frame torn in through the roof) and <see cref="until"/> (its operator beaten) it shows <see cref="during"/> and,
    /// while the lead stands in the box, makes the zone a combat place (<see cref="ZoneController.FightPocket"/>: Shaped bodies, the skill row, enemies awake), the reverse of a
    /// SafePocket; stepping out, or the window closing, gives the zone its own kind back. Before the window it shows <see cref="before"/>, after it <see cref="after"/>. <see cref="alarm"/>
    /// comes on in the window once the boss whose Health id is <see cref="alarmBoss"/> is in <see cref="alarmPhase"/> or later. Every state is read from the flags each frame, so a save
    /// loads the room as it stood with nothing played.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FightPocket:MonoBehaviour
    {
        public string from,until;
        public GameObject[] before=System.Array.Empty<GameObject>(),during=System.Array.Empty<GameObject>(),after=System.Array.Empty<GameObject>();
        public GameObject[] alarm=System.Array.Empty<GameObject>();public string alarmBoss;public int alarmPhase=2;
        /// <summary>0 before the window, 1 in it, 2 after it.</summary>
        public int State{get;private set;}=-1;
        BoxCollider room;bool inside,alarmed,shown;
        void Awake(){room=GetComponent<BoxCollider>();}
        void Update()
        {
            if(GameServices.Current==null)return;
            var flags=GameServices.Current.Flags;
            int state=!string.IsNullOrEmpty(until)&&flags.GetBool(until)?2:string.IsNullOrEmpty(from)||flags.GetBool(from)?1:0;
            if(state!=State||!shown)
            {
                State=state;shown=true;
                Show(before,state==0);Show(during,state==1);Show(after,state==2);
                Debug.Log("FIGHT_POCKET_STAGE "+name+" "+state);
            }
            bool sound=state==1&&!string.IsNullOrEmpty(alarmBoss)&&BossController.All.Exists(b=>b!=null&&b.Health!=null&&b.Health.id==alarmBoss&&b.Phase>=alarmPhase);
            if(sound&&!alarmed)Debug.Log("FIGHT_POCKET_ALARM "+name);
            if(sound!=alarmed||alarm.Length>0&&alarm[0]!=null&&alarm[0].activeSelf!=sound){alarmed=sound;Show(alarm,sound);}
            var party=PartyController.Current;var zone=ZoneController.Current;
            if(party==null||party.Active==null||zone==null)return;
            bool now=state==1&&room.bounds.Contains(party.Active.transform.position+Vector3.up*.5f);
            if(now==inside)return;
            inside=now;zone.FightPocket(now);Debug.Log("FIGHT_POCKET "+name+" "+(now?"in":"out"));
        }
        static void Show(GameObject[] set,bool on){foreach(var go in set)if(go!=null&&go.activeSelf!=on)go.SetActive(on);}
        /// <summary>Switched off with the lead inside, the zone gets its own kind back. Not at a scene's unload: the zone and the party go with the scene, and handing it back
        /// refreshed heroes the unload had already destroyed, which threw (FightPocketUnloadTests).</summary>
        void OnDisable()
        {
            var zone=ZoneController.Current;var party=PartyController.Current;
            // The services go at an unload too, sometimes before this does (suite nov24a, the Warmaster's field: the zone's Apply threw on them).
            bool whole=GameServices.Current!=null&&zone!=null&&zone.isActiveAndEnabled&&party!=null&&party.members!=null&&System.Array.TrueForAll(party.members,m=>m!=null);
            if(inside&&whole){zone.FightPocket(false);Debug.Log("FIGHT_POCKET "+name+" out");}
            inside=false;
        }
    }
}
