using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// Something the dawn changes as its line reaches it (chapter 9's Terminator; <see cref="DawnLine"/>): what a player watches happen to something else before it can happen to the party.
    /// <list type="bullet">
    /// <item><b>Heat</b>: a rock, a wreck, a car. It goes hot as the line crosses it (its own colour brightened toward a burning white-gold, by property block), the whole of it once
    /// the line is <see cref="span"/> metres past where it first reached it.</item>
    /// <item><b>Paint</b>: an old crawler buggy in the Crawl's colours. Reached, its paint blisters over a second and a half (its tint to a scorched brown, puffs of smoke), then it
    /// smoulders, hot, as a Heat piece does.</item>
    /// <item><b>Creature</b>: a heat-scavenger's body (drawn only, never a fighter) hunting along the line: it skitters across the way in front of it, quicker as the light comes, and the
    /// light takes it: it rolls over, burns black, sinks a hand and smokes. The run's first lesson.</item>
    /// </list>
    /// Where the line is decides everything, so a load with the line already past it has it as the light left it, with nothing played. A still in an editor shows it as the line the
    /// still poses (<see cref="DawnLine"/> calls <see cref="Pose"/>), by property block only: nothing a still does is saved with the scene.
    /// Logs <c>DAWN_TOUCHED &lt;name&gt;</c> the moment the line reaches a Paint or a Creature in play.
    /// </summary>
    [ExecuteAlways]
    public sealed class DawnTouched:MonoBehaviour
    {
        public enum Kind{Heat,Paint,Creature}
        public Kind kind;
        /// <summary>How far along the line's way the line first reaches it (DawnLine's coordinate), and how much further it goes before the whole of it is lit.</summary>
        public float at,span=4;
        /// <summary>Its colour before the light (what a PropTint would have given it), and, for paint, what the light leaves.</summary>
        public Color tint=Color.white,scorched=new(.3f,.2f,.15f);
        /// <summary>How hot the light makes it: its colour is multiplied by this at full heat.</summary>
        public Color hot=new(1.9f,1.66f,1.36f);
        /// <summary>A creature's body (a child), how far either side of where it was put it skitters (across the line's way), and how quick.</summary>
        public Transform body;public float skitter=3.2f,pace=2.2f;
        static readonly int BaseColor=Shader.PropertyToID("_BaseColor");
        static readonly Color Burnt=new(.07f,.055f,.05f);
        MaterialPropertyBlock block;Renderer[] parts;Vector3 rest;Quaternion restTurn;bool measured,dead;float caughtAt=-1,nextPuff,phase;
        /// <summary>The line has reached it.</summary>
        public bool Reached{get;private set;}
        /// <summary>A creature the light has taken.</summary>
        public bool Dead=>dead;
        void OnEnable(){measured=false;Measure();Paint(0,0,false);}
        void Measure()
        {
            if(measured)return;measured=true;block??=new MaterialPropertyBlock();
            parts=GetComponentsInChildren<Renderer>(true);
            var moved=body!=null?body:transform;rest=moved.position;restTurn=moved.rotation;phase=Mathf.Abs(transform.position.x*.37f+transform.position.z*.11f);
        }
        void Paint(float heat,float scorch,bool black)
        {
            if(parts==null)return;
            var colour=black?Burnt:Color.Lerp(tint,scorched,scorch);
            if(!black)colour=new Color(colour.r*Mathf.Lerp(1,hot.r,heat),colour.g*Mathf.Lerp(1,hot.g,heat),colour.b*Mathf.Lerp(1,hot.b,heat),1);
            foreach(var part in parts){if(part==null)continue;part.GetPropertyBlock(block);block.SetColor(BaseColor,colour);part.SetPropertyBlock(block);}
        }
        /// <summary>As the line at <paramref name="line"/> leaves it. A still poses; play plays.</summary>
        public void Pose(float line,bool still)
        {
            Measure();
            float into=line-at;bool reached=into>=0;float heat=reached?Mathf.Clamp01(into/Mathf.Max(.5f,span)):0;
            switch(kind)
            {
                case Kind.Heat:Paint(heat,0,false);break;
                case Kind.Paint:
                    if(still){Paint(heat,reached?1:0,false);break;}
                    if(reached&&!Reached){caughtAt=Time.time;Debug.Log("DAWN_TOUCHED "+name);}
                    float blister=caughtAt<0?(reached?1:0):Mathf.Clamp01((Time.time-caughtAt)/1.5f);
                    Paint(heat,blister,false);
                    if(reached&&caughtAt>=0&&Time.time>=nextPuff&&Time.time-caughtAt<9){nextPuff=Time.time+(blister<1?.35f:1.6f);CombatVfx.Burst(Centre()+Vector3.up*.6f,new Color(.42f,.38f,.36f,.6f),"steam");}
                    break;
                case Kind.Creature:Creature(into,still);break;
            }
            Reached=reached;
        }
        Vector3 Centre()
        {
            if(parts==null||parts.Length==0||parts[0]==null)return transform.position;
            var box=parts[0].bounds;foreach(var p in parts)if(p!=null)box.Encapsulate(p.bounds);return box.center;
        }
        void Creature(float into,bool still)
        {
            var moved=body!=null?body:transform;
            if(still){Paint(0,0,into>=0);return;}
            if(into<0&&!dead)
            {
                // Hunting along the warm edge: across the way and back, quicker as the light comes on.
                float urgency=1+2*Mathf.Clamp01((into+6)/6);phase+=Time.deltaTime*pace*urgency/Mathf.Max(.5f,skitter);
                var side=transform.right;side.y=0;side.Normalize();
                moved.position=rest+side*(Mathf.Sin(phase)*skitter);
                var heading=side*Mathf.Cos(phase);if(heading.sqrMagnitude>.001f)moved.rotation=Quaternion.LookRotation(heading.normalized,Vector3.up);
                return;
            }
            if(!dead)
            {
                dead=true;caughtAt=Time.time;Debug.Log("DAWN_TOUCHED "+name);
                CombatVfx.Burst(moved.position+Vector3.up*.5f,new Color(2.2f,1.8f,1.2f,1),"kill");CombatVfx.Burst(moved.position+Vector3.up*.4f,new Color(.6f,.56f,.52f,.6f),"steam");
                AudioManager.PlayAt("forceField_000",null,moved.position+Vector3.up,.2f,AudioBus.SFX,90,.06f,.55f);
                restTurn=moved.rotation;rest=moved.position;
            }
            // Caught: it rolls over and burns out where it lay.
            float k=Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.time-caughtAt)/.4f));
            var forward=restTurn*Vector3.forward;forward.y=0;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            moved.SetPositionAndRotation(rest+Vector3.down*.18f*k,Quaternion.Slerp(restTurn,Quaternion.AngleAxis(170,forward.normalized)*Quaternion.LookRotation(forward.normalized,Vector3.up),k));
            Paint(0,0,true);
            if(Time.time>=nextPuff&&Time.time-caughtAt<8){nextPuff=Time.time+1.2f;CombatVfx.Burst(moved.position+Vector3.up*.3f,new Color(.4f,.38f,.36f,.5f),"steam");}
        }
        /// <summary>Its colour now, for a test.</summary>
        public Color Drawn(){Measure();if(parts==null||parts.Length==0||parts[0]==null)return tint;parts[0].GetPropertyBlock(block);return block.GetColor(BaseColor);}
    }
}
