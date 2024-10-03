using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_camerashake")]
    class CameraShake
    {
        public static ICoreClientAPI ClientAPI { get; set; } = null;
        public Harmony harmonyPatcher;

        public static float DamageShakeMultiplier => InitializeMod.ModConfig.DamageShakeMultiplier;
        public static bool DamageTilt => InitializeMod.ModConfig.DamageTilt;
        public static bool BlockBreakScreenshake => InitializeMod.ModConfig.BlockBreakScreenshake;
        public static bool BlockPlaceScreenshake => InitializeMod.ModConfig.BlockPlaceScreenshake;
        public static bool DropItemScreenshake => InitializeMod.ModConfig.DropItemScreenshake;
        public static float DropItemScreenshakeStrength => InitializeMod.ModConfig.DropItemScreenshakeStrength;
        public static float BlockBreakScreenshakeStrength => InitializeMod.ModConfig.BlockBreakScreenshakeStrength;
        public static float BlockPlaceScreenshakeStrength => InitializeMod.ModConfig.BlockPlaceScreenshakeStrength;

        public void Init(ICoreClientAPI api)
        {
            ClientAPI = api;
            Debug.Log($"Initialized [{InitializeMod.ModInfo.Name}] {nameof(CameraShake)}!");
        }

        public void Patch()
        {
            if(!Harmony.HasAnyPatches("betterfpcamera_camerashake"))
            {
                harmonyPatcher = new Harmony("betterfpcamera_camerashake");
                harmonyPatcher.PatchCategory("betterfpcamera_camerashake");
            }
        }

        public void Unpatch()
        {
            if(Harmony.HasAnyPatches("betterfpcamera_camerashake"))
            {
                harmonyPatcher.UnpatchAll();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Block), "OnBlockBroken")]
        public static void OnBlockBroken(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if(ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(byPlayer != null && BlockBreakScreenshake && world.Side == EnumAppSide.Client)
                {
                    ClientAPI.World.SetCameraShake(BlockBreakScreenshakeStrength);
                    Debug.Log("Block broken!");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlockBehavior), "DoPlaceBlock")]
        public static void DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            if(ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(byPlayer != null && ClientAPI.World.Side == EnumAppSide.Client)
                {
                    if(world.Side == EnumAppSide.Client && BlockPlaceScreenshake)
                    {
                        ClientAPI.World.SetCameraShake(BlockPlaceScreenshakeStrength);
                        Debug.Log("Placed block!");
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPlayerInventoryManager), "DropItem")]
        public static void DropItem(ItemSlot slot, bool fullStack, bool __result)
        {
            if(ClientAPI.Render.CameraType == EnumCameraMode.FirstPerson)
            {
                if(ClientAPI.World.Side == EnumAppSide.Client && __result && DropItemScreenshake)
                {
                    ClientAPI.World.SetCameraShake(DropItemScreenshakeStrength);
                    Debug.Log("Drop item!");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "OnHurt")]
        public static void OnHurt(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            // Ensure damageSource is valid, player is alive, damage tilt is enabled and the camera is first person...
            if(damageSource == null || !__instance.Alive || !DamageTilt || ClientAPI.Render.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            // Determine the position of the damage source...
            Vec3d damageSourcePosition = damageSource.HitPosition ?? damageSource.GetSourcePosition();

            // Check if there is damage and the game is running on the client side for the current player...
            if(damage > 0f && ClientAPI.World != null && ClientAPI.World.Side == EnumAppSide.Client)
            {
                IClientWorldAccessor clientWorld = ClientAPI.World;

                if(clientWorld?.Player.Entity.EntityId == __instance.EntityId && damageSourcePosition != null)
                {
                    // Calculate and set screen shake based on damage...
                    float shakeAmount = GameMath.Min(DamageShakeMultiplier, (damage / 100f) * DamageShakeMultiplier);
                    ClientAPI.World.SetCameraShake(shakeAmount);
                    Debug.Log($"Camera shake: {shakeAmount}");
                }
            }
        }
    }
}
