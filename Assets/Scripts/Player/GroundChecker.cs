using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    private CharacterController controller;
    private PlayerConfig config;

    private float ungroundedTimer;
    private const float GROUND_GRACE_TIME = 0.1f;
    private const float MAX_GROUND_ANGLE = 60f;

    public bool IsGrounded { get; private set; }
    public Vector3 GroundNormal { get; private set; }

    public void Init(CharacterController cc, PlayerConfig cfg)
    {
        controller = cc;
        config = cfg;
    }

    public void Tick()
    {
        float bottomY = controller.center.y - controller.height * 0.5f;
        Vector3 origin = transform.position + Vector3.up * (bottomY + config.groundCheckRadius + 0.02f);

        bool sphereCheck = Physics.SphereCast(
            origin, config.groundCheckRadius,
            Vector3.down, out RaycastHit hit,
            config.groundCheckRadius + 0.08f,
            config.groundLayers,
            QueryTriggerInteraction.Ignore
        );

        bool validGround = sphereCheck && Vector3.Angle(hit.normal, Vector3.up) < MAX_GROUND_ANGLE;
        bool rawGrounded = validGround || (controller.isGrounded && (controller.collisionFlags & CollisionFlags.Below) != 0);

        if (rawGrounded)
        {
            ungroundedTimer = 0f;
            IsGrounded = true;
        }
        else
        {
            ungroundedTimer += Time.deltaTime;
            IsGrounded = ungroundedTimer < GROUND_GRACE_TIME;
        }

        GroundNormal = validGround ? hit.normal : Vector3.up;
    }

    public bool IsOnSteepSlope(float slopeLimit)
    {
        return IsGrounded && Vector3.Angle(GroundNormal, Vector3.up) > slopeLimit;
    }

    void OnDrawGizmosSelected()
    {
        if (config == null || controller == null) return;
        float bottomY = controller.center.y - controller.height * 0.5f;
        Vector3 origin = transform.position + Vector3.up * (bottomY + config.groundCheckRadius + 0.02f);
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin + Vector3.down * config.groundCheckRadius, config.groundCheckRadius);
    }
}
