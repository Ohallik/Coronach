using System.Collections;
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
            if(change!=null)StopCoroutine(change);change=StartCoroutine(Transition(form));
        }
        IEnumerator Transition(BodyForm form)
        {
            Shaping=true;CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,"shape");yield return new WaitForSecondsRealtime(.3f);Current=form;
            if(natural!=null)natural.SetActive(form==BodyForm.Natural);
            if(shaped!=null)shaped.SetActive(form==BodyForm.Shaped);
            if(flight!=null){flight.SetActive(form==BodyForm.Flight||form==BodyForm.CivilFlight);flight.transform.localRotation=Quaternion.Euler(90,0,0);}
            foreach(var vane in GetComponentsInChildren<Transform>(true))if(vane.name.StartsWith("Vane_"))
                vane.localRotation=Quaternion.Euler(0,0,(vane.name.EndsWith("L")?-1:1)*(form==BodyForm.Flight?55:form==BodyForm.CivilFlight?25:5));
            Debug.Log("FORM "+form);yield return new WaitForSecondsRealtime(.3f);Shaping=false;
        }
    }
}
