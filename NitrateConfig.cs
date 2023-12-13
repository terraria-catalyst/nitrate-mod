using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Nitrate
{
    internal sealed class NitrateConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        [ReloadRequired]
        public bool ExperimentalTileRenderer { get; set; }
    }
}
