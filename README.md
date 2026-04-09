# Aseprite2Unity-Ext

> **版本 1.2.0** &nbsp;|&nbsp; **Unity 2020.3+** &nbsp;|&nbsp; **MIT License**

[![GitHub](https://img.shields.io/badge/GitHub-IsakWong%2FAseprite2Unity--Ext-blue?logo=github)](https://github.com/IsakWong/Aseprite2Unity-Ext)

基于 [Aseprite2Unity](https://github.com/Seanba/Aseprite2Unity)（Sean Barton）的扩展版本。在保留原有功能的基础上，新增了 **合图（Sprite Atlas）支持**、**可扩展的导入处理管线**、**全局配置与逐资源覆盖机制** 以及 **Welcome Window / Project Settings 面板** 等功能。

---

## 目录

- [安装](#安装)
- [功能概览](#功能概览)
  - [合图支持（Sprite Atlas）](#1-合图支持sprite-atlas)
  - [可扩展的导入处理管线（Processor Pipeline）](#2-可扩展的导入处理管线processor-pipeline)
  - [全局配置与逐资源覆盖（Global Config + Override）](#3-全局配置与逐资源覆盖global-config--override)
  - [Welcome Window 与 Project Settings 面板](#4-welcome-window-与-project-settings-面板)
  - [动画功能](#5-动画功能)
- [快速开始](#快速开始)
- [自定义 Processor 示例](#自定义-processor-示例)
  - [基础 Processor](#基础-processor)
  - [带逐资源配置的 Processor](#带逐资源配置的-processor)
  - [Visitor 模式（遍历 Chunk 数据）](#visitor-模式遍历-chunk-数据)
- [导入管线流程](#导入管线流程)
- [核心 API 参考](#核心-api-参考)
- [作者](#作者)
- [许可证](#许可证)
- [致谢](#致谢)

---

## 安装

### 方式一：Git URL（推荐）

在项目的 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.isakwong.aseprite2unity-ext": "https://github.com/IsakWong/Aseprite2Unity-Ext.git?path=/Aseprite2Unity/Packages/com.isakwong.aseprite2unity"
  }
}
```

如果需要锁定版本，可以在 URL 末尾追加版本标签：

```
"com.isakwong.aseprite2unity-ext": "https://github.com/IsakWong/Aseprite2Unity-Ext.git?path=/Aseprite2Unity/Packages/com.isakwong.aseprite2unity#v1.2.0"
```

### 方式二：本地文件引用（适合开发调试）

将本仓库克隆到本地后，在 `manifest.json` 中使用相对路径引用：

```json
{
  "dependencies": {
    "com.isakwong.aseprite2unity-ext": "file:../Aseprite2Unity-Ext/Aseprite2Unity/Packages/com.isakwong.aseprite2unity"
  }
}
```

### 方式三：OpenUPM（计划中）

OpenUPM 源尚在准备中，后续版本将提供支持。

---

## 功能概览

### 1. 合图支持（Sprite Atlas）

导入 Aseprite 文件时，所有帧会自动合并为 **一张图集纹理（Atlas）**，而非为每帧生成单独的 `Texture2D`。

- 通过 Inspector 中的 **`Create Atlas`** 开关控制是否启用合图
- 可配置 **`Atlas Padding`**（帧之间的像素间距）
- 图集采用网格布局，自动计算最优行列数以生成接近正方形的纹理
- 显著减少 Draw Call，提升运行时渲染性能

### 2. 可扩展的导入处理管线（Processor Pipeline）

提供了基于 `AsepriteProcessor` 的扩展机制，允许在业务层自定义 Aseprite 导入逻辑，**无需修改插件源码**。

**核心特性：**

| 特性 | 说明 |
|---|---|
| **自动发现** | 所有 `AsepriteProcessor` 子类通过反射自动注册，无需手动配置 |
| **执行顺序** | 通过 `ProcessOrder` 属性控制执行先后（数值越小越先执行） |
| **条件执行** | 重写 `ShouldProcess()` 方法决定是否对当前资源执行处理 |
| **Visitor 模式** | 设置 `NeedVisitChunks = true` 可遍历 Aseprite 原始 Chunk 数据（图层、Cel、调色板、Slice 等） |
| **数据共享** | 通过 `AsepriteImportResult.SharedData` 字典在不同 Processor 之间传递中间数据 |
| **逐资源配置** | 继承泛型基类 `AsepriteProcessor<TSettings>` 可为每个资源保存独立的序列化配置 |

**核心类：**

| 类 | 说明 |
|---|---|
| `AsepriteProcessor` | 处理器基类，继承后重写方法即可扩展导入流程 |
| `AsepriteProcessor<TSettings>` | 泛型处理器基类，支持逐资源序列化配置 |
| `AsepriteProcessorRegistry` | 处理器注册中心，自动发现并按 `ProcessOrder` 排序执行 |
| `AsepriteImportResult` | 导入结果上下文，包含 `Sprites`、`AnimationClips`、`AseFile`、`GameObject`、`SharedData` |
| `AsepriteImportConfig` | 全局导入配置（ScriptableObject） |
| `AsepriteProcessorSettings` | 逐资源处理器配置的基类 |

### 3. 全局配置与逐资源覆盖（Global Config + Override）

> **1.2.0 新增**

引入三级配置解析机制，按优先级从高到低依次为：

1. **逐资源覆盖（Per-Asset Override）** — 若对应的 `m_OverrideXXX` 标志为 `true`，使用该资源上的值
2. **全局配置（Global Config）** — `AsepriteImportConfig` ScriptableObject 中设置的值
3. **字段默认值（Field Default）** — 代码中的硬编码默认值

**可配置项一览：**

| 配置项 | 全局字段 | 逐资源覆盖标志 | 默认值 |
|---|---|---|---|
| Pixels Per Unit | `DefaultPixelsPerUnit` | `m_OverridePixelsPerUnit` | `16` |
| Frame Rate | `DefaultFrameRate` | `m_OverrideFrameRate` | `60` |
| Create Atlas | `DefaultCreateAtlas` | `m_OverrideCreateAtlas` | `true` |
| Atlas Padding | `DefaultAtlasPadding` | `m_OverrideAtlasPadding` | `0` |
| Default Material | `DefaultMaterial` | `m_OverrideMaterial` | `null` |
| Create Animations | `DefaultCreateAnimations` | `m_OverrideCreateAnimations` | `true` |

在代码中通过 `AsepriteImporter` 上的 `Effective*` 属性获取解析后的最终值：

```csharp
float ppu = importer.EffectivePixelsPerUnit;
Material mat = importer.EffectiveMaterial;
```

### 4. Welcome Window 与 Project Settings 面板

> **1.2.0 新增**

#### Welcome Window

通过菜单 **`Window → Aseprite2Unity-Ext → Welcome`** 打开。

- 显示包版本与状态概览
- 检查全局配置（`AsepriteImportConfig`）是否已创建
- 列出当前注册的所有 Processor
- 提供 4 步入门引导
- 快捷操作按钮（创建配置、打开 Project Settings 等）

#### Project Settings 面板

通过菜单 **`Edit → Project Settings → Aseprite2Unity`** 打开。

- 编辑全局导入配置的所有字段
- 查看已注册的 Processor 列表（含执行顺序和类型信息）
- 一键批量重新导入项目中所有 Aseprite 文件
- 刷新 Processor 注册表

### 5. 动画功能

- 根据 Aseprite 文件中的 **Frame Tags** 自动生成 `AnimationClip`
- 支持通过 Cel 的 **UserData** 添加动画事件，格式为 `event:EventName`
- 内置 **20+ 种 Burst 编译的像素混合模式**，精确还原 Aseprite 图层混合效果

---

## 快速开始

1. **安装包** — 按照[安装](#安装)章节的任意方式将包添加到项目
2. **打开 Welcome Window** — 菜单 `Window → Aseprite2Unity-Ext → Welcome`，确认包状态正常
3. **配置全局默认值** — 菜单 `Edit → Project Settings → Aseprite2Unity`，设置 Pixels Per Unit、Frame Rate 等
4. **导入素材** — 将 `.aseprite` / `.ase` 文件拖入 Project 窗口，资源将自动导入
5. **逐资源调整** — 在 Inspector 中勾选对应的 Override 开关，覆盖全局配置
6. **扩展管线** — 编写自定义 `AsepriteProcessor`，实现项目特有的导入逻辑

---

## 自定义 Processor 示例

### 基础 Processor

继承 `AsepriteProcessor` 并实现 `OnImportAseprite` 方法，插件会通过反射自动发现并执行。

```csharp
using Aseprite2Unity.Editor;
using UnityEditor.AssetImporters;

public class MyCustomProcessor : AsepriteProcessor
{
    public override string DisplayName => "My Custom Processor";
    public override int ProcessOrder => 100;

    public override bool ShouldProcess(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
    {
        // 仅处理特定目录下的资源
        return ctx.assetPath.Contains("/MyFolder/");
    }

    public override void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
    {
        var sprites = result.Sprites;
        var clips = result.AnimationClips;
        var go = result.GameObject;
        var material = importer.EffectiveMaterial;
        // 自定义处理逻辑...
    }
}
```

### 带逐资源配置的 Processor

通过泛型基类 `AsepriteProcessor<TSettings>` 为每个 `.aseprite` 资源保存独立的序列化设置，设置数据存储在对应的 `.meta` 文件中。

```csharp
using System;
using Aseprite2Unity.Editor;
using UnityEditor.AssetImporters;

[Serializable]
public class MySettings : AsepriteProcessorSettings
{
    public override string DisplayName => "My Settings";
    public bool enabled = true;
    public string sortingLayer = "Default";
}

public class MyProcessor : AsepriteProcessor<MySettings>
{
    public override string DisplayName => "My Processor";
    public override int ProcessOrder => 200;

    public override void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
    {
        if (!Settings.enabled) return;
        // 使用 Settings.sortingLayer 等配置...
    }
}
```

### Visitor 模式（遍历 Chunk 数据）

设置 `NeedVisitChunks = true` 后，导入时会自动遍历 Aseprite 文件的原始 Chunk 数据，可以重写对应的 `Visit*` 方法来处理图层、Cel、调色板、Slice 等信息。

```csharp
using Aseprite2Unity.Editor;
using UnityEditor.AssetImporters;
using UnityEngine;

public class SliceCollector : AsepriteProcessor
{
    public override string DisplayName => "Slice Collector";
    public override int ProcessOrder => 150;
    public override bool NeedVisitChunks => true;

    public override void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
    {
        // Visitor 回调会在此方法之后触发
    }

    public override void VisitSliceChunk(AseSliceChunk slice, AsepriteImportResult result)
    {
        Debug.Log($"Found slice: {slice.Name}");
    }
}
```

---

## 导入管线流程

```
.aseprite / .ase 文件导入
    ↓
ScriptedImporter.OnImportAsset()
    ↓
AseReader 解析文件 → AseFile
    ↓
Visitor 遍历：生成 Sprites、AnimationClips、TagSprites
    ↓
AsepriteProcessorRegistry.ProcessImport()：
  按 ProcessOrder 排序后，对每个 Processor 依次执行：
    1. ShouldProcess() → 返回 false 则跳过
    2. OnImportAseprite()
    3. 若 NeedVisitChunks = true → PerformChunkVisit()
    ↓
清理临时数据
    ↓
导入完成
```

---

## 核心 API 参考

### AsepriteProcessor

| 成员 | 类型 | 说明 |
|---|---|---|
| `DisplayName` | `string` | 处理器显示名称（用于 UI 和日志） |
| `ProcessOrder` | `int` | 执行顺序，数值越小越先执行 |
| `NeedVisitChunks` | `bool` | 是否需要遍历原始 Chunk 数据（默认 `false`） |
| `ShouldProcess()` | `bool` | 条件判断：是否对当前资源执行处理 |
| `OnImportAseprite()` | `void` | 核心处理方法，在此实现自定义导入逻辑 |
| `VisitLayerChunk()` | `void` | Visitor 回调：图层 Chunk |
| `VisitCelChunk()` | `void` | Visitor 回调：Cel Chunk |
| `VisitPaletteChunk()` | `void` | Visitor 回调：调色板 Chunk |
| `VisitSliceChunk()` | `void` | Visitor 回调：Slice Chunk |

### AsepriteImportResult

| 成员 | 类型 | 说明 |
|---|---|---|
| `Sprites` | `List<Sprite>` | 导入生成的所有 Sprite |
| `AnimationClips` | `List<AnimationClip>` | 导入生成的所有动画片段 |
| `AseFile` | `AseFile` | 解析后的 Aseprite 文件数据 |
| `GameObject` | `GameObject` | 导入生成的根 GameObject |
| `SharedData` | `Dictionary<string, object>` | Processor 间数据共享字典 |

### AsepriteImporter（Effective 属性）

| 属性 | 类型 | 说明 |
|---|---|---|
| `EffectivePixelsPerUnit` | `float` | 解析后的 Pixels Per Unit |
| `EffectiveFrameRate` | `float` | 解析后的帧率 |
| `EffectiveCreateAtlas` | `bool` | 是否生成合图 |
| `EffectiveAtlasPadding` | `int` | 合图像素间距 |
| `EffectiveMaterial` | `Material` | 解析后的默认材质 |
| `EffectiveCreateAnimations` | `bool` | 是否生成动画 |

---

## 作者

- **IsakWong** — Aseprite2Unity-Ext（[GitHub](https://github.com/IsakWong/Aseprite2Unity-Ext)）
- **Sean Barton** — 原版 Aseprite2Unity（[Website](https://seanba.com)）

## 许可证

本项目基于 MIT 许可证开源，详情请参阅 [LICENSE](LICENSE) 文件。

## 致谢

- 原版 [Aseprite2Unity](https://github.com/Seanba/Aseprite2Unity) — Sean Barton
- 示例背景素材使用了 [surt](https://opengameart.org/users/surt) 制作的 [Omega Team tileset](https://opengameart.org/content/omega-team)
