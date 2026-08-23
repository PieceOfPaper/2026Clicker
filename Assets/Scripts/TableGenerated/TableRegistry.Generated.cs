using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
namespace GoogleSheetsTable
{
	public readonly struct TableDefinition
	{
		public readonly string TableName;
		public readonly string FileName;
		public readonly string ResourcePath;
		public readonly Action<TableManager, XmlReader> LoadXml;
		public readonly Action<TableManager, BinaryReader> LoadBinary;
		public readonly Action<TableManager> Dispose;

		public TableDefinition(string tableName, string fileName, string resourcePath,
			Action<TableManager, XmlReader> loadXml, Action<TableManager, BinaryReader> loadBinary,
			Action<TableManager> dispose)
		{
			TableName = tableName;
			FileName = fileName;
			ResourcePath = resourcePath;
			LoadXml = loadXml;
			LoadBinary = loadBinary;
			Dispose = dispose;
		}
	}

	public static class TableRegistry
	{
		public static readonly string[] TABLE_FILE_NAMES =
		{
			"stage",
			"monster",
		};

		public static readonly TableDefinition[] TABLES =
		{
			new TableDefinition("Stage", "stage", "TableData/stage",
				(manager, reader) => manager.LoadTable_Stage(reader),
				(manager, reader) => manager.LoadTable_Stage(reader),
				manager => manager.Dispose_Stage()),
			new TableDefinition("Monster", "monster", "TableData/monster",
				(manager, reader) => manager.LoadTable_Monster(reader),
				(manager, reader) => manager.LoadTable_Monster(reader),
				manager => manager.Dispose_Monster()),
		};

		private static readonly Dictionary<string, TableDefinition> TABLES_BY_FILE_NAME = CreateLookup();

		public static bool TryGetDefinition(string fileName, out TableDefinition definition)
		{
			return TABLES_BY_FILE_NAME.TryGetValue(fileName, out definition);
		}

		private static Dictionary<string, TableDefinition> CreateLookup()
		{
			var result = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);
			foreach (var table in TABLES)
				result.Add(table.FileName, table);
			return result;
		}
	}
}
