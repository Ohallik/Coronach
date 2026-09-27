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
            foreach(var obstacle in GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>())obstacle.enabled=!open;
            if(!open)
            {
                if(dissolve!=null){StopCoroutine(dissolve);dissolve=null;}
                foreach(var r in GetComponentsInChildren<Renderer>()){r.enabled=true;ResetDissolve(r);}
            }
            else if(Application.isPlaying&&changed&&gameObject.activeInHierarchy)
            {
                if(dissolve!=null)StopCoroutine(dissolve);
                foreach(var r in GetComponentsInChildren<Renderer>())r.enabled=true;
                dissolve=StartCoroutine(Dissolve());
            }
            else if(dissolve==null)foreach(var r in GetComponentsInChildren<Renderer>()){r.enabled=false;ResetDissolve(r);}
        }
        static void ResetDissolve(Renderer renderer)
        {
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            block.SetVector("_DissolveParams",Vector4.zero);renderer.SetPropertyBlock(block);
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
            foreach(var r in renderers){r.enabled=false;ResetDissolve(r);}dissolve=null;
        }
    }
}
