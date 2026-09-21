using System.Collections.Generic;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Dialogue
{
    public interface IDialogueView
    {
        bool AdvanceRequested{get;}
        void Show(string speaker,PortraitEmotion emotion,Sprite portrait,string body);
        void SetVisibleCharacters(int count);
        void Hide();
        void Options(IReadOnlyList<string> labels,System.Action<int> selected);
        void ClearOptions();
    }
}
