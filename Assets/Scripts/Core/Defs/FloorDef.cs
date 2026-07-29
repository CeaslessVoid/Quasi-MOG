using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewFloorDef", menuName = "Defs/Floor Def")]
    public class FloorDef : Def
    {
        [SerializeField] private Sprite sprite;

        public Sprite Sprite => sprite;
        public bool HasTexture => sprite != null;
    }
}