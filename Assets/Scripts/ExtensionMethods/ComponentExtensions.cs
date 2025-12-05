using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.ExtensionMethods
{
    public static class ComponentExtensions
    {
        public static bool Is<T>(this Component component) where T : Component {
          return component.GetComponent<T>() != null;   
        } 
        
        public static T As<T>(this Component component) where T : Component {
            return component.GetComponent<T>();   
        }
    }
}