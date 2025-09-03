using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

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
    public Vector2Int gateValueRange = new Vector2Int(1, 6); // used for + and ÷; × is always 2

    [Header("Enemy Spawns")]
    public int maxZombiesPerPair = 50;
    public float enemySpawnOffsetZ = 5f;
    public float enemySpacingX = 2f;
    [Tooltip("Vertical placement of enemies above ground (set to 1.6)")]
    public float enemyFootOffset = 1.6f;

    [Header("Enemy Spawn Scaling")]
    [Tooltip("Zombies contributed by troop count: floor((troops/10) * factor)")]
    public float zombiesPerTenTroops = 1.0f;     // e.g., 50 troops -> floor(5 * factor)
    [Tooltip("Extra zombies per gate already passed")]
    public int zombiesPerGatePassed = 2;

    [Header("Dynamic Subtract Scaling")]
    public int lowTroopMin = 1;
    public int highTroopMax = 100;
    public Vector2Int subtractAtLow = new Vector2Int(5, 10);     // when troopCount ~ lowTroopMin
    public Vector2Int subtractAtHigh = new Vector2Int(150, 250);  // when troopCount ~ highTroopMax

    // cached layout bounds
    float leftHalfMinX, leftHalfMaxX, rightHalfMinX, rightHalfMaxX;

    // sequential spawning
    int nextPairToSpawn = 0;

    void OnEnable() { Gate.GateTriggered += OnGateTriggered; }
    void OnDisable() { Gate.GateTriggered -= OnGateTriggered; }

    private void Start()
    {
        PrepareLayout();
        // Spawn the first pair only; others come after gate trigger
        SpawnPair(0);
        nextPairToSpawn = 1;
    }

    void PrepareLayout()
    {
        if (!leftWall || !rightWall)
        {
            Debug.LogError("Assign leftWall and rightWall in LevelGenerator.");
            return;
        }

        float leftX = leftWall.position.x;
        float rightX = rightWall.position.x;
        if (rightX < leftX) (leftX, rightX) = (rightX, leftX);

        float innerWidth = (rightX - leftX) - (sideMargin * 2f);
        float halfWidth = (innerWidth - centerGap) * 0.5f;

        leftHalfMinX = leftX + sideMargin;
        leftHalfMaxX = leftHalfMinX + halfWidth;
        rightHalfMaxX = rightX - sideMargin;
        rightHalfMinX = rightHalfMaxX - halfWidth;
    }

    void OnGateTriggered(int pairId)
    {
        // Only spawn the next pair when the just-passed pair matches (prevents double spawns)
        if (pairId == nextPairToSpawn - 1 && nextPairToSpawn < gateCount)
        {
            SpawnPair(nextPairToSpawn);
            nextPairToSpawn++;
        }
    }

    void SpawnPair(int pairIndex)
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

        // ---- Choose pair types with current rules ----
        int troopCountAtSpawn = ReadTroopCountSafe();
        (GateType a, GateType b) = PickPairTypes(pairIndex, troopCountAtSpawn);

        // Randomize left/right assignment
        bool flip = Random.value < 0.5f;
        GateType leftType = flip ? a : b;
        GateType rightType = flip ? b : a;

        // Base values: × is always 2; others randomized (− may be overridden)
        int leftVal = (leftType == GateType.Multiply) ? 2 : Random.Range(gateValueRange.x, gateValueRange.y + 1);
        int rightVal = (rightType == GateType.Multiply) ? 2 : Random.Range(gateValueRange.x, gateValueRange.y + 1);

        // Dynamic “−” magnitude from current troop count at spawn time
        (int subMin, int subMax) = DynamicSubtractRange(troopCountAtSpawn);

        if (leftType == GateType.Subtract && rightType == GateType.Subtract)
        {
            // Ensure two different values: one strictly LESS than the other
            int lesser = Random.Range(subMin, subMax + 1);
            int greater;

            if (lesser < subMax)
            {
                // pick something >= lesser+1 .. subMax
                greater = Random.Range(lesser + 1, subMax + 1);
            }
            else
            {
                // lesser == subMax; step one down to guarantee inequality
                lesser = Mathf.Max(1, subMax - 1);
                greater = subMax;
            }

            // Randomly assign which side is lesser/greater
            if (Random.value < 0.5f)
            {
                leftVal = lesser;
                rightVal = greater;
            }
            else
            {
                leftVal = greater;
                rightVal = lesser;
            }
        }
        else
        {
            // Single subtract in the pair: give it one dynamic value
            int subMag = Random.Range(subMin, subMax + 1);
            if (leftType == GateType.Subtract) leftVal = Mathf.Max(1, subMag);
            if (rightType == GateType.Subtract) rightVal = Mathf.Max(1, subMag);
        }

        var lg = leftGateObj.GetComponent<Gate>();
        var rg = rightGateObj.GetComponent<Gate>();
        if (lg) { lg.Configure(leftType, Mathf.Max(1, leftVal)); lg.pairId = pairIndex; }
        if (rg) { rg.Configure(rightType, Mathf.Max(1, rightVal)); rg.pairId = pairIndex; }

        // Link siblings so only one can trigger
        if (lg && rg)
        {
            lg.LinkSibling(rg);
            rg.LinkSibling(lg);
        }

        // ----- Enemy spawns scale with troop count AND gates passed -----
        int gatesPassed = pairIndex; // since we spawn pairIndex after passing previous one
        int fromTroops = Mathf.FloorToInt((troopCountAtSpawn / 10f) * Mathf.Max(0f, zombiesPerTenTroops));
        int fromGates = Mathf.Max(0, gatesPassed * zombiesPerGatePassed);
        int zombieCount = Mathf.Clamp(Mathf.Max(1, fromTroops + fromGates), 1, maxZombiesPerPair);

        float spawnZ = z + enemySpawnOffsetZ;

        if (pairIndex == gateCount - 1 && bossPrefab != null)
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

    // ----------------------------------------------------------------------
    // Pair picking logic:
    // - If troopCount > 50: ONLY allow (−,÷), (−,−), (−,×), regardless of pairIndex.
    // - Else: use standard set; for first two pairs, exclude only (−,÷) and (×,÷).
    // ----------------------------------------------------------------------
    (GateType a, GateType b) PickPairTypes(int pairIndex, int troopCount)
    {
        if (troopCount > 50)
        {
            var highAllowed = new List<(GateType, GateType)>
            {
                (GateType.Subtract, GateType.Divide),    // (−,÷)
                (GateType.Subtract, GateType.Subtract),  // (−,−)
                (GateType.Subtract, GateType.Multiply),  // (−,×)
            };
            int idxH = Random.Range(0, highAllowed.Count);
            return highAllowed[idxH];
        }

        var allowed = new List<(GateType, GateType)>
        {
            (GateType.Add,      GateType.Subtract),
            (GateType.Add,      GateType.Multiply),
            (GateType.Add,      GateType.Divide),
            (GateType.Subtract, GateType.Multiply),
            (GateType.Subtract, GateType.Divide),
            (GateType.Multiply, GateType.Divide),
        };

        if (pairIndex < 2)
        {
            allowed.Remove((GateType.Subtract, GateType.Divide)); // (−,÷)
            allowed.Remove((GateType.Multiply, GateType.Divide)); // (×,÷)
            allowed.Remove((GateType.Subtract, GateType.Multiply)); // (−,x)
        }

        int idx = Random.Range(0, allowed.Count);
        return allowed[idx];
    }

    // Map troop count to subtract magnitude (smooth):
    // lowTroopMin → subtractAtLow | highTroopMax → subtractAtHigh
    (int, int) DynamicSubtractRange(int troopCount)
    {
        int tMin = Mathf.Min(lowTroopMin, highTroopMax);
        int tMax = Mathf.Max(lowTroopMin, highTroopMax);

        int t = Mathf.Clamp(troopCount, tMin, tMax);
        float u = (t - tMin) / Mathf.Max(1f, (tMax - tMin)); // normalized 0..1

        float minVal = Mathf.Lerp(subtractAtLow.x, subtractAtHigh.x, u);
        float maxVal = Mathf.Lerp(subtractAtLow.y, subtractAtHigh.y, u);

        int minI = Mathf.RoundToInt(minVal);
        int maxI = Mathf.Max(minI, Mathf.RoundToInt(maxVal));
        return (minI, maxI);
    }

    int ReadTroopCountSafe()
    {
        int fallback = 10;
        var tm = FindFirstObjectByType<TroopManager>();
        if (tm == null) return fallback;

        // Try common shapes: GetTroopCount(), TroopCount property, Count property
        var m = tm.GetType().GetMethod("GetTroopCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null)
        {
            try { return Mathf.Max(1, (int)m.Invoke(tm, null)); } catch { }
        }
        var p = tm.GetType().GetProperty("TroopCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null)
        {
            try { return Mathf.Max(1, (int)p.GetValue(tm)); } catch { }
        }
        var p2 = tm.GetType().GetProperty("Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p2 != null)
        {
            try { return Mathf.Max(1, (int)p2.GetValue(tm)); } catch { }
        }
        return fallback;
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
