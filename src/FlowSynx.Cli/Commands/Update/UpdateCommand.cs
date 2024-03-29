﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using FlowSynx.Environment;
using EnsureThat;
using FlowSynx.Cli.Formatter;
using System.Security.Cryptography;
using System.Text;
using FlowSynx.Cli.Services;
using FlowSynx.IO.Compression;
using FlowSynx.IO.Serialization;
using System.CommandLine;
using FlowSynx.Cli.Common;

namespace FlowSynx.Cli.Commands.Update;

internal class UpdateCommand : BaseCommand<UpdateCommandOptions, UpdateCommandOptionsHandler>
{
    public UpdateCommand() : base("update", "Update FlowSynx system and Cli")
    {
        var forceOption = new Option<bool>("--force" , getDefaultValue: () => false, description: "Force terminate FlowSynx system if it is running");

        AddOption(forceOption);
    }
}

internal class UpdateCommandOptions : ICommandOptions
{
    public bool Force { get; set; }
}

internal class UpdateCommandOptionsHandler : ICommandOptionsHandler<UpdateCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IVersion _version;
    private readonly IOperatingSystemInfo _operatingSystemInfo;
    private readonly IZipFile _zipFile;
    private readonly IGZipFile _gZipFile;
    private readonly IDeserializer _deserializer;
    private readonly ILocation _location;

    public UpdateCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
        IVersion version, IOperatingSystemInfo operatingSystemInfo, IZipFile zipFile, IGZipFile gZipFile,
        IDeserializer deserializer, ILocation location)
    {
        EnsureArg.IsNotNull(outputFormatter, nameof(outputFormatter));
        EnsureArg.IsNotNull(spinner, nameof(spinner));
        EnsureArg.IsNotNull(version, nameof(version));
        EnsureArg.IsNotNull(operatingSystemInfo, nameof(operatingSystemInfo));
        EnsureArg.IsNotNull(zipFile, nameof(zipFile));
        EnsureArg.IsNotNull(gZipFile, nameof(gZipFile));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));

        _outputFormatter = outputFormatter;
        _spinner = spinner;
        _version = version;
        _operatingSystemInfo = operatingSystemInfo;
        _zipFile = zipFile;
        _gZipFile = gZipFile;
        _deserializer = deserializer;
        _location = location;
    }

    public async Task<int> HandleAsync(UpdateCommandOptions options, CancellationToken cancellationToken)
    {
        DownloadUpdateResult result = new();
        await _spinner.DisplayLineSpinnerAsync(async () => result = await DownloadAndValidate(options, cancellationToken));

        if (result.Success)
        {
            try
            {
                await Task
                    .Run(() => ExtractFlowSynx(result.FlowSynxDownloadPath, cancellationToken), cancellationToken)
                    .ContinueWith(t =>
                    {
                        if (t.IsCanceled || t.IsFaulted)
                        {
                            var exception = t.Exception;
                            throw new Exception(exception?.Message);
                        }
                        ExtractFlowSynxCli(result.CliDownloadPath, cancellationToken);
                    }, cancellationToken);
            }
            catch (Exception ex)
            {
                _outputFormatter.WriteError(ex.Message);
            }
        }
        return 0;
    }

    private async Task<DownloadUpdateResult> DownloadAndValidate(UpdateCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            _outputFormatter.Write("Checking for updates...");

            if (options.Force)
            {
                ProcessHelper.TerminateProcess("flowsynx", ".");
                _outputFormatter.Write("The FlowSynx system was stopped successfully.");
            }
            else
            {
                if (ProcessHelper.IsProcessRunning("flowsynx", "."))
                {
                    _outputFormatter.Write("The FlowSynx engine is running. Please stop it by run the command: 'Synx stop', and try update again.");
                    return new DownloadUpdateResult { Success = false };
                }
            }

            var latestVersion = await GetLatestVersion(GitHubHelper.CliRepository);
            var currentVersion = $"v{_version.Version}";

            if (IsUpdateAvailable(latestVersion, currentVersion))
            {
                _outputFormatter.Write($"An update is available! Version {latestVersion}");
                _outputFormatter.Write("Beginning update...");

                var flowSynxDownloadPath = await DownloadFlowSynxAsset(latestVersion, Path.GetTempPath(), cancellationToken);
                var isFlowSynxValid = await ValidateFlowSynxDownloadedAsset(flowSynxDownloadPath, latestVersion, cancellationToken);

                var cliDownloadPath = await DownloadCliAsset(latestVersion, Path.GetTempPath(), cancellationToken);
                var isCliValid = await ValidateCliDownloadedAsset(cliDownloadPath, latestVersion, cancellationToken);

                if (isFlowSynxValid && isCliValid)
                {
                    return new DownloadUpdateResult
                    {
                        Success = true,
                        FlowSynxDownloadPath = flowSynxDownloadPath,
                        CliDownloadPath = cliDownloadPath
                    };
                }

                _outputFormatter.Write("Validating download - Fail!");
                _outputFormatter.Write("The downloaded data may has been corrupted!");
                return new DownloadUpdateResult { Success = false };
            }

            _outputFormatter.Write("The current version is up to dated");
            return new DownloadUpdateResult { Success = false };
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
            return new DownloadUpdateResult { Success = false };
        }
    }

    private async Task<bool> ValidateFlowSynxDownloadedAsset(string path, string latestVersion, CancellationToken cancellationToken)
    {
        var expectedHash = await DownloadFlowSynxHashAsset(latestVersion, cancellationToken);
        var downloadHash = HashHelper.ComputeSha256Hash(path);

        if (string.Equals(downloadHash.Trim(), expectedHash.Trim(), StringComparison.CurrentCultureIgnoreCase))
            return true;

        File.Delete(path);
        return false;
    }

    private Task ExtractFlowSynx(string sourcePath, CancellationToken cancellationToken)
    {
        var enginePath = Path.Combine(PathHelper.DefaultFlowSynxDirectoryName, "engine");
        var extractTarget = Path.Combine(enginePath, "downloadedFiles");

        ExtractFile(sourcePath, extractTarget);
        File.Delete(sourcePath);

        Directory.CreateDirectory(enginePath);

        foreach (var newPath in Directory.GetFiles(extractTarget, "*.*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            File.Copy(newPath, newPath.Replace(extractTarget, enginePath), true);
        }

        Directory.Delete(extractTarget, true);
        return Task.CompletedTask;
    }

    private async Task<bool> ValidateCliDownloadedAsset(string path, string latestVersion, CancellationToken cancellationToken)
    {
        var expectedHash = await DownloadCliHashAsset(latestVersion, cancellationToken);
        var downloadHash = HashHelper.ComputeSha256Hash(path);

        if (string.Equals(downloadHash.Trim(), expectedHash.Trim(), StringComparison.CurrentCultureIgnoreCase))
            return true;

        File.Delete(path);
        return false;
    }

    private Task ExtractFlowSynxCli(string sourcePath, CancellationToken cancellationToken)
    {
        const string extractTarget = "./downloadedFiles";
        ExtractFile(sourcePath, extractTarget);
        File.Delete(sourcePath);

        var synxUpdateExeFile = Path.GetFullPath(PathHelper.LookupSynxBinaryFilePath(extractTarget));
        var files = Directory
                                    .GetFiles(extractTarget, "*.*", SearchOption.AllDirectories)
                                    .Where(name => !string.Equals(Path.GetFullPath(name), synxUpdateExeFile, StringComparison.InvariantCultureIgnoreCase));

        foreach (var newPath in files)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            File.Copy(newPath, newPath.Replace(extractTarget, "."), true);
        }

        var synxExeFile = Path.GetFullPath(PathHelper.LookupSynxBinaryFilePath(_location.RootLocation));
        SelfUpdate(synxUpdateExeFile, synxExeFile);
        return Task.CompletedTask;
    }

    private async Task<string> GetLatestVersion(string repositoryName)
    {
        var httpClient = new HttpClient();
        var uri = $"https://api.github.com/repos/{GitHubHelper.Organization}/{repositoryName}/tags";
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request");

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(uri)
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var tags = _deserializer.Deserialize<List<GitHubTag>>(responseBody);
        return tags.Count > 0 ? tags[0].Name : string.Empty;
    }

    private async Task<string> DownloadFlowSynxAsset(string version, string destinationPath, CancellationToken cancellationToken)
    {
        var uri = $"https://github.com/{GitHubHelper.Organization}/{GitHubHelper.FlowSynxRepository}/releases/download/v{version}/{FlowSynxArchiveFileName}";
        var stream = await NetHelper.DownloadFile(uri, cancellationToken);
        var path = Path.Combine(destinationPath, FlowSynxArchiveFileName);
        StreamHelper.SaveStreamToFile(stream, path);
        return path;
    }

    private async Task<string> DownloadCliAsset(string version, string destinationPath, CancellationToken cancellationToken)
    {
        var uri = $"https://github.com/{GitHubHelper.Organization}/{GitHubHelper.CliRepository}/releases/download/v{version}/{FlowSynxCliArchiveFileName}";
        var stream = await NetHelper.DownloadFile(uri, cancellationToken);
        var path = Path.Combine(destinationPath, FlowSynxCliArchiveFileName);
        StreamHelper.SaveStreamToFile(stream, path);
        return path;
    }

    private async Task<string> DownloadFlowSynxHashAsset(string version, CancellationToken cancellationToken)
    {
        var uri = $"https://github.com/{GitHubHelper.Organization}/{GitHubHelper.FlowSynxRepository}/releases/download/v{version}/{FlowSynxArchiveHashFileName}";
        var stream = await NetHelper.DownloadFile(uri, cancellationToken);
        return await HashHelper.GetAssetHashCode(stream, cancellationToken);
    }

    private async Task<string> DownloadCliHashAsset(string version, CancellationToken cancellationToken)
    {
        var uri = $"https://github.com/{GitHubHelper.Organization}/{GitHubHelper.CliRepository}/releases/download/v{version}/{FlowSynxCliArchiveHashFileName}";
        var stream = await NetHelper.DownloadFile(uri, cancellationToken);
        return await HashHelper.GetAssetHashCode(stream, cancellationToken);
    }

    private string FlowSynxArchiveFileName => $"flowsynx-{ArchiveName.ToLower()}";
    private string FlowSynxArchiveHashFileName => $"flowsynx-{ArchiveName.ToLower()}.sha256";
    private string FlowSynxCliArchiveFileName => $"synx-{ArchiveName.ToLower()}";
    private string FlowSynxCliArchiveHashFileName => $"synx-{ArchiveName.ToLower()}.sha256";
    private string ArchiveName => $"{_operatingSystemInfo.Type}-{_operatingSystemInfo.Architecture}.{Extension}";
    private string Extension => string.Equals(_operatingSystemInfo.Type, "windows", StringComparison.OrdinalIgnoreCase) ? "zip" : "tar.gz";
    
    private static bool IsUpdateAvailable(string latestVersion, string currentVersion)
    {
        if (string.IsNullOrEmpty(latestVersion)) return false;

        var current = new System.Version(currentVersion[1..]);
        var latest = new System.Version(latestVersion[1..]);
        return latest > current;
    }

    private void ExtractFile(string sourcePath, string destinationPath)
    {
        if (Extension == "tar.gz")
            _gZipFile.Decompression(sourcePath, destinationPath, true);
        else
            _zipFile.Decompression(sourcePath, destinationPath, true);
    }

    private void SelfUpdate(string updateFile, string selfFile)
    {
        var stream = File.OpenRead(updateFile);

        var selfFileName = Path.GetFileName(selfFile);
        var directoryName = Path.GetDirectoryName(selfFile);
        var downloadedFilesPath = Path.GetDirectoryName(updateFile);
        if (string.IsNullOrEmpty(directoryName))
            return;

        string selfWithoutExt = Path.Combine(directoryName, Path.GetFileNameWithoutExtension(selfFile));
        StreamHelper.SaveStreamToFile(stream, selfWithoutExt + GetUpdateFilePath());

        string updateExeFile = selfWithoutExt + GetUpdateFilePath();
        string scriptFile = selfWithoutExt + GetScriptFilePath();

        var updateScript = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            string.Format(Resources.UpdateScript_Bat, selfFileName, updateExeFile, selfFile, updateExeFile, downloadedFilesPath) :
            string.Format(Resources.UpdateScript_Shell, updateExeFile, selfFile, selfFileName, updateExeFile, downloadedFilesPath);

        StreamWriter streamWriter = new(File.Create(scriptFile));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            streamWriter.NewLine = "\n";

        streamWriter.Write(updateScript);
        streamWriter.Close();

        ProcessStartInfo startInfo = new(scriptFile)
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = directoryName
        };

        try
        {
            Process.Start(startInfo);
            _outputFormatter.Write("The FlowSynx system updated successfully.");
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
            System.Environment.Exit(0);
        }
    }
    
    private static string GetUpdateFilePath()
    {
        var binFileName = "Update";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            binFileName += ".exe";

        return binFileName;
    }

    private static string GetScriptFilePath()
    {
        var scriptFileName = "Update.sh";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            scriptFileName = "Update.bat";

        return scriptFileName;
    }

    private class DownloadUpdateResult
    {
        public bool Success { get; init; }
        public string FlowSynxDownloadPath { get; init; } = string.Empty;
        public string CliDownloadPath { get; init; } = string.Empty;
    }

    private class GitHubTag
    {
        public required string Name { get; set; }
    }
}