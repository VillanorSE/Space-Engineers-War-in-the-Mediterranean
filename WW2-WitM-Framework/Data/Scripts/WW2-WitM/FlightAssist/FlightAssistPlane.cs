using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Utils;
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

        public readonly PitchStats Stats = new PitchStats();

        const double LogMinSpeed = 100.0;
        const int LogEveryTicks = 6;

        double smoothedPitch, smoothedYaw;
        double lastAskedUp;
        bool hasLastAsked;
        int logTick;
        readonly List<IMyCubeGrid> logGroup = new List<IMyCubeGrid>();

        // Returns false when the controller can no longer be flown (the plane is then dropped).
        public bool Tick(bool playerWantsAssist, bool log)
        {
            var c = Controller;
            if (c == null || c.Closed || c.MarkedForClose || !c.IsUnderControl || !c.CanControlShip || Grid.Physics == null)
            {
                Release();
                return false;
            }

            double speed = c.GetShipSpeed();
            var s = Settings;
            if (log && Eligible && speed >= LogMinSpeed && ++logTick % LogEveryTicks == 0)
                WriteLog(c, speed);
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

            // Pitch readout (/assist status): the rate asked last tick against the rate the plane has now.
            if (hasLastAsked)
                Stats.Add(NoseUpDeg(m.Right * (-smoothedPitch * s.PitchRate), m), lastAskedUp, NoseUpDeg(current, m), speed);
            lastAskedUp = NoseUpDeg(worldRate, m);
            hasLastAsked = true;

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
            hasLastAsked = false;
            smoothedPitch = 0;
            smoothedYaw = 0;
        }

        // One /assist log line: flight state, rotation per axis, wing lift and its moment about the whole plane's centre of
        // mass (from where the wing script applied it), the gyro budget, and the subgrid moving most
        // against the main grid (slip = its velocity minus the velocity it would have if bolted rigidly to the main grid).
        void WriteLog(IMyShipController c, double speed)
        {
            MatrixD m = c.WorldMatrix;
            var v = c.GetShipVelocities();
            Vector3D vel = v.LinearVelocity;
            Vector3D w = v.AngularVelocity;
            double fwd = Vector3D.Dot(vel, m.Forward);
            double aoa = MathHelper.ToDegrees(Math.Atan2(-Vector3D.Dot(vel, m.Up), fwd));
            double side = MathHelper.ToDegrees(Math.Atan2(Vector3D.Dot(vel, m.Right), fwd));

            var main = Grid.Physics;
            Vector3D mainCom = main.CenterOfMassWorld;
            Vector3D mainVel = main.LinearVelocity;
            Vector3D mainRate = main.AngularVelocity;
            double maxSlip = 0, maxSpin = 0, groupMass = 0;
            Vector3D groupMoment = Vector3D.Zero;
            string slipGrid = "-", spinGrid = "-";
            logGroup.Clear();
            MyAPIGateway.GridGroups.GetGroup(Grid, GridLinkTypeEnum.Physical, logGroup);
            foreach (var g in logGroup)
            {
                if (g.Physics == null)
                    continue;
                Vector3D com = g.Physics.CenterOfMassWorld;
                groupMass += g.Physics.Mass;
                groupMoment += com * g.Physics.Mass;
                if (g == Grid)
                    continue;
                Vector3D rigid = mainVel + Vector3D.Cross(mainRate, com - mainCom);
                double slip = (g.Physics.LinearVelocity - rigid).Length();
                double spin = ((Vector3D)g.Physics.AngularVelocity - mainRate).Length();
                string id = g.DisplayName + "#" + (g.EntityId % 10000);
                if (slip > maxSlip) { maxSlip = slip; slipGrid = id; }
                if (spin > maxSpin) { maxSpin = spin; spinGrid = id; }
            }
            int subs = logGroup.Count - 1;
            logGroup.Clear();

            // Offset of the main grid's centre of mass from the whole plane's, in cm along forward/up/right.
            Vector3D groupCom = groupMass > 0 ? groupMoment / groupMass : mainCom;
            Vector3D offset = mainCom - groupCom;
            Vector3D lift = Vector3D.Zero, liftAt = groupCom;
            WW2WitM.Aerodynamics.Wing.LiftTotal total;
            if (WW2WitM.Aerodynamics.Wing.GridLift.TryGetValue(Grid.EntityId, out total)
                && MyAPIGateway.Session.GameplayFrameCounter - total.Frame <= 2)
            {
                lift = total.Force;
                liftAt = total.At;
            }
            // Twist from lift about the whole plane's centre of mass, from where the wing script applied it.
            Vector3D moment = Vector3D.Cross(liftAt - groupCom, lift);

            // Approximate override torque the gyros can give: ForceMagnitude x strength (Ship Core) x power.
            double gyroMax = 0;
            foreach (var gyro in Gyros)
            {
                if (gyro.Closed || !gyro.IsFunctional)
                    continue;
                var def = MyDefinitionManager.Static.GetCubeBlockDefinition(gyro.BlockDefinition) as MyGyroDefinition;
                if (def != null)
                    gyroMax += def.ForceMagnitude * gyro.GyroStrengthMultiplier * gyro.GyroPower;
            }

            MyLog.Default.WriteLine(string.Format(
                "WW2-WitM Flight Assist log: grid={0} v={1:0.0} aoa={2:+0.0;-0.0} side={3:+0.0;-0.0}" +
                " inUp={4:+0.0;-0.0} askUp={5:+0.0;-0.0} gotUp={6:+0.0;-0.0} gotRight={7:+0.0;-0.0} gotRoll={8:+0.0;-0.0}" +
                " inYaw={9:+0.00;-0.00} inRoll={10:+0.00;-0.00} eng={11}" +
                " liftUp={12:+0;-0} liftSide={13:+0;-0} liftFwd={14:+0;-0} comOff={15:+0.0;-0.0}/{16:+0.0;-0.0}/{17:+0.0;-0.0}" +
                " liftMomUp={18:+0.0;-0.0} liftMomRight={19:+0.0;-0.0} liftMomRoll={20:+0.0;-0.0} gyroMax={21:0}" +
                " subs={22} slip={23:0.00}@{24} spin={25:0.0}@{26}",
                Grid.EntityId % 10000, speed, aoa, side,
                NoseUpDeg(m.Right * (-smoothedPitch * Settings.PitchRate), m), hasLastAsked ? lastAskedUp : 0.0,
                NoseUpDeg(w, m), MathHelper.ToDegrees(Vector3D.Dot(Vector3D.Cross(w, m.Forward), m.Right)),
                MathHelper.ToDegrees(Vector3D.Dot(w, m.Forward)),
                smoothedYaw, c.RollIndicator, Engaged ? 1 : 0,
                Vector3D.Dot(lift, m.Up) / 1000, Vector3D.Dot(lift, m.Right) / 1000, Vector3D.Dot(lift, m.Forward) / 1000,
                Vector3D.Dot(offset, m.Forward) * 100, Vector3D.Dot(offset, m.Up) * 100, Vector3D.Dot(offset, m.Right) * 100,
                Vector3D.Dot(moment, m.Right) / 1000, -Vector3D.Dot(moment, m.Up) / 1000, Vector3D.Dot(moment, m.Forward) / 1000,
                gyroMax / 1000, subs, maxSlip, slipGrid, MathHelper.ToDegrees(maxSpin), spinGrid));
        }

        // Nose-up rate in degrees per second for an angular velocity (negative = nose down).
        static double NoseUpDeg(Vector3D rate, MatrixD m)
        {
            return MathHelper.ToDegrees(Vector3D.Dot(Vector3D.Cross(rate, m.Forward), m.Up));
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

    // Assisted-flight pitch since the last /assist status, split by the pilot's pitch input.
    // A gyro on override closes a small rate gap within a few ticks, so a lasting shortfall means it is at full torque.
    public class PitchStats
    {
        const double InputDeg = 0.5;
        const double ShortDeg = 1.0;
        static readonly string[] Names = { "Hands off", "Pitching up", "Pitching down" };

        readonly int[] ticks = new int[3];
        readonly int[] shortTicks = new int[3];
        readonly double[] asked = new double[3];
        readonly double[] got = new double[3];
        double speedSum;

        public void Add(double pilotUp, double askedUp, double gotUp, double speed)
        {
            int k = Math.Abs(pilotUp) < InputDeg ? 0 : pilotUp > 0 ? 1 : 2;
            ticks[k]++;
            asked[k] += askedUp;
            got[k] += gotUp;
            if (Math.Abs(askedUp - gotUp) > ShortDeg)
                shortTicks[k]++;
            speedSum += speed;
        }

        // Builds the report and starts a new period.
        public string Report()
        {
            int total = ticks[0] + ticks[1] + ticks[2];
            if (total == 0)
                return " Pitch: no assisted flight since the last status.";
            var sb = new System.Text.StringBuilder();
            sb.Append(" Pitch since the last status (").Append((total * Plane.Dt).ToString("0.0")).Append(" s, avg ")
                .Append((speedSum / total).ToString("0")).Append(" m/s; + = nose up, deg/s):");
            for (int k = 0; k < 3; k++)
            {
                if (ticks[k] == 0)
                    continue;
                double a = asked[k] / ticks[k], g = got[k] / ticks[k];
                sb.Append(' ').Append(Names[k]).Append(' ').Append((ticks[k] * Plane.Dt).ToString("0.0")).Append(" s: asked ")
                    .Append(a.ToString("+0.0;-0.0")).Append(", got ").Append(g.ToString("+0.0;-0.0"));
                if (Math.Abs(a) >= InputDeg)
                    sb.Append(" (").Append((g / a * 100).ToString("0")).Append("%)");
                sb.Append(", gyro short ").Append((100.0 * shortTicks[k] / ticks[k]).ToString("0")).Append("% of the time.");
            }
            for (int k = 0; k < 3; k++)
            {
                ticks[k] = shortTicks[k] = 0;
                asked[k] = got[k] = 0;
            }
            speedSum = 0;
            return sb.ToString();
        }
    }
}
