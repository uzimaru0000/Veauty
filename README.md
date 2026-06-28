# Veauty

Implementation of Virtual-Dom for Unity3D

## Environment

- Unity3D 2020.3 or higher
- C# 7.0

## Install

Add line in `Packages/manifest.json`

```json
{
  "dependencies": {
    ...
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git",
    ...
  }
}
```

## Example

- [Veauty-GameObject](https://github.com/uzimaru0000/Veauty-GameObject)

## Patch semantics

`Diff<T>.Calc(oldTree, newTree)` returns patches indexed by preorder traversal. The root node is index `0`; each child increments the index by one, then reserves its descendant count.

- `Redraw(index, vTree)` replaces the node at `index` with `vTree`.
- `Attrs(index, attrs)` applies changed attributes to the node at `index`. A key with `null` removes that attribute.
- `Attach(index, oldComponent, newComponent)` changes the typed component attached to a typed node.
- `Append(index, length, kids)` appends `kids[length..]` to the node at `index`.
- `RemoveLast(index, length, diff)` removes `diff` children starting at child position `length`.
- `Remove(index)` removes the node at `index`.
- `Remove(index, patches, entry)` detaches the node at `index`, applies `patches`, and stores it for a keyed move represented by `entry`.
- `Reorder(index, patches, inserts, endInserts)` applies keyed child changes under the node at `index`. First apply `patches`, then place `inserts` at their sibling indexes, then append `endInserts`.

`Entry.Type.Insert` means render `entry.vTree`. `Entry.Type.Move` means reuse the detached node associated with the same `Entry`. `Entry.Type.Remove` is an intermediate diff state and should not appear as an insert target. Keyed nodes are intended to use unique keys among siblings; duplicate keys are treated as distinct fallback entries only when a conflict is detected.

I'm happy if you contribute！

## Development
1. Make your project and make directory Packages in it.
2. In Packages, git clone https://github.com/uzimaru0000/Veauty.git

## Contribution

- Fork it
- Create your feature branch
- Commit your changes
- Push to the branch
- Create new Pull Request
