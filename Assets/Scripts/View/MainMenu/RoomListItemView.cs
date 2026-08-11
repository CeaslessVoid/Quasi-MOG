using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Networking;

namespace UI.MainMenu
{
    public class RoomListItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button joinButton;

        public void SetInfo(RoomInfo info, Action onJoin)
        {
            nameText.text = info.roomName;
            countText.text = $"{info.playerCount}/{info.maxPlayers}";
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(() => onJoin?.Invoke());
        }
    }
}
