using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace BetterFPCamera
{
    [HarmonyPatchCategory("betterfpcamera_camerashake")]
    internal sealed class CameraShake
    {
        private const float StoneThrowSeconds = 0.35f;
        private const float SnowballThrowSeconds = 0.35f;
        private const float BeenadeThrowSeconds = 0.35f;
        private const float SpearThrowSeconds = 0.35f;
        private const float BowShootSeconds = 0.65f;

        public static ICoreClientAPI ClientAPI { get; private set; } = null!;

        private Harmony? harmonyPatcher;

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
            if(harmonyPatcher != null && Harmony.HasAnyPatches(harmonyPatcher.Id))
            {
                harmonyPatcher.UnpatchAll(harmonyPatcher.Id);
                harmonyPatcher = null;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Block), "OnBlockBroken")]
        private static void OnBlockBroken(Block __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            if(byPlayer == null)
            {
                return;
            }

            if(!BlockBreakScreenshake)
            {
                return;
            }

            if(world.Side != EnumAppSide.Client)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(BlockBreakScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemSpear), "OnHeldInteractStop")]
        private static void OnHeldInteractStopItemSpear(ItemSpear __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            if(!ThrowSpearScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity == null)
            {
                return;
            }

            if(byEntity == null || byEntity.EntityId != playerEntity.EntityId)
            {
                return;
            }

            if(secondsUsed < SpearThrowSeconds)
            {
                return;
            }

            ClientAPI?.World?.SetCameraShake(ThrowSpearScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemBow), "OnHeldInteractStop")]
        private static void OnHeldInteractStopItemBow(ItemBow __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            if(!ShootBowScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity == null)
            {
                return;
            }

            if(byEntity == null || byEntity.EntityId != playerEntity.EntityId)
            {
                return;
            }

            if(secondsUsed < BowShootSeconds)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(ShootBowScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlockBehavior), "DoPlaceBlock")]
        private static void DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            if(byPlayer == null)
            {
                return;
            }

            if(world.Side != EnumAppSide.Client)
            {
                return;
            }

            if(!BlockPlaceScreenshake)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(BlockPlaceScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPlayerInventoryManager), "DropItem")]
        private static void DropItem(ItemSlot slot, bool fullStack, bool __result)
        {
            if(!__result || !DropItemScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(ClientAPI?.World?.Player?.Entity is not EntityPlayer playerEntity)
            {
                return;
            }

            if(!playerEntity.Alive)
            {
                return;
            }

            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(DropItemScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemStone), "OnHeldInteractStop")]
        private static void OnHeldInteractStopItemStone(ItemStone __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity == null)
            {
                return;
            }

            if(!ThrowStoneScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(secondsUsed < StoneThrowSeconds)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(ThrowStoneScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemSnowball), "OnHeldInteractStop")]
        private static void OnHeldInteractStopItemSnowball(ItemSnowball __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity == null)
            {
                return;
            }

            if(!ThrowSnowballScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(secondsUsed < SnowballThrowSeconds)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(ThrowSnowballScreenshakeStrength);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemClosedBeenade), "OnHeldInteractStop")]
        private static void OnHeldInteractStopItemClosedBeenade(ItemClosedBeenade __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if(ClientAPI?.Render?.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            EntityPlayer? playerEntity = ClientAPI.World?.Player?.Entity;

            if(playerEntity == null)
            {
                return;
            }

            if(!ThrowBeenadeScreenshake)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(secondsUsed < BeenadeThrowSeconds)
            {
                return;
            }

            ClientAPI.World.SetCameraShake(ThrowBeenadeScreenshakeStrength);
        }

        private static DamageSourceCategory GetDamageSourceCategory(EnumDamageSource source)
        {
            switch(source)
            {
                case EnumDamageSource.Internal:
                case EnumDamageSource.Unknown:
                case EnumDamageSource.Void:
                case EnumDamageSource.Suicide:
                case EnumDamageSource.Revive:
                    {
                        return DamageSourceCategory.Ignored;
                    }
                default:
                    {
                        return DamageSourceCategory.Valid;
                    }
            }
        }

        public enum DamageSourceCategory
        {
            Valid,
            Ignored
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "OnHurt")]
        private static void OnHurt(EntityPlayer __instance, DamageSource damageSource, float damage)
        {
            if(damageSource == null || !__instance.Alive || !DamageTilt || ClientAPI.Render.CameraType != EnumCameraMode.FirstPerson)
            {
                return;
            }

            // Ignore any unwanted damage source types...
            if(GetDamageSourceCategory(damageSource.Source) == DamageSourceCategory.Ignored)
            {
                return;
            }

            if(damage <= 0f)
            {
                return;
            }

            if(ClientAPI.World == null || ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            IClientWorldAccessor? clientWorld = ClientAPI.World as IClientWorldAccessor;

            if(clientWorld == null)
            {
                return;
            }

            if(clientWorld.Player.Entity.EntityId != __instance.EntityId)
            {
                return;
            }

            float normalizedDamage = damage / 100f;
            float rawShake = normalizedDamage * DamageShakeMultiplier;

            float shakeAmount;

            if(MaxDamageShake <= 0f)
            {
                shakeAmount = rawShake;
            }
            else
            {
                float maxShake = GameMath.Min(DamageShakeMultiplier, MaxDamageShake);
                shakeAmount = GameMath.Clamp(rawShake, 0f, maxShake);
            }

            ClientAPI.World.SetCameraShake(shakeAmount);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "Die")]
        private static void Die(EntityPlayer __instance, EnumDespawnReason reason, DamageSource damageSourceForDeath)
        {
            if(ClientAPI?.World == null)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(ClientAPI.World.Player?.Entity?.EntityId != __instance.EntityId)
            {
                return;
            }

            ClientAPI.World.ReduceCameraShake(9999f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityPlayer), "Initialize")]
        private static void Initialize(EntityPlayer __instance, EntityProperties properties, ICoreAPI api, long chunkindex3d)
        {
            if(ClientAPI?.World == null)
            {
                return;
            }

            if(ClientAPI.World.Side != EnumAppSide.Client)
            {
                return;
            }

            if(ClientAPI.World.Player?.Entity?.EntityId != __instance.EntityId)
            {
                return;
            }

            ClientAPI.World.ReduceCameraShake(9999f);
        }

        private static float DamageShakeMultiplier
        {
            get
            {
                return InitializeMod.ModConfig.DamageShakeMultiplier;
            }
        }

        private static bool DamageTilt
        {
            get
            {
                return InitializeMod.ModConfig.DamageTilt;
            }
        }

        private static bool BlockBreakScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.BlockBreakScreenshake;
            }
        }

        private static bool BlockPlaceScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.BlockPlaceScreenshake;
            }
        }

        private static bool DropItemScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.DropItemScreenshake;
            }
        }

        private static bool ShootBowScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.ShootBowScreenshake;
            }
        }

        private static bool ThrowSpearScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.ThrowSpearScreenshake;
            }
        }

        private static float DropItemScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.DropItemScreenshakeStrength;
            }
        }

        private static float BlockBreakScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.BlockBreakScreenshakeStrength;
            }
        }

        private static float ThrowSpearScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.ThrowSpearScreenshakeStrength;
            }
        }

        private static float ShootBowScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.ShootBowScreenshakeStrength;
            }
        }

        private static float BlockPlaceScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.BlockPlaceScreenshakeStrength;
            }
        }

        private static float MaxDamageShake
        {
            get
            {
                return InitializeMod.ModConfig.MaxDamageShake;
            }
        }

        private static bool ThrowStoneScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.ThrowStoneScreenshake;
            }
        }

        private static bool ThrowSnowballScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.ThrowSnowballScreenshake;
            }
        }

        private static bool ThrowBeenadeScreenshake
        {
            get
            {
                return InitializeMod.ModConfig.ThrowBeenadeScreenshake;
            }
        }

        private static float ThrowStoneScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.ThrowStoneScreenshakeStrength;
            }
        }

        private static float ThrowSnowballScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.ThrowSnowballScreenshakeStrength;
            }
        }

        private static float ThrowBeenadeScreenshakeStrength
        {
            get
            {
                return InitializeMod.ModConfig.ThrowBeenadeScreenshakeStrength;
            }
        }
    }
}