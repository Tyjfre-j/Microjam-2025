using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem; // 👈 required for new input system

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    [Header("Game State")]
    public bool isGameActive = false;
    public bool isGamePaused = false;
    
    [Header("References")]
    public PaperSortingGame paperSortingGame;
    public UIManager uiManager;
    
    [Header("Score System")]
    public int currentScore = 0;
    public int highScore = 0;
    
    private const string HIGH_SCORE_KEY = "HighScore";
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        LoadHighScore();
        ShowMainMenu();
    }
    
    public void StartGame()
    {
        isGameActive = true;
        currentScore = 0;
        
        if (paperSortingGame != null)
        {
            paperSortingGame.enabled = true;
            paperSortingGame.ResetGame();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowGameUI();
        }
    }
    
    public void PauseGame()
    {
        isGamePaused = true;
        Time.timeScale = 0f;
        
        if (uiManager != null)
        {
            uiManager.ShowPauseMenu();
        }
    }
    
    public void ResumeGame()
    {
        isGamePaused = false;
        Time.timeScale = 1f;
        
        if (uiManager != null)
        {
            uiManager.ShowGameUI();
        }
    }
    
    public void GameOver()
    {
        isGameActive = false;
        
        // Check if new high score
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
        }
        
        if (uiManager != null)
        {
            uiManager.ShowGameOverScreen();
        }
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    public void ShowMainMenu()
    {
        isGameActive = false;
        Time.timeScale = 1f;
        
        if (paperSortingGame != null)
        {
            paperSortingGame.enabled = false;
        }
        
        if (uiManager != null)
        {
            uiManager.ShowMainMenu();
        }
    }
    
    public void QuitGame()
    {
        Application.Quit();
    }
    
    public void UpdateScore(int newScore)
    {
        currentScore = newScore;
        
        if (uiManager != null)
        {
            uiManager.UpdateScore(currentScore);
        }
    }
    
    public void UpdateAngerLevel(int angerLevel, int maxAngerLevel)
    {
        if (uiManager != null)
        {
            uiManager.UpdateAngerBar(angerLevel, maxAngerLevel);
        }
    }
    
    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
    }
    
    void SaveHighScore()
    {
        PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
        PlayerPrefs.Save();
    }
    
    public int GetHighScore()
    {
        return highScore;
    }
}