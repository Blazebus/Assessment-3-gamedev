using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public HUDController hud;
    public GameObject blockingOverlay;
    public TextMeshProUGUI countdownText;
    public GameObject gameOverOverlay;
    public TextMeshProUGUI gameOverText;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip introMusic;
    public AudioClip normalMusic;
    public AudioClip scaredMusic;
    public AudioClip ghostDeadMusic;
    public AudioClip ghostEatSound;
    public AudioClip deathSound;
    public AudioClip wallBumpSound;
    public AudioClip pelletSound;
    public AudioClip cherrySound;
    public AudioClip powerPelletSound;

    [Header("Effects")]
    public ParticleSystem deathEffect;
    public ParticleSystem wallBumpEffect;

    [Header("Game State")]
    public bool isGameActive = false;
    public int score = 0;
    public int lives = 3;
    public float gameTimer = 0f;
    public int totalPellets = 0;
    public int pelletsCollected = 0;

    public enum GhostState { Normal, Scared, Recovering, Dead }
    public GhostState globalGhostState = GhostState.Normal;
    private float scaredTimer = 0f;

    private List<GhostController> ghosts = new List<GhostController>();
    private Vector3 pacStartPos;
    private PacStudentController pacStudent;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        pacStudent = FindAnyObjectByType<PacStudentController>();
        if (pacStudent != null) pacStartPos = pacStudent.transform.position;
        
        ghosts.AddRange(FindObjectsByType<GhostController>(FindObjectsSortMode.None));
        
        // Find HUD if not assigned
        if (hud == null) hud = FindAnyObjectByType<HUDController>();

        // Wait a frame for LevelGenerator to finish instantiating pellets
        yield return null;

        // Count initial pellets in scene
        totalPellets = GameObject.FindGameObjectsWithTag("Pellet").Length + GameObject.FindGameObjectsWithTag("PowerPellet").Length;

        StartCoroutine(RoundStartSequence());
    }

    private IEnumerator RoundStartSequence()
    {
        isGameActive = false;
        if (blockingOverlay != null) blockingOverlay.SetActive(true);
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        
        if (musicSource != null && introMusic != null)
        {
            musicSource.clip = introMusic;
            musicSource.loop = false;
            musicSource.Play();
        }

        string[] sequence = { "3", "2", "1", "GO!" };
        foreach (var s in sequence)
        {
            if (countdownText != null) countdownText.text = s;
            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (blockingOverlay != null) blockingOverlay.SetActive(false);
        
        isGameActive = true;
        
        if (musicSource != null && normalMusic != null)
        {
            musicSource.clip = normalMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void Update()
    {
        if (!isGameActive) return;

        gameTimer += Time.deltaTime;
        UpdateHUD();
        UpdateMusic();

        if (scaredTimer > 0)
        {
            scaredTimer -= Time.deltaTime;
            if (scaredTimer <= 3f && globalGhostState == GhostState.Scared)
            {
                SetGlobalGhostState(GhostState.Recovering);
            }
            if (scaredTimer <= 0)
            {
                SetGlobalGhostState(GhostState.Normal);
            }
        }
    }

    public void GhostResurrected()
    {
        UpdateMusic();
    }

    public void UpdateMusic()
    {
        if (musicSource == null) return;

        AudioClip targetClip = normalMusic;

        bool anyGhostDead = false;
        foreach (var ghost in ghosts)
        {
            if (ghost.currentState == GhostController.GhostState.Dead)
            {
                anyGhostDead = true;
                break;
            }
        }

        if (anyGhostDead)
        {
            targetClip = ghostDeadMusic;
        }
        else if (globalGhostState == GhostState.Scared || globalGhostState == GhostState.Recovering)
        {
            targetClip = scaredMusic;
        }

        if (musicSource.clip != targetClip && targetClip != null)
        {
            musicSource.clip = targetClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void UpdateHUD()
    {
        if (hud == null) return;

        if (hud.ScoreText != null) hud.ScoreText.text = score.ToString("D6");
        
        int mins = (int)(gameTimer / 60);
        int secs = (int)(gameTimer % 60);
        int ms = (int)((gameTimer * 100) % 100); // centiseconds
        if (hud.TimerText != null) hud.TimerText.text = string.Format("{0:00}:{1:00}:{2:00}", mins, secs, ms);

        if (hud.LevelNameText != null)
        {
            hud.LevelNameText.text = SceneManager.GetActiveScene().name == "InnovationScene" ? "Level 2" : "Level 1";
        }

        if (scaredTimer > 0)
        {
            if (hud.GhostTimerText != null)
            {
                hud.GhostTimerText.text = Mathf.Ceil(scaredTimer).ToString();
                hud.GhostTimerText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (hud.GhostTimerText != null) hud.GhostTimerText.gameObject.SetActive(false);
        }

        if (hud.Life1 != null) hud.Life1.enabled = lives >= 1;
        if (hud.Life2 != null) hud.Life2.enabled = lives >= 2;
        if (hud.Life3 != null) hud.Life3.enabled = lives >= 3;
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void PelletCollected()
    {
        AddScore(10);
        pelletsCollected++;
        if (pelletsCollected >= totalPellets)
        {
            if (SceneManager.GetActiveScene().name == "RecreatedLevel")
            {
                PlayerPrefs.SetInt("Level1Completed", 1);
            }
            GameOver();
        }
    }

    public void PowerPelletCollected()
    {
        AddScore(50);
        scaredTimer = 10f;
        SetGlobalGhostState(GhostState.Scared);
        if (musicSource != null && scaredMusic != null)
        {
            musicSource.clip = scaredMusic;
            musicSource.Play();
        }
    }

    public void CherryCollected()
    {
        AddScore(100);
        if (musicSource != null && cherrySound != null)
        {
            musicSource.PlayOneShot(cherrySound);
        }
    }

    public void GhostEaten(GhostController ghost)
    {
        AddScore(300);
        if (musicSource != null && ghostEatSound != null)
        {
            musicSource.PlayOneShot(ghostEatSound);
        }
        ghost.Die();
    }

    private void SetGlobalGhostState(GhostState state)
    {
        globalGhostState = state;
        foreach (var ghost in ghosts)
        {
            ghost.UpdateState();
        }
    }

    public void PacDeath()
    {
        lives--;
        UpdateHUD();
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isGameActive = false;
        if (deathEffect != null) deathEffect.Play();
        if (musicSource != null && deathSound != null) musicSource.PlayOneShot(deathSound);
        
        pacStudent.SetDead(true);
        
        yield return new WaitForSeconds(3f);
        
        if (lives > 0)
        {
            ResetLevel();
            pacStudent.SetDead(false);
            StartCoroutine(RoundStartSequence());
        }
        else
        {
            GameOver();
        }
    }

    private void ResetLevel()
    {
        pacStudent.transform.position = pacStartPos;
        foreach (var ghost in ghosts)
        {
            ghost.Reset();
        }
    }

    private void GameOver()
    {
        isGameActive = false;
        if (gameOverOverlay != null) gameOverOverlay.SetActive(true);
        if (gameOverText != null) gameOverText.text = "Game Over";
        
        SaveHighScore();
        Invoke("LoadStartScene", 3f);
    }

    private void SaveHighScore()
    {
        string levelPrefix = SceneManager.GetActiveScene().name == "InnovationScene" ? "Level2" : "Level1";
        string scoreKey = levelPrefix + "_HighScore";
        string timeKey = levelPrefix + "_BestTime";

        int bestScore = PlayerPrefs.GetInt(scoreKey, 0);
        float bestTime = PlayerPrefs.GetFloat(timeKey, float.MaxValue);

        if (score > bestScore || (score == bestScore && gameTimer < bestTime))
        {
            PlayerPrefs.SetInt(scoreKey, score);
            PlayerPrefs.SetFloat(timeKey, gameTimer);
            PlayerPrefs.Save();
        }
    }

    private void LoadStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
}
