using UnityEngine;

public class PowerPelletFlashing : MonoBehaviour
{
    [Tooltip("Flash interval in seconds (time visible / time hidden)")]
    public float flashInterval = 0.2f;

    private SpriteRenderer spriteRenderer;
    private float timer = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        timer += Time.deltaTime;
        if (timer >= flashInterval)
        {
            timer -= flashInterval;
            spriteRenderer.enabled = !spriteRenderer.enabled;
        }
    }
}
