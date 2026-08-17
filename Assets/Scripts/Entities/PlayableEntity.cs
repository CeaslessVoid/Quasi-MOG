using System.Collections.Generic;
using UnityEngine;

namespace Entities
{
    public class PlayableEntity : LivingEntity
    {
        [SerializeField] private string characterName;

        public readonly List<string> skillIds = new List<string>();
        public readonly List<string> implantIds = new List<string>();

        public string CharacterName
        {
            get => characterName;
            set => characterName = value;
        }

        public override string DisplayName => string.IsNullOrEmpty(characterName) ? base.DisplayName : characterName;
    }
}