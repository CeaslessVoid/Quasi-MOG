using UnityEngine;

namespace GameDefs
{
    public enum PropCategory
    {
        Normal = 0,
        Decorative = 1,
        Wall = 2,
    }

    public enum PropFacing
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
    }

    public enum PropInteractionType
    {
        None = 0,
        Generic = 1,
        Storage = 2,
    }

    [CreateAssetMenu(fileName = "NewPropDef", menuName = "Defs/Prop Def")]
    public class PropDef : BlockerDef
    {
        [SerializeField] private PropCategory category = PropCategory.Normal;
        [SerializeField] private int width = 1;
        [SerializeField] private int height = 1;
        [SerializeField] private PropInteractionType interactionType = PropInteractionType.None;
        [SerializeField] private Sprite northSprite;
        [SerializeField] private Sprite southSprite;
        [SerializeField] private Sprite eastSprite;

        public PropCategory Category => category;
        public int Width => Mathf.Max(1, width);
        public int Height => Mathf.Max(1, height);
        public PropInteractionType InteractionType => interactionType;
        public bool IsInteractable => interactionType != PropInteractionType.None;
        public bool HasTexture => northSprite != null || southSprite != null || eastSprite != null;

        public Sprite GetSprite(PropFacing facing) => facing switch
        {
            PropFacing.North => northSprite,
            PropFacing.South => southSprite,
            PropFacing.East => eastSprite,
            PropFacing.West => eastSprite,
            _ => northSprite
        };
    }
}