using System.Collections;
using UnityEngine;

public class EnemyWaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave{
        public GameObject enemyPrefab;
        public int count = 5;
        public float rate = 1f; // enemies per second
    }

    public Wave[] waves;
    public Transform[] spawnPoints;

    private int nextWave = 0;
    private int aliveEnemies = 0;
    private bool spawningWave = false;

    void Update()
    {
        // If we're still spawning or enemies are alive, do nothing
        if (spawningWave || aliveEnemies > 0)
            return;

        // If no enemies alive → start next wave
        if(nextWave < waves.Length){
            StartCoroutine(SpawnWave(waves[nextWave]));
            nextWave++;
        }else{
            Debug.Log("All waves finished!");
        }
    }

    IEnumerator SpawnWave(Wave wave){
        spawningWave = true;
        Debug.Log("Spawning Wave " + (nextWave + 1));

        aliveEnemies = wave.count;

        for(int i = 0; i < wave.count; i++){
            SpawnEnemy(wave.enemyPrefab);
            yield return new WaitForSeconds(1f / wave.rate);
        }

        spawningWave = false;
    }

    void SpawnEnemy(GameObject enemyPrefab){
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, sp.position, sp.rotation);

        // Give enemy a callback so it reports its death
        //enemy.GetComponent<Enemy>().OnDeath = EnemyDied;
    }

    // Called by enemies when they die
    public void EnemyDied(){
        aliveEnemies--;

        if(aliveEnemies <= 0){
            Debug.Log("Wave cleared!");
        }
    }
}
