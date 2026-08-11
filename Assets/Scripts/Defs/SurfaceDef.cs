using UnityEngine;

namespace GameDefs
{
    public abstract class SurfaceDef : Def
    {
        [SerializeField] private Sprite sprite;

        public Sprite Sprite => sprite;
        public bool HasTexture => sprite != null;
    }
}