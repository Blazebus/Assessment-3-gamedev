using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class LevelGenerator : MonoBehaviour
{
    public GameObject[] tilePrefabs = new GameObject[8];
    public GameObject outsideCornerPrefab;
    public GameObject outsideWallPrefab;
    public GameObject insideCornerPrefab;
    public GameObject insideWallPrefab;
    public GameObject pelletSpacePrefab;
    public GameObject powerPelletSpacePrefab;
    public GameObject tJunctionPrefab;
    public GameObject ghostExitPrefab;

    public Sprite outsideCornerSprite;
    public Sprite outsideWallSprite;
    public Sprite insideCornerSprite;
    public Sprite insideWallSprite;
    public Sprite pelletSpaceSprite;
    public Sprite powerPelletSpaceSprite;
    public Sprite tJunctionSprite;
    public Sprite ghostExitSprite;

    public GameObject pelletPrefab;
    public GameObject powerPelletPrefab;

    public float tileSize = 1.0f;
    public Transform levelRoot;
    public GameObject manualLevel;
    public Camera targetCamera;

    public int[,] levelMap = new int[15, 14]
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0}
    };

    private void Start()
    {
        DeleteManualLevel();
        GenerateLevel();
        AutoFitCamera();
    }

    public void DeleteManualLevel()
    {
        if (manualLevel != null)
        {
            Destroy(manualLevel);
            manualLevel = null;
        }
        else
        {
            string[] candidates = new string[] { "Walls", "ManualLevel", "Manual Layout", "Manual_Level" };
            foreach (var name in candidates)
            {
                var go = GameObject.Find(name);
                if (go != null && go != gameObject && (levelRoot == null || go != levelRoot.gameObject))
                {
                    Destroy(go);
                }
            }
        }
    }

    [ContextMenu("Generate Level")]
    public void GenerateLevel()
    {
        ClearLevel();

        int qRows = levelMap.GetLength(0);
        int qCols = levelMap.GetLength(1);

        int totalCols = qCols * 2;
        int totalRows = qRows * 2 - 1;

        int[,] fullGrid = BuildFullMirroredGrid(levelMap, qRows, qCols, totalRows, totalCols);

        if (levelRoot == null)
        {
            var existing = transform.Find("GeneratedLevel");
            if (existing != null)
            {
                levelRoot = existing;
            }
            else
            {
                GameObject rootGo = new GameObject("GeneratedLevel");
                rootGo.transform.SetParent(transform, false);
                levelRoot = rootGo.transform;
            }
        }

        float startX = -(totalCols - 1) * tileSize * 0.5f;
        float startY = (totalRows - 1) * tileSize * 0.5f;

        Transform qTL = CreateQuadrantParent("TopLeft");
        Transform qTR = CreateQuadrantParent("TopRight");
        Transform qBL = CreateQuadrantParent("BottomLeft");
        Transform qBR = CreateQuadrantParent("BottomRight");

        for (int r = 0; r < totalRows; r++)
        {
            for (int c = 0; c < totalCols; c++)
            {
                int tileType = fullGrid[r, c];
                if (tileType == 0)
                    continue;

                Vector3 pos = new Vector3(startX + c * tileSize, startY - r * tileSize, 0f);
                float rotationZ = CalculateRotationFromNeighbours(fullGrid, totalRows, totalCols, r, c, tileType);

                Transform parentGroup;
                if (r < qRows && c < qCols) parentGroup = qTL;
                else if (r < qRows && c >= qCols) parentGroup = qTR;
                else if (r >= qRows && c < qCols) parentGroup = qBL;
                else parentGroup = qBR;

                GameObject tileObj = InstantiateTile(tileType, pos, rotationZ, parentGroup);
                if (tileObj != null)
                {
                    tileObj.name = string.Format("Tile_{0:D2}_{1:D2}_Type{2}", r, c, tileType);
                }

                if (tileType == 5 && pelletPrefab != null)
                {
                    GameObject p;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        p = (GameObject)PrefabUtility.InstantiatePrefab(pelletPrefab, parentGroup);
                        p.transform.position = pos;
                        p.transform.rotation = Quaternion.identity;
                    }
                    else
#endif
                    {
                        p = Instantiate(pelletPrefab, pos, Quaternion.identity, parentGroup);
                    }
                    p.name = string.Format("Pellet_{0}_{1}", r, c);
                }
                else if (tileType == 6 && powerPelletPrefab != null)
                {
                    GameObject pp;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        pp = (GameObject)PrefabUtility.InstantiatePrefab(powerPelletPrefab, parentGroup);
                        pp.transform.position = pos;
                        pp.transform.rotation = Quaternion.identity;
                    }
                    else
#endif
                    {
                        pp = Instantiate(powerPelletPrefab, pos, Quaternion.identity, parentGroup);
                    }
                    pp.name = string.Format("PowerPellet_{0}_{1}", r, c);
                }
            }
        }
    }

    private Transform CreateQuadrantParent(string name)
    {
        Transform child = levelRoot.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(levelRoot, false);
            child = go.transform;
        }
        return child;
    }

    public static int[,] BuildFullMirroredGrid(int[,] map, int qRows, int qCols, int totalRows, int totalCols)
    {
        int[,] full = new int[totalRows, totalCols];

        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                int val = map[r, c];

                // top-left
                full[r, c] = val;

                // top-right (horizontal mirror)
                int mirrorCol = qCols + (qCols - 1 - c);
                full[r, mirrorCol] = val;

                // bottom-left (vertical mirror, excluding middle row)
                if (r < qRows - 1)
                {
                    int botR = (totalRows - 1) - r;
                    full[botR, c] = val;

                    // bottom-right (horizontal + vertical mirror)
                    full[botR, mirrorCol] = val;
                }
            }
        }

        return full;
    }

    public static float CalculateRotationFromNeighbours(int[,] grid, int totalRows, int totalCols, int r, int c, int tileType)
    {
        bool up = (r > 0 && IsWall(grid[r - 1, c]));
        bool down = (r < totalRows - 1 && IsWall(grid[r + 1, c]));
        bool left = (c > 0 && IsWall(grid[r, c - 1]));
        bool right = (c < totalCols - 1 && IsWall(grid[r, c + 1]));

        switch (tileType)
        {
            case 1:
            case 3:
                if (down && right && !up && !left) return 0f;
                if (up && right && !down && !left) return 90f;
                if (up && left && !down && !right) return 180f;
                if (down && left && !up && !right) return 270f;
                if (down && right) return 0f;
                if (up && right) return 90f;
                if (up && left) return 180f;
                if (down && left) return 270f;
                return 0f;

            case 2:
                bool upOutside = (r == 0 || grid[r - 1, c] == 0);
                bool downOutside = (r == totalRows - 1 || grid[r + 1, c] == 0);
                bool leftOutside = (c == 0 || grid[r, c - 1] == 0);
                bool rightOutside = (c == totalCols - 1 || grid[r, c + 1] == 0);

                if (up && down && !left && !right) return 90f;
                if (left && right && !up && !down) return 0f;

                if (leftOutside && !rightOutside) return 90f;
                if (rightOutside && !leftOutside) return 270f;
                if (upOutside && !downOutside) return 0f;
                if (downOutside && !upOutside) return 180f;

                if (up || down) return 90f;
                return 0f;

            case 4:
            case 8:
                if ((up || down) && !left && !right) return 90f;
                if (up && down) return 90f;
                return 0f;

            case 7:
                if (!up) return 0f;
                if (!left) return 90f;
                if (!down) return 180f;
                if (!right) return 270f;
                return 0f;

            default:
                return 0f;
        }
    }

    public static bool IsWall(int type)
    {
        return type == 1 || type == 2 || type == 3 || type == 4 || type == 7 || type == 8;
    }

    private GameObject InstantiateTile(int tileType, Vector3 pos, float rotZ, Transform parent)
    {
        GameObject prefab = GetPrefabForType(tileType);
        Quaternion rot = Quaternion.Euler(0, 0, rotZ);

        if (prefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                return obj;
            }
#endif
            return Instantiate(prefab, pos, rot, parent);
        }

        Sprite sprite = GetSpriteForType(tileType);
        if (sprite != null)
        {
            GameObject tileObj = new GameObject();
            tileObj.transform.SetParent(parent, false);
            tileObj.transform.position = pos;
            tileObj.transform.rotation = rot;

            var sr = tileObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            if (IsWall(tileType))
            {
                var col = tileObj.AddComponent<BoxCollider2D>();
                col.size = Vector2.one;
            }

            return tileObj;
        }

        return null;
    }

    public GameObject GetPrefabForType(int type)
    {
        if (tilePrefabs != null && type >= 1 && type <= tilePrefabs.Length && tilePrefabs[type - 1] != null)
        {
            return tilePrefabs[type - 1];
        }

        switch (type)
        {
            case 1: return outsideCornerPrefab;
            case 2: return outsideWallPrefab;
            case 3: return insideCornerPrefab;
            case 4: return insideWallPrefab;
            case 5: return pelletSpacePrefab;
            case 6: return powerPelletSpacePrefab;
            case 7: return tJunctionPrefab;
            case 8: return ghostExitPrefab;
            default: return null;
        }
    }

    public Sprite GetSpriteForType(int type)
    {
        switch (type)
        {
            case 1: return outsideCornerSprite;
            case 2: return outsideWallSprite;
            case 3: return insideCornerSprite;
            case 4: return insideWallSprite;
            case 5: return pelletSpaceSprite;
            case 6: return powerPelletSpaceSprite;
            case 7: return tJunctionSprite;
            case 8: return ghostExitSprite;
            default: return null;
        }
    }

    [ContextMenu("Auto Fit Camera")]
    public void AutoFitCamera()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        int qRows = levelMap.GetLength(0);
        int qCols = levelMap.GetLength(1);
        int totalCols = qCols * 2;
        int totalRows = qRows * 2 - 1;

        float levelWidth = totalCols * tileSize;
        float levelHeight = totalRows * tileSize;

        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographic = true;

        float padding = 1.0f;
        float halfHeight = (levelHeight * 0.5f) + padding;
        float halfWidth = ((levelWidth * 0.5f) + padding) / Mathf.Max(cam.aspect, 0.1f);
        cam.orthographicSize = Mathf.Max(halfHeight, halfWidth);
    }

    [ContextMenu("Clear Level")]
    public void ClearLevel()
    {
        if (levelRoot != null)
        {
            for (int i = levelRoot.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(levelRoot.GetChild(i).gameObject);
            }
        }
    }
}
