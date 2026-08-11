using System;
using Unity.Netcode;
using UnityEngine;
using RoomGen;

namespace Networking
{
    public class NetworkedLevelSync : NetworkBehaviour
    {
        public static NetworkedLevelSync Instance { get; private set; }

        public event Action<LevelGrid> OnLevelReady;

        private LevelGrid _grid;
        public LevelGrid Grid => _grid;

        private void Awake() => Instance = this;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                var generator = FindOrCreateGenerator();
                generator.Generate();
                _grid = generator.Grid;
                OnLevelReady?.Invoke(_grid);

                SendLevelToAllClients();
                NetworkManager.OnClientConnectedCallback += HandleLateJoin;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientConnectedCallback -= HandleLateJoin;
            if (Instance == this) Instance = null;
        }

        private RoomGenerator FindOrCreateGenerator()
        {
            var existing = FindFirstObjectByType<RoomGenerator>();
            if (existing != null) return existing;

            var go = new GameObject("RoomGenerator");
            return go.AddComponent<RoomGenerator>();
        }

        private void SendLevelToAllClients()
        {
            byte[] payload = LevelNetworkSerializer.Serialize(_grid);
            ReceiveLevelClientRpc(payload);
        }

        private void HandleLateJoin(ulong clientId)
        {
            if (_grid == null) return;
            if (clientId == NetworkManager.LocalClientId) return;

            byte[] payload = LevelNetworkSerializer.Serialize(_grid);
            var sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };
            ReceiveLevelClientRpc(payload, sendParams);
        }

        [ClientRpc]
        private void ReceiveLevelClientRpc(byte[] payload, ClientRpcParams rpcParams = default)
        {
            if (IsServer) return;
            _grid = LevelNetworkSerializer.Deserialize(payload);
            OnLevelReady?.Invoke(_grid);
        }
    }
}
