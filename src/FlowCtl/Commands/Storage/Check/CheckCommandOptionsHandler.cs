﻿using EnsureThat;
using FlowCtl.Services.Abstracts;
using FlowSynx.Client;
using FlowSynx.Client.Requests.Storage;

namespace FlowCtl.Commands.Storage.Check;

internal class CheckCommandOptionsHandler : ICommandOptionsHandler<CheckCommandOptions>
{
    private readonly IOutputFormatter _outputFormatter;
    private readonly ISpinner _spinner;
    private readonly IFlowSynxClient _flowSynxClient;

    public CheckCommandOptionsHandler(IOutputFormatter outputFormatter, ISpinner spinner,
        IFlowSynxClient flowSynxClient)
    {
        EnsureArg.IsNotNull(outputFormatter, nameof(outputFormatter));
        EnsureArg.IsNotNull(spinner, nameof(spinner));
        EnsureArg.IsNotNull(flowSynxClient, nameof(flowSynxClient));

        _outputFormatter = outputFormatter;
        _spinner = spinner;
        _flowSynxClient = flowSynxClient;
    }

    public async Task<int> HandleAsync(CheckCommandOptions options, CancellationToken cancellationToken)
    {
        await _spinner.DisplayLineSpinnerAsync(async () => await Execute(options, cancellationToken));
        return 0;
    }

    private async Task Execute(CheckCommandOptions options, CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrEmpty(options.Address))
                _flowSynxClient.ChangeConnection(options.Address);

            var request = new CheckRequest()
            {
                SourcePath = options.SourcePath,
                DestinationPath = options.DestinationPath,
                Include = options.Include,
                Exclude = options.Exclude,
                MinAge = options.MinAge,
                MaxAge = options.MaxAge,
                MinSize = options.MinSize,
                MaxSize = options.MaxSize,
                CaseSensitive = options.CaseSensitive,
                Recurse = options.Recurse,
                CheckSize = options.CheckSize,
                CheckHash = options.CheckHash,
                OneWay = options.OneWay
            };

            var result = await _flowSynxClient.Check(request, cancellationToken);
            if (result is { Succeeded: false })
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