using UnityEngine;

namespace Networking.App
{
    public enum NetworkRole { Singleplayer, Host, Client }

    public class AppState : MonoBehaviour
    {
        private const string PlayerNamePrefKey = "RoomGen.LocalPlayerName";

        public static AppState Instance { get; private set; }

        public NetworkRole Role { get; private set; } = NetworkRole.Singleplayer;
        public int SelectedSaveSlot { get; private set; } = -1;
        public string PendingSaveName { get; private set; }
        public bool IsNewGame { get; private set; }
        public string LocalPlayerName { get; private set; } = "Player";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LocalPlayerName = PlayerPrefs.GetString(PlayerNamePrefKey, "Player");
        }

        public static AppState EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("AppState");
            return go.AddComponent<AppState>();
        }

        public void SetLocalPlayerName(string name)
        {
            LocalPlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
            PlayerPrefs.SetString(PlayerNamePrefKey, LocalPlayerName);
            PlayerPrefs.Save();
        }

        public void ConfigureSingleplayerNewGame(int slotIndex, string saveName)
        {
            Role = NetworkRole.Singleplayer;
            SelectedSaveSlot = slotIndex;
            PendingSaveName = saveName;
            IsNewGame = true;
        }

        public void ConfigureSingleplayerLoad(int slotIndex)
        {
            Role = NetworkRole.Singleplayer;
            SelectedSaveSlot = slotIndex;
            IsNewGame = false;
        }

        public void ConfigureMultiplayerHost(int slotIndex, string saveName, bool isNewGame)
        {
            Role = NetworkRole.Host;
            SelectedSaveSlot = slotIndex;
            PendingSaveName = saveName;
            IsNewGame = isNewGame;
        }

        public void ConfigureMultiplayerClient()
        {
            Role = NetworkRole.Client;
            SelectedSaveSlot = -1;
            IsNewGame = false;
        }

        public bool IsMultiplayer => Role == NetworkRole.Host || Role == NetworkRole.Client;
        public bool IsServerAuthority => Role == NetworkRole.Singleplayer || Role == NetworkRole.Host;
    }
}
