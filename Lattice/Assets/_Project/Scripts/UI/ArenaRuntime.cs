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
            // Only the heroes out take the field (Deployment): a party of two is both, as ever. With three, the one resting has no body here, so nothing that walks
            // the party's members can hit, count or frame them, and the lead is found among the two out. Two ships arrive their hulls' width apart (HeroCollision.Apart).
            var field=Deployment.Of(state);
            float spacing=!ZoneController.Current.Flight?2:field.Count>1?HeroCollision.Apart(field[0].id,field[1].id):HeroCollision.ArrivalSpacing;
            party.members=field.Select((m,i)=>ActorFactory.Hero(m.id,position+Vector3.right*i*spacing)).ToArray();
            party.index=Mathf.Clamp(Deployment.FieldIndex(state),0,party.members.Length-1);
            foreach(var member in party.members)member.transform.SetParent(root.transform);
            var rig=new GameObject("CameraRig",typeof(CameraRig));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig,gameObject.scene);
            gameObject.AddComponent<GameHud>();gameObject.AddComponent<PauseMenu>();gameObject.AddComponent<ShopUi>();
            gameObject.AddComponent<Lattice.Dialogue.DialogueSystem>();gameObject.AddComponent<DialoguePanel>();gameObject.AddComponent<WorldUiBridge>();
            gameObject.AddComponent<DefeatPanel>();
            gameObject.AddComponent<BarkPanel>();
            gameObject.AddComponent<CombatVfx>();
            gameObject.AddComponent<Impact>();
            gameObject.AddComponent<ImpactHud>();
            gameObject.AddComponent<ObjectiveHud>();
            gameObject.AddComponent<QuestToast>();
            gameObject.AddComponent<Credits>();
            gameObject.AddComponent<PickupFeed>();
            gameObject.AddComponent<BuffMarks>();
            gameObject.AddComponent<ExtraHud>();
            gameObject.AddComponent<PeopleMarks>();
            gameObject.AddComponent<ConversationStage>();
            if(DevArgs.Has("-smoketest")&&spawnArenaEnemies)gameObject.AddComponent<ArenaSmoke>();
            SignStyle.Apply();
            Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Reach,gameObject.scene.name);
            // Having stood here is what puts a place on Sela's chart (chapter 3 on; the first two chapters' zones write nothing).
            RouteChart.Arrive(gameObject.scene.name);
            if(spawnArenaEnemies)foreach(var spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))spawner.Spawn();
        }
    }
}
