using AutoFixture.AutoMoq;
using AutoFixture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Xunit;
using Arc4u.Caching.Memory;
using System.Globalization;
using FluentAssertions;
using Moq;
using Arc4u.Serializer;
using Arc4u.Dependency;
using System;
using Arc4u.Configuration.Memory;

namespace Arc4u.Standard.UnitTest.Caching;

[Trait("Category", "CI")]
public class MemoryTest
{
    public MemoryTest()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    private readonly Fixture _fixture;

    [Fact]
    public void AddOptionByCodeToServiceCollectionShould()
    {
        // arrange
        var option1 = _fixture.Create<MemoryCacheOption>();

        IServiceCollection services = new ServiceCollection();

        services.AddMemoryCache("option1", options =>
        {
            options.SizeLimit = option1.SizeLimit;
            options.SerializerName = option1.SerializerName;
            options.CompactionPercentage = option1.CompactionPercentage;
        });

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("option1");

        // assert
        sut.CompactionPercentage.Should().Be(option1.CompactionPercentage);
        sut.SizeLimit.Should().Be(option1.SizeLimit * 1024 * 1024);
        sut.SerializerName.Should().Be(option1.SerializerName);
    }

    [Fact]
    public void AddOptionByConfigToServiceCollectionShould()
    {
        // arrange
        var option1 = _fixture.Build<MemoryCacheOption>().With(m => m.CompactionPercentage, 0.8).Create(); ;

        var config = new ConfigurationBuilder()
                     .AddInMemoryCollection(
                         new Dictionary<string, string?>
                         {
                             ["Option1:SizeLimit"] = option1.SizeLimit.ToString(CultureInfo.InvariantCulture),
                             ["Option1:CompactionPercentage"] = option1.CompactionPercentage.ToString(CultureInfo.InvariantCulture),
                             ["Option1:SerializerName"] = option1.SerializerName,
                         }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddMemoryCache("option1", configuration, "Option1");

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("option1");

        // assert
        sut.CompactionPercentage.Should().Be(option1.CompactionPercentage);
        sut.SizeLimit.Should().Be(option1.SizeLimit * 1024 * 1024);
        sut.SerializerName.Should().Be(option1.SerializerName);
    }

    [Fact]
    public void MemoryCacheShould()
    {
        // arrange

        var config = new ConfigurationBuilder()
                             .AddInMemoryCollection(
                                 new Dictionary<string, string?>
                                 {
                                     ["Store:SizeLimit"] = "10"
                                 }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddMemoryCache("Store", configuration, "Store");
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTransient<IObjectSerialization, JsonSerialization>();

        var serviceProvider = services.BuildServiceProvider();

        var mockIContainer = _fixture.Freeze<Mock<IContainerResolve>>();
        mockIContainer.Setup(m => m.Resolve<IObjectSerialization>()).Returns(serviceProvider.GetService<IObjectSerialization>()!);

        var mockIOptions = _fixture.Freeze<Mock<IOptionsMonitor<MemoryCacheOption>>>();
        mockIOptions.Setup(m => m.Get("Store")).Returns(serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("Store"));

        // act
        var cache = _fixture.Create<MemoryCache>();

        cache.Initialize("Store");

        cache.Put("test", "test");

        var value = cache.Get<string>("test");

        // assert
        value.Should().Be("test");
    }
}
