using UnityEngine;
//movement changed
public class PacStudentController : MonoBehaviour
{
    public float speed = 3.0f;
    public Transform[] waypoints;

    public Vector3[] corners = new Vector3[]
    {
        new Vector3(-12.5f, 13.0f, 0f),
        new Vector3(-7.5f, 13.0f, 0f),
        new Vector3(-7.5f, 9.0f, 0f),
        new Vector3(-12.5f, 9.0f, 0f)
    };

    public Sprite spriteUp;
    public Sprite spriteDown;
    public Sprite spriteLeft;
    public Sprite spriteRight;
    public Sprite spriteDead;

    public AudioSource moveAudioSource;

    public const int DIR_UP = 0;
    public const int DIR_DOWN = 1;
    public const int DIR_LEFT = 2;
    public const int DIR_RIGHT = 3;

    private int currentDirection = DIR_RIGHT;
    private int currentCornerIndex = 0;
    private float timer = 0f;
    private bool isDead = false;

    public bool IsDead => isDead;
    public int CurrentDirection => currentDirection;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (waypoints != null && waypoints.Length >= 2)
        {
            corners = new Vector3[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    corners[i] = waypoints[i].position;
                }
            }
        }
        else if (corners == null || corners.Length < 2)
        {
            corners = new Vector3[]
            {
                new Vector3(-12.5f, 13.0f, 0f),
                new Vector3(-7.5f, 13.0f, 0f),
                new Vector3(-7.5f, 9.0f, 0f),
                new Vector3(-12.5f, 9.0f, 0f)
            };
        }

        currentCornerIndex = 0;
        timer = 0f;
        transform.position = corners[0];

        UpdateDirectionForSegment(corners[0], corners[1]);

        if (moveAudioSource != null)
        {
            moveAudioSource.loop = true;
            if (moveAudioSource.clip != null)
            {
                moveAudioSource.Play();
            }
        }
    }

    private void Update()
    {
        if (isDead)
            return;

        if (corners == null || corners.Length < 2)
            return;

        Vector3 startPos = corners[currentCornerIndex];
        int nextCornerIndex = (currentCornerIndex + 1) % corners.Length;
        Vector3 targetPos = corners[nextCornerIndex];

        float distance = Vector3.Distance(startPos, targetPos);
        if (distance <= Mathf.Epsilon)
        {
            AdvanceToNextCorner(nextCornerIndex);
            return;
        }

        float duration = distance / speed;
        timer += Time.deltaTime;

        while (timer >= duration && duration > Mathf.Epsilon)
        {
            transform.position = targetPos;
            timer -= duration;

            currentCornerIndex = nextCornerIndex;
            nextCornerIndex = (currentCornerIndex + 1) % corners.Length;

            startPos = corners[currentCornerIndex];
            targetPos = corners[nextCornerIndex];
            distance = Vector3.Distance(startPos, targetPos);
            duration = distance / speed;

            UpdateDirectionForSegment(startPos, targetPos);
        }

        if (duration > Mathf.Epsilon)
        {
            float t = timer / duration;
            transform.position = Vector3.Lerp(startPos, targetPos, t);
        }
    }

    private void AdvanceToNextCorner(int nextIndex)
    {
        currentCornerIndex = nextIndex;
        timer = 0f;

        Vector3 startPos = corners[currentCornerIndex];
        Vector3 targetPos = corners[(currentCornerIndex + 1) % corners.Length];
        transform.position = startPos;

        UpdateDirectionForSegment(startPos, targetPos);
    }

    private void UpdateDirectionForSegment(Vector3 from, Vector3 to)
    {
        Vector3 displacement = to - from;
        int dir;

        if (Mathf.Abs(displacement.x) > Mathf.Abs(displacement.y))
        {
            dir = displacement.x > 0 ? DIR_RIGHT : DIR_LEFT;
        }
        else
        {
            dir = displacement.y > 0 ? DIR_UP : DIR_DOWN;
        }

        SetDirection(dir);
    }

    public void SetDirection(int direction)
    {
        currentDirection = direction;

        string stateName;
        switch (direction)
        {
            case DIR_UP:
                stateName = "WalkingUp";
                break;
            case DIR_DOWN:
                stateName = "WalkingDown";
                break;
            case DIR_LEFT:
                stateName = "WalkingLeft";
                break;
            case DIR_RIGHT:
            default:
                stateName = "WalkingRight";
                break;
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
                if (spriteUp != null)
                    spriteRenderer.sprite = spriteUp;
                break;

            case DIR_DOWN:
                if (spriteDown != null)
                    spriteRenderer.sprite = spriteDown;
                break;

            case DIR_LEFT:
                if (spriteLeft != null)
                    spriteRenderer.sprite = spriteLeft;
                break;

            case DIR_RIGHT:
                if (spriteRight != null)
                    spriteRenderer.sprite = spriteRight;
                break;
        }
    }

    public void SetDead(bool dead)
    {
        isDead = dead;

        if (animator != null)
        {
            animator.SetBool("IsDead", isDead);
            if (isDead)
            {
                animator.Play("Dead");
            }
        }

        if (isDead)
        {
            if (spriteDead != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = spriteDead;
            }
            if (moveAudioSource != null && moveAudioSource.isPlaying)
            {
                moveAudioSource.Stop();
            }
        }
        else
        {
            UpdateVisualsForDirection(currentDirection);
        }
    }
}
