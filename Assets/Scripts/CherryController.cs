using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private float initialSpawnDelay = 5.0f;
    [SerializeField] private float respawnDelay = 5.0f;
    [SerializeField] private float cherrySpeed = 5.0f; // about 5 tiles per second
    [SerializeField] private float moveDuration = 10.0f;

    [Header("References")]
    [SerializeField] private GameObject cherryPrefab;
    [SerializeField] private Tweener tweener;
    [SerializeField] private Camera targetCamera;

    [Header("Level Bounds")]
    [SerializeField] private Vector2 levelCenter = Vector2.zero;
    [SerializeField] private float levelHalfWidth = 14.0f;
    [SerializeField] private float levelHalfHeight = 14.5f;

    private GameObject activeCherryInstance;
    private float timer = 0f;
    private bool isWaitingForSpawn = true;
    private bool hasPassedCenter = false;
    private Vector3 cherryMoveDir = Vector3.zero;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (tweener == null)
        {
            tweener = GetComponent<Tweener>();
            if (tweener == null) tweener = Object.FindAnyObjectByType<Tweener>();
            if (tweener == null) tweener = gameObject.AddComponent<Tweener>();
        }

        if (cherryPrefab == null)
        {
#if UNITY_EDITOR
            cherryPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cherry.prefab");
#endif
        }
    }

    private void Start()
    {
        // 5 seconds after scene start, spawn first cherry
        timer = initialSpawnDelay;
        isWaitingForSpawn = true;
    }

    private void Update()
    {
        if (isWaitingForSpawn)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnCherry();
            }
        }
        else
        {
            if (activeCherryInstance == null)
            {
                // Cherry was destroyed (e.g. eaten or cleaned up) -> respawn after 5 seconds
                isWaitingForSpawn = true;
                timer = respawnDelay;
            }
            else
            {
                Vector3 currentPos = activeCherryInstance.transform.position;
                Vector3 center3D = new Vector3(levelCenter.x, levelCenter.y, 0f);

                // Track if cherry has passed the center of the level
                if (!hasPassedCenter)
                {
                    if (Vector3.Dot(currentPos - center3D, cherryMoveDir) >= 0f)
                    {
                        hasPassedCenter = true;
                    }
                }
                else
                {
                    // Once past the center, destroy itself as soon as it is completely outside the camera view
                    if (IsOutsideCameraView(currentPos))
                    {
                        DestroyCherry();
                    }
                }
            }
        }
    }

    private void SpawnCherry()
    {
        isWaitingForSpawn = false;
        hasPassedCenter = false;

        Vector3 spawnPos;
        Vector3 targetPos;
        CalculateTrajectoryPoints(out spawnPos, out targetPos);

        cherryMoveDir = (targetPos - spawnPos).normalized;

        if (cherryPrefab != null)
        {
            activeCherryInstance = Instantiate(cherryPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            activeCherryInstance = CreateDefaultCherryObject(spawnPos);
        }

        // Configure SpriteRenderer: visible only inside level via Sprite Mask, top sorting layer
        var sr = activeCherryInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            sr.sortingLayerName = "Top";
            sr.sortingOrder = 50;
        }

        // Play cherry audio
        var audioSource = activeCherryInstance.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.loop = true;
            if (audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        // Calculate duration based on distance and speed (or fallback to moveDuration)
        float totalDist = Vector3.Distance(spawnPos, targetPos);
        float duration = cherrySpeed > 0f ? (totalDist / cherrySpeed) : moveDuration;

        // Tween-style movement through level centre to opposite side (no DoTween)
        tweener.AddTween(activeCherryInstance.transform, spawnPos, targetPos, duration, DestroyCherry);
    }

    private void CalculateTrajectoryPoints(out Vector3 spawn, out Vector3 target)
    {
        // Margin just outside the map bounds
        float margin = 1.0f;
        int side = Random.Range(0, 4);
        float spawnX = 0f;
        float spawnY = 0f;

        switch (side)
        {
            case 0: // Top
                spawnX = Random.Range(-levelHalfWidth, levelHalfWidth);
                spawnY = levelHalfHeight + margin;
                break;
            case 1: // Bottom
                spawnX = Random.Range(-levelHalfWidth, levelHalfWidth);
                spawnY = -(levelHalfHeight + margin);
                break;
            case 2: // Left
                spawnX = -(levelHalfWidth + margin);
                spawnY = Random.Range(-levelHalfHeight, levelHalfHeight);
                break;
            case 3: // Right
            default:
                spawnX = levelHalfWidth + margin;
                spawnY = Random.Range(-levelHalfHeight, levelHalfHeight);
                break;
        }

        spawn = new Vector3(spawnX, spawnY, 0f);

        Vector3 center = new Vector3(levelCenter.x, levelCenter.y, 0f);
        Vector3 dir = (center - spawn).normalized;

        // Calculate distance from center to point completely outside camera view along dir
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        float camH = cam != null ? cam.orthographicSize : 15.5f;
        float camW = cam != null ? (camH * cam.aspect) : 28.0f;

        float extraPadding = 2.0f;
        float dx = Mathf.Abs(dir.x) > 0.001f ? (camW + extraPadding) / Mathf.Abs(dir.x) : float.MaxValue;
        float dy = Mathf.Abs(dir.y) > 0.001f ? (camH + extraPadding) / Mathf.Abs(dir.y) : float.MaxValue;
        float exitDist = Mathf.Min(dx, dy) + 1.0f;

        // Target position is in a straight line through the level center to the opposite side
        target = center + dir * exitDist;
    }

    private bool IsOutsideCameraView(Vector3 pos)
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return false;

        float camH = cam.orthographicSize;
        float camW = cam.orthographicSize * cam.aspect;
        float padding = 1.0f;

        Vector3 camPos = cam.transform.position;
        return Mathf.Abs(pos.x - camPos.x) > (camW + padding) ||
               Mathf.Abs(pos.y - camPos.y) > (camH + padding);
    }

    private void DestroyCherry()
    {
        if (activeCherryInstance != null)
        {
            Destroy(activeCherryInstance);
            activeCherryInstance = null;
        }

        // 5 seconds after each cherry is destroyed, spawn next
        timer = respawnDelay;
        isWaitingForSpawn = true;
    }

    private GameObject CreateDefaultCherryObject(Vector3 pos)
    {
        GameObject go = new GameObject("Cherry");
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("CherrySprite");
        if (sprite == null)
        {
#if UNITY_EDITOR
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/CherrySprite.png");
#endif
        }
        sr.sprite = sprite;
        sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        sr.sortingLayerName = "Top";
        sr.sortingOrder = 50;

        return go;
    }

    private void OnDestroy()
    {
        if (activeCherryInstance != null)
        {
            Destroy(activeCherryInstance);
        }
    }
}
