using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewFloorDef", menuName = "Defs/Floor Def")]
    public class FloorDef : Def
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color tintColor = Color.white;

        public Sprite Sprite => sprite;
        public Color TintColor => tintColor;
        public bool HasTexture => sprite != null;
    }
}
