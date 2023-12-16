using JetBrains.Annotations;
using System;

namespace Nitrate;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Interface |
    AttributeTargets.Enum,
    Inherited = false
)]
internal sealed class ApiReleaseCandidateAttribute : Attribute
{
    [UsedImplicitly(ImplicitUseKindFlags.Access)]
    public string TargetVersion { get; }

    public ApiReleaseCandidateAttribute(string targetVersion)
    {
        TargetVersion = targetVersion;
    }
}