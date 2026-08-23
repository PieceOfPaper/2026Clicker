using UnityEngine.Pool;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
namespace GoogleSheetsTable
{
	public partial class TableManager
	{
		private readonly Dictionary<int, Stage> m_DicStage = new Dictionary<int, Stage>();
		public IReadOnlyDictionary<int, Stage> StageDatas => m_DicStage;
		public void LoadTable_Stage(System.Xml.XmlReader xmlReader)
		{
			m_DicStage.Clear();
			while (xmlReader.Read())
			{
				if (xmlReader.NodeType != System.Xml.XmlNodeType.Element) continue;
				if (xmlReader.Name != "Stage") continue;
				var data = new Stage(xmlReader);
				m_DicStage.Add(data.ID, data);
			}
		}
		public void LoadTable_Stage(System.IO.BinaryReader binaryReader)
		{
			m_DicStage.Clear();
			var count = binaryReader.ReadInt32();
			for (var i = 0; i < count; i ++)
			{
				var data = new Stage(binaryReader);
				m_DicStage.Add(data.ID, data);
			}
		}
		public void ExportBinary_Stage(System.IO.BinaryWriter binaryWriter)
		{
			binaryWriter.Write(m_DicStage.Count);
			foreach (var data in m_DicStage.Values)
			{
				data.ExportBinary(binaryWriter);
			}
		}
		public void Dispose_Stage()
		{
			foreach (var data in m_DicStage.Values)
			{
				data.Dispose();
			}
			m_DicStage.Clear();
		}
		public Stage GetStageByID(int id)
		{
			if (m_DicStage.ContainsKey(id) == false) return default;
			return m_DicStage[id];
		}
		public int GetStageDataCount()
		{
			return m_DicStage.Count;
		}
		public IEnumerable<Stage> GetAllStageData()
		{
			return m_DicStage.Values;
		}
	}
}
