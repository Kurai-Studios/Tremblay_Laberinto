using UnityEngine;

public class PlayerLocomotion : MonoBehaviour
{
    private CharacterController controller;
    private PlayerConfig config;
    private Transform cameraTransform;

    private float currentSpeed;
    private float speedVelocity;
    private float rotationVelocity;

    public float NormalizedSpeed { get; private set; }
    public float CurrentSpeed => currentSpeed;

    public void Init(CharacterController cc, PlayerConfig cfg, Transform camTransform)
    {
        controller = cc;
        config = cfg;
        cameraTransform = camTransform;
    }

    public Vector3 CalculateMovement(Vector2 input, bool sprint, bool crouch, float verticalVelocity)
    {
        if (input.sqrMagnitude < 0.01f)
        {
            currentSpeed = Mathf.SmoothDamp(currentSpeed, 0f, ref speedVelocity, config.speedSmoothTime);
            NormalizedSpeed = Mathf.Clamp01(currentSpeed / config.sprintSpeed);
            return Vector3.up * verticalVelocity;
        }

        float targetSpeed = crouch ? config.crouchSpeed :
                            sprint ? config.sprintSpeed :
                                     config.walkSpeed;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, config.speedSmoothTime);
        NormalizedSpeed = Mathf.Clamp01(currentSpeed / config.sprintSpeed);

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        float smoothAngle = Mathf.SmoothDampAngle(
            transform.eulerAngles.y, targetAngle,
            ref rotationVelocity, config.rotationSmoothTime
        );
        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

        return moveDirection * currentSpeed + Vector3.up * verticalVelocity;
    }

    public Vector3 CalculateSlopeSlide(Vector3 normal)
    {
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, normal).normalized;
        return slideDirection * config.slopeSlideSpeed;
    }
}
