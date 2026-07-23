using UnityEngine;
using GameTexture;

namespace RoomGen
{
    public abstract class RoomVisualsBase : MonoBehaviour
    {
        private const string DefaultWallId = "SmoothWall";
        private const string DefaultFloorId = "BasicFloor";

        [SerializeField] private string wallId = DefaultWallId;
        [SerializeField] private string floorId = DefaultFloorId;
        [SerializeField] protected float cellSize = 1f;

        protected WallTexture wallAsset;
        protected FloorTexture floorAsset;
        private bool _initialized;

        public float CellSize => cellSize;

        protected virtual void Awake() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            ResolveAssets();
            OnInitialize();
        }

        private void ResolveAssets()
        {
            GameManager.EnsureExists();
            var db = GameManager.Instance.Assets;
            wallAsset = db != null ? db.Get<WallTexture>(wallId) : null;
            floorAsset = db != null ? db.Get<FloorTexture>(floorId) : null;

        }

        protected abstract void OnInitialize();

        protected static int ComputeBitmask(bool n, bool e, bool s, bool w) => WallAtlas.ComputeBitmask(n, e, s, w);
    }
}