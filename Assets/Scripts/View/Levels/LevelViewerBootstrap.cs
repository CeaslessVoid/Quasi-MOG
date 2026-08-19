using UnityEngine;
using Unity.Netcode;
using Networking;
using Entities;

namespace RoomGen
{
    public class LevelViewerBootstrap : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 20f;
        [SerializeField] private NetworkedLevelSync networkedLevelSyncPrefab;
        [SerializeField] private string playerEntityDefName = "Human";

        private LevelVisuals _visuals;

        private void Start()
        {
            var appState = GameManager.EnsureExists();

            if (GameManager.Instance.IsMultiplayer && NetworkManager.Singleton != null)
                StartMultiplayer();
            else
                StartSingleplayer();
        }

        private void StartSingleplayer()
        {
            SpawnLocalCamera();

            var levelGO = new GameObject("LevelView");
            _visuals = levelGO.AddComponent<LevelVisuals>();

            var generator = levelGO.AddComponent<RoomGenerator>();
            generator.Generate();
            _visuals.Rebuild(generator.Grid);

            var entities = EntitySpawner.SpawnPlayers(generator.Grid, playerEntityDefName, 1, _visuals.CellSize);
            if (entities.Count > 0)
                SimpleTopDownCameraController.Instance?.CenterOn(entities[0].transform.position);
        }

        private void StartMultiplayer()
        {
            SpawnLocalCamera();

            var levelGO = new GameObject("LevelView");
            _visuals = levelGO.AddComponent<LevelVisuals>();

            if (NetworkManager.Singleton.IsServer)
            {
                if (networkedLevelSyncPrefab == null)
                {
                    Debug.LogError("LevelViewerBootstrap: networkedLevelSyncPrefab is not assigned. Multiplayer level generation cannot start.");
                    return;
                }

                var sync = Instantiate(networkedLevelSyncPrefab);
                sync.OnLevelReady += HandleLevelReady;
                sync.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                WaitForLevelSync();
            }
        }

        private void WaitForLevelSync()
        {
            if (NetworkedLevelSync.Instance != null)
            {
                NetworkedLevelSync.Instance.OnLevelReady += HandleLevelReady;
                if (NetworkedLevelSync.Instance.Grid != null)
                    HandleLevelReady(NetworkedLevelSync.Instance.Grid);
            }
            else
            {
                Invoke(nameof(WaitForLevelSync), 0.1f);
            }
        }

        private void HandleLevelReady(LevelGrid grid) => _visuals.Rebuild(grid);

        private void SpawnLocalCamera()
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
            camController.Configure(cam);
        }
    }
}