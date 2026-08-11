using System;
using Unity.Netcode;
using UnityEngine;
using RoomGen;

namespace Networking
{
    public class NetworkedLevelSync : NetworkBehaviour
    {
        public static NetworkedLevelSync Instance { get; private set; }

        private const int ChunkSize = 500;

        public event Action<LevelGrid> OnLevelReady;

        private LevelGrid _grid;
        public LevelGrid Grid => _grid;

        private byte[] _receiveBuffer;
        private int _receiveOffset;
        private int _receiveTotalChunks;
        private int _receiveChunkCount;

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
            SendChunks(payload, null);
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
            SendChunks(payload, sendParams);
        }

        private void SendChunks(byte[] payload, ClientRpcParams? targetParams)
        {
            int total = Mathf.Max(1, Mathf.CeilToInt(payload.Length / (float)ChunkSize));

            for (int i = 0; i < total; i++)
            {
                int offset = i * ChunkSize;
                int length = Mathf.Min(ChunkSize, payload.Length - offset);
                var chunk = new byte[length];
                Array.Copy(payload, offset, chunk, 0, length);

                if (targetParams.HasValue)
                    ReceiveLevelChunkClientRpc(chunk, i, total, payload.Length, targetParams.Value);
                else
                    ReceiveLevelChunkClientRpc(chunk, i, total, payload.Length);
            }
        }

        [ClientRpc]
        private void ReceiveLevelChunkClientRpc(byte[] chunk, int index, int total, int totalLength, ClientRpcParams rpcParams = default)
        {
            if (IsServer) return;

            if (index == 0)
            {
                _receiveBuffer = new byte[totalLength];
                _receiveOffset = 0;
                _receiveTotalChunks = total;
                _receiveChunkCount = 0;
            }

            Array.Copy(chunk, 0, _receiveBuffer, _receiveOffset, chunk.Length);
            _receiveOffset += chunk.Length;
            _receiveChunkCount++;

            if (_receiveChunkCount < _receiveTotalChunks) return;

            _grid = LevelNetworkSerializer.Deserialize(_receiveBuffer);
            _receiveBuffer = null;
            OnLevelReady?.Invoke(_grid);
        }
    }
}