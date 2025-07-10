using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CacheData
{
    public Cache_Paper_Color cacheColor;      // Enum for color comparison
    public GameObject cacheObject;            // The actual cache object in the scene
    public string grabAnimationName;          // Animation to grab this cache
    public string returnAnimationName;        // Animation to return cache to place
}

public class InputMapper : MonoBehaviour
{
    [Header("Cache Mappings")]
    public CacheData upArrowCache;
    public CacheData downArrowCache;
    public CacheData leftArrowCache;
    public CacheData rightArrowCache;

    private Dictionary<KeyCode, CacheData> inputMapping;

    public Dictionary<KeyCode, CacheData> CurrentMapping => inputMapping;

    void Awake()
    {
        InitializeMapping();
    }

    private void InitializeMapping()
    {
        inputMapping = new Dictionary<KeyCode, CacheData>
        {
            { KeyCode.UpArrow, upArrowCache },
            { KeyCode.DownArrow, downArrowCache },
            { KeyCode.LeftArrow, leftArrowCache },
            { KeyCode.RightArrow, rightArrowCache }
        };
    }

    public void ShuffleInputMapping()
    {
        List<KeyCode> keys = new List<KeyCode>
        {
            KeyCode.UpArrow,
            KeyCode.DownArrow,
            KeyCode.LeftArrow,
            KeyCode.RightArrow
        };

        List<CacheData> values = new List<CacheData>
        {
            upArrowCache,
            downArrowCache,
            leftArrowCache,
            rightArrowCache
        };

        // Fisher-Yates Shuffle
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }

        inputMapping.Clear();
        for (int i = 0; i < keys.Count; i++)
        {
            inputMapping[keys[i]] = values[i];
        }

        Debug.Log("Input mappings shuffled!");
        foreach (var kvp in inputMapping)
        {
            Debug.Log($"Key: {kvp.Key} → Color: {kvp.Value.cacheColor}, Grab: {kvp.Value.grabAnimationName}");
        }
    }
}

