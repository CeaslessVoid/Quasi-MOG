using UnityEngine;

namespace RoomGen
{
    public abstract class RoomVisualsBase : MonoBehaviour
    {
        [SerializeField] protected float cellSize = 1f;

        private bool _initialized;

        public float CellSize => cellSize;

        protected virtual void Awake() => EnsureInitialized();

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            OnInitialize();
        }

        protected abstract void OnInitialize();

        protected static int ComputeBitmask(bool n, bool e, bool s, bool w) => WallAtlas.ComputeBitmask(n, e, s, w);
    }
}
