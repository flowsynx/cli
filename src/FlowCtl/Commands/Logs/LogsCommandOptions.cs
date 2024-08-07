﻿using FlowSynx.Logging;

namespace FlowCtl.Commands.Logs;

internal class LogsCommandOptions : ICommandOptions
{
    public string? MinAge { get; set; }
    public string? MaxAge { get; set; }
    public string? Level { get; set; }
    public string ExportTo { get; set; } = string.Empty;
    public string? Address { get; set; } = string.Empty;
    public Output Output { get; set; } = Output.Json;
}