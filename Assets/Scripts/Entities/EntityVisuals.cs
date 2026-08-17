using GameDefs;
using RoomGen;
using TMPro;
using UnityEngine;

namespace Entities
{
    public class EntityVisuals : MonoBehaviour
    {
        private static readonly Color LocalPlayerNameColor = new Color(0.35f, 0.65f, 1f);
        private static readonly Color DefaultNameColor = Color.white;

        [SerializeField] private float spriteSize = 1f;
        [SerializeField] private float nameLabelOffset = 0.65f;
        [SerializeField] private float nameFontSize = 2.5f;

        private SpriteRenderer _bodyRenderer;
        private TextMeshPro _nameLabel;
        private bool _initialized;

        public void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            var bodyGO = new GameObject("Body");
            bodyGO.transform.SetParent(transform, false);
            _bodyRenderer = bodyGO.AddComponent<SpriteRenderer>();
            _bodyRenderer.sortingOrder = 6;

            var labelGO = new GameObject("NameLabel");
            labelGO.transform.SetParent(transform, false);
            labelGO.transform.localPosition = new Vector3(0f, nameLabelOffset, 0f);
            _nameLabel = labelGO.AddComponent<TextMeshPro>();
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.fontSize = nameFontSize;
            _nameLabel.sortingOrder = 12;
        }

        public void Refresh(LivingEntity entity, bool isLocalPlayer = false)
        {
            EnsureInitialized();

            var def = entity.Def;
            Sprite sprite = def != null && def.HasWorldSprite ? def.WorldSprite : DefVisualUtility.MissingSprite;
            _bodyRenderer.sprite = sprite;
            _bodyRenderer.color = def != null && def.HasWorldSprite ? Color.white : DefVisualUtility.MissingColor;

            var fitSize = PropPlacementUtility.GetUniformFitSize(sprite, spriteSize, spriteSize);
            _bodyRenderer.transform.localScale = new Vector3(fitSize.x, fitSize.y, 1f);

            SetName(entity.DisplayName, isLocalPlayer);
        }

        public void SetName(string value, bool isLocalPlayer = false)
        {
            EnsureInitialized();
            if (_nameLabel == null) return;
            _nameLabel.text = value;
            _nameLabel.color = isLocalPlayer ? LocalPlayerNameColor : DefaultNameColor;
        }
    }
}