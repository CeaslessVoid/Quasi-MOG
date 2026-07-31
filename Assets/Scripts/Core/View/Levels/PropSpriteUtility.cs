using UnityEngine;

namespace RoomGen
{
    public static class PropSpriteUtility
    {
        public static Vector2 GetUniformFitSize(Sprite sprite, float targetWidth, float targetHeight)
        {
            if (sprite == null) return new Vector2(targetWidth, targetHeight);

            Vector2 native = sprite.rect.size / sprite.pixelsPerUnit;
            if (native.x <= 0f || native.y <= 0f) return new Vector2(targetWidth, targetHeight);

            float scale = Mathf.Min(targetWidth / native.x, targetHeight / native.y);
            return native * scale;
        }
    }
}