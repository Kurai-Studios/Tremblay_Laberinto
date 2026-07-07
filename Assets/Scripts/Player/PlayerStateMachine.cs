using UnityEngine;

public enum PlayerState
{
    Locomotion,
    Air,
    Crouch,
    Ladder
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private PlayerConfig config;

    public PlayerState CurrentState { get; private set; } = PlayerState.Locomotion;

    private CharacterController controller;
    private PlayerInputReader input;
    private GroundChecker groundChecker;
    private PlayerLocomotion locomotion;
    private PlayerGravityJump gravityJump;
    private LadderController ladderController;
    private PlayerAnimator playerAnimator;
    private LedgeAssist ledgeAssist;

    private float standingHeight;
    private float standingCenterY;
    private float originalStepOffset;
    private float originalSlopeLimit;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        input = GetComponent<PlayerInputReader>();

        groundChecker = gameObject.AddComponent<GroundChecker>();
        groundChecker.Init(controller, config);

        locomotion = gameObject.AddComponent<PlayerLocomotion>();

        gravityJump = gameObject.AddComponent<PlayerGravityJump>();
        gravityJump.Init(config);

        ladderController = gameObject.AddComponent<LadderController>();
        ladderController.Init(controller, config);

        playerAnimator = gameObject.AddComponent<PlayerAnimator>();

        ledgeAssist = gameObject.AddComponent<LedgeAssist>();
        ledgeAssist.Init(controller, config);

        standingHeight = config.standingHeight;
        standingCenterY = config.standingCenterY;

        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingCenterY, 0f);

        originalStepOffset = controller.stepOffset;
        originalSlopeLimit = controller.slopeLimit;
        input.OnJumpPressed += HandleJump;
    }

    void Start()
    {
        Transform camTransform = Camera.main != null ? Camera.main.transform : null;
        locomotion.Init(controller, config, camTransform);

        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null)
            playerAnimator.Init(animator);
    }

    void Update()
    {
        bool feetOnGround = controller.isGrounded;
        controller.stepOffset = feetOnGround ? originalStepOffset : 0f;
        controller.slopeLimit = feetOnGround ? originalSlopeLimit : 90f;

        groundChecker.Tick();

        switch (CurrentState)
        {
            case PlayerState.Locomotion:
                UpdateLocomotion();
                break;
            case PlayerState.Air:
                UpdateAir();
                break;
            case PlayerState.Crouch:
                UpdateCrouch();
                break;
            case PlayerState.Ladder:
                UpdateLadder();
                break;
        }

        UpdateAnimator();
    }

    // --- Locomotion ---

    private void UpdateLocomotion()
    {
        if (!groundChecker.IsGrounded)
        {
            TransitionTo(PlayerState.Air);
            return;
        }

        if (input.CrouchHeld)
        {
            EnterCrouch();
            return;
        }

        if (groundChecker.IsOnSteepSlope(controller.slopeLimit))
        {
            Vector3 slide = locomotion.CalculateSlopeSlide(groundChecker.GroundNormal);
            controller.Move(slide * Time.deltaTime);
            return;
        }

        gravityJump.Tick(true);
        Vector3 move = locomotion.CalculateMovement(
            input.MoveInput, input.SprintHeld, false,
            gravityJump.VerticalVelocity
        );
        controller.Move(move * Time.deltaTime);
    }

    // --- Air ---

    private void UpdateAir()
    {
        if (groundChecker.IsGrounded)
        {
            TransitionTo(PlayerState.Locomotion);
            return;
        }

        gravityJump.Tick(false);
        Vector3 move = locomotion.CalculateMovement(
            input.MoveInput, false, false,
            gravityJump.VerticalVelocity
        );
        move = ledgeAssist.CancelWallVelocity(move);
        controller.Move(move * Time.deltaTime);
    }

    // --- Crouch ---

    private void EnterCrouch()
    {
        CurrentState = PlayerState.Crouch;
        controller.height = config.crouchHeight;
        controller.center = new Vector3(0f, config.crouchCenterY, 0f);
    }

    private void UpdateCrouch()
    {
        if (!groundChecker.IsGrounded)
        {
            ExitCrouch();
            TransitionTo(PlayerState.Air);
            return;
        }

        if (!input.CrouchHeld && CanStandUp())
        {
            ExitCrouch();
            TransitionTo(PlayerState.Locomotion);
            return;
        }

        gravityJump.Tick(true);
        Vector3 move = locomotion.CalculateMovement(
            input.MoveInput, false, true,
            gravityJump.VerticalVelocity
        );
        controller.Move(move * Time.deltaTime);
    }

    private void ExitCrouch()
    {
        controller.height = standingHeight;
        controller.center = new Vector3(0f, standingCenterY, 0f);
    }

    private bool CanStandUp()
    {
        float checkHeight = standingHeight - config.crouchHeight;
        Vector3 bottom = transform.position + Vector3.up * config.crouchHeight;
        return !Physics.SphereCast(
            bottom, controller.radius * 0.9f,
            Vector3.up, out _, checkHeight,
            config.groundLayers, QueryTriggerInteraction.Ignore
        );
    }

    // --- Ladder ---

    private void UpdateLadder()
    {
        if (!ladderController.IsOnLadder)
        {
            TransitionTo(PlayerState.Locomotion);
            return;
        }

        ladderController.Tick(input.MoveInput.y);
    }

    // --- Public API for LadderController ---

    public void EnterLadder()
    {
        CurrentState = PlayerState.Ladder;
        gravityJump.ResetVerticalVelocity();
    }

    public void ExitLadder()
    {
        TransitionTo(PlayerState.Locomotion);
    }

    // --- Jump ---

    private void HandleJump()
    {
        if (CurrentState == PlayerState.Ladder)
        {
            ladderController.Detach();
            TransitionTo(PlayerState.Air);
            return;
        }

        if (CurrentState == PlayerState.Crouch)
        {
            if (CanStandUp())
            {
                ExitCrouch();
                TransitionTo(PlayerState.Locomotion);
            }
            return;
        }

        gravityJump.OnJumpRequested();
        playerAnimator.TriggerJump();
    }

    // --- Transitions ---

    private void TransitionTo(PlayerState newState)
    {
        CurrentState = newState;
    }

    // --- Animator bridge ---

    private void UpdateAnimator()
    {
        if (playerAnimator == null) return;

        float maxSpeed = CurrentState == PlayerState.Crouch ? config.crouchSpeed : config.sprintSpeed;
        float animSpeed = Mathf.Clamp01(locomotion.CurrentSpeed / maxSpeed);
        playerAnimator.SetSpeed(animSpeed);
        playerAnimator.SetGrounded(groundChecker.IsGrounded);
        playerAnimator.SetVerticalVelocity(gravityJump.VerticalVelocity);
        playerAnimator.SetCrouching(CurrentState == PlayerState.Crouch);
        playerAnimator.SetClimbing(CurrentState == PlayerState.Ladder);
        playerAnimator.SetClimbSpeed(CurrentState == PlayerState.Ladder ? input.MoveInput.y : 0f);
    }

    void OnDestroy()
    {
        if (input != null)
            input.OnJumpPressed -= HandleJump;
    }
}
