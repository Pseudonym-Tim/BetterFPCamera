using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BetterFPCamera
{
    public class InitializeMod : ModSystem
    {
        public static ICoreClientAPI ClientAPI { get; set; } = null;
        public static ModInfo ModInfo { get; set; } = null;

        readonly CameraTilt cameraTilt = new();
        readonly CameraHeadbob cameraHeadbob = new();
        readonly CameraShake cameraShake = new();
        readonly CameraFix cameraFix = new();

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

        public void CheckCreateConfig()
        {
            ModConfig modConfiguration = null;
            ModConfig defaultConfig = new ModConfig(); // Create an instance of default config...

            try
            {
                modConfiguration = ClientAPI.LoadModConfig<ModConfig>("betterfpcam_config.json");
            }
            catch(Exception exception)
            {
                Debug.Log("Failed to load mod configuration...");
                modConfiguration = null;
            }

            // If the config is null, create a new one
            if(modConfiguration == null)
            {
                Debug.Log("Generating new mod config!");
                modConfiguration = new ModConfig();
            }
            else
            {
                // Fix missing or invalid properties in the loaded config...
                modConfiguration.FixMissingOrInvalidProperties(defaultConfig);
            }

            // Save the (potentially updated) config back to the file...
            ClientAPI.StoreModConfig(modConfiguration, "betterfpcam_config.json");

            ModConfig = modConfiguration;
        }

        // Load client-side only...
        public override bool ShouldLoad(EnumAppSide appSide) => appSide == EnumAppSide.Client;
    
        public static ModConfig ModConfig
        {
            get { return (ModConfig)ClientAPI.ObjectCache["betterfpcam_config.json"]; }
            set { ClientAPI.ObjectCache.Add("betterfpcam_config.json", value); }
        }
    }

    public class Debug
    {
        private static readonly OperatingSystem system = Environment.OSVersion;
        static private ILogger loggerUtility;

        static public void LoadLogger(ILogger logger) => loggerUtility = logger;

        static public void Log(string message)
        {
            if((system.Platform == PlatformID.Unix || system.Platform == PlatformID.Other) && Environment.UserInteractive)
            {
                Console.WriteLine($"{DateTime.Now:d.M.yyyy HH:mm:ss} [{InitializeMod.ModInfo.Name}] {message}");
            }
            else
            {
                loggerUtility?.Log(EnumLogType.Notification, $"[{InitializeMod.ModInfo.Name}] {message}");
            }
        }
    }
}
