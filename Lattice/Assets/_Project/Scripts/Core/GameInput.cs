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
        bool blocked;
        /// <summary>How long Interact waits once a screen has handed the world back.</summary>
        public const float HandBackHold=.35f;
        readonly System.Collections.Generic.HashSet<InputAction> carriedOver=new();
        public bool Blocked
        {
            get=>blocked;
            set
            {
                if(blocked&&!value)
                {
                    carriedOver.Clear();foreach(var action in Active)if(action.type==InputActionType.Button&&!ContinuousMovement(action.name)&&action.IsPressed())carriedOver.Add(action);
                    // One rule for whatever hands the world back, a conversation, a menu, a shop or a load. A thumb still tapping must not start the talk over,
                    // or take the exit the party arrived beside: a dock and a warp go at once, with no question. A held button waits for its release above; a new tap waits here.
                    PromptService.HoldOffInteract(HandBackHold);
                }
                blocked=value;
            }
        }
        // The button dismissing a menu/dialogue belongs to that UI interaction: it needs a release before gameplay
        // can read it as dodge, fire or interact. Only that button waits. Every other one answers at once, or a swap
        // pressed as A lets go is eaten. Continuous movement controls can remain held across UI; they must not lose
        // braking/thrust when gameplay resumes.
        static bool ContinuousMovement(string name) => name=="Brake"||name=="Boost"||name=="Sprint";
        bool CarriedOver(InputAction action)
        {
            if(action==null||!carriedOver.Contains(action))return false;
            if(action.IsPressed())return true;
            carriedOver.Remove(action);return false;
        }
        public InputActionMap Active => FlightMode ? Flight : Ground;
        public Vector2 Move
        {
            get
            {
                if(Blocked||PadCycling)return Vector2.zero;
                var action=Active["Move"];
                // On analog pads the d-pad selects items/targets. Precision exposes its
                // sole directional control as both leftStick and dpad through the bridge.
                if(action.activeControl?.device is Gamepad pad&&!PadBridge.StickIsDpad(pad))return pad.leftStick.ReadValue();
                return action.ReadValue<Vector2>();
            }
        }
        public bool PadCycling => Gamepad.current!=null&&PadBridge.StickIsDpad(Gamepad.current)&&Gamepad.current.leftShoulder.isPressed;
        public int Cycle(string name)
        {
            if(Blocked)return 0;
            var action=Active[name];
            if(action.WasPerformedThisFrame())return (int)Mathf.Sign(action.ReadValue<float>());
            var pad=Gamepad.current;if(pad==null)return 0;
            // Digital-only Precision uses LB as the cycle modifier; other pads retain d-pad cycling.
            if(!PadCycling&&PadBridge.StickIsDpad(pad))return 0;
            if(name=="CycleTarget")return pad.dpad.right.wasPressedThisFrame?1:pad.dpad.left.wasPressedThisFrame?-1:0;
            return pad.dpad.up.wasPressedThisFrame?1:pad.dpad.down.wasPressedThisFrame?-1:0;
        }
        public bool SkillMod => Held("SkillMod");
        int lockPolled=-2;bool lockTap,lockToggled;
        /// <summary>Whether lock-on toggles this frame. A Precision's LB is also its cycle modifier, and LB and a direction must not lock,
        /// unlock or move anything on the way in, so on that pad a tap counts on its release, and only if no direction was pressed while
        /// it was held. Keys and every other pad toggle on the press.</summary>
        public bool LockOnToggled()
        {
            int frame=Time.frameCount;if(frame==lockPolled)return lockToggled;
            // Only live play asks: a gap means a menu, a talk or a stagger took the rest of that press.
            if(frame!=lockPolled+1)lockTap=false;
            lockPolled=frame;
            bool pressed=Pressed("LockOn");var pad=Gamepad.current;
            if(pad==null||!PadBridge.StickIsDpad(pad)){lockTap=false;return lockToggled=pressed;}
            var lb=pad.leftShoulder;
            if(pressed&&lb.wasPressedThisFrame){lockTap=true;pressed=false;}
            var d=pad.dpad;if(d.up.wasPressedThisFrame||d.down.wasPressedThisFrame||d.left.wasPressedThisFrame||d.right.wasPressedThisFrame)lockTap=false;
            if(lockTap&&!lb.isPressed){lockTap=false;pressed=true;}
            return lockToggled=pressed;
        }

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
            // A saved rebind finds its binding by id: an id generated fresh on every launch finds nothing, and the key goes back.
            if(key!=null) a.AddBinding(new InputBinding{path="<Keyboard>/"+key,id=StableId(map.name+"/"+name)});
            if(pad!=null) a.AddBinding("<Gamepad>/"+pad);
            return a;
        }
        static Guid StableId(string text)
        {
            using var md5=System.Security.Cryptography.MD5.Create();
            return new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text)));
        }
        public void SetFlight(bool flight)
        {
            // Crossing a safe pocket re-applies the zone with nothing changed: re-enabling the map drops every held button.
            if(FlightMode==flight&&Active.enabled)return;
            Ground.Disable(); Flight.Disable(); FlightMode=flight; Active.Enable(); PromptService.ForgetTags();
        }
        public InputAction Find(string name) => Active.FindAction(name) ?? Common.FindAction(name);
        public bool Pressed(string name) { if(Blocked)return false;var action=Find(name);return !CarriedOver(action)&&action?.WasPressedThisFrame()==true; }
        public bool Held(string name) { if(Blocked)return false;var action=Find(name);return (ContinuousMovement(name)||!CarriedOver(action))&&action?.IsPressed()==true; }
        public void Dispose()
        {
            Asset.Disable();
            if(Application.isPlaying) UnityEngine.Object.Destroy(Asset);
            else UnityEngine.Object.DestroyImmediate(Asset);
            if(Current==this) Current=null;
        }
    }
}
