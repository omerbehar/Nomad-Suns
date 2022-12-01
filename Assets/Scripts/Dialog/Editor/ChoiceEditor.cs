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
        private string selectedChoice;
        private DialogNode parentNode;
        private int outerChoiceIndex;
        private int innerChoiceIndex;
        private bool focused;

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

        private void OnGUI()
        {
            if (selectedChoice == null)
            {
                if (GUILayout.Button("Create new choice"))
                {
                    string newChoice = "";
                    //newChoice.name = Guid.NewGuid().ToString();
                    selectedChoice = newChoice;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Choice selected:");
                GUI.SetNextControlName("text");
                selectedChoice = EditorGUILayout.TextArea(selectedChoice, GUILayout.Height(120));
                if (!focused)
                {
                    GUI.FocusControl("text");
                    TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    textEditor.cursorIndex = 1;
                    focused = true;
                }
                if (outerChoiceIndex != -1)
                {
                    parentNode.GetOuterChoiceAtIndex(outerChoiceIndex).SetInnerChoiceAtIndex(innerChoiceIndex, selectedChoice);
                }
                else
                {
                    parentNode.SetHeader(selectedChoice);
                }
            }
            if (GUILayout.Button("Close window"))
            {
                Close();
            }
        }

        public void Init(DialogNode node, int outerChoiceIndex, int innerChoiceIndex, string choice)
        {
            parentNode = node;
            this.outerChoiceIndex = outerChoiceIndex;
            this.innerChoiceIndex = innerChoiceIndex;
            selectedChoice = choice;
        }
        public void Init(DialogNode node, string header)
        {
            parentNode = node;
            selectedChoice = header;
            outerChoiceIndex = -1;
            innerChoiceIndex = -1;
        }
    }
}
