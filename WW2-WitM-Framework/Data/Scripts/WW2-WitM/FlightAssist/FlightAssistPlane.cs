using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

namespace WW2WitM.FlightAssist
{
    // Per-plane settings: defaults, or the [Flight Assist] section of the cockpit's Custom Data.
    public class AssistSettings
    {
        public const string Section = "Flight Assist";

        public bool Enabled = true;
        public float PitchRateDeg = 60f;
        public float YawRateDeg = 30f;
        public float RollRateDeg = 100f;
        public float TurnRoll = 0.8f;
        public float Level = 0.3f;
        public float LevelDeadZoneDeg = 3f;
        public float LevelFadeEndDeg = 150f;
        public float PathLimitDeg = 25f;
        public float MinSpeed = 30f;
        public float Smoothing = 0.08f;

        public double PitchRate, YawRate, RollRate, Alpha;

        public static readonly AssistSettings Default = Build(null);

        public static AssistSettings Build(MyIni ini)
        {
            var s = new AssistSettings();
            if (ini != null)
            {
                s.Enabled = ini.Get(Section, "Enabled").ToBoolean(s.Enabled);
                s.PitchRateDeg = Clamp(ini.Get(Section, "Pitch rate at full mouse (deg per s)").ToSingle(s.PitchRateDeg), 0f, 360f);
                s.YawRateDeg = Clamp(ini.Get(Section, "Yaw rate at full mouse (deg per s)").ToSingle(s.YawRateDeg), 0f, 360f);
                s.RollRateDeg = Clamp(ini.Get(Section, "Roll rate at full roll (deg per s)").ToSingle(s.RollRateDeg), 0f, 360f);
                s.TurnRoll = Clamp(ini.Get(Section, "Roll into turns (0-1, negative reverses)").ToSingle(s.TurnRoll), -1f, 1f);
                s.Level = Clamp(ini.Get(Section, "Return to level (0-1)").ToSingle(s.Level), 0f, 1f);
                s.LevelDeadZoneDeg = Clamp(ini.Get(Section, "Level dead zone (degrees of bank)").ToSingle(s.LevelDeadZoneDeg), 0f, 45f);
                s.LevelFadeEndDeg = Clamp(ini.Get(Section, "Level fades out by (degrees of bank; 180 always rolls upright)").ToSingle(s.LevelFadeEndDeg), 90f, 180f);
                s.PathLimitDeg = Clamp(ini.Get(Section, "Max nose angle from flight path (degrees; 180 off)").ToSingle(s.PathLimitDeg), 5f, 180f);
                s.MinSpeed = Clamp(ini.Get(Section, "Assist above speed (m/s)").ToSingle(s.MinSpeed), 0f, 1000f);
                s.Smoothing = Clamp(ini.Get(Section, "Mouse smoothing (seconds)").ToSingle(s.Smoothing), 0f, 1f);
            }
            s.PitchRate = MathHelper.ToRadians(s.PitchRateDeg);
            s.YawRate = MathHelper.ToRadians(s.YawRateDeg);
            s.RollRate = MathHelper.ToRadians(s.RollRateDeg);
            s.Alpha = s.Smoothing <= 0f ? 1.0 : 1.0 - Math.Exp(-Plane.Dt / s.Smoothing);
            return s;
        }

        static float Clamp(float v, float lo, float hi)
        {
            if (float.IsNaN(v)) return lo;
            return v < lo ? lo : v > hi ? hi : v;
        }
    }

    // One piloted plane: the law from the plane script (witm-plane-control.cs 1.6), on the plane's own gyros.
    public class Plane
    {
        public const double Dt = 1.0 / 60.0;
        const float MouseFullScale = 20f;
        const double MaxRateStep = 0.3;
        const double PathReturnGain = 3.0;
        const double ReleaseFraction = 0.4;
        const float WriteEpsilon = 0.002f;

        public IMyCubeGrid Grid;
        public IMyShipController Controller;
        public ulong PilotSteamId;
        public bool Eligible;
        public string Reason = "";
        public int NextCheckTick;
        public bool SeenThisScan;
        public AssistSettings Settings = AssistSettings.Default;
        public string SettingsSource;
        public readonly List<IMyGyro> Gyros = new List<IMyGyro>();
        public bool Engaged;

        double smoothedPitch, smoothedYaw;

        // Returns false when the controller can no longer be flown (the plane is then dropped).
        public bool Tick(bool playerWantsAssist)
        {
            var c = Controller;
            if (c == null || c.Closed || c.MarkedForClose || !c.IsUnderControl || !c.CanControlShip || Grid.Physics == null)
            {
                Release();
                return false;
            }

            double speed = c.GetShipSpeed();
            var s = Settings;
            bool fast = speed >= s.MinSpeed || (Engaged && speed >= s.MinSpeed * ReleaseFraction);
            if (!Eligible || !playerWantsAssist || !s.Enabled || !fast || Gyros.Count == 0)
            {
                if (Engaged)
                    Release();
                return true;
            }

            Vector2 mouse = c.RotationIndicator;
            smoothedPitch += (MathHelper.Clamp(mouse.X / MouseFullScale, -1f, 1f) - smoothedPitch) * s.Alpha;
            smoothedYaw += (MathHelper.Clamp(mouse.Y / MouseFullScale, -1f, 1f) - smoothedYaw) * s.Alpha;
            double rollInput = c.RollIndicator;

            MatrixD m = c.WorldMatrix;
            double roll = rollInput + s.TurnRoll * smoothedYaw;
            bool pilotRolling = Math.Abs(rollInput) > 0.01;
            bool pilotTurning = Math.Abs(smoothedYaw) > 0.02;
            Vector3D gravity = c.GetNaturalGravity();
            if (!pilotRolling && !pilotTurning && gravity.LengthSquared() > 0.01)
            {
                Vector3D down = Vector3D.Normalize(gravity);
                double right = Vector3D.Dot(down, m.Right);
                double below = -Vector3D.Dot(down, m.Up);
                double weight = Math.Sqrt(right * right + below * below);
                double bankDeg = MathHelper.ToDegrees(Math.Atan2(right, below));
                double absBank = Math.Abs(bankDeg);
                double fade = absBank <= 90.0 || s.LevelFadeEndDeg >= 180f ? 1.0
                    : MathHelper.Clamp((s.LevelFadeEndDeg - absBank) / Math.Max(s.LevelFadeEndDeg - 90.0, 1.0), 0.0, 1.0);
                if (absBank > s.LevelDeadZoneDeg)
                    roll -= s.Level * MathHelper.Clamp(bankDeg / 90.0, -1.0, 1.0) * weight * fade;
            }
            roll = MathHelper.Clamp(roll, -1.0, 1.0);

            // Same axes and signs as the game's cockpit gyro control (MyShipController.MoveAndRotate: -X, -Y, -roll).
            Vector3D worldRate = m.Right * (-smoothedPitch * s.PitchRate)
                + m.Up * (-smoothedYaw * s.YawRate)
                + m.Backward * (-roll * s.RollRate);

            var velocities = c.GetShipVelocities();
            Vector3D velocity = velocities.LinearVelocity;
            if (s.PathLimitDeg < 180f && velocity.LengthSquared() > 1.0)
            {
                Vector3D forward = m.Forward;
                Vector3D flight = Vector3D.Normalize(velocity);
                double offDeg = MathHelper.ToDegrees(Math.Acos(MathHelper.Clamp(Vector3D.Dot(forward, flight), -1.0, 1.0)));
                Vector3D away = Vector3D.Cross(flight, forward);
                if (offDeg > 0.5 && away.LengthSquared() > 1e-8)
                {
                    away = Vector3D.Normalize(away);
                    double softStart = s.PathLimitDeg * 0.6;
                    double keep = MathHelper.Clamp((s.PathLimitDeg - offDeg) / Math.Max(s.PathLimitDeg - softStart, 1.0), 0.0, 1.0);
                    double outward = Vector3D.Dot(worldRate, away);
                    if (outward > 0)
                        worldRate -= away * outward * (1.0 - keep);
                    if (offDeg > s.PathLimitDeg)
                        worldRate -= away * MathHelper.ToRadians(offDeg - s.PathLimitDeg) * PathReturnGain;
                }
            }

            // Ramp-safe step (MyGridGyroSystem.UpdateOverrideAccelerationRampFrames; knowledge se-gyro-override-ramp).
            Vector3D current = velocities.AngularVelocity;
            Vector3D step = worldRate - current;
            double stepSq = step.LengthSquared();
            if (stepSq > MaxRateStep * MaxRateStep)
                worldRate = current + step * (MaxRateStep / Math.Sqrt(stepSq));

            // Set before the first override write so Release() always clears it, even if a write throws.
            Engaged = true;
            for (int i = Gyros.Count - 1; i >= 0; i--)
            {
                var gyro = Gyros[i];
                if (gyro.Closed || gyro.MarkedForClose)
                {
                    Gyros.RemoveAtFast(i);
                    continue;
                }
                if (!gyro.IsFunctional)
                    continue;
                if (!gyro.GyroOverride)
                    gyro.GyroOverride = true;
                MatrixD gm = gyro.WorldMatrix;
                // IMyGyro Pitch/Yaw/Roll are the negated override velocity in the gyro's frame (MyGyro).
                float pitch = (float)-Vector3D.Dot(worldRate, gm.Right);
                float yaw = (float)-Vector3D.Dot(worldRate, gm.Up);
                float rollRate = (float)-Vector3D.Dot(worldRate, gm.Backward);
                if (Math.Abs(gyro.Pitch - pitch) > WriteEpsilon) gyro.Pitch = pitch;
                if (Math.Abs(gyro.Yaw - yaw) > WriteEpsilon) gyro.Yaw = yaw;
                if (Math.Abs(gyro.Roll - rollRate) > WriteEpsilon) gyro.Roll = rollRate;
            }
            return true;
        }

        public void Release()
        {
            if (Engaged)
            {
                foreach (var gyro in Gyros)
                    ReleaseGyro(gyro);
            }
            Engaged = false;
            smoothedPitch = 0;
            smoothedYaw = 0;
        }

        public static void ReleaseGyro(IMyGyro gyro)
        {
            if (gyro == null || gyro.Closed || !gyro.GyroOverride)
                return;
            gyro.Pitch = 0f;
            gyro.Yaw = 0f;
            gyro.Roll = 0f;
            gyro.GyroOverride = false;
        }
    }
}
