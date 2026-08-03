using System.Collections.Generic;
using UnityEngine;

namespace RoomGen.UI
{
    public class DefListPanel : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private DefListItemView itemPrefab;

        private readonly List<DefListItemView> _pool = new List<DefListItemView>();

        public void Populate(IReadOnlyList<PlaceableItem> items, System.Func<string, bool> isSelected = null)
        {
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
        }

        private void EnsurePoolSize(int count)
        {
            while (_pool.Count < count)
                _pool.Add(Instantiate(itemPrefab, content));
        }
    }
}