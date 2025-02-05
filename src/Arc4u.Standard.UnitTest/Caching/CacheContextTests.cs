using AutoFixture.AutoMoq;
using AutoFixture;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Globalization;
using System;
using FluentAssertions;
using Arc4u.Caching.Memory;
using Microsoft.Extensions.Options;
using Arc4u.Caching;
using Arc4u.Configuration.Memory;
using Arc4u.Configuration.Redis;
using Arc4u.Configuration.Sql;
using System.IO;
using Moq;
using Arc4u.Dependency;
using Arc4u.Serializer;

namespace Arc4u.Standard.UnitTest.Caching;

[Trait("Category", "CI")]
public class CacheContextTests
{
    public CacheContextTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    private readonly Fixture _fixture;

    [Fact]
    public void ConfigSettingsShouldBe()
    {
        var cache = new Configuration.Caching
        {
            Default = "Volatile"
        };

        cache.Principal.CacheName = "Volatile";
        cache.Principal.Duration = TimeSpan.FromSeconds(1);
        cache.Principal.IsEnabled = true;
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = true, Kind = "Memory", Name = "Volatile" });

        var memorySettings = _fixture.Build<MemoryCacheOption>().With(m => m.CompactionPercentage, 0.8).Create(); ;

        var config = new ConfigurationBuilder()
                     .AddInMemoryCollection(
                         new Dictionary<string, string?>
                         {
                             ["Caching:Default"] = cache.Default,
                             ["Caching:Principal:CacheName"] = cache.Principal.CacheName.ToString(CultureInfo.InvariantCulture),
                             ["Caching:Principal:Duration"] = cache.Principal.Duration.ToString(),
                             ["Caching:Principal:IsEnabled"] = cache.Principal.IsEnabled.ToString(),
                             ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                             ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                             ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                             ["Caching:Caches:0:Settings"] = cache.Caches[0].IsAutoStart.ToString(),
                             ["Caching:Caches:0:Settings:SizeLimit"] = memorySettings.SizeLimit.ToString(CultureInfo.InvariantCulture),
                             ["Caching:Caches:0:Settings:CompactionPercentage"] = memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture),
                             ["Caching:Caches:0:Settings:SerializerName"] = memorySettings.SerializerName,
                         }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var bindedCaching = new Configuration.Caching();
        configuration.GetSection("Caching").Bind(bindedCaching);

        bindedCaching.Should().NotBeNull();
        bindedCaching.Default.Should().Be(cache.Default);
        bindedCaching.Principal.CacheName.Should().Be(cache.Principal.CacheName);
        bindedCaching.Principal.Duration.Should().Be(cache.Principal.Duration);
        bindedCaching.Principal.IsEnabled.Should().Be(cache.Principal.IsEnabled);

        IServiceCollection services = new ServiceCollection();

        services.AddMemoryCache("option1", configuration, "Caching:Caches:0:Settings");

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("option1");

        // assert
        sut.CompactionPercentage.Should().Be(memorySettings.CompactionPercentage);
        sut.SizeLimit.Should().Be(memorySettings.SizeLimit * 1024 * 1024);
        sut.SerializerName.Should().Be(memorySettings.SerializerName);
    }

    [Fact]
    public void RegisterOptionSettingsShould()
    {
        // arrange
        var cache = new Configuration.Caching
        {
            Default = "Volatile"
        };

        cache.Principal.CacheName = "Volatile";
        cache.Principal.Duration = TimeSpan.FromSeconds(1);
        cache.Principal.IsEnabled = true;
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = false, Kind = CacheContext.Redis, Name = "Performance" });
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = false, Kind = CacheContext.Sql, Name = "Compromize" });
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = true, Kind = CacheContext.Memory, Name = "Volatile" });
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = false, Kind = CacheContext.Dapr, Name = "Diversisty" });

        var memorySettings = _fixture.Build<MemoryCacheOption>().With(m => m.CompactionPercentage, 0.2).Create();
        var redisSettings = _fixture.Create<RedisCacheOption>();
        var sqlSettings = _fixture.Create<SqlCacheOption>();

        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
             .AddInMemoryCollection(
                 new Dictionary<string, string?>
                 {
                     ["Caching:Default"] = cache.Default,
                     ["Caching:Principal:CacheName"] = cache.Principal.CacheName.ToString(CultureInfo.InvariantCulture),
                     ["Caching:Principal:Duration"] = cache.Principal.Duration.ToString(),
                     ["Caching:Principal:IsEnabled"] = cache.Principal.IsEnabled.ToString(),

                     ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                     ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                     ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                     ["Caching:Caches:0:Settings:ConnectionString"] = redisSettings.ConnectionString,
                     ["Caching:Caches:0:Settings:InstanceName"] = redisSettings.InstanceName,
                     ["Caching:Caches:0:Settings:SerializerName"] = redisSettings.SerializerName,

                     ["Caching:Caches:1:Name"] = cache.Caches[1].Name,
                     ["Caching:Caches:1:Kind"] = cache.Caches[1].Kind,
                     ["Caching:Caches:1:IsAutoStart"] = cache.Caches[1].IsAutoStart.ToString(),
                     ["Caching:Caches:1:Settings:SchemaName"] = sqlSettings.SchemaName,
                     ["Caching:Caches:1:Settings:TableName"] = sqlSettings.TableName,
                     ["Caching:Caches:1:Settings:ConnectionString"] = sqlSettings.ConnectionString,
                     ["Caching:Caches:1:Settings:SerializerName"] = sqlSettings.SerializerName,

                     ["Caching:Caches:2:Name"] = cache.Caches[2].Name,
                     ["Caching:Caches:2:Kind"] = cache.Caches[2].Kind,
                     ["Caching:Caches:2:IsAutoStart"] = cache.Caches[2].IsAutoStart.ToString(),
                     ["Caching:Caches:2:Settings:SizeLimit"] = memorySettings.SizeLimit.ToString(CultureInfo.InvariantCulture),
                     ["Caching:Caches:2:Settings:CompactionPercentage"] = memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture),
                     ["Caching:Caches:2:Settings:SerializerName"] = memorySettings.SerializerName,

                     ["Caching:Caches:3:Name"] = cache.Caches[3].Name,
                     ["Caching:Caches:3:Kind"] = cache.Caches[3].Kind,
                     ["Caching:Caches:3:IsAutoStart"] = cache.Caches[3].IsAutoStart.ToString(),

                 }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCacheContext(configuration);
        services.AddSingleton(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sutRedis = serviceProvider.GetService<IOptionsMonitor<RedisCacheOption>>().Get("Performance");
        var sutSql = serviceProvider.GetService<IOptionsMonitor<SqlCacheOption>>()?.Get("Compromize");
        var sutMemory = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()?.Get("Volatile");

        // assert
        // redis
        sutRedis.Should().NotBeNull();
        sutRedis.InstanceName.Should().Be(redisSettings.InstanceName);
        sutRedis.ConnectionString.Should().Be(redisSettings.ConnectionString);
        sutRedis.SerializerName.Should().Be(redisSettings.SerializerName);
        // sql

        sutSql.ConnectionString.Should().Be(sqlSettings.ConnectionString);
        sutSql.TableName.Should().Be(sqlSettings.TableName);
        sutSql.SchemaName.Should().Be(sqlSettings.SchemaName);
        sutSql.SerializerName.Should().Be(sqlSettings.SerializerName);
        // memory
        sutMemory.Should().NotBeNull();
        sutMemory.SerializerName.Should().Be(memorySettings.SerializerName);
        sutMemory.SizeLimit.Should().Be(memorySettings.SizeLimit * 1024 * 1024);
        sutMemory.CompactionPercentage.Should().Be(memorySettings.CompactionPercentage);

    }

    [Fact]
    public void InitializeAndUseOfMemoryShould()
    {
        var cache = new Configuration.Caching
        {
            Default = "Volatile"
        };

        cache.Principal.CacheName = "Volatile";
        cache.Principal.Duration = TimeSpan.FromSeconds(1);
        cache.Principal.IsEnabled = true;
        cache.Caches.Add(new Configuration.CachingCache { IsAutoStart = true, Kind = CacheContext.Memory, Name = "Volatile" });

        var memorySettings = new MemoryCacheOption { SizeLimit = 10 };

        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
             .AddInMemoryCollection(
                 new Dictionary<string, string?>
                 {
                     ["Caching:Default"] = cache.Default,
                     ["Caching:Principal:CacheName"] = cache.Principal.CacheName.ToString(CultureInfo.InvariantCulture),
                     ["Caching:Principal:Duration"] = cache.Principal.Duration.ToString(),
                     ["Caching:Principal:IsEnabled"] = cache.Principal.IsEnabled.ToString(),

                     ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                     ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                     ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                     ["Caching:Caches:0:Settings:SizeLimit"] = memorySettings.SizeLimit.ToString(CultureInfo.InvariantCulture),
                     ["Caching:Caches:0:Settings:CompactionPercentage"] = memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture),

                 }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCacheContext(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var mockIOptions = _fixture.Freeze<Mock<IOptionsMonitor<MemoryCacheOption>>>();
        mockIOptions.Setup(m => m.Get("Volatile")).Returns(serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("Volatile"));

        var mockIContainer = _fixture.Freeze<Mock<IContainerResolve>>();
        mockIContainer.Setup(m => m.Resolve<IObjectSerialization>()).Returns(new JsonSerialization());

        ICache mockCache = _fixture.Create<MemoryCache>();
        mockIContainer.Setup(m => m.TryResolve<ICache>(CacheContext.Memory, out mockCache)).Returns(true);

        _fixture.Inject<IConfiguration>(configuration);

        var sut = _fixture.Create<CacheContext>();

        var cacheInstance = sut["Volatile"];

        cacheInstance.Put("key", "value");

        cacheInstance.Get<string>("key").Should().Be("value");


    }

}
