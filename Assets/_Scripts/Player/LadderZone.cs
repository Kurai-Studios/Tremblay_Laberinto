using UnityEngine;

public class LadderZone : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    // Local nudge applied on top of the real platform anchor when exiting at the top. X = radial
    // (inward, onto the platform's width), Y = up. Deliberately no Z (tangential) component: probed
    // live against actual platform geometry and confirmed the platform extends along the ladder's
    // local X axis, not Z -- a Z offset walks the player sideways off the platform's edge instead
    // of further onto it.
    [SerializeField] private Vector3 topExitOffset = new Vector3(0.6f, 0.4f, 0f);
    [Tooltip("Distance from the ladder's center, along its forward axis, that the player stands while climbing. Must clear the solid collider's half-thickness plus the player's CharacterController radius (and skin width) or the two will overlap and fight each other every Move() call.")]
    [SerializeField] private float climbStandoff = 0.5f;

    // Real-world position of the platform this ladder actually leads to, set once by LadderPlacer
    // right after it instantiates this ladder (it already knows the exact upper-platform anchor it
    // aligned the ladder to). When present, the top exit lands the player there instead of guessing
    // via a fixed offset from this ladder's own TopPoint -- a constant offset can't be right for
    // every ladder, since the real gap between a ladder's top and its platform varies per connection.
    private Vector3 topExitTarget;
    private bool hasTopExitTarget;

    public Vector3 TopPoint => topPoint != null ? topPoint.position : transform.position + Vector3.up * 3f;
    public Vector3 BottomPoint => bottomPoint != null ? bottomPoint.position : transform.position;

    // Where the player should end up after climbing off the top: the real platform anchor (if known)
    // plus a small nudge so they land clear of the edge, rather than exactly on it. Falls back to the
    // old TopPoint-relative guess for ladders LadderPlacer never set a target on (e.g. hand-placed ones).
    public Vector3 TopExitPoint =>
        (hasTopExitTarget ? topExitTarget : TopPoint) + transform.TransformDirection(topExitOffset);

    public Vector3 ClimbAxis => (TopPoint - BottomPoint).normalized;

    public void SetTopExitTarget(Vector3 worldPoint)
    {
        topExitTarget = worldPoint;
        hasTopExitTarget = true;
    }

    // --- IInteractable ---

    public string InteractionPrompt => "Press E to climb";

    public bool CanInteract(GameObject interactor)
    {
        LadderController controller = interactor.GetComponent<LadderController>();
        return controller != null && !controller.IsOnLadder;
    }

    public void Interact(GameObject interactor)
    {
        LadderController controller = interactor.GetComponent<LadderController>();
        if (controller == null) return;

        controller.Attach(this);
    }

    // Which side of the ladder (along its local forward axis) the player is approaching from:
    // +1 if in front of the wide face's normal, -1 if behind it. Used so the player attaches and
    // climbs on whichever side they actually walked up to, instead of a fixed side.
    private float GetApproachSide(Vector3 playerPos)
    {
        float dot = Vector3.Dot(playerPos - transform.position, transform.forward);
        return dot >= 0f ? 1f : -1f;
    }

    public Vector3 GetSnapPosition(Vector3 playerPos)
    {
        float side = GetApproachSide(playerPos);

        Vector3 ladderCenter = transform.position;
        ladderCenter.y = playerPos.y;

        Vector3 offset = transform.forward * (climbStandoff * side);
        return ladderCenter + offset;
    }

    public Quaternion GetFacingRotation(Vector3 playerPos, float facingOffset)
    {
        float side = GetApproachSide(playerPos);
        Vector3 faceDirection = -transform.forward * side;

        return Quaternion.LookRotation(faceDirection) * Quaternion.Euler(0f, facingOffset, 0f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 top = topPoint != null ? topPoint.position : transform.position + Vector3.up * 3f;
        Vector3 bot = bottomPoint != null ? bottomPoint.position : transform.position;
        Gizmos.DrawLine(bot, top);
        Gizmos.DrawSphere(top, 0.15f);
        Gizmos.DrawSphere(bot, 0.15f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(top, TopExitPoint);
        Gizmos.DrawSphere(TopExitPoint, 0.12f);
    }
}
