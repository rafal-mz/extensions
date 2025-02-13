﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Metering;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.FaultInjection.Test.Internals;

public class ChaosPolicyFactoryTest
{
    private readonly string _testOptionsGroupName = "TestGroupName";
    private readonly ChaosPolicyOptionsGroup _testChaosPolicyOptionsGroup;
    private readonly IChaosPolicyFactory _testPolicyFactory;
    private readonly string _testExceptionKey = "TestExceptionKey";
    private readonly InjectedFaultException _testException;

    public ChaosPolicyFactoryTest()
    {
        _testChaosPolicyOptionsGroup = new ChaosPolicyOptionsGroup
        {
            LatencyPolicyOptions = new LatencyPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 0.3
            },
            HttpResponseInjectionPolicyOptions = new HttpResponseInjectionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 0.4
            },
            ExceptionPolicyOptions = new ExceptionPolicyOptions
            {
                Enabled = true,
                FaultInjectionRate = 0.5,
                ExceptionKey = _testExceptionKey
            }
        };
        _testException = new InjectedFaultException();

        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetering()
            .AddFaultInjection(builder => builder.Configure(
            options =>
            {
                options.ChaosPolicyOptionsGroups.Add(_testOptionsGroupName, _testChaosPolicyOptionsGroup);
            })
        .AddException(_testExceptionKey, _testException));

        using var provider = services.BuildServiceProvider();
        _testPolicyFactory = provider.GetRequiredService<IChaosPolicyFactory>();
    }

    [Fact]
    public void CreateInjectLatencyPolicy_WithDelegateFunctions_ShouldReturnInstance()
    {
        var policy = _testPolicyFactory.CreateLatencyPolicy<string>();
        Assert.NotNull(policy);
    }

    [Fact]
    public void CreateInjectExceptionPolicy_WithDelegateFunctions_ShouldReturnInstance()
    {
        var policy = _testPolicyFactory.CreateExceptionPolicy();
        Assert.NotNull(policy);
    }

    [Fact]
    public async Task GetEnabledAsync_WhenNoOptionsGroupNameFound_ShouldReturnFalse()
    {
        var context = new Context();

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledAsync_WhenNoOptionsGroupFound_ShouldReturnFalse()
    {
        var context = new Context();
        context.WithFaultInjection("RandomName");

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledAsync_ForLatencyPolicyOptions_ShouldReturnEnabled()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.LatencyPolicyOptions!.Enabled, result);
    }

    [Fact]
    public async Task GetEnabledAsync_ForLatencyPolicyOptions_WhenNoLatencyPolicyFoundInOptionsGroup_ShouldReturnFalse()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetering()
            .AddFaultInjection(builder => builder.Configure(
            options =>
            {
                options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions);
            }));

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((ChaosPolicyFactory)testPolicyFactory).GetEnabledAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetEnabledAsync_ForExceptionPolicyOptions_ShouldReturnEnabled()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetEnabledAsync<ExceptionPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.ExceptionPolicyOptions!.Enabled, result);
    }

    [Fact]
    public async Task GetEnabledAsync_ForExceptionPolicyOptions_WhenNoExceptionPolicyFoundInOptionsGroup_ShouldReturnFalse()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetering()
            .AddFaultInjection(builder => builder.Configure(
            options =>
            {
                options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions);
            }));

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((ChaosPolicyFactory)testPolicyFactory).GetEnabledAsync<ExceptionPolicyOptions>(context, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_WhenNoOptionsGroupNameFound_ShouldReturnZero()
    {
        var context = new Context();

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_WhenNoOptionsGroupFound_ShouldReturnZero()
    {
        var context = new Context();
        context.WithFaultInjection("RandomName");

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ForLatencyPolicyOptions_ShouldReturnInjectionRate()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.LatencyPolicyOptions!.FaultInjectionRate, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ForLatencyPolicyOptions_WhenNoLatencyPolicyFoundInOptionsGroup_ShouldReturnZero()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetering()
            .AddFaultInjection(builder => builder.Configure(
            options =>
            {
                options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions);
            }));

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((ChaosPolicyFactory)testPolicyFactory).GetInjectionRateAsync<LatencyPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ForExceptionPolicyOptions_ShouldReturnInjectionRate()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetInjectionRateAsync<ExceptionPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.ExceptionPolicyOptions!.FaultInjectionRate, result);
    }

    [Fact]
    public async Task GetInjectionRateAsync_ForExceptionPolicyOptions_WhenNoExceptionPolicyFoundInOptionsGroup_ShouldReturnZero()
    {
        var testGroupName = "TestGroup";
        var tesOptionsGroupNoPolicyOptions = new ChaosPolicyOptionsGroup();
        var services = new ServiceCollection();
        services
            .AddLogging()
            .RegisterMetering()
            .AddFaultInjection(builder => builder.Configure(
            options =>
            {
                options.ChaosPolicyOptionsGroups.Add(testGroupName, tesOptionsGroupNoPolicyOptions);
            }));

        using var provider = services.BuildServiceProvider();
        var testPolicyFactory = provider.GetRequiredService<IChaosPolicyFactory>();

        var context = new Context();
        context.WithFaultInjection(testGroupName);

        var result = await ((ChaosPolicyFactory)testPolicyFactory).GetInjectionRateAsync<ExceptionPolicyOptions>(context, CancellationToken.None);
        Assert.Equal(0.0, result);
    }

    [Fact]
    public async Task GetLatencyAsync_ShouldReturnLatency()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetLatencyAsync(context, CancellationToken.None);
        Assert.Equal(_testChaosPolicyOptionsGroup!.LatencyPolicyOptions!.Latency, result);
    }

    [Fact]
    public async Task GetExceptionAsync_ShouldReturnExceptionInstance()
    {
        var context = new Context();
        context.WithFaultInjection(_testOptionsGroupName);

        var result = await ((ChaosPolicyFactory)_testPolicyFactory).GetExceptionAsync(context, CancellationToken.None);
        Assert.Equal(_testException, result);
    }
}
