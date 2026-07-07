using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player Config", fileName = "PlayerConfig", order = 0)]
public class PlayerConfig : ScriptableObject
{
    [Header("Locomotion")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float crouchSpeed = 1.5f;
    public float rotationSmoothTime = 0.12f;
    public float speedSmoothTime = 0.1f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float fallMultiplier = 2.5f;
    public float coyoteTime = 0.15f;
    public float jumpBufferTime = 0.2f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayers = ~0;

    [Header("Crouch")]
    public float standingHeight = 2f;
    public float crouchHeight = 1.2f;
    public float crouchCenterY = 0.6f;
    public float standingCenterY = 1f;

    [Header("Slopes")]
    public float slopeSlideSpeed = 5f;

    [Header("Ladder")]
    public float ladderClimbSpeed = 2f;
    public float ladderFacingOffset = 180f;
}
