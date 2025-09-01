using UnityEngine;
using TMPro;

public class TroopCounterUI : MonoBehaviour
{
    public TextMeshProUGUI counterText;
    private TroopManager manager;

    void Start()
    {
        manager = FindFirstObjectByType<TroopManager>();
    }

    void Update()
    {
        counterText.text = "Troops: " + manager.GetTroopCount();
    }
}
