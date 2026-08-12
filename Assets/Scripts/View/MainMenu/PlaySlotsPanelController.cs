using UnityEngine;
using UnityEngine.SceneManagement;
using Networking.App;
using Save;

namespace UI.MainMenu
{
    public class PlaySlotsPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenu;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private SaveSlotSelectController saveSlotSelect;
        [SerializeField] private string gameSceneName = "Game";

        private void Awake()
        {
            backButton.onClick.AddListener(HandleBack);
        }

        private void OnEnable()
        {
            saveSlotSelect.Open(LoadSlot, HandleNewGame);
        }

        private void OnDisable()
        {
            saveSlotSelect.Close();
        }

        private void HandleBack()
        {
            mainMenu.ShowRoot();
        }

        private void HandleNewGame(int slotIndex, string saveName)
        {
            var app = AppState.EnsureExists();
            app.ConfigureSingleplayerNewGame(slotIndex, saveName);
            SceneManager.LoadScene(gameSceneName);
        }

        private void LoadSlot(int slotIndex)
        {
            SaveManager.TouchSlot(slotIndex);
            var app = AppState.EnsureExists();
            app.ConfigureSingleplayerLoad(slotIndex);
            SceneManager.LoadScene(gameSceneName);
        }
    }
}