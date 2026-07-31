using System.Collections.Generic;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public class PropInstance : MonoBehaviour
    {
        private static readonly HashSet<PropInstance> _interactable = new HashSet<PropInstance>();
        public static IReadOnlyCollection<PropInstance> Interactable => _interactable;

        private PropDef _def;
        private float _currentHp;

        public PropDef Def => _def;
        public bool IsInteractable => _def != null && _def.IsInteractable;
        public float CurrentHp => _currentHp;

        public void Configure(PropDef def)
        {
            _def = def;
            _currentHp = def != null ? def.MaxHp : 0f;
            if (IsInteractable) _interactable.Add(this);
        }

        public void ApplyDamage(float amount)
        {
            if (_def == null) return;
            _currentHp = Mathf.Max(0f, _currentHp - amount);
        }

        private void OnDestroy()
        {
            _interactable.Remove(this);
        }
    }
}