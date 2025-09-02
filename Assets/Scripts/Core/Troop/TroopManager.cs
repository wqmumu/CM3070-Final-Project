using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopManager : MonoBehaviour
{
    public GameObject troopPrefab;
    public Transform spawnPoint;
    public int startingTroops = 1;
    public int maxTroops = 100;

    private readonly List<GameObject> activeTroops = new List<GameObject>();
    private readonly Queue<GameObject> troopPool = new Queue<GameObject>();

    private Transform leader;

    // Enemies listen for leader changes
    public static event Action<Transform> OnLeaderChanged;

    // Troops listen to pause/resume during combat
    public static event Action<bool> OnCombatStateChanged;

    [Header("Combat Engage Settings")]
    [Tooltip("How far ahead of the leader to scan for enemies")]
    [SerializeField] private float engageRadius = 7f;
    [Tooltip("Small bias so 'ahead' feels in front of the leader along +Z")]
    [SerializeField] private float aheadBiasZ = 0.5f;
    [Tooltip("Optional: restrict scan to your Enemy layer")]
    [SerializeField] private LayerMask enemyMask = 0;

    private bool combatEngaged = false;

    void Start()
    {
        InitPool(maxTroops);
        SpawnInitialTroop();

        int extra = Mathf.Clamp(startingTroops - 1, 0, maxTroops);
        SpawnTroops(extra);
    }

    void Update()
    {
        ScanForEngagement();
    }

    void InitPool(int size)
    {
        for (int i = 0; i < size; i++)
        {
            var troop = Instantiate(troopPrefab);
            troop.SetActive(false);
            troopPool.Enqueue(troop);
        }
    }

    void SpawnInitialTroop()
    {
        if (troopPool.Count == 0) return;

        GameObject firstTroop = troopPool.Dequeue();
        firstTroop.transform.position = spawnPoint.position;
        firstTroop.transform.SetParent(null);
        firstTroop.SetActive(true);
        activeTroops.Add(firstTroop);

        firstTroop.GetComponent<TroopMovement>()?.SetAsLeader(true);
        SetLeader(firstTroop.transform);

        StartCoroutine(FlashTroop(firstTroop));
    }

    void SpawnTroops(int count)
    {
        EnsureLeaderIsValid();
        if (leader == null) return;

        for (int i = 0; i < count; i++)
        {
            if (troopPool.Count == 0) return;

            var troop = troopPool.Dequeue();
            Vector3 basePos = leader.position;
            troop.transform.position = basePos + GetOffset(i + 1);
            troop.transform.SetParent(null);
            troop.SetActive(true);
            activeTroops.Add(troop);

            StartCoroutine(FlashTroop(troop));
        }
    }

    // --- Public API used by gates/enemies ------------------------------------

    public void RemoveTroops(int count)
    {
        int toRemove = Mathf.Min(count, activeTroops.Count);
        for (int i = 0; i < toRemove; i++)
        {
            var troop = activeTroops[activeTroops.Count - 1];
            RemoveOneTroop(troop);
        }
    }

    public void DivideTroops(int divisor)
    {
        if (divisor <= 0) return;
        int current = activeTroops.Count;
        int target = Mathf.Max(1, current / divisor);
        int toRemove = current - target;

        for (int i = 0; i < toRemove && activeTroops.Count > 0; i++)
        {
            var troop = activeTroops[activeTroops.Count - 1];
            RemoveOneTroop(troop);
        }
    }

    public void SubtractTroops(int amount)
    {
        int toRemove = Mathf.Min(amount, activeTroops.Count);
        for (int i = 0; i < toRemove && activeTroops.Count > 0; i++)
        {
            var troop = activeTroops[activeTroops.Count - 1];
            RemoveOneTroop(troop);
        }
    }

    public void MultiplyTroops(int factor)
    {
        int currentCount = activeTroops.Count;
        int newCount = Mathf.Min(currentCount * factor, maxTroops);
        int toAdd = newCount - currentCount;
        SpawnTroops(toAdd);
    }

    public void AddTroops(int amount)
    {
        int newCount = Mathf.Min(activeTroops.Count + amount, maxTroops);
        int toAdd = newCount - activeTroops.Count;
        SpawnTroops(toAdd);
    }

    public int GetTroopCount() => activeTroops.Count;
    public List<GameObject> GetActiveTroops() => activeTroops;

    public Transform GetLeadTroop()
    {
        EnsureLeaderIsValid();
        return leader;
    }

    // --- Internals ------------------------------------------------------------

    private void RemoveOneTroop(GameObject troop)
    {
        if (!troop) return;

        activeTroops.Remove(troop);

        if (leader == troop.transform)
            SetLeader(null);

        var unit = troop.GetComponent<TroopUnit>();
        if (unit != null)
        {
            unit.PlayDeath(() =>
            {
                troop.SetActive(false);
                troopPool.Enqueue(troop);
            });
        }
        else
        {
            troop.SetActive(false);
            troopPool.Enqueue(troop);
        }

        EnsureLeaderIsValid();
    }

    private void SetLeader(Transform newLeader)
    {
        if (leader == newLeader) return;
        leader = newLeader;

        var camFollow = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;
        if (camFollow)
        {
            camFollow.target = leader;
            camFollow.SendMessage("InitializeOffset", SendMessageOptions.DontRequireReceiver);
        }

        OnLeaderChanged?.Invoke(leader);
    }

    private void EnsureLeaderIsValid()
    {
        if (leader != null && leader.gameObject.activeInHierarchy) return;

        Transform next = null;
        for (int i = 0; i < activeTroops.Count; i++)
        {
            var go = activeTroops[i];
            if (!go || !go.activeInHierarchy) continue;

            var u = go.GetComponent<TroopUnit>();
            if (u != null && u.IsDying) continue;

            next = go.transform;
            break;
        }

        SetLeader(next);
    }

    Vector3 GetOffset(int index)
    {
        // Vogel spiral (elliptical)
        float angleIncrement = 137.5f;
        float a = 0.5f;
        float b = 0.25f;

        float angle = index * angleIncrement * Mathf.Deg2Rad;
        float radiusX = a * Mathf.Sqrt(index);
        float radiusZ = b * Mathf.Sqrt(index);

        float x = Mathf.Cos(angle) * radiusX;
        float z = Mathf.Sin(angle) * radiusZ;

        return new Vector3(x, 0, z);
    }

    IEnumerator FlashTroop(GameObject troop)
    {
        Vector3 original = troop.transform.localScale;
        troop.transform.localScale = original * 1.5f;
        yield return new WaitForSeconds(0.2f);
        troop.transform.localScale = original;
    }

    // --- Combat scanner: enemies ahead? then pause; else resume --------------

    void ScanForEngagement()
    {
        EnsureLeaderIsValid();
        if (leader == null)
        {
            if (combatEngaged)
            {
                combatEngaged = false;
                OnCombatStateChanged?.Invoke(false);
            }
            return;
        }

        Vector3 center = leader.position + new Vector3(0f, 0f, aheadBiasZ);
        Collider[] hits = (enemyMask.value == 0)
            ? Physics.OverlapSphere(center, engageRadius)
            : Physics.OverlapSphere(center, engageRadius, enemyMask);

        bool anyAhead = false;
        if (hits != null && hits.Length > 0)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var eb = hits[i].GetComponentInParent<EnemyBase>();
                if (!eb || !eb.IsAlive || !hits[i].gameObject.activeInHierarchy) continue; // ignore dead

                if (eb.transform.position.z >= leader.position.z)
                {
                    anyAhead = true;
                    break;
                }
            }
        }

        if (anyAhead != combatEngaged)
        {
            combatEngaged = anyAhead;
            OnCombatStateChanged?.Invoke(combatEngaged);
        }
    }

    public List<Vector3> GetOffsets()
    {
        var offsets = new List<Vector3>(activeTroops.Count);
        for (int i = 0; i < activeTroops.Count; i++)
        {
            offsets.Add(GetOffset(i));
        }
        return offsets;
    }
}
