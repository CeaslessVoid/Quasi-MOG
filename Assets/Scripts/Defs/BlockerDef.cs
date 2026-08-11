using UnityEngine;

namespace GameDefs
{
    public abstract class BlockerDef : Def
    {
        [Range(0f, 1f)]
        [SerializeField] private float chanceToBlockBullet = 1f;
        [SerializeField] private bool blocksVision = true;
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private PhysicsMaterial2D physicsMaterial;

        public float ChanceToBlockBullet => chanceToBlockBullet;
        public bool BlocksVision => blocksVision;
        public float MaxHp => maxHp;
        public PhysicsMaterial2D PhysicsMaterial => physicsMaterial;
    }
}