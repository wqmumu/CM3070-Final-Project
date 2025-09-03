using System.Collections.Generic;
using UnityEngine;

public class TroopFollowerUpdater : MonoBehaviour
{
    public TroopManager manager;
    public float leftBound = -13f;
    public float rightBound = 13f;

    void LateUpdate()
    {
        if (manager == null) return;

        Transform lead = manager.GetLeadTroop();
        if (lead == null) return;

        List<GameObject> troops = manager.GetActiveTroops();
        List<Vector3> offsets = manager.GetOffsets();

        // Skip leader at index 0
        for (int i = 1; i < troops.Count; i++)
        {
            var t = troops[i];
            if (!t) continue;

            // Extra safety: never move a dying unit (in case it temporarily remains referenced)
            var unit = t.GetComponent<TroopUnit>();
            if (unit != null && unit.IsDying) continue;

            Vector3 targetPos = lead.position + offsets[i];

            // Clamp X so units don't clip walls
            float clampedX = Mathf.Clamp(targetPos.x, leftBound, rightBound);
            Vector3 clampedPos = new Vector3(clampedX, targetPos.y, targetPos.z);

            t.transform.position = Vector3.Lerp(
                t.transform.position, clampedPos, Time.deltaTime * 10f
            );
        }
    }
}
