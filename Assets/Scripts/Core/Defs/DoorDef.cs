using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewDoorDef", menuName = "Defs/Door Def")]
    public class DoorDef : BlockerDef
    {
        [SerializeField] private bool isDoubleDoor;
        [SerializeField] private Sprite northSprite;
        [SerializeField] private Sprite eastSprite;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private DoorDef singleDoorFallback;

        public bool IsDoubleDoor => isDoubleDoor;
        public Sprite NorthSprite => northSprite;
        public Sprite EastSprite => eastSprite;
        public Color TintColor => tintColor;
        public DoorDef SingleDoorFallback => singleDoorFallback;
        public bool HasTexture => northSprite != null && eastSprite != null;
    }
}