using UnityEngine;
namespace Lattice.World
{
    public sealed class Membrane:MonoBehaviour
    {
        public bool Open{get;private set;}
        Coroutine dissolve;
        public void SetOpen(bool open)
        {
            bool changed=Open!=open;Open=open;
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=!open;
            if(Application.isPlaying&&open&&changed&&gameObject.activeInHierarchy){if(dissolve!=null)StopCoroutine(dissolve);dissolve=StartCoroutine(Dissolve());}
            else foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=!open;
        }
        System.Collections.IEnumerator Dissolve()
        {
            var renderers=GetComponentsInChildren<Renderer>();var block=new MaterialPropertyBlock();
            Lattice.Combat.CombatVfx.Burst(transform.position,Color.cyan,"shape");
            for(float t=0;t<.5f;t+=Time.deltaTime)
            {
                if(!Open)yield break;
                foreach(var r in renderers){r.GetPropertyBlock(block);block.SetVector("_DissolveParams",new Vector4(t/.5f,3,0,0));r.SetPropertyBlock(block);}yield return null;
            }
            foreach(var r in renderers){r.enabled=false;r.SetPropertyBlock(null);}dissolve=null;
        }
    }
}
