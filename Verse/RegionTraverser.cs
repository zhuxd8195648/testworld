using System;
using System.Collections.Generic;

namespace Verse
{
	public static class RegionTraverser
	{
		private class BFSWorker
		{
			private Queue<Region> open = new Queue<Region>();

			private int numRegionsProcessed;

			private uint closedIndex = 1u;

			private int closedArrayPos;

			private const int skippableRegionSize = 4;

			public BFSWorker(int closedArrayPos)
			{
				this.closedArrayPos = closedArrayPos;
			}

			public void Clear()
			{
				open.Clear();
			}

			// 这是在找到新区域时调用的方法 
			private void QueueNewOpenRegion(Region region)
			{
				// 如果该区域已经关闭
				if (region.closedIndex[closedArrayPos] == closedIndex) 
				{
					throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region.ToString());
				}
				open.Enqueue(region); // 将该区域加入到队列中
				region.closedIndex[closedArrayPos] = closedIndex; // 将该区域的关闭索引设置为当前索引
			}

			private void FinalizeSearch()
			{
			}

			public void BreadthFirstTraverseWork(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				// 如果根区域不可遍历，则返回 
				if ((root.type & traversableRegionTypes) == 0) 
					return;
				}
				closedIndex++;// 闭合索引加1
				open.Clear(); // 清空队列
				numRegionsProcessed = 0; // 已处理区域数为0
				QueueNewOpenRegion(root); // 将根区域加入队列
				while (open.Count > 0)
				{
					Region region = open.Dequeue(); // 从队列中取出一个区域
					if (DebugViewSettings.drawRegionTraversal) // 如果开启了区域遍历调试
					{
						region.Debug_Notify_Traversed(); // 通知该区域被遍历
					}
					if (regionProcessor != null && regionProcessor(region)) // 如果区域处理器不为空且返回true
					{
						FinalizeSearch(); // 结束搜索
						return; 
					}
					if (ShouldCountRegion(region)) // 如果该区域应该被计数
					{
						numRegionsProcessed++; // 已处理区域数加1
					}
					if (numRegionsProcessed >= maxRegions) // 如果已处理区域数大于等于最大区域数
					{
						FinalizeSearch(); // 结束搜索
						return;
					}
					for (int i = 0; i < region.links.Count; i++)  // 遍历该区域的所有连接
					{
						RegionLink regionLink = region.links[i]; // 获取连接
						for (int j = 0; j < 2; j++) //遍历连接的两个区域
						{
							Region region2 = regionLink.regions[j]; // 获取连接的另一个区域
							// 如果该区域不为空且未关闭且可遍历且入口条件为空或者入口条件返回true
							if (region2 != null && region2.closedIndex[closedArrayPos] != closedIndex && (region2.type & traversableRegionTypes) != 0 && (entryCondition == null || entryCondition(region, region2))) 
							{
								QueueNewOpenRegion(region2); // 将该区域加入队列
							}
						}
					}
				}
				FinalizeSearch(); // 结束搜索
			}
		}

		private static Queue<BFSWorker> freeWorkers;

		public static int NumWorkers;

		public static readonly RegionEntryPredicate PassAll;

		public static Room FloodAndSetRooms(Region root, Map map, Room existingRoom)
		{
			Room floodingRoom;
			if (existingRoom == null)
			{
				floodingRoom = Room.MakeNew(map);
			}
			else
			{
				floodingRoom = existingRoom;
			}
			root.Room = floodingRoom;
			if (!root.type.AllowsMultipleRegionsPerRoom())
			{
				return floodingRoom;
			}
			RegionEntryPredicate entryCondition = (Region from, Region r) => r.type == root.type && r.Room != floodingRoom;
			RegionProcessor regionProcessor = delegate(Region r)
			{
				r.Room = floodingRoom;
				return false;
			};
			BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
			return floodingRoom;
		}

		public static void FloodAndSetNewRegionIndex(Region root, int newRegionGroupIndex)
		{
			root.newRegionGroupIndex = newRegionGroupIndex;
			if (root.type.AllowsMultipleRegionsPerRoom())
			{
				RegionEntryPredicate entryCondition = (Region from, Region r) => r.type == root.type && r.newRegionGroupIndex < 0;
				RegionProcessor regionProcessor = delegate(Region r)
				{
					r.newRegionGroupIndex = newRegionGroupIndex;
					return false;
				};
				BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
			}
		}

		public static bool WithinRegions(this IntVec3 A, IntVec3 B, Map map, int regionLookCount, TraverseParms traverseParams, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			Region region = A.GetRegion(map, traversableRegionTypes);
			if (region == null)
			{
				return false;
			}
			Region regB = B.GetRegion(map, traversableRegionTypes);
			if (regB == null)
			{
				return false;
			}
			if (region == regB)
			{
				return true;
			}
			RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParams, isDestination: false);
			bool found = false;
			RegionProcessor regionProcessor = delegate(Region r)
			{
				if (r == regB)
				{
					found = true;
					return true;
				}
				return false;
			};
			BreadthFirstTraverse(region, entryCondition, regionProcessor, regionLookCount, traversableRegionTypes);
			return found;
		}

		public static void MarkRegionsBFS(Region root, RegionEntryPredicate entryCondition, int maxRegions, int inRadiusMark, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			BreadthFirstTraverse(root, entryCondition, delegate(Region r)
			{
				r.mark = inRadiusMark;
				return false;
			}, maxRegions, traversableRegionTypes);
		}

		public static bool ShouldCountRegion(Region r)
		{
			return !r.IsDoorway;
		}

		static RegionTraverser()
		{
			freeWorkers = new Queue<BFSWorker>();
			NumWorkers = 8;
			PassAll = (Region from, Region to) => true;
			RecreateWorkers();
		}

		public static void RecreateWorkers()
		{
			freeWorkers.Clear();
			for (int i = 0; i < NumWorkers; i++)
			{
				freeWorkers.Enqueue(new BFSWorker(i));
			}
		}

		public static void BreadthFirstTraverse(IntVec3 start, Map map, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			Region region = start.GetRegion(map, traversableRegionTypes);
			if (region != null)
			{
				BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			}
		}

		public static void BreadthFirstTraverse(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			if (freeWorkers.Count == 0)
			{
				Log.Error("No free workers for breadth-first traversal. Either BFS recurred deeper than " + NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.");
				return;
			}
			if (root == null)
			{
				Log.Error("BreadthFirstTraverse with null root region.");
				return;
			}
			BFSWorker bFSWorker = freeWorkers.Dequeue();
			try
			{
				bFSWorker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			}
			catch (Exception ex)
			{
				Log.Error("Exception in BreadthFirstTraverse: " + ex.ToString());
			}
			finally
			{
				bFSWorker.Clear();
				freeWorkers.Enqueue(bFSWorker);
			}
		}
		
	}
}
