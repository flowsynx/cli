﻿namespace FlowSynx.Cli.Formatter;

public interface IOutputFormatter
{
    void WriteError(string message);
    void WriteError(object data);
    void Write(string message);
    void Write<T>(T? data, Output output = Output.Json);
    void Write<T>(List<T>? data, Output output = Output.Json);
}