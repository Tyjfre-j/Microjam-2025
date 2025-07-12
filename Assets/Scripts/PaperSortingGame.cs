using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem; // 👈 required for new input system


public class PaperSortingGame : MonoBehaviour
{
    /* -------------------------------------------------
       INSPECTOR FIELDS
       -------------------------------------------------*/
    [Header("Assign These in Inspector")]
    public GameObject[] paperPrefabs = new GameObject[4];          // 4 paper prefabs (couleurs)
    public Transform   paperSpawnPoint;
    public Transform   paperDestination;

    [Header("Desk Cachet Objects")]
    public GameObject[] cachetObjects   = new GameObject[4];       // Cachets physiques
    public Transform[]  cachetPositions = new Transform[4];        // Positions de base sur le bureau

    [Header("Boss Anger Indicator")]
    public GameObject bossCylinder;
    public Color[]    angerColors = new Color[4]
    { Color.green, Color.yellow, Color.red, Color.black };         // 0‑4 niveaux de colère

    [Header("Hand Animation")]
    public Animator  handAnimator;
    public Renderer  stampRenderer;                                // Renderer du tampon
    public Material[] stampMaterials = new Material[4];            // Matériaux du tampon

    [Header("Game Settings")]
    public float paperSpeed              = 2f;
    public float spawnInterval           = 2f;
    public float timeLimit               = 2f;                     // Temps de réaction du joueur
    public float shuffleAnimationDuration= 1f;                     // Durée du mélange

    [Header("Colors")]
    public Color[] paperColors = new Color[4]
    { Color.red, Color.blue, Color.green, Color.yellow };

    [Header("Dynamic Difficulty Settings")]
    public float difficultyRampInterval = 15f;
    public float maxPaperSpeed          = 6f;
    public float minTimeLimit           = 0.8f;
    public float maxStampSpeed          = 2f;
    public float minShuffleDuration     = 0.4f;

    [Header("Animation Settings")]
    public float    stampAnimationSpeed = 1f;
    public Animator BossAnimator;

    /* -------------------------------------------------
       PRIVATE STATE
       -------------------------------------------------*/
    private float  gameTimeElapsed = 0f;
    private float  difficultyTimer = 0f;
    private bool   isBossAnimating = false;

    // Mapping des touches : Up=0(Red), Down=1(Blue), Left=2(Green), Right=3(Yellow)
    private readonly string[] colorNames     = { "Red", "Blue", "Green", "Yellow" };
    private          int[]    currentMapping = { 0, 1, 2, 3 };

    private List<GameObject> activePapers = new List<GameObject>();

    // Input System
    private Keyboard keyboard;

    // Anger / score / états
    private int   angerLevel   = 0;
    private const int maxAngerLevel = 4;
    private float paperReachedTime = 0f;
    private bool  paperWaitingForInput = false;
    private bool  gameOver     = false;
    private bool  isShuffling  = false;
    private bool  waitingForStampAnimation = false;

    // Timer pause (pendant shuffle)
    private bool  timerPaused = false;
    private float pausedTime  = 0f;

    // Scoring
    private int score = 0;
    private int correctCacheStreak = 0;
    public  int requiredStreakToCalm = 3;

    private GameObject currentStampedPaper;

    /* -------------------------------------------------
       UNITY LIFECYCLE
       -------------------------------------------------*/
    void Start()
    {
        keyboard = Keyboard.current;
        UpdateBossColor();
        StartCoroutine(SpawnPapers());

        Debug.Log("Score: " + score);
    }

    void Update()
    {
        // Si le GameManager existe et a mis le jeu en pause ⇒ on sort
        if (GameManager.Instance != null && !GameManager.Instance.isGameActive)
            return;

        if (gameOver || isShuffling)
            return;

        gameTimeElapsed += Time.deltaTime;
        difficultyTimer += Time.deltaTime;

        if (difficultyTimer >= difficultyRampInterval)
        {
            difficultyTimer = 0f;
            IncreaseDifficulty();
        }

        HandleInput();
        MovePapers();
        CheckTimeLimit();
    }

    /* -------------------------------------------------
       PUBLIC API
       -------------------------------------------------*/

    /// <summary>Remet le jeu à zéro (appelable depuis UI/menu).</summary>
    public void ResetGame()
    {
        // Réinitialiser les flags / états
        gameOver                = false;
        isShuffling             = false;
        isBossAnimating         = false;
        paperWaitingForInput    = false;
        waitingForStampAnimation= false;
        timerPaused             = false;

        // Paramètres de difficulté (valeurs de départ)
        paperSpeed              = 2f;
        timeLimit               = 2f;
        stampAnimationSpeed     = 1f;
        shuffleAnimationDuration= 1f;

        // Score / colère
        angerLevel          = 0;
        score               = 0;
        correctCacheStreak  = 0;
        gameTimeElapsed     = 0f;
        difficultyTimer     = 0f;
        pausedTime          = 0f;

        // Mapping par défaut
        currentMapping = new int[4] { 0, 1, 2, 3 };

        // Détruire les papiers restants
        foreach (GameObject paper in activePapers)
            if (paper != null) Destroy(paper);
        activePapers.Clear();

        // Replacer les cachets
        ResetCachetPositions();

        // UI / GameManager
        UpdateBossColor();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateScore(score);
            GameManager.Instance.UpdateAngerLevel(angerLevel, maxAngerLevel);
        }

        Debug.Log("Game Reset Complete");
    }

    /// <summary>Renvoie le score courant.</summary>
    public int GetScore() => score;

    /// <summary>Réinitialise uniquement le score (optionnel).</summary>
    public void ResetScore()
    {
        score = 0;
        Debug.Log("Score reset to: " + score);

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateScore(score);
    }

    /* -------------------------------------------------
       COROUTINES
       -------------------------------------------------*/
    IEnumerator SpawnPapers()
    {
        while (true)
        {
            if (activePapers.Count == 0 && !gameOver && !isShuffling && !waitingForStampAnimation)
                SpawnPaper();

            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator ShuffleCachets()
    {
        isShuffling = true;
        PauseTimer();

        Debug.Log("Shuffling cachet positions…");

        /* 1. Enregistrer positions initiales */
        Vector3[] originalPositions = new Vector3[4];
        for (int i = 0; i < 4; i++)
            originalPositions[i] = cachetObjects[i].transform.position;

        /* 2. Générer une nouvelle permutation (Fisher‑Yates) */
        int[] newMapping = { 0, 1, 2, 3 };
        for (int i = 0; i < newMapping.Length; i++)
        {
            int r = Random.Range(i, newMapping.Length);
            (newMapping[i], newMapping[r]) = (newMapping[r], newMapping[i]);
        }

        /* 3. Calculer les positions cibles */
        Vector3[] targetPositions = new Vector3[4];
        for (int keyIndex = 0; keyIndex < 4; keyIndex++)
        {
            int colorIndex       = newMapping[keyIndex];
            targetPositions[colorIndex] = cachetPositions[keyIndex].position;
        }

        /* 4. Animation */
        float t = 0f;
        while (t < shuffleAnimationDuration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.SmoothStep(0f, 1f, t / shuffleAnimationDuration);
            for (int i = 0; i < 4; i++)
                cachetObjects[i].transform.position = Vector3.Lerp(originalPositions[i], targetPositions[i], lerp);

            yield return null;
        }
        for (int i = 0; i < 4; i++)
            cachetObjects[i].transform.position = targetPositions[i];

        currentMapping = newMapping;

        Debug.Log($"New mapping – Up:{colorNames[currentMapping[0]]}, Down:{colorNames[currentMapping[1]]}, Left:{colorNames[currentMapping[2]]}, Right:{colorNames[currentMapping[3]]}");

        ResumeTimer();
        isShuffling = false;
    }

    IEnumerator WaitForBossAnimationThenShuffle()
    {
        isBossAnimating = true;

        if (BossAnimator != null)
        {
            string state = $"Anger{angerLevel}";
            BossAnimator.SetTrigger(state);
            yield return null;

            while (!BossAnimator.GetCurrentAnimatorStateInfo(0).IsName(state))
                yield return null;

            yield return new WaitForSeconds(BossAnimator.GetCurrentAnimatorStateInfo(0).length);
        }

        isBossAnimating = false;
        yield return StartCoroutine(ShuffleCachets());
    }

    /* -------------------------------------------------
       INPUT / LOGIC
       -------------------------------------------------*/
    void HandleInput()
    {
        if (keyboard == null || !paperWaitingForInput || waitingForStampAnimation || isShuffling || isBossAnimating)
            return;

        if      (keyboard.upArrowKey.wasPressedThisFrame)    { TriggerStampAnimation(0); CheckPaperMatch(0); }
        else if (keyboard.downArrowKey.wasPressedThisFrame)  { TriggerStampAnimation(1); CheckPaperMatch(1); }
        else if (keyboard.leftArrowKey.wasPressedThisFrame)  { TriggerStampAnimation(2); CheckPaperMatch(2); }
        else if (keyboard.rightArrowKey.wasPressedThisFrame) { TriggerStampAnimation(3); CheckPaperMatch(3); }
    }

    void CheckPaperMatch(int keyIndex)
    {
        GameObject targetPaper = null;
        foreach (GameObject paper in activePapers)
        {
            if (paper == null) continue;
            PaperData pd = paper.GetComponent<PaperData>();
            if (!pd.isMoving) { targetPaper = paper; break; }
        }
        if (targetPaper == null) return;

        PaperData data = targetPaper.GetComponent<PaperData>();
        int expected   = currentMapping[keyIndex];

        if (data.colorIndex == expected)
        {
            /* ---------- CORRECT ---------- */
            score++;
            correctCacheStreak++;

            if (GameManager.Instance != null)
                GameManager.Instance.UpdateScore(score);

            if (correctCacheStreak >= requiredStreakToCalm)
            {
                DecreaseAnger();
                correctCacheStreak = 0;
            }

            Debug.Log($"SUCCESS! {colorNames[expected]} | Score: {score}");
        }
        else
        {
            /* ---------- WRONG ---------- */
            Debug.Log($"Wrong color! Paper:{colorNames[data.colorIndex]} vs Key:{GetKeyName(keyIndex)}");
            correctCacheStreak = 0;

            if (GameManager.Instance != null)
                GameManager.Instance.UpdateScore(score);          // score inchangé mais notification éventuelle

            IncreaseAnger("Wrong key pressed!");
            StartCoroutine(WaitForBossAnimationThenShuffle());
        }

        activePapers.Remove(targetPaper);
        currentStampedPaper   = targetPaper;
        paperWaitingForInput  = false;
        waitingForStampAnimation = true;
    }

    void MovePapers()
    {
        for (int i = activePapers.Count - 1; i >= 0; i--)
        {
            if (activePapers[i] == null) { activePapers.RemoveAt(i); continue; }

            GameObject paper = activePapers[i];
            PaperData  pd    = paper.GetComponent<PaperData>();

            if (pd.isMoving)
            {
                pd.speed = paperSpeed;

                paper.transform.position = Vector3.MoveTowards(
                    paper.transform.position, pd.targetPosition, pd.speed * Time.deltaTime);
                paper.transform.rotation = Quaternion.RotateTowards(
                    paper.transform.rotation, pd.targetRotation, 360f * Time.deltaTime);

                if (Vector3.Distance(paper.transform.position, pd.targetPosition) < 0.1f)
                {
                    pd.isMoving = false;
                    paperWaitingForInput = true;
                    paperReachedTime     = Time.time;
                    pausedTime           = 0f;

                    Debug.Log($"Paper reached – Up:{colorNames[currentMapping[0]]}, Down:{colorNames[currentMapping[1]]}, Left:{colorNames[currentMapping[2]]}, Right:{colorNames[currentMapping[3]]}");

                    Rigidbody rb = paper.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = false;
                }
            }
        }
    }

    /* -------------------------------------------------
       TIME / DIFFICULTY
       -------------------------------------------------*/
    void IncreaseDifficulty()
    {
        paperSpeed          = Mathf.Min(paperSpeed + 0.5f, maxPaperSpeed);
        timeLimit           = Mathf.Max(timeLimit - 0.2f, minTimeLimit);
        stampAnimationSpeed = Mathf.Min(stampAnimationSpeed + 0.25f, maxStampSpeed);
        shuffleAnimationDuration = Mathf.Max(shuffleAnimationDuration - 0.1f, minShuffleDuration);

        Debug.Log($"[Difficulty+] Speed:{paperSpeed} Time:{timeLimit} StampSpd:{stampAnimationSpeed} Shuffle:{shuffleAnimationDuration}");
    }

    void CheckTimeLimit()
    {
        if (!paperWaitingForInput || timerPaused) return;

        float elapsed = Time.time - paperReachedTime - pausedTime;
        float remaining = timeLimit - elapsed;

        // UI – afficher temps restant
        if (GameManager.Instance?.uiManager != null)
            GameManager.Instance.uiManager.UpdateTime(Mathf.Max(0f, remaining));

        if (elapsed <= timeLimit) return;

        Debug.Log("Time limit exceeded! Paper discarded.");

        GameObject target = null;
        foreach (GameObject paper in activePapers)
        {
            if (paper == null) continue;
            if (!paper.GetComponent<PaperData>().isMoving)
            { target = paper; break; }
        }

        if (target != null)
        {
            activePapers.Remove(target);
            Destroy(target);
        }

        paperWaitingForInput = false;
        pausedTime           = 0f;

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateScore(score);

        IncreaseAnger("Time limit exceeded!");
        correctCacheStreak = 0;
    }

    /* -------------------------------------------------
       ANGER SYSTEM
       -------------------------------------------------*/
    void IncreaseAnger(string reason)
    {
        angerLevel = Mathf.Clamp(angerLevel + 1, 0, maxAngerLevel);
        Debug.Log($"{reason} Anger:{angerLevel}/{maxAngerLevel}");

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateAngerLevel(angerLevel, maxAngerLevel);

        if (BossAnimator != null)
            BossAnimator.SetTrigger($"Anger{angerLevel}");

        UpdateBossColor();

        if (angerLevel >= maxAngerLevel)
            GameOver();
    }

    void DecreaseAnger()
    {
        if (angerLevel <= 0) return;

        angerLevel--;
        Debug.Log($"Boss calming… Anger:{angerLevel}/{maxAngerLevel}");

        if (GameManager.Instance != null)
            GameManager.Instance.UpdateAngerLevel(angerLevel, maxAngerLevel);

        UpdateBossColor();
    }

    void UpdateBossColor()
    {
        if (bossCylinder == null) return;

        Renderer r = bossCylinder.GetComponent<Renderer>();
        if (r != null && angerLevel >= 0 && angerLevel < angerColors.Length)
            r.material.color = angerColors[angerLevel];
    }

    /* -------------------------------------------------
       GAME OVER
       -------------------------------------------------*/
    void GameOver()
    {
        gameOver = true;
        Debug.Log($"GAME OVER! Max anger. Final Score:{score}");

        foreach (GameObject p in activePapers)
            if (p != null) Destroy(p);
        activePapers.Clear();

        if (GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }

    /* -------------------------------------------------
       ANIMATION HOOKS
       -------------------------------------------------*/
    public void OnStampHit()      // Appel via AnimationEvent
    {
        if (currentStampedPaper != null)
        {
            Destroy(currentStampedPaper);
            currentStampedPaper = null;

            paperWaitingForInput    = false;
            waitingForStampAnimation= false;
            handAnimator.speed      = 1f;

            Debug.Log($"STAMP HIT COMPLETE! Score:{score}");
        }
    }

    void TriggerStampAnimation(int colorIndex)
    {
        if (stampRenderer != null && colorIndex >= 0 && colorIndex < stampMaterials.Length)
            stampRenderer.material = stampMaterials[colorIndex];

        if (handAnimator != null)
        {
            handAnimator.speed = stampAnimationSpeed;
            handAnimator.Play("Stamp", 0, 0f);
        }
    }

    /* -------------------------------------------------
       HELPERS
       -------------------------------------------------*/
    void SpawnPaper()
    {
        int colorIndex = Random.Range(0, paperPrefabs.Length);
        GameObject paper = Instantiate(paperPrefabs[colorIndex], paperSpawnPoint.position, Quaternion.identity);

        Rigidbody rb = paper.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        PaperData pd   = paper.GetComponent<PaperData>();
        pd.colorIndex  = colorIndex;
        pd.targetRotation = paperDestination.rotation;
        pd.targetPosition = paperDestination.position;
        pd.speed          = paperSpeed;
        pd.isMoving       = true;

        activePapers.Add(paper);
    }

    void PauseTimer()
    {
        if (!timerPaused)
        {
            timerPaused = true;
            Debug.Log("Timer paused (shuffle)");
        }
    }

    void ResumeTimer()
    {
        if (timerPaused)
        {
            timerPaused = false;
            pausedTime += shuffleAnimationDuration;
            Debug.Log("Timer resumed");
        }
    }

    void ResetCachetPositions()
    {
        for (int i = 0; i < 4; i++)
        {
            if (cachetObjects[i] != null && cachetPositions[i] != null)
                cachetObjects[i].transform.position = cachetPositions[i].position;
        }
    }

    /* -------------------------------------------------
       DEBUG GUI (optionnel)
       -------------------------------------------------*/
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20), $"Paper Speed: {paperSpeed:F2}");
        GUI.Label(new Rect(10, 30, 400, 20), $"Time Limit : {timeLimit:F2}");
        GUI.Label(new Rect(10, 50, 400, 20), $"Stamp Speed: {stampAnimationSpeed:F2}");
        GUI.Label(new Rect(10, 70, 400, 20), $"Shuffle Dur.: {shuffleAnimationDuration:F2}");
    }

    string GetKeyName(int i) => i switch
    {
        0 => "Up", 1 => "Down", 2 => "Left", 3 => "Right", _ => "Unknown"
    };
}