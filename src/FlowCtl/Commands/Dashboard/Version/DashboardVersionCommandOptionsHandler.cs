﻿using EnsureThat;
using FlowCtl.Services.Abstracts;

namespace FlowCtl.Commands.Dashboard.Version;

internal class DashboardVersionCommandOptionsHandler : ICommandOptionsHandler<DashboardVersionCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IVersionHandler _versionHandler;
    private readonly ILocation _location;

    public DashboardVersionCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
         IVersionHandler versionHandler, ILocation location)
    {
        EnsureArg.IsNotNull(outputFormatter, nameof(outputFormatter));
        EnsureArg.IsNotNull(spinner, nameof(spinner));
        EnsureArg.IsNotNull(versionHandler, nameof(versionHandler));
        EnsureArg.IsNotNull(location, nameof(location));

        _outputFormatter = outputFormatter;
        _spinner = spinner;
        _versionHandler = versionHandler;
        _location = location;
    }

    public async Task<int> HandleAsync(DashboardVersionCommandOptions options, CancellationToken cancellationToken)
    {
        await _spinner.DisplayLineSpinnerAsync(() => Execute(options));
        return 0;
    }

    private Task Execute(DashboardVersionCommandOptions options)
    {
        try
        {
            var fullVersion = new
            {
                Version = GetDashboardVersion()
            };

            _outputFormatter.Write(fullVersion, options.Output);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
            return Task.CompletedTask;
        }
    }
    
    private string GetDashboardVersion()
    {
        var dashboardBinaryFile = _location.LookupDashboardBinaryFilePath(Path.Combine(_location.DefaultFlowSynxBinaryDirectoryName, "dashboard"));
        var dashboardVersion = _versionHandler.GetApplicationVersion(dashboardBinaryFile);
        return string.IsNullOrEmpty(dashboardVersion) ? Resources.VersionCommandNotInitialized : dashboardVersion;
    }
}