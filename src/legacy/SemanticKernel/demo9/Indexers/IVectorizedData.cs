using System;

namespace Demo9.Indexers;

public interface IVectorizedData
{
    public string Identifier { get; }
    public string Summary { get; }
    public ReadOnlyMemory<float> VectorizedSummary { get; set; }
}