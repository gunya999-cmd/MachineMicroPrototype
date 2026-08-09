using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Part : MonoBehaviour
{
    [Header("Settings")]
    public bool draggable = true;

    [Tooltip("Dynamic — объект физически активен. Kinematic — объект не двигается физикой, но может быть перемещён игроком.")]
    public RigidbodyType2D runBodyType = RigidbodyType2D.Dynamic;

    private Rigidbody2D rb;

    private Vector3 savedPosition;
    private Quaternion savedRotation;

    private Vector3 dragOffset;
    private bool isDragging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        savedPosition = transform.position;
        savedRotation = transform.rotation;

        FreezeEdit();
    }

    public void SaveState()
    {
        savedPosition = transform.position;
        savedRotation = transform.rotation;
    }

    public void EnterRun()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = runBodyType;
    }

    public void FreezeEdit()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Restore()
    {
        transform.position = savedPosition;
        transform.rotation = savedRotation;
        FreezeEdit();
    }

    private void Update()
    {
        if (isDragging && !Input.GetMouseButton(0))
        {
            isDragging = false;
        }
    }

    private void OnMouseDown()
    {
        if (!CanDrag())
        {
            return;
        }

        isDragging = true;

        Vector3 mouseWorld = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorld;
    }

    private void OnMouseDrag()
    {
        if (!CanDrag() || !isDragging)
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorld + dragOffset;

        newPosition.z = 0f;
        transform.position = newPosition;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private bool CanDrag()
    {
        return draggable
            && GameManager.Instance != null
            && GameManager.Instance.CurrentMode == GameManager.GameMode.Edit;
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (Camera.main == null)
        {
            return Vector3.zero;
        }

        Vector3 screenPosition = Input.mousePosition;

        screenPosition.z = -Camera.main.transform.position.z;

        return Camera.main.ScreenToWorldPoint(screenPosition);
    }
}
