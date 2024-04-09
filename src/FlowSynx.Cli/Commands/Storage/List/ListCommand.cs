﻿using System.CommandLine;

namespace FlowSynx.Cli.Commands.Storage.List;

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
        var hashingOption = new Option<bool?>("--hashing", getDefaultValue: () => false, "Display hashing content in response data [default: off]");
        var maxResultsOption = new Option<int?>("--max-results", "The maximum number of results to return [default: off]");
        var showMetadataOption = new Option<bool?>("--show-metadata", getDefaultValue: () => false, "Display metadata in response data [default: off]");
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
        AddOption(hashingOption);
        AddOption(maxResultsOption);
        AddOption(showMetadataOption);
        AddOption(outputOption);
    }
}