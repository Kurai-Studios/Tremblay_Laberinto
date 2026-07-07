using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, PlayerControls.IPlayerActions
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }

    public event Action OnJumpPressed;

    private PlayerControls controls;

    void Awake()
    {
        controls = new PlayerControls();
        controls.Player.SetCallbacks(this);
    }

    void OnEnable() => controls.Player.Enable();
    void OnDisable() => controls.Player.Disable();

    public void EnablePlayerInput() => controls.Player.Enable();
    public void DisablePlayerInput() => controls.Player.Disable();

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        LookInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnJumpPressed?.Invoke();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        SprintHeld = context.ReadValueAsButton();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        CrouchHeld = context.ReadValueAsButton();
    }
}
