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
        if (Application.CanStreamedLevelBeLoaded(level2SceneName))
        {
            Debug.Log("Loading Level 2: " + level2SceneName);
            SceneManager.LoadScene(level2SceneName);
        }
        else
        {
            Debug.Log("Level 2 (" + level2SceneName + ") is not yet implemented or not in Build Settings.");
        }
    }

    private void InitializeRecords()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        float bestTime = PlayerPrefs.GetFloat("BestTime", 0);

        if (level1ScoreText != null)
            level1ScoreText.text = highScore.ToString("D6");

        if (level1TimeText != null)
        {
            if (bestTime > 0)
            {
                int mins = (int)(bestTime / 60);
                int secs = (int)(bestTime % 60);
                int ms = (int)((bestTime * 100) % 100);
                level1TimeText.text = string.Format("{0:00}:{1:00}:{2:00}", mins, secs, ms);
            }
            else
            {
                level1TimeText.text = "00:00:00";
            }
        }

        if (level2ScoreText != null && string.IsNullOrEmpty(level2ScoreText.text))
            level2ScoreText.text = "000000";

        if (level2TimeText != null && string.IsNullOrEmpty(level2TimeText.text))
            level2TimeText.text = "00:00:00";
    }
}
