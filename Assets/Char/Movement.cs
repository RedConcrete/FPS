using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Model;

public class Movement : MonoBehaviour
{
    private CharacterController characterController;

    private Inputs inputs;
    public Vector2 input_Movement;
    public Vector2 input_View;


    private Vector3 camera_Rotation;
    private Vector3 char_Rotation;


    [Header("ref")]
    public Transform cameraHolder;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float viewClampYMin = -70;
    public float viewClampYMax = 80;

    [Header("Garvity")]
    public float g;
    public float gMin;
    private float playerG;

    public Vector3 jumpForce;
    private Vector3 jumpForceV;

    [Header("Stancs")]
    public PlayerStance playerStance;
    public float playerStanceSmooth;
    
    public CharStance playerStandStance;
    public CharStance playerCrouchStance;
    public CharStance playerProneStance;
    
    private float cameraHeight;
    private float cameraHeightV;

    private Vector3 stanceCapsulCenter;
    private Vector3 stanceCapsulCenterVelocity;

    private float stanceCapsuleHeight;
    private float stanceCapsuleHeightVelocity;

    public void Awake()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        inputs = new Inputs();

        inputs.Char.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        inputs.Char.View.performed += e => input_View = e.ReadValue<Vector2>();
        inputs.Char.Jump.performed += e => Jump();

        inputs.Enable();

        camera_Rotation = cameraHolder.localRotation.eulerAngles;
        char_Rotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localPosition.y;
    }

    private void Update()
    {
        CalcView();
        CalcMovement();
        CalcJump();
        CalcStance();
    }

    private void CalcView()
    {
        char_Rotation.y += playerSettings.ViewXSen * (playerSettings.ViewYInverted ? -input_View.x : input_View.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(char_Rotation);

        camera_Rotation.x += playerSettings.ViewYSen * (playerSettings.ViewYInverted ? input_View.y : -input_View.y)  * Time.deltaTime;
        camera_Rotation.x = Mathf.Clamp(camera_Rotation.x, viewClampYMin, viewClampYMax);

        cameraHolder.localRotation = Quaternion.Euler(camera_Rotation);

    }

    private void CalcMovement()
    {

        var verticalSpeed = playerSettings.walkingSpeed * input_Movement.y * Time.deltaTime;
        var horizontalSpeed = playerSettings.walkingStrafeSpeed * input_Movement.x * Time.deltaTime;

        var movemnetSpeed = new Vector3(horizontalSpeed, 0, verticalSpeed);

        movemnetSpeed = transform.TransformDirection(movemnetSpeed);

        if (playerG > gMin)
        {
            playerG -= g * Time.deltaTime;
        }
       
        if (playerG < -0.1f && characterController.isGrounded)
        {
            playerG = -0.1f;
        }

     

        movemnetSpeed.y += playerG;

        movemnetSpeed += jumpForce * Time.deltaTime;

        characterController.Move(movemnetSpeed);
    }

    private void CalcJump()
    {
        jumpForce = Vector3.SmoothDamp(jumpForce, Vector3.zero, ref jumpForceV, playerSettings.JumpingFallDown);

    }

    private void Jump()
    {
        if (characterController.isGrounded)
        {
            jumpForce = Vector3.up * playerSettings.JumpingHight;
            playerG = 0;
        }
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
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x ,cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, curenntStance.capsuleCollider.height , ref stanceCapsuleHeightVelocity, playerStanceSmooth);
        characterController.center = Vector3.SmoothDamp(characterController.center, curenntStance.capsuleCollider.center, ref stanceCapsulCenterVelocity, playerStanceSmooth);



    }
}
