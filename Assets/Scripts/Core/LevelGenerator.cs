using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject gatePrefab;
    public GameObject zombiePrefab;
    public GameObject bossPrefab;

    [Header("Gate Settings")]
    public int gateCount = 4;
    public float spacingZ = 50f;
    public float gateOffsetZ = 1f;

    [Header("Enemy Spawn Settings")]
    public int maxZombiesPerGate = 50;
    public float enemySpawnOffsetZ = 5f;
    public float enemySpacingX = 2f;

    private void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        for (int i = 0; i < gateCount; i++)
        {
            // Set gate position at Y = 1.5
            Vector3 gatePos = new Vector3(0, 1.5f, i * spacingZ + gateOffsetZ);
            GameObject gateObj = Instantiate(gatePrefab, gatePos, Quaternion.identity);

            // Assign gate logic
            Gate gate = gateObj.GetComponent<Gate>();

            if (i == 0)
            {
                // First gate must be Add or Multiply
                int safeType = Random.Range(0, 2) == 0 ? 0 : 2;
                gate.type = (GateType)safeType;
            }
            else
            {
                gate.type = (GateType)Random.Range(0, 4);
            }

            gate.value = Random.Range(1, 6);

            // Determine enemy count (progressively increasing)
            int zombieCount = Mathf.Min(i + 2, maxZombiesPerGate);

            Vector3 baseEnemyPos = gatePos + new Vector3(0, 0, enemySpawnOffsetZ);

            if (i == gateCount - 1)
            {
                // Boss behind last gate
                Instantiate(bossPrefab, baseEnemyPos, Quaternion.identity);
            }
            else
            {
                for (int j = 0; j < zombieCount; j++)
                {
                    float xOffset = (j - (zombieCount - 1) / 2f) * enemySpacingX;
                    Vector3 offsetPos = baseEnemyPos + new Vector3(xOffset, 0, 0);
                    Instantiate(zombiePrefab, offsetPos, Quaternion.identity);
                }
            }
        }
    }
}
