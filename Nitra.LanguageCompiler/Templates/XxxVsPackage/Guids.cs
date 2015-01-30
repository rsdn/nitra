﻿// Guids.cs
// MUST match guids.h
using System;

namespace XCompanyX.XxxVsPackage
{
    static class GuidList
    {
        public const string GuidXxxVsPackagePkgString    = "85C0A77E-A20E-4F36-A1E4-1EC513B41B0E";
        public const string GuidXxxVsPackageCmdSetString = "1E508B08-3BEF-4197-81C1-06B84A49F118";
        public const string GuidProject                  = "09CD39E9-5139-48B5-A1AE-B8EB59CEE1CD";

        public static readonly Guid GuidXxxVsPackageCmdSet = new Guid(GuidXxxVsPackageCmdSetString);
    };
}