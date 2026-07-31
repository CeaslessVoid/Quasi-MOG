using UnityEngine;

namespace GameDefs
{
    public static class DefTintRenderer
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SecondaryColorId = Shader.PropertyToID("_SecondaryColor");
        private static readonly int HasMaskId = Shader.PropertyToID("_HasMask");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");

        private static Material _material;
        private static MaterialPropertyBlock _block;

        private static Material SharedMaterial
        {
            get
            {
                if (_material == null)
                    _material = new Material(Shader.Find("Custom/MaskedTintSprite")) { name = "MaskedTintSprite (Shared)" };
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