"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
/// <reference path="../ProjectChecker/editor.d.ts" />
const csharp_1 = require("csharp");
const App = csharp_1.FairyEditor.App;
/**
 * 新建包时自动生成的标准子目录。
 * 如需调整团队规范，直接改这里即可（顺序即创建顺序）。
 */
const SUB_FOLDERS = ["Buttons", "Images", "Items", "Windows", "Audio", "Spine"];
/** 新建窗口组件的默认尺寸（横版 PC 设计分辨率） */
const WINDOW_WIDTH = 1920;
const WINDOW_HEIGHT = 1080;
/**
 * 缩写白名单：命中的词保留全大写，避免严格帕斯卡把缩写规整坏。
 * 依据项目现有命名整理（如 ActivityBP / ActivityPVPDuck / TTMail / WXInvite
 * / HMSMini / KSMini / IDCard / QRCode）。需要可自行增删。
 */
const ABBREVIATIONS = new Set(["BP", "PVP", "TT", "WX", "HMS", "KS", "ID", "QR"]);
/**
 * 将任意输入规整为严格帕斯卡命名（PascalCase）。
 * - 按分隔符（空格 / 下划线 / 连字符 等）与驼峰边界切词
 * - 每个词首字母大写、其余字母一律小写
 * - 命中缩写白名单的词保留全大写
 *
 * 例：
 *   "hotsale"        -> "Hotsale"      （全小写无分隔也规整）
 *   "HOTSALE"        -> "Hotsale"
 *   "mini_feed_pot"  -> "MiniFeedPot"
 *   "miniFeedPot"    -> "MiniFeedPot"
 *   "activityBP"     -> "ActivityBP"   （缩写保留）
 *   "PVPDuck"        -> "PVPDuck"
 */
function toPascalCase(raw) {
    const s = (raw || "").trim();
    if (!s) {
        return "";
    }
    // 切词：连续大写(后接大写+小写的缩写) / 首字母大写词 / 全小写词 / 全大写词 / 数字
    const tokens = s.match(/[A-Z]+(?=[A-Z][a-z])|[A-Z]?[a-z]+|[A-Z]+|[0-9]+/g);
    if (!tokens) {
        return "";
    }
    let result = "";
    for (let i = 0, n = tokens.length; i < n; i++) {
        const token = tokens[i];
        const upper = token.toUpperCase();
        if (ABBREVIATIONS.has(upper)) {
            result += upper;
        }
        else {
            result += token.charAt(0).toUpperCase() + token.slice(1).toLowerCase();
        }
    }
    return result;
}
/**
 * 忽略大小写按名称查重，避免在不同系统下出现同名包
 */
function packageExists(name) {
    const project = App.project;
    if (project.GetPackageByName(name)) {
        return true;
    }
    const lower = name.toLowerCase();
    const all = project.allPackages;
    for (let i = 0, n = all.Count; i < n; i++) {
        if (all.get_Item(i).name.toLowerCase() === lower) {
            return true;
        }
    }
    return false;
}
/**
 * 创建包并按规范补齐标准子目录
 */
function createScaffoldPackage(rawName) {
    const name = toPascalCase(rawName);
    if (!name) {
        return;
    }
    if (!App.project || !App.project.opened) {
        App.Alert("请先打开一个工程再新建包");
        return;
    }
    if (packageExists(name)) {
        App.Alert(`包 "${name}" 已存在，请换一个名字`);
        return;
    }
    try {
        const pkg = App.project.CreatePackage(name);
        if (!pkg) {
            App.Alert(`创建包 "${name}" 失败`);
            return;
        }
        pkg.EnsureOpen();
        pkg.BeginBatch();
        for (const folder of SUB_FOLDERS) {
            pkg.CreateFolder(folder);
        }
        pkg.EndBatch();
        pkg.Save();
        App.project.Save();
        console.log(`PackageScaffold: 已创建包 ${name}，子目录 [${SUB_FOLDERS.join(", ")}]`);
        App.Alert(`已创建包 "${name}"\n子目录：${SUB_FOLDERS.join("、")}`);
    }
    catch (e) {
        console.error(`PackageScaffold: ${e}`);
        App.Alert(`创建包 "${name}" 失败：${e}`);
    }
}
/**
 * 弹出输入框，让用户填写包名
 */
function promptCreate() {
    if (!App.project || !App.project.opened) {
        App.Alert("请先打开一个工程再新建包");
        return;
    }
    App.Input("请输入新包名称", "", (name) => createScaffoldPackage(name));
}
/** 取文件夹 item 的内部路径，形如 "/Windows/"；根目录返回 "/" */
function folderInnerPath(folder) {
    if (folder.isRoot) {
        return "/";
    }
    let p = folder.path || "/";
    if (p.charAt(p.length - 1) !== "/") {
        p += "/";
    }
    return p + folder.name + "/";
}
/**
 * 根据库视图当前右键/选中上下文，解析要创建到哪个包的哪个路径。
 */
function getTargetLocation() {
    const lib = App.libView;
    const folder = lib.GetSelectedFolder();
    if (folder && folder.owner) {
        return { pkg: folder.owner, path: folderInnerPath(folder) };
    }
    const res = lib.GetSelectedResource();
    if (res && res.owner) {
        return { pkg: res.owner, path: res.path || "/" };
    }
    const active = App.GetActiveFolder();
    if (active && active.owner) {
        return { pkg: active.owner, path: folderInnerPath(active) };
    }
    return null;
}
/**
 * 在右键所在的包/文件夹下创建一个 1920x1080 的窗口组件并打开
 */
function createWindowComponent() {
    if (!App.project || !App.project.opened) {
        App.Alert("请先打开一个工程");
        return;
    }
    const loc = getTargetLocation();
    if (!loc) {
        App.Alert("请先在库中选中一个包或文件夹");
        return;
    }
    App.Input("请输入窗口名称", "NewWindow", (raw) => {
        const name = toPascalCase(raw);
        if (!name) {
            return;
        }
        try {
            const pkg = loc.pkg;
            pkg.EnsureOpen();
            const pi = pkg.CreateComponentItem(name, WINDOW_WIDTH, WINDOW_HEIGHT, loc.path, undefined, true, true);
            pkg.Save();
            App.project.Save();
            console.log(`PackageScaffold: 已创建窗口 ${name} (${WINDOW_WIDTH}x${WINDOW_HEIGHT}) @ ${pkg.name}${loc.path}`);
            App.docView.OpenDocument(pi.GetURL(), true);
        }
        catch (e) {
            console.error(`PackageScaffold: ${e}`);
            App.Alert(`创建窗口失败：${e}`);
        }
    });
}
const toolMenu = App.menu.GetSubMenu("tool");
toolMenu.AddItem("新建包(含标准子目录)\t⌘⇧N", "pkgscaffold_create", promptCreate);
App.pluginManager.SetHotkey("CTRL+SHIFT+N", () => {
    promptCreate();
});
// 库视图右键菜单：在资源 / 文件夹 / 包上右键，均可新建窗口
const WINDOW_MENU_CAPTION = `新建窗口(${WINDOW_WIDTH}x${WINDOW_HEIGHT})`;
const libView = App.libView;
libView.contextMenu.AddItem(WINDOW_MENU_CAPTION, "pkgscaffold_new_window", createWindowComponent);
if (libView.contextMenu_Folder) {
    libView.contextMenu_Folder.AddItem(WINDOW_MENU_CAPTION, "pkgscaffold_new_window_folder", createWindowComponent);
}
if (libView.contextMenu_Package) {
    libView.contextMenu_Package.AddItem(WINDOW_MENU_CAPTION, "pkgscaffold_new_window_pkg", createWindowComponent);
}
