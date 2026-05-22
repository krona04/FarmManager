using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.5f;
    public LayerMask groundMask;

    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;

    private Vector2 _moveInput;
    private bool _jumpPressed;
    private bool _isRunning;

    void Start()
    {
        _controller = GetComponent<CharacterController>();

        if (groundCheck == null)
        {
            GameObject gc = new GameObject("GroundCheck");
            gc.transform.SetParent(transform);
            gc.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = gc.transform;
        }
    }

    void Update()
    {
        ReadInput();
        HandleGroundCheck();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void ReadInput()
    {
        if (FarmPlot.IsPlantingQteActive)
        {
            _moveInput = Vector2.zero;
            _isRunning = false;
            _jumpPressed = false;
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        _moveInput = Vector2.zero;
        if (keyboard.wKey.isPressed) _moveInput.y += 1f;
        if (keyboard.sKey.isPressed) _moveInput.y -= 1f;
        if (keyboard.aKey.isPressed) _moveInput.x -= 1f;
        if (keyboard.dKey.isPressed) _moveInput.x += 1f;

        _isRunning = keyboard.leftShiftKey.isPressed;
        _jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
    }

    void HandleGroundCheck()
    {
        if (groundMask == 0)
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance);
        else
            _isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (_isGrounded && _velocity.y < 0)
            _velocity.y = -2f;
    }

    void HandleMovement()
    {
        Vector3 move = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        if (move.magnitude > 1f)
            move.Normalize();

        float baseSpeed = _isRunning ? runSpeed : walkSpeed;
        float speedModifier = TalentManager.Instance.GetTalentModifier(TalentType.MovementSpeed);
        float finalSpeed = baseSpeed * speedModifier;

        _controller.Move(move * finalSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (_jumpPressed && _isGrounded)
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    void ApplyGravity()
    {
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }
}