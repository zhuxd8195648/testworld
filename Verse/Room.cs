using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace Verse
{
	public sealed class Room
	{
		public sbyte mapIndex = -1;

		private RoomGroup groupInt;

		private List<Region> regions = new List<Region>();

		public int ID = -16161616;

		/// <summary>
		/// 最后改变标记
		/// </summary>
		public int lastChangeTick = -1;

		/// <summary>
		/// 触摸地图边缘的区域
		/// </summary>
		private int numRegionsTouchingMapEdge;

		/// <summary>
		/// 缓存的露天屋顶数量
		/// </summary>
		private int cachedOpenRoofCount = -1;

		/// <summary>
		/// 缓存的露天屋顶状态
		/// </summary>
		private IEnumerator<IntVec3> cachedOpenRoofState;

		/// <summary>
		/// 是监狱
		/// </summary>
		public bool isPrisonCell;

		/// <summary>
		/// 缓存的细胞计数
		/// </summary>
		private int cachedCellCount = -1;

		/// <summary>
		/// 统计和角色毒品
		/// </summary>
		private bool statsAndRoleDirty = true;

		/// <summary>
		/// 状态
		/// </summary>
		private DefMap<RoomStatDef, float> stats = new DefMap<RoomStatDef, float>();

		/// <summary>
		/// 职位
		/// </summary>
		private RoomRoleDef role;

		/// <summary>
		/// 新的或重用的房间组索引
		/// </summary>
		public int newOrReusedRoomGroupIndex = -1;

		/// <summary>
		/// 下一个房间ID
		/// </summary>
		private static int nextRoomID;

		/// <summary>
		/// 地区数量巨大
		/// </summary>
		private const int RegionCountHuge = 60;

		/// <summary>
		/// 最大区域分配房间角色职位
		/// </summary>
		private const int MaxRegionsToAssignRoomRole = 36;

		/// <summary>
		/// 监狱实地颜色
		/// </summary>
		private static readonly Color PrisonFieldColor = new Color(1f, 0.7f, 0.2f);

		/// <summary>
		/// 非监狱场地颜色
		/// </summary>
		private static readonly Color NonPrisonFieldColor = new Color(0.3f, 0.3f, 1f);

		/// <summary>
		/// 独特的邻居集
		/// </summary>
		private HashSet<Room> uniqueNeighborsSet = new HashSet<Room>();

		/// <summary>
		/// 独特的邻居
		/// </summary>
		private List<Room> uniqueNeighbors = new List<Room>();

		/// <summary>
		/// 唯一的包含物集
		/// </summary>
		private HashSet<Thing> uniqueContainedThingsSet = new HashSet<Thing>();

		/// <summary>
		/// 独特的东西
		/// </summary>
		private List<Thing> uniqueContainedThings = new List<Thing>();

		/// <summary>
		/// 唯一的包含Def的东西
		/// </summary>
		private HashSet<Thing> uniqueContainedThingsOfDef = new HashSet<Thing>();

		/// <summary>
		/// 场设置
		/// </summary>
		private static List<IntVec3> fields = new List<IntVec3>();

		public Map Map
		{
			get
			{
				if (mapIndex >= 0)
				{
					return Find.Maps[mapIndex];
				}
				return null;
			}
		}

		/// <summary>
		/// 区域类型
		/// </summary>
		public RegionType RegionType
		{
			get
			{
				if (!regions.Any())
				{
					return RegionType.None;
				}
				return regions[0].type;
			}
		}

		/// <summary>
		/// 区域
		/// </summary>
		public List<Region> Regions => regions;

		/// <summary>
		/// 区域总数
		/// </summary>
		public int RegionCount => regions.Count;

		/// <summary>
		/// 是巨大的
		/// </summary>
		public bool IsHuge => regions.Count > 60;

		/// <summary>
		/// 被间接引用的
		/// </summary>
		public bool Dereferenced => regions.Count == 0;
		
		/// <summary>
		/// 触摸地图边缘
		/// </summary>
		public bool TouchesMapEdge => numRegionsTouchingMapEdge > 0;
		
		/// <summary>
		/// 温度
		/// </summary>
		public float Temperature => Group.Temperature;

		/// <summary>
		/// 使用室外温度
		/// </summary>
		public bool UsesOutdoorTemperature => Group.UsesOutdoorTemperature;

		
		/// <summary>
		/// 房间组
		/// </summary>
		public RoomGroup Group
		{
			get
			{
				return groupInt;
			}
			set
			{
				if (value != groupInt)
				{
					if (groupInt != null)
					{
						groupInt.RemoveRoom(this);
					}
					groupInt = value;
					if (groupInt != null)
					{
						groupInt.AddRoom(this);
					}
				}
			}
		}

		/// <summary>
		/// 细胞数
		/// </summary>
		public int CellCount
		{
			get
			{
				if (cachedCellCount == -1)
				{
					cachedCellCount = 0;
					for (int i = 0; i < regions.Count; i++)
					{
						cachedCellCount += regions[i].CellCount;
					}
				}
				return cachedCellCount;
			}
		}

		/// <summary>
		/// 开放的屋顶计数
		/// </summary>
		public int OpenRoofCount => OpenRoofCountStopAt(int.MaxValue);

		///<summary>
		///判断是否在心理上室外
		///</summary>
		public bool PsychologicallyOutdoors
		{
   			 ///<summary>当区域内开放天窗数量大于等于300时返回真</summary>
    		get
   		 {
		//OpenRoofCountStopAt是一个方法，用于计算区域中开放的天窗数量。它的参数是一个整数值，表示在达到这个数量时停止计数。
        if (OpenRoofCountStopAt(300) >= 300)
        {
            return true;
        }
        ///<summary>当组中有任何一个房间接触到地图边缘并且开放的天窗数量超过总数的一半时返回真</summary>
        if (Group.AnyRoomTouchesMapEdge && (float)OpenRoofCount / (float)CellCount >= 0.5f)
        {
            return true;
        }
        return false;
    		}
		}	

		/// <summary>
		/// 在户外工作
		/// </summary>
		public bool OutdoorsForWork
		{
			get
			{
				if (OpenRoofCountStopAt(101) > 100 || (float)OpenRoofCount > (float)CellCount * 0.25f)
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// 邻居
		/// </summary>
		public List<Room> Neighbors
		{
			get
			{
				uniqueNeighborsSet.Clear();
				uniqueNeighbors.Clear();
				for (int i = 0; i < regions.Count; i++)
				{
					foreach (Region neighbor in regions[i].Neighbors)
					{
						if (uniqueNeighborsSet.Add(neighbor.Room) && neighbor.Room != this)
						{
							uniqueNeighbors.Add(neighbor.Room);
						}
					}
				}
				uniqueNeighborsSet.Clear();
				return uniqueNeighbors;
			}
		}

		/// <summary>
		/// 细胞
		/// </summary>
		public IEnumerable<IntVec3> Cells
		{
			get
			{
				for (int i = 0; i < regions.Count; i++)
				{
					foreach (IntVec3 cell in regions[i].Cells)
					{
						yield return cell;
					}
				}
			}
		}

		/// <summary>
		/// 边界细胞
		/// </summary>
		public IEnumerable<IntVec3> BorderCells
		{
			get
			{
				foreach (IntVec3 c in Cells)
				{
					int i = 0;
					while (i < 8)
					{
						IntVec3 intVec = c + GenAdj.AdjacentCells[i];
						Region region = (c + GenAdj.AdjacentCells[i]).GetRegion(Map);
						if (region == null || region.Room != this)
						{
							yield return intVec;
						}
						int num = i + 1;
						i = num;
					}
				}
			}
		}

		/// <summary>
		/// 所有者
		/// </summary>
		public IEnumerable<Pawn> Owners
		{
			get
			{
				if (TouchesMapEdge || IsHuge || (Role != RoomRoleDefOf.Bedroom && Role != RoomRoleDefOf.PrisonCell && Role != RoomRoleDefOf.Barracks && Role != RoomRoleDefOf.PrisonBarracks))
				{
					yield break;
				}
				Pawn pawn = null;
				Pawn secondOwner = null;
				foreach (Building_Bed containedBed in ContainedBeds)
				{
					if (!containedBed.def.building.bed_humanlike)
					{
						continue;
					}
					for (int i = 0; i < containedBed.OwnersForReading.Count; i++)
					{
						if (pawn == null)
						{
							pawn = containedBed.OwnersForReading[i];
							continue;
						}
						if (secondOwner == null)
						{
							secondOwner = containedBed.OwnersForReading[i];
							continue;
						}
						yield break;
					}
				}
				if (pawn != null)
				{
					if (secondOwner == null)
					{
						yield return pawn;
					}
					else if (LovePartnerRelationUtility.LovePartnerRelation已存在(pawn, secondOwner))
					{
						yield return pawn;
						yield return secondOwner;
					}
				}
			}
		}

		/// <summary>
		/// 包含床
		/// </summary>
		public IEnumerable<Building_Bed> ContainedBeds
		{
			get
			{
				List<Thing> things = ContainedAndAdjacentThings;
				for (int i = 0; i < things.Count; i++)
				{
					Building_Bed building_Bed = things[i] as Building_Bed;
					if (building_Bed != null)
					{
						yield return building_Bed;
					}
				}
			}
		}

		/// <summary>
		/// 有雾的
		/// </summary>
		public bool Fogged
		{
			get
			{
				if (regions.Count == 0)
				{
					return false;
				}
				return regions[0].AnyCell.Fogged(Map);
			}
		}

		/// <summary>
		/// 有门口
		/// </summary>
		public bool IsDoorway
		{
			get
			{
				if (regions.Count == 1)
				{
					return regions[0].IsDoorway;
				}
				return false;
			}
		}

		/// <summary>
		/// 包含的和相邻的事物
		/// </summary>
		public List<Thing> ContainedAndAdjacentThings
		{
			get
			{
				uniqueContainedThingsSet.Clear();
				uniqueContainedThings.Clear();
				for (int i = 0; i < regions.Count; i++)
				{
					List<Thing> allThings = regions[i].ListerThings.AllThings;
					if (allThings == null)
					{
						continue;
					}
					for (int j = 0; j < allThings.Count; j++)
					{
						Thing item = allThings[j];
						if (uniqueContainedThingsSet.Add(item))
						{
							uniqueContainedThings.Add(item);
						}
					}
				}
				uniqueContainedThingsSet.Clear();
				return uniqueContainedThings;
			}
		}

		/// <summary>
		/// 职位
		/// </summary>
		public RoomRoleDef Role
		{
			get
			{
				if (statsAndRoleDirty)
				{
					UpdateRoomStatsAndRole();
				}
				return role;
			}
		}

		/// <summary>
		/// 更新
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static Room MakeNew(Map map)
		{
			Room result = new Room
			{
				mapIndex = (sbyte)map.Index,
				ID = nextRoomID
			};
			nextRoomID++;
			return result;
		}

		/// <summary>
		/// 添加区域
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public void AddRegion(Region r)
		{
			if (regions.Contains(r))
			{
				Log.Error(string.Concat("尝试将相同的区域添加到房间两次.区域 =", r, ", room=", this));
				return;
			}
			regions.Add(r);
			if (r.touchesMapEdge)
			{
				numRegionsTouchingMapEdge++;
			}
			if (regions.Count == 1)
			{
				Map.regionGrid.allRooms.Add(this);
			}
		}

		/// <summary>
		/// 删除区域
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public void RemoveRegion(Region r)
		{
			if (!regions.Contains(r))
			{
				Log.Error(string.Concat("试图从房间中删除区域，但该区域不在这里.地区 =", r, ", room=", this));
				return;
			}
			regions.Remove(r);
			if (r.touchesMapEdge)
			{
				numRegionsTouchingMapEdge--;
			}
			if (regions.Count == 0)
			{
				Group = null;
				cachedOpenRoofCount = -1;
				cachedOpenRoofState = null;
				statsAndRoleDirty = true;
				Map.regionGrid.allRooms.Remove(this);
			}
		}

		/// <summary>
		/// 通知关联删除
		/// </summary>
		/// <returns></returns>
		public void Notify_MyMapRemoved()
		{
			mapIndex = -1;
		}

		/// <summary>
		/// 通知所包含的对象已生成或已删除
		/// </summary>
		/// <param name="th"></param>
		/// <returns></returns>
		public void Notify_ContainedThingSpawnedOrDespawned(Thing th)
		{
			if (th.def.category == ThingCategory.Mote || th.def.category == ThingCategory.Projectile || th.def.category == ThingCategory.Ethereal || th.def.category == ThingCategory.Pawn)
			{
				return;
			}
			if (IsDoorway)
			{
				for (int i = 0; i < regions[0].links.Count; i++)
				{
					Region otherRegion = regions[0].links[i].GetOtherRegion(regions[0]);
					if (otherRegion != null && !otherRegion.IsDoorway)
					{
						otherRegion.Room.Notify_ContainedThingSpawnedOrDespawned(th);
					}
				}
			}
			statsAndRoleDirty = true;
		}

		/// <summary>
		/// 通知地形变化
		/// </summary>
		/// <returns></returns>
		public void Notify_TerrainChanged()
		{
			statsAndRoleDirty = true;
		}

		/// <summary>
		/// 通知床类型更改
		/// </summary>
		/// <returns></returns>
		public void Notify_BedTypeChanged()
		{
			statsAndRoleDirty = true;
		}

		/// <summary>
		/// 通知屋顶改变
		/// </summary>
		/// <returns></returns>
		public void Notify_RoofChanged()
		{
			cachedOpenRoofCount = -1;
			cachedOpenRoofState = null;
			Group.Notify_RoofChanged();
		}

		/// <summary>
		/// 通知房间形状或包含的床已更改
		/// </summary>
		/// <returns></returns>
		public void Notify_RoomShapeOrContainedBedsChanged()
		{
			cachedCellCount = -1;
			cachedOpenRoofCount = -1;
			cachedOpenRoofState = null;
			if (Current.ProgramState == ProgramState.Playing && !Fogged)
			{
				Map.autoBuildRoofAreaSetter.TryGenerateAreaFor(this);
			}
			isPrisonCell = false;
			if (Building_Bed.RoomCanBePrisonCell(this))
			{
				List<Thing> containedAndAdjacentThings = ContainedAndAdjacentThings;
				for (int i = 0; i < containedAndAdjacentThings.Count; i++)
				{
					Building_Bed building_Bed = containedAndAdjacentThings[i] as Building_Bed;
					if (building_Bed != null && building_Bed.ForPrisoners)
					{
						isPrisonCell = true;
						break;
					}
				}
			}
			List<Thing> list = Map.listerThings.ThingsOfDef(ThingDefOf.NutrientPasteDispenser);
			for (int j = 0; j < list.Count; j++)
			{
				list[j].Notify_ColorChanged();
			}
			if (Current.ProgramState == ProgramState.Playing && isPrisonCell)
			{
				foreach (Building_Bed containedBed in ContainedBeds)
				{
					containedBed.ForPrisoners = true;
				}
			}
			lastChangeTick = Find.TickManager.TicksGame;
			statsAndRoleDirty = true;
			FacilitiesUtility.NotifyFacilitiesAboutChangedLOSBlockers(regions);
		}

		/// <summary>
		/// 是否包含单元格
		/// </summary>
		/// <param name="cell"></param>
		/// <returns></returns>
		public bool ContainsCell(IntVec3 cell)
		{
			if (Map != null)
			{
				return cell.GetRoom(Map, RegionType.Set_All) == this;
			}
			return false;
		}

		/// <summary>
		/// 是否包含的东西
		/// </summary>
		/// <param name="def"></param>
		/// <returns></returns>
		public bool ContainsThing(ThingDef def)
		{
			for (int i = 0; i < regions.Count; i++)
			{
				if (regions[i].ListerThings.ThingsOfDef(def).Any())
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// 包含了一些东西
		/// </summary>
		/// <param name="def"></param>
		/// <returns></returns>
		public IEnumerable<Thing> ContainedThings(ThingDef def)
		{
			uniqueContainedThingsOfDef.Clear();
			int i = 0;
			while (i < regions.Count)
			{
				List<Thing> things = regions[i].ListerThings.ThingsOfDef(def);
				int num;
				for (int j = 0; j < things.Count; j = num)
				{
					if (uniqueContainedThingsOfDef.Add(things[j]))
					{
						yield return things[j];
					}
					num = j + 1;
				}
				num = i + 1;
				i = num;
			}
			uniqueContainedThingsOfDef.Clear();
		}

		/// <summary>
		/// 东西总数
		/// </summary>
		/// <param name="def"></param>
		/// <returns></returns>
		public int ThingCount(ThingDef def)
		{
			uniqueContainedThingsOfDef.Clear();
			int num = 0;
			for (int i = 0; i < regions.Count; i++)
			{
				List<Thing> list = regions[i].ListerThings.ThingsOfDef(def);
				for (int j = 0; j < list.Count; j++)
				{
					if (uniqueContainedThingsOfDef.Add(list[j]))
					{
						num += list[j].stackCount;
					}
				}
			}
			uniqueContainedThingsOfDef.Clear();
			return num;
		}

		/// <summary>
		/// 地图索引
		/// </summary>
		/// <returns></returns>
		public void DecrementMapIndex()
		{
			if (mapIndex <= 0)
			{
				Log.Warning("Tried to decrement map index for room " + ID + ", but mapIndex=" + mapIndex);
			}
			else
			{
				mapIndex--;
			}
		}

		/// <summary>
		/// 得到状态
		/// </summary>
		/// <param name="roomStat"></param>
		/// <returns></returns>
		public float GetStat(RoomStatDef roomStat)
		{
			if (statsAndRoleDirty)
			{
				UpdateRoomStatsAndRole();
			}
			if (stats == null)
			{
				return roomStat.roomlessScore;
			}
			return stats[roomStat];
		}

		/// <summary>
		/// 获取统计核心阶段
		/// </summary>
		/// <param name="stat"></param>
		/// <returns></returns>
		public RoomStatScoreStage GetStatScoreStage(RoomStatDef stat)
		{
			return stat.GetScoreStage(GetStat(stat));
		}

		/// <summary>
		/// 绘制场边
		/// </summary>
		/// <returns></returns>
		public void DrawFieldEdges()
		{
			fields.Clear();
			fields.AddRange(Cells);
			Color color = (isPrisonCell ? PrisonFieldColor : NonPrisonFieldColor);
			color.a = Pulser.PulseBrightness(1f, 0.6f);
			GenDraw.DrawFieldEdges(fields, color);
			fields.Clear();
		}

		/// <summary>
		/// 开放屋顶计数停止
		/// </summary>
		/// <param name="threshold"></param>
		/// <returns></returns>
		public int OpenRoofCountStopAt(int threshold)
		{
			if (cachedOpenRoofCount == -1 && cachedOpenRoofState == null)
			{
				cachedOpenRoofCount = 0;
				cachedOpenRoofState = Cells.GetEnumerator();
			}
			if (cachedOpenRoofCount < threshold && cachedOpenRoofState != null)
			{
				RoofGrid roofGrid = Map.roofGrid;
				while (cachedOpenRoofCount < threshold && cachedOpenRoofState.MoveNext())
				{
					if (!roofGrid.Roofed(cachedOpenRoofState.Current))
					{
						cachedOpenRoofCount++;
					}
				}
				if (cachedOpenRoofCount < threshold)
				{
					cachedOpenRoofState = null;
				}
			}
			return cachedOpenRoofCount;
		}

		/// <summary>
		/// 更新房间状态和角色职位
		/// </summary>
		/// <returns></returns>
		private void UpdateRoomStatsAndRole()
		{
			statsAndRoleDirty = false;
			if (!TouchesMapEdge && RegionType == RegionType.Normal && regions.Count <= 36)
			{
				if (stats == null)
				{
					stats = new DefMap<RoomStatDef, float>();
				}
				foreach (RoomStatDef item in DefDatabase<RoomStatDef>.AllDefs.OrderByDescending((RoomStatDef x) => x.updatePriority))
				{
					stats[item] = item.Worker.GetScore(this);
				}
				role = DefDatabase<RoomRoleDef>.AllDefs.MaxBy((RoomRoleDef x) => x.Worker.GetScore(this));
			}
			else
			{
				stats = null;
				role = RoomRoleDefOf.None;
			}
		}

		/// <summary>
		/// 调试绘图
		/// </summary>
		/// <returns></returns>
		internal void DebugDraw()
		{
			int hashCode = GetHashCode();
			foreach (IntVec3 cell in Cells)
			{
				CellRenderer.RenderCell(cell, (float)hashCode * 0.01f);
			}
		}

		/// <summary>
		/// 字符串调试
		/// </summary>
		/// <returns></returns>
		internal string DebugString()
		{
			return string.Concat("Room ID=", ID, "\n  first cell=", Cells.FirstOrDefault(), "\n  RegionCount=", RegionCount, "\n  RegionType=", RegionType, "\n  CellCount=", CellCount, "\n  OpenRoofCount=", OpenRoofCount, "\n  numRegionsTouchingMapEdge=", numRegionsTouchingMapEdge, "\n  lastChangeTick=", lastChangeTick, "\n  isPrisonCell=", isPrisonCell.ToString(), "\n  RoomGroup=", (Group != null) ? Group.ID.ToString() : "null");
		}

		/// <summary>
		/// 将字符串重写为
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "Room(roomID=" + ID + ", first=" + Cells.FirstOrDefault().ToString() + ", RegionsCount=" + RegionCount.ToString() + ", lastChangeTick=" + lastChangeTick + ")";
		}

		/// <summary>
		/// 得到哈希值
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Gen.HashCombineInt(ID, 1538478890);
		}
	}
}
