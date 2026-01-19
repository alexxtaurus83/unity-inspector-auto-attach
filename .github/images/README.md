# Unity Auto Attach Component via Attributes

A Unity editor tool that simplifies assigning component references in the Inspector by using attributes. Instead of dragging and dropping references, decorate your fields with attributes, and the tool will automatically find and assign the appropriate components based on the attribute's rules.

**Forum Thread:** https://forum.unity.com/threads/auto-attach-components-via-attributes.928098/

## Installation

Add this as a package to your project by adding the below as an entry to dependencies in `/Packages/manifest.json` file:

```json
"nrjwolf.games.attachattributes": "https://github.com/Nrjwolf/unity-auto-attach-component-attributes.git"
```

For more information on adding git repositories as a package see the [Git support on Package Manager](https://docs.unity3d.com/Manual/upm-git.html) in the Unity Documentation.

Alternatively, you can simply place the `Scripts` folder into your `Assets` folder or add it as a Git submodule.

## Preview Video

[![Play](https://img.youtube.com/vi/LdiJdgHrBl4/0.jpg)](https://www.youtube.com/watch?v=LdiJdgHrBl4)

## Features

- **Automatic Component Assignment:** Automatically find and assign components based on name, path, or parent/child relationships.
- **Multiple Search Strategies:** Search for components on the same GameObject, children, parents, or by path.
- **Array Population:** Populate arrays or lists with components from multiple GameObjects.
- **Prefab Loading:** Load and assign prefab assets directly to fields.
- **Path Copy Helper:** Copy hierarchy paths to easily set up path-based attributes.

## Attributes

### `GetComponent`
Automatically attaches a component from the same GameObject as the MonoBehavior script.

```csharp
using AttachAttributes;
using UnityEngine;

public class ExampleComponent : MonoBehaviour
{
    [GetComponent] // Finds Rigidbody on the same GameObject as ExampleComponent
    public Rigidbody myRigidbody;
}
```

### `GetComponentInChildren`
Automatically attaches a component from a child GameObject. If a name is provided, it searches for a child GameObject with that name.

```csharp
using AttachAttributes;
using UnityEngine;

public class ExampleComponent : MonoBehaviour
{
    [GetComponentInChildren] // Finds Renderer on any child of this GameObject
    public Renderer childRenderer;

    [GetComponentInChildren("SpecificChildName")] // Finds Transform on a child named "SpecificChildName"
    public Transform namedChildTransform;
}
```

### `GetComponentInParent`
Automatically attaches a component from a parent GameObject. If a name is provided, it searches for a parent GameObject with that name.

```csharp
using AttachAttributes;
using UnityEngine;

public class ChildExample : MonoBehaviour
{
    [GetComponentInParent] // Finds ParentScript on a parent GameObject
    public ParentScript parentScript;

    [GetComponentInParent("SpecificParentName")] // Finds Transform on a parent named "SpecificParentName"
    public Transform namedParentTransform;
}
```

### `GetComponentByPath`
Automatically attaches a component by specifying a path relative to the current GameObject. This attribute **only** performs a search if the path contains a `/`. If the path does not contain a `/`, the tool does nothing.

```csharp
using AttachAttributes;
using UnityEngine;

public class ExampleComponent : MonoBehaviour
{
    // Finds a component on the grandchild: "ChildObject/GrandchildObject/Collider"
    [GetComponentByPath("ChildObject/GrandchildObject")]
    public Collider pathCollider;
}
```

### `GetComponentsInChildren`
Populates an array or List with components from child GameObjects. If names are provided, it searches for children with those names.

```csharp
using AttachAttributes;
using UnityEngine;
using System.Collections.Generic;

public class ExampleComponent : MonoBehaviour
{
    [SerializeField] private List<Renderer> childRenderersList; // This field will be populated

    [GetComponentsInChildren] // Populates childRenderersList with Renderers from any child
    private int _dummy; // Dummy field to trigger the attribute

    [SerializeField] private Renderer[] namedChildRenderersArray; // This field will be populated

    [GetComponentsInChildren(new string[] { "Child1", "Child2" })] // Populates namedChildRenderersArray with Renderers from "Child1" and "Child2"
    private int _dummy2; // Dummy field to trigger the attribute
}
```

### `GetComponentsInParent`
Populates an array or List with components from parent GameObjects.

```csharp
using AttachAttributes;
using UnityEngine;
using System.Collections.Generic;

public class ChildExample : MonoBehaviour
{
    [SerializeField] private List<ParentScript> parentScriptsList; // This field will be populated

    [GetComponentsInParent] // Populates parentScriptsList with ParentScript components from any parent
    private int _dummy; // Dummy field to trigger the attribute
}
```

### `GetComponentsByPath`
Populates an array or List with components found by specifying paths relative to the current GameObject. This attribute **only** performs a search if a path contains a `/`. If the path does not contain a `/`, the tool does nothing.

```csharp
using AttachAttributes;
using UnityEngine;
using System.Collections.Generic;

public class ExampleComponent : MonoBehaviour
{
    [SerializeField] private List<Collider> pathCollidersList; // This field will be populated

    [GetComponentsByPath(new string[] { "Child1/ColliderObject", "Child2/ColliderObject" })] // Populates pathCollidersList
    private int _dummy; // Dummy field to trigger the attribute
}
```

### `AddComponent`
Automatically adds a component to the same GameObject if it doesn't already exist and assigns it to the field.

```csharp
using AttachAttributes;
using UnityEngine;

public class ExampleComponent : MonoBehaviour
{
    [AddComponent] // Adds a new Rigidbody to this GameObject if one doesn't exist
    public Rigidbody newRigidbody;
}
```

### `AddComponentAtParent`
Automatically adds a component to the parent GameObject if it doesn't already exist and assigns it to the field.

```csharp
using AttachAttributes;
using UnityEngine;

public class ChildExample : MonoBehaviour
{
    [AddComponentAtParent] // Adds a new ParentScript to the immediate parent GameObject if one doesn't exist
    public ParentScript parentScript;
}
```

### `GetPrefab`
Loads a prefab asset and assigns it to the field.

```csharp
using AttachAttributes;
using UnityEngine;

public class ExampleComponent : MonoBehaviour
{
    [GetPrefab("Assets/Prefabs/MyPrefab.prefab")] // Loads the specified prefab asset
    public GameObject myPrefab;
}
```

## Editor Tools

### Copy Hierarchy Path

The tool includes a helpful context menu option to copy the hierarchy path of any GameObject, making it easier to set up path-based attributes like `GetComponentByPath` and `GetComponentsByPath`.

**To use this feature:**

1. Select a GameObject in the Hierarchy window
2. Right-click and choose **"Copy Hierarchy Path"** from the context menu
3. The path will be copied to your clipboard in the format: `/ParentName/ChildName/GrandchildName`
4. Paste this path directly into your `GetComponentByPath` or `GetComponentsByPath` attributes

This eliminates the need to manually type complex hierarchy paths and reduces errors when setting up path-based component references.

## Usage Example

```csharp
using AttachAttributes;

[GetComponent] 
[SerializeField] private Camera m_Camera;
 
[GetComponentInChildren(true)] // include inactive
[SerializeField] private Button m_Button;

[GetComponentInChildren("Buttons/Button1")] // Get component from children by path "Buttons/Button1" in hierarchy
[SerializeField] private Button m_Button;
 
[AddComponent] // Add component in editor and attach it to field
[SerializeField] private SpringJoint2D m_SpringJoint2D;
 
[GetComponentInParent] // Get component from parent
[SerializeField] private Canvas m_Canvas;
```

Now all components will automatically attach when you select your GameObject in the hierarchy.

![Global Setting](https://github.com/Nrjwolf/unity-auto-attach-component-attributes/blob/master/.github/images/globalSettingInContextMenu.png "Global active/deactive")

You can turn it on/off in the component context menu or via `Tools/Evial/AttachAttributes`.

## New Features

### Force Re-attach Attributes Context Menu

The tool now includes a context menu option that allows you to manually force all `Attach` attributes on a component to re-run their logic, even if fields are already filled. This is particularly useful when you've made changes to your scene hierarchy or when you want to refresh attached references without changing the script or entering play mode.

**To use this feature:**

1. Right-click on a component in the Inspector that has fields with Attach attributes (such as `[GetComponent]`, `[GetComponentInChildren]`, etc.)
2. Select **"Force Re-attach Attributes"** from the context menu
3. The tool will iterate through all fields with Attach attributes on that component and re-execute their attachment logic
4. All applicable fields will be refreshed with the current state of the scene, regardless of whether they were previously filled

This is especially helpful when you've made structural changes to your GameObject hierarchy that affect the attached components, or when you want to quickly refresh all references without recompiling your scripts.

## Notes

- **Multi-Object Editing:** Currently, this tool does not support multi-object editing in the Inspector. The attributes will only work when a single GameObject is selected.
- **Play Mode:** The automatic attachment only occurs in the Editor when not in Play Mode.
- **Performance:** The tool includes caching mechanisms to prevent repeated expensive searches for components that are not found. Failed lookups are retried periodically.
- **Visual Feedback:** If a search fails, a subtle visual cue (like a warning icon) may appear in the Inspector, depending on the Unity version and settings.

## About

This asset helps you to auto attach components into your serialized fields in the Inspector. I started using it to avoid assigning components in `Awake/Start` functions every time.

So, you might ask why I need it? Well, maybe you use code like this and don't know that this is bad for performance:

```csharp
private Transform m_CachedTransform
public Transform transform
{
  get
  {
    if (m_CachedTransform == null)
      m_CachedTransform = InternalGetTransform();
    return m_CachedTransform;
  }
}
```

You can read more about this here: https://blogs.unity3d.com/ru/2014/05/16/custom-operator-should-we-keep-it/

---

> **Telegram:** https://t.me/nrjwolf_games  
> **Discord:** https://discord.gg/jwPVsat  
> **Reddit:** https://www.reddit.com/r/Nrjwolf/  
> **Twitter:** https://twitter.com/nrjwolf