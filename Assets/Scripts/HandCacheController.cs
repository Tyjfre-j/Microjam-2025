using UnityEngine;

public class HandCacheController : MonoBehaviour
{
    public Animator handAnimator;
    public InputMapper inputMapper;
    public GameManager gameManager;
    public PaperStackManager paperStackManager;
    public Transform cacheHoldPoint; // Assign in inspector

    private bool isAnimating = false;
    private GameObject heldCache;
    private CacheData currentCacheData;

    void Update()
    {
        if (isAnimating) return;

        foreach (var entry in inputMapper.CurrentMapping)
        {
            if (Input.GetKeyDown(entry.Key))
            {
                HandleCache(entry.Value);
                break;
            }
        }
    }

    void HandleCache(CacheData cache)
    {
        if (string.IsNullOrEmpty(cache.grabAnimationName)) return;

        if (!handAnimator.HasState(0, Animator.StringToHash(cache.grabAnimationName)))
        {
            Debug.LogWarning($"Animator does not contain state '{cache.grabAnimationName}'");
            return;
        }

        currentCacheData = cache;
        isAnimating = true;

        handAnimator.Play(cache.grabAnimationName);
    }

    // Called via Animation Event after hand reaches cache
    public void AttachCache()
    {
        if (currentCacheData.cacheObject == null || cacheHoldPoint == null) return;

        heldCache = currentCacheData.cacheObject;
        heldCache.transform.SetParent(cacheHoldPoint);
        heldCache.transform.localPosition = Vector3.zero;
        heldCache.transform.localRotation = Quaternion.identity;

        Debug.Log("Cache attached to hand");
    }

    // Called via Animation Event when hand is above the paper
    public void TryCachePaper()
    {
        var topPaper = paperStackManager.GetTopPaper();
        if (topPaper == null)
        {
            Debug.Log("No paper on stack.");
            return;
        }

        var paper = topPaper.GetComponent<Paper>();
        if (paper.colorType == currentCacheData.cacheColor)
        {
            paperStackManager.RemoveTopPaper();
            gameManager.OnPaperCached();
        }
        else
        {
            gameManager.IncreaseBossAnger();
        }

        handAnimator.Play(currentCacheData.returnAnimationName);
    }

    // Called via Animation Event near end of return animation
    public void DetachCache()
    {
        if (heldCache != null)
        {
            heldCache.transform.SetParent(null);
            heldCache = null;
            Debug.Log("Cache detached from hand");
        }
    }

    // Called at end of return animation
    public void FinishCacheCycle()
    {
        isAnimating = false;
        Debug.Log("Cache cycle complete, input re-enabled.");
    }
}
