﻿namespace FlowCtl.Commands.Plugins;

internal class PluginsCommandOptions : ICommandOptions
{
    public string Type { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public Output Output { get; set; } = Output.Json;
}