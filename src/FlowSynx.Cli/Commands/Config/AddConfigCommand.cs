﻿using System.CommandLine;
using FlowSynx.Abstractions;
using FlowSynx.Environment;
using FlowSynx.IO.Serialization;
using FlowSynx.Net;
using EnsureThat;
using FlowSynx.Cli.Formatter;
using FlowSynx.IO.Exceptions;

namespace FlowSynx.Cli.Commands.Config;

internal class AddConfigCommand : BaseCommand<AddConfigCommandOptions, AddConfigCommandOptionsHandler>
{
    public AddConfigCommand() : base("add", "Add configuration section")
    {
        var nameOption = new Option<string>("--name", "The unique configuration section name") { IsRequired = true };
        var typeOption = new Option<string>("--type", "The type of plugin") { IsRequired = true };
        var specificationsOption = new Option<string>( "--spec", "The specifications regarding configuration section. They should be passed in pairs of key value");

        AddOption(nameOption);
        AddOption(typeOption);
        AddOption(specificationsOption);
    }
}

internal class AddConfigCommandOptions : ICommandOptions
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Spec { get; set; } = string.Empty;
}

internal class AddConfigCommandOptionsHandler : ICommandOptionsHandler<AddConfigCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IEndpoint _endpoint;
    private readonly IHttpRequestService _httpRequestService;
    private readonly IDeserializer _deserializer;

    public AddConfigCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
        IEndpoint endpoint, IHttpRequestService httpRequestService, IDeserializer deserializer)
    {
        EnsureArg.IsNotNull(outputFormatter, nameof(outputFormatter));
        EnsureArg.IsNotNull(spinner, nameof(spinner));
        EnsureArg.IsNotNull(endpoint, nameof(endpoint));
        EnsureArg.IsNotNull(httpRequestService, nameof(httpRequestService));
        EnsureArg.IsNotNull(deserializer, nameof(deserializer));

        _outputFormatter = outputFormatter;
        _spinner = spinner;
        _endpoint = endpoint;
        _httpRequestService = httpRequestService;
        _deserializer = deserializer;
    }

    public async Task<int> HandleAsync(AddConfigCommandOptions options, CancellationToken cancellationToken)
    {
        await _spinner.DisplayLineSpinnerAsync(async () => await Execute(options, cancellationToken));
        return 0;
    }

    private async Task Execute(AddConfigCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            const string relativeUrl = "config/add";

            var specification = new Dictionary<string, string?>();
            if (!string.IsNullOrEmpty(options.Spec))
            {
                specification = _deserializer.Deserialize<Dictionary<string, string?>>(options.Spec);
            }

            var request = new AddConfigRequest {Name = options.Name, Type = options.Type, Specifications = specification};
            var result = await _httpRequestService.PostRequestAsync<AddConfigRequest, Result<AddConfigResponse?>>($"{_endpoint.GetDefaultHttpEndpoint()}/{relativeUrl}", request, cancellationToken);

            if (result is {Succeeded: false})
                _outputFormatter.WriteError(result.Messages);
            else
                _outputFormatter.Write(result?.Data);
        }
        catch (DeserializerException)
        {
            _outputFormatter.WriteError("Could not parse the entered specifications. Please check it and try again!");
        }
        catch (Exception ex)
        {
            _outputFormatter.WriteError(ex.Message);
        }
    }
}

public class AddConfigRequest
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public Dictionary<string, string?>? Specifications { get; set; }
}

public class AddConfigResponse
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
}