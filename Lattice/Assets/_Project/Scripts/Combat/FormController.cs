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
        public void Set(BodyForm form)
        {
            if(!Shaping&&Current==form)
            {
                var visual=form==BodyForm.Natural?natural:form==BodyForm.Shaped?shaped:flight;
                if(visual!=null&&visual.activeSelf)return;
            }
            if(change!=null)StopCoroutine(change);change=StartCoroutine(Transition(form));
        }
        IEnumerator Transition(BodyForm form)
        {
            Shaping=true;
            var actor=GetComponent<CombatActor>();var color=actor!=null&&actor.character=="Taren"?new Color(1,.65f,.2f):Color.cyan;
            CombatVfx.Burst(transform.position+Vector3.up,color,"shape");
            var previous=natural!=null&&natural.activeSelf?natural:shaped!=null&&shaped.activeSelf?shaped:flight;
            float time=0;
            while(time<.3f){if(!GameTime.Paused){time+=Time.unscaledDeltaTime;if(previous!=null)previous.transform.localScale=Vector3.one*Mathf.Lerp(1,.15f,Mathf.SmoothStep(0,1,time/.3f));}yield return null;}
            foreach(var visual in new[]{natural,shaped,flight})if(visual!=null)visual.transform.localScale=Vector3.one;
            Current=form;
            if(natural!=null)natural.SetActive(form==BodyForm.Natural);
            if(shaped!=null)shaped.SetActive(form==BodyForm.Shaped);
            if(flight!=null){flight.transform.localRotation=Quaternion.identity;flight.transform.localPosition=new Vector3(0,.8f,0);flight.SetActive(form==BodyForm.Flight||form==BodyForm.CivilFlight);}
            foreach(var vanes in GetComponentsInChildren<GeneratedVanes>(true))vanes.SetForm(form);
            var incoming=form==BodyForm.Natural?natural:form==BodyForm.Shaped?shaped:flight;time=0;
            while(time<.3f){if(!GameTime.Paused){time+=Time.unscaledDeltaTime;if(incoming!=null)incoming.transform.localScale=Vector3.one*Mathf.Lerp(.15f,1,Mathf.SmoothStep(0,1,time/.3f));}yield return null;}
            if(incoming!=null)incoming.transform.localScale=Vector3.one;
            Debug.Log("FORM "+form);Shaping=false;change=null;
        }
    }
}
