using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewLimbDef", menuName = "Defs/Limb Def")]
    public class LimbDef : Def
    {
        [SerializeField] private LimbDef childLimbDef;

        public LimbDef ChildLimbDef => childLimbDef;
        public bool HasChild => childLimbDef != null;
    }
}