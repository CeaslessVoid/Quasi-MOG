using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoomGen
{
    /// <summary>
    /// Drop this on any empty GameObject in an otherwise empty scene and press Play - sets
    /// up a camera (with pan/zoom), generates a level from whatever's in
    /// Assets/StreamingAssets/Rooms, and renders it with real textures via LevelVisuals.
    /// This is "see the generated map in the actual game camera," not the Scene-view-only
    /// gizmo debug draw RoomGenerator also still has.
    /// </summary>
    public class LevelViewerBootstrap : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 20f;

        [Header("Wall Tiles - assign ONE of the two below")]
        [SerializeField] private TileBase[] wallTiles = new TileBase[16];
        [SerializeField] private Sprite[] wallSprites = new Sprite[16];
        [SerializeField] private Sprite floorSprite;

        private void Start()
        {
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
            camController.Configure(cam); // no reserved panel here - full screen is world view

            var levelGO = new GameObject("LevelView");
            var visuals = levelGO.AddComponent<LevelVisuals>();
            visuals.Configure(wallTiles, wallSprites, floorSprite);

            var generator = levelGO.AddComponent<RoomGenerator>();
            generator.Generate();
            visuals.Rebuild(generator.Grid);
        }
    }
}