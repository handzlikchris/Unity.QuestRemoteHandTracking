using System;
using System.Linq;
using System.Reflection;
using Assets.RemoteHandsTracking.Utilities;
using UnityEditor;
using UnityEngine;

namespace Assets.RemoteHandsTracking.Editor
{
    // Initial Concept by http://www.reddit.com/user/zaikman
    // Revised by http://www.reddit.com/user/quarkism

#if UNITY_EDITOR
#endif

#if UNITY_EDITOR
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class EditorButton : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var mono = target as MonoBehaviour;

            var methods = mono.GetType()
                .GetMembers(BindingFlags.Instance | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                            BindingFlags.NonPublic)
                .Where(o => Attribute.IsDefined((MemberInfo) o, typeof(EditorButtonAttribute)));

            foreach (var memberInfo in methods)
            {
                if (GUILayout.Button(memberInfo.Name))
                {
                    var method = memberInfo as MethodInfo;
                    method.Invoke(mono, null);
                }
            }
        }
    }
#endif
}
