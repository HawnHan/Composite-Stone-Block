# Composite Stone Processor — MOD 拓展编写指南

## 目录
1. [文件结构](#1-文件结构)
2. [核心建筑 —— ThingDef](#2-核心建筑--thingdef)
3. [加工配方 —— RecipeDef](#3-加工配方--recipedef)
4. [升级模块 —— MachineUpdateRecipeExtension](#4-升级模块--machineupdaterecipeextension)
5. [配方消耗 —— MachineRecipeExtension](#5-配方消耗--machinerecipeextension)
6. [研究项目 —— ResearchProjectDef](#6-研究项目--researchprojectdef)
7. [升级物资搬运 —— WorkGiverDef](#7-升级物资搬运--workgiverdef)
8. [建设产热 —— CompProperties_HeatPusher](#8-建筑产热--compproperties_heatpusher)
9. [翻译文件](#9-翻译文件)
10. [Mod 设置](#10-mod-设置)
11. [完整示例：添加一个升级模块](#11-完整示例添加一个升级模块)
12. [完整示例：添加一个加工配方](#12-完整示例添加一个加工配方)

---

## 1. 文件结构

```
Composite-Stone-Block/
├── About/
│   └── About.xml                    # MOD 元数据
├── Assemblies/
│   └── CompositeStoneProcessor.dll  # 编译后的 DLL
├── Defs/
│   ├── MachinesUpdateRecipeDefs/
│   │   └── B_CompositeUpgrades.xml  # 升级模块定义
│   ├── RecipeDefs/
│   │   ├── B_CompositeStoneProcessorRecipes.xml  # 加工配方
│   │   └── B_Stonecutting.xml      # 原 MOD 配方（不修改）
│   ├── ResearchDefs/
│   │   ├── ResearchProjectDefs.xml  # 研究项目
│   │   └── ResearchTabDefs.xml      # 研究标签
│   ├── TerrainDefs/
│   │   └── GeneralStoneTile.xml     # 复合石材地板（不修改）
│   ├── ThingDefs/
│   │   ├── B_CompositeStoneProcessor.xml  # 建筑定义
│   │   └── B_CurrencyStone.xml     # 复合石材物品（不修改）
│   └── WorkGiverDefs/
│       └── B_UpgradeHaul.xml        # 升级物资搬运
├── Languages/
│   ├── ChineseSimplified/Keyed/
│   │   └── B_CompositeStoneProcessor.xml  # 中文翻译
│   └── English/Keyed/
│       └── B_CompositeStoneProcessor.xml  # 英文翻译
├── Source/
│   └── CompositeStoneProcessor/     # C# 源码
│       ├── Properties/
│       │   └── AssemblyInfo.cs
│       ├── Alert_CompositeStoneProcessor.cs
│       ├── Building_CompositeStoneProcessor.cs  # 主建筑逻辑
│       ├── CompositeStoneProcessorMod.cs        # Mod 设置
│       ├── ITab_Upgrades.cs                     # 升级界面
│       ├── MachineRecipeExtension.cs             # 配方消耗扩展
│       ├── MachineUpdateRecipeDef.cs             # 升级模块扩展
│       └── WorkGiver_UpgradeHaul.cs             # 物资搬运
└── Textures/
    └── Things/Building/
        ├── CompositeStoneProcessor.png     # 建筑纹理
        ├── CompositeStoneUpgradeMK1.png     # 升级图标
        ├── CompositeStoneUpgradeMK2.png
        └── CompositeStoneUpgradeMK3.png
```

---

## 2. 核心建筑 —— ThingDef

**文件**：`Defs/ThingDefs/B_CompositeStoneProcessor.xml`

### 关键属性

| 节点 | 值 | 说明 |
|------|-----|------|
| `ParentName` | `BuildingBase` | 继承基础建筑属性 |
| `thingClass` | `CompositeStoneProcessor.Building_CompositeStoneProcessor` | C# 主类 |
| `tickerType` | `Normal` | 每 tick 调用 |
| `size` | `(1,1)` | 1×1 |
| `hasInteractionCell` | `true` | 产物掉落点 |
| `interactionCellOffset` | `(0,0,-1)` | 掉落点偏移 |
| `rotatable` | `true` | 可旋转 |
| `designationCategory` | `Production` | 生产栏 |
| `containedItemsSelectable` | `true` | 内部物品可选 |

### comps 组件

```xml
<comps>
  <!-- 1. 燃料系统 -->
  <li Class="CompProperties_Refuelable">
    <fuelCapacity>75</fuelCapacity>
    <targetFuelLevelConfigurable>true</targetFuelLevelConfigurable>
    <fuelFilter>
      <thingDefs>
        <li>Chemfuel</li>
        <li>WoodLog</li>
      </thingDefs>
    </fuelFilter>
    <consumeFuelOnlyWhenUsed>true</consumeFuelOnlyWhenUsed>
    <showAllowAutoRefuelToggle>true</showAllowAutoRefuelToggle>
    <autoRefuelPercent>0.2</autoRefuelPercent>
    <showFuelGizmo>true</showFuelGizmo>
    <drawOutOfFuelOverlay>false</drawOutOfFuelOverlay>
  </li>

  <!-- 2. 电力系统 -->
  <li Class="CompProperties_Power">
    <compClass>CompPowerTrader</compClass>
    <basePowerConsumption>50</basePowerConsumption>
  </li>

  <!-- 3. 产热系统（原版 CompHeatPusherPowered） -->
  <li Class="CompProperties_HeatPusher">
    <compClass>CompHeatPusherPowered</compClass>
    <heatPerSecond>3</heatPerSecond>
  </li>
</comps>
```

### inspectorTabs

```xml
<inspectorTabs>
  <li>ITab_Bills</li>                            <!-- 清单界面 -->
  <li>ITab_Storage</li>                          <!-- 储存设置 -->
  <li>CompositeStoneProcessor.ITab_Upgrades</li>  <!-- 升级界面 -->
</inspectorTabs>
```

---

## 3. 加工配方 —— RecipeDef

**文件**：`Defs/RecipeDefs/B_CompositeStoneProcessorRecipes.xml`

### 模板

```xml
<RecipeDef ParentName="MakeCompositeStoneAutoBase">
    <defName>YourRecipeDefName</defName>
    <label>Your recipe label</label>
    <workAmount>1600</workAmount>                    <!-- 所需工作量（tick） -->
    <ingredients>
      <li>
        <filter>
          <categories>
            <li>StoneChunks</li>                    <!-- 接受所有石块类型 -->
          </categories>
        </filter>
        <count>1</count>                            <!-- 消耗数量 -->
      </li>
    </ingredients>
    <products>
      <BlockCompositeStone>20</BlockCompositeStone>  <!-- 产出复合石材数量 -->
    </products>
    <skillRequirements>                              <!-- 可选：技能要求 -->
      <Crafting>6</Crafting>
    </skillRequirements>
    <researchPrerequisite>Fastcutting</researchPrerequisite>  <!-- 可选：研究前置 -->
    <modExtensions>                                  <!-- 可选：燃料/电力消耗 -->
      <li Class="CompositeStoneProcessor.MachineRecipeExtension">
        <fuelConsumptionRate>0.5</fuelConsumptionRate>
        <powerConsume>200</powerConsume>
      </li>
    </modExtensions>
</RecipeDef>
```

### 现有配方参考

| 配方 defName | 工作量 | 消耗 | 产出 | 技能 | 研究 |
|-------------|--------|------|------|------|------|
| `MakeCompositeStoneAuto` | 1600 | 1 石块 | 20 | — | — |
| `MakeCompositeStoneAutoS` | 1200 | 1 石块 | 15 | Craft 4 | Fastcutting |
| `MakeCompositeStoneAutoD` | 2000 | 1 石块 | 25 | Craft 4 | Delicatecutting |
| `MakeCompositeStoneAutoB` | 4320 | 3 石块 | 45 | Craft 6 | Fastcutting + Delicatecutting |
| `MakeCompositeStoneAutoFB` | 3240 | 3 石块 | 45 | Craft 8 | FastcuttingB |
| `MakeCompositeStoneAutoDB` | 5400 | 3 石块 | 75 | Craft 8 | DelicatecuttingB |
| `MakeCompositeStoneAutoM` | 3240 | 3 石块 | 75 | Craft 10 | Mastercutting |

> **注意**：配方必须通过 `<recipeUsers><li>CompositeStoneProcessor</li></recipeUsers>` 关联到加工站（已在抽象基类 `MakeCompositeStoneAutoBase` 中定义）。

---

## 4. 升级模块 —— MachineUpdateRecipeExtension

**文件**：`Defs/MachinesUpdateRecipeDefs/B_CompositeUpgrades.xml`

### C# 定义

```csharp
public class MachineUpdateRecipeExtension : DefModExtension
{
    public float speedUp;         // 速度增量
    public int sortOrder = 999;   // 排序序号
    public int skillLevel;        // 等效技能（显示用）
    public List<RecipeDef> unlockRecipe;  // 解锁的清单
}
```

### XML 模板

```xml
<RecipeDef>
    <defName>YourUpgradeDefName</defName>
    <label>Your Upgrade Label</label>
    <description>Description of your upgrade module.</description>
    <workAmount>2000</workAmount>                     <!-- 安装所需 tick -->
    <researchPrerequisite>CompositeStoneUpgradeMK1</researchPrerequisite>  <!-- 研究前置 -->
    <ingredients>
      <li>
        <filter>
          <thingDefs>
            <li>Steel</li>
          </thingDefs>
        </filter>
        <count>50</count>                            <!-- 所需钢材 -->
      </li>
      <li>
        <filter>
          <thingDefs>
            <li>ComponentIndustrial</li>
          </thingDefs>
        </filter>
        <count>3</count>                             <!-- 所需零件 -->
      </li>
    </ingredients>
    <modExtensions>
      <li Class="CompositeStoneProcessor.MachineUpdateRecipeExtension">
        <speedUp>0.35</speedUp>         <!-- float, 速度增量 -->
        <sortOrder>10</sortOrder>       <!-- int, 排序 -->
        <skillLevel>10</skillLevel>     <!-- int, 等效技能 -->
        <unlockRecipe>                   <!-- 可选：解锁的清单 -->
          <li>SomeRecipeDefName</li>
        </unlockRecipe>
      </li>
    </modExtensions>
</RecipeDef>
```

### 字段说明

| 字段 | 类型 | 必需 | 默认值 | 说明 |
|------|------|------|--------|------|
| `speedUp` | float | 否 | 0 | 速度增量，累加到总速度系数。例：0.35 → 速度 ×1.35 |
| `sortOrder` | int | 否 | 999 | 升级界面排序序号，升序排列 |
| `skillLevel` | int | 否 | 0 | 等效手工技能，仅用于显示 |
| `unlockRecipe` | list | 否 | — | 安装后自动作为清单添加的 RecipeDef 列表 |

> **注意**：
> - 升级 RecipeDef 不应包含 `<recipeUsers>`，否则会出现在工作台清单中
> - 不包含 `<jobString>`，不参与工作台工作流程
> - 物资通过 `WorkGiver_UpgradeHaul` 由殖民者搬运

---

## 5. 配方消耗 —— MachineRecipeExtension

**文件**：`Defs/RecipeDefs/B_CompositeStoneProcessorRecipes.xml`（附加在每个配方上）

### C# 定义

```csharp
public class MachineRecipeExtension : DefModExtension
{
    public float fuelConsumptionRate;  // 每 60 tick 消耗燃料量
    public int powerConsume;           // 加工时额外功率（W）
}
```

### XML 模板

```xml
<modExtensions>
  <li Class="CompositeStoneProcessor.MachineRecipeExtension">
    <fuelConsumptionRate>0.5</fuelConsumptionRate>  <!-- 每 60 tick 消耗 -->
    <powerConsume>200</powerConsume>                 <!-- 加工功率（W） -->
  </li>
</modExtensions>
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `fuelConsumptionRate` | float | 是 | 每 60 tick 消耗的燃料单位数。必须 > 0 |
| `powerConsume` | int | 是 | 加工时的额外耗电量（W），叠加在待机 50W 之上 |

> 不含此扩展的配方使用 Mod 设置中的默认值（`defaultFuelRate` / `defaultPowerConsume`）。

---

## 6. 研究项目 —— ResearchProjectDef

**文件**：`Defs/ResearchDefs/ResearchProjectDefs.xml`

### 模板

```xml
<ResearchProjectDef>
    <defName>YourResearchDefName</defName>
    <label>Your Research Label</label>
    <techLevel>Industrial</techLevel>
    <baseCost>1200</baseCost>
    <description>Description of your research.</description>
    <prerequisites>
      <li>RequiredResearchDefName</li>              <!-- 前置研究 -->
    </prerequisites>
    <tab>CompositeTechnology</tab>                  <!-- 研究标签 -->
    <researchViewX>1</researchViewX>                <!-- X 坐标 -->
    <researchViewY>6</researchViewY>                <!-- Y 坐标 -->
</ResearchProjectDef>
```

### 现有研究参考

| defName | 成本 | 前置 | 坐标 |
|---------|------|------|------|
| `CompositeStoneProcessing` | 700 | Stonecutting | (0,6) |
| `CompositeStoneUpgradeMK1` | 1200 | CompositeStoneProcessing | (1,6) |
| `CompositeStoneUpgradeMK2` | 2200 | CompositeStoneUpgradeMK1 | (2,6) |
| `CompositeStoneUpgradeMK3` | 3500 | CompositeStoneUpgradeMK2 | (3,6) |

---

## 7. 升级物资搬运 —— WorkGiverDef

**文件**：`Defs/WorkGiverDefs/B_UpgradeHaul.xml`

```xml
<WorkGiverDef>
    <defName>DeliverResourcesToProcessor</defName>
    <label>deliver resources to processor upgrade</label>
    <giverClass>CompositeStoneProcessor.WorkGiver_UpgradeHaul</giverClass>
    <workType>Hauling</workType>
    <priorityInType>8</priorityInType>
    <verb>deliver to</verb>
    <gerund>delivering to</gerund>
    <requiredCapacities>
      <li>Manipulation</li>
    </requiredCapacities>
</WorkGiverDef>
```

> **工作原理**：当加工站有升级请求时，此 WorkGiver 扫描地图上所需的 Steel/Component，生成 `HaulToCell` 任务。殖民者将物资搬运到加工站后，`Notify_ReceivedThing` 吸入并计数。无需玩家干预。

---

## 8. 建筑产热 —— CompProperties_HeatPusher

```xml
<li Class="CompProperties_HeatPusher">
    <compClass>CompHeatPusherPowered</compClass>   <!-- 使用原版有功耗热器 -->
    <heatPerSecond>3</heatPerSecond>               <!-- 每秒产热量 -->
</li>
```

> 使用原版 `CompHeatPusherPowered`，仅在建筑有功率输出时产热（加工时或待机时有电网连接时）。燃料模式下加工也产热（通过内部调整 `PowerOutput`）。

---

## 9. 翻译文件

### 中文翻译

**文件**：`Languages/ChineseSimplified/Keyed/B_CompositeStoneProcessor.xml`

### 英文翻译

**文件**：`Languages/English/Keyed/B_CompositeStoneProcessor.xml`

格式：
```xml
<LanguageData>
  <YourKey>Your translation text</YourKey>
</LanguageData>
```

所有显示给玩家的字符串均需添加翻译键。当前所有 45 个键已完整覆盖中英文。

---

## 10. Mod 设置

**文件**：`Source/CompositeStoneProcessor/CompositeStoneProcessorMod.cs`

在游戏主菜单 → 选项 → Mod 选项 → Composite Stone Processor 中配置：

| 设置 | 类型 | 默认 | 范围 |
|------|------|------|------|
| `alertEnabled` | bool | true | 资源不足提醒 |
| `tickInterval` | int | 120 | 60~1000，步进 10 |
| `bgColorHex` | string | "4B4B4B" | 十六进制色码 |
| `defaultFuelRate` | float | 0.5 | 0.1~3.0，步进 0.1 |
| `defaultPowerConsume` | int | 200 | 50~500，步进 10 |

---

## 11. 完整示例：添加一个升级模块

以下展示添加一个名为 "MK4 Speed Module" 的新升级，需要研究 `CompositeStoneUpgradeMK3`，消耗 150 Steel + 15 Component，速度 +0.60。

### 步骤 1：添加研究

在 `Defs/ResearchDefs/ResearchProjectDefs.xml` 中添加：

```xml
<ResearchProjectDef>
    <defName>CompositeStoneUpgradeMK4</defName>
    <label>Processor upgrade MK4</label>
    <techLevel>Industrial</techLevel>
    <baseCost>5000</baseCost>
    <description>Ultimate processor upgrade. Equivalent to Crafting skill 25.</description>
    <prerequisites>
      <li>CompositeStoneUpgradeMK3</li>
    </prerequisites>
    <tab>CompositeTechnology</tab>
    <researchViewX>4</researchViewX>
    <researchViewY>6</researchViewY>
</ResearchProjectDef>
```

### 步骤 2：添加升级模块

在 `Defs/MachinesUpdateRecipeDefs/B_CompositeUpgrades.xml` 中添加：

```xml
<RecipeDef>
    <defName>CSP_UpgradeMK4</defName>
    <label>MK4 Speed Module</label>
    <description>Ultimate speed enhancement module. Requires MK1+MK2+MK3. Equivalent to Crafting skill 25.</description>
    <workAmount>2000</workAmount>
    <researchPrerequisite>CompositeStoneUpgradeMK4</researchPrerequisite>
    <ingredients>
      <li>
        <filter><thingDefs><li>Steel</li></thingDefs></filter>
        <count>150</count>
      </li>
      <li>
        <filter><thingDefs><li>ComponentIndustrial</li></thingDefs></filter>
        <count>15</count>
      </li>
    </ingredients>
    <modExtensions>
      <li Class="CompositeStoneProcessor.MachineUpdateRecipeExtension">
        <speedUp>0.60</speedUp>
        <sortOrder>40</sortOrder>
        <skillLevel>25</skillLevel>
      </li>
    </modExtensions>
</RecipeDef>
```

### 步骤 3（可选）：添加翻译

在翻译文件中添加：
```xml
<CompositeStoneUpgradeMK4.label>加工站升级MK4</CompositeStoneUpgradeMK4.label>
<CompositeStoneUpgradeMK4.description>加工站的最终极致升级，处理速度提升至等效手工技能25级。</CompositeStoneUpgradeMK4.description>
```

升级界面中的名称和技能等级由 `<label>` 和 `skillLevel` 字段自动生成，无需额外翻译。

### 无需修改

- C# 代码 → 无需修改，`DefDatabase<RecipeDef>` 自动读取
- ITab_Upgrades → 自动显示，`sortOrder` 决定排序
- WorkGiver → 自动读取材料清单
- 安装逻辑 → 自动处理

---

## 12. 完整示例：添加一个加工配方

以下展示添加一个名为 "批量快速加工复合石材（效率型）" 的新配方，消耗 2 石块产出 50 复合石材。

### 步骤 1：添加配方

在 `Defs/RecipeDefs/B_CompositeStoneProcessorRecipes.xml` 中添加：

```xml
<RecipeDef ParentName="MakeCompositeStoneAutoBase">
    <defName>MakeCompositeStoneAutoEF</defName>
    <label>Composite stone processing (Efficiency)</label>
    <workAmount>2800</workAmount>
    <ingredients>
      <li>
        <filter><categories><li>StoneChunks</li></categories></filter>
        <count>2</count>
      </li>
    </ingredients>
    <products><BlockCompositeStone>50</BlockCompositeStone></products>
    <skillRequirements><Crafting>8</Crafting></skillRequirements>
    <researchPrerequisite>FastcuttingB</researchPrerequisite>
    <modExtensions>
      <li Class="CompositeStoneProcessor.MachineRecipeExtension">
        <fuelConsumptionRate>0.9</fuelConsumptionRate>
        <powerConsume>300</powerConsume>
      </li>
    </modExtensions>
</RecipeDef>
```

### 步骤 2：添加翻译

```xml
<MakeCompositeStoneAutoEF.label>加工复合石材（效率型）</MakeCompositeStoneAutoEF.label>
<MakeCompositeStoneAutoEF.description>将2块大块石头加工成50份复合石材，效率提升。</MakeCompositeStoneAutoEF.description>
```

### 无需修改

- C# 代码 → 无需修改
- `recipeUsers` → 已在抽象基类中定义
- 技能检查 → 自动对比 `EffectiveSkill` 和配方要求
- 燃料/电力消耗 → 自动读取 `MachineRecipeExtension`

---

## 附录：速度计算公式

```
总速度系数 = 1.0 + 所有已安装升级的 speedUp 之和
每次加工推进 = tickInterval × 总速度系数 × 温度效率
```

## 附录：燃料/电力逻辑

| 状态 | 电力消耗 | 燃料消耗 |
|------|---------|---------|
| 待机 + 有电网 | 50W | 无 |
| 待机 + 仅燃料 | 0 | 无 |
| 加工 + 有电网 | 50 + 配方 powerConsume | 无 |
| 加工 + 仅燃料 | 虚拟负载（触发产热） | 配方 fuelConsumptionRate / 60tick |
| 无电无燃料 | 0 | 无 |

## 附录：温度效率曲线

| 温度范围 | 效率 |
|---------|------|
| -10°C ~ 80°C | 100% |
| -30°C ~ -10°C | 100% → 0%（线性下降） |
| 80°C ~ 120°C | 100% → 0%（线性下降） |
| < -30°C 或 > 120°C | 0%（停止工作） |
