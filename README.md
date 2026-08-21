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

绑定范例：`UnityProject/Assets/Scripts/UI/Example/ExampleWindow.cs`，对应 FGUI 包 `Example` / 组件 `ExampleWindow`。

设计分辨率：**1920×1080**（横版 PC / 16:9）。FGUI Adaptation、新建窗口插件、运行时 `SetContentScaleFactor` 都按这个值。

## 工作流

1. FairyGUI 打开 `FGUIProject/FGUIProject.fairy`，编辑窗口后发布到 `UnityProject/Assets/UI/`（也可在 Unity 菜单 **WorkCard → 发布FGUI**）
2. 手写 C# 类，用 `[UIClass]` / `[UICom]` / `[UIProp]` 声明包名、组件名和节点字段
3. Unity 菜单 **WorkCard → UI编辑器**：把 `[UIProp]` 绑到 FGUI 节点和点击回调。若类上的包名/组件名在 FGUI 工程里找不到，会显示红色按钮：`UI类（Xxx）关联FGUI组件（Yyy）不存在，请检查配置`
4. 点 **导出**，写出 `Assets/UI/UI/ui_config.json`
5. 打开 `Assets/Scenes/Main` 并 Play。`GameEntry` 会加载 UI 包、调用 `UIBinder.BindAll()`，再 `Show` `ExampleWindow`

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
