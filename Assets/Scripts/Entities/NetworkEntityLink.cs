using GameDefs;
using RoomGen;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(PlayableEntity))]
    public class NetworkEntityLink : NetworkBehaviour
    {
        private readonly NetworkVariable<FixedString32Bytes> _entityDefName =
            new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<FixedString32Bytes> _characterName =
            new NetworkVariable<FixedString32Bytes>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<ulong> _ownerClientId =
            new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _cellX =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> _cellY =
            new NetworkVariable<int>(writePerm: NetworkVariableWritePermission.Server);

        private PlayableEntity _entity;

        private void Awake()
        {
            _entity = GetComponent<PlayableEntity>();
        }

        public void ServerInitialize(string entityDefName, string characterName, ulong ownerClientId, Vector2Int cell)
        {
            _entityDefName.Value = entityDefName;
            _characterName.Value = characterName;
            _ownerClientId.Value = ownerClientId;
            _cellX.Value = cell.x;
            _cellY.Value = cell.y;
        }

        public override void OnNetworkSpawn()
        {
            _entityDefName.OnValueChanged += (oldValue, newValue) => Apply();
            _characterName.OnValueChanged += (oldValue, newValue) => Apply();
            _cellX.OnValueChanged += (oldValue, newValue) => Apply();
            _cellY.OnValueChanged += (oldValue, newValue) => Apply();

            Apply();
        }

        private void Apply()
        {
            if (_entityDefName.Value.IsEmpty) return;

            var def = DefDatabase.Get<EntityDef>(_entityDefName.Value.ToString());
            var cell = new Vector2Int(_cellX.Value, _cellY.Value);

            _entity.Configure(def, cell, EntityConstants.CellSize);
            _entity.CharacterName = _characterName.Value.ToString();

            bool isLocalPlayer = NetworkManager.Singleton != null && _ownerClientId.Value == NetworkManager.Singleton.LocalClientId;
            _entity.RefreshVisuals(isLocalPlayer);

            if (isLocalPlayer)
                SimpleTopDownCameraController.Instance?.CenterOn(transform.position);
        }
    }
}