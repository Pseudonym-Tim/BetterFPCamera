using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_cameratilt")]
    internal sealed class CameraTilt
    {
        public static ICoreClientAPI ClientAPI { get; private set; } = null!;
        public static Camera? Camera { get; private set; } = null;

        private Harmony? harmonyPatcher;

        private static float currentRoll = 0f;
        private static float currentPitch = 0f;
        private static float damageRoll = 0f;
        private static float damagePitch = 0f;

        private static readonly Random random = new Random();
        private static int deathRollValue = 0;

        public void Init(ICoreClientAPI api)
        {
            ClientAPI = api;
            Debug.Log($"Initialized [{InitializeMod.ModInfo.Name}] {nameof(CameraTilt)}!");
        }

        public void Patch()
        {
            if(!Harmony.HasAnyPatches("betterfpcamera_cameratilt"))
            {
                harmonyPatcher = new Harmony("betterfpcamera_cameratilt");
                harmonyPatcher.PatchCategory("betterfpcamera_cameratilt");
            }
        }

        public void Unpatch()
        {
            if(harmonyPatcher != null && Harmony.HasAnyPatches(harmonyPatcher.Id))
            {
                harmonyPatcher.UnpatchAll(harmonyPatcher.Id);
                harmonyPatcher = null;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntityPlayer), "Initialize")]
        private static void Initialize(EntityPlayer __instance, EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            ResetCameraTilt();
            deathRollValue = random.Next(0, 2);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "Revive")]
        private static void Revive()
        {
            ResetCameraTilt();
            deathRollValue = random.Next(0, 2);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Camera), "Update")]
        private static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity != null && ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                bool isTiltAllowed = (playerEntity.OnGround || AllowMidairTilt) && playerEntity.Controls.TriesToMove;

                float targetRoll = (isTiltAllowed ? GetRollTilt(playerEntity) : 0f) + damageRoll;
                float targetPitch = (isTiltAllowed ? GetPitchTilt(playerEntity) : 0f) + damagePitch;

                float lerpTime = (playerEntity.Alive ? 10f : 20f) * TiltSpeedMultiplier;

                if(!playerEntity.Alive)
                {
                    if(playerEntity.AnimManager != null && playerEntity.AnimManager.Animator != null)
                    {
                        if(!playerEntity.AnimManager.IsAnimationActive("die") || playerEntity.AnimManager.GetAnimationState("die").AnimProgress >= 0.8f)
                        {
                            targetRoll = deathRollValue == 0 ? -(TiltStrength * 3f) : (TiltStrength * 3f);
                            lerpTime /= 3f;
                        }
                        else
                        {
                            targetRoll = 0f;
                        }
                    }
                    else
                    {
                        targetRoll = 0f;
                    }

                    targetPitch = 0f;
                    damageRoll = 0f;
                    damagePitch = 0f;
                }

                currentRoll = GameMath.Lerp(currentRoll, targetRoll, deltaTime * lerpTime);
                currentPitch = GameMath.Lerp(currentPitch, targetPitch, deltaTime * lerpTime);

                damageRoll = GameMath.Lerp(damageRoll, 0f, deltaTime * lerpTime);
                damagePitch = GameMath.Lerp(damagePitch, 0f, deltaTime * lerpTime);

                RotateCameraLocal(__instance, currentRoll, currentPitch);
            }

            Camera = __instance;
        }

        public static void ResetCameraTilt()
        {
            currentRoll = 0f;
            currentPitch = 0f;
            damageRoll = 0f;
            damagePitch = 0f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "OnHurt")]
        private static void OnHurt(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            if(damageSource == null || !__instance.Alive || Camera == null || !DamageTilt)
            {
                return;
            }

            Vec3d? damageSourcePosition = damageSource.HitPosition ?? damageSource.GetSourcePosition();

            if(damageSourcePosition == null)
            {
                return;
            }

            Vec3d playerPosition = __instance.Pos.XYZ;
            Vec3d damageDirection = damageSourcePosition.Sub(playerPosition).Normalize();

            Vec3d forward = Camera.forwardVec;
            Vec3d up = new Vec3d(0d, 1d, 0d);
            Vec3d right = forward.Cross(up).Normalize();

            float tiltAmount = TiltStrength * 2f;

            float tiltRoll = CalculateDamageTilt(damageDirection, right, tiltAmount);
            float tiltPitch = CalculateDamagePitch(damageDirection, forward, tiltAmount);

            damageRoll += tiltRoll;
            damagePitch += tiltPitch;
        }

        private static void RotateCameraLocal(Camera __instance, double rollRotation, double pitchRotation)
        {
            Vec3d forward = __instance.forwardVec;
            Vec3d up = new Vec3d(0d, 1d, 0d);
            Vec3d right = forward.Cross(up).Normalize();

            double[] rollAxis = forward.ToDoubleArray();
            Mat4d.Rotate(ClientAPI.Render.CameraMatrixOrigin, ClientAPI.Render.CameraMatrixOrigin, rollRotation, rollAxis);

            double[] pitchAxis = right.ToDoubleArray();
            Mat4d.Rotate(ClientAPI.Render.CameraMatrixOrigin, ClientAPI.Render.CameraMatrixOrigin, pitchRotation, pitchAxis);

            for(int i = 0; i < 16; i++)
            {
                ClientAPI.Render.CameraMatrixOriginf[i] = (float)ClientAPI.Render.CameraMatrixOrigin[i];
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShaderProgramNightsky), "ViewMatrix", MethodType.Setter)]
        private static void ViewMatrix(ShaderProgramNightsky __instance, ref float[] value)
        {
            float[] cameraMatrix = (float[])ClientAPI.Render.CameraMatrixOriginf.Clone();

            for(int i = 0; i < 16; i++)
            {
                value[i] = cameraMatrix[i];
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShaderProgramSky), "ModelViewMatrix", MethodType.Setter)]
        private static void ModelViewMatrix(ShaderProgramSky __instance, ref float[] value)
        {
            float[] cameraMatrix = (float[])ClientAPI.Render.CameraMatrixOriginf.Clone();

            for(int i = 0; i < 16; i++)
            {
                value[i] = cameraMatrix[i];
            }
        }

        private static float CalculateDamageTilt(Vec3d damageDirection, Vec3d right, float tiltStrength)
        {
            double rightFactor = damageDirection.Dot(right);
            float rollTilt = (float)(rightFactor * TiltStrength);
            return rollTilt;
        }

        private static float CalculateDamagePitch(Vec3d damageDirection, Vec3d forward, float tiltStrength)
        {
            double forwardDot = forward.Dot(damageDirection);
            float tiltPitch = forwardDot < -0.5d ? -tiltStrength : (forwardDot > 0.5d ? tiltStrength : 0f);
            return tiltPitch;
        }

        private static float GetRollTilt(EntityPlayer player)
        {
            float lateralMovement = player.Controls.Left ? -1f : (player.Controls.Right ? 1f : 0f);
            return lateralMovement * (InvertTiltDirection ? TiltStrength : -TiltStrength);
        }

        private static float GetPitchTilt(EntityPlayer player)
        {
            float forwardMovement = player.Controls.Forward ? -1f : (player.Controls.Backward ? 1f : 0f);
            return forwardMovement * (InvertTiltDirection ? TiltStrength : -TiltStrength);
        }
    
        private static float TiltStrength
        {
            get
            {
                return InitializeMod.ModConfig.TiltStrength;
            }
        }

        private static float TiltSpeedMultiplier
        {
            get
            {
                return InitializeMod.ModConfig.TiltSpeedMultiplier;
            }
        }

        private static bool AllowMidairTilt
        {
            get
            {
                return InitializeMod.ModConfig.AllowMidairTilt;
            }
        }

        private static bool InvertTiltDirection
        {
            get
            {
                return InitializeMod.ModConfig.InvertTiltDirection;
            }
        }

        private static bool DamageTilt
        {
            get
            {
                return InitializeMod.ModConfig.DamageTilt;
            }
        }
    }
}