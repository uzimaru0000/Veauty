# はじめに

[English](../getting-started.md)

コアパッケージをインストールし、上位パッケージが自動化している render → update のサイクルを一度手動で回して仕組みを理解します。

## インストール

`Packages/manifest.json` にパッケージを追加:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

Unity 6000.5 以上が必要です。実際のプロジェクトでは通常 `com.uzimaru.veauty-gameobject` (レンダラー) と `com.uzimaru.veauty-ugui` (ウィジェット) も併用しますが、このページではすべてのステップが見えるようにコアのみを使います。

## 最小の動作例

コアはホスト非依存です。`Node<T>` や `Diff<T>` の `T` はレンダラーが管理するオブジェクト型 (Unity では `GameObject`、テストでは `object`) です。以下は `object` を使ってサイクルを手動で回す例で、GameObject パッケージの `VeautyObject<State>` が肩代わりしている処理そのものです。

```csharp
using System;
using Veauty;
using Veauty.VTree;

public static class Example
{
    // 1. 状態を持つ関数コンポーネント
    [Component]
    static IVTree Counter()
    {
        var count = Hooks.UseState(0);

        Hooks.UseEffect(() =>
        {
            Console.WriteLine($"count is now {count.Value}");
            return () => Console.WriteLine("cleanup");
        }, new object[] { count.Value });

        return new Node<object>("counter",
            new IAttribute<object>[] { },
            new Node<object>($"label:{count.Value}", new IAttribute<object>[] { }));
    }

    public static void Run()
    {
        // 2. マウントするツリーごとに 1 つの runtime。State.Set でコールバックが発火する。
        HookRuntime runtime = null;
        IVTree currentTree = null;

        runtime = new HookRuntime(() =>
        {
            // 3. 再レンダリング: 新ツリーを resolve → 旧ツリーと diff → 適用 → commit
            var nextTree = runtime.Resolve<object>(Counter());
            var patches = Diff<object>.Calc(currentTree, nextTree);
            // ... ここで `patches` をパッチ適用側に渡す ...
            currentTree = nextTree;
            runtime.CommitEffects();
        });

        // 4. 初回マウント: resolve → render → commit
        currentTree = runtime.Resolve<object>(Counter());
        // ... ここで `currentTree` を初回レンダリング ...
        runtime.CommitEffects();

        // 5. 破棄: すべての effect のクリーンアップを実行
        runtime.Dispose();
    }
}
```

## 各行の解説

- **`[Component]`** — static メソッドを関数コンポーネントにするマーカー。コンパイル時に IL ポストプロセッサがメソッドを書き換え、`Counter()` の呼び出しが本体を即実行せずに `FunctionComponentNode` を返すようになります。これにより本体は独自のフック状態とマウント/アンマウントのライフサイクルを持ちます。詳細は [関数コンポーネント](function-components.md)。
- **`Hooks.UseState(0)`** — 状態スロットを宣言します。初期値は初回レンダリングでのみ使用され、`count.Set(...)` は新しい値を保存して runtime の `requestRender` コールバックを呼びます。
- **`Hooks.UseEffect(..., new object[] { count.Value })`** — 副作用をステージングします。実行は `CommitEffects()` の中 (レンダリング中には決して実行されない) で、依存値が前回から変わったときのみです (`object.Equals` による要素ごとの比較)。戻り値の Action はクリーンアップで、次回実行前とアンマウント時に呼ばれます。詳細は [Hooks](hooks.md)。
- **`new Node<object>(tag, attrs, kids)`** — 通常の仮想ノード。`tag` はノードの種類を識別し (tag が異なるノードは更新ではなく再描画)、`attrs` はホストオブジェクトに適用、`kids` は位置ベースで差分されます。リストには `KeyedNode` を使ってください。
- **`new HookRuntime(requestRender)`** — マウントされた 1 つのツリーのコンポーネントインスタンスとフックスロットをすべて所有します。いずれかの `State<T>.Set` が実行されるとコールバックが (同期的に) 呼ばれます。
- **`runtime.Resolve<object>(tree)`** — すべての `FunctionComponentNode` を、フックを有効にした状態でコンポーネント関数を呼んで具象ノードに展開します。`Diff.Calc` に渡す **両方の** ツリーは resolve 済みでなければならず、未解決のコンポーネントを diff すると `InvalidOperationException` が投げられます。
- **`Diff<object>.Calc(old, new)`** — pre-order インデックスで宛先を示すパッチ列 (`Redraw`, `Attrs`, `Append`, ...) を生成します。詳細は [差分検出](diffing.md)。
- **`runtime.CommitEffects()`** — ステージされた effect を実行し、直近の `Resolve` で消えたコンポーネントをクリーンアップします。effect が更新後のホストツリーを観測できるよう、パッチ適用の **後** に呼びます。
- **`runtime.Dispose()`** — 残っているクリーンアップをすべて実行します。ツリーを完全にアンマウントするときに呼びます。

## 次のステップ

- [アーキテクチャ](architecture.md) — 各部品の組み合わさり方。
- [Hooks](hooks.md) — Hooks API とルールの全容。
- [API リファレンス](api-reference.md) — すべての公開型。
