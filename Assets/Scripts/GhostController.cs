using UnityEngine;
using System.Collections;

public class GhostController : MonoBehaviour
{
    public enum GhostState { Normal, Scared, Recovering, Dead }
    public GhostState currentState = GhostState.Normal;

    private Vector3 startPos;
    private Animator animator;
    private Vector3 lastPos;

    private void Awake()
    {
        startPos = transform.position;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
        {
            if (animator != null) animator.speed = 0f;
            return;
        }
        
        if (animator != null) animator.speed = 1f;
    }

    public void UpdateState()
    {
        // Don't change state if dead, unless reset
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
        StartCoroutine(RespawnTimer());
    }

    private IEnumerator RespawnTimer()
    {
        yield return new WaitForSeconds(3f);
        // Return to current global state
        if (GameManager.Instance != null)
        {
            currentState = (GhostState)GameManager.Instance.globalGhostState;
        }
        if (animator != null) animator.SetInteger("State", (int)currentState);
    }

    public void Reset()
    {
        StopAllCoroutines();
        transform.position = startPos;
        currentState = GhostState.Normal;
        if (animator != null) animator.SetInteger("State", (int)GhostState.Normal);
    }
}
