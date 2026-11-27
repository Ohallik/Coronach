using Lattice.Combat;
using UnityEngine;
using UnityEngine.UI;
namespace Lattice.UI
{
    /// <summary>The far end of a big arena can leave a boss off screen even with the camera drawn back as far as it goes: a small arrow on the screen's edge points to it.
    /// With no fight on, a smaller one in the objective's cream does the same for where the objective sends the party. In a flight zone, whose screen can hold nothing but specks for half a crossing, and on foot for a hero who's down and out of shot.</summary>
    public sealed class BossEdgeMark:MonoBehaviour
    {
        const float Inset=.05f,Margin=.02f,BossBox=56,GoalBox=40,AlongEdge=.03f;const int OutwardSteps=8;
        /// <summary>The goal's mark stands in from the edges by these shares of the screen: over the hint bar and the skill row, along the foot.</summary>
        const float GoalSide=.04f,GoalTop=.06f,GoalFoot=.16f;
        static readonly Color GoalCream=new(1,.93f,.8f,.95f),GoalEdge=new(.02f,.03f,.05f,.85f);
        const float GoalEdgeWidth=3;
        /// <summary>A locked target nowhere on the screen: a small mark in the lock's amber at the edge on its side, pointing to it (D356). The card went on naming a lock the player
        /// couldn't see, with nothing to say which way it was. A boss's own mark stands for it when the lock is on her.</summary>
        const float LockBox=44;
        static readonly Color LockAmber=new(1,.76f,.3f,.95f);
        Image mark,goalMark,lockMark;RectTransform place,goalPlace,lockPlace;ObjectiveHud objective;
        /// <summary>Held, not asked of a mark: this goes on working behind a menu or a conversation, which turn the canvas off, and a graphic has no canvas then.</summary>
        Canvas surface;
        public void Build(Transform parent)
        {
            surface=parent.GetComponent<Canvas>();
            mark=UiKit.Panel(parent,"BossEdgeMark",new Color(1,.46f,.2f,.95f));mark.sprite=UiSkin.Pointer();mark.raycastTarget=false;mark.preserveAspect=true;
            place=UiKit.Rect(mark.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(BossBox,BossBox));mark.enabled=false;
            goalMark=UiKit.Panel(parent,"GoalEdgeMark",GoalCream);goalMark.sprite=UiSkin.Pointer();goalMark.raycastTarget=false;goalMark.preserveAspect=true;
            // Cream on a cream planet it was lost, just where the screen has least else on it. A dark edge holds it on anything, and on black space it doesn't show. The boss's orange shares the sprite and keeps its look, so the edge belongs to this mark and not to the art.
            var edge=goalMark.gameObject.AddComponent<Outline>();edge.effectColor=GoalEdge;edge.effectDistance=new Vector2(GoalEdgeWidth,GoalEdgeWidth);
            goalPlace=UiKit.Rect(goalMark.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(GoalBox,GoalBox));goalMark.enabled=false;
            lockMark=UiKit.Panel(parent,"LockEdgeMark",LockAmber);lockMark.sprite=UiSkin.Pointer();lockMark.raycastTarget=false;lockMark.preserveAspect=true;
            var rim=lockMark.gameObject.AddComponent<Outline>();rim.effectColor=GoalEdge;rim.effectDistance=new Vector2(GoalEdgeWidth,GoalEdgeWidth);
            lockPlace=UiKit.Rect(lockMark.gameObject,new(.5f,.5f),new(.5f,.5f),Vector2.zero,new(LockBox,LockBox));lockMark.enabled=false;
            objective=GetComponent<ObjectiveHud>();
        }
        void Update()
        {
            if(mark==null)return;
            var party=PartyController.Current;var camera=Camera.main;
            var boss=party!=null&&party.Active!=null&&!party.Active.flight?BossController.Shown(party.Active):null;
            bool show=false;
            if(boss!=null&&camera!=null)
            {
                var view=camera.WorldToViewportPoint(boss.transform.position+Vector3.up);
                if(view.z<=0){view.x=1-view.x;view.y=1-view.y;}
                show=view.z<=0||view.x<Margin||view.x>1-Margin||view.y<Margin||view.y>1-Margin;
                if(show)
                {
                    var from=new Vector2(view.x-.5f,view.y-.5f);
                    float reach=Mathf.Min((.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.x)),(.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.y)));
                    var at=new Vector2(.5f,.5f)+from*reach;
                    place.anchorMin=place.anchorMax=OffThePlates(at,BossBox,camera,Mathf.Abs(at.x-.5f)>=.5f-Inset-.001f);place.anchoredPosition=Vector2.zero;
                    place.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(from.y*9,from.x*16)*Mathf.Rad2Deg-90);
                }
            }
            if(mark.enabled!=show)mark.enabled=show;
            bool goalShow=camera!=null&&party!=null&&party.Active!=null&&GoalMark(camera,party);
            if(goalMark.enabled!=goalShow)goalMark.enabled=goalShow;
            bool lockShow=camera!=null&&party!=null&&party.Active!=null&&LockMark(camera,party.Active,show?boss:null);
            if(lockMark.enabled!=lockShow)lockMark.enabled=lockShow;
        }
        /// <summary>Where the line from the screen's middle to the lock meets the screen's edge, in from it as the boss's mark stands, and off the panels along that edge.</summary>
        bool LockMark(Camera camera,CombatActor lead,BossController markedBoss)
        {
            var target=lead.target;
            if(!lead.TargetLocked||target==null||!target.Alive||!target.gameObject.activeInHierarchy)return false;
            if(markedBoss!=null&&(target==markedBoss.Health||target.transform.IsChildOf(markedBoss.transform)))return false;
            var view=camera.WorldToViewportPoint(target.transform.position+Vector3.up);
            if(view.z<=0){view.x=1-view.x;view.y=1-view.y;}
            if(view.z>0&&view.x>=Margin&&view.x<=1-Margin&&view.y>=Margin&&view.y<=1-Margin)return false;
            var from=new Vector2(view.x-.5f,view.y-.5f);if(from.sqrMagnitude<1e-6f)from=Vector2.down;
            float reach=Mathf.Min((.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.x)),(.5f-Inset)/Mathf.Max(.0001f,Mathf.Abs(from.y)));
            var at=new Vector2(.5f,.5f)+from*reach;
            lockPlace.anchorMin=lockPlace.anchorMax=OffThePlates(at,LockBox,camera,Mathf.Abs(at.x-.5f)>=.5f-Inset-.001f);lockPlace.anchoredPosition=Vector2.zero;
            lockPlace.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(from.y*9,from.x*16)*Mathf.Rad2Deg-90);
            return true;
        }
        /// <summary>It points as the panel's arrow points, along a short step toward the goal: a far goal can lie behind the camera itself. Where that line from the hero meets the edge is where it stands.</summary>
        bool GoalMark(Camera camera,PartyController party)
        {
            var lead=party.Active;
            if(objective==null||!objective.Goal.HasValue||!lead.flight&&!objective.GoalIsHero)return false;
            var goal=objective.Goal.Value;var hero=lead.transform.position;
            var view=camera.WorldToViewportPoint(goal+Vector3.up);
            if(view.z>0&&view.x>=Margin&&view.x<=1-Margin&&view.y>=Margin&&view.y<=1-Margin)return false;
            if(BossController.Focus(hero)!=null||EnemyBrain.AwakeNear(hero,lead.flight?GameHud.FlightFightRange:GameHud.FightRange))return false;
            var gap=goal-hero;gap.y=0;
            Vector2 from=camera.WorldToScreenPoint(hero),way=(Vector2)camera.WorldToScreenPoint(hero+gap.normalized*2)-from;
            float width=camera.pixelWidth,height=camera.pixelHeight,left=width*GoalSide,right=width*(1-GoalSide),foot=height*GoalFoot,top=height*(1-GoalTop);
            float along=Mathf.Min(way.x>0?(right-from.x)/way.x:way.x<0?(left-from.x)/way.x:float.MaxValue,way.y>0?(top-from.y)/way.y:way.y<0?(foot-from.y)/way.y:float.MaxValue);
            var at=from+way*Mathf.Max(0,along);at.x=Mathf.Clamp(at.x,left,right);at.y=Mathf.Clamp(at.y,foot,top);
            goalPlace.anchorMin=goalPlace.anchorMax=OffThePlates(new Vector2(at.x/width,at.y/height),GoalBox,camera,at.x<=left+1||at.x>=right-1);goalPlace.anchoredPosition=Vector2.zero;
            goalPlace.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(way.y,way.x)*Mathf.Rad2Deg-90);
            return true;
        }
        /// <summary>Along its edge toward the middle until no panel stands over it: in a corner it lay under the objective, which draws on top. It still points the way it did.
        /// With nowhere clear on that edge it stays where the way leads: along the foot the hint bar and the skill row fill the whole width, and sliding on carried a mark for a boss down and to the left to the bottom middle.</summary>
        /// <para>D416: and outward along the edge too, nearest first. Toward the middle of the top edge is where the lock's card stands, so a mark that began on the card slid
        /// further onto it and stayed, the cream arrow cutting into 'LOCKED' (nov24-whole s62 121).</para>
        Vector2 OffThePlates(Vector2 at,float box,Camera camera,bool onSide)
        {
            float width=camera.pixelWidth,height=camera.pixelHeight,half=box*.5f*(surface!=null?surface.scaleFactor:1);
            bool Clear(Vector2 tried)=>!PanelFade.Cuts(new Rect(tried.x*width-half,tried.y*height-half,half*2,half*2),width,height);
            if(Clear(at))return at;
            float along=onSide?at.y:at.x,toward=along<.5f?1:-1;
            for(int step=1;step<=16;step++)
            {
                foreach(float way in new[]{toward,-toward})
                {
                    float next=along+way*AlongEdge*step;if(next<Inset*.5f||next>1-Inset*.5f)continue;
                    // Toward the middle it may not pass it, as before.
                    if(way==toward&&(toward>0?next>.5f:next<.5f))continue;
                    // Outward no further than a card's width or so: a mark carried to a far corner points nowhere it means.
                    if(way!=toward&&step>OutwardSteps)continue;
                    var tried=onSide?new Vector2(at.x,next):new Vector2(next,at.y);
                    if(Clear(tried))return tried;
                }
            }
            return at;
        }
    }
}
