using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartSceneManager : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string level1SceneName = "RecreatedLevel";
    [SerializeField] private string level2SceneName = "InnovationScene";

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickSound;

    [Header("Buttons")]
    [SerializeField] private Button level1Button;
    [SerializeField] private Button level2Button;

    [Header("Record Displays")]
    [SerializeField] private TextMeshProUGUI level1ScoreText;
    [SerializeField] private TextMeshProUGUI level1TimeText;
    [SerializeField] private TextMeshProUGUI level2ScoreText;
    [SerializeField] private TextMeshProUGUI level2TimeText;

    private void Awake()
    {
        if (level1Button != null)
        {
            level1Button.onClick.RemoveListener(LoadLevel1);
            level1Button.onClick.AddListener(LoadLevel1);
        }

        if (level2Button != null)
        {
            level2Button.onClick.RemoveListener(LoadLevel2);
            level2Button.onClick.AddListener(LoadLevel2);
            
            // Unlock logic
            bool level1Completed = PlayerPrefs.GetInt("Level1Completed", 0) == 1;
            level2Button.interactable = level1Completed;
            
            // Visual feedback for locked button
            var canvasGroup = level2Button.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = level2Button.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = level1Completed ? 1f : 0.5f;
        }

        InitializeRecords();
    }

    public void PlayClickSound()
    {
        if (sfxSource != null && clickSound != null)
        {
            sfxSource.PlayOneShot(clickSound);
        }
    }

    /// <summary>
    /// Loads Level 1 (the A3 recreated Pac-Man level scene).
    /// </summary>
    public void LoadLevel1()
    {
        PlayClickSound();
        Debug.Log("Loading Level 1: " + level1SceneName);
        SceneManager.LoadScene(level1SceneName);
    }

    /// <summary>
    /// Loads Level 2 (InnovationScene), or does nothing / logs if not yet implemented.
    /// </summary>
    public void LoadLevel2()
    {
        PlayClickSound();
        if (PlayerPrefs.GetInt("Level1Completed", 0) == 1)
        {
            Debug.Log("Loading Level 2: " + level2SceneName);
            SceneManager.LoadScene(level2SceneName);
        }
        else
        {
            Debug.Log("Level 2 is locked! Complete Level 1 first.");
        }
    }

    private void InitializeRecords()
    {
        // Level 1
        int l1Score = PlayerPrefs.GetInt("Level1_HighScore", 0);
        float l1Time = PlayerPrefs.GetFloat("Level1_BestTime", 0);

        if (level1ScoreText != null)
            level1ScoreText.text = l1Score.ToString("D6");

        if (level1TimeText != null)
            level1TimeText.text = FormatTime(l1Time);

        // Level 2
        int l2Score = PlayerPrefs.GetInt("Level2_HighScore", 0);
        float l2Time = PlayerPrefs.GetFloat("Level2_BestTime", 0);

        if (level2ScoreText != null)
            level2ScoreText.text = l2Score.ToString("D6");

        if (level2TimeText != null)
            level2TimeText.text = FormatTime(l2Time);
    }

    private string FormatTime(float time)
    {
        if (time <= 0) return "00:00:00";
        int mins = (int)(time / 60);
        int secs = (int)(time % 60);
        int ms = (int)((time * 100) % 100);
        return string.Format("{0:00}:{1:00}:{2:00}", mins, secs, ms);
    }
}
