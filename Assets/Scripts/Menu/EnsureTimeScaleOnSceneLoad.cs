using UnityEngine;

public class EnsureTimeScaleOnSceneLoad : MonoBehaviour
{
    [SerializeField] private bool forceTimeScaleOne = true;
    private void Awake()
    {
        if (forceTimeScaleOne && Time.timeScale != 1f)
            Time.timeScale = 1f;
    }
}
