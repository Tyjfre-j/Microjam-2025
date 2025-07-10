using System.Collections;
using UnityEngine;

public class PaperSpawner : MonoBehaviour
{
    public GameObject[] paperPrefabs;
    public Transform spawnPoint;
    public int papersPerWave = 10;
    public float delayBetweenPapers = 0.1f;

    public void StartSpawningWaves(int waveCount)
    {
        StartCoroutine(SpawnWaves(waveCount));
    }

    private IEnumerator SpawnWaves(int waveCount)
    {
        for (int i = 0; i < waveCount; i++)
        {
            for (int j = 0; j < papersPerWave; j++)
            {
                SpawnPaper();
                yield return new WaitForSeconds(delayBetweenPapers);
            }
        }
    }

    private void SpawnPaper()
    {
        if (paperPrefabs.Length == 0 || spawnPoint == null) return;

        var prefab = paperPrefabs[Random.Range(0, paperPrefabs.Length)];
        var paper = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        var stackManager = FindObjectOfType<PaperStackManager>();
        if (stackManager != null)
        {
            stackManager.AddPaper(paper);
        }
    }
}
