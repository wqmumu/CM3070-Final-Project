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

        for (int i = 1; i < troops.Count; i++) // Skip leader at index 0
        {
            if (troops[i] != null)
            {
                Vector3 targetPos = lead.position + offsets[i];

                // Clamp the X axis to prevent wall clipping
                float clampedX = Mathf.Clamp(targetPos.x, leftBound, rightBound);
                Vector3 clampedPos = new Vector3(clampedX, targetPos.y, targetPos.z);

                troops[i].transform.position = Vector3.Lerp(
                    troops[i].transform.position, clampedPos, Time.deltaTime * 10f
                );
            }
        }
    }

}
