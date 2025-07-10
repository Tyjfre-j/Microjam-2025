using System.Collections;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public PaperSpawner paperSpawner;
    public HandCacheController handController;
    public InputMapper inputMapper;
    public PaperStackManager paperStackManager;

    [Header("UI")]
    public GameObject countdownUI;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI bossAngerText;

    [Header("Settings")]
    public int papersPerWave = 10;
    public float countdownDelay = 1f;
    public int wavesBeforeShuffle = 3;
    public int maxBossAnger = 5;

    private int currentWave = 0;
    private int bossAngerLevel = 0;

    void Start()
    {
        handController.gameManager = this;
        handController.enabled = false;

        StartCoroutine(StartGameSequence());
        UpdateBossAngerUI();
    }

    IEnumerator StartGameSequence()
    {
        yield return StartCoroutine(SpawnAndWaitForCountdown());

        handController.enabled = true;
        Debug.Log("Game started. First wave in play.");
    }

    IEnumerator SpawnAndWaitForCountdown()
    {
        paperSpawner.papersPerWave = papersPerWave;
        paperSpawner.StartSpawningWaves(1);

        countdownUI.SetActive(true);
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(countdownDelay);
        }
        countdownText.text = "Go!";
        yield return new WaitForSeconds(countdownDelay);
        countdownUI.SetActive(false);
    }

    public void OnPaperCached()
    {
        if (paperStackManager.paperStack.Count == 0)
        {
            currentWave++;

            if (currentWave % wavesBeforeShuffle == 0)
            {
                inputMapper.ShuffleInputMapping();
            }

            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(1f);
        paperSpawner.papersPerWave = papersPerWave;
        paperSpawner.StartSpawningWaves(1);
    }

    public void IncreaseBossAnger()
    {
        bossAngerLevel++;
        UpdateBossAngerUI();

        if (bossAngerLevel >= maxBossAnger)
        {
            Debug.Log("Game Over - Boss too angry!");
            handController.enabled = false;
            StopAllCoroutines();
        }
    }

    private void UpdateBossAngerUI()
    {
        bossAngerText.text = $"Anger: {bossAngerLevel}/{maxBossAnger}";
    }
}
