using System.Reflection;

namespace BetterFPCamera
{
    public sealed class ModConfig
    {
        public bool HorizontalHeadbob { get; set; } = true;
        public bool HideHandsOnDeath { get; set; } = true;
        public bool AllowMidairTilt { get; set; } = false;

        public bool BlockBreakScreenshake { get; set; } = true;
        public bool BlockPlaceScreenshake { get; set; } = true;
        public bool ThrowSpearScreenshake { get; set; } = true;
        public bool ShootBowScreenshake { get; set; } = true;
        public bool DropItemScreenshake { get; set; } = true;
        public bool ThrowStoneScreenshake { get; set; } = true;
        public bool ThrowSnowballScreenshake { get; set; } = true;
        public bool ThrowBeenadeScreenshake { get; set; } = true;

        public bool DamageTilt { get; set; } = true;

        public float DamageShakeMultiplier { get; set; } = 5f;
        public float MaxDamageShake { get; set; } = 1f;
        public float TiltStrength { get; set; } = 0.025f;
        public float TiltSpeedMultiplier { get; set; } = 0.75f;

        public float BlockBreakScreenshakeStrength { get; set; } = 0.15f;
        public float ThrowSpearScreenshakeStrength { get; set; } = 0.20f;
        public float ShootBowScreenshakeStrength { get; set; } = 0.20f;
        public float ThrowStoneScreenshakeStrength { get; set; } = 0.12f;
        public float ThrowSnowballScreenshakeStrength { get; set; } = 0.10f;
        public float ThrowBeenadeScreenshakeStrength { get; set; } = 0.22f;
        public float BlockPlaceScreenshakeStrength { get; set; } = 0.10f;
        public float DropItemScreenshakeStrength { get; set; } = 0.25f;

        public bool InvertTiltDirection { get; set; } = false;

        public void FixMissingOrInvalidProperties(ModConfig defaultConfig)
        {
            PropertyInfo[] properties = typeof(ModConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach(PropertyInfo propertyInfo in properties)
            {
                object? currentValue = propertyInfo.GetValue(this);
                object? defaultValue = propertyInfo.GetValue(defaultConfig);

                if(currentValue == null)
                {
                    propertyInfo.SetValue(this, defaultValue);
                    continue;
                }

                if(propertyInfo.PropertyType == typeof(float))
                {
                    float floatValue = (float)currentValue;

                    if(float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                    {
                        propertyInfo.SetValue(this, defaultValue);
                    }
                }
            }
        }
    }
}