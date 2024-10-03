using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_cameratilt")]
    class CameraTilt
    {
        public static ICoreClientAPI ClientAPI { get; set; } = null;
        public static Camera Camera { get; set; } = null;
        public Harmony harmonyPatcher;

        private static float currentRoll = 0f;
        private static float currentPitch = 0f;
        private static float damageRoll = 0f;
        private static float damagePitch = 0f;
        private static Random random = new Random();
        private static int deathRollValue;

        public static float TiltStrength => InitializeMod.ModConfig.TiltStrength;
        public static float TiltSpeedMultiplier => InitializeMod.ModConfig.TiltSpeedMultiplier;
        public static bool AllowMidairTilt => InitializeMod.ModConfig.AllowMidairTilt;
        public static bool InvertTiltDirection => InitializeMod.ModConfig.InvertTiltDirection;

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
            if(Harmony.HasAnyPatches("betterfpcamera_cameratilt"))
            {
                harmonyPatcher.UnpatchAll();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntityPlayer), "Initialize")]
        public static void Initialize(EntityPlayer __instance, EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            ResetCameraTilt();
            deathRollValue = random.Next(0, 2);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "Revive")]
        public static void Revive()
        {
            ResetCameraTilt();
            deathRollValue = random.Next(0, 2);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Camera), "Update")]
        public static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            // Get the player entity from the intersection tester
            IClientWorldAccessor clientWorld = (intersectionTester.blockSelectionTester as IClientWorldAccessor);
            EntityPlayer playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity != null && ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                bool isTiltAllowed = (playerEntity.OnGround || AllowMidairTilt) && playerEntity.Controls.TriesToMove;

                float targetRoll = (isTiltAllowed ? GetRollTilt(playerEntity) : 0f) + damageRoll;
                float targetPitch = (isTiltAllowed ? GetPitchTilt(playerEntity) : 0f) + damagePitch;

                float lerpTime = (playerEntity.Alive ? 10f : 20f) * TiltSpeedMultiplier;

                if(!playerEntity.Alive)
                {
                    // If the player's animation manager and animator are both available...
                    if(playerEntity.AnimManager != null && playerEntity.AnimManager.Animator != null)
                    {
                        // Check if the "die" animation is either not active or has progressed beyond 80%...
                        if(!playerEntity.AnimManager.IsAnimationActive("die") || playerEntity.AnimManager.GetAnimationState("die").AnimProgress >= 0.8f)
                        {
                            // Determine the death roll direction, adjust the roll and lerp time accordingly...
                            targetRoll = deathRollValue == 0 ? -(TiltStrength * 3) : (TiltStrength * 3);
                            lerpTime /= 3;
                        }
                        else
                        {
                            targetRoll = 0;
                        }
                    }
                    else
                    {
                        targetRoll = 0;
                    }

                    // Reset pitch and damage roll...
                    targetPitch = 0;
                    damageRoll = 0;
                    damagePitch = 0;
                }

                // Smoothly interpolate the roll and pitch...
                currentRoll = GameMath.Lerp(currentRoll, targetRoll, deltaTime * lerpTime);
                currentPitch = GameMath.Lerp(currentPitch, targetPitch, deltaTime * lerpTime);

                // Gradually reduce the damage-induced tilt over time...
                damageRoll = GameMath.Lerp(damageRoll, 0f, deltaTime * lerpTime);
                damagePitch = GameMath.Lerp(damagePitch, 0f, deltaTime * lerpTime);

                // Rotate the camera around it's local space now based on the current roll and pitch!
                RotateCameraLocal(__instance, currentRoll, currentPitch);
            }

            // Update camera instance...
            Camera = __instance;
        }

        public static void ResetCameraTilt()
        {
            if(Camera != null)
            {
                currentRoll = 0;
                currentPitch = 0;
                damageRoll = 0;
                damagePitch = 0;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "OnHurt")]
        public static void OnHurt(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            // Ensure we have a valid damage source, the player is alive, and the camera isn't null...
            if(damageSource == null || !__instance.Alive || Camera == null)
            {
                return;
            }

            Vec3d damageSourcePosition = damageSource.HitPosition ?? damageSource.GetSourcePosition();

            if(damageSourcePosition != null && Camera != null)
            {
                // Get the player's position and the damage source position...
                Vec3d playerPosition = __instance.Pos.XYZ;

                // Calculate the direction of the damage relative to the player
                Vec3d damageDirection = damageSourcePosition.Sub(playerPosition).Normalize();

                // Get the forward and up vectors...
                Vec3d forward = Camera.forwardVec;
                Vec3d up = new Vec3d(0, 1, 0);

                // Calculate the right vector as the cross product of forward and up...
                Vec3d right = forward.Cross(up).Normalize();

                // Calculate tilt amount...
                float tiltAmount = TiltStrength * 2;

                // Calculate the tilt roll...
                float tiltRoll = CalculateDamageTilt(damageDirection, right, tiltAmount);
                float tiltPitch = CalculateDamagePitch(damageDirection, forward, tiltAmount);

                // Add the damage-induced tilt to the current roll and pitch...
                damageRoll += tiltRoll;
                damagePitch += tiltPitch;

                Debug.Log($"damageRoll: {damageRoll} damagePitch: {damagePitch}");
            }
        }

        private static void RotateCameraLocal(Camera __instance, double rollRotation, double pitchRotation)
        {
            // Get the forward and up vectors...
            Vec3d forward = __instance.forwardVec;
            Vec3d up = new Vec3d(0, 1, 0);

            // Calculate the right vector as the cross product of forward and up...
            Vec3d right = forward.Cross(up).Normalize();

            // Apply roll to the camera matrix by rotating around the forward vector...
            double[] rollAxis = forward.ToDoubleArray();
            Mat4d.Rotate(ClientAPI.Render.CameraMatrixOrigin, ClientAPI.Render.CameraMatrixOrigin, rollRotation, rollAxis);

            // Apply pitch to the camera matrix by rotating around the right vector...
            double[] pitchAxis = right.ToDoubleArray();
            Mat4d.Rotate(ClientAPI.Render.CameraMatrixOrigin, ClientAPI.Render.CameraMatrixOrigin, pitchRotation, pitchAxis);

            // Update the float array to reflect the modified matrix...
            for(int i = 0; i < 16; i++)
            {
                ClientAPI.Render.CameraMatrixOriginf[i] = (float)ClientAPI.Render.CameraMatrixOrigin[i];
            }
        }

        // (Fix the night skybox rotating along with the camera)...
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShaderProgramNightsky), "ViewMatrix", MethodType.Setter)]
        public static void ViewMatrix(ShaderProgramNightsky __instance, ref float[] value)
        {
            float[] cameraMatrix = (float[])ClientAPI.Render.CameraMatrixOriginf.Clone();

            for(int i = 0; i < 16; i++)
            {
                value[i] = (float)cameraMatrix[i];
            }
        }

        // (Fix the daytime skybox rotating along with the camera)...
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShaderProgramSky), "ModelViewMatrix", MethodType.Setter)]
        public static void ModelViewMatrix(ShaderProgramSky __instance, ref float[] value)
        {
            float[] cameraMatrix = (float[])ClientAPI.Render.CameraMatrixOriginf.Clone();

            for(int i = 0; i < 16; i++)
            {
                value[i] = (float)cameraMatrix[i];
            }
        }

        // Calculate damage roll tilt based on damage direction (relative to the player)...
        private static float CalculateDamageTilt(Vec3d damageDirection, Vec3d right, float tiltStrength)
        {
            double rightFactor = damageDirection.Dot(right);
            float rollTilt = (float)(rightFactor * TiltStrength);
            return rollTilt;
        }

        // Calculate damage pitch tilt based on damage direction (relative to the player)...
        private static float CalculateDamagePitch(Vec3d damageDirection, Vec3d forward, float tiltStrength)
        {
            double forwardDot = forward.Dot(damageDirection);
            float tiltPitch = forwardDot < -0.5 ? -tiltStrength : (forwardDot > 0.5 ? tiltStrength : 0);
            return tiltPitch;
        }

        // Calculate roll tilt based on movement direction (left or right strafing)...
        static float GetRollTilt(EntityPlayer player)
        {
            float lateralMovement = player.Controls.Left ? -1 : player.Controls.Right ? 1 : 0;
            return lateralMovement * (InvertTiltDirection ? TiltStrength : -TiltStrength);
        }

        // Calculate pitch tilt based on movement direction (forward or backward)...
        static float GetPitchTilt(EntityPlayer player)
        {
            float forwardMovement = player.Controls.Forward ? -1 : player.Controls.Backward ? 1 : 0;
            return forwardMovement * (InvertTiltDirection ? TiltStrength : -TiltStrength);
        }
    }
}
