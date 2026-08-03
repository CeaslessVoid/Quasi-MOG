using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public class DefListItemView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;
        [SerializeField] private GameObject selectedHighlight;

        private System.Action _onClick;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        public void Bind(PlaceableItem item, bool isSelected)
        {
            if (icon != null)
            {
                icon.sprite = item.Icon;
                icon.enabled = item.Icon != null;
            }
            if (label != null) label.text = item.DisplayName;
            if (selectedHighlight != null) selectedHighlight.SetActive(isSelected);
            _onClick = item.OnSelect;
        }

        private void HandleClick() => _onClick?.Invoke();
    }
}