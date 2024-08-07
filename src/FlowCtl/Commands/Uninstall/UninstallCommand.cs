﻿using System.CommandLine;

namespace FlowCtl.Commands.Uninstall;

internal class UninstallCommand : BaseCommand<UninstallCommandOptions, UninstallCommandOptionsHandler>
{
    public UninstallCommand() : base("uninstall", Resources.UninstallCommandDescription)
    {
        var forceOption = new Option<bool>(new[] { "-f", "--force" }, 
            getDefaultValue :() => false, 
            description: Resources.CommandForceOption);

        AddOption(forceOption);
    }
}