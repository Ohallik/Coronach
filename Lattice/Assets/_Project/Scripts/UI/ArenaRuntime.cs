using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.UI
{
    public sealed class ArenaRuntime:MonoBehaviour
    {
        public bool spawnArenaEnemies=true;
        /// <summary>How tall a warp's sign stands on a 1080p screen. It has to read from the camera, and the type floor is 24.</summary>
        const float WarpSignPixels=30;
        void Start()
        {
            var state=GameServices.Current.State;
            if(spawnArenaEnemies&&state.party.All(m=>m.id!="Sela"))state.party.Add(new MemberState{id="Sela"});
            var spawn=FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).FirstOrDefault(s=>s.id==(DevArgs.Value("-spawn")??state.spawn));
            Vector3 position=spawn!=null?spawn.transform.position:new Vector3(0,0,-5);
            var root=new GameObject("Party");var party=root.AddComponent<PartyController>();
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);
            float spacing=ZoneController.Current.Flight?HeroCollision.ArrivalSpacing:2;
            party.members=state.party.Select((m,i)=>ActorFactory.Hero(m.id,position+Vector3.right*i*spacing)).ToArray();
            party.index=Mathf.Clamp(state.activeMember,0,party.members.Length-1);
            foreach(var member in party.members)member.transform.SetParent(root.transform);
            var rig=new GameObject("CameraRig",typeof(CameraRig));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig,gameObject.scene);
            gameObject.AddComponent<GameHud>();gameObject.AddComponent<PauseMenu>();gameObject.AddComponent<ShopUi>();
            gameObject.AddComponent<Lattice.Dialogue.DialogueSystem>();gameObject.AddComponent<DialoguePanel>();gameObject.AddComponent<WorldUiBridge>();
            gameObject.AddComponent<DefeatPanel>();
            gameObject.AddComponent<BarkPanel>();
            gameObject.AddComponent<CombatVfx>();
            gameObject.AddComponent<ObjectiveHud>();
            gameObject.AddComponent<QuestToast>();
            gameObject.AddComponent<PickupFeed>();
            gameObject.AddComponent<PeopleMarks>();
            if(DevArgs.Has("-smoketest")&&spawnArenaEnemies)gameObject.AddComponent<ArenaSmoke>();
            // Floor signs in ground zones; name labels stay on their people and flight signs hang in space.
            if(ZoneController.Current==null||!ZoneController.Current.Flight)
                foreach(var sign in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
                    if(sign.transform.parent==null&&sign.name.StartsWith("Sign_")&&sign.GetComponent<SignFade>()==null)sign.gameObject.AddComponent<SignFade>();
            foreach(var sign in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(sign.name.StartsWith("Sign_")){sign.font=UiSkin.Display;sign.outlineWidth=.22f;sign.outlineColor=new Color32(5,12,22,255);}
            // A warp's sign is built at 1.5: 12 px tall under the flight camera. Sized here against this zone's own camera, so every builder's signs agree and no rebuild shrinks them.
            float readable=UiKit.WorldFontSize(WarpSignPixels);
            var warps=FindObjectsByType<Lattice.World.WarpBeacon>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var sign in FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(sign.fontSize<readable&&!string.IsNullOrEmpty(sign.text)&&(sign.text.StartsWith("Warp")||warps.Any(w=>w.prompt==sign.text)))sign.fontSize=readable;
            Lattice.Rpg.RpgServices.Quests.Report(Lattice.Data.ObjectiveKind.Reach,gameObject.scene.name);
            if(spawnArenaEnemies)foreach(var spawner in FindObjectsByType<Spawner>(FindObjectsSortMode.None))spawner.Spawn();
        }
    }
}
