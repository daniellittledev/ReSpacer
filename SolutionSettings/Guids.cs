// Guids.cs
// MUST match guids.h
using System;

namespace Enexure.SolutionSettings
{
    static class GuidList
    {
        public const string guidSolutionSettingsPkgString = "7e5931da-cc33-4000-af07-9498b9a84a19";
        public const string guidSolutionSettingsCmdSetString = "eddb2579-fbce-4900-b7d5-ed9813b0c206";

        public static readonly Guid guidSolutionSettingsCmdSet = new Guid(guidSolutionSettingsCmdSetString);
    };
}