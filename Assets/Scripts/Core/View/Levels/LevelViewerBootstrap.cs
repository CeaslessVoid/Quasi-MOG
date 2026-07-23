using UnityEngine;

namespace RoomGen
{
    public class LevelViewerBootstrap : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 20f;

        private void Start()
        {
            GameManager.EnsureExists();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Level Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.transform.position = new Vector3(orthographicSize, orthographicSize, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.03f);

            var camController = cam.gameObject.GetComponent<SimpleTopDownCameraController>();
            if (camController == null) camController = cam.gameObject.AddComponent<SimpleTopDownCameraController>();
            camController.Configure(cam);

            var levelGO = new GameObject("LevelView");
            var visuals = levelGO.AddComponent<LevelVisuals>();

            var generator = levelGO.AddComponent<RoomGenerator>();
            generator.Generate();
            visuals.Rebuild(generator.Grid);
        }
    }
}