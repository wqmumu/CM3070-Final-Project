using UnityEngine;

public class TroopMovement : MonoBehaviour
{
    public float speed = 5f;
    public float MouseMoveSpeed = 5f;

    private Camera mainCamera;
    private bool isLeader = false;
    private bool _paused = false;
    private TroopUnit unit;  // <-- cache

    public void SetAsLeader(bool status) { isLeader = status; }
    public void SetMovementPaused(bool paused) { _paused = paused; }

    void Awake() { unit = GetComponent<TroopUnit>(); }
    void OnEnable() { TroopManager.OnCombatStateChanged += HandleCombatState; }
    void OnDisable() { TroopManager.OnCombatStateChanged -= HandleCombatState; }
    void HandleCombatState(bool engaged) { _paused = engaged; }

    void Start() { mainCamera = Camera.main; }

    void Update()
    {
        // if dying, do nothing at all
        if (unit != null && unit.IsDying) return;

        // leader can still be dragged left/right
        if (isLeader) HandleLateralInput();

        // pause only forward motion
        if (_paused) return;

        // forward movement
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void HandleLateralInput()
    {
        if (mainCamera == null) return;
        if (!Input.GetMouseButton(0)) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            float clampedX = Mathf.Clamp(hit.point.x, -13f, 13f);
            Vector3 newPos = new Vector3(clampedX, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * MouseMoveSpeed);
        }
    }
}
