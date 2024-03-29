﻿using System.CommandLine;
using FlowSynx.Abstractions;
using FlowSynx.Environment;
using FlowSynx.Net;
using EnsureThat;
using FlowSynx.Cli.Formatter;

namespace FlowSynx.Cli.Commands.Storage;

internal class ListCommand : BaseCommand<ListCommandOptions, ListCommandOptionsHandler>
{
    public ListCommand() : base("list", "List of entities regarding specific path")
    {
        var pathOption = new Option<string>("--path", "The path to get about") { IsRequired = true };
        var kindOption = new Option<string?>("--kind", getDefaultValue: () => nameof(ItemKind.FileAndDirectory), "Should apply format for byte size. Valid values are File, Directory, and FileAndDirectory.");
        var includeOption = new Option<string?>("--include", "Include entities matching pattern");
        var excludeOption = new Option<string?>("--exclude", "Exclude entities matching pattern");
        var minAgeOption = new Option<string?>("--min-age", "Filter entities older than this in s or suffix ms|s|m|h|d|w|M|y [default: off]");
        var maxAgeOption = new Option<string?>("--max-age", "Filter entities younger than this in s or suffix ms|s|m|h|d|w|M|y [default: off]");
        var minSizeOption = new Option<string?>("--min-size", "Filter entities bigger than this in KiB or suffix B|K|M|G|T|P [default: off]");
        var maxSizeOption = new Option<string?>("--max-size", "Filter entities smaller than this in KiB or suffix B|K|M|G|T|P [default: off]");
        var fullOption = new Option<bool?>("--full", getDefaultValue: () => false, "Full numbers instead of human-readable");
        var sortingOption = new Option<string?>("--sorting", "Sorting entities based on field name and ascending and descending. Like Property ASC, Property2 DESC [default: off]");
        var caseSensitiveOption = new Option<bool?>("--case-sensitive", getDefaultValue: () => false, "Ignore or apply case sensitive in filters");
        var recurseOption = new Option<bool?>("--recurse", getDefaultValue: () => false, "Apply recursion on filtering entities in the specified path");
        var maxResultsOption = new Option<int?>("--max-results", "The maximum number of results to return [default: off]");
        var outputOption = new Option<Output>("--output", getDefaultValue: () => Output.Json, "Formatting CLI output");

        AddOption(pathOption);
        AddOption(kindOption);
        AddOption(includeOption);
        AddOption(excludeOption);
        AddOption(minAgeOption);
        AddOption(maxAgeOption);
        AddOption(minSizeOption);
        AddOption(maxSizeOption);
        AddOption(fullOption);
        AddOption(sortingOption);
        AddOption(caseSensitiveOption);
        AddOption(recurseOption);
        AddOption(maxResultsOption);
        AddOption(outputOption);
    }
}

internal class ListCommandOptions : ICommandOptions
{
    public string Path { get; set; } = string.Empty;
    public string? Kind { get; set; } = nameof(ItemKind.FileAndDirectory);
    public string? Include { get; set; } = string.Empty;
    public string? Exclude { get; set; } = string.Empty;
    public string? MinAge { get; set; } = string.Empty;
    public string? MaxAge { get; set; } = string.Empty;
    public string? MinSize { get; set; } = string.Empty;
    public string? MaxSize { get; set; } = string.Empty;
    public bool? Full { get; set; } = false;
    public string? Sorting { get; set; } = string.Empty;
    public bool? CaseSensitive { get; set; } = false;
    public bool? Recurse { get; set; } = false;
    public int? MaxResults { get; set; }
    public Output Output { get; set; } = Output.Json;
}

internal class ListCommandOptionsHandler : ICommandOptionsHandler<ListCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IEndpoint _endpoint;
    private readonly IHttpRequestService _httpRequestService;

    public ListCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
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

    public async Task<int> HandleAsync(ListCommandOptions options, CancellationToken cancellationToken)
    {
        await _spinner.DisplayLineSpinnerAsync(async () => await Execute(options, cancellationToken));
        return 0;
    }

    private async Task Execute(ListCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            const string relativeUrl = "storage/list";
            var request = new ListRequest
            {
                Path = options.Path,
                Kind = options.Kind,
                Include = options.Include,
                Exclude = options.Exclude,
                MinAge = options.MinAge,
                MaxAge = options.MaxAge,
                MinSize = options.MinSize,
                MaxSize = options.MaxSize,
                CaseSensitive = options.CaseSensitive,
                Recurse = options.Recurse,
                Full = options.Full,
                Sorting = options.Sorting,
                MaxResults = options.MaxResults
            };

            var result = await _httpRequestService.PostRequestAsync<ListCommandOptions, Result<List<ListResponse>?>>($"{_endpoint.GetDefaultHttpEndpoint()}/{relativeUrl}", options, cancellationToken);

            if (result is {Succeeded: false})
            {
                _outputFormatter.WriteError(result.Messages);
            }
            else
            {
                if (result?.Data is not null)
                    _outputFormatter.Write(result.Data, options.Output);
                else
                    _outputFormatter.Write(result?.Messages);
            }
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
        }
    }
}

public class ListRequest
{
    public required string Path { get; set; }
    public string? Kind { get; set; } = nameof(ItemKind.FileAndDirectory);
    public string? Include { get; set; }
    public string? Exclude { get; set; }
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? MinSize { get; set; }
    public string? MaxSize { get; set; }
    public bool? Full { get; set; } = false;
    public string? Sorting { get; set; }
    public bool? CaseSensitive { get; set; } = false;
    public bool? Recurse { get; set; } = false;
    public int? MaxResults { get; set; }
}

public class ListResponse
{
    public string? Id { get; set; }
    public string? Kind { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string? Path { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? MimeType { get; set; }
    public DateTimeOffset? ModifiedTime { get; set; }
}