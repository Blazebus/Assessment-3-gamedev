using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Scene Navigation")]
    [SerializeField] private string startSceneName = "StartScene";

    [Header("Lives")]
    [SerializeField] private Image life1;
    [SerializeField] private Image life2;
    [SerializeField] private Image life3;

    [Header("Displays")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI ghostTimerText;
    [SerializeField] private TextMeshProUGUI levelNameText;

    [Header("Buttons")]
    [SerializeField] private Button exitButton;

    public Image Life1 => life1;
    public Image Life2 => life2;
    public Image Life3 => life3;
    public TextMeshProUGUI ScoreText => scoreText;
    public TextMeshProUGUI TimerText => timerText;
    public TextMeshProUGUI GhostTimerText => ghostTimerText;
    public TextMeshProUGUI LevelNameText => levelNameText;
    public Button ExitButton => exitButton;

    private void Awake()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClicked);
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
    }

    /// <summary>
    /// Returns the player to the Start Scene.
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("Exiting to: " + startSceneName);
        SceneManager.LoadScene(startSceneName);
    }
}
