using System.Collections;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class FormController:MonoBehaviour
    {
        public GameObject natural,shaped,flight;
        public BodyForm Current{get;private set;}
        public bool Shaping{get;private set;}
        Coroutine change;
        BodyForm requested;
        GameObject Visual(BodyForm form)=>form==BodyForm.Natural?natural:form==BodyForm.Shaped?shaped:flight;
        GameObject Visible=>natural!=null&&natural.activeSelf?natural:shaped!=null&&shaped.activeSelf?shaped:flight!=null&&flight.activeSelf?flight:null;
        public void Set(BodyForm form)
        {
            if(Shaping&&Visual(requested)==Visual(form))
            {
                requested=form;
                if(Visible==Visual(form)&&Current!=form)Show(form,false);
                return;
            }
            if(!Shaping&&Current==form)
            {
                var visual=Visual(form);
                if(visual!=null&&visual.activeSelf)return;
            }
            requested=form;
            if(change!=null){StopCoroutine(change);change=null;}
            var previous=Visible;var incoming=Visual(form);
            // Civil/combat flight are different rules for the same hull. Keep
            // its live bank/roll and scale instead of transforming it again.
            if(previous==incoming&&incoming!=null&&incoming.transform.localScale==Vector3.one)
            {Show(form,false);Shaping=false;Debug.Log("FORM "+form);return;}
            change=StartCoroutine(Transition(previous,incoming));
        }
        void Show(BodyForm form,bool changedBody)
        {
            var incoming=Visual(form);Current=form;
            foreach(var visual in new[]{natural,shaped,flight})if(visual!=null)
            {
                if(visual!=incoming)visual.transform.localScale=Vector3.one;
                visual.SetActive(visual==incoming);
            }
            if(changedBody&&incoming==flight&&flight!=null)
            {flight.transform.localRotation=Quaternion.identity;flight.transform.localPosition=new Vector3(0,.8f,0);}
            foreach(var vanes in GetComponentsInChildren<GeneratedVanes>(true))vanes.SetForm(form);
        }
        static IEnumerator Resize(GameObject visual,Vector3 destination)
        {
            if(visual==null)yield break;
            var start=visual.transform.localScale;float time=0;
            while(time<.3f)
            {
                if(!GameTime.Paused)
                {time+=Time.unscaledDeltaTime;visual.transform.localScale=Vector3.Lerp(start,destination,Mathf.SmoothStep(0,1,time/.3f));}
                yield return null;
            }
            visual.transform.localScale=destination;
        }
        IEnumerator Transition(GameObject previous,GameObject incoming)
        {
            Shaping=true;
            var actor=GetComponent<CombatActor>();var color=actor!=null&&actor.character=="Taren"?new Color(1,.65f,.2f):Color.cyan;
            CombatVfx.Burst(transform.position+Vector3.up,color,"shape");
            if(previous!=incoming)
            {
                yield return Resize(previous,Vector3.one*.15f);
                if(incoming!=null)incoming.transform.localScale=Vector3.one*.15f;
                Show(requested,true);
            }
            else Show(requested,false);
            // Reversing to the still-visible body grows from its current scale;
            // an interrupted shrink must not jump back to one or shrink twice.
            yield return Resize(incoming,Vector3.one);
            Debug.Log("FORM "+requested);Shaping=false;change=null;
        }
    }
}
