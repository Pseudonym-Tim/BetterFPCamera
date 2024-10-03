using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_cameraheadbob")]
    class CameraHeadbob
    {
        public static ICoreClientAPI ClientAPI { get; set; } = null;
        public Harmony harmonyPatcher;
        public static double previousHorizontalBob = 0.0f;

        public static bool HorizontalHeadbob => InitializeMod.ModConfig.HorizontalHeadbob;

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
            if(Harmony.HasAnyPatches("betterfpcamera_cameraheadbob"))
            {
                harmonyPatcher.UnpatchAll();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Camera), "Update")]
        public static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            // Get the player entity from the intersection tester...
            IClientWorldAccessor clientWorld = (intersectionTester.blockSelectionTester as IClientWorldAccessor);
            EntityPlayer playerEntity = ClientAPI.World?.Player?.Entity;

            if(ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(playerEntity != null && ClientAPI.Settings.Bool["viewBobbing"] && HorizontalHeadbob)
                {
                    FieldInfo walkCounterField = AccessTools.Field(typeof(EntityPlayer), "walkCounter");

                    // Get the current value of walkCounter...
                    double walkCounter = (double)walkCounterField.GetValue(playerEntity);

                    // Get necessary values from the player controls...
                    EntityControls entityControls = playerEntity.Controls;

                    bool isWalking = entityControls.TriesToMove && playerEntity.OnGround;

                    const double bobSpeedMultiplier = 5.75; // Change this to adjust the speed of bobbing...
                    const double bobAmplitudeMultiplier = 1.0; // Change this to adjust the amplitude independently...

                    // Set the base frequency and amplitude based on sneak/sprint status...
                    double sneakMultiplier = entityControls.Sneak ? 5.0 : 1.8;
                    double baseBobFrequency = (playerEntity.FeetInLiquid ? 0.8 : (1.0 + (entityControls.Sprint ? 0.07 : 0.0))) / (3.0 * sneakMultiplier);

                    // The frequency is affected by the speed multiplier...
                    double bobFrequency = baseBobFrequency * bobSpeedMultiplier;

                    // Set the base amplitude (before applying the amplitude multiplier)...
                    double baseBobAmplitude = -0.2 / sneakMultiplier;

                    // Lerp between the current horizontal bob and the new calculated bob...
                    double targetBob = baseBobAmplitude * bobAmplitudeMultiplier * GameMath.Sin(5.5 * walkCounter * bobFrequency);

                    // Smooth the transition using Lerp, where 'previousBob' is the previous frame's bob value...
                    double horizontalBob = GameMath.Lerp(previousHorizontalBob, targetBob, deltaTime * 5.0);

                    // Update previousBob for the next frame...
                    previousHorizontalBob = horizontalBob;

                    // Apply horizontal bob...
                    ClientAPI.Render.CameraMatrixOrigin[12] = horizontalBob;

                    // Update the float array to reflect the modified matrix...
                    for(int i = 0; i < 16; i++)
                    {
                        ClientAPI.Render.CameraMatrixOriginf[i] = (float)ClientAPI.Render.CameraMatrixOrigin[i];
                    }
                }
            }
        }
    }
}
