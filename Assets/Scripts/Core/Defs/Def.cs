using UnityEngine;

namespace GameDefs
{
    public abstract class Def : ScriptableObject
    {
        [SerializeField] private string defName;
        public string DefName => defName;
    }
}
