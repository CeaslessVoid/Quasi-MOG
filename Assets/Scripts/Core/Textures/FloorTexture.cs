using UnityEngine;

namespace GameTexture
{
    [CreateAssetMenu(fileName = "NewFloorTexture", menuName = "Textures/Floor Texture")]
    public class FloorTexture : TextureRef
    {
        [SerializeField] private Sprite sprite;
        public Sprite Sprite => sprite;
    }
}