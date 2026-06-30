# Veauty

Unity 向け Virtual DOM 実装。React ライクな宣言的 API で、仮想ツリーの差分検出・パッチ適用により効率的に UI を構築できます。

## 要件

- Unity 6000.5 以上 (Unity 6)

## インストール

`Packages/manifest.json` に追加:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

## パッケージ構成

Veauty は 3 つのパッケージで構成されています:

| パッケージ | 役割 |
|-----------|------|
| `com.uzimaru.veauty` (このパッケージ) | VTree 型定義、Diff アルゴリズム、Hooks、`[Component]` 属性 |
| [`com.uzimaru.veauty-gameobject`](https://github.com/uzimaru0000/Veauty-GameObject) | Renderer、Patch 適用、`VeautyObject` エントリーポイント |
| [`com.uzimaru.veauty-ugui`](https://github.com/uzimaru0000/Veauty-uGUI) | uGUI ウィジェット、Presets API (`V.Text`, `V.Button` 等) |

## VTree 型

- **`Node<T>`** / **`Node<T,U>`** — タグ、属性、子ノードを持つ基本ノード。`U` は Unity コンポーネント型。
- **`KeyedNode<T,U>`** — キー付きノード。リストの効率的な差分検出用。
- **`FunctionComponentNode`** — 関数コンポーネント。`[Component]` 属性または `FunctionComponents.Create()` で生成。

## Hooks

関数コンポーネント内で状態やライフサイクルを管理する API です。

```csharp
// 状態管理 - 値の変更で再レンダリングがトリガーされる
var count = Hooks.UseState(0);
count.Set(x => x + 1);

// 参照 - 値を変更しても再レンダリングされない
var renderCount = Hooks.UseRef(0);
renderCount.Current++;

// 副作用 - マウント/アンマウント時やデプス変更時に実行
Hooks.UseEffect(() =>
{
    Debug.Log("mounted");
    return () => Debug.Log("unmounted");  // クリーンアップ
}, new object[] { /* dependencies */ });
```

## `[Component]` 属性

static メソッドに `[Component]` をつけると、そのメソッドが関数コンポーネントになります。ILPostProcessor がコンパイル時にメソッド本体を `FunctionComponents.Create()` でラップするため、呼び出し側は普通の関数呼び出しと同じように書けます。

```csharp
// 定義
[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    Hooks.UseEffect(() => { /* ... */ }, new object[] { interval });
    return new Node<GameObject>("Timer", /* ... */);
}

// 呼び出し - ラップ不要
Timer(1.0f)
```

`[Component]` をつけないと、Hooks が呼び出し元のスコープで実行され、独立したライフサイクル (マウント/アンマウント) を持てません。

### ILPostProcessor のビルド

ソースは `SourceGenerator~/` にあります。変更後の再ビルド:

```bash
cd SourceGenerator~
dotnet build -c Release
cp bin/Release/netstandard2.1/Veauty.CodeGen.dll ../CodeGen/
```

## Diff / Patch

`Diff<T>.Calc(oldTree, newTree)` で差分を計算し、`Patch.Apply()` で既存の GameObject ツリーに適用します。

| パッチ種別 | 動作 |
|-----------|------|
| `Redraw` | ノードを置き換え |
| `Attrs` | 属性の差分を適用 |
| `Attach` | 型付きコンポーネントの変更 |
| `Append` | 子ノードを末尾に追加 |
| `RemoveLast` | 末尾の子ノードを削除 |
| `Remove` | ノードを削除 |
| `Reorder` | キー付き子ノードの並べ替え |
