using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARBlueprint
{
    /// <summary>
    /// Owns the generated floors of the building. Shows one floor or all floors stacked, as a 2D blueprint
    /// or as the 3D model that is extruded from it.
    /// </summary>
    public class BuildingController : MonoBehaviour
    {
        public const int Stacked = 0;

        [SerializeField] private float stackSpacing = 0.75f;

        private readonly List<FloorView> floors = new List<FloorView>();
        private BuildingSpec spec;

        public event Action Changed;

        public int FloorCount => floors.Count;
        public int SelectedFloor { get; private set; } = 1;
        public bool Is3D { get; private set; }

        /// <summary>(Re)builds every floor from the spec and shows the 2D blueprint of floor 1.</summary>
        public void Initialize(BuildingSpec buildingSpec)
        {
            foreach (FloorView floor in floors)
            {
                if (floor != null)
                    Destroy(floor.gameObject);
            }

            floors.Clear();
            spec = buildingSpec.Clone();
            for (int i = 0; i < spec.floors.Count && i < BuildingSpec.MaxFloors; i++)
                floors.Add(FloorGenerator.Build(spec.floors[i], i, transform));

            SelectedFloor = 1;
            Is3D = false;
            Apply(false);
            Changed?.Invoke();
        }

        /// <param name="floorNumber">1 to 3 for a single floor, or <see cref="Stacked"/> for all floors.</param>
        public void SelectFloor(int floorNumber)
        {
            if (floorNumber != Stacked && (floorNumber < 1 || floorNumber > floors.Count))
                return;

            SelectedFloor = floorNumber;
            Apply(false);
            Changed?.Invoke();
        }

        /// <summary>Switches between the 2D blueprint and the 3D model; the walls rise from the plan.</summary>
        public void SetMode3D(bool show3D)
        {
            if (Is3D == show3D)
                return;

            Is3D = show3D;
            Apply(true);
            Changed?.Invoke();
        }

        public string DescribeSelection()
        {
            if (floors.Count == 0)
                return string.Empty;

            string what = SelectedFloor == Stacked
                ? $"All {floors.Count} floors stacked"
                : floors[SelectedFloor - 1].DisplayName;
            return Is3D ? what + " - 3D model" : what + " - 2D blueprint";
        }

        private void Apply(bool animateMode)
        {
            for (int i = 0; i < floors.Count; i++)
            {
                bool visible = SelectedFloor == Stacked || SelectedFloor == i + 1;
                FloorView floor = floors[i];
                floor.gameObject.SetActive(visible);
                floor.transform.localPosition = SelectedFloor == Stacked
                    ? Vector3.up * (i * stackSpacing)
                    : Vector3.zero;

                bool modeChanged = floor.Is3D != Is3D;
                if (visible && modeChanged)
                    floor.ShowMode(Is3D, animateMode);
                else if (!visible && modeChanged)
                    floor.ShowMode(Is3D, false);
            }
        }
    }
}
