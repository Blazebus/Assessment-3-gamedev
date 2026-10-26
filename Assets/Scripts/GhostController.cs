using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }
    public GhostState currentState = GhostState.Normal;
    public int ghostType = 1; // 1 to 4

    private Vector3 startWorldPos;
    private Vector2Int startGridPos;
    private Vector2Int currentGridPos;
    private Vector2Int lastGridPos;
    private int currentDirection = -1; // 0: Up, 1: Down, 2: Left, 3: Right

    private Animator animator;
    private Tweener tweener;
    private LevelGenerator levelGenerator;
    private PacStudentController pacStudent;

    private int[,] fullGrid;
    private int totalRows;
    private int totalCols;
    private float tileSize;
    private float startX;
    private float startY;

    private bool isOutsideSpawn = false;

    private bool isLevel2 = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        tweener = GetComponent<Tweener>();
        if (tweener == null) tweener = gameObject.AddComponent<Tweener>();
        
        levelGenerator = Object.FindAnyObjectByType<LevelGenerator>();
        pacStudent = Object.FindAnyObjectByType<PacStudentController>();

        isLevel2 = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "InnovationScene";
    }

    private void Start()
    {
        InitializeLevelGrid();
        startWorldPos = transform.position;
        startGridPos = new Vector2Int(currentGridRow, currentGridCol);
        Reset();
    }

    private int currentGridRow;
    private int currentGridCol;

    private void InitializeLevelGrid()
    {
        if (levelGenerator != null)
        {
            var levelMap = levelGenerator.levelMap;
            tileSize = levelGenerator.tileSize;
            int qRows = levelMap.GetLength(0);
            int qCols = levelMap.GetLength(1);
            totalCols = qCols * 2;
            totalRows = qRows * 2 - 1;
            fullGrid = LevelGenerator.BuildFullMirroredGrid(levelMap, qRows, qCols, totalRows, totalCols);
            startX = -(totalCols - 1) * tileSize * 0.5f;
            startY = (totalRows - 1) * tileSize * 0.5f;
        }

        currentGridCol = Mathf.RoundToInt((transform.position.x - startX) / tileSize);
        currentGridRow = Mathf.RoundToInt((startY - transform.position.y) / tileSize);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (animator != null) animator.speed = 0f;
            return;
        }
        
        if (animator != null) animator.speed = 1f;

        if (currentState == GhostState.Dead)
        {
            MoveTowardsSpawn();
            return;
        }

        if (!tweener.TweenExists(transform))
        {
            DetermineNextMove();
        }
    }

    private void MoveTowardsSpawn()
    {
        float speed = GetCurrentSpeed();
        transform.position = Vector3.MoveTowards(transform.position, startWorldPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, startWorldPos) < 0.01f)
        {
            transform.position = startWorldPos;
            currentGridRow = startGridPos.x;
            currentGridCol = startGridPos.y;
            isOutsideSpawn = false;
            currentDirection = -1;
            
            // Resurrection
            if (GameManager.Instance != null)
            {
                currentState = (GhostState)GameManager.Instance.globalGhostState;
                UpdateState(); 
                GameManager.Instance.GhostResurrected();
            }
        }
    }

    public void UpdateState()
    {
        if (currentState == GhostState.Dead) return;

        if (GameManager.Instance != null)
        {
            currentState = (GhostState)GameManager.Instance.globalGhostState;
        }
        
        if (animator != null)
        {
            animator.SetInteger("State", (int)currentState);
        }
    }

    public void Die()
    {
        currentState = GhostState.Dead;
        if (animator != null) animator.SetInteger("State", (int)GhostState.Dead);
        tweener.CancelTween(transform);
    }

    public void Reset()
    {
        StopAllCoroutines();
        tweener.CancelTween(transform);
        transform.position = startWorldPos;
        currentGridRow = startGridPos.x;
        currentGridCol = startGridPos.y;
        currentState = GhostState.Normal;
        isOutsideSpawn = false;
        currentDirection = -1;
        if (animator != null)
        {
            animator.SetInteger("State", (int)GhostState.Normal);
            animator.speed = 1f;
        }
    }

    private float GetCurrentSpeed()
    {
        float pacSpeed = (pacStudent != null) ? pacStudent.speed : 5.0f;
        float normalSpeed = pacSpeed * 0.9f;
        if (currentState == GhostState.Normal) return normalSpeed;
        return normalSpeed * 0.5f; // Scared, Recovering, Dead use 50% of Normal speed
    }

    private GhostController blinky;

    private void DetermineNextMove()
    {
        // If inside spawn, follow exit logic
        if (!isOutsideSpawn)
        {
            MoveTowardsExit();
            return;
        }

        List<int> validDirections = GetValidDirections();
        if (validDirections.Count == 0) return;

        // Forced turn if only 1 option
        if (validDirections.Count == 1)
        {
            StartMove(validDirections[0]);
            return;
        }

        int selectedDir = -1;
        
        // If Scared or Recovering, all use Ghost 1 behavior (random/flee)
        if (currentState == GhostState.Scared || currentState == GhostState.Recovering)
        {
            selectedDir = BehaviorGhost1(validDirections);
        }
        else if (isLevel2)
        {
            selectedDir = SmartBehavior(validDirections);
        }
        else
        {
            switch (ghostType)
            {
                case 1: selectedDir = BehaviorGhost1(validDirections); break;
                case 2: selectedDir = BehaviorGhost2(validDirections); break;
                case 3: selectedDir = BehaviorGhost3(validDirections); break;
                case 4: selectedDir = BehaviorGhost4(validDirections); break;
            }
        }

        if (selectedDir != -1)
        {
            StartMove(selectedDir);
        }
    }

    private int SmartBehavior(List<int> valid)
    {
        if (pacStudent == null) return valid[Random.Range(0, valid.Count)];

        Vector2Int pacPos = new Vector2Int(pacStudent.CurrentGridRow, pacStudent.CurrentGridCol);
        Vector2Int target = pacPos;
        
        switch (ghostType)
        {
            case 1: // Blinky Style: Aggressive Pursuit
                target = pacPos;
                break;
            case 2: // Pinky Style: Interception (predictive)
                target = GetPredictedPacPos(4);
                break;
            case 3: // Inky Style: Cooperation (flanking)
                target = GetCooperativeTarget(pacPos);
                break;
            case 4: // Clyde Style: Aggressive nearby, else unpredictable
                float dist = Vector2.Distance(new Vector2(currentGridRow, currentGridCol), new Vector2(pacPos.x, pacPos.y));
                if (dist < 8) target = pacPos;
                else target = new Vector2Int(totalRows - 2, 1); // Corner
                break;
        }

        return GetBestDirection(valid, target);
    }

    private Vector2Int GetPredictedPacPos(int offset)
    {
        Vector2Int pacPos = new Vector2Int(pacStudent.CurrentGridRow, pacStudent.CurrentGridCol);
        int pacDir = pacStudent.CurrentDirection;

        Vector2Int offsetVec = Vector2Int.zero;
        switch (pacDir)
        {
            case 0: offsetVec = new Vector2Int(-offset, 0); break; // Up
            case 1: offsetVec = new Vector2Int(offset, 0); break;  // Down
            case 2: offsetVec = new Vector2Int(0, -offset); break; // Left
            case 3: offsetVec = new Vector2Int(0, offset); break;  // Right
        }

        Vector2Int predicted = pacPos + offsetVec;
        predicted.x = Mathf.Clamp(predicted.x, 0, totalRows - 1);
        predicted.y = Mathf.Clamp(predicted.y, 0, totalCols - 1);
        
        return predicted;
    }

    private Vector2Int GetCooperativeTarget(Vector2Int pacPos)
    {
        if (blinky == null)
        {
            foreach (var g in Object.FindObjectsByType<GhostController>(FindObjectsSortMode.None))
            {
                if (g.ghostType == 1) { blinky = g; break; }
            }
        }

        if (blinky == null) return pacPos;

        Vector2Int pivot = GetPredictedPacPos(2);
        Vector2Int blinkyPos = new Vector2Int(blinky.currentGridRow, blinky.currentGridCol);
        
        Vector2Int target = pivot + (pivot - blinkyPos);
        target.x = Mathf.Clamp(target.x, 0, totalRows - 1);
        target.y = Mathf.Clamp(target.y, 0, totalCols - 1);
        
        return target;
    }

    private int GetBestDirection(List<int> valid, Vector2Int targetGrid)
    {
        int bestDir = valid[0];
        float minDist = float.MaxValue;

        foreach (int d in valid)
        {
            Vector2Int next = GetAdjacentGridPos(currentGridRow, currentGridCol, d);
            float dist = Vector2.Distance(new Vector2(next.x, next.y), new Vector2(targetGrid.x, targetGrid.y));
            if (dist < minDist)
            {
                minDist = dist;
                bestDir = d;
            }
        }
        return bestDir;
    }

    private void MoveTowardsExit()
    {
        // Ghost 1 & 3 use upper exit (row 12, col 13/14)
        // Ghost 2 & 4 use lower exit (row 16, col 13/14)
        int targetRow = (ghostType == 1 || ghostType == 3) ? 12 : 16;

        if (currentGridRow == targetRow)
        {
            isOutsideSpawn = true;
            DetermineNextMove();
            return;
        }

        int dir = (currentGridRow > targetRow) ? 0 : 1; // 0: Up, 1: Down
        StartMove(dir);
    }

    private int BehaviorGhost1(List<int> valid)
    {
        // Level 1 behavior: Random direction increasing or maintaining distance to PacStudent
        float currentDist = GetDistanceToPac(currentGridRow, currentGridCol);
        List<int> options = new List<int>();
        foreach (int d in valid)
        {
            Vector2Int next = GetAdjacentGridPos(currentGridRow, currentGridCol, d);
            if (GetDistanceToPac(next.x, next.y) >= currentDist) options.Add(d);
        }
        if (options.Count > 0) return options[Random.Range(0, options.Count)];
        return valid[Random.Range(0, valid.Count)];
    }

    private int BehaviorGhost2(List<int> valid)
    {
        // Level 1 behavior: Random direction decreasing or maintaining distance to PacStudent
        float currentDist = GetDistanceToPac(currentGridRow, currentGridCol);
        List<int> options = new List<int>();
        foreach (int d in valid)
        {
            Vector2Int next = GetAdjacentGridPos(currentGridRow, currentGridCol, d);
            if (GetDistanceToPac(next.x, next.y) <= currentDist) options.Add(d);
        }
        if (options.Count > 0) return options[Random.Range(0, options.Count)];
        return valid[Random.Range(0, valid.Count)];
    }

    private int BehaviorGhost3(List<int> valid)
    {
        // Level 1 behavior: Random direction (already non-reversing by GetValidDirections)
        return valid[Random.Range(0, valid.Count)];
    }

    private int BehaviorGhost4(List<int> valid)
    {
        // Level 1 behavior: Clockwise path around the outer wall
        int[] clockwiseOrder = { 3, 1, 2, 0 }; // Right, Down, Left, Up
        
        int currentIndex = -1;
        for(int i=0; i<4; i++) if(clockwiseOrder[i] == currentDirection) currentIndex = i;

        for (int i = 0; i < 4; i++)
        {
            int nextDir = clockwiseOrder[(currentIndex + i) % 4];
            if (valid.Contains(nextDir)) return nextDir;
        }

        return valid[Random.Range(0, valid.Count)];
    }

    private List<int> GetValidDirections()
    {
        List<int> valid = new List<int>();
        for (int d = 0; d < 4; d++)
        {
            // Never reverse direction unless it is the only available move
            if (currentDirection != -1 && IsOpposite(d, currentDirection)) continue;

            Vector2Int next = GetAdjacentGridPos(currentGridRow, currentGridCol, d);
            if (IsWalkable(next.x, next.y)) valid.Add(d);
        }

        // If no non-reversing options, allow reversing
        if (valid.Count == 0 && currentDirection != -1)
        {
            int opp = GetOpposite(currentDirection);
            Vector2Int next = GetAdjacentGridPos(currentGridRow, currentGridCol, opp);
            if (IsWalkable(next.x, next.y)) valid.Add(opp);
        }

        return valid;
    }

    private void StartMove(int dir)
    {
        currentDirection = dir;
        Vector2Int target = GetAdjacentGridPos(currentGridRow, currentGridCol, dir);
        Vector3 targetPos = GetWorldPosition(target.x, target.y);
        float duration = tileSize / GetCurrentSpeed();

        tweener.AddTween(transform, transform.position, targetPos, duration, () => {
            currentGridRow = target.x;
            currentGridCol = target.y;
        });
    }

    private bool IsWalkable(int row, int col)
    {
        if (row < 0 || row >= totalRows || col < 0 || col >= totalCols) return false;
        int type = fullGrid[row, col];
        
        // Must never enter walls
        if (LevelGenerator.IsWall(type)) return false;

        // Must treat teleporters as walls
        if (row == 14 && (col == 0 || col == totalCols - 1)) return false;

        // May not re-enter spawn area unless Dead
        if (currentState != GhostState.Dead && isOutsideSpawn)
        {
            if (IsInsideSpawn(row, col)) return false;
        }

        return true;
    }

    private bool IsInsideSpawn(int row, int col)
    {
        return row >= 13 && row <= 15 && col >= 11 && col <= 16;
    }

    private float GetDistanceToPac(int row, int col)
    {
        if (pacStudent == null) return 0f;
        return Vector2.Distance(new Vector2(row, col), new Vector2(pacStudent.CurrentGridRow, pacStudent.CurrentGridCol));
    }

    private Vector2Int GetAdjacentGridPos(int row, int col, int dir)
    {
        if (dir == 0) return new Vector2Int(row - 1, col); // Up
        if (dir == 1) return new Vector2Int(row + 1, col); // Down
        if (dir == 2) return new Vector2Int(row, col - 1); // Left
        if (dir == 3) return new Vector2Int(row, col + 1); // Right
        return new Vector2Int(row, col);
    }

    private Vector3 GetWorldPosition(int row, int col)
    {
        return new Vector3(startX + col * tileSize, startY - row * tileSize, 0f);
    }

    private bool IsOpposite(int d1, int d2)
    {
        if (d1 == 0 && d2 == 1) return true;
        if (d1 == 1 && d2 == 0) return true;
        if (d1 == 2 && d2 == 3) return true;
        if (d1 == 3 && d2 == 2) return true;
        return false;
    }

    private int GetOpposite(int d)
    {
        if (d == 0) return 1;
        if (d == 1) return 0;
        if (d == 2) return 3;
        if (d == 3) return 2;
        return -1;
    }
}

