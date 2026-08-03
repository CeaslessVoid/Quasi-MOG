using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public class CategoryTabBar : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private Button tabButtonPrefab;
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        private readonly List<Button> _buttons = new List<Button>();
        private Action<int> _onSelected;

        public void Setup(IReadOnlyList<string> labels, Action<int> onSelected, int defaultIndex = 0)
        {
            _onSelected = onSelected;
            EnsureButtonCount(labels.Count);

            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                var button = _buttons[i];
                button.gameObject.SetActive(true);
                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = labels[i];
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Select(index));
            }

            for (int i = labels.Count; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);

            if (labels.Count > 0) Select(Mathf.Clamp(defaultIndex, 0, labels.Count - 1));
        }

        public void Select(int index)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (!_buttons[i].gameObject.activeSelf) continue;
                if (_buttons[i].targetGraphic is Image image)
                    image.color = i == index ? selectedColor : normalColor;
            }
            _onSelected?.Invoke(index);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        private void EnsureButtonCount(int count)
        {
            while (_buttons.Count < count)
                _buttons.Add(Instantiate(tabButtonPrefab, content));
        }
    }
}