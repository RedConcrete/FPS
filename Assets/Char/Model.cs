using System;
using System.Collections.Generic;
using UnityEngine;

public static class Model
{
    #region - Player -

    public enum PlayerStance
    {
        Stand,
        Crouch,
        Prone
    }

    [Serializable]
    public class PlayerSettingsModel
    {
        [Header("View Settings")]

        public float ViewXSen;
        public float ViewYSen;

        public bool ViewXInverted;
        public bool ViewYInverted;

        [Header("Movemnet Settings")]

        public float walkingSpeed;
        public float walkingBackwardSpeed;
        public float walkingStrafeSpeed;

        [Header("Jumping")]

        public float JumpingHight;
        public float JumpingFallDown;


    }

    [Serializable]
    public class CharStance
    {

        public float CameraHeight;
        public CapsuleCollider capsuleCollider;

    }

    #endregion
}
