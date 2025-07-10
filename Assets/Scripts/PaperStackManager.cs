using System.Collections.Generic;
using UnityEngine;

public class PaperStackManager : MonoBehaviour
{
    public List<GameObject> paperStack = new List<GameObject>();

    public void AddPaper(GameObject paper)
    {
        paperStack.Insert(0, paper);
    }

    public GameObject GetTopPaper()
    {
        return paperStack.Count > 0 ? paperStack[0] : null;
    }

    public void RemoveTopPaper()
    {
        if (paperStack.Count > 0)
        {
            var top = paperStack[0];
            paperStack.RemoveAt(0);
            Destroy(top);
        }
    }
}