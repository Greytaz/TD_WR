using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Towers;

namespace TowerDefense.Core
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Size")]
        public int gridWidth = 10;
        public int gridHeight = 15;
        public float cellSize = 1.5f;
        public Vector3 gridOrigin = Vector3.zero;

        [Header("Waypoints")]
        [Tooltip("The path enemies will walk. Towers cannot be built on or too close to this path.")]
        public List<Vector3> pathWaypoints = new List<Vector3>();

        private TowerBase[,] towerGrid;
        private HashSet<Vector2Int> pathCells = new HashSet<Vector2Int>();
        private GameObject[,] visualTiles;

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

            towerGrid = new TowerBase[gridWidth, gridHeight];
            
            if (Application.isPlaying)
            {
                GenerateProceduralPath();
            }
            else
            {
                CalculatePathCells();
            }
        }

        private void Start()
        {
            // Visual grid is generated inside GenerateProceduralPath if in play mode,
            // but if we are not in play mode or if it hasn't been generated, let's call it.
            if (Application.isPlaying && (visualTiles == null || visualTiles.Length == 0))
            {
                GenerateVisualGrid();
            }
        }

        private void GenerateVisualGrid()
        {
            var oldParent = transform.Find("--- grid visualizer ---");
            if (oldParent != null)
            {
                Destroy(oldParent.gameObject);
            }

            GameObject gridVisualParent = new GameObject("--- grid visualizer ---");
            gridVisualParent.transform.SetParent(transform);

            Shader pipelineShader = Shader.Find("Universal Render Pipeline/Lit");
            if (pipelineShader == null) pipelineShader = Shader.Find("Lightweight Render Pipeline/Lit");
            if (pipelineShader == null) pipelineShader = Shader.Find("Standard");

            Material pathMat = new Material(pipelineShader);
            pathMat.color = new Color(0.35f, 0.15f, 0.15f, 0.8f); // Reddish-brown road
            
            Material buildableMat = new Material(pipelineShader);
            buildableMat.color = new Color(0.15f, 0.35f, 0.15f, 0.35f); // Subtle green tile

            visualTiles = new GameObject[gridWidth, gridHeight];

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 cellWorldPos = GetCellWorldPosition(x, z);
                    
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{z}";
                    tile.transform.SetParent(gridVisualParent.transform);
                    tile.transform.position = cellWorldPos + new Vector3(0f, 0.015f, 0f); // Hover just above ground to avoid z-fighting
                    tile.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    tile.transform.localScale = new Vector3(cellSize * 0.95f, cellSize * 0.95f, 1f); // Grid margins look nice

                    var col = tile.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    var r = tile.GetComponent<Renderer>();
                    if (r != null)
                    {
                        if (pathCells.Contains(new Vector2Int(x, z)))
                        {
                            r.sharedMaterial = pathMat;
                        }
                        else
                        {
                            r.sharedMaterial = buildableMat;
                        }
                    }

                    visualTiles[x, z] = tile;
                }
            }
        }

        private void CalculatePathCells()
        {
            pathCells.Clear();
            if (pathWaypoints == null || pathWaypoints.Count < 2) return;

            // Mark any grid cell that is close to any segment of the path
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 cellWorldPos = GetCellWorldPosition(x, z);
                    if (IsPointCloseToPath(cellWorldPos, cellSize * 0.75f))
                    {
                        pathCells.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        private bool IsPointCloseToPath(Vector3 point, float threshold)
        {
            for (int i = 0; i < pathWaypoints.Count - 1; i++)
            {
                Vector3 p1 = pathWaypoints[i];
                Vector3 p2 = pathWaypoints[i + 1];
                
                // Keep computations in 2D (X/Z plane)
                p1.y = 0;
                p2.y = 0;
                Vector3 p = point;
                p.y = 0;

                float dist = MinimumDistanceToSegment(p1, p2, p);
                if (dist < threshold)
                {
                    return true;
                }
            }
            return false;
        }

        private float MinimumDistanceToSegment(Vector3 p1, Vector3 p2, Vector3 p)
        {
            Vector3 v = p2 - p1;
            Vector3 w = p - p1;

            float c1 = Vector3.Dot(w, v);
            if (c1 <= 0) return Vector3.Distance(p, p1);

            float c2 = Vector3.Dot(v, v);
            if (c2 <= c1) return Vector3.Distance(p, p2);

            float b = c1 / c2;
            Vector3 pb = p1 + b * v;
            return Vector3.Distance(p, pb);
        }

        public List<Vector3> GetPathWaypoints()
        {
            return pathWaypoints;
        }

        [Header("Procedural Setup")]
        [Tooltip("Toggle procedural generation. If disabled, loads the handcrafted reference path.")]
        public bool useProceduralPath = true;

        [Header("Reference Path (Benchmark)")]
        [Tooltip("The reference handcrafted benchmark route.")]
        public List<Vector3> referencePathWaypoints = new List<Vector3>()
        {
            new Vector3(1.51f, 0f, 0.99f),
            new Vector3(1.5f, 0f, 10.5f),
            new Vector3(8.93f, 0f, 10.5f),
            new Vector3(9.13f, 0f, 19.5f),
            new Vector3(1.4f, 0f, 19.5f)
        };

        public HashSet<Vector2Int> GetPathCells()
        {
            return pathCells;
        }

        public List<Vector2Int> GetOrderedPathCells()
        {
            List<Vector2Int> list = new List<Vector2Int>();
            foreach (var wp in pathWaypoints)
            {
                Vector3 localPos = wp - gridOrigin;
                int ix = Mathf.RoundToInt(localPos.x / cellSize);
                int iz = Mathf.RoundToInt(localPos.z / cellSize);
                list.Add(new Vector2Int(ix, iz));
            }
            return list;
        }

        public void GenerateProceduralPath()
        {
            // Clear existing towers from the grid
            ClearGrid();

            if (!useProceduralPath)
            {
                Debug.Log("[GridManager] Loading reference benchmark path waypoints.");
                pathWaypoints = new List<Vector3>(referencePathWaypoints);
                CalculatePathCells();
                if (Application.isPlaying)
                {
                    GenerateVisualGrid();
                }
                return;
            }

            System.Random rand = new System.Random();
            
            // Randomly choose between a vertical-start path or a horizontal-start path
            bool verticalStart = rand.Next(2) == 0;

            pathWaypoints = new List<Vector3>();
            pathCells.Clear();

            if (verticalStart)
            {
                // Vertical-first path (from bottom to top or top to bottom)
                bool startBottom = rand.Next(2) == 0;
                
                // P0 starts on one edge
                int ix0 = rand.Next(1, gridWidth);
                int iz0 = startBottom ? 0 : gridHeight;

                // Intermediate turning points
                int iz1 = startBottom ? rand.Next(3, gridHeight / 2) : rand.Next(gridHeight / 2 + 1, gridHeight - 2);
                
                int ix2 = rand.Next(1, gridWidth);
                while (ix2 == ix0 && gridWidth > 2)
                {
                    ix2 = rand.Next(1, gridWidth);
                }

                int iz3 = startBottom ? rand.Next(gridHeight / 2 + 1, gridHeight - 2) : rand.Next(2, gridHeight / 2);
                
                int ix4 = rand.Next(1, gridWidth);
                while (ix4 == ix2 && gridWidth > 2)
                {
                    ix4 = rand.Next(1, gridWidth);
                }

                pathWaypoints.Add(gridOrigin + new Vector3(ix0 * cellSize, 0f, iz0 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix0 * cellSize, 0f, iz1 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix2 * cellSize, 0f, iz1 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix2 * cellSize, 0f, iz3 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix4 * cellSize, 0f, iz3 * cellSize));
            }
            else
            {
                // Horizontal-first path (from left to right or right to left)
                bool startLeft = rand.Next(2) == 0;

                int ix0 = startLeft ? 0 : gridWidth;
                int iz0 = rand.Next(1, gridHeight);

                int ix1 = startLeft ? rand.Next(3, gridWidth / 2) : rand.Next(gridWidth / 2 + 1, gridWidth - 2);
                
                int iz2 = rand.Next(1, gridHeight);
                while (iz2 == iz0 && gridHeight > 2)
                {
                    iz2 = rand.Next(1, gridHeight);
                }

                int ix3 = startLeft ? rand.Next(gridWidth / 2 + 1, gridWidth - 2) : rand.Next(2, gridWidth / 2);
                
                int iz4 = rand.Next(1, gridHeight);
                while (iz4 == iz2 && gridHeight > 2)
                {
                    iz4 = rand.Next(1, gridHeight);
                }

                pathWaypoints.Add(gridOrigin + new Vector3(ix0 * cellSize, 0f, iz0 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix1 * cellSize, 0f, iz0 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix1 * cellSize, 0f, iz2 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix3 * cellSize, 0f, iz2 * cellSize));
                pathWaypoints.Add(gridOrigin + new Vector3(ix3 * cellSize, 0f, iz4 * cellSize));
            }

            CalculatePathCells();

            if (Application.isPlaying)
            {
                GenerateVisualGrid();
            }
        }

        public void RestoreProceduralPath(List<Vector2Int> path)
        {
            ClearGrid();

            pathWaypoints = new List<Vector3>();
            pathCells.Clear();

            foreach (var cell in path)
            {
                pathWaypoints.Add(gridOrigin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize));
            }

            CalculatePathCells();

            if (Application.isPlaying)
            {
                GenerateVisualGrid();
            }
        }

        public Vector3 GetCellWorldPosition(int x, int z)
        {
            return gridOrigin + new Vector3(
                (x * cellSize) + (cellSize * 0.5f), 
                0f, 
                (z * cellSize) + (cellSize * 0.5f)
            );
        }

        public bool WorldToGrid(Vector3 worldPos, out int x, out int z)
        {
            Vector3 localPos = worldPos - gridOrigin;
            x = Mathf.FloorToInt(localPos.x / cellSize);
            z = Mathf.FloorToInt(localPos.z / cellSize);

            return x >= 0 && x < gridWidth && z >= 0 && z < gridHeight;
        }

        public bool IsCellBuildable(int x, int z)
        {
            if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight) return false;
            
            // Check if it's on path
            if (pathCells.Contains(new Vector2Int(x, z))) return false;

            // Check if there is already a tower
            if (towerGrid[x, z] != null) return false;

            return true;
        }

        public bool PlaceTower(int x, int z, TowerBase tower)
        {
            if (!IsCellBuildable(x, z)) return false;

            towerGrid[x, z] = tower;
            tower.transform.position = GetCellWorldPosition(x, z);

            // Hide the green highlight tile because a tower is standing here
            if (visualTiles != null && x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                if (visualTiles[x, z] != null)
                {
                    visualTiles[x, z].SetActive(false);
                }
            }
            return true;
        }

        public void FreeCell(int x, int z)
        {
            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                towerGrid[x, z] = null;

                // Re-enable the green highlight tile
                if (visualTiles != null && visualTiles[x, z] != null)
                {
                    visualTiles[x, z].SetActive(true);
                }
            }
        }

        public void ClearGrid()
        {
            if (towerGrid == null) return;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (towerGrid[x, z] != null)
                    {
                        Destroy(towerGrid[x, z].gameObject);
                        towerGrid[x, z] = null;
                        if (visualTiles != null && visualTiles[x, z] != null)
                        {
                            visualTiles[x, z].SetActive(true);
                        }
                    }
                }
            }
        }

        public TowerBase GetTowerAtCell(int x, int z)
        {
            if (x >= 0 && x < gridWidth && z >= 0 && z < gridHeight)
            {
                return towerGrid[x, z];
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            // Simple visualizer in Editor
            Gizmos.color = Color.white;
            for (int x = 0; x <= gridWidth; x++)
            {
                Gizmos.DrawLine(
                    gridOrigin + new Vector3(x * cellSize, 0f, 0f), 
                    gridOrigin + new Vector3(x * cellSize, 0f, gridHeight * cellSize)
                );
            }
            for (int z = 0; z <= gridHeight; z++)
            {
                Gizmos.DrawLine(
                    gridOrigin + new Vector3(0f, 0f, z * cellSize), 
                    gridOrigin + new Vector3(gridWidth * cellSize, 0f, z * cellSize)
                );
            }

            // Draw path cells as red and buildable cells as green
            if (!Application.isPlaying)
            {
                // Force recalculate cells for visualization in Edit mode
                CalculatePathCells();
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    Vector3 pos = GetCellWorldPosition(x, z);
                    if (pathCells.Contains(new Vector2Int(x, z)))
                    {
                        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
                        Gizmos.DrawCube(pos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                    }
                    else if (towerGrid != null && towerGrid[x, z] != null)
                    {
                        Gizmos.color = new Color(0f, 0f, 1f, 0.4f);
                        Gizmos.DrawCube(pos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                    }
                    else
                    {
                        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
                        Gizmos.DrawCube(pos, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
                    }
                }
            }

            // Draw Waypoint line
            if (pathWaypoints != null && pathWaypoints.Count > 1)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < pathWaypoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(pathWaypoints[i], pathWaypoints[i + 1]);
                    Gizmos.DrawSphere(pathWaypoints[i], 0.2f);
                }
                Gizmos.DrawSphere(pathWaypoints[pathWaypoints.Count - 1], 0.2f);
            }
        }
    }
}
