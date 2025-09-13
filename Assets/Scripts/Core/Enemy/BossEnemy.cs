using UnityEngine;

public class BossEnemy : EnemyBase
{
#if UNITY_EDITOR
    private void Reset()
    {
        damagePerHit = 10;
        attackRange = 2.5f;
        attackInterval = 2.0f;

        var so = new UnityEditor.SerializedObject(this);
        so.FindProperty("attackClipName").stringValue = "BossAttack";
        so.FindProperty("attackSfx").enumValueIndex = (int)SfxId.BossAttack;
        so.FindProperty("dieSfx").enumValueIndex = (int)SfxId.BossDie;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
#endif
}
