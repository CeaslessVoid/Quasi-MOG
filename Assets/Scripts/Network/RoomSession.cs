using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking
{
    public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
    {
        public ulong clientId;
        public FixedString32Bytes playerName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
        }

        public bool Equals(PlayerInfo other) => clientId == other.clientId;
    }

    public class RoomSession : NetworkBehaviour
    {
        public static RoomSession Instance { get; private set; }

        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private string gameSceneName = "Game";

        private readonly NetworkList<PlayerInfo> _players = new NetworkList<PlayerInfo>();

        public event Action OnPlayersChanged;

        public int MaxPlayers => maxPlayers;
        public int PlayerCount => _players.Count;

        private void Awake()
        {
            Instance = this;
            _players.OnListChanged += HandleListChanged;
        }

        private void HandleListChanged(NetworkListEvent<PlayerInfo> _) => OnPlayersChanged?.Invoke();

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

                if (!_players.Contains(new PlayerInfo { clientId = NetworkManager.LocalClientId }))
                    _players.Add(new PlayerInfo { clientId = NetworkManager.LocalClientId, playerName = "Host" });
            }

            SubmitNameRpc(App.AppState.EnsureExists().LocalPlayerName);
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager != null && IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer) return;
            if (clientId == NetworkManager.LocalClientId) return;
            if (_players.Count >= maxPlayers) return;
            _players.Add(new PlayerInfo { clientId = clientId, playerName = "Player" });
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer) return;
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].clientId != clientId) continue;
                _players.RemoveAt(i);
                return;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitNameRpc(string playerName, RpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].clientId != senderId) continue;
                var updated = _players[i];
                updated.playerName = playerName;
                _players[i] = updated;
                return;
            }
        }

        public PlayerInfo GetPlayer(int index) => _players[index];

        public void ServerLoadGameScene()
        {
            if (!IsServer) return;
            NetworkManager.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }
}