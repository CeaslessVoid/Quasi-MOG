using UnityEngine;

namespace GameTexture
{
    public abstract class TextureRef : ScriptableObject
    {
        [SerializeField] private string id;
        public string Id => id;
    }
}