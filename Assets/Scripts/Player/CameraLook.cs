using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 0.15f;
    public float verticalClamp = 80f;

    private float _xRotation = 0f;
    private Transform _playerBody;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _playerBody = transform.parent;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCursorToggle();
    }

    void HandleMouseLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue() * (mouseSensitivity * 0.01f);

        _xRotation -= mouseDelta.y;
        _xRotation = Mathf.Clamp(_xRotation, -verticalClamp, verticalClamp);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        _playerBody.Rotate(Vector3.up * mouseDelta.x);
    }

    void HandleCursorToggle()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        var mouse = Mouse.current;
        if (mouse != null &&
            mouse.leftButton.wasPressedThisFrame &&
            Cursor.lockState == CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}