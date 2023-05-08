using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Model;


public class WeponController : MonoBehaviour
{

    private Movement charController;

    [Header("References")]
    public Animator wepaonAnimator;
    public GameObject bullet;
    public Transform BulletSpawn;

    [Header("Settings")]
    public WeponSettingsModel weponSettingsModel;

    [Header("Shooting")]
    public float rateOfFire;
    private float currentFireRate;
    public List<WaeponFireType> allowedFireTypes;
    public WaeponFireType currentFireType;
    [HideInInspector]
    public bool isShooting;
    public float shootForce;

    bool isInitialize;

    Vector3 newWeaponRot;
    Vector3 newWeaponRotV;

    Vector3 targetWeaponRot;
    Vector3 targetWeaponRotV;

    Vector3 newWeaponMovRot;
    Vector3 newWeaponMovRotV;

    Vector3 targetWeaponMovRot;
    Vector3 targetWeaponMovRotV;


    private void Start()
    {
        newWeaponRot = transform.localRotation.eulerAngles;
        currentFireType = allowedFireTypes.First();
    }


    public void Initialize(Movement Movement)
    {
        charController = Movement;
        isInitialize = true;
    }

    private void Update()
    {
        if (!isInitialize)
        {
            return;
        }

        CalcWeaponRot();
        SetWeaponAnim();
        CalcShooting();
    }

    private void CalcWeaponRot()
    {
        wepaonAnimator.speed = charController.weaponAnimSpeed;

        newWeaponRot.y += weponSettingsModel.swayAmount * (weponSettingsModel.swayXInverted ? -charController.input_View.x : charController.input_View.x) * Time.deltaTime;
        newWeaponRot.x += weponSettingsModel.swayAmount * (weponSettingsModel.swayYInverted ? charController.input_View.y : -charController.input_View.y) * Time.deltaTime;

        targetWeaponRot.x = Mathf.Clamp(targetWeaponRot.x, -weponSettingsModel.swayClampX, weponSettingsModel.swayClampX);
        targetWeaponRot.y = Mathf.Clamp(targetWeaponRot.y, -weponSettingsModel.swayClampY, weponSettingsModel.swayClampY);
        targetWeaponRot.z = targetWeaponRot.y;

        targetWeaponRot = Vector3.SmoothDamp(targetWeaponRot, Vector3.zero, ref targetWeaponRotV, weponSettingsModel.swayResetSmothing);
        newWeaponRot = Vector3.SmoothDamp(newWeaponRot, targetWeaponRot, ref newWeaponRotV, weponSettingsModel.swaySmothing);

        targetWeaponMovRot.z = weponSettingsModel.movSwayX * (weponSettingsModel.movSwayXInverted ? -charController.input_Movement.x : charController.input_Movement.x);
        targetWeaponMovRot.x = weponSettingsModel.movSwayY * (weponSettingsModel.movSwayYInverted ? -charController.input_Movement.y : charController.input_Movement.y);

        targetWeaponMovRot = Vector3.SmoothDamp(targetWeaponMovRot, Vector3.zero, ref targetWeaponMovRotV, weponSettingsModel.movSwaySmothing);
        newWeaponMovRot = Vector3.SmoothDamp(newWeaponMovRot, targetWeaponMovRot, ref newWeaponMovRotV, weponSettingsModel.movSwaySmothing);

        transform.localRotation = Quaternion.Euler(newWeaponRot + newWeaponMovRot);
    }

    private void SetWeaponAnim()
    {
        wepaonAnimator.SetBool("isSprinting", charController.isSprint);
    }

    private void Shoot()
    {
        GameObject bulletInstance = Instantiate(bullet, BulletSpawn);
        Rigidbody bulletRigidbody = bulletInstance.GetComponent<Rigidbody>();
        bulletRigidbody.AddForce(transform.forward * shootForce, ForceMode.Impulse);
        bulletInstance.transform.parent = null;

    }

    private void CalcShooting()
    {
        if (isShooting)
        {
            Shoot();

            if (currentFireType == WaeponFireType.Semi)
            {
                isShooting = false;
            }
        }
    }

}
