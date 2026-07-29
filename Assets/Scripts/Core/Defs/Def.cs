using UnityEngine;

namespace GameDefs
{
    public abstract class Def : ScriptableObject
    {
        [SerializeField] private string defName;
        [SerializeField] private Color tintColor = Color.white;

        public string DefName => defName;
        public Color TintColor => tintColor;
    }
}