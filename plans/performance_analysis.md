# Performance Analysis (`AttachAttributesEditor.cs`)

This document outlines the performance analysis of the editor script.

## Performance Considerations

1.  **`OnGUI` Performance:**
    *   The `OnGUI` method in `AttachAttributePropertyDrawer` calls `ShouldUpdateProperty` on every repaint. This method performs a dictionary lookup. While dictionary lookups are fast, `OnGUI` can be called multiple times per frame, and this could add up, especially with many decorated fields.
    *   The expensive operations are deferred with `EditorApplication.delayCall`, which is good. This prevents the editor from freezing during GUI layout and rendering.

2.  **`Find` Operations:**
    *   The script frequently uses `FindGameObject...` methods, which often rely on `GetComponentsInChildren<Transform>` and then perform a LINQ `Where` clause. This can be slow in very deep or wide hierarchies.
    *   `FindGameObjectInScene` and `FindGameObjectsInScene` iterate over all root objects and then all their children. This is a very expensive operation and should be avoided if possible, especially inside `OnGUI` (even if deferred).

3.  **Caching:**
    *   The `s_FailedLookups` cache is a good idea to prevent repeated expensive searches for objects that don't exist. The 2-second retry interval is a reasonable default.
    *   The `s_ComponentTypeCache` is excellent. It uses `TypeCache.GetTypesDerivedFrom<Component>()`, which is much faster than the legacy reflection approach.
    *   The `s_PropertyDrawers` cache, as mentioned in the bug analysis, is a memory leak and its keying strategy is flawed.

4.  **Reflection:**
    *   The use of `TypeCache` is a significant performance improvement over the old `AppDomain.CurrentDomain.GetAssemblies()` method. `GetAllTypesLegacy` is correctly retained only as a fallback.
    *   The reflection used to get the type from `CustomPropertyDrawer` (`s_CustomPropertyDrawerTypeField`, `s_CustomPropertyDrawerUseForChildrenField`) is done only when needed and the results are not cached, but it's a minor point.

5.  **LINQ Usage:**
    *   There are several places where LINQ is used (`FirstOrDefault`, `Where`, `Select`). While convenient, in performance-critical code like editor GUI, it can be slower than manual iteration and can generate garbage. For example, in `FindGameObjectsByName`, the `ToList()` call allocates a new list.