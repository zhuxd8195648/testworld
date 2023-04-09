using RimWorld;
using UnityEngine;

namespace Verse
{
public class RoofGrid
{
	// 声明一个Map 类型的变量
private Map map; 

// 声明一个RoofDef类型的数组roofGrid，用来存储屋顶信息
private RoofDef[] roofGrid;

// 声明一个CellBoolDrawer类型的drawerInt变量
private CellBoolDrawer drawerInt;

// 定义Drawer 属性，如果drawerInt 为空，则新建一个CellBoolDrawer对象，并返回drawerInt
public CellBoolDrawer Drawer
{
    get
    {
        if (drawerInt == null)
        {
            drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z, 3620);
        }
        return drawerInt;
    }
}

// 定义一个Color属性Color，返回颜色值(0.3,1.0,0.4)
public Color Color => new Color(0.3f, 1f, 0.4f);

// 构造方法，用于初始化RoofGrid对象
public RoofGrid(Map map)
{
    this.map = map;
    roofGrid = new RoofDef[map.cellIndices.NumGridCells];
}

// 序列化方法 
public void ExposeData()
{
    // 调用MapExposeUtility.ExposeUshort方法，将roofs数组的数据进行序列化并保存到磁盘
    MapExposeUtility.ExposeUshort(map, (IntVec3 c) => (ushort)((roofGrid[map.cellIndices.CellToIndex(c)] != null) ? roofGrid[map.cellIndices.CellToIndex(c)].shortHash : 0), delegate(IntVec3 c, ushort val)
    {
        SetRoof(c, DefDatabase<RoofDef>.GetByShortHash(val));
    }, "roofs");
}

// 获取指定网格坐标的屋顶信息，返回bool类型值
public bool GetCellBool(int index)
{
    if (roofGrid[index] != null)
    {
        return !map.fogGrid.IsFogged(index);
    }
    return false;
}

// 获取指定网格坐标的颜色
public Color GetCellExtraColor(int index)
{
    if (RoofDefOf.RoofRockThick != null && roofGrid[index] == RoofDefOf.RoofRockThick)
    {
        return Color.gray;
    }
    return Color.white;
}

		// 判断该索引处是否有屋顶
    public bool Roofed(int index)
    {
        return roofGrid[index] != null;
    }

    // 判断某个坐标处是否有屋顶
    public bool Roofed(int x, int z)
    {
        return roofGrid[map.cellIndices.CellToIndex(x, z)] != null;
    }

    // 判断某个坐标处是否有屋顶
    public bool Roofed(IntVec3 c)
    {
        return roofGrid[map.cellIndices.CellToIndex(c)] != null;
    }

    // 获取某个索引处的屋顶定义信息
    public RoofDef RoofAt(int index)
    {
        return roofGrid[index];
    }

    // 获取某个坐标处的屋顶定义信息
    public RoofDef RoofAt(IntVec3 c)
    {
        return roofGrid[map.cellIndices.CellToIndex(c)];
    }

    // 获取某个坐标处的屋顶定义信息
    public RoofDef RoofAt(int x, int z)
    {
        return roofGrid[map.cellIndices.CellToIndex(x, z)];
    }

    // 设置某个坐标上的屋顶
    public void SetRoof(IntVec3 c, RoofDef def)
    {
        if (roofGrid[map.cellIndices.CellToIndex(c)] != def)
        {
            // 设置屋顶，并标记矩形内的所有格子为“需要重新计算光亮度”
            roofGrid[map.cellIndices.CellToIndex(c)] = def;
            map.glowGrid.MarkGlowGridDirty(c);
            // 更新该区域的房间，因为屋顶改变可能会对区域的性质造成影响
            map.regionGrid.GetValidRegionAt_NoRebuild(c)?.Room.Notify_RoofChanged();
            // 该代码段更新了屋顶绘制工具的参数
            if (drawerInt != null)
            {
                drawerInt.SetDirty();
            }
            // 通知地图网格更新屋顶信息
            map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
        }
    }

    // 更新屋顶信息
    public void RoofGridUpdate()
    {
        // 如果显示屋顶叠加效果，则将屋顶绘制工具添加到“需要重新绘制”列表中
        if (Find.PlaySettings.showRoofOverlay)
        {
            Drawer.MarkForDraw();
        }
        // 更新所有单元格的布尔值矩阵
        Drawer.CellBoolDrawerUpdate();
    }
	}
}
