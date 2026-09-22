using NUnit.Framework;
using FoggyRoad.Driving;

public class CarControllerManualGearTests
{
    [Test]
    public void FirstGear_CanStartFromRest()
    {
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(0f, 0f, 12f), Is.True);
    }

    [Test]
    public void FirstGear_CanStartWhileTheCarIsSlightlyRollingBackward()
    {
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(-0.25f, 0f, 12f), Is.True);
    }

    [TestCase(0f, 9.5f, 22f)]
    [TestCase(9.49f, 9.5f, 22f)]
    [TestCase(18.99f, 19f, 35f)]
    [TestCase(29.99f, 30f, 50f)]
    public void HigherGear_CannotPullBelowItsMinimumSpeed(float speed, float minimumSpeed, float maximumSpeed)
    {
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(speed, minimumSpeed, maximumSpeed), Is.False);
    }

    [TestCase(9.5f, 9.5f, 22f)]
    [TestCase(19f, 19f, 35f)]
    [TestCase(30f, 30f, 50f)]
    public void HigherGear_CanPullAtOrAboveItsMinimumSpeed(float speed, float minimumSpeed, float maximumSpeed)
    {
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(speed, minimumSpeed, maximumSpeed), Is.True);
    }

    [Test]
    public void Gear_DoesNotPullPastItsMaximumSpeed()
    {
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(22f, 9.5f, 22f), Is.False);
    }

    [TestCase(1, 22f, 9.5f)]
    [TestCase(2, 35f, 19f)]
    [TestCase(3, 50f, 30f)]
    public void ExistingCars_ReceiveManualMinimumSpeeds(int gearIndex, float maxSpeed, float expectedMinimumSpeed)
    {
        Assert.That(ManualTransmissionRules.GetDefaultMinimumDriveSpeed(gearIndex, maxSpeed), Is.EqualTo(expectedMinimumSpeed));
    }

    [Test]
    public void AdjacentGears_WorkInTheirSharedSpeedRange()
    {
        // 36 km/h: both 1st and 2nd gear can pull.
        const float firstSecondOverlapSpeed = 10f;
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(firstSecondOverlapSpeed, 0f, 12f), Is.True);
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(firstSecondOverlapSpeed, 9.5f, 22f), Is.True);

        // 72 km/h: both 2nd and 3rd gear can pull.
        const float secondThirdOverlapSpeed = 20f;
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(secondThirdOverlapSpeed, 9.5f, 22f), Is.True);
        Assert.That(ManualTransmissionRules.IsForwardGearSpeedValid(secondThirdOverlapSpeed, 19f, 35f), Is.True);
    }

    [Test]
    public void Steering_DoesNotRotateTheCarAtRest()
    {
        Assert.That(ManualTransmissionRules.GetTurnStrength(0f, 35f, 95f, 4f), Is.EqualTo(0f));
    }

    [Test]
    public void Steering_BeginsOnlyAfterTheCarStartsMoving()
    {
        Assert.That(ManualTransmissionRules.GetTurnStrength(0.34f, 35f, 95f, 4f), Is.EqualTo(0f));
        Assert.That(ManualTransmissionRules.GetTurnStrength(0.36f, 35f, 95f, 4f), Is.GreaterThan(0f));
    }

    [Test]
    public void Steering_UsesFullStrengthAtDrivingSpeed()
    {
        Assert.That(ManualTransmissionRules.GetTurnStrength(4f, 35f, 95f, 4f), Is.EqualTo(95f));
    }

    [Test]
    public void Steering_ReversesDirectionWhileBackingUp()
    {
        Assert.That(ManualTransmissionRules.GetSteeringDirection(-1f), Is.EqualTo(-1f));
        Assert.That(ManualTransmissionRules.GetSteeringDirection(1f), Is.EqualTo(1f));
    }

    [Test]
    public void KeyboardInput_OverridesTheConfiguredInputAxis()
    {
        Assert.That(ManualTransmissionRules.ResolveDigitalAxis(true, false, 0f), Is.EqualTo(1f));
        Assert.That(ManualTransmissionRules.ResolveDigitalAxis(false, true, 0f), Is.EqualTo(-1f));
    }

    [Test]
    public void KeyboardInput_UsesTheConfiguredInputAxisWhenNoKeyIsHeld()
    {
        Assert.That(ManualTransmissionRules.ResolveDigitalAxis(false, false, 0.4f), Is.EqualTo(0.4f));
    }

    [Test]
    public void SteeringAngle_IsStoredWhenTheKeyIsReleased()
    {
        float turnedAngle = ManualTransmissionRules.UpdateSteeringAngle(0f, 1f, 110f, 32f, 0.1f);
        float heldAngle = ManualTransmissionRules.UpdateSteeringAngle(turnedAngle, 0f, 110f, 32f, 0.1f);

        Assert.That(turnedAngle, Is.EqualTo(11f));
        Assert.That(heldAngle, Is.EqualTo(turnedAngle));
    }

    [Test]
    public void SteeringAngle_IsClampedToTheMaximum()
    {
        Assert.That(ManualTransmissionRules.UpdateSteeringAngle(30f, 1f, 110f, 32f, 0.1f), Is.EqualTo(32f));
        Assert.That(ManualTransmissionRules.UpdateSteeringAngle(-30f, -1f, 110f, 32f, 0.1f), Is.EqualTo(-32f));
    }

    [Test]
    public void SteeringWheelVisual_FollowsTheStoredSteeringAngle()
    {
        Assert.That(ManualTransmissionRules.GetSteeringWheelVisualAngle(20f, 12f), Is.EqualTo(240f));
        Assert.That(ManualTransmissionRules.GetSteeringWheelVisualAngle(-20f, 12f), Is.EqualTo(-240f));
    }
}
