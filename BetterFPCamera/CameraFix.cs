using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_camerafix")]
    class CameraFix
    {
        public static ICoreClientAPI ClientAPI { get; set; } = null;
        public Harmony harmonyPatcher;

        public static bool HideHandsOnDeath => InitializeMod.ModConfig.HideHandsOnDeath;

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
            if(Harmony.HasAnyPatches("betterfpcamera_camerafix"))
            {
                harmonyPatcher.UnpatchAll();
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Camera), "Update")]
        public static void Update(Camera __instance, float deltaTime, AABBIntersectionTest intersectionTester)
        {
            EntityPlayer playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity != null && ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(!playerEntity.Alive)
                {
                    // Fix player rotating their body and camera yaw while they're dead/dying...
                    __instance.Yaw = playerEntity.BodyYaw;
                    playerEntity.Pos.Yaw = playerEntity.BodyYaw;
                    if(HideHandsOnDeath) { ClientAPI.Settings.Bool["hideFpHands"] = true; } // HACK!!!
                }
                else
                {
                    if(HideHandsOnDeath) { ClientAPI.Settings.Bool["hideFpHands"] = false; } // HACK!!!
                }
            }
        }
    }
}
