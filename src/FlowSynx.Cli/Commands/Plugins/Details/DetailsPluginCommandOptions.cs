﻿namespace FlowSynx.Cli.Commands.Plugins.Details;

internal class DetailsPluginCommandOptions : ICommandOptions
{
    public Guid Id { get; set; } = Guid.Empty;
    public Output Output { get; set; } = Output.Json;
}