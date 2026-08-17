using System.Collections.Generic;
using UnityEngine;

namespace GameDefs
{
    [CreateAssetMenu(fileName = "NewEntityDef", menuName = "Defs/Entity Def")]
    public class EntityDef : Def
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private List<LimbDef> limbs = new List<LimbDef>();
        [SerializeField] private Sprite worldSprite;

        public float MaxHealth => maxHealth;
        public IReadOnlyList<LimbDef> Limbs => limbs;
        public Sprite WorldSprite => worldSprite;
        public bool HasWorldSprite => worldSprite != null;
    }
}