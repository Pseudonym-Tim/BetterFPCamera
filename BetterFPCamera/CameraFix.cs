using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_camerafix")]
    internal sealed class CameraFix
    {
        public static ICoreClientAPI ClientAPI { get; private set; } = null!;

        private Harmony? harmonyPatcher;

        public void Init(ICoreClientAPI api)
        {
            ClientAPI = api;
            Debug.Log($"Initialized [{InitializeMod.ModInfo.Name}] {nameof(CameraFix)}!");
        }

        public void Patch()
        {
            if(!Harmony.HasAnyPatches("betterfpcamera_camerafix"))
            {
                harmonyPatcher = new Harmony("betterfpcamera_camerafix");
                harmonyPatcher.PatchCategory("betterfpcamera_camerafix");
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
        [HarmonyPatch(typeof(Camera), "Update")]
        private static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity != null && ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(!playerEntity.Alive)
                {
                    __instance.Yaw = playerEntity.BodyYaw;
                    playerEntity.Pos.Yaw = playerEntity.BodyYaw;

                    if(HideHandsOnDeath)
                    {
                        ClientAPI.Settings.Bool["hideFpHands"] = true;
                    }
                }
                else
                {
                    if(HideHandsOnDeath)
                    {
                        ClientAPI.Settings.Bool["hideFpHands"] = false;
                    }
                }
            }
        }

        private static bool HideHandsOnDeath
        {
            get
            {
                return InitializeMod.ModConfig.HideHandsOnDeath;
            }
        }
    }
}
