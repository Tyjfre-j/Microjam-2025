using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("State")]
    public bool isGameActive = false;
    public bool isGamePaused = false;

    [Header("Score")]
    public int currentScore = 0;

    [Header("Reference to UIManager")]
    public UIManager uiManager;

    private const string HighScoreKey = "HighScore"; // Key to store high score in PlayerPrefs

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        isGamePaused = false;
        currentScore = 0;

        Time.timeScale = 1f;
        uiManager.ShowGameUI();
    }

    public void PauseGame()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
        uiManager.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        uiManager.ShowGameUI();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowMainMenu()
    {
        isGameActive = false;
        isGamePaused = false;
        Time.timeScale = 1f;
        uiManager.ShowMainMenu();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public void EndGame()
    {
        isGameActive = false;
        Time.timeScale = 0f;

        SaveHighScore(); // ✅ Save if currentScore > existing high score

        uiManager.ShowGameOverScreen();
    }

    // ✅ Save high score using PlayerPrefs
    public void SaveHighScore()
    {
        int storedHighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (currentScore > storedHighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, currentScore);
            PlayerPrefs.Save();
            Debug.Log("🎉 New high score saved: " + currentScore);
        }
    }

    // ✅ Get high score from PlayerPrefs
    public int GetHighScore()
    {
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    // Optional: Reset high score (for debugging or menu button)
    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey(HighScoreKey);
        PlayerPrefs.Save();
        Debug.Log("High score reset.");
    }
}
