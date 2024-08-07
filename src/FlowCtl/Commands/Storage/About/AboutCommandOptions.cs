﻿namespace FlowCtl.Commands.Storage.About;

internal class AboutCommandOptions : ICommandOptions
{
    public string? Path { get; set; } = string.Empty;
    public bool? Full { get; set; } = false;
    public string? Address { get; set; } = string.Empty;
    public Output Output { get; set; } = Output.Json;
}