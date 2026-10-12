// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Server.SS220.Forensics.Serialization;

/// <summary>
/// Loads individual material profiles while discarding older maps' material and color descriptions.
/// Those descriptions contain no source identity and cannot be converted into identifiable profiles.
/// </summary>
public sealed class MicroFiberTraceSetSerializer : ITypeSerializer<HashSet<MicroFiberTrace>, SequenceDataNode>
{
    public HashSet<MicroFiberTrace> Read(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<HashSet<MicroFiberTrace>>? instanceProvider = null)
    {
        HashSet<MicroFiberTrace> traces = instanceProvider != null ? instanceProvider() : [];
        foreach (var entry in node.Sequence)
        {
            if (entry is ValueDataNode)
                continue;

            traces.Add(serializationManager.Read<MicroFiberTrace>(entry, hookCtx, context));
        }

        return traces;
    }

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        List<ValidationNode> entries = [];
        foreach (var entry in node.Sequence)
        {
            if (entry is ValueDataNode)
            {
                entries.Add(new ValidatedValueNode(entry));
                continue;
            }

            entries.Add(serializationManager.ValidateNode<MicroFiberTrace>(entry, context));
        }

        return new ValidatedSequenceNode(entries);
    }

    public DataNode Write(ISerializationManager serializationManager, HashSet<MicroFiberTrace> value,
        IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        var sequence = new SequenceDataNode(value.Count);
        foreach (var trace in value)
        {
            sequence.Add(serializationManager.WriteValue(trace, alwaysWrite, context));
        }

        return sequence;
    }
}
