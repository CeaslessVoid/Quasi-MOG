using TMPro;
using UnityEngine.EventSystems;

namespace Util
{
    public static class InputFocusUtility
    {
        public static bool IsTypingInField
        {
            get
            {
                var es = EventSystem.current;
                if (es == null) return false;
                var selected = es.currentSelectedGameObject;
                if (selected == null) return false;
                var field = selected.GetComponent<TMP_InputField>();
                return field != null && field.isFocused;
            }
        }
    }
}