using System.Globalization;
using UnityEngine;
using TMPro;

namespace RoomGen.UI
{
    public class RoomWeightsPanel : MonoBehaviour
    {
        [SerializeField] private RoomBuilderController controller;

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
            desiredConnectionsInput.SetTextWithoutNotify(controller.CurrentDesiredConnections.ToString());
            chanceToConnectWhenBelowTargetInput.SetTextWithoutNotify(controller.CurrentChanceToConnectWhenBelowTarget.ToString(CultureInfo.InvariantCulture));
            selectionWeightInput.SetTextWithoutNotify(controller.CurrentSelectionWeight.ToString(CultureInfo.InvariantCulture));
        }

        private void HandleDesiredConnections(string value)
        {
            if (int.TryParse(value, out int result)) controller.SetDesiredConnections(result);
            RefreshFields();
        }

        private void HandleChanceToConnectWhenBelowTarget(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) controller.SetChanceToConnectWhenBelowTarget(result);
            RefreshFields();
        }

        private void HandleSelectionWeight(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)) controller.SetSelectionWeight(result);
            RefreshFields();
        }
    }
}