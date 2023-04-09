using System;
using System.Collections.Generic;
using System.Linq;

namespace Verse
{
	/// <summary>
	/// 区域和房间更新器
	/// </summary>
	public class RegionAndRoomUpdater
	{
		private Map map;
		/// <summary>
		/// 新区域
		/// </summary>
		private List<Region> newRegions = new List<Region>();

		/// <summary>
		/// 新房间
		/// </summary>
		private List<Room> newRooms = new List<Room>();

		/// <summary>
		/// 重置旧房间
		/// </summary>
		private HashSet<Room> reusedOldRooms = new HashSet<Room>();

		/// <summary>
		/// 新房间组
		/// </summary>
		private List<RoomGroup> newRoomGroups = new List<RoomGroup>();

		/// <summary>
		/// 重置旧房间组
		/// </summary>
		private HashSet<RoomGroup> reusedOldRoomGroups = new HashSet<RoomGroup>();

		/// <summary>
		/// 当前区域组
		/// </summary>
		private List<Region> currentRegionGroup = new List<Region>();

		/// <summary>
		/// 当前房间组
		/// </summary>
		private List<Room> currentRoomGroup = new List<Room>();

		/// <summary>
		/// tmp房间堆栈
		/// </summary>
		private Stack<Room> tmpRoomStack = new Stack<Room>();

		/// <summary>
		/// tmp显示房间
		/// </summary>
		private HashSet<Room> tmpVisitedRooms = new HashSet<Room>();

		/// <summary>
		/// 是否初始化
		/// </summary>
		private bool initialized;

		/// <summary>
		/// 是否在工作
		/// </summary>
		private bool working;

		/// <summary>
		/// 是否启动int
		/// </summary>
		private bool enabledInt = true;

		/// <summary>
		/// 是否开启
		/// </summary>
		public bool Enabled
		{
			get
			{
				return enabledInt;
			}
			set
			{
				enabledInt = value;
			}
		}

		/// <summary>
		/// 任何重建
		/// </summary>
		public bool AnythingToRebuild
		{
			get
			{
				if (!map.regionDirtyer.AnyDirty)
				{
					return !initialized;
				}
				return true;
			}
		}

		public RegionAndRoomUpdater(Map map)
		{
			this.map = map;
		}

		/// <summary>
		/// 重建所有区域和房间
		/// </summary>
		/// <returns></returns>
		public void RebuildAllRegionsAndRooms()
		{
			if (!Enabled)
			{
				Log.Warning("调用RebuildAllRegionsAndRooms()，但RegionAndRoomUpdater被禁用。 地区不会重建.");
			}
			map.temperatureCache.ResetTemperatureCache();
			map.regionDirtyer.SetAllDirty();
			TryRebuildDirtyRegionsAndRooms();
		}

		/// <summary>
		/// 尝试重建肮脏的区域和房间
		/// </summary>
		/// <returns></returns>
		public void TryRebuildDirtyRegionsAndRooms()
		{
			if (working || !Enabled)
			{
				return;
			}
			working = true;
			if (!initialized)
			{
				RebuildAllRegionsAndRooms();
			}
			if (!map.regionDirtyer.AnyDirty)
			{
				working = false;
				return;
			}
			try
			{
				RegenerateNewRegionsFromDirtyCells();
				CreateOrUpdateRooms();
			}
			catch (Exception arg)
			{
				Log.Error("在重建脏区域时异常: " + arg);
			}
			newRegions.Clear();
			map.regionDirtyer.SetAllClean();
			initialized = true;
			working = false;
			if (DebugSettings.detectRegionListersBugs)
			{
				Autotests_RegionListers.CheckBugs(map);
			}
		}

		/// <summary>
		/// 从脏细胞中再生新区域
		/// </summary>
		/// <returns></returns>
		private void RegenerateNewRegionsFromDirtyCells()
		{
			newRegions.Clear();
			List<IntVec3> dirtyCells = map.regionDirtyer.DirtyCells;
			for (int i = 0; i < dirtyCells.Count; i++)
			{
				IntVec3 intVec = dirtyCells[i];
				if (intVec.GetRegion(map, RegionType.Set_All) == null)
				{
					Region region = map.regionMaker.TryGenerateRegionFrom(intVec);
					if (region != null)
					{
						newRegions.Add(region);
					}
				}
			}
		}

		/// <summary>
		/// 创建或更新房间
		/// </summary>
		/// <returns></returns>
		private void CreateOrUpdateRooms()
		{
			// 清除新建的房间列表和重复使用的旧房间列表
			newRooms.Clear();
			reusedOldRooms.Clear();

			// 清除新的房间组列表和重复使用的旧房间组列表
			newRoomGroups.Clear();
			reusedOldRoomGroups.Clear();

			// 将新区域合并成连续的组，并返回组数
			int numRegionGroups = CombineNewRegionsIntoContiguousGroups();

			// 创建或将区域附加到现有房间中
			CreateOrAttachToExistingRooms(numRegionGroups);

			// 将新的和重复使用的房间组合并成连续的组，并返回组数
			int numRoomGroups = CombineNewAndReusedRoomsIntoContiguousGroups();

			// 创建或将房间附加到现有房间组中
			CreateOrAttachToExistingRoomGroups(numRoomGroups);

			// 通知受影响的房间和房间组，并更新温度
			NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature();

			// 清除新建的房间列表、重用的旧房间列表、新建的房间组列表和重用的旧房间组列表。
			newRooms.Clear();
			reusedOldRooms.Clear();
			newRoomGroups.Clear();
			reusedOldRoomGroups.Clear();
		}

		/// <summary>
		/// 将新的区域合并到相邻的组中
		/// </summary>
		/// <returns></returns>
		private int CombineNewRegionsIntoContiguousGroups()
		{
			int num = 0;
			for (int i = 0; i < newRegions.Count; i++)
			{
				if (newRegions[i].newRegionGroupIndex < 0)
				{
					RegionTraverser.FloodAndSetNewRegionIndex(newRegions[i], num);
					num++;
				}
			}
			return num;
		}

		/// <summary>
		/// 创建或附加到现有的房间
		/// </summary>
		/// <param name="numRegionGroups"></param>
		/// <returns></returns>
		private void CreateOrAttachToExistingRooms(int numRegionGroups)
		{
			for (int i = 0; i < numRegionGroups; i++)
			{
				currentRegionGroup.Clear();
				for (int j = 0; j < newRegions.Count; j++)
				{
					if (newRegions[j].newRegionGroupIndex == i)
					{
						currentRegionGroup.Add(newRegions[j]);
					}
				}
				if (!currentRegionGroup[0].type.AllowsMultipleRegionsPerRoom())
				{
					if (currentRegionGroup.Count != 1)
					{
						Log.Error("区域类型不允许每个房间有多个区域，但在这个组中有>1个区域.");
					}
					Room room = Room.MakeNew(map);
					currentRegionGroup[0].Room = room;
					newRooms.Add(room);
					continue;
				}
				bool multipleOldNeighborRooms;
				Room room2 = FindCurrentRegionGroupNeighborWithMostRegions(out multipleOldNeighborRooms);
				if (room2 == null)
				{
					Room item = RegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, null);
					newRooms.Add(item);
				}
				else if (!multipleOldNeighborRooms)
				{
					for (int k = 0; k < currentRegionGroup.Count; k++)
					{
						currentRegionGroup[k].Room = room2;
					}
					reusedOldRooms.Add(room2);
				}
				else
				{
					RegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, room2);
					reusedOldRooms.Add(room2);
				}
			}
		}

		/// <summary>
		/// 将新的和重复使用的房间组合成连续的组 
		/// </summary>
		/// <returns></returns>
		private int CombineNewAndReusedRoomsIntoContiguousGroups()
		{
			int num = 0;
			foreach (Room reusedOldRoom in reusedOldRooms)
			{
				reusedOldRoom.newOrReusedRoomGroupIndex = -1;
			}
			foreach (Room item in reusedOldRooms.Concat(newRooms))
			{
				if (item.newOrReusedRoomGroupIndex >= 0)
				{
					continue;
				}
				tmpRoomStack.Clear();
				tmpRoomStack.Push(item);
				item.newOrReusedRoomGroupIndex = num;
				while (tmpRoomStack.Count != 0)
				{
					Room room = tmpRoomStack.Pop();
					foreach (Room neighbor in room.Neighbors)
					{
						if (neighbor.newOrReusedRoomGroupIndex < 0 && ShouldBeInTheSameRoomGroup(room, neighbor))
						{
							neighbor.newOrReusedRoomGroupIndex = num;
							tmpRoomStack.Push(neighbor);
						}
					}
				}
				tmpRoomStack.Clear();
				num++;
			}
			return num;
		}

		/// <summary>
		/// 创建或附加到现有的房间组
		/// </summary>
		/// <param name="numRoomGroups"></param>
		/// <returns></returns>
		private void CreateOrAttachToExistingRoomGroups(int numRoomGroups)
		{
			for (int i = 0; i < numRoomGroups; i++)
			{
				currentRoomGroup.Clear();
				foreach (Room reusedOldRoom in reusedOldRooms)
				{
					if (reusedOldRoom.newOrReusedRoomGroupIndex == i)
					{
						currentRoomGroup.Add(reusedOldRoom);
					}
				}
				for (int j = 0; j < newRooms.Count; j++)
				{
					if (newRooms[j].newOrReusedRoomGroupIndex == i)
					{
						currentRoomGroup.Add(newRooms[j]);
					}
				}
				bool multipleOldNeighborRoomGroups;
				RoomGroup roomGroup = FindCurrentRoomGroupNeighborWithMostRegions(out multipleOldNeighborRoomGroups);
				if (roomGroup == null)
				{
					RoomGroup roomGroup2 = RoomGroup.MakeNew(map);
					FloodAndSetRoomGroups(currentRoomGroup[0], roomGroup2);
					newRoomGroups.Add(roomGroup2);
				}
				else if (!multipleOldNeighborRoomGroups)
				{
					for (int k = 0; k < currentRoomGroup.Count; k++)
					{
						currentRoomGroup[k].Group = roomGroup;
					}
					reusedOldRoomGroups.Add(roomGroup);
				}
				else
				{
					FloodAndSetRoomGroups(currentRoomGroup[0], roomGroup);
					reusedOldRoomGroups.Add(roomGroup);
				}
			}
		}

		/// <summary>
		/// 洪水和设置房间组
		/// </summary>
		/// <param name="start"></param>
		/// <param name="roomGroup"></param>
		/// <returns></returns>
		private void FloodAndSetRoomGroups(Room start, RoomGroup roomGroup)
		{
			tmpRoomStack.Clear();
			tmpRoomStack.Push(start);
			tmpVisitedRooms.Clear();
			tmpVisitedRooms.Add(start);
			while (tmpRoomStack.Count != 0)
			{
				Room room = tmpRoomStack.Pop();
				room.Group = roomGroup;
				foreach (Room neighbor in room.Neighbors)
				{
					if (!tmpVisitedRooms.Contains(neighbor) && ShouldBeInTheSameRoomGroup(room, neighbor))
					{
						tmpRoomStack.Push(neighbor);
						tmpVisitedRooms.Add(neighbor);
					}
				}
			}
			tmpVisitedRooms.Clear();
			tmpRoomStack.Clear();
		}

		/// <summary>
		/// 通知受影响的房间和房间组并更新温度 
		/// </summary>
		/// <returns></returns>
		private void NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature()
		{
			foreach (Room reusedOldRoom in reusedOldRooms)
			{
				reusedOldRoom.Notify_RoomShapeOrContainedBedsChanged();
			}
			for (int i = 0; i < newRooms.Count; i++)
			{
				newRooms[i].Notify_RoomShapeOrContainedBedsChanged();
			}
			foreach (RoomGroup reusedOldRoomGroup in reusedOldRoomGroups)
			{
				reusedOldRoomGroup.Notify_RoomGroupShapeChanged();
			}
			for (int j = 0; j < newRoomGroups.Count; j++)
			{
				RoomGroup roomGroup = newRoomGroups[j];
				roomGroup.Notify_RoomGroupShapeChanged();
				if (map.temperatureCache.TryGetAverageCachedRoomGroupTemp(roomGroup, out var result))
				{
					roomGroup.Temperature = result;
				}
			}
		}

		/// <summary>
		/// 查找具有最多区域的当前区域组邻居 
		/// </summary>
		/// <param name="multipleOldNeighborRooms"></param>
		/// <returns></returns>
		private Room FindCurrentRegionGroupNeighborWithMostRegions(out bool multipleOldNeighborRooms)
		{
			multipleOldNeighborRooms = false;
			Room room = null;
			for (int i = 0; i < currentRegionGroup.Count; i++)
			{
				foreach (Region item in currentRegionGroup[i].NeighborsOfSameType)
				{
					if (item.Room == null || reusedOldRooms.Contains(item.Room))
					{
						continue;
					}
					if (room == null)
					{
						room = item.Room;
					}
					else if (item.Room != room)
					{
						multipleOldNeighborRooms = true;
						if (item.Room.RegionCount > room.RegionCount)
						{
							room = item.Room;
						}
					}
				}
			}
			return room;
		}

		/// <summary>
		/// 查找当前房间组邻居最多的区域 
		/// </summary>
		/// <param name="multipleOldNeighborRoomGroups"></param>
		/// <returns></returns>
		private RoomGroup FindCurrentRoomGroupNeighborWithMostRegions(out bool multipleOldNeighborRoomGroups)
		{
			multipleOldNeighborRoomGroups = false;
			RoomGroup roomGroup = null;
			for (int i = 0; i < currentRoomGroup.Count; i++)
			{
				foreach (Room neighbor in currentRoomGroup[i].Neighbors)
				{
					if (neighbor.Group == null || !ShouldBeInTheSameRoomGroup(currentRoomGroup[i], neighbor) || reusedOldRoomGroups.Contains(neighbor.Group))
					{
						continue;
					}
					if (roomGroup == null)
					{
						roomGroup = neighbor.Group;
					}
					else if (neighbor.Group != roomGroup)
					{
						multipleOldNeighborRoomGroups = true;
						if (neighbor.Group.RegionCount > roomGroup.RegionCount)
						{
							roomGroup = neighbor.Group;
						}
					}
				}
			}
			return roomGroup;
		}

		/// <summary>
		/// 应该住在同一个房间吗
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		private bool ShouldBeInTheSameRoomGroup(Room a, Room b)
		{
			RegionType regionType = a.RegionType;
			RegionType regionType2 = b.RegionType;
			if (regionType == RegionType.Normal || regionType == RegionType.ImpassableFreeAirExchange)
			{
				if (regionType2 != RegionType.Normal)
				{
					return regionType2 == RegionType.ImpassableFreeAirExchange;
				}
				return true;
			}
			return false;
		}
	}
}
