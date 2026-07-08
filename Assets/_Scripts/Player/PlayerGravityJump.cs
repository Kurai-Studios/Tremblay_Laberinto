using UnityEngine;

public class PlayerGravityJump : MonoBehaviour
{
    private PlayerConfig config;

    public float VerticalVelocity { get; private set; }

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpConsumed;

    public void Init(PlayerConfig cfg)
    {
        config = cfg;
    }

    public void OnJumpRequested()
    {
        jumpBufferTimer = config.jumpBufferTime;
    }

    public void Tick(bool isGrounded)
    {
        if (isGrounded && VerticalVelocity < 0f)
        {
            VerticalVelocity = -2f;
            coyoteTimer = config.coyoteTime;
            jumpConsumed = false;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !jumpConsumed)
        {
            VerticalVelocity = Mathf.Sqrt(config.jumpHeight * -2f * config.gravity);
            jumpBufferTimer = 0f;
            jumpConsumed = true;
            coyoteTimer = 0f;
        }

        float gravityThisFrame = config.gravity;
        if (VerticalVelocity < 0f)
            gravityThisFrame *= config.fallMultiplier;

        VerticalVelocity += gravityThisFrame * Time.deltaTime;
    }

    public void ResetVerticalVelocity()
    {
        VerticalVelocity = 0f;
    }
}
