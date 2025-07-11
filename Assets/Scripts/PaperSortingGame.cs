using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaperSortingGame : MonoBehaviour
{
    [Header("Assign These in Inspector")]
    public GameObject[] paperPrefabs = new GameObject[4]; // Array for 4 different paper prefabs
    public Transform paperSpawnPoint;
    public Transform paperDestination;

    [Header("Desk Cachet Objects")]
    public GameObject[] cachetObjects = new GameObject[4]; // The actual cachet1, cachet2, cachet3, cachet4 GameObjects in the scene
    public Transform[] cachetPositions = new Transform[4]; // Original positions on desk

    [Header("Boss Anger Indicator")]
    public GameObject bossCylinder; // The boss cylinder object
    public Color[] angerColors = new Color[4] { Color.green, Color.yellow, Color.red, Color.black }; // 0-4 anger levels 

    [Header("Hand Animation")]
    public Animator handAnimator;
    public Renderer stampRenderer; // Assign the stamp model inside the hand
    public Material[] stampMaterials = new Material[4]; // 0 = Red, 1 = Blue, etc.

    [Header("Game Settings")]
    public float paperSpeed = 2f;
    public float spawnInterval = 2f;
    public float timeLimit = 2f; // Time limit for player to respond
    public float shuffleAnimationDuration = 1f; // How long the shuffle animation takes

    [Header("Colors")]
    public Color[] paperColors = new Color[4] { Color.red, Color.blue, Color.green, Color.yellow };

    // Key mappings: Up=Red, Down=Blue, Left=Green, Right=Yellow (initial)
    private string[] colorNames = new string[4] { "Red", "Blue", "Green", "Yellow" };

    // Current mapping after shuffles - indices correspond to Up, Down, Left, Right keys
    private int[] currentMapping = new int[4] { 0, 1, 2, 3 }; // Initially: Up=0(Red), Down=1(Blue), Left=2(Green), Right=3(Yellow)

    private List<GameObject> activePapers = new List<GameObject>();

    // Input System references
    private Keyboard keyboard;

    // Anger system
    private int angerLevel = 0;
    private const int maxAngerLevel = 4;
    private float paperReachedTime = 0f;
    private bool paperWaitingForInput = false;
    private bool gameOver = false;
    private bool isShuffling = false; // Prevent input during shuffle animation

    // NEW: Timer pause system
    private bool timerPaused = false;
    private float pausedTime = 0f; // Time accumulated while paused

    // NEW: Scoring system
    private int score = 0;

    private GameObject currentStampedPaper; // Holds the paper to be destroyed by animation
    private bool waitingForStampAnimation = false;


    void Start()
    {
        keyboard = Keyboard.current;
        UpdateBossColor();
        StartCoroutine(SpawnPapers());

        // NEW: Display initial score
        Debug.Log("Score: " + score);
    }

    IEnumerator SpawnPapers()
    {
        while (true)
        {
            // Only spawn if there are no active papers, game is not over, and not shuffling
            if (activePapers.Count == 0 && !gameOver && !isShuffling && !waitingForStampAnimation)
            {
                SpawnPaper();
            }
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }

    void SpawnPaper()
    {
        // Choose random paper prefab
        int colorIndex = Random.Range(0, paperPrefabs.Length);
        GameObject newPaper = Instantiate(paperPrefabs[colorIndex], paperSpawnPoint.position, Quaternion.identity);

        // Disable physics during movement
        Rigidbody rb = newPaper.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // Set paper data (no need to change color since prefab already has it)
        PaperData paperData = newPaper.GetComponent<PaperData>();
        paperData.colorIndex = colorIndex;
        paperData.targetRotation = paperDestination.rotation;
        paperData.targetPosition = paperDestination.position;
        paperData.speed = paperSpeed;
        paperData.isMoving = true;

        activePapers.Add(newPaper);
    }

    void Update()
    {
        if (gameOver || isShuffling) return;

        HandleInput();
        MovePapers();
        CheckTimeLimit();
    }

    void HandleInput()
    {
        if (keyboard == null || !paperWaitingForInput) return;

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            TriggerStampAnimation(0); // Red
            CheckPaperMatch(0);
        }
        else if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            TriggerStampAnimation(1); // Blue
            CheckPaperMatch(1);
        }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            TriggerStampAnimation(2); // Green
            CheckPaperMatch(2);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            TriggerStampAnimation(3); // Yellow
            CheckPaperMatch(3);
        }
    }

    void CheckPaperMatch(int keyIndex)
    {
        // Find papers that have reached the destination and are not moving
        GameObject targetPaper = null;

        foreach (GameObject paper in activePapers)
        {
            if (paper != null)
            {
                PaperData paperData = paper.GetComponent<PaperData>();

                // Only check papers that have reached the destination
                if (!paperData.isMoving)
                {
                    targetPaper = paper;
                    break; // Take the first one that reached destination
                }
            }
        }

        if (targetPaper != null)
        {
            PaperData paperData = targetPaper.GetComponent<PaperData>();
            int expectedColorIndex = currentMapping[keyIndex]; // What color this key should match

            if (paperData.colorIndex == expectedColorIndex)
            {
                // NEW: Increase score for correct match
                score++;
                Debug.Log("SUCCESS! Correct color match: " + colorNames[expectedColorIndex] + " | Score: " + score);

                activePapers.Remove(targetPaper);
                currentStampedPaper = targetPaper; // Delay destruction
                paperWaitingForInput = false;
                waitingForStampAnimation = true;
            }
            else
            {
                string keyName = GetKeyName(keyIndex);
                Debug.Log("Wrong color! Paper is " + colorNames[paperData.colorIndex] + " but you pressed " + keyName + " (expecting " + colorNames[expectedColorIndex] + ")");

                // Discard the paper and shuffle
                activePapers.Remove(targetPaper);
                Destroy(targetPaper);
                paperWaitingForInput = false;

                IncreaseAnger("Wrong key pressed!");
                StartCoroutine(ShuffleCachets());
            }
        }
    }

    public void OnStampHit()
    {
        // Only destroy if a stamp animation is waiting to resolve
        if (currentStampedPaper != null)
        {
            activePapers.Remove(currentStampedPaper);
            Destroy(currentStampedPaper);
            currentStampedPaper = null;

            paperWaitingForInput = false;
            waitingForStampAnimation = false; // ✅ Allow spawning again

            Debug.Log("STAMP HIT SUCCESS! Score: " + score);
        }
    }




    void CheckTimeLimit()
    {
        // NEW: Don't check time limit if timer is paused
        if (paperWaitingForInput && !timerPaused)
        {
            float elapsedTime = Time.time - paperReachedTime - pausedTime;

            if (elapsedTime > timeLimit)
            {
                Debug.Log("Time limit exceeded! Paper discarded.");

                // Find and discard the paper
                GameObject targetPaper = null;
                foreach (GameObject paper in activePapers)
                {
                    if (paper != null)
                    {
                        PaperData paperData = paper.GetComponent<PaperData>();
                        if (!paperData.isMoving)
                        {
                            targetPaper = paper;
                            break;
                        }
                    }
                }

                if (targetPaper != null)
                {
                    activePapers.Remove(targetPaper);
                    Destroy(targetPaper);
                }

                paperWaitingForInput = false;
                pausedTime = 0f; // Reset paused time
                IncreaseAnger("Time limit exceeded!");
            }
        }
    }

    void TriggerStampAnimation(int colorIndex)
    {
        // 1. Change stamp color first (instant)
        if (stampRenderer != null && stampMaterials != null && colorIndex >= 0 && colorIndex < stampMaterials.Length)
        {
            stampRenderer.material = stampMaterials[colorIndex];
        }

        // 2. Immediately play the animation without 1-frame delay
        if (handAnimator != null)
        {
            handAnimator.Play("Stamp", 0, 0f); // Replace "Stamp" with your actual animation state name
        }
    }

    IEnumerator ShuffleCachets()
    {
        isShuffling = true;

        // NEW: Pause the timer during shuffle
        PauseTimer();

        Debug.Log("Shuffling cachet positions...");

        // Store original positions
        Vector3[] originalPositions = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            originalPositions[i] = cachetObjects[i].transform.position;
        }

        // Create new random mapping using Fisher-Yates shuffle
        int[] newMapping = new int[4] { 0, 1, 2, 3 };

        // Shuffle the array to create a new random mapping
        for (int i = 0; i < newMapping.Length; i++)
        {
            int randomIndex = Random.Range(i, newMapping.Length);
            int temp = newMapping[i];
            newMapping[i] = newMapping[randomIndex];
            newMapping[randomIndex] = temp;
        }

        // Animate the shuffle
        float elapsedTime = 0f;
        Vector3[] targetPositions = new Vector3[4];

        // Calculate target positions:
        // newMapping[keyIndex] tells us which color should be at which key position
        // We need to move each cachet object to its new position
        for (int keyIndex = 0; keyIndex < 4; keyIndex++)
        {
            int colorIndex = newMapping[keyIndex]; // Which color should be at this key position
            targetPositions[colorIndex] = cachetPositions[keyIndex].position; // Move that color's cachet to this position
        }

        // Animate movement
        while (elapsedTime < shuffleAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / shuffleAnimationDuration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth animation curve

            for (int i = 0; i < 4; i++)
            {
                cachetObjects[i].transform.position = Vector3.Lerp(originalPositions[i], targetPositions[i], t);
            }

            yield return null;
        }

        // Ensure final positions are exact
        for (int i = 0; i < 4; i++)
        {
            cachetObjects[i].transform.position = targetPositions[i];
        }

        // Update the mapping
        currentMapping = newMapping;

        // Log the new mapping
        Debug.Log("New mapping - Up: " + colorNames[currentMapping[0]] +
                 ", Down: " + colorNames[currentMapping[1]] +
                 ", Left: " + colorNames[currentMapping[2]] +
                 ", Right: " + colorNames[currentMapping[3]]);

        // NEW: Resume the timer after shuffle
        ResumeTimer();

        isShuffling = false;
    }

    // NEW: Timer pause methods
    void PauseTimer()
    {
        if (!timerPaused)
        {
            timerPaused = true;
            Debug.Log("Timer paused during shuffle animation");
        }
    }

    void ResumeTimer()
    {
        if (timerPaused)
        {
            timerPaused = false;
            pausedTime += shuffleAnimationDuration; // Add the shuffle duration to paused time
            Debug.Log("Timer resumed after shuffle animation");
        }
    }

    // NEW: Public method to get current score
    public int GetScore()
    {
        return score;
    }

    // NEW: Public method to reset score (if needed)
    public void ResetScore()
    {
        score = 0;
        Debug.Log("Score reset to: " + score);
    }

    string GetPositionName(Vector3 position)
    {
        float minDistance = float.MaxValue;
        string positionName = "Unknown";

        for (int i = 0; i < cachetPositions.Length; i++)
        {
            float distance = Vector3.Distance(position, cachetPositions[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                positionName = GetKeyName(i);
            }
        }

        return positionName;
    }

    string GetKeyName(int keyIndex)
    {
        switch (keyIndex)
        {
            case 0: return "Up";
            case 1: return "Down";
            case 2: return "Left";
            case 3: return "Right";
            default: return "Unknown";
        }
    }

    void IncreaseAnger(string reason)
    {
        angerLevel++;
        Debug.Log(reason + " Anger Level: " + angerLevel + "/" + maxAngerLevel);

        UpdateBossColor();

        if (angerLevel >= maxAngerLevel)
        {
            GameOver();
        }
    }

    void UpdateBossColor()
    {
        if (bossCylinder != null)
        {
            Renderer bossRenderer = bossCylinder.GetComponent<Renderer>();
            if (bossRenderer != null)
            {
                bossRenderer.material.color = angerColors[angerLevel];
            }
        }
    }

    void GameOver()
    {
        gameOver = true;
        Debug.Log("GAME OVER! Maximum anger level reached! Final Score: " + score);

        // Destroy all remaining papers
        foreach (GameObject paper in activePapers)
        {
            if (paper != null)
            {
                Destroy(paper);
            }
        }
        activePapers.Clear();
    }

    void MovePapers()
    {
        for (int i = activePapers.Count - 1; i >= 0; i--)
        {
            if (activePapers[i] == null)
            {
                activePapers.RemoveAt(i);
                continue;
            }

            GameObject paper = activePapers[i];
            PaperData paperData = paper.GetComponent<PaperData>();

            if (paperData.isMoving)
            {
                // Move paper towards destination
                paper.transform.position = Vector3.MoveTowards(
                    paper.transform.position,
                    paperData.targetPosition,
                    paperData.speed * Time.deltaTime


                );
                paper.transform.rotation = Quaternion.RotateTowards(
    paper.transform.rotation,
    paperData.targetRotation,
                 360 * Time.deltaTime // Degrees per second — adjust for smoothness
);

                // Check if paper reached destination
                if (Vector3.Distance(paper.transform.position, paperData.targetPosition) < 0.1f)
                {
                    paperData.isMoving = false;
                    paperWaitingForInput = true;
                    paperReachedTime = Time.time;
                    pausedTime = 0f; // Reset paused time for new paper

                    Debug.Log("Paper reached destination! Current mapping - Up: " + colorNames[currentMapping[0]] +
                             ", Down: " + colorNames[currentMapping[1]] +
                             ", Left: " + colorNames[currentMapping[2]] +
                             ", Right: " + colorNames[currentMapping[3]]);

                    // Enable physics when it reaches destination
                    Rigidbody rb = paper.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false;
                    }
                }
            }
        }
    }
}