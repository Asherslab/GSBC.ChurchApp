using System.Buffers;
using Grpc.Core;

namespace GSBC.ChurchApp.Platform.Client.Native;

internal sealed class BufferSerializationContext : SerializationContext
{
    private ArrayBufferWriter<byte>? _writer;
    private byte[]? _payload;

    public byte[] Payload => _payload ?? _writer?.WrittenSpan.ToArray() ?? [];

    public override void Complete(byte[] payload) => _payload = payload;

    public override IBufferWriter<byte> GetBufferWriter() => _writer ??= new ArrayBufferWriter<byte>();

    public override void SetPayloadLength(int payloadLength)
    {
    }

    public override void Complete()
    {
    }
}

internal sealed class BufferDeserializationContext(byte[] payload) : DeserializationContext
{
    public override int PayloadLength => payload.Length;

    public override byte[] PayloadAsNewBuffer() => payload.ToArray();

    public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new(payload);
}
