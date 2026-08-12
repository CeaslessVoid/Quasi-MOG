using System;
using System.Collections;
using System.Collections.Generic;
using Networking;
using Networking.App;
using RoomGen;
using Save;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class MultiplayerPanelController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private MainMenuController mainMenu;
        [SerializeField] private Button backButton;

        [Header("Browse")]
        [SerializeField] private GameObject browsePanel;
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField createRoomNameInput;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button refreshRoomsButton;
        [SerializeField] private Transform roomListContent;
        [SerializeField] private RoomListItemView roomListItemPrefab;
        [SerializeField] private LanRoomDiscovery discovery;

        [Header("Networking Prefabs")]
        [SerializeField] private RoomSession roomSessionPrefab;

        [Header("Lobby")]
        [SerializeField] private GameObject lobbyPanel;
        [SerializeField] private TMP_Text lobbyRoomNameText;
        [SerializeField] private Transform lobbyPlayerListContent;
        [SerializeField] private LobbyPlayerRowView lobbyPlayerRowPrefab;
        [SerializeField] private Button startGameButton;
        [SerializeField] private TMP_Text waitingForHostText;
        [SerializeField] private Button leaveLobbyButton;

        [Header("Shared Save Slot Select (Host)")]
        [SerializeField] private SaveSlotSelectController saveSlotSelect;

        private readonly List<RoomListItemView> _roomRows = new List<RoomListItemView>();
        private readonly List<LobbyPlayerRowView> _playerRows = new List<LobbyPlayerRowView>();
        private string _pendingRoomName;

        private Coroutine _waitForSessionRoutine;
        private RoomSession _subscribedSession;

        private void Awake()
        {
            backButton.onClick.AddListener(HandleBack);
            createRoomButton.onClick.AddListener(HandleCreateRoom);
            refreshRoomsButton.onClick.AddListener(RefreshRoomList);
            leaveLobbyButton.onClick.AddListener(HandleLeaveLobby);
            startGameButton.onClick.AddListener(HandleStartGame);

            if (usernameInput != null)
                usernameInput.text = AppState.EnsureExists().LocalPlayerName;
        }

        private void OnEnable()
        {
            ShowBrowse();
            discovery.StartBrowsing();
        }

        private void OnDisable()
        {
            discovery.StopAll();
            StopWaitingForSession();
        }

        private void ShowBrowse()
        {
            browsePanel.SetActive(true);
            lobbyPanel.SetActive(false);
            saveSlotSelect.Close();
            RefreshRoomList();
        }

        private void ShowLobby()
        {
            browsePanel.SetActive(false);
            lobbyPanel.SetActive(true);
            saveSlotSelect.Close();

            var nm = NetworkManager.Singleton;
            bool isHost = nm != null && nm.IsHost;
            startGameButton.gameObject.SetActive(isHost);
            waitingForHostText.gameObject.SetActive(!isHost);

            ClearPlayerRows();
            StopWaitingForSession();
            _waitForSessionRoutine = StartCoroutine(WaitForRoomSessionThenSubscribe());
        }
        private void RefreshRoomList()
        {
            var rooms = discovery.DiscoveredRooms;
            EnsureRowCount(rooms.Count);

            int i = 0;
            foreach (var room in rooms)
            {
                _roomRows[i].SetInfo(room, () => HandleJoinRoom(room));
                _roomRows[i].gameObject.SetActive(true);
                i++;
            }
            for (; i < _roomRows.Count; i++) _roomRows[i].gameObject.SetActive(false);
        }

        private void EnsureRowCount(int count)
        {
            while (_roomRows.Count < count)
                _roomRows.Add(Instantiate(roomListItemPrefab, roomListContent));
        }

        private void HandleCreateRoom()
        {
            var app = AppState.EnsureExists();
            app.SetLocalPlayerName(usernameInput != null ? usernameInput.text : "Player");

            _pendingRoomName = createRoomNameInput != null && !string.IsNullOrWhiteSpace(createRoomNameInput.text)
                ? createRoomNameInput.text.Trim()
                : $"{app.LocalPlayerName}'s Room";

            if (!NetworkGameLauncher.StartHost())
            {
                Debug.LogError("MultiplayerPanelController: failed to start host.");
                return;
            }

            app.ConfigureMultiplayerHost(-1, null, isNewGame: false);

            if (roomSessionPrefab != null)
            {
                var session = Instantiate(roomSessionPrefab);
                session.GetComponent<NetworkObject>().Spawn();
            }
            else
            {
                Debug.LogError("MultiplayerPanelController: roomSessionPrefab is not assigned.");
            }

            discovery.StartAdvertising(_pendingRoomName, NetworkGameLauncher.DefaultPort,
                RoomSession.Instance != null ? RoomSession.Instance.MaxPlayers : 4,
                () => RoomSession.Instance != null ? RoomSession.Instance.PlayerCount : 1);

            lobbyRoomNameText.text = _pendingRoomName;
            ShowLobby();
        }

        private void HandleJoinRoom(RoomInfo room)
        {
            var app = AppState.EnsureExists();
            app.SetLocalPlayerName(usernameInput != null ? usernameInput.text : "Player");
            app.ConfigureMultiplayerClient();

            if (!NetworkGameLauncher.StartClient(room.hostAddress, room.hostPort))
            {
                Debug.LogError("MultiplayerPanelController: failed to start client.");
                return;
            }

            lobbyRoomNameText.text = room.roomName;
            ShowLobby();
        }
        private IEnumerator WaitForRoomSessionThenSubscribe()
        {
            const float timeout = 10f;
            float elapsed = 0f;

            while (RoomSession.Instance == null)
            {
                if (elapsed >= timeout)
                {
                    Debug.LogWarning("MultiplayerPanelController: timed out waiting for RoomSession to spawn.");
                    yield break;
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _subscribedSession = RoomSession.Instance;
            _subscribedSession.OnPlayersChanged += RefreshLobbyPlayers;
            RefreshLobbyPlayers();
        }

        private void StopWaitingForSession()
        {
            if (_waitForSessionRoutine != null)
            {
                StopCoroutine(_waitForSessionRoutine);
                _waitForSessionRoutine = null;
            }

            if (_subscribedSession != null)
            {
                _subscribedSession.OnPlayersChanged -= RefreshLobbyPlayers;
                _subscribedSession = null;
            }
        }

        private void RefreshLobbyPlayers()
        {
            if (RoomSession.Instance == null) return;
            int count = RoomSession.Instance.PlayerCount;
            while (_playerRows.Count < count)
                _playerRows.Add(Instantiate(lobbyPlayerRowPrefab, lobbyPlayerListContent));

            for (int i = 0; i < _playerRows.Count; i++)
            {
                if (i < count)
                {
                    _playerRows[i].gameObject.SetActive(true);
                    _playerRows[i].SetInfo(RoomSession.Instance.GetPlayer(i));
                }
                else
                {
                    _playerRows[i].gameObject.SetActive(false);
                }
            }
        }

        private void ClearPlayerRows()
        {
            foreach (var row in _playerRows) row.gameObject.SetActive(false);
        }
        private void HandleStartGame()
        {
            var nm = NetworkManager.Singleton;
            if (RoomSession.Instance == null || nm == null || !nm.IsHost) return;

            lobbyPanel.SetActive(false);
            saveSlotSelect.Open(HandleHostLoadSlot, HandleHostNewGame);
        }

        private void HandleHostLoadSlot(int slotIndex)
        {
            var info = SaveManager.GetSlots()[slotIndex];
            LaunchWithSlot(slotIndex, info.saveName, isNewGame: false);
        }

        private void HandleHostNewGame(int slotIndex, string saveName)
        {
            LaunchWithSlot(slotIndex, saveName, isNewGame: true);
        }

        private void LaunchWithSlot(int index, string saveName, bool isNewGame)
        {
            var app = AppState.EnsureExists();
            app.ConfigureMultiplayerHost(index, saveName, isNewGame);
            discovery.StopAdvertising();
            saveSlotSelect.Close();
            RoomSession.Instance.ServerLoadGameScene();
        }

        private void HandleLeaveLobby()
        {
            StopWaitingForSession();
            discovery.StopAll();
            NetworkGameLauncher.Shutdown();
            ShowBrowse();
        }

        private void HandleBack()
        {
            HandleLeaveLobby();
            mainMenu.ShowRoot();
        }
    }
}