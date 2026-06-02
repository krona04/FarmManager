using UnityEngine;
using UnityEngine.InputSystem;

public class CameraLook : MonoBehaviour
{
    [Header("Mouse Settings")]
    public float mouseSensitivity = 10f;
    public float verticalClamp = 80f;

    private float _xRotation = 0f;
    private Transform _playerBody;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        _playerBody = transform.parent;
        mouseSensitivity = SettingsManager.MouseSensitivity;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        HandleMouseLook();
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
}