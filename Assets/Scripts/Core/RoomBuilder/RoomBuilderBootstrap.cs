using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    /// <summary>
    /// Drop this on any empty GameObject in an otherwise empty scene and press Play - it
    /// sets up a top-down orthographic camera and the builder itself, fully wired. No
    /// manual scene assembly required.
    /// </summary>
    public class RoomBuilderBootstrap : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 12f;
        [SerializeField] private float panelWidth = 300f;

        [Header("Wall Tiles - assign ONE of the two below")]
        [SerializeField] private TileBase[] wallTiles = new TileBase[16];
        [SerializeField] private Sprite[] wallSprites = new Sprite[16];
        [SerializeField] private Sprite floorSprite;

        private void Awake()
        {
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
            visuals.ConfigureTextures(wallTiles, wallSprites, floorSprite);
            var controller = builderGO.AddComponent<RoomBuilderController>();
            controller.Configure(cam, visuals);
        }
    }
}