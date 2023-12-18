using JetBrains.Annotations;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace Nitrate;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class NitrateConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [ReloadRequired]
    [DefaultValue(true)]
    [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
    public bool ExperimentalTileRenderer { get; set; }

    [DefaultValue(false)]
    [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
    public bool ExperimentalTileRendererWarning { get; set; }

    /*[DefaultValue(true)]
    [UsedImplicitly(ImplicitUseKindFlags.Access | ImplicitUseKindFlags.Assign)]
    public bool FasterCursor { get; set; }*/
}