﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Microsoft.Extensions.Http.Resilience.Internal.Validators;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HedgingHttpClientBuilderExtensions
{
    private const string StandardHandlerPostfix = "standard-hedging";
    private const string StandardInnerHandlerPostfix = "standard-hedging-endpoint";

    /// <summary>
    /// Adds a standard hedging handler which wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="configure">Configures the routing strategy associated with this handler.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pipeline pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    ///
    /// It is recommended that you configure the way the pipelines are selected by calling 'SelectPipelineByAuthority' extensions on top of returned <see cref="IStandardHedgingHandlerBuilder"/>.
    ///
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the policies inside the pipeline.
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder, Action<IRoutingStrategyBuilder> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        var hedgingBuilder = builder.AddStandardHedgingHandler();

        configure(hedgingBuilder.RoutingStrategyBuilder);

        return hedgingBuilder;
    }

    /// <summary>
    /// Adds a standard hedging handler which wraps the execution of the request with a standard hedging mechanism.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>
    /// A <see cref="IStandardHedgingHandlerBuilder"/> builder that can be used to configure the standard hedging behavior.
    /// </returns>
    /// <remarks>
    /// The standard hedging uses a pipeline pool of circuit breakers to ensure that unhealthy endpoints are not hedged against.
    /// By default, the selection from pool is based on the URL Authority (scheme + host + port).
    ///
    /// It is recommended that you configure the way the pipelines are selected by calling 'SelectPipelineByAuthority' extensions on top of returned <see cref="IStandardHedgingHandlerBuilder"/>.
    ///
    /// See <see cref="HttpStandardHedgingResilienceOptions"/> for more details about the policies inside the pipeline.
    /// </remarks>
    public static IStandardHedgingHandlerBuilder AddStandardHedgingHandler(this IHttpClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        var optionsName = builder.Name;
        var routingBuilder = new RoutingStrategyBuilder(builder.Name, builder.Services);

        _ = builder.Services.AddValidatedOptions<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsValidator>(optionsName);
        _ = builder.Services.AddValidatedOptions<HttpStandardHedgingResilienceOptions, HttpStandardHedgingResilienceOptionsCustomValidator>(optionsName);
        _ = builder.Services.AddRequestCloner();

        // configure outer handler
        var outerHandler = builder.AddResilienceHandler(StandardHandlerPostfix);
        _ = outerHandler.AddRoutingPolicy(serviceProvider => serviceProvider.GetRoutingFactory(routingBuilder.Name));
        _ = outerHandler.AddRequestMessageSnapshotPolicy();
        _ = outerHandler.AddPolicy((builder, serviceProvider) =>
        {
            var options = GetOptions(serviceProvider);
            var hedgedTaskProvider = CreateHedgedTaskProvider(outerHandler.PipelineName);

            _ = builder
                .AddTimeoutPolicy(StandardHedgingPolicyNames.TotalRequestTimeout, options.TotalRequestTimeoutOptions)
                .AddHedgingPolicy(StandardHedgingPolicyNames.Hedging, hedgedTaskProvider, options.HedgingOptions);
        });

        // configure inner handler
        var innerBuilder = builder.AddResilienceHandler(StandardInnerHandlerPostfix);
        _ = innerBuilder.SelectPipelineByAuthority(new DataClassification("FIXME", 1));
        _ = innerBuilder.AddPolicy((builder, serviceProvider) =>
        {
            var options = GetOptions(serviceProvider).EndpointOptions;

            _ = builder
                .AddBulkheadPolicy(StandardHedgingPolicyNames.Bulkhead, options.BulkheadOptions)
                .AddCircuitBreakerPolicy(StandardHedgingPolicyNames.CircuitBreaker, options.CircuitBreakerOptions)
                .AddTimeoutPolicy(StandardHedgingPolicyNames.AttemptTimeout, options.TimeoutOptions);
        });

        return new StandardHedgingHandlerBuilder(builder.Name, builder.Services, routingBuilder, innerBuilder);

        HttpStandardHedgingResilienceOptions GetOptions(IServiceProvider serviceProvider)
            => serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(optionsName);
    }
}
