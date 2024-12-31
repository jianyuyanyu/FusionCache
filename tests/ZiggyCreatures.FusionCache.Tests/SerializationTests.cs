﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FusionCacheTests.Stuff;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using ZiggyCreatures.Caching.Fusion.Internals;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;
using ZiggyCreatures.Caching.Fusion.Serialization;

namespace FusionCacheTests;

public partial class SerializationTests
	: AbstractTests
{
	private static readonly ComplexType[] BigData;

	static SerializationTests()
	{
		var len = 1024 * 1024;
		BigData = new ComplexType[len];
		for (int i = 0; i < len; i++)
		{
			BigData[i] = ComplexType.CreateSample();
		}
	}

	public SerializationTests(ITestOutputHelper output)
			: base(output, null)
	{
	}

	//private static readonly Regex __re_VersionExtractor = VersionExtractorRegEx();

	private const string SampleString = "Supercalifragilisticexpialidocious";

	private static T? LoopDeLoop<T>(IFusionCacheSerializer serializer, T? obj)
	{
		var data = serializer.Serialize(obj);
		return serializer.Deserialize<T>(data);
	}

	private static async Task<T?> LoopDeLoopAsync<T>(IFusionCacheSerializer serializer, T? obj)
	{
		var data = await serializer.SerializeAsync(obj);
		return await serializer.DeserializeAsync<T>(data);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithSimpleTypesAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = await LoopDeLoopAsync(serializer, SampleString);
		Assert.Equal(SampleString, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithSimpleTypes(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = LoopDeLoop(serializer, SampleString);
		Assert.Equal(SampleString, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithComplexTypesAsync(SerializerType serializerType)
	{
		var data = ComplexType.CreateSample();
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = await LoopDeLoopAsync(serializer, data);
		Assert.Equal(data, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithComplexTypes(SerializerType serializerType)
	{
		var data = ComplexType.CreateSample();
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = LoopDeLoop(serializer, data);
		Assert.Equal(data, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithComplexTypesArrayAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = await LoopDeLoopAsync(serializer, BigData);
		Assert.Equal(BigData, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithComplexTypesArray(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = LoopDeLoop(serializer, BigData);
		Assert.Equal(BigData, looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopDoesNotFailWithNullAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = await LoopDeLoopAsync<string>(serializer, null);
		Assert.Null(looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopDoesNotFailWithNull(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var looped = LoopDeLoop<string>(serializer, null);
		Assert.Null(looped);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithDistributedEntryAndSimpleTypesAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<string>(SampleString, [], new FusionCacheEntryMetadata(true, now.AddSeconds(9), "abc123", now, 123), now.UtcTicks, now.AddSeconds(10).UtcTicks);

		var data = await serializer.SerializeAsync(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = await serializer.DeserializeAsync<FusionCacheDistributedEntry<string>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Equal(obj.Metadata!.IsFromFailSafe, looped.Metadata!.IsFromFailSafe);
		Assert.Equal(obj.Metadata!.EagerExpiration, looped.Metadata!.EagerExpiration);
		Assert.Equal(obj.Metadata!.ETag, looped.Metadata!.ETag);
		Assert.Equal(obj.Metadata!.LastModified, looped.Metadata!.LastModified);
		Assert.Equal(obj.Metadata!.Size, looped.Metadata!.Size);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithDistributedEntryAndSimpleTypes(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<string>(SampleString, [], new FusionCacheEntryMetadata(true, now.AddSeconds(9), "abc123", now, 123), now.UtcTicks, now.AddSeconds(10).UtcTicks);

		var data = serializer.Serialize(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = serializer.Deserialize<FusionCacheDistributedEntry<string>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Equal(obj.Metadata!.IsFromFailSafe, looped.Metadata!.IsFromFailSafe);
		Assert.Equal(obj.Metadata!.EagerExpiration, looped.Metadata!.EagerExpiration);
		Assert.Equal(obj.Metadata!.ETag, looped.Metadata!.ETag);
		Assert.Equal(obj.Metadata!.LastModified, looped.Metadata!.LastModified);
		Assert.Equal(obj.Metadata!.Size, looped.Metadata!.Size);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithDistributedEntryAndNoMetadataAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<string>(SampleString, [], null, now.UtcTicks, now.AddSeconds(10).UtcTicks);

		var data = await serializer.SerializeAsync(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = await serializer.DeserializeAsync<FusionCacheDistributedEntry<string>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Null(looped!.Metadata);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithDistributedEntryAndNoMetadata(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<string>(SampleString, [], null, now.UtcTicks, now.AddSeconds(10).UtcTicks);

		var data = serializer.Serialize(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = serializer.Deserialize<FusionCacheDistributedEntry<string>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Null(looped!.Metadata);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task LoopSucceedsWithDistributedEntryAndComplexTypesAsync(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<ComplexType>(ComplexType.CreateSample(), [], new FusionCacheEntryMetadata(true, now.AddSeconds(9).AddMicroseconds(now.Microsecond * -1), "abc123", now.AddMicroseconds(now.Microsecond * -1), 123), now.UtcTicks, now.AddSeconds(10).AddMicroseconds(now.Nanosecond * -1).UtcTicks);

		var data = await serializer.SerializeAsync(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = await serializer.DeserializeAsync<FusionCacheDistributedEntry<ComplexType>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Equal(obj.Metadata!.IsFromFailSafe, looped.Metadata!.IsFromFailSafe);
		Assert.Equal(obj.Metadata!.EagerExpiration, looped.Metadata!.EagerExpiration);
		Assert.Equal(obj.Metadata!.ETag, looped.Metadata!.ETag);
		Assert.Equal(obj.Metadata!.LastModified, looped.Metadata!.LastModified);
		Assert.Equal(obj.Metadata!.Size, looped.Metadata!.Size);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void LoopSucceedsWithDistributedEntryAndComplexTypes(SerializerType serializerType)
	{
		var serializer = TestsUtils.GetSerializer(serializerType);
		var now = DateTimeOffset.UtcNow;
		var obj = new FusionCacheDistributedEntry<ComplexType>(ComplexType.CreateSample(), [], new FusionCacheEntryMetadata(true, now.AddSeconds(9).AddMicroseconds(now.Microsecond * -1), "abc123", now.AddMicroseconds(now.Microsecond * -1), 123), now.UtcTicks, now.AddSeconds(10).AddMicroseconds(now.Nanosecond * -1).UtcTicks);

		var data = serializer.Serialize(obj);

		Assert.NotNull(data);
		Assert.NotEmpty(data);

		var looped = serializer.Deserialize<FusionCacheDistributedEntry<ComplexType>>(data);
		Assert.NotNull(looped);
		Assert.Equal(obj.Value, looped.Value);
		Assert.Equal(obj.Timestamp, looped.Timestamp);
		Assert.Equal(obj.LogicalExpirationTimestamp, looped.LogicalExpirationTimestamp);
		Assert.Equal(obj.Metadata!.IsFromFailSafe, looped.Metadata!.IsFromFailSafe);
		Assert.Equal(obj.Metadata!.EagerExpiration, looped.Metadata!.EagerExpiration);
		Assert.Equal(obj.Metadata!.ETag, looped.Metadata!.ETag);
		Assert.Equal(obj.Metadata!.LastModified, looped.Metadata!.LastModified);
	}

	//[Theory]
	//[ClassData(typeof(SerializerTypesClassData))]
	//public async Task CanDeserializeOldSnapshotsAsync(SerializerType serializerType)
	//{
	//	var serializer = TestsUtils.GetSerializer(serializerType);

	//	var assembly = serializer.GetType().Assembly;
	//	var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
	//	string? currentVersion = fvi.FileVersion![..fvi.FileVersion!.LastIndexOf('.')];

	//	var filePrefix = $"{serializer.GetType().Name}__";

	//	var files = Directory.GetFiles("Snapshots", filePrefix + "*.bin");

	//	TestOutput.WriteLine($"Found {files.Length} snapshots for {serializer.GetType().Name}");

	//	foreach (var file in files)
	//	{
	//		var payloadVersion = __re_VersionExtractor.Match(file).Groups[1].Value.Replace('_', '.');

	//		var payload = File.ReadAllBytes(file);
	//		var deserialized = await serializer.DeserializeAsync<FusionCacheDistributedEntry<string>>(payload);
	//		Assert.False(deserialized is null, $"Failed deserializing payload from v{payloadVersion}");

	//		TestOutput.WriteLine($"Correctly deserialized payload from v{payloadVersion} to v{currentVersion} (current) using {serializer.GetType().Name}");
	//	}
	//}

	//[Theory]
	//[ClassData(typeof(SerializerTypesClassData))]
	//public void CanDeserializeOldSnapshots(SerializerType serializerType)
	//{
	//	var serializer = TestsUtils.GetSerializer(serializerType);

	//	var assembly = serializer.GetType().Assembly;
	//	var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
	//	string? currentVersion = fvi.FileVersion![..fvi.FileVersion!.LastIndexOf('.')];

	//	var filePrefix = $"{serializer.GetType().Name}__";

	//	var files = Directory.GetFiles("Snapshots", filePrefix + "*.bin");

	//	TestOutput.WriteLine($"Found {files.Length} snapshots for {serializer.GetType().Name}");

	//	foreach (var file in files)
	//	{
	//		var payloadVersion = __re_VersionExtractor.Match(file).Groups[1].Value.Replace('_', '.');

	//		var payload = File.ReadAllBytes(file);
	//		var deserialized = serializer.Deserialize<FusionCacheDistributedEntry<string>>(payload);
	//		Assert.False(deserialized is null, $"Failed deserializing payload from v{payloadVersion}");

	//		TestOutput.WriteLine($"Correctly deserialized payload from v{payloadVersion} to v{currentVersion} (current) using {serializer.GetType().Name}");
	//	}
	//}

	//[GeneratedRegex(@"\w+__v(\d+_\d+_\d+)_\d+\.bin", RegexOptions.Compiled)]
	//private static partial Regex VersionExtractorRegEx();

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public async Task CanWorkWithByteArraysAsync(SerializerType serializerType)
	{
		var logger = CreateXUnitLogger<bool>();
		var serializer = TestsUtils.GetSerializer(serializerType);

		var sourceString = new String('x', 1_000);
		logger.LogInformation("SOURCE STRING: {chars} chars", sourceString.Length);

		var sourceData = Encoding.UTF8.GetBytes(sourceString);
		logger.LogInformation("SOURCE DATA: {bytes} bytes", sourceData.Length);

		var serializedData = await serializer.SerializeAsync(sourceData);
		logger.LogInformation("SERIALIZED DATA: {bytes} bytes (+{delta} bytes)", serializedData.Length, serializedData.Length - sourceData.Length);

		var targetData = await serializer.DeserializeAsync<byte[]>(serializedData);
		logger.LogInformation("TARGET DATA: {bytes} bytes", targetData!.Length);

		var targetString = Encoding.UTF8.GetString(targetData!);
		logger.LogInformation("TARGET STRING: {chars} chars", targetString.Length);

		Assert.Equal(sourceString, targetString);
	}

	[Theory]
	[ClassData(typeof(SerializerTypesClassData))]
	public void CanWorkWithByteArrays(SerializerType serializerType)
	{
		var logger = CreateXUnitLogger<bool>();
		var serializer = TestsUtils.GetSerializer(serializerType);

		var sourceString = new String('x', 1_000);
		logger.LogInformation("SOURCE STRING: {chars} chars", sourceString.Length);

		var sourceData = Encoding.UTF8.GetBytes(sourceString);
		logger.LogInformation("SOURCE DATA: {bytes} bytes", sourceData.Length);

		var serializedData = serializer.Serialize(sourceData);
		logger.LogInformation("SERIALIZED DATA: {bytes} bytes (+{delta} bytes)", serializedData.Length, serializedData.Length - sourceData.Length);

		var targetData = serializer.Deserialize<byte[]>(serializedData);
		logger.LogInformation("TARGET DATA: {bytes} bytes", targetData!.Length);

		var targetString = Encoding.UTF8.GetString(targetData!);
		logger.LogInformation("TARGET STRING: {chars} chars", targetString.Length);

		Assert.Equal(sourceString, targetString);
	}
}
