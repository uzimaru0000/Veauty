# Diffing

[日本語](ja/diffing.md)

The diff algorithm in `Diff<T>.Calc`, every patch type, the pre-order index contract, and how keyed children are matched.

## Entry point

```csharp
IPatch<T>[] patches = Diff<T>.Calc(oldTree, newTree);
```

Both trees must be **resolved** (no `FunctionComponentNode` anywhere — see [Function components](function-components.md)); otherwise `InvalidOperationException` is thrown. `IVTreeWrapper`s are unwrapped before comparison. An unknown `IVTree` implementation throws `ArgumentException`.

## Node comparison order

For a pair of nodes `x` (old) and `y` (new):

1. **Plain vs keyed mismatch** — if one side is a `BaseNode<T>` and the other a `BaseKeyedNode<T>`, the keyed side is *de-keyed* (converted to an equivalent plain node, preserving typed component and host lifecycles) and diffing continues positionally. No redraw.
2. **Node type mismatch** (any remaining `GetNodeType()` difference) → `Redraw`.
3. **Tag mismatch** → `Redraw`. Nothing below the node is examined.
4. **Typed component** — if both are `ITypedNode` and `GetComponentType()` differs → `Attach(old, new)` patch (children/attrs are still diffed; the node is not redrawn).
5. **Attributes** — key-wise comparison of the attribute dictionaries:
   - key in old only → entry `key -> null` (removal),
   - key in both but `!old.Equals(new)` → entry `key -> newAttr`,
   - key in new only → entry `key -> newAttr`.
   A non-empty result becomes one `Attrs` patch. Attribute equality is `Attribute<T,U>`'s key + `object.Equals(value)` — attributes holding values without proper `Equals` will always look changed.
6. **Children** — positionally for plain nodes, by key for keyed nodes (below).

## The pre-order index contract

Patches address nodes with an integer index over the **old** tree in pre-order:

```
index(root) = 0
index(first child) = parent + 1
index(next sibling) = previous sibling + 1 + previousSibling.GetDescendantsCount()
```

`GetDescendantsCount()` is precomputed in node constructors as `kids.Length + Σ kid.GetDescendantsCount()`. `FunctionComponentNode.GetDescendantsCount()` is `0` — a placeholder occupies a single slot until resolved (resolved trees never contain them anyway).

The patch applicator in the renderer package walks the real object hierarchy with the *same* numbering to find each patch's target and calls `SetTarget`. This is why:

- patches must be applied to the exact host tree that corresponds to `oldTree`;
- internal host children that are invisible to the VTree must be skipped by the applicator (the GameObject package handles this with a child offset).

### Positional child diff (`DiffKids`)

For plain nodes with old length `m`, new length `n`:

- `m > n` → one `RemoveLast(index, n, m - n)` (drop trailing children).
- `m < n` → one `Append(index, m, newKids)` (render `newKids[m..]`).
- Then the first `min(m, n)` pairs are recursed into, advancing the index by `1 + oldKid.GetDescendantsCount()` per child.

Positional diffing means *inserting at the front of an unkeyed list rewrites every element*. Use keyed nodes for dynamic lists.

## Keyed child diff (`DiffKeyedKids`)

Children of `KeyedNode` are `(string key, IVTree)` pairs. The algorithm walks both lists with two cursors and a one-element look-ahead, classifying each step:

| Case | Condition | Action |
|------|-----------|--------|
| match | `xKey == yKey` | recurse, advance both |
| swap | `xKey == yNextKey && yKey == xNextKey` | diff `x` against `yNext`, insert `yKey`, remove old `xNext`; advance both by 2 |
| insert | `xKey == yNextKey` | insert `yKey` at this position, diff `x` against `yNext`; advance x by 1, y by 2 |
| remove | `yKey == xNextKey` | remove `xKey`, diff `xNext` against `y`; advance x by 2, y by 1 |
| replace | `xNextKey == yNextKey` | remove `xKey`, insert `yKey`, diff the nexts; advance both by 2 |
| bail | none of the above | stop the scan; remaining old keys are removed, remaining new keys become *end inserts* |

Removal + insertion of the **same key** is fused into a **move**: the `Entry` bookkeeping (in `IPatch.cs`) flips the entry to `Entry.Type.Move`, diffs the old vs new subtree, and attaches the sub-patches to the pending `Remove` patch, whose applicator then detaches and re-inserts the existing host object instead of destroying it.

If anything changed, a single `Reorder(index, subPatches, inserts, endInserts)` patch is emitted for the parent.

### Why keys must be stable and unique

- **Stable:** identity across renders is *only* the key string. A key that changes (e.g. derived from the item's list position) turns a move into a remove + create, destroying host state and unmounting any function components inside.
- **Unique:** when a duplicate key is seen, the algorithm silently disambiguates it by recursing with `key + "UniVDOM"` (and `"UniVDOMUniVDOM"`, ...). The sentinel-suffixed entries can no longer pair up with the real key, so duplicates degrade into remove/insert churn — and if a user key literally ends with `"UniVDOM"` it can collide with the sentinel. Never rely on duplicate keys.

## Patch type reference

| Patch | Emitted when | Payload |
|-------|--------------|---------|
| `Redraw<T>` | node type or tag differs | `vTree` — the replacement subtree |
| `Attach<T>` | `ITypedNode` component type differs | `oldComponent`, `newComponent` types |
| `Attrs<T>` | attribute dictionary differs | `attrs: key -> newAttr` (`null` = remove) |
| `Append<T>` | new node has more children | `length` (old count), `kids` (full new list) |
| `RemoveLast<T>` | new node has fewer children | `length` (new count), `diff` (how many to drop) |
| `Remove<T>` | keyed child removed (or move-detach) | optional `patches` + `entry` when part of a move |
| `Reorder<T>` | keyed children changed | `patches`, `inserts`, `endInserts` (`Insert.index == -1` for end inserts inside their entries) |

All patch types implement `IPatch<T>` (`GetIndex`, `GetTarget`/`SetTarget`) and structural `Equals`/`GetHashCode`.

See also: [Architecture](architecture.md), [API reference](api-reference.md).
