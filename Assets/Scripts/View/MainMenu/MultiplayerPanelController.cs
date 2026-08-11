using Networking;
using Networking.App;
using RoomGen;
using Save;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
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

        [Header("Host Save Selection")]
        [SerializeField] private GameObject hostSaveSelectPanel;
        [SerializeField] private SaveSlotView[] hostSaveSlotViews;
        [SerializeField] private GameObject hostNewGameDialog;
        [SerializeField] private TMP_InputField hostNewGameNameInput;
        [SerializeField] private Button hostNewGameConfirmButton;
        [SerializeField] private Button hostNewGameCancelButton;

        [Header("Host Save Selection: Delete")]
        [SerializeField] private GameObject hostDeleteConfirmDialog;
        [SerializeField] private TMP_Text hostDeleteConfirmText;
        [SerializeField] private Button hostDeleteConfirmButton;
        [SerializeField] private Button hostDeleteCancelButton;

        private readonly List<RoomListItemView> _roomRows = new List<RoomListItemView>();
        private readonly List<LobbyPlayerRowView> _playerRows = new List<LobbyPlayerRowView>();
        private float _refreshTimer;
        private string _pendingRoomName;
        private int _hostPendingSlotIndex = -1;
        private int _hostPendingDeleteIndex = -1;

        private void Awake()
        {
            backButton.onClick.AddListener(HandleBack);
            createRoomButton.onClick.AddListener(HandleCreateRoom);
            leaveLobbyButton.onClick.AddListener(HandleLeaveLobby);
            startGameButton.onClick.AddListener(HandleStartGame);

            hostNewGameConfirmButton.onClick.AddListener(ConfirmHostNewGame);
            hostNewGameCancelButton.onClick.AddListener(() => hostNewGameDialog.SetActive(false));

            hostDeleteConfirmButton.onClick.AddListener(ConfirmHostDelete);
            hostDeleteCancelButton.onClick.AddListener(() => hostDeleteConfirmDialog.SetActive(false));
        }

        private void OnEnable()
        {
            ShowBrowse();
            discovery.StartBrowsing();
        }

        private void OnDisable()
        {
            discovery.StopAll();
        }

        private void Update()
        {
            if (!browsePanel.activeSelf) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            if (_refreshTimer > 0f) return;
            _refreshTimer = 0.5f;
            RefreshRoomList();
        }

        private void ShowBrowse()
        {
            browsePanel.SetActive(true);
            lobbyPanel.SetActive(false);
            hostSaveSelectPanel.SetActive(false);
        }

        private void ShowLobby()
        {
            browsePanel.SetActive(false);
            lobbyPanel.SetActive(true);
            hostSaveSelectPanel.SetActive(false);

            var nm = NetworkManager.Singleton;
            bool isHost = nm != null && nm.IsHost;
            startGameButton.gameObject.SetActive(isHost);
            waitingForHostText.gameObject.SetActive(!isHost);

            if (RoomSession.Instance != null)
                RoomSession.Instance.OnPlayersChanged += RefreshLobbyPlayers;
            RefreshLobbyPlayers();
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

        private void HandleStartGame()
        {
            var nm = NetworkManager.Singleton;
            if (RoomSession.Instance == null || nm == null || !nm.IsHost) return;

            lobbyPanel.SetActive(false);
            hostSaveSelectPanel.SetActive(true);
            RefreshHostSaveSlots();
        }

        private void RefreshHostSaveSlots()
        {
            var slots = SaveManager.GetSlots();
            for (int i = 0; i < hostSaveSlotViews.Length && i < slots.Length; i++)
            {
                int index = i;
                hostSaveSlotViews[i].Bind(index, () => HandleHostSlotClicked(index), () => HandleHostDeleteRequested(index));
                hostSaveSlotViews[i].SetInfo(slots[i]);
            }
        }

        private void HandleHostSlotClicked(int index)
        {
            var info = SaveManager.GetSlots()[index];
            if (info.hasSave)
            {
                LaunchWithSlot(index, info.saveName, isNewGame: false);
            }
            else
            {
                _hostPendingSlotIndex = index;
                if (hostNewGameNameInput != null) hostNewGameNameInput.text = $"Save {index + 1}";
                hostNewGameDialog.SetActive(true);
            }
        }

        private void ConfirmHostNewGame()
        {
            if (_hostPendingSlotIndex < 0) return;
            string name = hostNewGameNameInput != null ? hostNewGameNameInput.text : null;
            SaveManager.CreateNewGame(_hostPendingSlotIndex, name);
            hostNewGameDialog.SetActive(false);
            LaunchWithSlot(_hostPendingSlotIndex, name, isNewGame: true);
        }

        private void HandleHostDeleteRequested(int index)
        {
            var info = SaveManager.GetSlots()[index];
            if (!info.hasSave) return;

            _hostPendingDeleteIndex = index;
            if (hostDeleteConfirmText != null)
            {
                string label = string.IsNullOrEmpty(info.saveName) ? $"Save {index + 1}" : info.saveName;
                hostDeleteConfirmText.text = $"Delete '{label}'? This cannot be undone.";
            }
            hostDeleteConfirmDialog.SetActive(true);
        }

        private void ConfirmHostDelete()
        {
            if (_hostPendingDeleteIndex < 0) return;
            SaveManager.DeleteSlot(_hostPendingDeleteIndex);
            _hostPendingDeleteIndex = -1;
            hostDeleteConfirmDialog.SetActive(false);
            RefreshHostSaveSlots();
        }

        private void LaunchWithSlot(int index, string saveName, bool isNewGame)
        {
            var app = AppState.EnsureExists();
            app.ConfigureMultiplayerHost(index, saveName, isNewGame);
            discovery.StopAdvertising();
            RoomSession.Instance.ServerLoadGameScene();
        }

        private void HandleLeaveLobby()
        {
            if (RoomSession.Instance != null)
                RoomSession.Instance.OnPlayersChanged -= RefreshLobbyPlayers;

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
