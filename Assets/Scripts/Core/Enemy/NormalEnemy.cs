using UnityEngine;

public class NormalEnemy : EnemyBase
{
#if UNITY_EDITOR
    // Helpful defaults when first add this component
    private void Reset()
    {
        damagePerHit = 1;
        attackRange = 1.5f;
        attackInterval = 1.0f;

        // Animator bindings for your zombie
        // (Change if parameter or clip names differ)
        // attackClipName is serialized in EnemyBase; setdefault here:
        var so = new UnityEditor.SerializedObject(this);
        so.FindProperty("attackClipName").stringValue = "ZombieAttack";
        so.FindProperty("attackSfx").enumValueIndex = (int)SfxId.ZombieAttack;
        so.FindProperty("dieSfx").enumValueIndex = (int)SfxId.ZombieDie;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
#endif
}
