# API リファレンス

[English](../api-reference.md)

`Veauty` アセンブリ (`com.uzimaru.veauty` 0.5.0) のすべての公開型を名前空間ごとにまとめたもの。`T` は常にホストオブジェクト型 (例: `GameObject`)、`U` はホストコンポーネント型。

## 名前空間 `Veauty`

### enum `VTreeType`

```csharp
public enum VTreeType { Node, KeyedNode, FunctionComponent }
```

diff のためのノード種別判別子。`FunctionComponent` ノードは diff 不可 — 先に resolve が必要。

### interface `IVTree`

```csharp
public interface IVTree
{
    VTreeType GetNodeType();
    int GetDescendantsCount();
}
```

| メンバー | 説明 |
|----------|------|
| `GetNodeType()` | ノード種別の判別子。 |
| `GetDescendantsCount()` | 全子孫の数。diff と patch が共有する pre-order インデックス規約を定義する ([差分検出](diffing.md) 参照)。`FunctionComponentNode` は 0 を返す。 |

### interface `IParent`

```csharp
public interface IParent
{
    IVTree[] GetKids();
}
```

レンダー順の子要素。キー付きノードはキーを外した子を返す。

### interface `IVTreeWrapper`

```csharp
public interface IVTreeWrapper : IVTree
{
    IVTree Unwrap();
}
```

別のノードに委譲するノード。`Diff.Calc` と `HookRuntime.Resolve` は透過的に unwrap する。

### class `Attributes<T>`

```csharp
public class Attributes<T>
{
    public Dictionary<string, IAttribute<T>> attrs;
    public Attributes(IEnumerable<IAttribute<T>> attrs);
}
```

キーでインデックスされた属性集合。**注意:** 重複キーは黙って畳まれる — 後の属性が勝つ。

### interface `IAttribute<T>`

```csharp
public interface IAttribute<T>
{
    string GetKey();
    void Apply(T obj);
}
```

### abstract class `Attribute<T, U>` : `IAttribute<T>`

```csharp
public abstract class Attribute<T, U> : IAttribute<T>
{
    protected Attribute(string key, U value);
    public string GetKey();
    protected U GetValue();
    public abstract void Apply(T obj);
    public override bool Equals(object obj);   // key == key && object.Equals(value, value)
    public override int GetHashCode();
}
```

値を保持する属性の基底クラス。等価性 (キー + 値の `object.Equals`) により diff は未変更の属性をスキップする — 意味のある `Equals` を持つ値を保持しないと、属性は毎レンダー再適用される。

### sealed class `ComponentAttribute` : `System.Attribute`

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComponentAttribute : Attribute { }
```

`IVTree` を返す **static** メソッドを関数コンポーネントとしてマークする。同梱の ILPostProcessor がコンパイル時に書き換え、属性自体は削除される — [関数コンポーネント](function-components.md) 参照。

### static class `Hooks`

```csharp
public static class Hooks
{
    public static State<T> UseState<T>(T initialValue);
    public static Ref<T>   UseRef<T>(T initialValue = default);
    public static void UseEffect(Action effect);                                   // 毎レンダー
    public static void UseEffect(Action effect, params object[] dependencies);     // deps 変更時
    public static void UseEffect(Func<Action> effect);                             // 毎レンダー、クリーンアップあり
    public static void UseEffect(Func<Action> effect, params object[] dependencies);
}
```

| メンバー | 説明 |
|----------|------|
| `UseState` | 状態スロット。初期値は初回レンダーでのみ保存される。 |
| `UseRef` | 安定した可変の `Ref<T>` ボックス。変更しても再レンダーされない。 |
| `UseEffect` | `HookRuntime.CommitEffects()` で実行される effect をステージする。deps は `object.Equals` で要素比較。空配列 = マウント時のみ。deps なしオーバーロード = 毎レンダー。`Func<Action>` 系はクリーンアップを返す。 |

関数コンポーネントのレンダー外で呼ぶと `InvalidOperationException`。詳細: [Hooks](hooks.md)。

### readonly struct `State<T>`

```csharp
public readonly struct State<T>
{
    public T Value { get; }
    public void Set(T value);
    public void Set(Func<T, T> update);   // update が null なら ArgumentNullException
}
```

状態スロットへのハンドル。`Set` は同期的に書き込み、常に runtime の `requestRender` コールバックを呼ぶ (等価性チェックなし、バッチなし)。

### sealed class `Ref<T>`

```csharp
public sealed class Ref<T>
{
    public Ref(T current);
    public T Current { get; set; }
}
```

### sealed class `HookRuntime`

```csharp
public sealed class HookRuntime
{
    public HookRuntime(Action requestRender = null);
    public IVTree Resolve<T>(IVTree tree);
    public void CommitEffects();
    public void Dispose();
}
```

| メンバー | 説明 |
|----------|------|
| コンストラクタ | `requestRender` は `State<T>.Set` のたびに呼ばれる。省略時は no-op。 |
| `Resolve<T>` | すべての `FunctionComponentNode` をフックを実行しながら具象ノードに展開する。`Diff.Calc` に渡す両ツリーは resolve 済みであること。`ArgumentNullException` (null ツリー)、`InvalidOperationException` (コンポーネントが null を返した / フックのルール違反)、`ArgumentException` (未知のノード型) を投げる。 |
| `CommitEffects` | ステージ済み effect を実行し (先に前回クリーンアップ)、直近の `Resolve` でアンマウントされたインスタンスをクリーンアップして削除する。パッチ適用後に呼ぶこと。 |
| `Dispose` | 保存済みクリーンアップをすべて実行し、全インスタンスをクリアする。 |

補足: `Hooks` が使う現在の runtime は `[ThreadStatic]` で、コンポーネントレンダー中のみセットされる。runtime はマウントされたツリーごとに 1 つ。

### static class `Diff<T>`

```csharp
public static class Diff<T>
{
    public static IPatch<T>[] Calc(IVTree x, IVTree y);
}
```

旧ツリー `x` と新ツリー `y` を差分し、pre-order インデックス順のパッチを返す。どちらかに `FunctionComponentNode` が含まれると `InvalidOperationException`。アルゴリズム詳細: [差分検出](diffing.md)。

### interface `IPatch<T>`

```csharp
public interface IPatch<T>
{
    T GetTarget();
    void SetTarget(in T target);
    int GetIndex();
}
```

旧ツリーへの pre-order インデックスで宛先を示すホストツリー変更。適用側は `SetTarget` で実オブジェクトを束縛してから適用する。

### class `Entry`

```csharp
public class Entry
{
    public enum Type { Move, Remove, Insert }
    public Type tag;
    public IVTree vTree;
    public int index;
    public object data;
    public Entry(Type tag, IVTree vTree, int index);
}
```

キー付き差分の帳簿。削除と挿入の両方で見つかったキーは `Move` に昇格し、適用側は既存のホストオブジェクトを付け替える。

## 名前空間 `Veauty.VTree`

### interface `ITypedNode`

```csharp
public interface ITypedNode
{
    System.Type GetComponentType();
}
```

`Node<T,U>` / `KeyedNode<T,U>` が実装。レンダー間でコンポーネント型が変わると `Attach<T>` パッチが発行される。

### abstract class `NodeBase<T>` : `IVTree`, `IParent`

```csharp
public abstract class NodeBase<T> : IVTree, IParent
{
    public readonly string tag;
    public readonly Attributes<T> attrs;
    protected NodeBase(string tag, IEnumerable<IAttribute<T>> attrs);
    public virtual VTreeType GetNodeType();          // VTreeType.Node
    public abstract int GetDescendantsCount();
    public abstract IVTree[] GetKids();
    // protected ヘルパー: AttributesEqual, CombineHash, GetAttributesHashCode, GetKidsHashCode
}
```

### class `BaseNode<T>` : `NodeBase<T>`

```csharp
public class BaseNode<T> : NodeBase<T>
{
    public readonly IVTree[] kids;
    protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
    public override int GetDescendantsCount();       // 事前計算
    public override IVTree[] GetKids();
    public override bool Equals(object obj);         // 構造的
    public override int GetHashCode();
}
```

通常ノード。子は位置ベースで差分される。子孫数はコンストラクタで事前計算。

### class `Node<T>` : `BaseNode<T>` / class `Node<T, U>` : `BaseNode<T>`, `ITypedNode`

```csharp
public class Node<T> : BaseNode<T>
{
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
}

public class Node<T, U> : BaseNode<T>, ITypedNode
{
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
    public Type GetComponentType();                  // typeof(U)
}
```

### class `BaseKeyedNode<T>` : `NodeBase<T>`

```csharp
public class BaseKeyedNode<T> : NodeBase<T>
{
    public readonly (string, IVTree)[] kids;
    protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids);
    protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids);
    public override VTreeType GetNodeType();         // VTreeType.KeyedNode
    public override int GetDescendantsCount();
    public override IVTree[] GetKids();              // キーを外した子
    public override bool Equals(object obj);
    public override int GetHashCode();
}
```

キー付きノード。子はキーで差分される。キーは親の中で安定かつ一意であること ([差分検出](diffing.md) 参照)。

### class `KeyedNode<T>` / class `KeyedNode<T, U>`

```csharp
public class KeyedNode<T> : BaseKeyedNode<T>
{
    public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids);
}

public class KeyedNode<T, U> : BaseKeyedNode<T>, ITypedNode
{
    public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids);
    public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids);
    public Type GetComponentType();
}
```

注意: `KeyedNode<T>` の唯一のコンストラクタは `IAttribute<T>[]` (配列) を取る。他のノード型は `IEnumerable` を受け付ける。

### interface `IHostLifecycle<T>`

```csharp
public interface IHostLifecycle<T>
{
    T Init(T obj);                 // ホスト生成後、属性・子の適用前
    void AfterRenderKids(T obj);   // 全子要素のレンダー後
    void Destroy(T obj);           // ホスト破棄前
}
```

ノード単位の命令的ライフサイクルコールバック (削除された `Widget<T>` の後継)。レンダラーパッケージが呼び出す。

### interface `IHostLifecycleTree<T>`

```csharp
public interface IHostLifecycleTree<T>
{
    IHostLifecycle<T>[] GetHostLifecycles();
}
```

### class `HostLifecycleNode<T>` / `HostLifecycleNode<T, U>` / `HostLifecycleKeyedNode<T>` / `HostLifecycleKeyedNode<T, U>`

```csharp
public class HostLifecycleNode<T> : Node<T>, IHostLifecycleTree<T>
{
    public HostLifecycleNode(string tag, IEnumerable<IAttribute<T>> attrs,
        IEnumerable<IHostLifecycle<T>> lifecycles, params IVTree[] kids);
    public HostLifecycleNode(string tag, IEnumerable<IAttribute<T>> attrs,
        IEnumerable<IHostLifecycle<T>> lifecycles, IEnumerable<IVTree> kids);
    public IHostLifecycle<T>[] GetHostLifecycles();
}
// HostLifecycleNode<T,U> : Node<T,U>; HostLifecycleKeyedNode<T> : KeyedNode<T>;
// HostLifecycleKeyedNode<T,U> : KeyedNode<T,U> — 同じ形で、keyed 系は (string, IVTree) の kids を取る。
```

`lifecycles` 引数の `null` は空として扱われる。`HookRuntime.Resolve` はこれらのノードを (resolve 済みの子で再構築して) 保持する。

### delegate `FunctionComponent` / `FunctionComponent<TProps>`

```csharp
public delegate IVTree FunctionComponent();
public delegate IVTree FunctionComponent<in TProps>(TProps props);
```

### class `FunctionComponentNode` : `IVTree`

```csharp
public class FunctionComponentNode : IVTree
{
    public readonly string key;
    public readonly Delegate component;
    public readonly object props;

    public FunctionComponentNode(FunctionComponent component);
    public FunctionComponentNode(string key, FunctionComponent component);
    public FunctionComponentNode(FunctionComponent component, string key);
    public FunctionComponentNode(FunctionComponent<object> component, object props);
    public FunctionComponentNode(string key, FunctionComponent<object> component, object props);

    public VTreeType GetNodeType();       // VTreeType.FunctionComponent
    public int GetDescendantsCount();     // 常に 0
    public override bool Equals(object obj);   // key + component + props
    public override int GetHashCode();
}
```

プレースホルダーノード。diff の前に `HookRuntime.Resolve` で展開必須。コンストラクタは component が null なら `ArgumentNullException` を投げる。

### static class `FunctionComponents`

```csharp
public static class FunctionComponents
{
    public static FunctionComponentNode Create(FunctionComponent component);
    public static FunctionComponentNode Create(string key, FunctionComponent component);
    public static FunctionComponentNode Keyed(string key, FunctionComponent component);
    public static FunctionComponentNode Create<TProps>(FunctionComponent<TProps> component, TProps props);
    public static FunctionComponentNode Create<TProps>(string key, FunctionComponent<TProps> component, TProps props);
    public static FunctionComponentNode Keyed<TProps>(string key, FunctionComponent<TProps> component, TProps props);
}
```

`Keyed` はキー付き `Create` オーバーロードのエイリアス。`[Component]` で書き換えられたメソッドはキーなしの `Create` を呼ぶ。

## 名前空間 `Veauty.Patch`

すべてのパッチクラスは `IPatch<T>` (`GetTarget` / `SetTarget` / `GetIndex`) と構造的 `Equals` / `GetHashCode` を実装します。以下ではそれらのメンバーを省略します。

### class `Redraw<T>`

```csharp
public class Redraw<T> : IPatch<T>
{
    public int index;
    public IVTree vTree;    // 置き換えるサブツリー
    public T target;
    public Redraw(int index, IVTree vTree);
}
```

ノードを丸ごと置き換える (ノード種別または tag の不一致で発行)。

### class `Attach<T>`

```csharp
public class Attach<T> : IPatch<T>
{
    public readonly System.Type oldComponent;
    public readonly System.Type newComponent;
    public Attach(int index, System.Type oldComponent, System.Type newComponent);
}
```

型付きコンポーネントの入れ替え (`Node<T,U>` の `U` が変化)。

### class `Attrs<T>`

```csharp
public class Attrs<T> : IPatch<T>
{
    public int index;
    public T target;
    public Dictionary<string, IAttribute<T>> attrs;  // 値が null = 属性の削除
    public Attrs(int index, Dictionary<string, IAttribute<T>> attrs);
}
```

### class `Append<T>`

```csharp
public class Append<T> : IPatch<T>
{
    public readonly int length;     // 旧の子の数。新しい子はここから始まる
    public readonly IVTree[] kids;  // 新しい子の全リスト
    public Append(int index, int length, IVTree[] kids);
}
```

### class `RemoveLast<T>`

```csharp
public class RemoveLast<T> : IPatch<T>
{
    public readonly int length;  // 新しい (残る) 子の数
    public readonly int diff;    // 削除する末尾の子の数
    public RemoveLast(int index, int length, int diff);
}
```

### class `Remove<T>`

```csharp
public class Remove<T> : IPatch<T>
{
    public IPatch<T>[] patches;  // キー付き move の一部なら非 null
    public Entry entry;          // キー付き move の一部なら非 null
    public Remove(int index, IPatch<T>[] patches = null, Entry entry = null);
}
```

キー付き子の単純削除、または `entry`/`patches` がセットされている場合は move の「デタッチ」側。

### class `Reorder<T>` (+ ネストした `Reorder<T>.Insert`)

```csharp
public class Reorder<T> : IPatch<T>
{
    public readonly IPatch<T>[] patches;    // Remove を含むサブパッチ
    public readonly Insert[] inserts;       // 位置指定の挿入/move
    public readonly Insert[] endInserts;    // 末尾への挿入 (entry.index == -1)
    public Reorder(int index, IPatch<T>[] patches, Insert[] inserts, Insert[] endInserts);

    public class Insert
    {
        public int index;    // 新しい子リスト内の位置
        public Entry entry;  // 挿入内容。Entry.Type.Move は既存オブジェクトの付け替え
    }
}
```

子が変化したキー付き親ごとに 1 つ。マッチングのヒューリスティクスは [差分検出](diffing.md) 参照。
