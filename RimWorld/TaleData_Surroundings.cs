using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace RimWorld
{
	public class TaleData_Surroundings : TaleData
	{
		public int tile;

		public float temperature;

		public float snowDepth;

		public WeatherDef weather;

		public RoomRoleDef roomRole;

		public float roomImpressiveness;

		public float roomBeauty;

		public float roomCleanliness;

		public bool Outdoors => weather != null;

		public override void ExposeData()
		{
			Scribe_Values.Look(ref tile, "tile", 0);
			Scribe_Values.Look(ref temperature, "temperature", 0f);
			Scribe_Values.Look(ref snowDepth, "snowDepth", 0f);
			Scribe_Defs.Look(ref weather, "weather");
			Scribe_Defs.Look(ref roomRole, "roomRole");
			Scribe_Values.Look(ref roomImpressiveness, "roomImpressiveness", 0f);
			Scribe_Values.Look(ref roomBeauty, "roomBeauty", 0f);
			Scribe_Values.Look(ref roomCleanliness, "roomCleanliness", 0f);
		}

		public override IEnumerable<Rule> GetRules()
		{
			yield return new Rule_String("BIOME", Find.WorldGrid[tile].biome.label);
			if (roomRole != null && roomRole != RoomRoleDefOf.None)
			{
				yield return new Rule_String("ROOM_role", roomRole.label);
				yield return new Rule_String("ROOM_roleDefinite", Find.ActiveLanguageWorker.WithDefiniteArticle(roomRole.label));
				yield return new Rule_String("ROOM_roleIndefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(roomRole.label));
				RoomStatScoreStage impressiveness = RoomStatDefOf.Impressiveness.GetScoreStage(roomImpressiveness);
				RoomStatScoreStage beauty = RoomStatDefOf.Beauty.GetScoreStage(roomBeauty);
				RoomStatScoreStage cleanliness = RoomStatDefOf.Cleanliness.GetScoreStage(roomCleanliness);
				yield return new Rule_String("ROOM_impressiveness", impressiveness.label);
				yield return new Rule_String("ROOM_impressivenessIndefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(impressiveness.label));
				yield return new Rule_String("ROOM_beauty", beauty.label);
				yield return new Rule_String("ROOM_beautyIndefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(beauty.label));
				yield return new Rule_String("ROOM_cleanliness", cleanliness.label);
				yield return new Rule_String("ROOM_cleanlinessIndefinite", Find.ActiveLanguageWorker.WithIndefiniteArticle(cleanliness.label));
			}
		}

		public static TaleData_Surroundings GenerateFrom(IntVec3 c, Map map)
		{
			// 创建一个空的“周边环境”故事数据
			TaleData_Surroundings taleData_Surroundings = new TaleData_Surroundings();
			// 设置故事数据的tile字段为地图的tile
			taleData_Surroundings.tile = map.Tile;
			// 获取与位置c相邻的房间
			Room roomOrAdjacent = c.GetRoomOrAdjacent(map, RegionType.Set_All);
			// 如果存在相邻的房间
			if (roomOrAdjacent != null)
			{
				// 如果房间的心理室外值为true，设置故事数据的weather字段为地图中当前可感知天气
				if (roomOrAdjacent.PsychologicallyOutdoors)
				{
					taleData_Surroundings.weather = map.weatherManager.CurWeatherPerceived;
				}
				// 设置故事数据的roomRole字段为房间的作用角色
				taleData_Surroundings.roomRole = roomOrAdjacent.Role;
				// 设置故事数据的roomImpressiveness字段为房间的印象值
				taleData_Surroundings.roomImpressiveness = roomOrAdjacent.GetStat(RoomStatDefOf.Impressiveness);
				// 设置故事数据的roomBeauty字段为房间的美观值
				taleData_Surroundings.roomBeauty = roomOrAdjacent.GetStat(RoomStatDefOf.Beauty);
				// 设置故事数据的roomCleanliness字段为房间的清洁值
				taleData_Surroundings.roomCleanliness = roomOrAdjacent.GetStat(RoomStatDefOf.Cleanliness);
			}
			// 如果无法获取位置c的温度信息，设置故事数据的temperature字段为21f
			if (!GenTemperature.TryGetTemperatureForCell(c, map, out taleData_Surroundings.temperature))
			{
				taleData_Surroundings.temperature = 21f;
			}
			// 设置故事数据的snowDepth字段为地图上位置c的积雪深度
			taleData_Surroundings.snowDepth = map.snowGrid.GetDepth(c);
			// 返回生成的“周边环境”故事数据
			return taleData_Surroundings;
		}

		public static TaleData_Surroundings GenerateRandom(Map map)
		{
			return GenerateFrom(CellFinder.RandomCell(map), map);
		}
	}
}
