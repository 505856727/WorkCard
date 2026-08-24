# WorkCard

Unity + FairyGUI 的 C# 项目。UI 源工程在 `FGUIProject/`，运行时工程在 `UnityProject/`。

## 目录

| 路径 | 说明 |
| --- | --- |
| `FGUIProject/` | FairyGUI 源工程 |
| `FGUIProject/assets/Example/` | 示例窗口（`ExampleWindow`） |
| `UnityProject/` | Unity 2022.3.62f3 工程 |
| `UnityProject/Assets/Scenes/Main.unity` | 进入 Play 后打开 ExampleWindow |
| `UnityProject/Assets/UI/` | FairyGUI 发布产物（`*_fui.bytes` / 图集） |
| `UnityProject/Assets/UI/UI/ui_config.json` | UI 编辑器导出的绑定配置 |
| `UnityProject/UIEditor/` | 每个组件的节点绑定配置 |
| `UnityProject/Assets/Scripts/UI/` | C# 窗口/组件（`[UIClass]` / `[UICom]` / `[UIProp]`） |
| `UnityProject/Assets/Editor/UIEditor/` | FGUI 导出与绑定编辑器 |
| `Tables/` | 配置表源文件（csv / xlsx，sheet 名以 `Config` 结尾） |
| `UnityProject/Assets/Resources/Config/` | 配置表导出的 `.bytes` |
| `UnityProject/Assets/Scripts/Config/` | 配置读取（`[Config]` / `ConfigManager`） |
| `UnityProject/Assets/Editor/ConfigEditor/` | 配置表本地导出编辑器 |

绑定范例：`UnityProject/Assets/Scripts/UI/Example/ExampleWindow.cs`，对应 FGUI 包 `Example` / 组件 `ExampleWindow`。

设计分辨率：**1920×1080**（横版 PC / 16:9）。FGUI Adaptation、新建窗口插件、运行时 `SetContentScaleFactor` 都按这个值。

## 工作流

1. FairyGUI 打开 `FGUIProject/FGUIProject.fairy`，编辑窗口后发布到 `UnityProject/Assets/UI/`（也可在 Unity 菜单 **WorkCard → 发布FGUI**）
2. 手写 C# 类，用 `[UIClass]` / `[UICom]` / `[UIProp]` 声明包名、组件名和节点字段
3. Unity 菜单 **WorkCard → UI编辑器**：把 `[UIProp]` 绑到 FGUI 节点和点击回调。若类上的包名/组件名在 FGUI 工程里找不到，会显示红色按钮：`UI类（Xxx）关联FGUI组件（Yyy）不存在，请检查配置`
4. 点 **导出**，写出 `Assets/UI/UI/ui_config.json`
5. Unity 菜单 **WorkCard → Addressables → 构建 UI Addressables**：同步 `Assets/UI/` 到 `WorkCard UI` 分组并构建本地内容
6. 打开 `Assets/Scenes/Main` 并 Play。`GameEntry` 会通过 Addressables 加载 UI 包和绑定配置、调用 `UIBinder.BindAll()`，再 `Show` `ExampleWindow`

## C# 绑定（对应 TS 装饰器）

```csharp
[UIClass(WindowGroup.Pop, "Example", "ExampleWindow")]
public class ExampleWindow : WindowBase
{
    [UIProp]
    public GButton btnOk;

    private void _OnBtnOk() {}
}

[UICom("Example", "ItemCard")]
public class ItemCard : UIComponent
{
    [UIProp]
    public GTextField txtName;
}
```

`@uiclass` → `[UIClass]`，`@uicom` → `[UICom]`，`@uiprop` → `[UIProp]`。节点查找走 `ui_config.json`，不要手写 `GetChild`。

发布路径：`FGUIProject/settings/Publish.json` → `../UnityProject/Assets/UI/{publish_file_name}`

## 配置表

对齐 MergeSeasons 的本地表格导出/读取（原工具是 TS/Python，这里用 C#）。

源表在 `Tables/`，sheet 名或 csv 文件名以 `Config` 结尾。前三行是表头：描述、类型、字段名（名字在类型下面，与 MergeSeasons 一致），从第四行开始是数据。

```
编号,名称,描述,图片
int,string,string,string
id,name,desc,image
1,加班通知,今天需要加班完成季度汇报,event_overtime
```

1. 在 `Tables/` 里改 csv / xlsx（Excel 可直接打开 csv）
2. Unity 菜单 **WorkCard → 配置编辑器**（或 **WorkCard → 导出配置**）导出
3. 二进制写到 `UnityProject/Assets/Resources/Config/{Name}.bytes`，可选再出一份 csv 到 `UnityProject/ConfigEditor/csv/`
4. 手写 `[Config("EventCardConfig")]` 的 item 类，字段名与表头一致（会转成 camelCase）
5. 运行时 `ConfigManager.LoadAll()`，再用对应容器读取

`@config` 挂在容器类上；这里把 `[Config]` 写在 item 类上，由 `ConfigManager` 按 `ConfigKind` 创建容器：

| Kind | 容器 | 读取 |
| --- | --- | --- |
| `Map` | `ConfigMap` | `GetMap<T>().GetItem(id)` |
| `List` | `ConfigList` | `GetList<T>().GetItem(index)` |
| `Group` | `ConfigGroup` | `GetGroup<T>().GetGroup(groupId)` |
| `MapList` | `ConfigMapList` | `GetItemById` / `GetItemByIndex` |
| `GroupList` | `ConfigGroupList` | 分组 + 列表索引 |
| `GroupMap` | `ConfigGroupMap` | 分组 + `GetItem(id)` |

```csharp
[Config("EventCardConfig", ConfigKind.Map)]
public class EventCardConfigItem : IConfigItem
{
    public int id;
    public void OnLoad() {}
}

[Config("DropConfig", ConfigKind.Group, GroupKey = "group")]
public class DropConfigItem : IConfigItem
{
    public int id;
    public int group;
    public void OnLoad() {}
}
```

配置编辑器的类型下拉、分组key、「索引从1开始」只用于 **复制声明**：按原工具逻辑生成 item + 容器类到剪贴板。`.bytes` 不区分容器类型。读取时看 `[Config]` 所在类：容器类会直接实例化，item 类则由 `ConfigManager` 按 Kind 包一层。

二进制格式与原工具 version 3 一致（大端，可含 string table）。暂不支持 object / expression / PropsConfig。

范例：`Tables/EventCardConfig.csv` → `Game.EventCardConfigItem`。`GameEntry` 启动时会加载并打印条数。
