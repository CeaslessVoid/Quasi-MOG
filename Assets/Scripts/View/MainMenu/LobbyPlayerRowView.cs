using UnityEngine;
using TMPro;
using Networking;

namespace UI.MainMenu
{
    public class LobbyPlayerRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;

        public void SetInfo(PlayerInfo info)
        {
            nameText.text = info.playerName.ToString();
        }
    }
}
