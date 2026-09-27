using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    [DefaultExecutionOrder(650)]
    public sealed class FormController:MonoBehaviour
    {
        public GameObject natural,shaped,flight;
        public BodyForm Current{get;private set;}
        public bool Shaping{get;private set;}
        enum Stage { Closing, Exchange, Opening, Holding }
        Stage stage;BodyForm requested;float weight,exchange;
        FormFold pose;CombatActor actor;Health health;bool first=true,departing;
        public bool DepartureReady=>departing&&stage==Stage.Holding;
        GameObject Visual(BodyForm form)=>form==BodyForm.Natural?natural:form==BodyForm.Shaped?shaped:flight;
        GameObject Visible=>natural!=null&&natural.activeSelf?natural:shaped!=null&&shaped.activeSelf?shaped:flight!=null&&flight.activeSelf?flight:null;
        void Awake(){actor=GetComponent<CombatActor>();health=GetComponent<Health>();if(health!=null)health.Died+=OnDown;}
        void OnDestroy(){if(health!=null)health.Died-=OnDown;}
        public void Set(BodyForm form)
        {
            var incoming=Visual(form);var visible=Visible;
            bool arriving=first&&SceneFlow.Current!=null&&SceneFlow.Current.DockArrival;first=false;
            if(arriving)
            {
                requested=form;Show(form);Shaping=true;weight=1;exchange=0;stage=Stage.Opening;
                if(incoming==flight){flight.transform.localPosition=Vector3.up*.8f;flight.transform.localRotation=Quaternion.identity;}
                pose=Prepare(incoming,true,true);if(pose!=null)pose.Apply(1);return;
            }
            if(departing)return;
            if(Shaping&&Visual(requested)==incoming)
            {requested=form;if(visible==incoming)Current=form;return;}
            requested=form;
            if(!Shaping&&visible==incoming)
            {Current=form;return;}
            if(Shaping&&visible==incoming)
            {Current=form;stage=Stage.Opening;exchange=0;return;}
            if(!Shaping)
            {
                Shaping=true;weight=0;exchange=0;
                if(actor!=null)actor.BeginFormChange();
                pose=Prepare(visible,visible==flight||incoming==flight);
            }
            // Retarget the existing closing pose without recapturing or popping
            // its bones. The new destination opens in its own correct pose.
            stage=Stage.Closing;
        }
        public void BeginDeparture(bool toFlight)
        {
            if(health!=null&&!health.Alive)return;
            departing=true;requested=Current;exchange=0;
            if(!Shaping)
            {
                weight=0;Shaping=true;if(actor!=null)actor.BeginFormChange();
                pose=Prepare(Visible,Visible==flight||toFlight);
            }
            stage=Stage.Closing;
        }
        FormFold Prepare(GameObject visual,bool airborne,bool incoming=false)
        {
            if(visual==null)return null;
            if(incoming){var rig=visual.GetComponentInChildren<Animator>();if(rig!=null)rig.Update(0);}
            var fold=visual.GetComponent<FormFold>()??visual.AddComponent<FormFold>();fold.Prepare(airborne);return fold;
        }
        void Show(BodyForm form)
        {
            Current=form;var incoming=Visual(form);
            foreach(var visual in new[]{natural,shaped,flight})if(visual!=null)visual.SetActive(visual==incoming);
        }
        void LateUpdate()
        {
            if(!Shaping)return;
            if(GameTime.Paused){if(pose!=null)pose.Apply(Mathf.SmoothStep(0,1,weight));return;}
            if(stage==Stage.Holding){if(pose!=null)pose.Apply(1);return;}
            float dt=Time.unscaledDeltaTime;
            if(stage==Stage.Closing)
            {
                weight=Mathf.Min(1,weight+dt/.23f);
                if(pose!=null)pose.Apply(Mathf.SmoothStep(0,1,weight));
                if(weight<1)return;
                if(departing){stage=Stage.Holding;return;}
                stage=Stage.Exchange;exchange=0;
                CombatVfx.Burst(transform.position+Vector3.up*.9f,actor!=null&&actor.character=="Taren"?new Color(1,.65f,.2f):Color.cyan,"form");
            }
            else if(stage==Stage.Exchange)
            {
                exchange+=dt;
                if(exchange<.05f)return;
                bool fromFlight=Visible==flight;
                if(pose!=null)pose.Restore();Show(requested);
                if(Visible==flight){flight.transform.localPosition=Vector3.up*.8f;flight.transform.localRotation=Quaternion.identity;}
                pose=Prepare(Visible,fromFlight||Visible==flight,true);weight=1;if(pose!=null)pose.Apply(1);
                stage=Stage.Opening;exchange=.05f;
            }
            else
            {
                if(exchange>0){exchange-=dt;return;}
                weight=Mathf.Max(0,weight-dt/.27f);
                if(pose!=null)pose.Apply(Mathf.SmoothStep(0,1,weight));
                if(weight>0)return;
                if(pose!=null)pose.Restore();pose=null;Shaping=false;
                Debug.Log("FORM "+requested);
            }
        }
        void OnDown(Health _,DamagePacket packet)
        {
            // Defeat presentation subscribes after this component. Give it the
            // one full-size body matching the active motor/collision rules.
            if(!Shaping)return;
            if(pose!=null)pose.Restore();Show(requested);
            if(Visible==flight){flight.transform.localPosition=Vector3.up*.8f;flight.transform.localRotation=Quaternion.identity;}
            pose=null;weight=exchange=0;Shaping=false;departing=false;
        }
    }
}
