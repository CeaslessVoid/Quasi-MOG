using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoomGen.UI
{
    public class DefListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private DefListItemView itemPrefab;

        private readonly List<DefListItemView> _pool = new List<DefListItemView>();

        public void Populate(IReadOnlyList<PlaceableItem> items, System.Func<string, bool> isSelected = null)
        {
            if (itemPrefab == null) return;
            if (content == null) return;

            EnsurePoolSize(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var view = _pool[i];
                view.gameObject.SetActive(true);
                bool selected = isSelected != null && isSelected(items[i].DefName);
                view.Bind(items[i], selected);
            }

            for (int i = items.Count; i < _pool.Count; i++)
                _pool[i].gameObject.SetActive(false);

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            Canvas.ForceUpdateCanvases();
        }

        private void EnsurePoolSize(int count)
        {
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
            }
        }
    }
}
