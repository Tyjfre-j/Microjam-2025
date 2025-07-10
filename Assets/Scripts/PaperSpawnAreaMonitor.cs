using UnityEngine;

public class PaperSpawnAreaMonitor : MonoBehaviour
{
    [HideInInspector]
    public bool handInArea = false; // True if the "Hand" is currently inside this trigger

    // Called when another collider enters this trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            handInArea = true;
            Debug.Log("Hand entered spawn area.");
        }
    }

    // Called when another collider exits this trigger
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Hand"))
        {
            handInArea = false;
            Debug.Log("Hand exited spawn area.");
        }
    }

    // Reminder: this object should have a trigger collider and a Rigidbody (kinematic is fine).
}
