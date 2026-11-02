using UnityEngine;
using UnityEngine.UI;

namespace ARBlueprint
{
    /// <summary>Connects the mobile UI to placement, floor selection, the 2D/3D view, and the floor creator.</summary>
    public class UIManager : MonoBehaviour
    {
        private static readonly Color Normal = new Color(0.18f, 0.22f, 0.28f);
        private static readonly Color Selected = new Color(0.15f, 0.65f, 0.85f);

        [SerializeField] private ARPlacementManager placementManager;
        [SerializeField] private FloorCreatorPanel creatorPanel;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button floor1Button;
        [SerializeField] private Button floor2Button;
        [SerializeField] private Button floor3Button;
        [SerializeField] private Button stackedButton;
        [SerializeField] private Button plan2DButton;
        [SerializeField] private Button convert3DButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button defaultPlansButton;
        [SerializeField] private Button resetButton;

        private BuildingController currentBuilding;

        private void Start()
        {
            floor1Button.onClick.AddListener(() => currentBuilding?.SelectFloor(1));
            floor2Button.onClick.AddListener(() => currentBuilding?.SelectFloor(2));
            floor3Button.onClick.AddListener(() => currentBuilding?.SelectFloor(3));
            stackedButton.onClick.AddListener(() => currentBuilding?.SelectFloor(BuildingController.Stacked));
            plan2DButton.onClick.AddListener(() => currentBuilding?.SetMode3D(false));
            convert3DButton.onClick.AddListener(() => currentBuilding?.SetMode3D(true));
            createButton.onClick.AddListener(() => creatorPanel.Open());
            defaultPlansButton.onClick.AddListener(() => placementManager.ApplyBuildingSpec(BuildingSpec.CreateDefault()));
            resetButton.onClick.AddListener(() => placementManager.ResetPlacement());
            OnPlacementReset();
        }

        public void OnBuildingPlaced(BuildingController building)
        {
            if (currentBuilding != null)
                currentBuilding.Changed -= Refresh;

            currentBuilding = building;
            currentBuilding.Changed += Refresh;
            Refresh();
        }

        public void OnPlacementReset()
        {
            if (currentBuilding != null)
                currentBuilding.Changed -= Refresh;

            currentBuilding = null;
            creatorPanel?.Close();
            Refresh();
        }

        public void SetStatus(string message)
        {
            if (statusText != null && currentBuilding == null && statusText.text != message)
                statusText.text = message;
        }

        /// <summary>Called after a new set of floors was applied from the creator or the default plans.</summary>
        public void OnBuildingRebuilt()
        {
            Refresh();
        }

        private void Refresh()
        {
            bool placed = currentBuilding != null;
            int count = placed ? currentBuilding.FloorCount : 0;
            int selected = placed ? currentBuilding.SelectedFloor : -1;
            bool is3D = placed && currentBuilding.Is3D;

            Button[] floorButtons = { floor1Button, floor2Button, floor3Button };
            for (int i = 0; i < floorButtons.Length; i++)
            {
                floorButtons[i].interactable = placed && i < count;
                SetButtonColor(floorButtons[i], selected == i + 1);
            }

            stackedButton.interactable = placed && count >= 2;
            SetButtonColor(stackedButton, selected == BuildingController.Stacked);

            plan2DButton.interactable = placed;
            convert3DButton.interactable = placed;
            SetButtonColor(plan2DButton, placed && !is3D);
            SetButtonColor(convert3DButton, is3D);

            createButton.interactable = placed;
            defaultPlansButton.interactable = placed;
            resetButton.interactable = placed;

            if (!placed)
            {
                instructionText.text = "Move the phone slowly, then tap a detected surface";
                statusText.text = "Searching for a horizontal plane...";
                return;
            }

            instructionText.text = is3D
                ? "3D model - drag, twist, or pinch to adjust"
                : "2D blueprint - tap CONVERT TO 3D to raise the walls";
            statusText.text = currentBuilding.DescribeSelection();
        }

        private static void SetButtonColor(Button button, bool selected)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? Selected : Normal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
    }
}
