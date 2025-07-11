using UnityEngine;

public class StampEventHandler : MonoBehaviour
{
    public PaperSortingGame gameManager;

    public void OnStampHit()
    {
        if (gameManager != null)
        {
            gameManager.OnStampHit();
        }
    }
}
