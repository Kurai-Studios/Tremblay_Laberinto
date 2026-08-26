using UnityEngine;

public class LadderController : MonoBehaviour
{
    private CharacterController controller;
    private PlayerConfig config;
    private LadderZone currentLadder;

    private Quaternion targetRotation;
    private const float ATTACH_ROTATION_SPEED = 720f;

    public bool IsOnLadder => currentLadder != null;

    public void Init(CharacterController cc, PlayerConfig cfg)
    {
        controller = cc;
        config = cfg;
    }

    public void Tick(float verticalInput)
    {
        if (currentLadder == null) return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, ATTACH_ROTATION_SPEED * Time.deltaTime
        );

        Vector3 climbDirection = currentLadder.ClimbAxis.normalized;
        Vector3 move = climbDirection * verticalInput * config.ladderClimbSpeed;
        controller.Move(move * Time.deltaTime);

        if (verticalInput > 0.1f && ReachedTop())
        {
            Vector3 exitMove = currentLadder.TopExitPoint - transform.position;
            Detach();
            controller.Move(exitMove);
        }
        else if (verticalInput < -0.1f && ReachedBottom())
        {
            Detach();
        }
    }

    public void Attach(LadderZone ladder)
    {
        currentLadder = ladder;

        Vector3 approachPosition = transform.position;

        Vector3 snapPosition = ladder.GetSnapPosition(approachPosition);
        snapPosition.y = transform.position.y;
        transform.position = snapPosition;

        targetRotation = ladder.GetFacingRotation(approachPosition, config.ladderFacingOffset);

        GetComponent<PlayerStateMachine>().EnterLadder();
    }

    public void Detach()
    {
        currentLadder = null;
        GetComponent<PlayerStateMachine>().ExitLadder();
    }

    private bool ReachedTop()
    {
        if (currentLadder == null) return false;
        return transform.position.y >= currentLadder.TopPoint.y - 0.1f;
    }

    private bool ReachedBottom()
    {
        if (currentLadder == null) return false;
        return transform.position.y <= currentLadder.BottomPoint.y + 0.1f;
    }
}
