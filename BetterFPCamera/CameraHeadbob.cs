using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_cameraheadbob")]
    internal sealed class CameraHeadbob
    {
        public static ICoreClientAPI ClientAPI { get; private set; } = null!;

        private Harmony? harmonyPatcher;

        private static double previousHorizontalBob = 0d;

        private static bool HorizontalHeadbob
        {
            get
            {
                return InitializeMod.ModConfig.HorizontalHeadbob;
            }
        }

        public void Init(ICoreClientAPI api)
        {
            ClientAPI = api;
            Debug.Log($"Initialized [{InitializeMod.ModInfo.Name}] {nameof(CameraHeadbob)}!");
        }

        public void Patch()
        {
            if(!Harmony.HasAnyPatches("betterfpcamera_cameraheadbob"))
            {
                harmonyPatcher = new Harmony("betterfpcamera_cameraheadbob");
                harmonyPatcher.PatchCategory("betterfpcamera_cameraheadbob");
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Camera), "Update")]
        private static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(ClientAPI.Render.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            if(playerEntity == null)
            {
                return;
            }

            if(!ClientAPI.Settings.Bool["viewBobbing"] || !HorizontalHeadbob)
            {
                return;
            }

            FieldInfo? walkCounterField = AccessTools.Field(playerEntity.GetType(), "walkCounter");

            if(walkCounterField == null)
            {
                walkCounterField = AccessTools.Field(playerEntity.GetType(), "walkCounterValue");
            }

            if(walkCounterField == null)
            {
                return;
            }

            object? rawWalkCounter = walkCounterField.GetValue(playerEntity);

            if(rawWalkCounter == null)
            {
                return;
            }

            double walkCounter = (double)rawWalkCounter;

            EntityControls entityControls = playerEntity.Controls;

            const double bobSpeedMultiplier = 5.75d;
            const double bobAmplitudeMultiplier = 1.0d;

            double sneakMultiplier = entityControls.Sneak ? 5.0d : 1.8d;
            double baseBobFrequency = (playerEntity.FeetInLiquid ? 0.8d : (1.0d + (entityControls.Sprint ? 0.07d : 0.0d))) / (3.0d * sneakMultiplier);
            double bobFrequency = baseBobFrequency * bobSpeedMultiplier;

            double baseBobAmplitude = -0.2d / sneakMultiplier;

            double targetBob = baseBobAmplitude * bobAmplitudeMultiplier * GameMath.Sin(5.5d * walkCounter * bobFrequency);

            double horizontalBob = GameMath.Lerp(previousHorizontalBob, targetBob, deltaTime * 5.0d);
            previousHorizontalBob = horizontalBob;

            ClientAPI.Render.CameraMatrixOrigin[12] = horizontalBob;

            for(int i = 0; i < 16; i++)
            {
                ClientAPI.Render.CameraMatrixOriginf[i] = (float)ClientAPI.Render.CameraMatrixOrigin[i];
            }
        }
    }
}