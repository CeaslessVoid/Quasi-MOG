using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Save;

namespace UI.MainMenu
{
    public class SaveSlotView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button deleteButton;

        private Action _onClick;
        private Action _onDelete;

        public void Bind(int index, Action onClick, Action onDelete = null)
        {
            _onClick = onClick;
            _onDelete = onDelete;

            if (button == null) button = GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke());

            if (deleteButton != null)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(() => _onDelete?.Invoke());
            }
        }

        public void SetInfo(SaveSlotInfo info)
        {
            if (info.hasSave)
            {
                titleText.text = string.IsNullOrEmpty(info.saveName) ? $"Save {info.slotIndex + 1}" : info.saveName;
                subtitleText.text = "Load";
            }
            else
            {
                titleText.text = $"Slot {info.slotIndex + 1}";
                subtitleText.text = "Empty";
            }

            if (deleteButton != null) deleteButton.gameObject.SetActive(info.hasSave);
        }
    }
}
