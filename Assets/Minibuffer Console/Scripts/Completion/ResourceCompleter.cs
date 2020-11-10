/*
  Copyright (c) 2016 Seawisp Hunter, LLC

  Author: Shane Celis
*/
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SeawispHunter.MinibufferConsole.Extensions;

namespace SeawispHunter.MinibufferConsole {
/** \cond HIDE */
public class ResourceCompleter : ICompleter, ICache, ICoercer {
  private bool _showHidden = false;
  [Variable("show-hidden", description = "Show hidden resources")]
  public bool showHidden {
    get { return _showHidden; }
    set {
      if (_showHidden != value)
        ResetCache();
      _showHidden = value;
    }
  }
  protected System.Type type;

  public System.Type defaultType { get { return type; } }

  private Dictionary<string, Object> _resources = null;
  private HashSet<string> namesThatCollided = new HashSet<string>();
  protected Dictionary<string, Object> resources {
    get {
      if (_resources == null) {
        _resources = new Dictionary<string, Object>();
        foreach (var resource in Resources.FindObjectsOfTypeAll(this.type)) {
        // foreach (var resource in Object.FindObjectsOfType(this.type)) {
          if (! FilterResource(resource))
            continue;

          var name = resourceName(resource);
          if (namesThatCollided.Contains(name)) {
            _resources[resourceNameUnique(resource)] = resource;
          } else if (_resources.ContainsKey(name)) {
            var oldResource = _resources[name];
            // Rename the previous resource.
            _resources[resourceNameUnique(oldResource)] = oldResource;
            _resources.Remove(name);
            // Add the new resource.
            _resources[resourceNameUnique(resource)] = resource;
            namesThatCollided.Add(name);
          } else {
            _resources[name] = resource;
          }
        }
      }
      return _resources;
    }
  }

  protected virtual string resourceName(Object resource) {
    if (type.Implements(typeof(Component)))
      return string.Format("{0}/{1}",
                           resource.name,
                           resource.GetType().PrettyName());
    else
      return resource.name;
  }

  static int count = 0;
  private string resourceNameUnique(Object resource) {
    return string.Format("{0}-{1}",
                         resourceName(resource),
                         // resource.GetInstanceID()
                         count++.ToString("D3")
                         );
  }

  /*
    Returns true to keep resource, false otherwise.
   */
  protected virtual bool FilterResource(Object resource) {
    if (! showHidden) {
      if (resource.hideFlags != HideFlags.None)
        return false;
    }
    return true;
  }

  private bool IsPrefab(Object o) {
    return false;
    // http://answers.unity3d.com/questions/218429/how-to-know-if-a-gameobject-is-a-prefab.html
    // bool isPrefabInstance = PrefabUtility.GetPrefabParent(gameObject) != null && PrefabUtility.GetPrefabObject(gameObject.transform) != null;
    // bool isPrefabOriginal = PrefabUtility.GetPrefabParent(gameObject) == null && PrefabUtility.GetPrefabObject(gameObject.transform) != null;
    // bool isDisconnectedPrefabInstance = PrefabUtility.GetPrefabParent(gameObject) != null && PrefabUtility.GetPrefabObject(gameObject.transform) == null;
    //return PrefabUtility.GetPrefabParent(o) == null;
  }

  /**
     Specify the type that Unity will try to find instances of.

     \note This only works with a subset of Unity types.
   */
  public ResourceCompleter(System.Type type) {
    this.type = type;
  }

  /*
    Return a list of matching strings for the given input.
   */
  public IEnumerable<string> Complete(string input) {
    // return resources
    //   .Select(x => x.name)
    //   .Where(y => y.StartsWith(input, caseInsensitive, null));
    return Matcher.Match(input, resources
                         //.Where(x => FilterResource(x.Value))
                         .Select(x => x.Key));
  }

  public object Coerce(string input, System.Type t) {
    if (! t.IsAssignableFrom(type))
      throw new CoercionException(type, t);
    // if (caseInsensitive)
    //   return resources.Where(x => string.Equals(x.name, input, System.StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    // else
    //   return resources.Where(x => x.name == input).FirstOrDefault();

    // XXX case sensitive only for now.
    if (resources.ContainsKey(input))
      return resources[input];
    else
      return null;
  }

  public void ResetCache() {
    _resources = null;
    namesThatCollided.Clear();
  }
}

/** \endcond */

/**
  Tab complete any [Unity resource type](https://docs.unity3d.com/ScriptReference/Resources.FindObjectsOfTypeAll.html).

  Works with the following classes: AnimationClip, AudioClip,
  Font, GUISkin, Material, Mesh, PhysicMaterial, Shader, Sprite,
  Texture, GameObject, Component, and more.

  These completers will be generated dynamically when any
  UnityEngine.Object is requested.  Meaning you can complete any
  scripts that inherit from MonoBehaviour without any setup.

  \note If multiple completion objects share the same name, it will append a
  distinguishing number to the others; however, there is no guarantee of order.

  \class ResourceCompleter \implements ICompleter
 */
public class ResourceCompleter<T> : ResourceCompleter, ICompleter {
  /**
     Provide the Unity type you wish to complete.  Uses
     [Resources.FindObjectsOfTypeAll()](https://docs.unity3d.com/ScriptReference/Resources.FindObjectsOfTypeAll.html)
     behind the scenes.  Works with user created scripts too!
   */
  public ResourceCompleter() : base(typeof(T)) {
  }
}

public class BehaviourCompleter : ResourceCompleter {

  private bool _onlyEnabled = false;
  [Variable("only-enabled", description = "Only show enabled MonoBehaviours")]
  public bool onlyEnabled {
    get { return _onlyEnabled; }
    set {
      if (_onlyEnabled != value)
        ResetCache();
      _onlyEnabled = value;
    }
  }

  public BehaviourCompleter(System.Type type) : base(type) {
  }

  /*
    Returns true to keep resource, false otherwise.
   */
  protected override bool FilterResource(Object resource) {
    if (onlyEnabled) {
      var component = resource as Behaviour;
      if (component != null && !component.enabled)
        return false;
    }
    return base.FilterResource(resource);
  }

}

// public class BehaviourCompleter<T> : BehaviourCompleter
//   where T : Behaviour {
//   public BehaviourCompleter() : base(typeof(T)) {
//   }
// }
}
