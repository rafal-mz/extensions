﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace System.Cloud.Messaging.Internal;

/// <summary>
/// Log utilities.
/// </summary>
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Used for EventId.")]
internal static partial class Log
{
    [LogMethod(0, LogLevel.Warning, "{messageSource} failed during reading message.")]
    public static partial void MessageSourceFailedDuringReadingMessage(ILogger logger, string messageSource, Exception messageFetchException);

    [LogMethod(1, LogLevel.Warning, "{messageSource} returned null MessageContext.")]
    public static partial void MessageSourceReturnedNullMessageContext(ILogger logger, string messageSource);

    [LogMethod(2, LogLevel.Warning, "Handling message procesing completion failed.")]
    public static partial void ExceptionOccuredDuringHandlingMessageProcessingCompletion(ILogger logger, Exception handlerException);

    [LogMethod(3, LogLevel.Warning, "Handling message procesing failure failed with {handlerException}.")]
    public static partial void ExceptionOccuredDuringHandlingMessageProcessingFailure(ILogger logger,
                                                                                      Exception processingException,
                                                                                      Exception handlerException);

    [LogMethod(4, LogLevel.Warning, "{messageprocessingStateHandler} failed during releasing context.")]
    public static partial void MessageSourceFailedDuringReleasingContext(ILogger logger,
                                                                         string messageprocessingStateHandler,
                                                                         Exception releaseException);
}
