using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using RoomGen;
using Entities;

namespace Networking
{
    public class NetworkedLevelSync : NetworkBehaviour
    {
        public static NetworkedLevelSync Instance { get; private set; }

        private const int ChunkSize = 500;

        [SerializeField] private NetworkObject playableEntityPrefab;
        [SerializeField] private string playerEntityDefName = "Human";

        public event Action<LevelGrid> OnLevelReady;

        private LevelGrid _grid;
        public LevelGrid Grid => _grid;

        private byte[] _receiveBuffer;
        private int _receiveOffset;
        private int _receiveTotalChunks;
        private int _receiveChunkCount;

        private readonly HashSet<Vector2Int> _usedSpawnCells = new HashSet<Vector2Int>();

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
                SpawnEntitiesForConnectedClients();

                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            if (Instance == this) Instance = null;
        }

        private RoomGenerator FindOrCreateGenerator()
        {
            var existing = FindFirstObjectByType<RoomGenerator>();
            if (existing != null) return existing;

            var go = new GameObject("RoomGenerator");
            return go.AddComponent<RoomGenerator>();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.LocalClientId) return;
            if (_grid == null) return;

            byte[] payload = LevelNetworkSerializer.Serialize(_grid);
            var sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };
            SendChunks(payload, sendParams);

            SpawnEntityForClient(clientId);
        }

        private void SendLevelToAllClients()
        {
            byte[] payload = LevelNetworkSerializer.Serialize(_grid);
            SendChunks(payload, null);
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

        private void SpawnEntitiesForConnectedClients()
        {
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
                SpawnEntityForClient(clientId);
        }

        private void SpawnEntityForClient(ulong clientId)
        {
            if (playableEntityPrefab == null)
            {
                Debug.LogError("NetworkedLevelSync: playableEntityPrefab is not assigned. Cannot spawn player entity.");
                return;
            }
            if (_grid == null) return;

            var candidates = EntitySpawner.FindSpawnableCells(_grid);
            var cell = EntitySpawner.PickUnusedCell(candidates, _usedSpawnCells, _grid.Origin);

            var instance = Instantiate(playableEntityPrefab);
            instance.SpawnWithOwnership(clientId);

            var link = instance.GetComponent<NetworkEntityLink>();
            link.ServerInitialize(playerEntityDefName, ResolveCharacterName(clientId), clientId, cell);
        }

        private string ResolveCharacterName(ulong clientId)
        {
            if (RoomSession.Instance != null)
            {
                for (int i = 0; i < RoomSession.Instance.PlayerCount; i++)
                {
                    var p = RoomSession.Instance.GetPlayer(i);
                    if (p.clientId == clientId) return p.playerName.ToString();
                }
            }

            return clientId == NetworkManager.LocalClientId ? GameManager.EnsureExists().LocalPlayerName : "Player";
        }
    }
}