using UnityEngine;

namespace FoggyRoad.Driving
{
    public static class ManualTransmissionRules
    {
        public static bool IsForwardGearSpeedValid(float forwardSpeed, float minDriveSpeed, float maxSpeed)
        {
            // A stopped rigidbody can drift a few centimetres backward on a slope.
            // First gear must still be able to pull away from that near-zero roll.
            float launchTolerance = minDriveSpeed <= 0.01f ? 0.5f : 0f;
            return forwardSpeed >= minDriveSpeed - launchTolerance && forwardSpeed < maxSpeed;
        }

        public static float GetDefaultMinimumDriveSpeed(int forwardGearIndex, float maxSpeed)
        {
            return forwardGearIndex switch
            {
                1 => 9.5f,
                2 => 19f,
                3 => 30f,
                _ => maxSpeed * 0.7f
            };
        }

        public static float GetTurnStrength(float forwardSpeed, float parkedTurnStrength, float movingTurnStrength, float fullStrengthSpeed)
        {
            const float minimumTurningSpeed = 0.35f;
            float speed = Mathf.Abs(forwardSpeed);
            if (speed < minimumTurningSpeed)
            {
                return 0f;
            }

            float speedFactor = Mathf.InverseLerp(minimumTurningSpeed, fullStrengthSpeed, speed);
            return Mathf.Lerp(parkedTurnStrength, movingTurnStrength, speedFactor);
        }

        public static float GetSteeringDirection(float forwardSpeed)
        {
            return forwardSpeed < -0.1f ? -1f : 1f;
        }

        public static float ResolveDigitalAxis(bool positivePressed, bool negativePressed, float fallbackAxis)
        {
            if (positivePressed == negativePressed)
            {
                return fallbackAxis;
            }

            return positivePressed ? 1f : -1f;
        }

        public static float UpdateSteeringAngle(float currentAngle, float steeringInput, float steeringSpeed, float maximumAngle, float deltaTime)
        {
            return Mathf.Clamp(
                currentAngle + steeringInput * steeringSpeed * deltaTime,
                -maximumAngle,
                maximumAngle);
        }

        public static float GetSteeringWheelVisualAngle(float steeringAngle, float wheelRotationMultiplier)
        {
            return steeringAngle * wheelRotationMultiplier;
        }
    }
}
