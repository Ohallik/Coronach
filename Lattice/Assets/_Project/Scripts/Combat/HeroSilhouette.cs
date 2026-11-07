using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>The hero's shape, drawn faintly over a big body standing in front of them for as long as that body asks. One more material on the hero's own
    /// meshes, which draws only where something nearer covers them, added and taken off on the change and never touched between.</summary>
    [DefaultExecutionOrder(950)]
    public sealed class HeroSilhouette:MonoBehaviour
    {
        static Material shared;
        readonly List<(Renderer body,Material[] usual)> shown=new();
        int asked=-10;bool on;
        public static void Show(CombatActor hero)
        {
            if(!hero.TryGetComponent<HeroSilhouette>(out var silhouette))silhouette=hero.gameObject.AddComponent<HeroSilhouette>();
            silhouette.asked=Time.frameCount;
        }
        void LateUpdate()
        {
            bool wanted=Time.frameCount-asked<=1;
            if(wanted==on)return;
            on=wanted;
            if(wanted)Put();else Restore();
        }
        void Put()
        {
            if(shared==null){var shader=Resources.Load<Shader>("Effects/HeroSilhouette");if(shader==null)return;shared=new Material(shader){name="hero silhouette (runtime)"};}
            foreach(var body in GetComponentsInChildren<Renderer>())
            {
                if(body is not SkinnedMeshRenderer&&body is not MeshRenderer||!body.enabled)continue;
                var usual=body.sharedMaterials;var with=new Material[usual.Length+1];usual.CopyTo(with,0);with[usual.Length]=shared;
                body.sharedMaterials=with;shown.Add((body,usual));
            }
        }
        void Restore()
        {
            foreach(var (body,usual) in shown)if(body!=null)body.sharedMaterials=usual;
            shown.Clear();
        }
        void OnDisable(){on=false;Restore();}
    }
}
