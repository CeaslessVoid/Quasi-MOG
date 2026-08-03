using System;
using UnityEngine;

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
}