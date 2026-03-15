# Aseprite2Unity-Ext

基于 [Aseprite2Unity](https://github.com/Seanba/Aseprite2Unity) 的扩展版本，在原有功能基础上增加了合图（Sprite Atlas）支持和可扩展的导入处理管线。

## 新增功能

### 合图支持（Sprite Atlas）

导入 Aseprite 文件时，所有帧会自动合并为一张图集纹理（Atlas），而非为每帧生成单独的 Texture2D。

- 在 Inspector 中通过 `Create Atlas` 开关控制是否启用合图
- 可配置 `Atlas Padding`（图集中帧之间的间距）
- 减少 Draw Call，提升运行时渲染性能

### 可扩展的导入处理管线（Processor Pipeline）

提供了基于 `AsepriteProcessor` 的扩展机制，允许在业务层自定义 Aseprite 导入逻辑，无需修改插件源码。

**使用方法：** 继承 `AsepriteProcessor` 并实现 `OnImportAseprite` 方法，插件会自动发现并执行所有 Processor。

```csharp
using Aseprite2Unity.Editor;
using UnityEditor.AssetImporters;

public class MyCustomProcessor : AsepriteProcessor
{
    public override string DisplayName => "我的自定义处理器";
    public override int ProcessOrder => 100;

    public override void OnImportAseprite(AssetImportContext ctx, AsepriteImporter importer, AsepriteImportResult result)
    {
        // 访问导入结果
        var sprites = result.Sprites;
        var clips = result.AnimationClips;
        var go = result.GameObject;

        // 自定义处理逻辑...
    }
}
```

**核心类说明：**

| 类 | 说明 |
|---|---|
| `AsepriteProcessor` | 处理器基类，继承后重写方法即可扩展导入流程 |
| `AsepriteProcessorRegistry` | 处理器注册中心，自动发现并管理所有 Processor |
| `AsepriteImportResult` | 导入结果上下文，包含 Sprites、AnimationClips、AseFile 等数据 |
| `AsepriteImportConfig` | 全局导入配置（ScriptableObject），通过 `Assets > Create > Aseprite2Unity > Import Config` 创建 |

**Processor 特性：**

- 自动发现 — 所有 `AsepriteProcessor` 子类会被自动注册，无需手动配置
- 执行顺序 — 通过 `ProcessOrder` 属性控制执行先后
- 条件执行 — 重写 `ShouldProcess` 方法决定是否对当前资源执行
- Visitor 模式 — 设置 `NeedVisitChunks = true` 后可遍历 Aseprite 原始 Chunk 数据（图层、Cel、调色板等）
- 数据共享 — 通过 `AsepriteImportResult.SharedData` 在不同 Processor 之间传递中间数据
- 每资源配置 — Processor 的序列化字段会保存在各 `.meta` 文件中，支持按资源独立配置

---

## 原始功能

Aseprite2Unity 可以将 `*.ase` / `*.aseprite` 文件导入 Unity 项目，自动生成 Sprite 帧和动画。

Simply install the Aseprite2Unity Unity Package into your Unity projects. Any Aseprite files you have in your project will be automatically imported into prefabs containing the sprite frames and animations.

### Prerequisites

Unity 2020.3 or later.

## Author

- **IsakWong** — Aseprite2Unity-Ext ([GitHub](https://github.com/IsakWong/Aseprite2Unity-Ext))
- **Sean Barton** — Original Aseprite2Unity ([Website](https://seanba.com))

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

* Original [Aseprite2Unity](https://github.com/Seanba/Aseprite2Unity) by Sean Barton
* Example background artwork was made with the [Omega Team tileset](https://opengameart.org/content/omega-team) created by [surt](https://opengameart.org/users/surt)
