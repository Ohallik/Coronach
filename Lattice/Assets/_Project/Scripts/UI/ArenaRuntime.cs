using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class ArenaRuntime:MonoBehaviour
    {
        public bool spawnArenaEnemies=true;
        void Start()
        {
            var state=GameServices.Current.State;
            if(spawnArenaEnemies&&state.party.All(m=>m.id!="Sela"))state.party.Add(new MemberState{id="Sela"});
            var spawn=FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(s=>s.id==(DevArgs.Value("-spawn")??state.spawn));
            Vector3 position=spawn!=null?spawn.transform.position:new Vector3(0,0,-5);
            var root=new GameObject("Party");var party=root.AddComponent<PartyController>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);
            party.members=state.party.Select((m,i)=>ActorFactory.Hero(m.id,position+Vector3.right*i*2)).ToArray();
            party.index=Mathf.Clamp(state.activeMember,0,party.members.Length-1);
            foreach(var member in party.members)member.transform.SetParent(root.transform);
            var rig=new GameObject("CameraRig",typeof(CameraRig));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig,gameObject.scene);
            gameObject.AddComponent<GameHud>();gameObject.AddComponent<PauseMenu>();gameObject.AddComponent<ShopUi>();
            gameObject.AddComponent<Lattice.Dialogue.DialogueSystem>();gameObject.AddComponent<DialoguePanel>();gameObject.AddComponent<WorldUiBridge>();
            gameObject.AddComponent<DefeatPanel>();
            gameObject.AddComponent<BarkPanel>();
            gameObject.AddComponent<CombatVfx>();
            gameObject.AddComponent<ObjectiveHud>();
            if(DevArgs.Has("-smoketest")&&spawnArenaEnemies)gameObject.AddComponent<ArenaSmoke>();
            Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Reach,gameObject.scene.name);
            if(spawnArenaEnemies)foreach(var spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))spawner.Spawn();
        }
    }
}
