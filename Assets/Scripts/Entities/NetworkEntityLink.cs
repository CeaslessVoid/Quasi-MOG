using GameDefs;
using RoomGen;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Entities
{
    public struct EntitySpawnState : INetworkSerializable, System.IEquatable<EntitySpawnState>
    {
        public FixedString32Bytes entityDefName;
        public FixedString32Bytes characterName;
        public ulong ownerClientId;
        public int cellX;
        public int cellY;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref entityDefName);
            serializer.SerializeValue(ref characterName);
            serializer.SerializeValue(ref ownerClientId);
            serializer.SerializeValue(ref cellX);
            serializer.SerializeValue(ref cellY);
        }

        public bool Equals(EntitySpawnState other) =>
            entityDefName.Equals(other.entityDefName) &&
            characterName.Equals(other.characterName) &&
            ownerClientId == other.ownerClientId &&
            cellX == other.cellX &&
            cellY == other.cellY;
    }

    [RequireComponent(typeof(PlayableEntity))]
    public class NetworkEntityLink : NetworkBehaviour
    {
        private readonly NetworkVariable<EntitySpawnState> _state =
            new NetworkVariable<EntitySpawnState>(writePerm: NetworkVariableWritePermission.Server);

        private PlayableEntity _entity;

        private void Awake()
        {
            _entity = GetComponent<PlayableEntity>();
        }

        public void ServerInitialize(string entityDefName, string characterName, ulong ownerClientId, Vector2Int cell)
        {
            _state.Value = new EntitySpawnState
            {
                entityDefName = entityDefName,
                characterName = characterName,
                ownerClientId = ownerClientId,
                cellX = cell.x,
                cellY = cell.y
            };
        }

        public override void OnNetworkSpawn()
        {
            _state.OnValueChanged += (oldValue, newValue) => Apply();
            Apply();
        }

        private void Apply()
        {
            var value = _state.Value;
            if (value.entityDefName.IsEmpty) return;

            var def = DefDatabase.Get<EntityDef>(value.entityDefName.ToString());
            var cell = new Vector2Int(value.cellX, value.cellY);

            _entity.Configure(def, cell, EntityConstants.CellSize);
            _entity.CharacterName = value.characterName.ToString();

            bool isLocalPlayer = NetworkManager.Singleton != null && value.ownerClientId == NetworkManager.Singleton.LocalClientId;
            _entity.RefreshVisuals(isLocalPlayer);

            if (isLocalPlayer)
                SimpleTopDownCameraController.Instance?.CenterOn(transform.position);
        }
    }
}