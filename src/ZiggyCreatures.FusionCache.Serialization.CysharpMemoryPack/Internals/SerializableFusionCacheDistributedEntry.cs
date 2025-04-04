﻿using MemoryPack;
using ZiggyCreatures.Caching.Fusion.Internals;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;

namespace ZiggyCreatures.Caching.Fusion.Serialization.CysharpMemoryPack.Internals;

[MemoryPackable]
internal partial class SerializableFusionCacheDistributedEntry<TValue>
{
	[MemoryPackIgnore]
	public readonly FusionCacheDistributedEntry<TValue>? Entry;

	[MemoryPackInclude]
	public TValue? Value => Entry is not null ? Entry.Value : default;

	[MemoryPackInclude]
	public long Timestamp => Entry?.Timestamp ?? 0L;

	[MemoryPackInclude]
	public long LogicalExpirationTimestamp => Entry?.LogicalExpirationTimestamp ?? 0L;

	[MemoryPackInclude]
	public string[]? Tags => Entry is not null ? Entry.Tags : default;

	[MemoryPackAllowSerialize]
	public FusionCacheEntryMetadata? Metadata => Entry?.Metadata;

	[MemoryPackConstructor]
	SerializableFusionCacheDistributedEntry(TValue value, long timestamp, long logicalExpirationTimestamp, string[]? tags, FusionCacheEntryMetadata? metadata)
	{
		this.Entry = new FusionCacheDistributedEntry<TValue>(value, timestamp, logicalExpirationTimestamp, tags, metadata);
	}

	public SerializableFusionCacheDistributedEntry(FusionCacheDistributedEntry<TValue>? entry)
	{
		this.Entry = entry;
	}
}

internal class FusionCacheDistributedEntryFormatter<TValue> : MemoryPackFormatter<FusionCacheDistributedEntry<TValue>>
{
	public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref FusionCacheDistributedEntry<TValue>? value)
	{
		if (value is null)
		{
			writer.WriteNullObjectHeader();
			return;
		}

		writer.WritePackable(new SerializableFusionCacheDistributedEntry<TValue>(value));
	}

	public override void Deserialize(ref MemoryPackReader reader, scoped ref FusionCacheDistributedEntry<TValue>? value)
	{
		if (reader.PeekIsNull())
		{
			reader.Advance(1);
			value = null;
			return;
		}

		var wrapped = reader.ReadPackable<SerializableFusionCacheDistributedEntry<TValue>>();
		if (wrapped is null)
		{
			value = null;
			return;
		}

		value = wrapped.Entry;
	}
}
