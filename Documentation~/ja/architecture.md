# アーキテクチャ

[English](../architecture.md)

宣言的なツリー記述を、順序付きのホストツリー変更リストへと変換するコアパッケージの内部構造。

## サイクル

コアは 4 フェーズのサイクルに参加します。フェーズ 1・2・4 がこのパッケージ、フェーズ 3 (実オブジェクトへのパッチ適用) はレンダラー (`com.uzimaru.veauty-gameobject`) の担当です。

```
        ユーザーコード                      このパッケージ                     レンダラーパッケージ
 ┌──────────────────────┐        ┌─────────────────────────────┐        ┌──────────────────────────┐
 │ [Component] メソッド、 │        │ 1. HookRuntime.Resolve<T>   │        │                          │
 │ Node/KeyedNode ツリー  ├───────►│    関数コンポーネント展開、  │        │                          │
 └──────────────────────┘        │    フック実行、effect 待機   │        │                          │
                                 │              │              │        │                          │
                                 │              ▼              │        │                          │
             旧 resolved ツリー ─►│ 2. Diff<T>.Calc(old, new)   ├───────►│ 3. Patch.Apply(root,     │
                                 │    → IPatch<T>[]            │        │      oldTree, patches)   │
                                 │                             │        │              │           │
                                 │ 4. HookRuntime.CommitEffects│◄───────┤              ▼           │
                                 │    effect とクリーンアップ   │        │    更新済みホストツリー    │
                                 └─────────────────────────────┘        └──────────────────────────┘
                                        ▲
                                        │  State<T>.Set(...)  →  requestRender コールバック
                                        └───────────────── 次のイテレーションをトリガー ─┘
```

### 1. Resolve

`HookRuntime.Resolve<T>(tree)` はツリーを走査し、すべての `FunctionComponentNode` をレンダー関数の出力に置き換えた新しいツリーを返します。

- `IVTreeWrapper` は unwrap されます。
- `FunctionComponentNode` に対しては、ノードの **レンダーパス** (ルートからの子インデックス / キーの列) と **コンポーネントトークン** (デリゲート型 + モジュール MVID + メソッドのメタデータトークン + ターゲット型) から **identity** を計算します。identity はフックスロットを保持する `ComponentInstance` に対応します。ノードの `key` は位置セグメントを置き換えるため、キー付きコンポーネントは兄弟が並べ替わっても状態を保持します。
- コンポーネント関数の実行中だけ `[ThreadStatic]` な「現在の runtime」ポインタがセットされ (`finally` で復元)、これが `Hooks.*` を機能させる仕組みであり、レンダリング外でフックが例外を投げる理由でもあります。
- 通常 / キー付きノードは (子を再帰的に resolve して) **再構築** されます。型付きコンポーネント (`Node<T,U>`) や `IHostLifecycle<T>` のリストは、リフレクションで生成される `HostLifecycleNode` / `HostLifecycleKeyedNode` 系の型を通じて保持されます。
- `Resolve` の呼び出しごとに内部の `renderVersion` が増加し、訪問された各インスタンスがそれを記録します。訪問され **なかった** インスタンスはアンマウント扱いとなり、次の `CommitEffects()` がクリーンアップを実行して破棄します。

### 2. Diff

`Diff<T>.Calc(old, new)` は **resolve 済み** の 2 ツリーを比較して `IPatch<T>[]` を返します。`FunctionComponentNode` が残っていると `InvalidOperationException` を投げます。ノードはノード種別 → tag → 型付きコンポーネント → 属性 → 子 (`Node` は位置、`KeyedNode` はキー) の順で比較されます。詳細は [差分検出](diffing.md)。

### 3. Patch (レンダラーパッケージ)

各パッチは **旧** ツリーに対する **pre-order インデックス** を持ちます。適用側は `IVTree.GetDescendantsCount()` が定める同じ番号付けで実ヒエラルキーを歩き、`SetTarget` で各パッチをホストオブジェクトに束縛してから変更を実行します。インデックスは旧ツリー基準なので、パッチの計算と適用は対応するスナップショットに対して行う必要があります。

### 4. Commit effects

`HookRuntime.CommitEffects()` はパッチ適用後に実行します:

- マウント中の各コンポーネントインスタンスについて、resolve 中にステージされた effect は、まず前回のクリーンアップを呼んでから実行され、新しいクリーンアップを保存します。
- `LastSeenVersion` が古いインスタンスは **すべて** のクリーンアップを実行して削除されます (アンマウント)。
- `Dispose()` は全インスタンスに対して同じクリーンアップを行います (完全な破棄)。

## 状態更新

`State<T>.Set(value)` (または `Set(func)`) はスロットに同期的に書き込み、`HookRuntime` コンストラクタに渡した `requestRender` コールバックを呼びます。コア自身は何もスケジュールしません — 次の resolve/diff/patch/commit をいつ回すかはホストが決めます。`Set` は新旧の値を比較しないため、同じ値でも毎回レンダリング要求が発生する点に注意してください。

## 型マップ

| レイヤー | 型 | ファイル |
|---------|-----|---------|
| ツリー契約 | `IVTree`, `IParent`, `IVTreeWrapper`, `VTreeType` | `Lib/IVTree.cs` |
| 通常ノード | `NodeBase<T>`, `BaseNode<T>`, `Node<T>`, `Node<T,U>`, `ITypedNode` | `Lib/VTree/Node.cs` |
| キー付きノード | `BaseKeyedNode<T>`, `KeyedNode<T>`, `KeyedNode<T,U>` | `Lib/VTree/Node.cs` |
| ライフサイクルノード | `IHostLifecycle<T>`, `IHostLifecycleTree<T>`, `HostLifecycleNode<...>`, `HostLifecycleKeyedNode<...>` | `Lib/VTree/HostLifecycleNode.cs` |
| 関数コンポーネント | `FunctionComponentNode`, `FunctionComponents`, `FunctionComponent` デリゲート | `Lib/VTree/FunctionComponentNode.cs` |
| コンポーネントマーカー | `ComponentAttribute` | `Lib/ComponentAttribute.cs` |
| Hooks | `Hooks`, `State<T>`, `Ref<T>` | `Lib/Hooks.cs` |
| フックエンジン | `HookRuntime` | `Lib/HookRuntime.cs` |
| 属性 | `Attributes<T>`, `IAttribute<T>`, `Attribute<T,U>` | `Lib/Attribute.cs` |
| Diff | `Diff<T>` | `Lib/Diff.cs` |
| Patch | `IPatch<T>`, `Entry`, `Append`, `Attach`, `Attrs`, `Redraw`, `Remove`, `RemoveLast`, `Reorder` | `Lib/IPatch.cs`, `Lib/Patch/*.cs` |
| コンパイル時コード生成 | `ComponentILPostProcessor` (ビルド済み `CodeGen/Veauty.CodeGen.dll`、ソースは `SourceGenerator~/`) | [関数コンポーネント](function-components.md) 参照 |

## 設計メモ

- **イミュータブル** — ノードは一度だけ構築され、`tag`・`attrs`・`kids` は readonly、子孫数はコンストラクタで事前計算されます。更新は常に新しいツリーの構築です。
- **構造的等価性** — ノード・属性・パッチは `Equals`/`GetHashCode` を構造的にオーバーライドします。diff は属性の等価性で未変更の属性をスキップし、テストはパッチ列全体を値で比較します。
- **HostLifecycleNode が `Widget<T>` を置き換え** — 命令的なセットアップ (`Init` / `AfterRenderKids` / `Destroy`) は独立したノード型ではなくノードに付加されるため、通常/キー付き、型付き/型なしのどのノードとも組み合わせられます。

参照: [Hooks](hooks.md)、[関数コンポーネント](function-components.md)、[差分検出](diffing.md)、[API リファレンス](api-reference.md)。
