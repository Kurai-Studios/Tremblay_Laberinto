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
            Vector3 exitOffset = currentLadder.TopExitOffset;
            Detach();
            controller.Move(exitOffset);
        }
        else if (verticalInput < -0.1f && ReachedBottom())
        {
            Detach();
        }
    }

    public void Attach(LadderZone ladder)
    {
        currentLadder = ladder;

        Vector3 snapPosition = ladder.GetSnapPosition(transform.position);
        snapPosition.y = transform.position.y;
        transform.position = snapPosition;

        targetRotation = Quaternion.LookRotation(-ladder.transform.forward) *
                         Quaternion.Euler(0f, config.ladderFacingOffset, 0f);

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

    void OnTriggerEnter(Collider other)
    {
        if (IsOnLadder) return;
        var ladder = other.GetComponent<LadderZone>();
        if (ladder != null)
            Attach(ladder);
    }
}
