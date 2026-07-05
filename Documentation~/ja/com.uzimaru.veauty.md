# Veauty

[English](../com.uzimaru.veauty.md)

Unity 向け Virtual DOM 実装 Veauty のコアパッケージ。仮想ツリー型、Diff アルゴリズム、Patch 型、Hooks、`[Component]` による関数コンポーネントシステムを提供します。

## このパッケージについて

`com.uzimaru.veauty` は Veauty スタックのレンダラー非依存なコアです。UI ツリーが「何であるか」と 2 つのツリーを「どう比較するか」を定義し、`GameObject` には一切触れません。レンダリングとパッチ適用は [`com.uzimaru.veauty-gameobject`](https://github.com/uzimaru0000/Veauty-GameObject)、uGUI ウィジェットは [`com.uzimaru.veauty-ugui`](https://github.com/uzimaru0000/Veauty-uGUI) が担当します。

| 役割 | 型 |
|------|-----|
| ツリー構造 | `IVTree`, `Node<T>` / `Node<T,U>`, `KeyedNode<T>` / `KeyedNode<T,U>`, `HostLifecycleNode<...>` |
| 属性 | `IAttribute<T>`, `Attribute<T,U>`, `Attributes<T>` |
| 関数コンポーネント | `FunctionComponentNode`, `FunctionComponents`, `[Component]`, `HookRuntime` |
| 状態・ライフサイクル | `Hooks.UseState` / `UseRef` / `UseEffect`, `State<T>`, `Ref<T>` |
| 差分検出 | `Diff<T>.Calc`, `IPatch<T>`, `Append` / `Attach` / `Attrs` / `Redraw` / `Remove` / `RemoveLast` / `Reorder` |

## 要件

- Unity 6000.5 以上 (Unity 6)
- `com.unity.nuget.mono-cecil` に依存 (同梱の IL ポストプロセッサが使用)

## インストール

`Packages/manifest.json` に追加:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

## クイックスタート

```csharp
using Veauty;
using Veauty.VTree;

[Component]
static IVTree Counter()
{
    var count = Hooks.UseState(0);
    return new Node<UnityEngine.GameObject>("counter",
        new IAttribute<UnityEngine.GameObject>[] { },
        new Node<UnityEngine.GameObject>($"count: {count.Value}",
            new IAttribute<UnityEngine.GameObject>[] { }));
}
```

マウントには `com.uzimaru.veauty-gameobject` の `VeautyObject` を使うか、`HookRuntime` + `Diff<T>` で自前でサイクルを回します ([アーキテクチャ](architecture.md) 参照)。

## ドキュメント

- [はじめに](getting-started.md) — インストールと最小のエンドツーエンド例。
- [アーキテクチャ](architecture.md) — resolve → diff → patch → commit のサイクル。
- [Hooks](hooks.md) — `UseState` / `UseRef` / `UseEffect`、フックのルール、副作用の実行タイミング。
- [関数コンポーネント](function-components.md) — `[Component]`、`FunctionComponents`、IL ポストプロセッサの仕組み。
- [差分検出](diffing.md) — Diff アルゴリズム、Patch の種類、pre-order インデックス規約、キー付き差分。
- [API リファレンス](api-reference.md) — すべての公開型とメンバー。

[エージェントガイド](../../AGENTS.md) と [Changelog](../../CHANGELOG.md) も参照してください。
