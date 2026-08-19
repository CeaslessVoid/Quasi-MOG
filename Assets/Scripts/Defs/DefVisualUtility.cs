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

    public static class DefTintRenderer
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int HasMaskId = Shader.PropertyToID("_HasMask");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");

        private static Material _material;
        private static MaterialPropertyBlock _block;

        private const string ShaderResourcePath = "Shaders/MaskedTintSprite";

        private static Material SharedMaterial
        {
            get
            {
                if (_material == null)
                {
                    var shader = Resources.Load<Shader>(ShaderResourcePath);
                    if (shader == null)
                    {
                        Debug.LogError($"DefTintRenderer: could not load shader at Resources/{ShaderResourcePath}. Falling back to Sprites/Default; tint/mask will not render correctly.");
                        shader = Shader.Find("Sprites/Default");
                    }
                    _material = new Material(shader) { name = "MaskedTintSprite (Shared)" };
                }
                return _material;
            }
        }

        public static void Apply(SpriteRenderer renderer, Def def)
        {
            if (def != null && def.HasMask)
                Apply(renderer, def.TintColor, def.SecondaryTintColor, def.MaskTexture);
            else
                Apply(renderer, def != null ? def.TintColor : Color.white, Color.white, null);
        }

        public static void Apply(SpriteRenderer renderer, Color primaryTint, Color secondaryTint, Texture2D mask)
        {
            renderer.sharedMaterial = SharedMaterial;
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);

            _block.SetColor(ColorId, primaryTint);
            _block.SetColor(SecondaryColorId, secondaryTint);
            _block.SetFloat(HasMaskId, mask != null ? 1f : 0f);
            if (mask != null) _block.SetTexture(MaskTexId, mask);

            renderer.SetPropertyBlock(_block);
            renderer.color = Color.white;
        }

        public static void ApplyFlatTint(SpriteRenderer renderer, Color tint)
        {
            Apply(renderer, tint, Color.white, null);
        }
    }
}
