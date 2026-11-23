using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.World
{
    /// <summary>
    /// A flight zone's haze measured down from the flight plane, not out from the lens. The zone's linear fog is tuned for the flight camera at its play distance (<see cref="lens"/> metres
    /// back along its axis to the plane): there it lies on the ground far under the ships and leaves the plane clear. A view drawn back to show something (a boss on show from her door, her
    /// going, a fight framed wide) stands two and three hundred metres back, and the same fog then covered everything: Sorrel's second sky from the valley's head was a flat pink sheet with
    /// the shelves and Mother of Doors in it as pale cut-outs (whole-game capture, November 20, s63/010). So the fog is moved out by however much further than the play distance the game's
    /// camera stands from the plane: the plane stays as clear as in play, and what lies under it is hazed by its own depth, the same at every zoom. Never nearer than the zone's own numbers
    /// (a closer view, the ground camera on a shelf, keeps them). Set after the camera has moved each frame, as the White Reach's whiteout sets its fog, and again as the game's camera
    /// begins to draw; a still drawn in the editor has it set for its own camera through <see cref="ApplyFor"/>.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(10000)]
    public sealed class PlaneHaze:MonoBehaviour
    {
        /// <summary>The flight plane's height, the zone's fog from and to (its ZoneLook numbers) and the flight camera's play distance to the plane those were tuned at.</summary>
        public float plane=1,start=70,end=330,lens=26;
        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering+=Before;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving+=Saving;
#endif
        }
        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering-=Before;
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving-=Saving;
#endif
            if(Ours)Set(0);
        }
        /// <summary>The fog is the active scene's: this zone's only while it is the active one (a builder or a still run opens the next zone over it).</summary>
        bool Ours=>gameObject.scene==UnityEngine.SceneManagement.SceneManager.GetActiveScene();
#if UNITY_EDITOR
        /// <summary>A scene is saved with the zone's own numbers, whatever an editor still last drew it at.</summary>
        void Saving(UnityEngine.SceneManagement.Scene scene,string path){if(scene==gameObject.scene&&Ours)Set(0);}
#endif
        void LateUpdate(){if(Application.isPlaying&&Ours&&Camera.main!=null)ApplyFor(Camera.main);}
        void Before(ScriptableRenderContext context,Camera view)
        {
            if(view==null||view.cameraType==CameraType.Preview||view.cameraType==CameraType.Reflection||!Ours)return;
            if(Application.isPlaying&&view!=Camera.main)return;
            ApplyFor(view);
        }
        /// <summary>Sets the zone's fog as it stands for this camera.</summary>
        public void ApplyFor(Camera view)=>Set(Beyond(view.transform.position,view.transform.forward));
        /// <summary>How much further than the play distance a lens at <paramref name="at"/> looking along <paramref name="forward"/> stands from the plane, measured along its own axis.</summary>
        public float Beyond(Vector3 at,Vector3 forward)
        {
            if(forward.y>-.05f)return 0;
            float along=(at.y-plane)/-forward.y;return Mathf.Max(0,along-lens);
        }
        void Set(float beyond){RenderSettings.fogStartDistance=start+beyond;RenderSettings.fogEndDistance=end+beyond;}
    }
}
