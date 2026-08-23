using UnityEngine.Pool;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
namespace GoogleSheetsTable
{
	public partial class TableManager
	{
		private readonly Dictionary<int, Monster> m_DicMonster = new Dictionary<int, Monster>();
		public IReadOnlyDictionary<int, Monster> MonsterDatas => m_DicMonster;
		public void LoadTable_Monster(System.Xml.XmlReader xmlReader)
		{
			m_DicMonster.Clear();
			while (xmlReader.Read())
			{
				if (xmlReader.NodeType != System.Xml.XmlNodeType.Element) continue;
				if (xmlReader.Name != "Monster") continue;
				var data = new Monster(xmlReader);
				m_DicMonster.Add(data.ID, data);
			}
		}
		public void LoadTable_Monster(System.IO.BinaryReader binaryReader)
		{
			m_DicMonster.Clear();
			var count = binaryReader.ReadInt32();
			for (var i = 0; i < count; i ++)
			{
				var data = new Monster(binaryReader);
				m_DicMonster.Add(data.ID, data);
			}
		}
		public void ExportBinary_Monster(System.IO.BinaryWriter binaryWriter)
		{
			binaryWriter.Write(m_DicMonster.Count);
			foreach (var data in m_DicMonster.Values)
			{
				data.ExportBinary(binaryWriter);
			}
		}
		public void Dispose_Monster()
		{
			foreach (var data in m_DicMonster.Values)
			{
				data.Dispose();
			}
			m_DicMonster.Clear();
		}
		public Monster GetMonsterByID(int id)
		{
			if (m_DicMonster.ContainsKey(id) == false) return default;
			return m_DicMonster[id];
		}
		public int GetMonsterDataCount()
		{
			return m_DicMonster.Count;
		}
		public IEnumerable<Monster> GetAllMonsterData()
		{
			return m_DicMonster.Values;
		}
	}
}
