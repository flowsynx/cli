﻿namespace FlowCtl.Commands;

public interface ICommandOptionsHandler<in TOptions>
{
    Task<int> HandleAsync(TOptions options, CancellationToken cancellationToken);
}