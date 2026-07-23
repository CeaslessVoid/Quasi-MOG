using UnityEngine;

namespace RoomGen
{
    public class RoomBuilderBootstrap : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 12f;
        [SerializeField] private float panelWidth = 300f;

        private void Awake()
        {
            GameManager.EnsureExists();

            var cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Builder Camera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }

            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            cam.transform.position = new Vector3(orthographicSize * 0.8f, orthographicSize * 0.6f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.08f);

            var camController = cam.gameObject.GetComponent<SimpleTopDownCameraController>();
            if (camController == null) camController = cam.gameObject.AddComponent<SimpleTopDownCameraController>();
            camController.Configure(cam, panelWidth);

            var builderGO = new GameObject("RoomBuilder");
            var visuals = builderGO.AddComponent<RoomBuilderVisuals>();
            var controller = builderGO.AddComponent<RoomBuilderController>();
            controller.Configure(cam, visuals);
        }
    }
}