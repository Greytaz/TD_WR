using UnityEngine;
using UnityEngine.InputSystem;
using TowerDefense.Core;
using TowerDefense.Data;
using TowerDefense.Utils;
using TowerDefense.Effects;

namespace TowerDefense.Towers
{
    public class TowerPlacement : MonoBehaviour
    {
        public static TowerPlacement Instance { get; private set; }

        [Header("Visual Indicators")]
        public GameObject rangeIndicatorPrefab; // Flat circle prefab with range shader
        public GameObject cellHighlightPrefab;   // Flat quad showing selected grid cell

        private TowerData pendingTowerData = null;
        private GameObject spawnedRangeIndicator;
        private GameObject cellHighlight;
        private Camera mainCamera;
        private TowerBase selectedTower = null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            mainCamera = Camera.main;
        }

        private void Start()
        {
            if (rangeIndicatorPrefab != null)
            {
                spawnedRangeIndicator = Instantiate(rangeIndicatorPrefab);
                spawnedRangeIndicator.SetActive(false);
            }
            if (cellHighlightPrefab != null)
            {
                cellHighlight = Instantiate(cellHighlightPrefab);
                cellHighlight.SetActive(false);
            }
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                HideIndicators();
                return;
            }

            Vector2 inputPos = GetCurrentInputPosition();
            bool isPressing = GetInputPressThisFrame();

            Ray ray = mainCamera.ScreenPointToRay(inputPos);
            // Create a plane at y=0 representing the ground grid
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                if (GridManager.Instance.WorldToGrid(hitPoint, out int x, out int z))
                {
                    Vector3 cellCenter = GridManager.Instance.GetCellWorldPosition(x, z);

                    if (pendingTowerData != null)
                    {
                        // PLACEMENT MODE
                        ShowPlacementIndicators(cellCenter, x, z);

                        if (isPressing)
                        {
                            TryPlaceTower(x, z);
                        }
                    }
                    else
                    {
                        // SELECTION MODE
                        HidePlacementIndicatorsOnly();

                        if (isPressing)
                        {
                            TrySelectOrDeselectTower(x, z);
                        }
                    }
                }
                else
                {
                    HideIndicators();
                    if (isPressing)
                    {
                        DeselectTower();
                    }
                }
            }
            else
            {
                HideIndicators();
            }
        }

        public void SetPendingTower(TowerData data)
        {
            pendingTowerData = data;
            DeselectTower();
        }

        private void ShowPlacementIndicators(Vector3 cellCenter, int x, int z)
        {
            bool buildable = GridManager.Instance.IsCellBuildable(x, z);

            // Positioning cell highlight
            if (cellHighlight != null)
            {
                cellHighlight.transform.position = cellCenter + new Vector3(0f, 0.05f, 0f); // slight hover
                float cellSize = GridManager.Instance.cellSize;
                cellHighlight.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                cellHighlight.SetActive(true);

                // Set highlight color: green if buildable, red if blocked
                Renderer highlightRenderer = cellHighlight.GetComponentInChildren<Renderer>();
                if (highlightRenderer != null)
                {
                    highlightRenderer.material.color = buildable ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
                }
            }

            // Position and scale range indicator
            if (spawnedRangeIndicator != null && pendingTowerData != null)
            {
                spawnedRangeIndicator.transform.position = cellCenter + new Vector3(0f, 0.08f, 0f);
                
                // Radius * 2 to represent diameter
                float range = pendingTowerData.tier1.range;
                float diameter = range * 2f;
                spawnedRangeIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);
                spawnedRangeIndicator.SetActive(true);

                Renderer rangeRenderer = spawnedRangeIndicator.GetComponentInChildren<Renderer>();
                if (rangeRenderer != null)
                {
                    rangeRenderer.material.color = new Color(0f, 0.8f, 1f, 0.25f);
                }
            }
        }

        private void HideIndicators()
        {
            if (cellHighlight != null) cellHighlight.SetActive(false);
            if (spawnedRangeIndicator != null && selectedTower == null) spawnedRangeIndicator.SetActive(false);
        }

        private void HidePlacementIndicatorsOnly()
        {
            if (cellHighlight != null) cellHighlight.SetActive(false);
            if (spawnedRangeIndicator != null && selectedTower == null) spawnedRangeIndicator.SetActive(false);
        }

        private void TryPlaceTower(int x, int z)
        {
            if (pendingTowerData == null) return;

            int cost = pendingTowerData.tier1.cost;

            if (GridManager.Instance.IsCellBuildable(x, z))
            {
                if (GameManager.Instance.SpendGold(cost))
                {
                    // Spawn and Place Tower
                    GameObject towerObj = Instantiate(pendingTowerData.prefab);
                    TowerBase towerInstance = towerObj.GetComponent<TowerBase>();
                    towerInstance.Initialize(pendingTowerData);

                    GridManager.Instance.PlaceTower(x, z, towerInstance);

                    // Spawn dust/placement VFX
                    if (ParticleManager.Instance != null)
                    {
                        ParticleManager.Instance.SpawnParticle("PlacementDust", towerInstance.transform.position, 1.0f);
                    }

                    EventBus.TriggerTowerPlaced();
                    pendingTowerData = null; // Clear pending
                    HideIndicators();
                }
                else
                {
                    Debug.Log("Not enough gold to build this tower!");
                    pendingTowerData = null; // Clear pending
                    HideIndicators();
                }
            }
            else
            {
                pendingTowerData = null; // Clear pending
                HideIndicators();
            }
        }

        private void TrySelectOrDeselectTower(int x, int z)
        {
            // Raycast on click was inside grid
            TowerBase tower = GridManager.Instance.GetTowerAtCell(x, z);
            if (tower != null)
            {
                SelectTower(tower);
            }
            else
            {
                DeselectTower();
            }
        }

        private void SelectTower(TowerBase tower)
        {
            selectedTower = tower;
            EventBus.TriggerTowerSelected(tower);

            // Show range indicator of selected tower
            if (spawnedRangeIndicator != null && selectedTower != null)
            {
                spawnedRangeIndicator.transform.position = selectedTower.transform.position + new Vector3(0f, 0.08f, 0f);
                float range = selectedTower.CurrentStats.range;
                float diameter = range * 2f;
                spawnedRangeIndicator.transform.localScale = new Vector3(diameter, diameter, 1f);
                spawnedRangeIndicator.SetActive(true);

                Renderer rangeRenderer = spawnedRangeIndicator.GetComponentInChildren<Renderer>();
                if (rangeRenderer != null)
                {
                    rangeRenderer.material.color = new Color(1f, 1f, 1f, 0.25f); // White range for selected
                }
            }
        }

        public void DeselectTower()
        {
            selectedTower = null;
            if (spawnedRangeIndicator != null)
            {
                spawnedRangeIndicator.SetActive(false);
            }
            EventBus.TriggerTowerDeselected();
        }

        // Helper to update range indicator if tower upgrades
        public void UpdateSelectedTowerIndicator()
        {
            if (selectedTower != null)
            {
                SelectTower(selectedTower);
            }
        }

        private Vector2 GetCurrentInputPosition()
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                return Touchscreen.current.primaryTouch.position.ReadValue();
            }
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }
            return Vector2.zero;
        }

        private bool GetInputPressThisFrame()
        {
            // Avoid clicking through UI
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // On mobile we check touch id, on desktop IsPointerOverGameObject() works
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                {
                    int pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                    if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(pointerId))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }
    }
}
