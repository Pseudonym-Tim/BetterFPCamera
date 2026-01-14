using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BetterFPCamera
{
    public sealed class InitializeMod : ModSystem
    {
        public static ICoreClientAPI ClientAPI { get; private set; } = null!;
        public static ModInfo ModInfo { get; private set; } = null!;

        private readonly CameraTilt cameraTilt = new CameraTilt();
        private readonly CameraHeadbob cameraHeadbob = new CameraHeadbob();
        private readonly CameraShake cameraShake = new CameraShake();
        private readonly CameraFix cameraFix = new CameraFix();

        public override void Start(ICoreAPI apiClient)
        {
            base.Start(apiClient);

            ModInfo = Mod.Info;

            Debug.LoadLogger(apiClient.Logger);
            Debug.Log($"Running on version: {Mod.Info.Version}");

            cameraTilt.Patch();
            cameraHeadbob.Patch();
            cameraShake.Patch();
            cameraFix.Patch();
        }

        public override void Dispose()
        {
            base.Dispose();

            cameraTilt.Unpatch();
            cameraHeadbob.Unpatch();
            cameraShake.Unpatch();
            cameraFix.Unpatch();
        }

        public override void StartClientSide(ICoreClientAPI apiClient)
        {
            base.StartClientSide(apiClient);

            ClientAPI = apiClient;

            CheckCreateConfig();

            cameraTilt.Init(apiClient);
            cameraHeadbob.Init(apiClient);
            cameraShake.Init(apiClient);
            cameraFix.Init(apiClient);
        }

        private static void SetObjectCache<T>(ICoreClientAPI apiClient, string key, T value) where T : class
        {
            if(apiClient.ObjectCache.ContainsKey(key))
            {
                apiClient.ObjectCache[key] = value;
                return;
            }

            apiClient.ObjectCache.Add(key, value);
        }

        public void CheckCreateConfig()
        {
            ModConfig? modConfiguration = null;
            ModConfig defaultConfig = new ModConfig();

            try
            {
                modConfiguration = ClientAPI.LoadModConfig<ModConfig>("betterfpcam_config.json");
            }
            catch(Exception exception)
            {
                Debug.Log($"Failed to load mod configuration: {exception.GetType().Name}: {exception.Message}");
                modConfiguration = null;
            }

            if(modConfiguration == null)
            {
                Debug.Log("Generating new mod config!");
                modConfiguration = new ModConfig();
            }
            else
            {
                modConfiguration.FixMissingOrInvalidProperties(defaultConfig);
            }

            ClientAPI.StoreModConfig(modConfiguration, "betterfpcam_config.json");

            SetObjectCache(ClientAPI, "betterfpcam_config.json", modConfiguration);
        }

        public override bool ShouldLoad(EnumAppSide appSide)
        {
            return appSide == EnumAppSide.Client;
        }

        public static ModConfig ModConfig
        {
            get
            {
                return (ModConfig)ClientAPI.ObjectCache["betterfpcam_config.json"];
            }
            private set
            {
                SetObjectCache(ClientAPI, "betterfpcam_config.json", value);
            }
        }
    }

    public static class Debug
    {
        private static readonly OperatingSystem system = Environment.OSVersion;
        private static ILogger? loggerUtility;

        public static void LoadLogger(ILogger logger)
        {
            loggerUtility = logger;
        }

        public static void Log(string message)
        {
            if((system.Platform == PlatformID.Unix || system.Platform == PlatformID.Other) && Environment.UserInteractive)
            {
                Console.WriteLine($"{DateTime.Now:d.M.yyyy HH:mm:ss} [{InitializeMod.ModInfo.Name}] {message}");
                return;
            }

            loggerUtility?.Log(EnumLogType.Notification, $"[{InitializeMod.ModInfo.Name}] {message}");
        }
    }
}