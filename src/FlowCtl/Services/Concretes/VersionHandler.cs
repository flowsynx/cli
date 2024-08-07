﻿using System.Diagnostics;
using System.Reflection;
using FlowCtl.Services.Abstracts;

namespace FlowCtl.Services.Concretes;

public class VersionHandler : IVersionHandler
{
    public string Version
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
        }
    }

    public string GetApplicationVersion(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return "";

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return versionInfo.FileVersion ?? "";
    }

    public bool CheckVersions(string latestVersion, string currentVersion)
    {
        if (string.IsNullOrEmpty(latestVersion)) return false;

        var current = new Version(currentVersion);
        var latest = new Version(latestVersion);
        return latest > current;
    }

    public string Normalize(string? version)
    {
        if (string.IsNullOrEmpty(version))
            return string.Empty;

        return version.StartsWith("v") ? version[1..] : version;
    }
}