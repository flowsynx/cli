﻿using System.CommandLine;
using FlowSynx.Environment;
using FlowSynx.Net;
using EnsureThat;
using FlowSynx.Cli.Formatter;
using FlowSynx.Cli.Common;

namespace FlowSynx.Cli.Commands.Storage;

internal class ReadCommand : BaseCommand<ReadCommandOptions, ReadCommandOptionsHandler>
{
    public ReadCommand() : base("read", "List of entities regarding specific path")
    {
        var pathOption = new Option<string>("--path", "The path to get about") { IsRequired = true };
        var savePathOption = new Option<string>("--save-to", "The path to get about") { IsRequired = true };
        var overWriteOption = new Option<bool?>("--overwrite", getDefaultValue: () => false, "The path to get about");

        AddOption(pathOption);
        AddOption(savePathOption);
        AddOption(overWriteOption);
    }
}

internal class ReadCommandOptions : ICommandOptions
{
    public string Path { get; set; } = string.Empty;
    public string SaveTo { get; set; } = string.Empty;
    public bool? Overwrite { get; set; } = false;
}

internal class ReadCommandOptionsHandler : ICommandOptionsHandler<ReadCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IEndpoint _endpoint;
    private readonly IHttpRequestService _httpRequestService;

    public ReadCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
        IEndpoint endpoint, IHttpRequestService httpRequestService)
    {
        EnsureArg.IsNotNull(outputFormatter, nameof(outputFormatter));
        EnsureArg.IsNotNull(spinner, nameof(spinner));
        EnsureArg.IsNotNull(endpoint, nameof(endpoint));
        EnsureArg.IsNotNull(httpRequestService, nameof(httpRequestService));

        _outputFormatter = outputFormatter;
        _spinner = spinner;
        _endpoint = endpoint;
        _httpRequestService = httpRequestService;
    }

    public async Task<int> HandleAsync(ReadCommandOptions options, CancellationToken cancellationToken)
    {
        await _spinner.DisplayLineSpinnerAsync(async () => await Execute(options, cancellationToken));
        return 0;
    }

    private async Task Execute(ReadCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            const string relativeUrl = "storage/read";
            var request = new ReadRequest { Path = options.Path };
            var result = await _httpRequestService.PostRequestAsync<ReadRequest>($"{_endpoint.GetDefaultHttpEndpoint()}/{relativeUrl}", request, cancellationToken);

            var filePath = options.SaveTo;
            if (Directory.Exists(filePath))
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(options.Path)}";
                filePath = Path.Combine(options.SaveTo, fileName);
            }

            if (!File.Exists(filePath) || (File.Exists(filePath) && options.Overwrite is true))
            {
                await StreamHelper.WriteStream(filePath, result, cancellationToken);
            }
            else
            {
                throw new Exception($"File '{filePath}' is already exist!");
            }
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
        }
    }
}

public class ReadRequest
{
    public required string Path { get; set; }
}
