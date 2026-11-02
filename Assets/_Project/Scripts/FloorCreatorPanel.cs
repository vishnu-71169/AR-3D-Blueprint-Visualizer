using System;
using UnityEngine;
using UnityEngine.UI;

namespace ARBlueprint
{
    /// <summary>
    /// Floor creator: choose the number of floors (up to three) and, for each floor, a layout, number of
    /// rooms, wall colour, flooring, and furniture. Every option button cycles through its values.
    /// </summary>
    public class FloorCreatorPanel : MonoBehaviour
    {
        [SerializeField] private ARPlacementManager placementManager;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button floorCountButton;
        [SerializeField] private Button tab1Button;
        [SerializeField] private Button tab2Button;
        [SerializeField] private Button tab3Button;
        [SerializeField] private Button layoutButton;
        [SerializeField] private Button roomsButton;
        [SerializeField] private Button wallsButton;
        [SerializeField] private Image wallSwatch;
        [SerializeField] private Button flooringButton;
        [SerializeField] private Button furnitureButton;
        [SerializeField] private Button buildButton;
        [SerializeField] private Button closeButton;

        private static readonly Color Normal = new Color(0.18f, 0.22f, 0.28f);
        private static readonly Color Selected = new Color(0.15f, 0.65f, 0.85f);

        private BuildingSpec draft = BuildingSpec.CreateDefault();
        private int editing;

        private void Start()
        {
            floorCountButton.onClick.AddListener(CycleFloorCount);
            tab1Button.onClick.AddListener(() => Edit(0));
            tab2Button.onClick.AddListener(() => Edit(1));
            tab3Button.onClick.AddListener(() => Edit(2));
            layoutButton.onClick.AddListener(() => Change(f => f.layout = Next(f.layout)));
            roomsButton.onClick.AddListener(() => Change(f => f.roomCount = f.roomCount % 4 + 1));
            wallsButton.onClick.AddListener(() => Change(f => f.wallColor = Next(f.wallColor)));
            flooringButton.onClick.AddListener(() => Change(f => f.flooring = Next(f.flooring)));
            furnitureButton.onClick.AddListener(() => Change(f => f.furniture = !f.furniture));
            buildButton.onClick.AddListener(Build);
            closeButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            draft = BuildingConfig.Current.Clone();
            editing = 0;
            Refresh();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void Build()
        {
            if (placementManager == null)
                placementManager = FindFirstObjectByType<ARPlacementManager>();

            placementManager.ApplyBuildingSpec(draft);
            Close();
        }

        private void CycleFloorCount()
        {
            int count = draft.floors.Count % BuildingSpec.MaxFloors + 1;
            while (draft.floors.Count < count)
                draft.floors.Add(BuildingSpec.DefaultFloor(draft.floors.Count));
            while (draft.floors.Count > count)
                draft.floors.RemoveAt(draft.floors.Count - 1);

            editing = Mathf.Min(editing, draft.floors.Count - 1);
            Refresh();
        }

        private void Edit(int index)
        {
            if (index >= draft.floors.Count)
                return;

            editing = index;
            Refresh();
        }

        private void Change(Action<FloorSpec> change)
        {
            change(draft.floors[editing]);
            Refresh();
        }

        private static T Next<T>(T value) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            int index = Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
        }

        private void Refresh()
        {
            FloorSpec floor = draft.floors[editing];
            bool custom = floor.layout == FloorLayout.Custom;
            bool terrace = floor.layout == FloorLayout.Terrace;

            SetLabel(floorCountButton, $"NUMBER OF FLOORS: {draft.floors.Count}");
            Button[] tabs = { tab1Button, tab2Button, tab3Button };
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].interactable = i < draft.floors.Count;
                SetLabel(tabs[i], $"FLOOR {i + 1}");
                SetColor(tabs[i], i == editing);
            }

            SetLabel(layoutButton, $"LAYOUT: {floor.LayoutName}");
            roomsButton.interactable = custom;
            SetLabel(roomsButton, custom ? $"ROOMS: {floor.roomCount}" : "ROOMS: set by layout");
            SetLabel(wallsButton, $"WALL COLOUR: {MaterialLibrary.WallColorName(floor.wallColor)}");
            wallSwatch.color = MaterialLibrary.WallColor(floor.wallColor);
            flooringButton.interactable = custom;
            SetLabel(flooringButton, custom ? $"FLOORING: {floor.flooring}" : "FLOORING: set by layout");
            SetLabel(furnitureButton, terrace
                ? $"PERGOLA AND PLANTS: {(floor.furniture ? "On" : "Off")}"
                : $"FURNITURE: {(floor.furniture ? "On" : "Off")}");
        }

        private static void SetLabel(Button button, string text)
        {
            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = text;
        }

        private static void SetColor(Button button, bool selected)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = selected ? Selected : Normal;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }
    }
}
