using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UI;
using static Model;

public class Movement : MonoBehaviour
{
    [SerializeField] private Transform debugHitPointTransform;
    [SerializeField] private Transform hookShotTransform;

    private CharacterController characterController;

    private Inputs inputs;
    [HideInInspector]
    public Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;

    private Vector3 camera_Rotation;
    private Vector3 char_Rotation;

    [Header("References")]
    public Transform cameraHolder;
    public Transform feetTransfrom;
    public Transform playerCam;
    public TextMeshProUGUI dashTimeTextField;
    public TextMeshProUGUI hookTimeTextField;
    public GameObject hookIndicator;
    public GameObject rig;
    private Animator animator;


    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -70;
    public float viewClampYMax = 80;
    public LayerMask playerMask;

    [Header("Garvity")]
    public float g;
    public float gMin;
    private float playerG;

    public Vector3 jumpForce;
    private Vector3 jumpForceV;
    private int jumpCount;

    [Header("Stancs")]
    public PlayerStance playerStance;
    public float playerStanceSmooth;

    public CharStance playerStandStance;
    public CharStance playerCrouchStance;
    public CharStance playerProneStance;
    private float stanceCheckErrorMargin = 0.05f;
    private float cameraHeight;
    private float cameraHeightV;

    private Vector3 stanceCapsulCenterVelocity;
    private float stanceCapsuleHeightVelocity;

    [HideInInspector]
    public bool isSprint;

    private Vector3 movementSpeed;
    private Vector3 movementSpeedVelocity;

    private State state;
    private Vector3 hookShotPos;
    private float hookShotSize;

    private CamraFOV camraFOV;
    private const float NORMAL_FOV = 60f;
    private const float HOOKSHOT_FOV = 100f;

    private bool isDashing = false;
    private bool isHooking = false;

    private RaycastHit hit;

    [Header("Weapon")]
    public WeponController weponController;
    public float weaponAnimSpeed;

    private enum State
    {
        Normal,
        HookShot,
        HookShotThrown,
        Dash,
        Climb,
    }


    public void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        inputs = new Inputs();

        inputs.Char.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        inputs.Char.View.performed += e => input_View = e.ReadValue<Vector2>();
        inputs.Char.Jump.performed += e => Jump();

        inputs.Char.Crouch.performed += e => Crouch();
        inputs.Char.Prone.performed += e => Prone();

        inputs.Char.Sprint.performed += e => ToggelSprint();

        inputs.Char.Dash.started += e => Dash();

        inputs.Char.Climb.performed += e => StartClimb();

        inputs.Weapon.FirePressed.performed += e => ShootingPressed();
        inputs.Weapon.FireReleased.performed += e => ShootingReleased();

        inputs.Enable();

        camera_Rotation = cameraHolder.localRotation.eulerAngles;
        char_Rotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();
        animator = rig.gameObject.GetComponent<Animator>();

        cameraHeight = cameraHolder.localPosition.y;

        state = State.Normal;
        hookShotTransform.gameObject.SetActive(false);

        camraFOV = playerCam.GetComponent<CamraFOV>();

        jumpCount = 0;

        if (weponController)
        {
            weponController.Initialize(this);
        }
    }

    private void Update()
    {

        switch (state)
        {
            default:
            case State.Normal:
                CalcView();
                CalcMovement();
                CalcJump();
                CalcStance();
                StartGrapple();
                break;
            case State.HookShot:
                CalcView();
                HandelHookshotMovement();
                break;
            case State.HookShotThrown:
                CalcView();
                CalcMovement();
                CalcJump();
                CalcStance();
                HandelHookshotThorwn();
                break;
            case State.Climb:
                CalcView();
                Climb();
                break;
        }

        if (playerSettings.hookCooldownTimer > 0f)
        {
            hookTimeTextField.text = Mathf.Floor(playerSettings.hookCooldownTimer * 10f) / 10f + "s";
            playerSettings.hookCooldownTimer -= Time.deltaTime;
        }

        if (playerSettings.dashCooldownTimer > 0f)
        {
            dashTimeTextField.text = Mathf.Floor(playerSettings.dashCooldownTimer * 10f) / 10f + "s";
            playerSettings.dashCooldownTimer -= Time.deltaTime;
        }

        if (input_Movement.y > 0.2f && characterController.isGrounded)
        {
            if (isSprint)
            {
                animator.SetBool("isRunning", true);
            }
            else
            {
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
        }

        if (!characterController.isGrounded)
        {
            animator.SetBool("isJumping", true);
        }
        else
        {
            animator.SetBool("isJumping", false);
        }

    }

    private void ShootingPressed()
    {
        if (weponController)
        {
            weponController.isShooting = true;
        }
    }
    private void ShootingReleased()
    {

    }


    private void CalcView()
    {
        char_Rotation.y += playerSettings.ViewXSen * (playerSettings.ViewYInverted ? -input_View.x : input_View.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(char_Rotation);

        camera_Rotation.x += playerSettings.ViewYSen * (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
        camera_Rotation.x = Mathf.Clamp(camera_Rotation.x, viewClampYMin, viewClampYMax);

        cameraHolder.localRotation = Quaternion.Euler(camera_Rotation);

    }

    private void CalcMovement()
    {

        if (input_Movement.y <= 0.2f)
        {
            isSprint = false;
        }

        var verticalSpeed = playerSettings.walkingSpeed;
        var horizontalSpeed = playerSettings.walkingStrafeSpeed;

        if (isSprint)
        {
            verticalSpeed = playerSettings.runningForwardSpeed;
            horizontalSpeed = playerSettings.runningStrafeSpeed;
        }

        if (!characterController.isGrounded)
        {
            playerSettings.speedEffect = playerSettings.fallingSpeedEffect;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.speedEffect = playerSettings.crouchSpeedEffect;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            playerSettings.speedEffect = playerSettings.proneSpeedEffect;
        }
        else
        {
            playerSettings.speedEffect = 1;
        }

        weaponAnimSpeed = characterController.velocity.magnitude / (playerSettings.walkingSpeed * playerSettings.speedEffect);
        
        if (weaponAnimSpeed > 1)
        {
            weaponAnimSpeed = 1;
        }

        verticalSpeed *= playerSettings.speedEffect;
        horizontalSpeed *= playerSettings.speedEffect;


        movementSpeed = Vector3.SmoothDamp(movementSpeed,
            new Vector3(horizontalSpeed * input_Movement.x * Time.deltaTime, 0, verticalSpeed * input_Movement.y * Time.deltaTime),
            ref movementSpeedVelocity,
            characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeedNotSmoothed = transform.TransformDirection(movementSpeed);

        if (playerG > gMin)
        {
            playerG -= g * Time.deltaTime;
        }

        if (playerG < -0.1f && characterController.isGrounded)
        {
            playerG = -0.1f;
        }

        movementSpeedNotSmoothed.y += playerG;
        movementSpeedNotSmoothed += jumpForce * Time.deltaTime;

        characterController.Move(movementSpeedNotSmoothed);
    }

    private void CalcJump()
    {
        jumpForce = Vector3.SmoothDamp(jumpForce, Vector3.zero, ref jumpForceV, playerSettings.JumpingFallDown);
    }

    private void CalcStance()
    {
        var curenntStance = playerStandStance;

        if (playerStance == PlayerStance.Crouch)
        {
            curenntStance = playerCrouchStance;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            curenntStance = playerProneStance;
        }

        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, curenntStance.CameraHeight, ref cameraHeightV, playerStanceSmooth);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, curenntStance.capsuleCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmooth);
        characterController.center = Vector3.SmoothDamp(characterController.center, curenntStance.capsuleCollider.center, ref stanceCapsulCenterVelocity, playerStanceSmooth);

    }

    private float CalcCloodownTimer(float cooldown)
    {
        if (cooldown > 0f)
        {
            hookTimeTextField.text = Mathf.Floor(cooldown * 10f) / 10f + "s";
            cooldown -= Time.deltaTime;
            return cooldown;
        }
        return cooldown;

    }

    private void Dash()
    {

        if (playerSettings.dashCooldownTimer <= 0f && !isDashing)
        {
            isDashing = true;
            playerSettings.dashTimer = playerSettings.dashDuration;
            state = State.Dash;
        }

        while (isDashing)
        {
            Vector3 moveDirection = transform.forward;
            characterController.Move(moveDirection * playerSettings.dashForce * playerSettings.dashSpeedMultiplier * Time.deltaTime);
            characterController.Move(moveDirection * Time.deltaTime);
            playerSettings.dashTimer -= Time.deltaTime;
            if (playerSettings.dashTimer <= 0f)
            {
                isDashing = false;
                playerSettings.dashCooldownTimer = playerSettings.dashCooldown;
                state = State.Normal;
            }
        }

    }

    private void Jump()
    {

        if (characterController.isGrounded)
        {
            jumpForce = Vector3.up * playerSettings.JumpingHight;
            playerG = 0;
            jumpCount = 0;
        }
        else if (jumpCount <= 0)
        {
            jumpForce = Vector3.up * playerSettings.JumpingHight;
            playerG = 0;
            jumpCount += 1;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            return;
        }

        if (playerStance == PlayerStance.Crouch)
        {
            return;
        }
    }

    private void Climb()
    {

        if (Physics.Raycast(transform.position, transform.forward, out hit, playerSettings.climbRange))
        {

            if (hit.collider.gameObject.tag == "Climbable")
            {
                float climbDirection = Input.GetAxis("Vertical");
                float climbAmount = climbDirection * playerSettings.climbSpeed * Time.deltaTime;
                transform.Translate(Vector3.up * climbAmount);
            }
            else
            {
                state = State.Normal;
                ResetG();
            }

        }
    }

    private void Crouch()
    {

        if (playerStance == PlayerStance.Crouch)
        {

            if (StanceCheck(playerStandStance.capsuleCollider.height))
            {
                return;
            }

            playerStance = PlayerStance.Stand;
            return;
        }

        if (StanceCheck(playerCrouchStance.capsuleCollider.height))
        {
            return;
        }

        playerStance = PlayerStance.Crouch;
    }

    private void Prone()
    {

        if (playerStance == PlayerStance.Prone)
        {
            if (StanceCheck(playerStandStance.capsuleCollider.height))
            {
                return;
            }
            playerStance = PlayerStance.Stand;
            return;
        }

        if (StanceCheck(playerProneStance.capsuleCollider.height))
        {
            return;
        }
        playerStance = PlayerStance.Prone;
    }

    private bool StanceCheck(float stanceCheckHight)
    {

        var start = new Vector3(feetTransfrom.position.x, feetTransfrom.position.y + characterController.radius + stanceCheckErrorMargin, feetTransfrom.position.z);
        var end = new Vector3(feetTransfrom.position.x, feetTransfrom.position.y - characterController.radius - stanceCheckErrorMargin + stanceCheckHight, feetTransfrom.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);

    }

    private void ToggelSprint()
    {
        
        if (input_Movement.y <= 0.2f)
        {
            isSprint = false;
            return;
        }

        isSprint = !isSprint;
    }

    private void StartGrapple()
    {

        if (playerSettings.hookCooldownTimer <= 0f && !isHooking)
        {
            isHooking = true;
            playerSettings.hookTimer = playerSettings.hookDuration;
        }

        if (isHooking)
        {
            if (TestInputDownHookShot())
            {
                if (Physics.Raycast(playerCam.position, playerCam.forward, out RaycastHit raycastHit))
                {
                    debugHitPointTransform.position = raycastHit.point;
                    hookShotPos = raycastHit.point;
                    hookShotSize = 0f;
                    hookShotTransform.gameObject.SetActive(true);
                    hookShotTransform.localScale = Vector3.zero;
                    state = State.HookShotThrown;
                }
            }
        }
    }

    private void StartClimb()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, playerSettings.climbRange))
        {
            if (hit.collider.gameObject.tag != null)
            {
                if (hit.collider.gameObject.tag == "Climbable")
                {
                    state = State.Climb;
                }
                else
                {
                    state = State.Normal;
                    ResetG();
                }
                Debug.Log("Tag: " + hit.collider.gameObject.tag);
            }
            else
            {
                state = State.Normal;
                ResetG();
            }
        }
    }


    private void HandelHookshotThorwn()
    {
        hookShotTransform.LookAt(hookShotPos);

        hookShotSize += playerSettings.hookShotThorwSpeed * Time.deltaTime;
        hookShotTransform.localScale = new Vector3(1, 1, hookShotSize);

        if (playerSettings.hookShotMaxDist >= Vector3.Distance(transform.position, hookShotPos))
        {
            hookIndicator.gameObject.SetActive(false);
            if (hookShotSize >= Vector3.Distance(transform.position, hookShotPos))
            {
                state = State.HookShot;
                camraFOV.SetCameraFov(HOOKSHOT_FOV);
            }
        }
        else
        {
            hookIndicator.gameObject.SetActive(true);
            state = State.Normal;
            hookShotTransform.gameObject.SetActive(false);
        }

    }

    private void HandelHookshotMovement()
    {
        hookShotTransform.LookAt(hookShotPos);
        Vector3 hookShotDir = (hookShotPos - transform.position).normalized;

        float hookShotSpeed = Mathf.Clamp(Vector3.Distance(transform.position, hookShotPos), playerSettings.hookShotSpeedMin, playerSettings.hookShotSpeedMax);

        characterController.Move(hookShotDir * hookShotSpeed * playerSettings.hookShotSpeedMultiplier * Time.deltaTime);

        if (Vector3.Distance(transform.position, hookShotPos) < playerSettings.reachedHookshotPositionDistance)
        {
            StopHookShot();
        }

        if (TestInputDownHookShot())
        {
            StopHookShot();
        }

    }

    private void StopHookShot()
    {
        isHooking = false;
        playerSettings.hookCooldownTimer = playerSettings.hookCooldown;
        state = State.Normal;
        ResetG();
        hookShotTransform.gameObject.SetActive(false);
        camraFOV.SetCameraFov(NORMAL_FOV);
    }

    private bool TestInputDownHookShot()
    {
        return Input.GetKeyDown(KeyCode.E);
    }
    private void ResetG()
    {
        playerG = 0;
    }
}
