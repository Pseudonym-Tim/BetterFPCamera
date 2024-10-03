namespace BetterFPCamera
{
    public class ModConfig
    {
        public bool HorizontalHeadbob { get; set; } = true;
        public bool HideHandsOnDeath { get; set; } = true;
        public bool AllowMidairTilt { get; set; } = false;
        public bool BlockBreakScreenshake { get; set; } = true;
        public bool BlockPlaceScreenshake { get; set; } = true;
        public bool DropItemScreenshake { get; set; } = true;
        public bool DamageTilt { get; set; } = true;
        public float DamageShakeMultiplier { get; set; } = 5f;
        public float TiltStrength { get; set; } = 0.025f;
        public float TiltSpeedMultiplier { get; set; } = 0.75f;
        public float BlockBreakScreenshakeStrength { get; set; } = 0.15f;
        public float BlockPlaceScreenshakeStrength { get; set; } = 0.1f;
        public float DropItemScreenshakeStrength { get; set; } = 0.15f;
        public bool InvertTiltDirection { get; set; } = false;

        public ModConfig()
        {
            // Initialize default settings...
            HorizontalHeadbob = true;
            HideHandsOnDeath = true;
            AllowMidairTilt = false;
            BlockBreakScreenshake = true;
            BlockPlaceScreenshake = true;
            DropItemScreenshake = true;
            DamageTilt = true;
            DamageShakeMultiplier = 5f;
            TiltStrength = 0.025f;
            TiltSpeedMultiplier = 0.75f;
            BlockBreakScreenshakeStrength = 0.15f;
            BlockPlaceScreenshakeStrength = 0.1f;
            DropItemScreenshakeStrength = 0.25f;
            InvertTiltDirection = false;
        }

        public void FixMissingOrInvalidProperties(ModConfig defaultConfig)
        {
            System.Reflection.PropertyInfo[] properties = typeof(ModConfig).GetProperties();

            foreach(System.Reflection.PropertyInfo prop in properties)
            {
                object currentValue = prop.GetValue(this);
                object defaultValue = prop.GetValue(defaultConfig);

                // If current value is null or the same as default, replace it with the default value...
                if(currentValue == null || currentValue.Equals(defaultValue))
                {
                    prop.SetValue(this, defaultValue);
                }
            }
        }
    }
}
