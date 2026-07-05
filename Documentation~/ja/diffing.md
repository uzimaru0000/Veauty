# 差分検出

[English](../diffing.md)

`Diff<T>.Calc` の差分アルゴリズム、全パッチ型、pre-order インデックス規約、キー付き子要素のマッチング。

## エントリポイント

```csharp
IPatch<T>[] patches = Diff<T>.Calc(oldTree, newTree);
```

両方のツリーは **resolve 済み** である必要があります (どこにも `FunctionComponentNode` がないこと — [関数コンポーネント](function-components.md) 参照)。そうでなければ `InvalidOperationException` が投げられます。`IVTreeWrapper` は比較前に unwrap されます。未知の `IVTree` 実装は `ArgumentException` になります。

## ノード比較の順序

ノードペア `x` (旧) と `y` (新) に対して:

1. **通常 vs キー付きの不一致** — 片方が `BaseNode<T>` でもう片方が `BaseKeyedNode<T>` の場合、キー付き側が *de-key* され (型付きコンポーネントとホストライフサイクルを保持したまま等価な通常ノードに変換)、位置ベースで差分が続行される。再描画はされない。
2. **ノード種別の不一致** (残る `GetNodeType()` の差) → `Redraw`。
3. **tag の不一致** → `Redraw`。そのノード以下は一切見ない。
4. **型付きコンポーネント** — 両方が `ITypedNode` で `GetComponentType()` が異なる → `Attach(old, new)` パッチ (子と属性は引き続き差分され、ノードは再描画されない)。
5. **属性** — 属性辞書のキー単位の比較:
   - 旧のみにあるキー → エントリ `key -> null` (削除)、
   - 両方にあり `!old.Equals(new)` → エントリ `key -> newAttr`、
   - 新のみにあるキー → エントリ `key -> newAttr`。
   結果が空でなければ 1 つの `Attrs` パッチになる。属性の等価性は `Attribute<T,U>` のキー + 値の `object.Equals` — 適切な `Equals` を持たない値を保持する属性は常に「変更あり」と見なされる。
6. **子要素** — 通常ノードは位置ベース、キー付きノードはキーベース (後述)。

## pre-order インデックス規約

パッチは **旧** ツリーに対する pre-order の整数インデックスでノードを指定します:

```
index(root) = 0
index(最初の子) = 親 + 1
index(次の兄弟) = 前の兄弟 + 1 + 前の兄弟.GetDescendantsCount()
```

`GetDescendantsCount()` はノードのコンストラクタで `kids.Length + Σ kid.GetDescendantsCount()` として事前計算されます。`FunctionComponentNode.GetDescendantsCount()` は `0` — プレースホルダーは resolve されるまで 1 スロットを占めます (そもそも resolve 済みツリーには存在しません)。

レンダラーパッケージのパッチ適用側は *同じ* 番号付けで実オブジェクト階層を歩いて各パッチのターゲットを見つけ、`SetTarget` を呼びます。したがって:

- パッチは `oldTree` に正確に対応するホストツリーに適用しなければならない;
- VTree から見えない内部ホスト子要素は適用側がスキップする必要がある (GameObject パッケージは子オフセットで対応)。

### 位置ベースの子差分 (`DiffKids`)

通常ノードで旧の長さ `m`、新の長さ `n` のとき:

- `m > n` → `RemoveLast(index, n, m - n)` を 1 つ (末尾の子を削除)。
- `m < n` → `Append(index, m, newKids)` を 1 つ (`newKids[m..]` をレンダリング)。
- その後、先頭 `min(m, n)` ペアを再帰的に差分し、子ごとにインデックスを `1 + 旧子.GetDescendantsCount()` 進める。

位置ベースの差分では、*キーなしリストの先頭への挿入は全要素を書き換えます*。動的リストにはキー付きノードを使ってください。

## キー付き子差分 (`DiffKeyedKids`)

`KeyedNode` の子は `(string key, IVTree)` ペアです。アルゴリズムは 2 つのカーソルと 1 要素の先読みで両リストを走査し、各ステップを分類します:

| ケース | 条件 | アクション |
|--------|------|-----------|
| 一致 | `xKey == yKey` | 再帰、両方進める |
| 入れ替え | `xKey == yNextKey && yKey == xNextKey` | `x` と `yNext` を差分、`yKey` を挿入、旧 `xNext` を削除; 両方 2 進める |
| 挿入 | `xKey == yNextKey` | この位置に `yKey` を挿入、`x` と `yNext` を差分; x を 1、y を 2 進める |
| 削除 | `yKey == xNextKey` | `xKey` を削除、`xNext` と `y` を差分; x を 2、y を 1 進める |
| 置換 | `xNextKey == yNextKey` | `xKey` を削除、`yKey` を挿入、next 同士を差分; 両方 2 進める |
| 打ち切り | どれにも該当しない | 走査を停止; 残りの旧キーは削除、残りの新キーは *end insert* になる |

**同じキー** の削除 + 挿入は **move** に融合されます: (`IPatch.cs` の) `Entry` 帳簿がエントリを `Entry.Type.Move` に切り替え、新旧サブツリーを差分し、そのサブパッチをペンディング中の `Remove` パッチに付加します。適用側はホストオブジェクトを破棄せずにデタッチして再挿入します。

何か変更があれば、親に対して 1 つの `Reorder(index, subPatches, inserts, endInserts)` パッチが発行されます。

### キーが安定かつ一意でなければならない理由

- **安定:** レンダー間の identity は *キー文字列のみ* です。変化するキー (例: リスト内の位置から導出) は move を remove + create に変え、ホストの状態を破壊し、内部の関数コンポーネントをアンマウントします。
- **一意:** 重複キーを検出すると、アルゴリズムは `key + "UniVDOM"` (さらに `"UniVDOMUniVDOM"`、...) で再帰して黙って区別します。センチネル付きのエントリは本来のキーとペアになれないため、重複は remove/insert の churn に劣化します — また、ユーザーのキーが文字通り `"UniVDOM"` で終わるとセンチネルと衝突する可能性があります。重複キーに依存しないでください。

## パッチ型リファレンス

| パッチ | 発行条件 | ペイロード |
|--------|----------|-----------|
| `Redraw<T>` | ノード種別または tag が異なる | `vTree` — 置き換えるサブツリー |
| `Attach<T>` | `ITypedNode` のコンポーネント型が異なる | `oldComponent`, `newComponent` 型 |
| `Attrs<T>` | 属性辞書が異なる | `attrs: key -> newAttr` (`null` = 削除) |
| `Append<T>` | 新ノードの子が多い | `length` (旧の数), `kids` (新の全リスト) |
| `RemoveLast<T>` | 新ノードの子が少ない | `length` (新の数), `diff` (削除数) |
| `Remove<T>` | キー付き子の削除 (または move のデタッチ) | move の一部なら `patches` + `entry` |
| `Reorder<T>` | キー付き子が変化 | `patches`, `inserts`, `endInserts` (end insert のエントリは `index == -1`) |

すべてのパッチ型は `IPatch<T>` (`GetIndex`, `GetTarget`/`SetTarget`) と構造的 `Equals`/`GetHashCode` を実装します。

参照: [アーキテクチャ](architecture.md)、[API リファレンス](api-reference.md)。
