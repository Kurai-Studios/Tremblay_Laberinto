using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;

    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int HashIsClimbing = Animator.StringToHash("IsClimbing");
    private static readonly int HashClimbSpeed = Animator.StringToHash("ClimbSpeed");
    private static readonly int HashJump = Animator.StringToHash("Jump");

    private bool initialized;

    public void Init(Animator anim)
    {
        animator = anim;
        initialized = animator != null;
    }

    public void SetSpeed(float value)
    {
        if (!initialized) return;
        animator.SetFloat(HashSpeed, value, 0.1f, Time.deltaTime);
    }

    public void SetGrounded(bool value)
    {
        if (!initialized) return;
        animator.SetBool(HashIsGrounded, value);
    }

    public void SetVerticalVelocity(float value)
    {
        if (!initialized) return;
        animator.SetFloat(HashVerticalVelocity, value);
    }

    public void SetCrouching(bool value)
    {
        if (!initialized) return;
        animator.SetBool(HashIsCrouching, value);
    }

    public void SetClimbing(bool value)
    {
        if (!initialized) return;
        animator.SetBool(HashIsClimbing, value);
    }

    public void SetClimbSpeed(float value)
    {
        if (!initialized) return;
        animator.SetFloat(HashClimbSpeed, value);
    }

    public void TriggerJump()
    {
        if (!initialized) return;
        animator.SetTrigger(HashJump);
    }
}
