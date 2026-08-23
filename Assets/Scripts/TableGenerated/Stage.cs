using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using GoogleSheetsTable;
namespace GoogleSheetsTable
{
	public partial struct Stage : IDisposable
	{
		public readonly bool IsValid;
		public readonly int ID;
		public readonly int[] MonsterIds;
		public readonly int BossMonsterId;
		public readonly int BossTimeLimitSeconds;
		public readonly string HpMultiplier;
		public readonly string RewardMultiplier;
		public readonly string PrefabName;
		public readonly int NextStageId;
		public Stage(System.Xml.XmlReader xmlReader)
		{
			IsValid = true;
			ID = default;
			int.TryParse(xmlReader.GetAttribute("ID"), out ID);
			MonsterIds = default;
			if (ParseUtility.TryParseArrayInt(xmlReader.GetAttribute("MonsterIds"), out MonsterIds) == false) MonsterIds = Array.Empty<int>();
			BossMonsterId = default;
			int.TryParse(xmlReader.GetAttribute("BossMonsterId"), out BossMonsterId);
			BossTimeLimitSeconds = default;
			int.TryParse(xmlReader.GetAttribute("BossTimeLimitSeconds"), out BossTimeLimitSeconds);
			HpMultiplier = default;
			HpMultiplier = xmlReader.GetAttribute("HpMultiplier");
			RewardMultiplier = default;
			RewardMultiplier = xmlReader.GetAttribute("RewardMultiplier");
			PrefabName = default;
			PrefabName = xmlReader.GetAttribute("PrefabName");
			NextStageId = default;
			int.TryParse(xmlReader.GetAttribute("NextStageId"), out NextStageId);
		}
		public Stage(System.IO.BinaryReader binaryReader)
		{
			IsValid = true;
			ID = binaryReader.ReadInt32();
			MonsterIds = new int[binaryReader.ReadInt32()];
			for (var i = 0; i < MonsterIds.Length; i ++)
				MonsterIds[i] = binaryReader.ReadInt32();
			BossMonsterId = binaryReader.ReadInt32();
			BossTimeLimitSeconds = binaryReader.ReadInt32();
			HpMultiplier = binaryReader.ReadString();
			RewardMultiplier = binaryReader.ReadString();
			PrefabName = binaryReader.ReadString();
			NextStageId = binaryReader.ReadInt32();
		}
		public void ExportBinary(System.IO.BinaryWriter binaryWriter)
		{
			binaryWriter.Write(ID);
			binaryWriter.Write(MonsterIds == null ? 0 : MonsterIds.Length);
			if (MonsterIds != null) for (var i = 0; i < MonsterIds.Length; i ++) binaryWriter.Write(MonsterIds[i]);
			binaryWriter.Write(BossMonsterId);
			binaryWriter.Write(BossTimeLimitSeconds);
			binaryWriter.Write(HpMultiplier);
			binaryWriter.Write(RewardMultiplier);
			binaryWriter.Write(PrefabName);
			binaryWriter.Write(NextStageId);
		}
		public void Dispose()
		{
		}
	}
}
