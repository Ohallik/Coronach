using Lattice.Combat;
using Lattice.Core;
using Lattice.Dialogue;
using Lattice.World;
using UnityEngine;
namespace Lattice.UI
{
    /// <summary>A conversation with someone on stage (a person, or a boss held for her own confrontation) frames the hero and the speaker together in the part of
    /// the screen above the dialogue box. The camera only moves if one of them would be out of it, so a talk with someone close by doesn't swing anything,
    /// and it eases back when the talk ends. Far-off voices (a radio, a recorded message) have nobody to frame.</summary>
    public sealed class ConversationFraming:MonoBehaviour
    {
        const float FarthestSpeaker=26,Farthest=36;
        const int OutranksAFight=1;
        /// <summary>Above the box, whose top edge is 370 of 1080, with room for the portrait's corner.</summary>
        static readonly Rect Above=Rect.MinMaxRect(.06f,.38f,.94f,.96f),HeroAbove=Rect.MinMaxRect(.06f,.38f,.94f,.92f);
        CameraRig rig;Renderer[] speaker;string framing;
        void Update()
        {
            var dialogue=DialogueSystem.Current;var party=PartyController.Current;
            if(dialogue==null||!dialogue.Running||dialogue.Preparing||party==null||party.Active==null){framing=null;speaker=null;return;}
            if(framing!=dialogue.Speaker){framing=dialogue.Speaker;speaker=Find(framing,party);}
            if(speaker==null||party.Active.flight)return;
            bool any=false;var bounds=default(Bounds);
            foreach(var body in speaker)
            {
                if(body==null||!body.enabled||!body.gameObject.activeInHierarchy)continue;
                if(any)bounds.Encapsulate(body.bounds);else{bounds=body.bounds;any=true;}
            }
            if(!any)return;
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();
            if(rig!=null)rig.FrameGround(bounds,Above,HeroAbove,Farthest,OutranksAFight);
        }
        /// <summary>Looked up once as the talk starts: an Npc by its speaker, a held boss by its id (Hessa, HessaKorl), or the partner. A name label over a head is not part of the body.</summary>
        static Renderer[] Find(string id,PartyController party)
        {
            if(string.IsNullOrEmpty(id))return null;
            var at=party.Active.transform.position;Transform found=null;float nearest=FarthestSpeaker*FarthestSpeaker;
            void Consider(Transform who){var offset=who.position-at;offset.y=0;if(offset.sqrMagnitude<nearest){nearest=offset.sqrMagnitude;found=who;}}
            foreach(var prompt in InteractionPrompt.Active)if(prompt is Npc npc&&npc.speaker==id)Consider(npc.transform);
            if(found==null)foreach(var health in Health.All)if(health!=null&&!health.friendly&&health.id!=null&&health.id.StartsWith(id))Consider(health.transform);
            if(found==null)foreach(var member in party.members)if(member!=party.Active&&member.character==id)Consider(member.transform);
            if(found==null)return null;
            var kept=new System.Collections.Generic.List<Renderer>();
            foreach(var body in found.GetComponentsInChildren<Renderer>())
                if((body is MeshRenderer||body is SkinnedMeshRenderer)&&body.GetComponent<TMPro.TMP_Text>()==null)kept.Add(body);
            return kept.Count>0?kept.ToArray():null;
        }
    }
}
