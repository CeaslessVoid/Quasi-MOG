using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Networking.App;
using Save;
using RoomGen;

namespace UI.MainMenu
{
    public class PlaySlotsPanelController : MonoBehaviour
    {
        [SerializeField] private MainMenuController mainMenu;
        [SerializeField] private Button backButton;
        [SerializeField] private SaveSlotView[] slotViews;

        [Header("New Game")]
        [SerializeField] private GameObject newGameDialog;
        [SerializeField] private TMP_InputField newGameNameInput;
        [SerializeField] private Button newGameConfirmButton;
        [SerializeField] private Button newGameCancelButton;

        [Header("Delete")]
        [SerializeField] private GameObject deleteConfirmDialog;
        [SerializeField] private TMP_Text deleteConfirmText;
        [SerializeField] private Button deleteConfirmButton;
        [SerializeField] private Button deleteCancelButton;

        [SerializeField] private string gameSceneName = "Game";

        private int _pendingSlotIndex = -1;
        private int _pendingDeleteIndex = -1;

        private void Awake()
        {
            backButton.onClick.AddListener(() => mainMenu.ShowRoot());

            newGameConfirmButton.onClick.AddListener(ConfirmNewGame);
            newGameCancelButton.onClick.AddListener(() => newGameDialog.SetActive(false));

            deleteConfirmButton.onClick.AddListener(ConfirmDelete);
            deleteCancelButton.onClick.AddListener(() => deleteConfirmDialog.SetActive(false));

            for (int i = 0; i < slotViews.Length; i++)
            {
                int index = i;
                slotViews[i].Bind(index, () => HandleSlotClicked(index), () => HandleDeleteRequested(index));
            }
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            var slots = SaveManager.GetSlots();
            for (int i = 0; i < slotViews.Length && i < slots.Length; i++)
                slotViews[i].SetInfo(slots[i]);
        }

        private void HandleSlotClicked(int index)
        {
            var info = SaveManager.GetSlots()[index];
            if (info.hasSave)
            {
                LoadSlot(index);
            }
            else
            {
                _pendingSlotIndex = index;
                if (newGameNameInput != null) newGameNameInput.text = $"Save {index + 1}";
                newGameDialog.SetActive(true);
            }
        }

        private void ConfirmNewGame()
        {
            if (_pendingSlotIndex < 0) return;
            string name = newGameNameInput != null ? newGameNameInput.text : null;
            SaveManager.CreateNewGame(_pendingSlotIndex, name);

            var app = AppState.EnsureExists();
            app.ConfigureSingleplayerNewGame(_pendingSlotIndex, name);

            newGameDialog.SetActive(false);
            SceneManager.LoadScene(gameSceneName);
        }

        private void LoadSlot(int index)
        {
            SaveManager.TouchSlot(index);
            var app = AppState.EnsureExists();
            app.ConfigureSingleplayerLoad(index);
            SceneManager.LoadScene(gameSceneName);
        }

        private void HandleDeleteRequested(int index)
        {
            var info = SaveManager.GetSlots()[index];
            if (!info.hasSave) return;

            _pendingDeleteIndex = index;
            if (deleteConfirmText != null)
            {
                string label = string.IsNullOrEmpty(info.saveName) ? $"Save {index + 1}" : info.saveName;
                deleteConfirmText.text = $"Delete '{label}'? This cannot be undone.";
            }
            deleteConfirmDialog.SetActive(true);
        }

        private void ConfirmDelete()
        {
            if (_pendingDeleteIndex < 0) return;
            SaveManager.DeleteSlot(_pendingDeleteIndex);
            _pendingDeleteIndex = -1;
            deleteConfirmDialog.SetActive(false);
            Refresh();
        }
    }
}
