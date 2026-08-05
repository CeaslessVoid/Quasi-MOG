using UnityEngine;
using UnityEngine.UI;

namespace RoomGen.UI
{
    public class RoomBuilderTopBarController : MonoBehaviour
    {
        [SerializeField] private RoomBuilderUIController sideUI;

        [SerializeField] private Button roomIOButton;
        [SerializeField] private Button tagsButton;
        [SerializeField] private Button doorDefaultsButton;

        [SerializeField] private GameObject roomIOPanel;
        [SerializeField] private GameObject tagsPanel;
        [SerializeField] private GameObject doorDefaultsPanel;

        [SerializeField] private Color activeColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color inactiveColor = Color.white;

        private GameObject _openPanel;
        private Button _openButton;

        private void Awake()
        {
            roomIOButton.onClick.AddListener(() => Toggle(roomIOPanel, roomIOButton));
            tagsButton.onClick.AddListener(() => Toggle(tagsPanel, tagsButton));
            doorDefaultsButton.onClick.AddListener(() => Toggle(doorDefaultsPanel, doorDefaultsButton));

            roomIOPanel.SetActive(false);
            tagsPanel.SetActive(false);
            doorDefaultsPanel.SetActive(false);
        }

        private void Toggle(GameObject panel, Button button)
        {
            bool wasOpen = _openPanel == panel;
            CloseAll();

            if (wasOpen) return;

            panel.SetActive(true);
            SetButtonColor(button, true);
            _openPanel = panel;
            _openButton = button;
            sideUI.CloseSidePanel();
        }

        public void CloseAll()
        {
            if (_openPanel != null) _openPanel.SetActive(false);
            if (_openButton != null) SetButtonColor(_openButton, false);
            _openPanel = null;
            _openButton = null;
        }

        private void SetButtonColor(Button button, bool active)
        {
            if (button.targetGraphic is Image image) image.color = active ? activeColor : inactiveColor;
        }
    }
}