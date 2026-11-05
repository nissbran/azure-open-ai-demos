namespace MaintenanceManualsIndexer;

internal static class ManualFileDiscovery
{
    internal static IReadOnlyList<string> Find(IEnumerable<string> paths)
    {
        var inputs = paths.ToArray();
        if (inputs.Length == 0)
            inputs = [Path.Combine(AppContext.BaseDirectory, "manuals")];

        HashSet<string> files = new(OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal);

        foreach (var input in inputs)
        {
            var fullPath = Path.GetFullPath(input);
            if (Directory.Exists(fullPath))
            {
                var manuals = Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
                    .Where(file => string.Equals(Path.GetExtension(file), ".pdf", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (manuals.Length == 0)
                    throw new InvalidOperationException($"No PDF manuals were found in '{fullPath}'.");

                files.UnionWith(manuals);
            }
            else if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("The manual file or directory does not exist.", fullPath);
            }
            else if (!string.Equals(Path.GetExtension(fullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The manual '{fullPath}' must have a .pdf extension.", nameof(paths));
            }
            else
            {
                files.Add(fullPath);
            }
        }

        return files.Order(StringComparer.Ordinal).ToArray();
    }
}
