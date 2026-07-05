# Hooks

[English](../hooks.md)

関数コンポーネントのための状態・参照・副作用: `Hooks` API、ランタイムが強制するルール、effect とクリーンアップの正確な実行タイミング。

## フックを呼べる場所

すべての `Hooks.*` メソッドは `HookRuntime.Current` に委譲します。これは `[ThreadStatic]` フィールドで、`HookRuntime.Resolve<T>()` の中で **関数コンポーネントのレンダー関数が実行されている間だけ** 非 null になります。それ以外の場所でフックを呼ぶと次の例外が投げられます:

```
InvalidOperationException: Hooks can only be used while rendering a function component.
```

このフィールドは各コンポーネントレンダーの前後で `try/finally` により保存・復元されるため、コンポーネントのネストは正しく動作し、レンダー中の例外で runtime ポインタが漏れることもありません。

## UseState

```csharp
State<T> Hooks.UseState<T>(T initialValue)
```

- 初回レンダー: 新しいスロットに `initialValue` を保存。
- 以降のレンダー: 現在の保存値を返す。`initialValue` は無視される。
- `state.Value` で読み取り、`state.Set(value)` / `state.Set(v => ...)` は **同期的に** 書き込んで runtime の `requestRender` コールバックを呼ぶ。
- `Set` は **等価性チェックを行わない** — 同じ値でもレンダリング要求が発生する。コアは更新をバッチしない (バッチするならホストの責務)。
- 同じスロット位置で `T` をレンダー間で変えると `InvalidOperationException("Hook slot type changed between renders.")`。

## UseRef

```csharp
Ref<T> Hooks.UseRef<T>(T initialValue = default)
```

- コンポーネントインスタンスの毎レンダーで **同一の `Ref<T>` インスタンス** を返す。
- `refBox.Current` の変更は再レンダリングを起こさない — レンダーカウンタ、キャッシュ、ホストオブジェクトの捕捉などに。
- スロット型チェックは `UseState` と同様。

## UseEffect

4 つのオーバーロード:

```csharp
void Hooks.UseEffect(Action effect)                              // クリーンアップなし、毎レンダー後に実行
void Hooks.UseEffect(Action effect, params object[] deps)        // クリーンアップなし、deps 変更時に実行
void Hooks.UseEffect(Func<Action> effect)                        // クリーンアップあり、毎レンダー後に実行
void Hooks.UseEffect(Func<Action> effect, params object[] deps)  // クリーンアップあり、deps 変更時に実行
```

`Func<Action>` 形式はクリーンアップの Action (不要なら `null`) を返します。

### 依存値のセマンティクス

`ShouldRun` が effect を (再) ステージするかを次の順で判定します:

1. まだ一度も実行していない → 実行 (マウント)。
2. どちらかの `deps` が `null` → 実行 (deps なしオーバーロードは `null` を渡す = 「毎レンダー」)。
3. 前回の deps と長さが違う → 実行。
4. それ以外は **`object.Equals`** で要素ごとに比較 — 1 つでも不一致なら実行。

帰結:

- `new object[] { }` (空配列) → effect は **マウント時のみ** 実行。
- 値型はボックス化されて値で比較される (`1.Equals(1)` → 等しい)。参照型は各自の `Equals` で比較され、意味のある `Equals` を持たない型 (ラムダや生の配列など) は参照比較になるため、新しいインスタンスを渡すたびに effect が再実行される。
- **注意:** `deps` は `params` なので、`Hooks.UseEffect(fn, null)` は `null` 配列を渡すことになり、ラッパーが空配列に変換する → マウント時のみになる。「毎レンダー」の挙動になるのは deps 引数の **ない** オーバーロードだけ。

### コミットタイミング

effect はレンダリング中には実行されません。`UseEffect` はスロットに effect を *ステージ* (`Pending = true`) するだけで、実行はホストが `HookRuntime.CommitEffects()` を呼んだときです。effect が更新後のホストツリーを観測できるよう、**パッチ適用後** に呼ぶべきです。ペンディング中の各 effect について:

1. 前回のクリーンアップ (あれば) を実行。
2. effect を実行し、その戻り値を新しいクリーンアップとして保存。
3. ステージされた deps を現在の deps にする。

### クリーンアップのセマンティクス

- 同じ effect の再実行のたびに: 先に前回のクリーンアップが実行される。
- **アンマウント時**: 直近の `Resolve` で訪問されなかったインスタンス (`LastSeenVersion` と runtime の `renderVersion` の比較で検出) は、`CommitEffects()` の中で全 effect のクリーンアップが実行され、インスタンスと状態が破棄される。
- **`HookRuntime.Dispose()` 時**: 全インスタンスのクリーンアップが実行され、すべての状態がクリアされる。マウントしたツリーを破棄するときに呼ぶこと。

## フックのルール (ランタイムで強制)

フックスロットはコンポーネントインスタンス内で **呼び出し順** により対応付けられます。ランタイムは毎レンダーで検証します:

1. **同じ順序・同じ数** — ある位置のスロット種別が変わると (例: 以前 `UseEffect` だった場所に `UseState`)、`InvalidOperationException("Hook order changed between renders.")`。回数が変動する `if` やループの中でフックを呼ばないこと。
2. **同じ値型** — スロットの `T` は変更不可: `"Hook slot type changed between renders."`
3. **レンダー中のみ** — 上記参照。

補足: 前回より *多く* フックを呼ぶと新しいスロットがエラーなく追加されますが、ルール 1 に従い、フック呼び出しは常に無条件にしてください。

## コンポーネントの identity — 状態はいつ生き残るか

フック状態は *コンポーネントインスタンス* に属し、レンダーパス + コンポーネントトークンで識別されます ([関数コンポーネント](function-components.md) 参照)。コンポーネントが同じ親チェーンの下で同じ位置に — または同じ `key` を — 保っている間、状態は再レンダーをまたいで生き残ります。パスが変わると (例: キーなしコンポーネントが兄弟の挿入でインデックスがずれる)、旧インスタンスはアンマウントされ (クリーンアップ実行)、新しいインスタンスがマウントされます。

## 完全な例

```csharp
[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    var running = Hooks.UseRef(true);

    // `interval` が変わるたびにティッカーを再起動し、アンマウント時に停止する
    Hooks.UseEffect(() =>
    {
        var handle = Ticker.Start(interval, dt =>
        {
            if (running.Current) elapsed.Set(x => x + dt);
        });
        return () => Ticker.Stop(handle);
    }, new object[] { interval });

    return new Node<GameObject>($"elapsed:{elapsed.Value:F1}",
        new IAttribute<GameObject>[] { });
}
```

参照: [関数コンポーネント](function-components.md)、[アーキテクチャ](architecture.md)、[API リファレンス](api-reference.md)。
