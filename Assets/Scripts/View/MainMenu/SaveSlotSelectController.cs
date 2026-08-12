using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Save;

namespace UI.MainMenu
{
    public class SaveSlotSelectController : MonoBehaviour
    {
        public static SaveSlotSelectController Instance { get; private set; }

        [SerializeField] private GameObject root;
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

        private Action<int> _onLoadSlot;
        private Action<int, string> _onNewGame;

        private int _pendingSlotIndex = -1;
        private int _pendingDeleteIndex = -1;

        private void Awake()
        {
            Instance = this;

            newGameConfirmButton.onClick.AddListener(ConfirmNewGame);
            newGameCancelButton.onClick.AddListener(() => newGameDialog.SetActive(false));

            deleteConfirmButton.onClick.AddListener(ConfirmDelete);
            deleteCancelButton.onClick.AddListener(() => deleteConfirmDialog.SetActive(false));

            for (int i = 0; i < slotViews.Length; i++)
            {
                int index = i;
                slotViews[i].Bind(index, () => HandleSlotClicked(index), () => HandleDeleteRequested(index));
            }

            newGameDialog.SetActive(false);
            deleteConfirmDialog.SetActive(false);
            root.SetActive(false);
        }

        public void Open(Action<int> onLoadSlot, Action<int, string> onNewGame)
        {
            _onLoadSlot = onLoadSlot;
            _onNewGame = onNewGame;
            root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            root.SetActive(false);
            newGameDialog.SetActive(false);
            deleteConfirmDialog.SetActive(false);
            _onLoadSlot = null;
            _onNewGame = null;
        }

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
                _onLoadSlot?.Invoke(index);
                return;
            }

            _pendingSlotIndex = index;
            if (newGameNameInput != null) newGameNameInput.text = $"Save {index + 1}";
            newGameDialog.SetActive(true);
        }

        private void ConfirmNewGame()
        {
            if (_pendingSlotIndex < 0) return;

            string name = newGameNameInput != null ? newGameNameInput.text : null;
            int slot = _pendingSlotIndex;
            _pendingSlotIndex = -1;
            newGameDialog.SetActive(false);

            SaveManager.CreateNewGame(slot, name);
            _onNewGame?.Invoke(slot, name);
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