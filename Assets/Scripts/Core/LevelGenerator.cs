using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Inside face of the LEFT wall")]
    public Transform leftWall;
    [Tooltip("Inside face of the RIGHT wall")]
    public Transform rightWall;

    [Header("Prefabs")]
    public GameObject gatePrefab;
    public GameObject zombiePrefab;
    public GameObject bossPrefab;

    [Header("Gate Pair Layout")]
    public int gateCount = 4;          // number of PAIRS
    public float spacingZ = 50f;       // distance between pairs
    public float gateOffsetZ = 1f;     // first pair z
    public float centerGap = 0.2f;     // small gap in the middle
    public float sideMargin = 0.1f;    // distance from each wall to the post

    [Header("Gate Values")]
    public Vector2Int gateValueRange = new Vector2Int(1, 6);

    [Header("Enemy Spawns")]
    public int maxZombiesPerPair = 50;
    public float enemySpawnOffsetZ = 5f;
    public float enemySpacingX = 2f;
    [Tooltip("Vertical placement of enemies above ground (set to 1.6)")]
    public float enemyFootOffset = 1.6f;

    private void Start()
    {
        GenerateLevel();
    }

    void GenerateLevel()
    {
        if (!leftWall || !rightWall)
        {
            Debug.LogError("Assign leftWall and rightWall in LevelGenerator.");
            return;
        }

        // Inner usable width between walls
        float leftX = leftWall.position.x;
        float rightX = rightWall.position.x;
        if (rightX < leftX) (leftX, rightX) = (rightX, leftX);

        float innerWidth = (rightX - leftX) - (sideMargin * 2f);
        float halfWidth = (innerWidth - centerGap) * 0.5f;

        float leftHalfMinX = leftX + sideMargin;
        float leftHalfMaxX = leftHalfMinX + halfWidth;
        float rightHalfMaxX = rightX - sideMargin;
        float rightHalfMinX = rightHalfMaxX - halfWidth;

        for (int pairIndex = 0; pairIndex < gateCount; pairIndex++)
        {
            float z = pairIndex * spacingZ + gateOffsetZ;

            // Spawn pair centered in each half
            GameObject leftGateObj = Instantiate(
                gatePrefab,
                new Vector3((leftHalfMinX + leftHalfMaxX) * 0.5f, 1.5f, z),
                Quaternion.identity
            );

            GameObject rightGateObj = Instantiate(
                gatePrefab,
                new Vector3((rightHalfMinX + rightHalfMaxX) * 0.5f, 1.5f, z),
                Quaternion.identity
            );

            // Scale to fit half width
            LayoutGateToSpan(leftGateObj, leftHalfMinX, leftHalfMaxX);
            LayoutGateToSpan(rightGateObj, rightHalfMinX, rightHalfMaxX);

            // Types/values (first pair forced Add/Sub; right is opposite)
            int absVal = Random.Range(gateValueRange.x, gateValueRange.y + 1);
            GateType leftType = (pairIndex == 0)
                ? (Random.value < 0.5f ? GateType.Add : GateType.Subtract)
                : (GateType)Random.Range(0, 4);
            GateType rightType = OppositeOf(leftType);

            var lg = leftGateObj.GetComponent<Gate>();
            var rg = rightGateObj.GetComponent<Gate>();
            if (lg) { lg.type = leftType; lg.value = absVal; lg.Configure(leftType, absVal); }
            if (rg) { rg.type = rightType; rg.value = absVal; rg.Configure(rightType, absVal); }

            // 🔗 Link siblings so only one can trigger
            if (lg && rg)
            {
                lg.LinkSibling(rg);
                rg.LinkSibling(lg);
            }

            // Enemies behind this pair (flat ground at y=0, so just use foot offset)
            int zombieCount = Mathf.Min(pairIndex + 2, maxZombiesPerPair);
            float spawnZ = z + enemySpawnOffsetZ;

            if (pairIndex == gateCount - 1)
            {
                Instantiate(bossPrefab, new Vector3(0f, enemyFootOffset, spawnZ), Quaternion.identity);
            }
            else
            {
                for (int j = 0; j < zombieCount; j++)
                {
                    float xOffset = (j - (zombieCount - 1) / 2f) * enemySpacingX;
                    Vector3 pos = new Vector3(xOffset, enemyFootOffset, spawnZ);
                    Instantiate(zombiePrefab, pos, Quaternion.identity);
                }
            }
        }
    }

    static GateType OppositeOf(GateType t)
    {
        switch (t)
        {
            case GateType.Add: return GateType.Subtract;
            case GateType.Subtract: return GateType.Add;
            case GateType.Multiply: return GateType.Divide;
            case GateType.Divide: return GateType.Multiply;
            default: return GateType.Add;
        }
    }

    void LayoutGateToSpan(GameObject gateObj, float minX, float maxX)
    {
        if (!gateObj) return;

        float targetWidth = Mathf.Max(0.01f, maxX - minX);
        ScaleRendererToWidth(gateObj.transform, targetWidth);

        // Center root across the half
        Vector3 pos = gateObj.transform.position;
        pos.x = (minX + maxX) * 0.5f;
        gateObj.transform.position = pos;
    }

    void ScaleRendererToWidth(Transform target, float targetWidth)
    {
        if (!target) return;
        Renderer[] rends = target.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0) return;

        Renderer widest = rends[0];
        foreach (var r in rends) if (r.bounds.size.x > widest.bounds.size.x) widest = r;

        var mf = widest.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        float meshLocalWidth = mf.sharedMesh.bounds.size.x;
        float currentLossyX = widest.transform.lossyScale.x;
        float currentRenderedWidth = meshLocalWidth * currentLossyX;
        float scaleFactor = targetWidth / Mathf.Max(currentRenderedWidth, 1e-6f);

        Vector3 ls = target.localScale;
        ls.x *= scaleFactor;
        target.localScale = ls;
    }
}
