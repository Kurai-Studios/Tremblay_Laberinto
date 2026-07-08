using UnityEngine;

public class LedgeAssist : MonoBehaviour
{
    private CharacterController controller;
    private PlayerConfig config;
    private PlayerStateMachine stateMachine;

    private Vector3 wallNormal;
    private bool hitWallThisFrame;

    public void Init(CharacterController cc, PlayerConfig cfg)
    {
        controller = cc;
        config = cfg;
        stateMachine = GetComponent<PlayerStateMachine>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (stateMachine.CurrentState != PlayerState.Air) return;
        if (hit.normal.y > 0.3f) return;

        wallNormal = hit.normal;
        hitWallThisFrame = true;
    }

    public Vector3 CancelWallVelocity(Vector3 moveVector)
    {
        if (!hitWallThisFrame)
            return moveVector;

        hitWallThisFrame = false;

        float dot = Vector3.Dot(moveVector, wallNormal);
        if (dot < 0f)
            moveVector -= wallNormal * dot;

        return moveVector;
    }
}
