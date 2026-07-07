using UnityEngine;

public class LadderZone : MonoBehaviour
{
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;
    [SerializeField] private Vector3 topExitOffset = new Vector3(0f, 0.5f, 1f);

    public Vector3 TopPoint => topPoint != null ? topPoint.position : transform.position + Vector3.up * 3f;
    public Vector3 BottomPoint => bottomPoint != null ? bottomPoint.position : transform.position;
    public Vector3 TopExitOffset => transform.TransformDirection(topExitOffset);

    public Vector3 ClimbAxis => (TopPoint - BottomPoint).normalized;

    public Vector3 GetSnapPosition(Vector3 playerPos)
    {
        Vector3 ladderCenter = transform.position;
        ladderCenter.y = playerPos.y;

        Vector3 offset = transform.forward * 0.3f;
        return ladderCenter - offset;
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
        Gizmos.DrawRay(top, transform.TransformDirection(topExitOffset));
    }
}
