using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;

namespace Nomad.Dialog.Editor
{
    public class ChoiceEditor : EditorWindow
    {
        private Choice selectedChoice;
        private DialogNode parentNode;
        private int outerChoiceIndex;
        private int innerChoiceIndex;

        public static void ShowEditorWindow()
        {
            GetWindow(typeof(ChoiceEditor), false, "Choice Editor");
        }
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Choice choice = EditorUtility.InstanceIDToObject(instanceID) as Choice;
            if (choice != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }
        private void OnSelectionChange()
        {
            Choice newChoice = Selection.activeObject as Choice;

            if (newChoice != null)
            {
                selectedChoice = newChoice;
                Repaint();
            }
        }
        private void OnGUI()
        {
            if (selectedChoice == null)
            {
                if (GUILayout.Button("Create new choice"))
                {
                    Choice newChoice = CreateInstance<Choice>();
                    newChoice.name = Guid.NewGuid().ToString();
                    selectedChoice = newChoice;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Choice selected:");
                selectedChoice.choiceText = EditorGUILayout.TextArea(selectedChoice.choiceText, GUILayout.Height(120));
                parentNode.GetOuterChoiceAtIndex(outerChoiceIndex).SetInnerChoiceAtIndex(innerChoiceIndex, selectedChoice);
            }
            if (GUILayout.Button("Close window"))
            {
                Close();
            }
        }

        public void Init(DialogNode node, int outerChoiceIndex, int innerChoiceIndex, Choice choice)
        {
            parentNode = node;
            this.outerChoiceIndex = outerChoiceIndex;
            this.innerChoiceIndex = innerChoiceIndex;
            selectedChoice = choice;
        }
    }
}
