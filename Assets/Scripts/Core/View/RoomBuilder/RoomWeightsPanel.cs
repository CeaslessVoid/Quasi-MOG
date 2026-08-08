using System.Globalization;
using UnityEngine;
using TMPro;

namespace RoomGen.UI
{
    public class RoomWeightsPanel : TopBarWindowPanel
    {
        [SerializeField] private TMP_InputField desiredConnectionsInput;
        [SerializeField] private TMP_InputField chanceToConnectWhenBelowTargetInput;
        [SerializeField] private TMP_InputField selectionWeightInput;

        private void Awake()
        {
            desiredConnectionsInput.onEndEdit.AddListener(HandleDesiredConnections);
            chanceToConnectWhenBelowTargetInput.onEndEdit.AddListener(HandleChanceToConnectWhenBelowTarget);
            selectionWeightInput.onEndEdit.AddListener(HandleSelectionWeight);
        }

        private void OnEnable() => RefreshFields();

        private void RefreshFields()
        {
            if (!Controller) return;
            Debug.Log(1);
            desiredConnectionsInput.SetTextWithoutNotify(Controller.CurrentDesiredConnections.ToString());
            Debug.Log(2);
            chanceToConnectWhenBelowTargetInput.SetTextWithoutNotify(Controller.CurrentChanceToConnectWhenBelowTarget.ToString(CultureInfo.InvariantCulture));
            Debug.Log(3);
            selectionWeightInput.SetTextWithoutNotify(Controller.CurrentSelectionWeight.ToString(CultureInfo.InvariantCulture));
        }

        private void HandleDesiredConnections(string value)
        {
            if (int.TryParse(value, out int result)) Controller.SetDesiredConnections(result);
            RefreshFields();
        }

        private void HandleChanceToConnectWhenBelowTarget(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) Controller.SetChanceToConnectWhenBelowTarget(result);
            RefreshFields();
        }

        private void HandleSelectionWeight(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) Controller.SetSelectionWeight(result);
            RefreshFields();
        }
    }
}
