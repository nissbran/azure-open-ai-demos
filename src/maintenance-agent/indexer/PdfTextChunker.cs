namespace MaintenanceManualsIndexer;

internal static class DocumentIntelligenceChunker
{
    internal static IReadOnlyList<string> Chunk(string text, int chunkSize = 2000, int overlap = 200)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkSize, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(overlap);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(overlap, chunkSize - 2);

        List<string> chunks = new List<string>();
        var start = 0;
        while (start < text.Length)
        {
            var end = start + Math.Min(chunkSize, text.Length - start);
            if (end < text.Length && char.IsHighSurrogate(text[end - 1]) && char.IsLowSurrogate(text[end]))
                end--;

            var chunk = text[start..end];
            if (!string.IsNullOrWhiteSpace(chunk))
                chunks.Add(chunk);

            if (end == text.Length)
                break;

            start = end - overlap;
            if (start > 0 && char.IsLowSurrogate(text[start]) && char.IsHighSurrogate(text[start - 1]))
                start++;
        }

        return chunks;
    }
}