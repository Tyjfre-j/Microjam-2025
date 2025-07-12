using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;   // ← new Input System API
using System.Collections;           // ← add this
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("Jumpscare")]
public GameObject jumpscareImage;
    /* ---------------------- UI PANELS ---------------------- */
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject gameUIPanel;
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;
    

    /* ------------------- MAIN MENU ELEMENTS ---------------- */
    [Header("Main Menu Elements")]
    public Button startButton;
    public Button quitButton;
    public TextMeshProUGUI highScoreText;
    public Image titleLogo;                 // Image replaces title text

    /* -------------------- GAME UI ELEMENTS ----------------- */
    [Header("Game UI Elements")]
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI timeText;
    public Button pauseButton;
    public Slider angerBar;
    public TextMeshProUGUI angerLevelText;
    public Image angerBarFill;

    /* ------------------- PAUSE MENU ELEMENTS --------------- */
    [Header("Pause Menu Elements")]
    public Button resumeButton;
    public Button mainMenuButton;
    public Button restartButton;

    /* ------------------ GAME‑OVER ELEMENTS ----------------- */
    [Header("Game Over Elements")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI newHighScoreText;
    public Button playAgainButton;
    public Button backToMenuButton;

    /* ------------------ ANGER BAR SETTINGS ----------------- */
    [Header("Anger Bar Colors")]
    public Color[] angerColors = new Color[5]
    {
        Color.green,               // Level 0
        Color.yellow,              // Level 1
        new Color(1f, 0.5f, 0f),   // Level 2 (Orange)
        Color.red,                 // Level 3
        Color.black                // Level 4 (Max)
    };

    /* ---------------- ANIMATION TWEAKABLES ----------------- */
    [Header("Animation Settings")]
    public float fadeSpeed = 2f;
    public float scaleSpeed = 1f;

    /* ======================================================= */
    void Start()
    {
        SetupButtons();
        ShowMainMenu();
    }

    /* ----------------- BUTTON INITIALISATION --------------- */
    void SetupButtons()
    {
        if (startButton)    startButton.onClick.AddListener(() => GameManager.Instance.StartGame());
        if (quitButton)     quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());

        if (pauseButton)    pauseButton.onClick.AddListener(() => GameManager.Instance.PauseGame());

        if (resumeButton)   resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());
        if (mainMenuButton) mainMenuButton.onClick.AddListener(() => GameManager.Instance.ShowMainMenu());
        if (restartButton)  restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());

        if (playAgainButton)playAgainButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        if (backToMenuButton)backToMenuButton.onClick.AddListener(() => GameManager.Instance.ShowMainMenu());
    }

    /* ---------------------- MAIN MENU ---------------------- */
    public void ShowMainMenu()
    {
        HideAllPanels();

        if (mainMenuPanel)
        {
            mainMenuPanel.SetActive(true);
            UpdateHighScoreDisplay();
        }
    }
    public void ShowJumpscare()
{
    if (jumpscareImage) jumpscareImage.SetActive(true);
    StartCoroutine(HideJumpscareAfterDelay());
}

IEnumerator HideJumpscareAfterDelay()
{
    yield return new WaitForSeconds(1.5f); // Duration of jumpscare
    if (jumpscareImage) jumpscareImage.SetActive(false);
}

    /* ---------------------- GAME UI ------------------------ */
    public void ShowGameUI()
    {
        HideAllPanels();

        if (gameUIPanel)
        {
            gameUIPanel.SetActive(true);
            UpdateScore(0);
            UpdateAngerBar(0, 4);
        }
    }

    /* --------------------- PAUSE MENU ---------------------- */
    public void ShowPauseMenu()
    {
        if (pauseMenuPanel)
            pauseMenuPanel.SetActive(true);
    }

    /* ------------------- GAME‑OVER SCREEN ------------------ */
    public void ShowGameOverScreen()
    {
        HideAllPanels();

        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);

            if (finalScoreText)
                finalScoreText.text = "Final Score: " + GameManager.Instance.currentScore;

            if (newHighScoreText)
            {
                if (GameManager.Instance.currentScore > GameManager.Instance.GetHighScore())
                {
                    newHighScoreText.text = "NEW HIGH SCORE!";
                    newHighScoreText.gameObject.SetActive(true);
                }
                else
                {
                    newHighScoreText.gameObject.SetActive(false);
                }
            }
        }
    }

    /* --------------------- GENERIC HELPERS ----------------- */
    void HideAllPanels()
    {
        if (mainMenuPanel)  mainMenuPanel.SetActive(false);
        if (gameUIPanel)    gameUIPanel.SetActive(false);
        if (pauseMenuPanel) pauseMenuPanel.SetActive(false);
        if (gameOverPanel)  gameOverPanel.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (currentScoreText)
            currentScoreText.text = "Score: " + score;
    }

    public void UpdateAngerBar(int currentAnger, int maxAnger)
    {
        if (angerBar)
        {
            angerBar.value = (float)currentAnger / maxAnger;

            if (angerBarFill && currentAnger < angerColors.Length)
                angerBarFill.color = angerColors[currentAnger];
        }

        if (angerLevelText)
            angerLevelText.text = $"Boss Anger: {currentAnger}/{maxAnger}";
    }

    public void UpdateTime(float timeRemaining)
    {
        if (timeText)
            timeText.text = "Time: " + timeRemaining.ToString("F1") + "s";
    }

    void UpdateHighScoreDisplay()
    {
        if (highScoreText)
            highScoreText.text = "High Score: " + GameManager.Instance.GetHighScore();
    }
    

    /* -------------------- ESC KEY HANDLER ------------------ */
    void Update()
    {
        if (GameManager.Instance.isGameActive &&
            Keyboard.current != null &&          // safeguard in case no keyboard
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameManager.Instance.isGamePaused)
                GameManager.Instance.ResumeGame();
            else
                GameManager.Instance.PauseGame();
        }
    }
}
