using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RoomGen.UI
{
    public readonly struct SimpleButtonItem
    {
        public readonly string Label;
        public readonly Action OnClick;
        public readonly bool Selected;

        public SimpleButtonItem(string label, Action onClick, bool selected = false)
        {
            Label = label;
            OnClick = onClick;
            Selected = selected;
        }
    }

    public class SimpleButtonListView : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private Button itemPrefab;
        [SerializeField] private Color selectedColor = new Color(0.25f, 0.65f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        private readonly List<Button> _pool = new List<Button>();

        private Transform ContentRoot => content != null ? content : transform;

        public void Populate(IReadOnlyList<SimpleButtonItem> items)
        {
            EnsurePoolSize(items.Count);
            if (_pool.Count < items.Count) return;

            for (int i = 0; i < items.Count; i++)
            {
                var button = _pool[i];
                button.gameObject.SetActive(true);

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = items[i].Label;

                if (button.targetGraphic is Image image)
                    image.color = items[i].Selected ? selectedColor : normalColor;

                var captured = items[i].OnClick;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => captured?.Invoke());
            }

            for (int i = items.Count; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        private void EnsurePoolSize(int count)
        {
            if (itemPrefab == null)
            {
                Debug.LogError($"SimpleButtonListView on '{name}': Item Prefab is not assigned.", this);
                return;
            }

            while (_pool.Count < count)
                _pool.Add(Instantiate(itemPrefab, ContentRoot));
        }
    }
}
