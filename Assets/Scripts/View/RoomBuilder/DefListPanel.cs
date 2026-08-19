using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoomGen.UI
{
    public readonly struct PlaceableItem
    {
        public readonly string DefName;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly Action OnSelect;

        public PlaceableItem(string defName, string displayName, Sprite icon, Action onSelect)
        {
            DefName = defName;
            DisplayName = displayName;
            Icon = icon;
            OnSelect = onSelect;
        }
    }

    public class DefListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private DefListItemView itemPrefab;

        private readonly List<DefListItemView> _pool = new List<DefListItemView>();

        public void Populate(IReadOnlyList<PlaceableItem> items, Func<string, bool> isSelected = null)
        {
            if (itemPrefab == null) return;
            if (content == null) return;

            bool grew = EnsurePoolSize(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var view = _pool[i];
                view.gameObject.SetActive(true);
                bool selected = isSelected != null && isSelected(items[i].DefName);
                view.Bind(items[i], selected);
            }

            for (int i = items.Count; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);

            if (grew && content is RectTransform contentRect)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }
        }

        private bool EnsurePoolSize(int count)
        {
            bool grew = false;
            int guard = 0;
            while (_pool.Count < count)
            {
                guard++;
                if (guard > 10000) break;

                var instance = Instantiate(itemPrefab, content);
                var rt = instance.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }
                _pool.Add(instance);
                grew = true;
            }
            return grew;
        }
    }
}