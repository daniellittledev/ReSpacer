// Guids.cs
// MUST match guids.h
using System;

namespace Enexure.SolutionSettings
{
    static class GuidList
    {
        public const string guidSolutionSettingsPkgString = "857D56EC-07D2-4565-9A2B-AFDBFA25D2FA";
        public const string guidSolutionSettingsCmdSetString = "FB6AAA41-0571-4BFD-88F1-6782DC2E1C10"; //eddb2579-fbce-4900-b7d5-ed9813b0c206

        public static readonly Guid guidSolutionSettingsCmdSet = new Guid(guidSolutionSettingsCmdSetString);
    };
}