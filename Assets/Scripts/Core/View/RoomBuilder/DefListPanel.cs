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
            if (itemPrefab == null)
            {
                Debug.LogError("DefListPanel: itemPrefab is not assigned. No items can be created.", this);
                return;
            }
            if (content == null)
            {
                Debug.LogError("DefListPanel: content is not assigned.", this);
                return;
            }

            Debug.Log($"DefListPanel.Populate: items={items.Count}, poolSizeBefore={_pool.Count}");
            EnsurePoolSize(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                var view = _pool[i];
                if (!view.gameObject.activeSelf) view.gameObject.SetActive(true);
                bool selected = isSelected != null && isSelected(items[i].DefName);
                view.Bind(items[i], selected);
            }

            for (int i = items.Count; i < _pool.Count; i++)
                if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);

            if (content is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        private void EnsurePoolSize(int count)
        {
            int guard = 0;
            while (_pool.Count < count)
            {
                guard++;
                if (guard > 10000)
                {
                    Debug.LogError("DefListPanel: aborting pool growth, exceeded sanity limit. Check itemPrefab.", this);
                    break;
                }

                var instance = Instantiate(itemPrefab, content);
                var rt = instance.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }
                instance.gameObject.SetActive(false);
                _pool.Add(instance);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Log Pool State")]
        private void LogPoolState()
        {
            Debug.Log($"DefListPanel pool size: {_pool.Count}, itemPrefab assigned: {itemPrefab != null}, content assigned: {content != null}");
            for (int i = 0; i < _pool.Count; i++)
                Debug.Log($"  [{i}] active={_pool[i].gameObject.activeSelf}", _pool[i]);
        }
#endif
    }
}