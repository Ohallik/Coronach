using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lattice.Core
{
    /// <summary>Code-built actions; no serialized package defaults or raw joystick bindings.</summary>
    public sealed class GameInput : IDisposable
    {
        public static GameInput Current { get; private set; }
        public InputActionAsset Asset { get; }
        public InputActionMap Ground { get; }
        public InputActionMap Flight { get; }
        public InputActionMap Common { get; }
        public bool FlightMode { get; private set; }
        public bool Blocked { get; set; }
        public InputActionMap Active => FlightMode ? Flight : Ground;
        public Vector2 Move => Blocked ? Vector2.zero : Active["Move"].ReadValue<Vector2>();
        public bool SkillMod => Held("SkillMod");

        public GameInput()
        {
            Asset = ScriptableObject.CreateInstance<InputActionAsset>();
            Asset.name = "LatticeInput";
            Ground = Asset.AddActionMap("Ground");
            Flight = Asset.AddActionMap("Flight");
            Common = Asset.AddActionMap("Common");
            Build(Ground, false); Build(Flight, true);
            Button(Common,"MenuPrev","q","leftShoulder");
            Button(Common,"MenuNext","e","rightShoulder");
            Common.Enable(); Ground.Enable();
            UiActions.Ensure();
            Current = this;
            var bindings = PlayerPrefs.GetString("keyboardBindings", "");
            if (!string.IsNullOrEmpty(bindings)) Asset.LoadBindingOverridesFromJson(bindings);
        }

        static void Build(InputActionMap map, bool flight)
        {
            var move = map.AddAction("Move", InputActionType.Value); move.expectedControlType="Vector2";
            move.AddBinding("<Gamepad>/leftStick"); move.AddBinding("<Gamepad>/dpad");
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s")
                .With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            Button(map, flight ? "Fire" : "Attack", "j", "buttonSouth");
            Button(map, flight ? "Roll" : "Dodge", "space", "buttonEast");
            Button(map, flight ? "Lunge" : "Guard", "k", "buttonWest");
            Button(map,"Swap","tab","buttonNorth");
            Button(map,"SkillMod",null,"rightShoulder");
            Button(map,"LockOn","q","leftShoulder");
            Button(map,flight ? "Boost" : "Sprint","leftShift","rightTrigger");
            Button(map,flight ? "Brake" : "QuickItem",flight ? "f" : "f","leftTrigger");
            Button(map,"Interact","e","buttonSouth");
            Button(map,"Pause","escape","start");
            Button(map,"Log","m","select");
            string[] faces={"buttonSouth","buttonEast","buttonWest","buttonNorth"};
            for(int i=0;i<4;i++)
            {
                var a=Button(map,"Skill"+(i+1),(i+1).ToString(),null);
                a.AddCompositeBinding("ButtonWithOneModifier").With("Modifier","<Gamepad>/rightShoulder")
                    .With("Button","<Gamepad>/"+faces[i]);
            }
            // Precision has only a d-pad. LB + direction cycles without stealing movement.
            foreach(var name in new[]{"CycleItem", "CycleTarget"})
            {
                var a=map.AddAction(name,InputActionType.Value); a.expectedControlType="Axis";
                a.AddCompositeBinding("1DAxis").With("Negative",name=="CycleItem"?"<Keyboard>/downArrow":"<Keyboard>/leftArrow")
                    .With("Positive",name=="CycleItem"?"<Keyboard>/upArrow":"<Keyboard>/rightArrow");
            }
        }

        static InputAction Button(InputActionMap map,string name,string key,string pad)
        {
            var a=map.AddAction(name,InputActionType.Button);
            if(key!=null) a.AddBinding("<Keyboard>/"+key);
            if(pad!=null) a.AddBinding("<Gamepad>/"+pad);
            return a;
        }
        public void SetFlight(bool flight)
        {
            Ground.Disable(); Flight.Disable(); FlightMode=flight; Active.Enable();
        }
        public InputAction Find(string name) => Active.FindAction(name) ?? Common.FindAction(name);
        public bool Pressed(string name) => !Blocked && Find(name)?.WasPressedThisFrame()==true;
        public bool Held(string name) => !Blocked && Find(name)?.IsPressed()==true;
        public void Dispose()
        {
            Asset.Disable();
            if(Application.isPlaying) UnityEngine.Object.Destroy(Asset);
            else UnityEngine.Object.DestroyImmediate(Asset);
            if(Current==this) Current=null;
        }
    }
}
