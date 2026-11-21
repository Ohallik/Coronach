using System.Linq;
using Lattice.Core;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Lattice.Tests.EditMode
{
    /// <summary>S3: the EXTRA move is RB and LT together on a pad and a key of its own on a keyboard. The pad on the desk is a Logitech Precision, ten buttons and
    /// no sticks, so the press has to be one it can make, and LT alone has to stay what it was.</summary>
    public class ExtraInputTests
    {
        /// <summary>The Precision's profile as PadBridge registers it: Logitech's order, with the d-pad on the stick's axes.</summary>
        static PadProfile Precision()=>new PadProfile{Name="LogitechPrecision",StickIsDpad=true,RightStickX=null,RightStickY=null};

        [Test] public void APrecisionCanHoldRbAndLtTogether()
        {
            var profile=Precision();
            Assert.LessOrEqual(profile.RightShoulder,10,"RB isn't one of the Precision's ten buttons");
            Assert.LessOrEqual(profile.LeftTrigger,10,"LT isn't one of the Precision's ten buttons");
            var both=PadBridge.Translate(new PadSnapshot().WithButton(profile.RightShoulder).WithButton(profile.LeftTrigger),profile);
            Assert.IsTrue((both.buttons&(1u<<(int)GamepadButton.RightShoulder))!=0,"the Precision's sixth button doesn't arrive as RB");
            Assert.AreEqual(1f,both.leftTrigger,"the Precision's seventh button doesn't arrive as LT, so RB+LT can't be pressed on it");
            var alone=PadBridge.Translate(new PadSnapshot().WithButton(profile.LeftTrigger),profile);
            Assert.AreEqual(0u,alone.buttons,"LT alone arrives with something else held: the quick item would read as the EXTRA move");
        }

        [Test] public void TheExtraMoveIsRbPlusLtOnAPadAndItsOwnKeyOnAKeyboardInBothForms()
        {
            var input=new GameInput();
            try
            {
                foreach(var map in new[]{input.Ground,input.Flight})
                {
                    var extra=map.FindAction("Extra");
                    Assert.IsNotNull(extra,map.name+" has no EXTRA action");
                    Assert.IsTrue(extra.bindings.Any(b=>b.path=="<Keyboard>/r"),map.name+": the EXTRA move has no key");
                    Assert.IsTrue(extra.bindings.Any(b=>b.isPartOfComposite&&b.path=="<Gamepad>/rightShoulder"),map.name+": the EXTRA move isn't held with RB");
                    Assert.IsTrue(extra.bindings.Any(b=>b.isPartOfComposite&&b.path=="<Gamepad>/leftTrigger"),map.name+": the EXTRA move isn't RB with LT");
                    Assert.IsFalse(extra.bindings.Any(b=>!b.isComposite&&!b.isPartOfComposite&&b.path.StartsWith("<Gamepad>")),map.name+": a pad button fires the EXTRA move on its own");
                    // LT alone is still the quick item on foot and the brake in flight.
                    var alone=map.FindAction(map==input.Flight?"Brake":"QuickItem");
                    Assert.IsTrue(alone.bindings.Any(b=>!b.isPartOfComposite&&b.path=="<Gamepad>/leftTrigger"),map.name+": LT alone lost its job");
                    Assert.IsFalse(map.actions.Where(a=>a!=extra).SelectMany(a=>a.bindings).Any(b=>b.path=="<Keyboard>/r"),map.name+": the EXTRA move's key already does something else");
                }
            }
            finally{input.Dispose();}
        }
    }
}
