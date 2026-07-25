using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewDoorDef", menuName = "Defs/Door Def")]
    public class DoorDef : Def
    {
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Color tintColor = Color.white;

        [Header("Physics")]
        [SerializeField] private PhysicsMaterial2D physicsMaterial;
        [SerializeField] private bool blocksProjectilesWhenClosed = true;

        public Sprite ClosedSprite => closedSprite;
        public Sprite OpenSprite => openSprite;
        public Color TintColor => tintColor;
        public PhysicsMaterial2D PhysicsMaterial => physicsMaterial;
        public bool BlocksProjectilesWhenClosed => blocksProjectilesWhenClosed;
        public bool HasTexture => closedSprite != null;
    }
}
