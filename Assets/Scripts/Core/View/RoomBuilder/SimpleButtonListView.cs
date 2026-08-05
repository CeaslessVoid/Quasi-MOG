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

        public SimpleButtonItem(string label, Action onClick)
        {
            Label = label;
            OnClick = onClick;
        }
    }

    public class SimpleButtonListView : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private Button itemPrefab;

        private readonly List<Button> _pool = new List<Button>();

        public void Populate(IReadOnlyList<SimpleButtonItem> items)
        {
            EnsurePoolSize(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var button = _pool[i];
                button.gameObject.SetActive(true);

                var text = button.GetComponentInChildren<TMP_Text>();
                if (text != null) text.text = items[i].Label;

                var captured = items[i].OnClick;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => captured?.Invoke());
            }

            for (int i = items.Count; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);
        }

        private void EnsurePoolSize(int count)
        {
            while (_pool.Count < count)
                _pool.Add(Instantiate(itemPrefab, content));
        }
    }
}