using System;
using System.IO;
using System.Xml;
using GoogleSheetsTable;
using UnityEngine;

public sealed class GameTableDatabase : IDisposable
{
    public TableManager Tables { get; } = new();

    public bool Load()
    {
        var loaded = true;
        foreach (var definition in TableRegistry.TABLES)
            loaded &= LoadXml(definition);

        return loaded;
    }

    public void Dispose()
    {
        foreach (var definition in TableRegistry.TABLES)
            definition.Dispose(Tables);
    }

    private bool LoadXml(TableDefinition definition)
    {
        var tableAsset = Resources.Load<TextAsset>(definition.ResourcePath);
        if (tableAsset == null)
        {
            Debug.LogError($"Table resource not found: Resources/{definition.ResourcePath}.xml");
            return false;
        }

        try
        {
            using var stringReader = new StringReader(tableAsset.text);
            using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true,
            });
            definition.LoadXml(Tables, xmlReader);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to load table '{definition.TableName}': {exception.Message}");
            return false;
        }
    }
}
