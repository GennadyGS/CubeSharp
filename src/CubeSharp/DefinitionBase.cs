namespace CubeSharp;

/// <summary>The base class of the definition.</summary>
/// <remarks>Contains common functionality of the all definitions.</remarks>
public class DefinitionBase
{
    private readonly Dictionary<string, object> _metadata = new();

    internal DefinitionBase(string? title)
    {
        Title = title;
    }

    /// <summary>Gets or sets the title.</summary>
    /// <value>The title.</value>
    public string? Title { get; set; }

    /// <summary>Gets the metadata.</summary>
    /// <value>The metadata in the form of dictionary.</value>
    public IReadOnlyDictionary<string, object> Metadata => _metadata;

    /// <summary>Add the single entry to the metadata.</summary>
    /// <param name="key">The key of the metadata entry.</param>
    /// <param name="value">The value of the metadata entry.</param>
    public void AddMetadata(string key, object value) =>
        _metadata.Add(key, value);

    /// <summary>Add the multiple entries to the metadata.</summary>
    /// <param name="metadata">
    /// The collection of the key-value pairs specifying the metadata entries.
    /// </param>
    public void AddMetadata(params (string key, object value)[] metadata)
    {
        foreach (var item in metadata)
        {
            _metadata.Add(item.key, item.value);
        }
    }
}
