using GameTexture;
using UnityEngine;

namespace RoomGen
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameTextureDatabase Assets;

        public static GameManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("GameManager");
            return go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}