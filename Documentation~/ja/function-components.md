# 関数コンポーネント

[English](../function-components.md)

`[Component]` 属性、`FunctionComponents` ファクトリ、`FunctionComponentNode`、`HookRuntime.Resolve` による解決、そしてコンパイル時コード生成の実際の仕組み。

## コンポーネントを作る 2 つの方法

### 1. `[Component]` (推奨)

```csharp
using Veauty;
using Veauty.VTree;

[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    Hooks.UseEffect(() => { /* ... */ }, new object[] { interval });
    return new Node<GameObject>("Timer", new IAttribute<GameObject>[] { });
}

// 呼び出し側 — 普通の呼び出しで FunctionComponentNode が返る:
var tree = Timer(1.0f);
```

制約 (IL ポストプロセッサが強制):

- メソッドは **`static` 必須** — そうでなければ `[Component] method '...' must be static.` というコンパイルエラーになる。
- 戻り値は **`IVTree`** として宣言すること (書き換え後の本体は `FunctionComponentNode` を返し、生成されるクロージャの invoker は `IVTree` 型)。
- **ジェネリックメソッドは非対応** (書き換え時にジェネリックパラメータをコピーしない)。
- `Veauty` アセンブリを参照するあらゆるアセンブリで動作する (ゲームのアセンブリを含む)。Veauty を参照しないアセンブリはスキップされる。

### 2. `FunctionComponents` ファクトリ (手動)

```csharp
// 引数なし
FunctionComponents.Create(() => Render());
FunctionComponents.Create("myKey", () => Render());          // キー付き
FunctionComponents.Keyed("myKey", () => Render());           // エイリアス

// 型付き props
FunctionComponents.Create((Props p) => Render(p), props);
FunctionComponents.Create("myKey", (Props p) => Render(p), props);
FunctionComponents.Keyed("myKey", (Props p) => Render(p), props);
```

いずれも `FunctionComponentNode` を構築します。直接のコンストラクタも public ですが、通常は不要です。

## FunctionComponentNode

`FunctionComponentNode` は *プレースホルダー* です。`GetNodeType()` は `VTreeType.FunctionComponent`、`GetDescendantsCount()` は常に `0`。保持するもの:

- `key` — 任意の identity キー (後述)、
- `component` — デリゲート (identity トークンに使用)、
- `props` — 捕捉された props (等価比較に使用)、
- 実際に関数を呼び出す内部の `render` クロージャ。

**diff の前に必ず resolve する必要があります**。これを含むツリーを `Diff<T>.Calc` に渡すと:

```
InvalidOperationException: Function components must be resolved before diffing.
```

## HookRuntime.Resolve

```csharp
var runtime = new HookRuntime(requestRender);
IVTree resolved = runtime.Resolve<GameObject>(tree);
```

`Resolve<T>` はツリーを走査し、各 `FunctionComponentNode` をレンダー関数の出力に置き換えます (再帰的 — コンポーネントが別のコンポーネントを返してもよい)。具象ノードは resolve 済みの子で再構築され、型付きコンポーネントとホストライフサイクルが保持されます。フックはこの呼び出しの中でのみ機能します。`Diff.Calc` に渡す新旧両方のツリーは `Resolve` の出力である必要があります。

### Identity

コンポーネントインスタンス (つまりそのフック状態) は次で識別されます:

```
identity = renderPath + "/c" + componentToken
componentToken = デリゲート型 | モジュール MVID | メソッドのメタデータトークン | ターゲット型
```

- `renderPath` は位置をエンコードします: 子インデックス (`i0`)、キー付き子のキー (`k...`)、ネストしたコンポーネント境界 (`c...`)、レンダー結果ルート (`r`)。
- ノードの `key` は **位置セグメントを置き換える** ため、`FunctionComponents.Keyed("item-42", ...)` は兄弟の並べ替えでも状態を保ちます。
- トークンはメソッドのメタデータトークンと宣言モジュールを使うため、引数付きの `[Component]` メソッドが呼び出しごとに新しいクロージャオブジェクトを割り当てても、レンダー間で安定です (デリゲートの *ターゲット型* は安定で、インスタンス自体はトークンに含まれない)。

帰結: 同じツリー位置でも異なるコンポーネント関数は別インスタンスになります。同じ関数がキーなしで別の位置に移動すると *新しい* インスタンスになります (旧インスタンスはアンマウント)。

## コード生成の仕組み

### 何が同梱され、何が動くか

リポジトリの `SourceGenerator~/` (末尾 `~` により Unity からは無視されるフォルダ) には **2 つ** のコード生成実装があります:

| ファイル | 種類 | 状態 |
|---------|------|------|
| `ComponentILPostProcessor.cs` | Unity の `ILPostProcessor` (Mono.Cecil) | **有効** — `CodeGen/Veauty.CodeGen.dll` にコンパイル済み |
| `ComponentGenerator.cs` | Roslyn の `IIncrementalGenerator` | **無効** — ビルドから除外 (csproj の `<Compile Remove="ComponentGenerator.cs" />`) |

同梱の `CodeGen/Veauty.CodeGen.dll` は Editor 有効のプラグインです。Unity のスクリプトパイプラインがその中の `ILPostProcessor` サブクラスを発見し、各アセンブリのコンパイル後に実行します。(放棄された Roslyn 生成器は `partial` クラス内の `_` プレフィックス付きメソッドにラッパーを生成する別方式で、現行システムの動作 **ではありません**。)

### IL 書き換えの内容

`Veauty` を参照するアセンブリ内の `[Component]` 付き `static` メソッドごとに:

1. 元のメソッド本体を隠しメソッド `<Name>__ComponentImpl` (同じ引数) に移動。
2. 元のメソッドに新しい本体を生成:
   - **引数なし:** `return FunctionComponents.Create(new FunctionComponent(<Name>__ComponentImpl));`
   - **引数あり:** 引数ごとの public フィールドと、フィールドを impl に転送する `__invoke() : IVTree` メソッドを持つ private なネストクロージャクラス `<Name>__ComponentClosure` を生成。ラッパーはクロージャを生成して引数をコピーし、`FunctionComponents.Create(new FunctionComponent(closure.__invoke))` を返す。
3. メソッドから `[Component]` 属性を削除 — コンパイル済みアセンブリには存在しない。

つまりメソッドの *呼び出し* は本体を実行せず、引数を捕捉するだけです。本体は後で `HookRuntime.Resolve` の中で、レンダーごとに 1 回実行されます。

### 注意: `[Component]` メソッドに `key` 引数はない

書き換えは常にキーなしの `FunctionComponents.Create` を呼びます。同じコンポーネントの *リスト* をレンダリングしてキー付き identity が必要な場合は、手動でラップしてください:

```csharp
FunctionComponents.Keyed(item.Id, () => TodoItem(item));
```

### ポストプロセッサの再ビルド

```bash
cd SourceGenerator~
dotnet build -c Release
cp bin/Release/netstandard2.1/Veauty.CodeGen.dll ../CodeGen/
```

csproj はローカルの Unity インストールから `Unity.CompilationPipeline.Common` と Mono.Cecil を参照します。Unity のバージョンが違う場合は `HintPath` を調整してください。

参照: [Hooks](hooks.md)、[アーキテクチャ](architecture.md)、[API リファレンス](api-reference.md)。
