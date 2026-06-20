# Composite Stone Processor — 开发指南

> **版本**: 1.0 (RimWorld 1.6)
> **仓库**: `D:\SteamLibrary\steamapps\common\RimWorld\Mods\Composite-Stone-Block`
> **源码**: `Source/CompositeStoneProcessor/`
> **构建**: `dotnet build "Source/CompositeStoneProcessor/CompositeStoneProcessor.csproj"`

---

## 1. 项目架构

### 文件树

```
Root/
├── About/About.xml
├── Assemblies/CompositeStoneProcessor.dll
├── Defs/
│   ├── MachinesUpdateRecipeDefs/B_CompositeUpgrades.xml
│   ├── RecipeDefs/B_CompositeStoneProcessorRecipes.xml
│   ├── ThingDefs/B_CompositeStoneProcessor.xml
│   ├── ResearchDefs/ResearchProjectDefs.xml
│   └── WorkGiverDefs/B_UpgradeHaul.xml
├── Languages/ChineseSimplified/Keyed/B_CompositeStoneProcessor.xml
├── Languages/English/Keyed/B_CompositeStoneProcessor.xml
├── Source/CompositeStoneProcessor/
│   ├── Building_CompositeStoneProcessor.cs         # 主建筑类 (~500 lines)
│   ├── ITab_Upgrades.cs                            # 升级界面标签 (~244 lines)
│   ├── MachineUpdateRecipeDef.cs                   # MachineUpdateRecipeExtension
│   ├── MachineRecipeExtension.cs                   # MachineRecipeExtension
│   ├── CompositeStoneProcessorMod.cs               # Mod Settings
│   ├── WorkGiver_UpgradeHaul.cs                    # 升级物资搬运 WorkGiver
│   ├── Alert_CompositeStoneProcessor.cs            # Alert
│   └── CompositeStoneProcessor.csproj
└── Textures/
```

### 继承链

```
Building_CompositeStoneProcessor
  extends Building_WorkTable
  implements IStoreSettingsParent   # 储存设置 + 搬运目标
  implements ISlotGroupParent       # HaulDestination
  implements IThingHolder           # ThingOwner<Thing>
```

---

## 2. 建筑主类 — Building_CompositeStoneProcessor

### 核心字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `innerContainer` | `ThingOwner<Thing>` | 内部不可见储存 (30 石块上限) |
| `installedUpgrades` | `List<RecipeDef>` | 已安装升级列表 |
| `pendingUpgradeQueue` | `List<RecipeDef>` | 等待安装的升级队列 |
| `pendingUpgradeResources` | `List<ThingDefCountClass>` | 当前等待收集的升级物资 |
| `upgradeProgressTicks` | `int` | 升级进度 (-1=等物资, 0+=计时中) |
| `progressTicks` | `int` | 当前加工累计 tick |
| `currentRecipe` | `RecipeDef` | 正在加工的配方 |
| `isProcessing` | `bool` | 是否正在加工 |

### Tick() 主循环

```
1. 升级队列处理 (IsHashIntervalTick(ti))
   - pendingUpgradeQueue[0] 资源收集完 -> 开始升级计时
   - upgradeProgressTicks >= workAmount -> CompleteUpgrade()

2. 能源检查
   - 无电+无燃料 -> SetPowerConsumption(0) + CancelProcessing()

3. 空闲找清单 (IsHashIntervalTick(ti) 降频)
   - FindBill() + HasIngredient() -> 启动加工

4. 加工推进 (IsHashIntervalTick(ti))
   - 温度检查 GetTempFactor()
   - 燃料消耗 / 电力切换
   - progressTicks += ti * TotalSpeed * tempFactor
   - >= workAmount -> CompleteProcessing()
```

### FindBill() - 清单过滤含技能等级

```
private Bill_Production FindBill()
{
    for each bill, if bill.ShouldDoNow():
        int req = bill.recipe.skillRequirements 有 ? minLevel : settings.defaultSkillLevel
        if EffectiveSkill >= req -> return bill
}
```

### Notify_ReceivedThing - 物品吸入

```
Thing 落到建筑格子时触发:
  石块(StoneChunks) -> AbsorbChunk() -> 吸入 innerContainer
  升级物资 -> TryAcceptUpgradeResource() -> 扣除并销毁
```

### DeSpawn() - 拆除清理

```
base.DeSpawn(mode)  // 先让基类清理 HaulDestination
slotGroup = null    // 之后才释放 slotGroup (顺序很重要!)
```

---

## 3. Def 扩展系统

### MachineRecipeExtension - 加工配方消耗

```xml
<li Class="CompositeStoneProcessor.MachineRecipeExtension">
  <fuelConsumptionRate>0.5</fuelConsumptionRate>  <!-- 每60tick消耗燃料 -->
  <powerConsume>200</powerConsume>                 <!-- 加工时额外W -->
</li>
```

无此扩展 -> 使用 Mod 设置 defaultFuelRate / defaultPowerConsume。

### MachineUpdateRecipeExtension - 升级模块

```xml
<li Class="CompositeStoneProcessor.MachineUpdateRecipeExtension">
  <speedUp>0.35</speedUp>       <!-- float, 加工速度增量 -->
  <sortOrder>10</sortOrder>     <!-- int, 排序(升序) -->
  <skillLevel>10</skillLevel>   <!-- int, 等效手工技能 -->
  <unlockRecipe>                <!-- list, 安装后自动解锁的清单 -->
    <li>SomeRecipeDef</li>
  </unlockRecipe>
  <componentsPrerequisites>     <!-- list, 必须已安装的前置升级 -->
    <li>PrereqDefName</li>
  </componentsPrerequisites>
</li>
```

---

## 4. 升级系统流程

```
点击安装 -> RequestUpgradeInstall(def)
  -> 验证(未安装+研究解锁+前置已装+不在队列)
  -> pendingUpgradeQueue.Add(def)
  -> 若为首个 -> 初始化 pendingUpgradeResources

殖民者搬运 -> Notify_ReceivedThing -> TryAcceptUpgradeResource
  -> 扣除并销毁 -> 超出需求时 GenPlace.TryPlaceThing 弹出

物资收集完 -> upgradeProgressTicks = 0 -> 计时 (ti * 0.5 each tick)

计时完成 -> CompleteUpgrade()
  -> installedUpgrades.Add(def)
  -> 添加 unlockRecipe 清单
  -> 出队 -> 下一个初始化
```

队列 FIFO。升级 RecipeDef 不应包含 <recipeUsers>。

---

## 5. 燃料/电力系统

### 切换逻辑

| 状态 | 电力 | 燃料 | PowerOutput |
|------|------|------|-------------|
| 待机(有电网) | 50W | 无 | -50 |
| 待机(无电网) | 0 | 无 | 0 |
| 加工(有电) | 50+recipe.power | 无 | -(50+power) |
| 加工(无电有燃料) | 0 | recipe.fuelRate | -1 |

有燃料时 DrawGUIOverlay() 跳过无电力图标。

---

## 6. 温度与产热

### 效率曲线

- -10C ~ 80C: 1.0x
- -10C -> -30C: 1.0 -> 0 (线性)
- 80C -> 120C: 1.0 -> 0 (线性)
- < -30C 或 > 120C: 0 (停止)

CompHeatPusherPowered 自动产热 (heatPerSecond=3)。

---

## 7. 升级界面 ITab

- 全动态高度: 34f + costH + 4f + speedH + 4f + thirdH + 4f
- 排序: sortOrder 升序
- 颜色: Locked=灰/Ready=白/Installing=橙/Installed=绿
- 锁定时框透明度 0.6

---

## 8. WorkGiver - 升级物资搬运

优先级 8 (原版搬运 15 之上)。HaulToCell Job。

---

## 9. Alert

扫描 allBuildingsColonist (非 ThingsInGroup)。缺原料或缺能源时触发。

---

## 10. Mod 设置

| 设置 | 默认 | 范围 |
|------|------|------|
| alertEnabled | true | - |
| tickInterval | 120 | 60~1000 |
| bgColorHex | "282828" | 6 chars |
| defaultFuelRate | 0.5 | 0.1~3.0 |
| defaultPowerConsume | 200 | 50~500 |
| defaultSkillLevel | 6 | 1~20 |

---

## 11. 常见问题与陷阱

1. **slotGroup空** -> base.DeSpawn 之后才设 slotGroup=null
2. **配方重复** -> 升级 RecipeDef 不应包含 recipeUsers
3. **researchPrerequisite/s 混用** -> 单个用单数，多个用复数
4. **燃料不消耗** -> consumeFuelOnlyWhenUsed=true 需手动 ConsumeFuel()
5. **无电力图标** -> 有燃料时 DrawGUIOverlay return
6. **效率恒1** -> speed = TotalSpeed * GetTempFactor()
7. **加工索引偏移** -> 建临时副本再遍历删除
8. **升级进度丢失** -> ExposeData 中序列化 upgradeProgressTicks

---

## 12. 性能说明

- 空闲降频: FindBill() 每 ProcTickInterval tick 一次
- Alert 扫描: allBuildingsColonist 替代 ThingsInGroup(BuildingArtificial)
- 无 Harmony 补丁
- 兼容: Butter++ / Performance Optimizer / Missile Girl / BWM / VFE
- 空闲 ~0.3us/tick, 加工 ~2us/120tick

---

## 13. 兼容性确认

- Butter++: 无冲突 (无 DoSingleTick 转译)
- Pick Up And Haul: 使用标准 HaulToCell Job
- Better Workbench Management: 通过 Patch 集成
- Vanilla Expanded Framework: 通过 Patch 兼容
