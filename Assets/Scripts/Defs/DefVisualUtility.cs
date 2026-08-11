using UnityEngine;

namespace GameDefs
{
    public static class DefVisualUtility
    {
        public static readonly Color MissingColor = new Color(1f, 0f, 1f, 1f);

        private static Sprite _missingSprite;
        private static Sprite _solidSprite;

        public static Sprite MissingSprite
        {
            get
            {
                if (_missingSprite == null) _missingSprite = BuildCheckerSprite();
                return _missingSprite;
            }
        }

        public static Sprite SolidSprite
        {
            get
            {
                if (_solidSprite == null) _solidSprite = BuildSolidSprite();
                return _solidSprite;
            }
        }

        private static Sprite BuildSolidSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private static Sprite BuildCheckerSprite()
        {
            const int size = 8;
            var tex = new Texture2D(size, size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool a = ((x / 2) + (y / 2)) % 2 == 0;
                    tex.SetPixel(x, y, a ? MissingColor : Color.black);
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
