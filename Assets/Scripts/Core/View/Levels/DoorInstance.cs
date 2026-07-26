using System.Collections;
using UnityEngine;
using GameDefs;

namespace RoomGen
{
    public class DoorInstance : MonoBehaviour
    {
        [SerializeField] private float openDuration = 0.25f;

        private DoorDef _def;
        private SpriteRenderer _leafA;
        private SpriteRenderer _leafB;
        private Collider2D _collider;

        private Vector3 _leafAClosedPos;
        private Vector3 _leafBClosedPos;
        private Vector3 _leafAOpenPos;
        private Vector3 _leafBOpenPos;

        private bool _isOpen;
        private Coroutine _animRoutine;

        public DoorDef Def => _def;
        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        public void Configure(DoorDef def, SpriteRenderer leafA, SpriteRenderer leafB, Vector3 leafAOpenDirection, Vector3 leafBOpenDirection, float slideDistance)
        {
            _def = def;
            _leafA = leafA;
            _leafB = leafB;

            _leafAClosedPos = leafA.transform.localPosition;
            _leafBClosedPos = leafB.transform.localPosition;

            _leafAOpenPos = _leafAClosedPos + leafAOpenDirection * slideDistance;
            _leafBOpenPos = _leafBClosedPos + leafBOpenDirection * slideDistance;
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            if (_collider != null) _collider.enabled = false;
            Restart(_leafAOpenPos, _leafBOpenPos, enableColliderAtEnd: false);
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            Restart(_leafAClosedPos, _leafBClosedPos, enableColliderAtEnd: true);
        }

        private void Restart(Vector3 targetA, Vector3 targetB, bool enableColliderAtEnd)
        {
            if (_animRoutine != null) StopCoroutine(_animRoutine);
            _animRoutine = StartCoroutine(AnimateTo(targetA, targetB, enableColliderAtEnd));
        }

        private IEnumerator AnimateTo(Vector3 targetA, Vector3 targetB, bool enableColliderAtEnd)
        {
            Vector3 startA = _leafA.transform.localPosition;
            Vector3 startB = _leafB.transform.localPosition;
            float t = 0f;

            while (t < openDuration)
            {
                t += Time.deltaTime;
                float f = Mathf.Clamp01(t / openDuration);
                _leafA.transform.localPosition = Vector3.Lerp(startA, targetA, f);
                _leafB.transform.localPosition = Vector3.Lerp(startB, targetB, f);
                yield return null;
            }

            _leafA.transform.localPosition = targetA;
            _leafB.transform.localPosition = targetB;

            if (enableColliderAtEnd && _collider != null) _collider.enabled = true;
            _animRoutine = null;
        }
    }
}