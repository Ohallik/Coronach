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
        /// <summary>Above the box, whose top edge is 370 of 1080, with room for the portrait's corner; and in from the sides. At six hundredths from the edge the talk on Rootmarket's
        /// rope opened with Taren at the left edge, Sela cut off past it and Ruva at the far right (nov20 s18 010).</summary>
        static readonly Rect Above=Rect.MinMaxRect(.1f,.38f,.9f,.94f),HeroAbove=Rect.MinMaxRect(.18f,.38f,.82f,.9f);
        CameraRig rig;Renderer[] speaker;string framing;DialoguePanel panel;int liftAsked=-1;bool lifted;
        /// <summary>How far over the choices' top the people stand while the choices are up, as a share of the screen's height.</summary>
        const float OverChoices=.02f;
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
            if(rig==null)return;
            // The choices stand on the box at its right end. Asked once a set of choices: if anyone in the talk (the speaker, the hero, the partner standing by) is under them,
            // the view keeps all three above the choices' top until they go. Mira's two choices sat on Sela (nov22 s01 025).
            if(panel==null)panel=FindFirstObjectByType<DialoguePanel>();
            var choices=panel!=null?panel.ChoiceArea:null;Bounds? partner=Partner(party,dialogue.Speaker);
            if(!choices.HasValue)lifted=false;
            else if(liftAsked!=panel.ChoicesFrame){liftAsked=panel.ChoicesFrame;lifted=Under(bounds,choices.Value)||Under(Body(party.Active.transform.position),choices.Value)||partner.HasValue&&Under(partner.Value,choices.Value);}
            if(!lifted){rig.FrameGround(bounds,Above,HeroAbove,Farthest,OutranksAFight);return;}
            float floor=choices.Value.yMax+OverChoices;
            Rect Lift(Rect area)=>Rect.MinMaxRect(area.xMin,Mathf.Min(Mathf.Max(area.yMin,floor),area.yMax-.2f),area.xMax,area.yMax);
            // The partner is held with the speaker, not given what room is left: left over, she stayed under the choices.
            if(partner.HasValue)bounds.Encapsulate(partner.Value);
            rig.FrameGround(bounds,Lift(Above),Lift(HeroAbove),Farthest,OutranksAFight);
        }
        /// <summary>A standing body's box, as a fight's framing takes one.</summary>
        static Bounds Body(Vector3 feet)=>new(feet+Vector3.up*.95f,new Vector3(1,1.9f,1));
        /// <summary>The partner standing by while the hero talks, unless she is the one speaking.</summary>
        static Bounds? Partner(PartyController party,string speaking)
        {
            foreach(var member in party.members)if(member!=null&&member!=party.Active&&member.character!=speaking&&member.Health!=null&&member.Health.Alive&&member.gameObject.activeInHierarchy)return Body(member.transform.position);
            return null;
        }
        /// <summary>Whether any of this box is drawn under the choices.</summary>
        static bool Under(Bounds box,Rect choices)
        {
            var camera=Camera.main;if(camera==null)return false;
            float l=float.MaxValue,b=float.MaxValue,r=float.MinValue,t=float.MinValue;
            for(int corner=0;corner<8;corner++)
            {
                var point=camera.WorldToViewportPoint(box.center+Vector3.Scale(box.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                if(point.z<=0)continue;l=Mathf.Min(l,point.x);b=Mathf.Min(b,point.y);r=Mathf.Max(r,point.x);t=Mathf.Max(t,point.y);
            }
            return l<=r&&Rect.MinMaxRect(l,b,r,t).Overlaps(choices);
        }
        /// <summary>Looked up once as the talk starts: an Npc by its speaker, a held boss by its id (Hessa, HessaKorl), or the partner. A name label over a head is not part of the body.</summary>
        static Renderer[] Find(string id,PartyController party)
        {
            if(string.IsNullOrEmpty(id))return null;
            var at=party.Active.transform.position;Transform found=null;float nearest=FarthestSpeaker*FarthestSpeaker;
            void Consider(Transform who){var offset=who.position-at;offset.y=0;if(offset.sqrMagnitude<nearest){nearest=offset.sqrMagnitude;found=who;}}
            foreach(var prompt in InteractionPrompt.Active)if(prompt is Npc npc&&npc.speaker==id)Consider(npc.transform);
            // By the start of its id (Hessa, HessaKorl) or its end (Warden, LastWarden): the Last Warden stood with his head off the top of the frame for his whole stand-down, framed by nobody (nov20 s41 103).
            if(found==null)foreach(var health in Health.All)if(health!=null&&!health.friendly&&health.id!=null&&(health.id.StartsWith(id)||health.id.EndsWith(id)))Consider(health.transform);
            if(found==null)foreach(var member in party.members)if(member!=party.Active&&member.character==id)Consider(member.transform);
            if(found==null)return null;
            var kept=new System.Collections.Generic.List<Renderer>();
            foreach(var body in found.GetComponentsInChildren<Renderer>())
                if((body is MeshRenderer||body is SkinnedMeshRenderer)&&body.GetComponent<TMPro.TMP_Text>()==null)kept.Add(body);
            return kept.Count>0?kept.ToArray():null;
        }
    }
}
