﻿namespace FlowSynx.Cli.Commands.Plugins.Details;

internal class PluginDetailsCommandOptions : ICommandOptions
{
    public Guid Id { get; set; } = Guid.Empty;
    public string? Address { get; set; } = string.Empty;
    public Output Output { get; set; } = Output.Json;
}