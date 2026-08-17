using System.Collections.Generic;
using GameDefs;
using UnityEngine;

namespace Entities
{
    [RequireComponent(typeof(EntityVisuals))]
    public class LivingEntity : MonoBehaviour, ITurnActor
    {
        [SerializeField] private EntityVisuals visuals;

        private EntityDef _def;
        private float _currentHealth;
        private readonly List<LimbInstance> _limbs = new List<LimbInstance>();
        private readonly Inventory _inventory = new Inventory();

        public EntityDef Def => _def;
        public float CurrentHealth => _currentHealth;
        public IReadOnlyList<LimbInstance> Limbs => _limbs;
        public Inventory Inventory => _inventory;
        public Vector2Int Cell { get; private set; }
        public bool IsAlive => _currentHealth > 0f;
        public bool CanAct => IsAlive;

        public virtual string DisplayName => _def != null ? _def.DisplayName : gameObject.name;

        public void Configure(EntityDef def, Vector2Int cell, float cellSize)
        {
            _def = def;
            _currentHealth = def != null ? def.MaxHealth : 0f;

            BuildLimbs(def);
            SetCell(cell, cellSize);
        }

        public void RefreshVisuals(bool isLocalPlayer = false)
        {
            if (visuals == null) visuals = GetComponent<EntityVisuals>();
            visuals.Refresh(this, isLocalPlayer);
        }

        private void BuildLimbs(EntityDef def)
        {
            _limbs.Clear();
            if (def == null) return;

            foreach (var limbDef in def.Limbs)
                if (limbDef != null) _limbs.Add(new LimbInstance(limbDef));
        }

        public bool TryGetLimb(LimbDef limbDef, out LimbInstance limb)
        {
            foreach (var l in _limbs)
            {
                if (l.def == limbDef)
                {
                    limb = l;
                    return true;
                }
            }
            limb = null;
            return false;
        }

        public bool TryGetLimb(string limbDefName, out LimbInstance limb)
        {
            foreach (var l in _limbs)
            {
                if (l.def != null && l.def.DefName == limbDefName)
                {
                    limb = l;
                    return true;
                }
            }
            limb = null;
            return false;
        }

        public void DetachLimb(LimbDef limbDef)
        {
            if (TryGetLimb(limbDef, out var limb)) DetachRecursive(limb);
        }

        private void DetachRecursive(LimbInstance limb)
        {
            if (!limb.attached) return;
            limb.attached = false;

            if (limb.def.HasChild && TryGetLimb(limb.def.ChildLimbDef, out var childInstance))
                DetachRecursive(childInstance);
        }

        public void SetCell(Vector2Int cell, float cellSize)
        {
            Cell = cell;
            transform.position = new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        }

        public virtual void TakeTurn()
        {
        }
    }
}