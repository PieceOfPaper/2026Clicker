using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using GoogleSheetsTable;
namespace GoogleSheetsTable
{
	public partial struct Monster : IDisposable
	{
		public readonly bool IsValid;
		public readonly int ID;
		public readonly string DisplayName;
		public readonly string BaseHp;
		public readonly string BaseRewardGold;
		public readonly string PrefabName;
		public Monster(System.Xml.XmlReader xmlReader)
		{
			IsValid = true;
			ID = default;
			int.TryParse(xmlReader.GetAttribute("ID"), out ID);
			DisplayName = default;
			DisplayName = xmlReader.GetAttribute("DisplayName");
			BaseHp = default;
			BaseHp = xmlReader.GetAttribute("BaseHp");
			BaseRewardGold = default;
			BaseRewardGold = xmlReader.GetAttribute("BaseRewardGold");
			PrefabName = default;
			PrefabName = xmlReader.GetAttribute("PrefabName");
		}
		public Monster(System.IO.BinaryReader binaryReader)
		{
			IsValid = true;
			ID = binaryReader.ReadInt32();
			DisplayName = binaryReader.ReadString();
			BaseHp = binaryReader.ReadString();
			BaseRewardGold = binaryReader.ReadString();
			PrefabName = binaryReader.ReadString();
		}
		public void ExportBinary(System.IO.BinaryWriter binaryWriter)
		{
			binaryWriter.Write(ID);
			binaryWriter.Write(DisplayName);
			binaryWriter.Write(BaseHp);
			binaryWriter.Write(BaseRewardGold);
			binaryWriter.Write(PrefabName);
		}
		public void Dispose()
		{
		}
	}
}
