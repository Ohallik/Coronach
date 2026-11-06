using Lattice.Combat;
using TMPro;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A floor sign is read from across a room. A hero standing on one read it across their own legs, so it steps aside.</summary>
    [DefaultExecutionOrder(910)]
    public sealed class SignFade:MonoBehaviour
    {
        const float Near=2.5f,Far=6.5f;
        TextMeshPro text;float full;
        void Awake(){text=GetComponent<TextMeshPro>();full=text.alpha;}
        void LateUpdate()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var offset=transform.position-party.Active.transform.position;offset.y=0;
            float alpha=full*Mathf.Clamp01((offset.magnitude-Near)/(Far-Near));
            if(Mathf.Abs(text.alpha-alpha)>.01f)text.alpha=alpha;
        }
    }
}
