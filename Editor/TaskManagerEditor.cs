using System.Collections;
using System.Collections.Generic;
using Samana.Tasks;
using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomEditor(typeof(TaskManager))]
public class TaskManagerEditor : Editor
{
    TaskManager _t;
    List<TaskQueue> _queues;
    private void OnEnable()
    {
        _t = (TaskManager)target;
        FieldInfo queuesField = _t.GetType().GetField("_queues", BindingFlags.NonPublic | BindingFlags.Instance);
        _queues = queuesField.GetValue(_t) as List<TaskQueue>;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Queues");
        EditorGUILayout.LabelField("Tasks count");
        EditorGUILayout.EndHorizontal();

        if (_queues != null)
        {
            for (int i = 0; i < _queues.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(_queues[i].name);
                EditorGUILayout.LabelField(_queues[i].tasks.Length.ToString());
                EditorGUILayout.EndHorizontal();
            }

            Repaint();
        }
    }
}
