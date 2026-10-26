using UnityEngine;

public class PacStudentController : MonoBehaviour
{
    [Header("Input")]
    public KeyCode lastInput = KeyCode.None;
    public KeyCode currentInput = KeyCode.None;

    [Header("Dependencies")]
    public Tweener tweener;
    public LevelGenerator levelGenerator;

    [Header("Level Map")]
    public int[,] levelMap;

    [Header("Movement")]
    public float speed = 5.0f; // about 5 tiles per second

    [Header("Audio")]
    public AudioSource moveAudioSource;
    public AudioClip pelletAudioClip;
    public AudioClip noPelletAudioClip;

    [Header("Effects")]
    public ParticleSystem dustEffect;

    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;
    public Sprite spriteDead;

    public const int DIR_UP = 0;
    public const int DIR_DOWN = 1;
    public const int DIR_LEFT = 2;
    public const int DIR_RIGHT = 3;

    private int currentDirection = DIR_RIGHT;
    private bool isDead = false;

    // Grid coordinates
    private int currentGridRow = 1;
    private int currentGridCol = 1;
    private int totalRows = 29;
    private int totalCols = 28;
    private float tileSize = 1.0f;
    private float startX = -13.5f;
    private float startY = 14.0f;
    private int[,] fullGrid;

    public bool IsDead => isDead;
    public int CurrentDirection => currentDirection;
    public int CurrentGridRow => currentGridRow;
    public int CurrentGridCol => currentGridCol;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (moveAudioSource == null) moveAudioSource = GetComponent<AudioSource>();

        if (tweener == null)
        {
            tweener = GetComponent<Tweener>();
            if (tweener == null) tweener = Object.FindAnyObjectByType<Tweener>();
            if (tweener == null) tweener = gameObject.AddComponent<Tweener>();
        }

        if (levelGenerator == null)
        {
            levelGenerator = Object.FindAnyObjectByType<LevelGenerator>();
        }

        if (dustEffect == null)
        {
            var dustChild = transform.Find("DustEffect");
            if (dustChild != null)
            {
                dustEffect = dustChild.GetComponent<ParticleSystem>();
            }
        }
    }

    private void Start()
    {
        InitializeLevelGrid();

        // Start stationary facing right
        lastInput = KeyCode.None;
        currentInput = KeyCode.None;
        currentDirection = DIR_RIGHT;

        SetDirection(DIR_RIGHT);
        if (animator != null)
        {
            animator.Play("WalkingRight", 0, 0f);
            animator.speed = 0f; // Walk animation plays only while lerping
        }

        if (moveAudioSource != null && moveAudioSource.isPlaying)
        {
            moveAudioSource.Stop();
        }

        if (dustEffect != null && dustEffect.isPlaying)
        {
            dustEffect.Stop();
        }
    }

    private void InitializeLevelGrid()
    {
        if (levelGenerator != null)
        {
            levelMap = levelGenerator.levelMap;
            tileSize = levelGenerator.tileSize;

            int qRows = levelMap.GetLength(0);
            int qCols = levelMap.GetLength(1);
            totalCols = qCols * 2;
            totalRows = qRows * 2 - 1;

            fullGrid = LevelGenerator.BuildFullMirroredGrid(levelMap, qRows, qCols, totalRows, totalCols);

            startX = -(totalCols - 1) * tileSize * 0.5f;
            startY = (totalRows - 1) * tileSize * 0.5f;
        }
        else if (levelMap != null)
        {
            int qRows = levelMap.GetLength(0);
            int qCols = levelMap.GetLength(1);
            totalCols = qCols * 2;
            totalRows = qRows * 2 - 1;
            fullGrid = LevelGenerator.BuildFullMirroredGrid(levelMap, qRows, qCols, totalRows, totalCols);
            startX = -(totalCols - 1) * tileSize * 0.5f;
            startY = (totalRows - 1) * tileSize * 0.5f;
        }

        // Calculate initial grid position from world position
        currentGridCol = Mathf.RoundToInt((transform.position.x - startX) / tileSize);
        currentGridRow = Mathf.RoundToInt((startY - transform.position.y) / tileSize);

        // Snap precisely to grid cell center
        transform.position = GetWorldPosition(currentGridRow, currentGridCol);
    }

    private Vector2Int previousGridPos;
    private bool hasBumped = false;

    private void Update()
    {
        if (isDead || (GameManager.Instance != null && !GameManager.Instance.isGameActive))
        {
            StopMovement();
            return;
        }

        ReadPlayerInput();

        bool isLerping = tweener != null && tweener.TweenExists(transform);

        if (!isLerping)
        {
            previousGridPos = new Vector2Int(currentGridRow, currentGridCol);
            hasBumped = false;

            // If not lerping: try lastInput's adjacent grid cell
            Vector2Int lastTarget = GetAdjacentCell(currentGridRow, currentGridCol, lastInput);
            if (lastInput != KeyCode.None && lastTarget != new Vector2Int(currentGridRow, currentGridCol) && IsWalkable(lastTarget.x, lastTarget.y))
            {
                currentInput = lastInput;
                StartMoveToCell(lastTarget.x, lastTarget.y, currentInput);
            }
            else
            {
                // Else try currentInput
                Vector2Int currentTarget = GetAdjacentCell(currentGridRow, currentGridCol, currentInput);
                if (currentInput != KeyCode.None && currentTarget != new Vector2Int(currentGridRow, currentGridCol) && IsWalkable(currentTarget.x, currentTarget.y))
                {
                    StartMoveToCell(currentTarget.x, currentTarget.y, currentInput);
                }
                else
                {
                    // Else stop
                    StopMovement();
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead || (GameManager.Instance != null && !GameManager.Instance.isGameActive)) return;

        if (other.name.Contains("Tile") && LevelGenerator.IsWall(ExtractTileType(other.name)))
        {
            HandleWallCollision();
        }
        else if (other.CompareTag("Pellet"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.PelletCollected();
        }
        else if (other.CompareTag("PowerPellet"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.PowerPelletCollected();
        }
        else if (other.name.Contains("Cherry"))
        {
            Destroy(other.gameObject);
            GameManager.Instance.CherryCollected();
        }
        else if (other.CompareTag("Ghost"))
        {
            GhostController ghost = other.GetComponent<GhostController>();
            if (ghost != null)
            {
                if (ghost.currentState == GhostController.GhostState.Normal)
                {
                    GameManager.Instance.PacDeath();
                }
                else if (ghost.currentState == GhostController.GhostState.Scared || ghost.currentState == GhostController.GhostState.Recovering)
                {
                    GameManager.Instance.GhostEaten(ghost);
                }
            }
        }
        else if (other.CompareTag("Teleporter"))
        {
            HandleTeleportation(other.gameObject);
        }
    }

    private int ExtractTileType(string name)
    {
        // Name format Tile_RR_CC_TypeX
        string[] parts = name.Split('_');
        if (parts.Length >= 4)
        {
            string typeStr = parts[3].Replace("Type", "");
            int type;
            if (int.TryParse(typeStr, out type)) return type;
        }
        return 0;
    }

    private void HandleWallCollision()
    {
        if (!hasBumped)
        {
            hasBumped = true;
            if (tweener != null) tweener.CancelTween(transform);
            
            // Snap back to previous valid grid position
            currentGridRow = previousGridPos.x;
            currentGridCol = previousGridPos.y;
            transform.position = GetWorldPosition(currentGridRow, currentGridCol);
            
            StopMovement();

            // Play bump sound and effect
            if (GameManager.Instance != null)
            {
                if (moveAudioSource != null && GameManager.Instance.wallBumpSound != null)
                    moveAudioSource.PlayOneShot(GameManager.Instance.wallBumpSound);
                
                if (GameManager.Instance.wallBumpEffect != null)
                {
                    GameManager.Instance.wallBumpEffect.transform.position = transform.position;
                    GameManager.Instance.wallBumpEffect.Play();
                }
            }
        }
    }

    private void HandleTeleportation(GameObject teleporter)
    {
        // Simple logic: if at col 0, move to totalCols - 1. If at totalCols - 1, move to 0.
        if (currentGridCol <= 0)
        {
            currentGridCol = totalCols - 1;
        }
        else if (currentGridCol >= totalCols - 1)
        {
            currentGridCol = 0;
        }
        
        transform.position = GetWorldPosition(currentGridRow, currentGridCol);
        
        // Restart tween if moving
        if (currentInput != KeyCode.None)
        {
            Vector2Int next = GetAdjacentCell(currentGridRow, currentGridCol, currentInput);
            if (IsWalkable(next.x, next.y))
            {
                StartMoveToCell(next.x, next.y, currentInput);
            }
        }
    }

    private void ReadPlayerInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) lastInput = KeyCode.W;
            else if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) lastInput = KeyCode.A;
            else if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) lastInput = KeyCode.S;
            else if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) lastInput = KeyCode.D;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) lastInput = KeyCode.W;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) lastInput = KeyCode.A;
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) lastInput = KeyCode.S;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) lastInput = KeyCode.D;
        }
        catch { }
#endif
    }

    private void StartMoveToCell(int targetRow, int targetCol, KeyCode inputKey)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = GetWorldPosition(targetRow, targetCol);

        float duration = tileSize / Mathf.Max(speed, 0.1f);

        // Update direction and visuals
        UpdateDirectionFromKey(inputKey);

        // Animator: walk animation plays only while lerping
        if (animator != null)
        {
            animator.speed = 1f;
        }

        // Particle effect: play while moving
        if (dustEffect != null && !dustEffect.isPlaying)
        {
            dustEffect.Play();
        }

        // Audio: two clips, pellet vs no-pellet, play only while lerping.
        // Check next cell's contents to choose clip
        bool nextHasPellet = CellHasPellet(targetRow, targetCol);
        AudioClip clipToPlay = nextHasPellet ? pelletAudioClip : noPelletAudioClip;

        if (moveAudioSource != null && clipToPlay != null)
        {
            if (moveAudioSource.clip != clipToPlay || !moveAudioSource.isPlaying)
            {
                moveAudioSource.clip = clipToPlay;
                moveAudioSource.loop = true;
                moveAudioSource.Play();
            }
        }

        // Tween to adjacent cell using Tweener
        int destRow = targetRow;
        int destCol = targetCol;
        tweener.AddTween(transform, startPos, targetPos, duration, () =>
        {
            OnTileReached(destRow, destCol);
        });
    }

    private void OnTileReached(int row, int col)
    {
        // Wrap for side tunnel (row 14)
        if (row == 14)
        {
            if (col < 0) col = totalCols - 1;
            else if (col >= totalCols) col = 0;
        }

        currentGridRow = row;
        currentGridCol = col;
        transform.position = GetWorldPosition(currentGridRow, currentGridCol);

        // Collect pellet if present
        CollectPelletAt(currentGridRow, currentGridCol);
    }

    private void StopMovement()
    {
        // Animator: walk animation plays only while lerping
        if (animator != null)
        {
            animator.speed = 0f;
        }

        // Audio: play only while lerping
        if (moveAudioSource != null && moveAudioSource.isPlaying)
        {
            moveAudioSource.Stop();
        }

        // Dust particle effect: stop when not moving
        if (dustEffect != null && dustEffect.isPlaying)
        {
            dustEffect.Stop();
        }
    }

    public bool IsWalkable(int row, int col)
    {
        // Check tunnel wrap at row 14
        if (row == 14 && (col < 0 || col >= totalCols))
            return true;

        if (fullGrid == null || row < 0 || row >= totalRows || col < 0 || col >= totalCols)
            return false;

        int tileType = fullGrid[row, col];

        // Ghost wall (type 8) must be treated as a normal wall
        // Types 1, 2, 3, 4, 7, 8 are walls
        if (LevelGenerator.IsWall(tileType) || tileType == 8)
            return false;

        return true;
    }

    public bool CellHasPellet(int row, int col)
    {
        if (row == 14 && (col < 0 || col >= totalCols))
            return false;

        if (fullGrid == null || row < 0 || row >= totalRows || col < 0 || col >= totalCols)
            return false;

        int tileType = fullGrid[row, col];
        return tileType == 5 || tileType == 6;
    }

    private void CollectPelletAt(int row, int col)
    {
        if (fullGrid != null && (fullGrid[row, col] == 5 || fullGrid[row, col] == 6))
        {
            // Clear pellet from grid so future visits play no-pellet clip
            fullGrid[row, col] = 0;

            // Destroy pellet GameObject in scene if present
            string pelletName = string.Format("Pellet_{0}_{1}", row, col);
            var pGo = GameObject.Find(pelletName);
            if (pGo == null)
            {
                string powerPelletName = string.Format("PowerPellet_{0}_{1}", row, col);
                pGo = GameObject.Find(powerPelletName);
            }
            if (pGo != null)
            {
                Destroy(pGo);
            }
        }
    }

    private Vector2Int GetAdjacentCell(int row, int col, KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return new Vector2Int(row - 1, col);
            case KeyCode.S: return new Vector2Int(row + 1, col);
            case KeyCode.A: return new Vector2Int(row, col - 1);
            case KeyCode.D: return new Vector2Int(row, col + 1);
            default: return new Vector2Int(row, col);
        }
    }

    private void UpdateDirectionFromKey(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: SetDirection(DIR_UP); break;
            case KeyCode.S: SetDirection(DIR_DOWN); break;
            case KeyCode.A: SetDirection(DIR_LEFT); break;
            case KeyCode.D: SetDirection(DIR_RIGHT); break;
        }
    }

    public void SetDirection(int direction)
    {
        currentDirection = direction;

        string stateName;
        switch (direction)
        {
            case DIR_UP: stateName = "WalkingUp"; break;
            case DIR_DOWN: stateName = "WalkingDown"; break;
            case DIR_LEFT: stateName = "WalkingLeft"; break;
            case DIR_RIGHT:
            default: stateName = "WalkingRight"; break;
        }

        if (animator != null)
        {
            animator.SetInteger("Direction", currentDirection);
            animator.Play(stateName);
        }

        UpdateVisualsForDirection(currentDirection);
    }

    private void UpdateVisualsForDirection(int direction)
    {
        if (spriteRenderer == null || isDead)
            return;

        switch (direction)
        {
            case DIR_UP:
                if (spriteUp != null) spriteRenderer.sprite = spriteUp;
                break;
            case DIR_DOWN:
                if (spriteDown != null) spriteRenderer.sprite = spriteDown;
                break;
            case DIR_LEFT:
                if (spriteLeft != null) spriteRenderer.sprite = spriteLeft;
                break;
            case DIR_RIGHT:
                if (spriteRight != null) spriteRenderer.sprite = spriteRight;
                break;
        }
    }

    public Vector3 GetWorldPosition(int row, int col)
    {
        return new Vector3(startX + col * tileSize, startY - row * tileSize, 0f);
    }

    public void SetDead(bool dead)
    {
        isDead = dead;

        if (animator != null)
        {
            animator.SetBool("IsDead", isDead);
            if (isDead)
            {
                animator.speed = 1f;
                animator.Play("Dead");
            }
        }

        if (isDead)
        {
            if (spriteDead != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = spriteDead;
            }
            StopMovement();
        }
        else
        {
            UpdateVisualsForDirection(currentDirection);
        }
    }
}

