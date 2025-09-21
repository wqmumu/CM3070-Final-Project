using UnityEngine;
using UnityEngine.EventSystems; 

public class TroopMovement : MonoBehaviour
{
    public float speed = 5f;
    public float MouseMoveSpeed = 5f;

    private Camera mainCamera;
    private bool isLeader = false;
    private bool _paused = false;
    private TroopUnit unit;

    public void SetAsLeader(bool status) { isLeader = status; }
    public void SetMovementPaused(bool paused) { _paused = paused; }

    void Awake() { unit = GetComponent<TroopUnit>(); }
    void OnEnable() { TroopManager.OnCombatStateChanged += HandleCombatState; }
    void OnDisable() { TroopManager.OnCombatStateChanged -= HandleCombatState; }
    void HandleCombatState(bool engaged) { _paused = engaged; }

    void Start() { mainCamera = Camera.main; }

    void Update()
    {
        // stop if dying
        if (unit != null && unit.IsDying) return;

        // only leader reads input
        if (isLeader) HandleLateralInput();

        // forward movement if not paused
        if (_paused) return;
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void HandleLateralInput()
    {
        if (mainCamera == null) return;

        // 1) Skip if pointer is over any UI
        if (IsPointerOverUI()) return;

        // 2) Skip if game globally paused
        if (Time.timeScale == 0f) return;

        if (!Input.GetMouseButton(0)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float clampedX = Mathf.Clamp(hit.point.x, -13f, 13f);
            Vector3 newPos = new Vector3(clampedX, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * MouseMoveSpeed);
        }
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // mouse
        if (EventSystem.current.IsPointerOverGameObject()) return true;

        // touches
        for (int i = 0; i < Input.touchCount; i++)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        }
        return false;
    }
}
