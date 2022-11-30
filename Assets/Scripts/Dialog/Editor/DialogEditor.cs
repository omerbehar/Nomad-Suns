using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Nomad.Dialog.Editor
{
    public class DialogEditor : EditorWindow
    {
        Dialog selectedDialog = null;
        [NonSerialized]
        GUIStyle nodeStyle;
        [NonSerialized]
        private GUIStyle OuterChoicestyle;
        [NonSerialized]
        DialogNode draggedNode = null;
        [NonSerialized]
        Vector2 dragOffset;
        private bool isScrolling;
        [NonSerialized]
        private DialogNode creatingNode = null;
        [NonSerialized]
        private bool isLinked;
        //[NonSerialized]
        List<Rect> lastRects = new List<Rect>();
        [NonSerialized]
        List<Rect> lastEffectorBtnRects = new List<Rect>();
        [NonSerialized]
        private DialogNode nodeToDelete = null;
        [NonSerialized]
        private DialogNode linkingParentNode = null;
        private DialogNode.OuterChoice linkingOuterChoice = null;
        [NonSerialized]
        private DialogNode linkingChildNode = null;
        [NonSerialized]
        List<List<Rect>> outerChoiceRect = new List<List<Rect>>();
        [NonSerialized]
        List<List<Rect>> effectorRect = new List<List<Rect>>();
        private Vector2 scrollPosition;
        [NonSerialized]
        private Vector2 scrollOffset;

        private DialogNode.EffectorsAndEffects effectorToRemove = null;
        [NonSerialized]
        private int effectorAreaHeight = 85;
        [NonSerialized]
        private float rootNodeHeightOffset = 150;
        [NonSerialized]
        private float nodeHeightOffset = 210;
        [NonSerialized]
        private Effector costOrBenefitToRemove = null;
        [NonSerialized]
        private int costOrBenefitsHeight = 22;
        const float dialogWindowSize = 4000;
        const float backgroundSize = 50;
        [MenuItem("Window/Dialog Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditor), false, "Dialog Editor");
        }
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            Dialog dialog = EditorUtility.InstanceIDToObject(instanceID) as Dialog;
            if (dialog != null)
            {
                ShowEditorWindow();
                return true;
            }
            return false;
        }
        private void OnEnable()
        {
            nodeStyle = new GUIStyle();
            OuterChoicestyle = new GUIStyle();
            StyleNode();
            StyleOuterChoice();
            if (selectedDialog) SetRectLists();
        }
        private void StyleNode()
        {
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
        }
        private void StyleOuterChoice()
        {
            OuterChoicestyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            OuterChoicestyle.padding = new RectOffset(12, 12, 10, 10);
            OuterChoicestyle.border = new RectOffset(12, 12, 12, 12);
        }
        private void OnSelectionChange()
        {
            Dialog newDialog = Selection.activeObject as Dialog;
            if (newDialog != null)
            {
                selectedDialog = newDialog;
                SetRectLists();
                Repaint();
            }
        }
        private void SetRectLists()
        {
            outerChoiceRect.Clear();
            effectorRect.Clear();
            lastRects.Clear();
            int index = 0;
            foreach (DialogNode node in selectedDialog.GetAllNodes())
            {
                outerChoiceRect.Add(new List<Rect>());
                effectorRect.Add(new List<Rect>());
                lastRects.Add(new Rect());
                foreach (DialogNode.OuterChoice outerChoice in node.GetOuterChoices())
                {
                    outerChoiceRect[index].Add(new Rect());
                }
                foreach (DialogNode.EffectorsAndEffects effectors in node.GetEffectors())
                {
                    effectorRect[index].Add(new Rect());
                }
                index++;
            }
        }
        private void OnGUI()
        {
            if (selectedDialog == null)
            {
                EditorGUILayout.LabelField("No dialog selected");
            }
            else
            {
                ProcessEvents();
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                Rect canvas = GUILayoutUtility.GetRect(dialogWindowSize, dialogWindowSize);
                Rect textCoords = new Rect(0, 0, dialogWindowSize / backgroundSize, dialogWindowSize / backgroundSize);
                Texture2D background = Resources.Load("background") as Texture2D;
                GUI.DrawTextureWithTexCoords(canvas, background, textCoords);
                int nodeCounter = 0;
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawConnections(node, nodeCounter);
                    nodeCounter++;
                }
                nodeCounter = 0;
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    DrawNode(node, nodeCounter);
                    nodeCounter++;
                }
                if (creatingNode != null)
                {
                    if (isLinked)
                    {
                        selectedDialog.CreateNode(creatingNode);
                        creatingNode = null;
                    }
                    else
                    {
                        Undo.RecordObject(creatingNode, "Added unlinked choice");
                        creatingNode.AddOuterChoice("");
                        creatingNode.GetOuterChoices().Last().AddInnerChoices();
                        creatingNode = null;
                    }
                }
                EditorGUILayout.EndScrollView();
                if (nodeToDelete != null)
                {
                    selectedDialog.DeleteNode(nodeToDelete);
                    nodeToDelete = null;
                }
                if (linkingParentNode != null && linkingChildNode != null)
                {
                    Undo.RecordObject(selectedDialog, "Linked node");
                    selectedDialog.LinkNodes(linkingParentNode, linkingChildNode, linkingOuterChoice);
                    linkingParentNode = null;
                    linkingChildNode = null;
                    linkingOuterChoice = null;
                }
                if (effectorToRemove != null)
                {
                    foreach (DialogNode node in selectedDialog.GetAllNodes())
                    {
                        node.RemoveEffector(effectorToRemove);
                    }
                }
                if (costOrBenefitToRemove != null)
                {
                    foreach (DialogNode node in selectedDialog.GetAllNodes())
                    {
                        node.RemoveCostOrBenefit(costOrBenefitToRemove);
                    }
                }
            }
        }
        private void ProcessEvents()
        {
            if (Event.current.type == EventType.MouseDown && draggedNode == null)
            {
                draggedNode = GetNodeAtPoint(Event.current.mousePosition + scrollPosition);
                if (draggedNode != null)
                {
                    dragOffset = Event.current.mousePosition - draggedNode.GetRect().position;
                    Selection.activeObject = draggedNode;
                }
                else
                {
                    isScrolling = true;
                    scrollOffset = Event.current.mousePosition + scrollPosition;
                    Selection.activeObject = selectedDialog;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && draggedNode != null)
            {
                draggedNode.SetRect(new Rect(Event.current.mousePosition - dragOffset, selectedDialog.GetRootNode().GetRect().size));
                GUI.FocusControl(null);
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseDrag && isScrolling)
            {
                Undo.RecordObject(selectedDialog, "Update window position");
                scrollPosition = scrollOffset - Event.current.mousePosition;
                GUI.FocusControl(null);
                GUI.changed = true;
            }
            else if (Event.current.type == EventType.MouseUp && draggedNode != null)
            {
                draggedNode = null;
            }
            else if (Event.current.type == EventType.MouseUp && isScrolling)
            {
                isScrolling = false;
            }
        }
        private void DrawNode(DialogNode node, int index)
        {
            float height = node.GetIsRoot() ? rootNodeHeightOffset + GetAllOuterChoicesSize(index) +
                effectorAreaHeight * node.GetEffectorsCount() : nodeHeightOffset + GetAllOuterChoicesSize(index)
                + effectorAreaHeight * node.GetEffectorsCount() + costOrBenefitsHeight * node.GetCostOrBenefitCount();
            node.SetRect(new Rect(node.GetRect().position, new Vector2(node.GetRect().width, height)));

            GUILayout.BeginArea(node.GetRect(), nodeStyle);
            DrawNodeMenu(node);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Speaker:");
            string newSpeaker = selectedDialog.GetSpeakers().Count != 0 ? selectedDialog.GetSpeakerAtIndex(EditorGUILayout.Popup(selectedDialog.GetSpeakerIndex(node.GetSpeaker()), selectedDialog.GetSpeakers().ToArray())) : "";
            node.SetSpeaker(newSpeaker);
            EditorGUILayout.EndHorizontal();
            string newHeader = node.GetHeader();
            string buttonName = newHeader != null && newHeader != "" ? newHeader : "Add Choice";
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.UpperLeft;
            if (GUILayout.Button(buttonName, buttonStyle, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(20)
                /*GUILayout.MaxWidth()*/))
            {
                ChoiceEditor choiceEditor = (ChoiceEditor)GetWindow(typeof(ChoiceEditor), false, "Choice Editor");
                choiceEditor.maxSize = new Vector2(300, 200);
                choiceEditor.minSize = new Vector2(300, 200);
                choiceEditor.Show();
                choiceEditor.Init(node, newHeader);
            }
            GUILayout.Label("Main Choices:");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add linked"))
            {
                creatingNode = node;
                isLinked = true;
                outerChoiceRect.Add(new List<Rect>());
                outerChoiceRect[index].Add(new Rect());
            }
            if (GUILayout.Button("Add unlinked"))
            {
                creatingNode = node;
                isLinked = false;
                outerChoiceRect.Add(new List<Rect>());
                outerChoiceRect[index].Add(new Rect());
            }
            EditorGUILayout.EndHorizontal();
            Rect tempRect = GUILayoutUtility.GetLastRect();
            if (lastRects.Count == index) lastRects.Add(new Rect());
            if (Event.current.type == EventType.Repaint)
            {
                lastRects[index] = tempRect;
            }
            int outerChoiceDrawCounter = 0;
            foreach (DialogNode.OuterChoice outerChoice in node.GetOuterChoices())
            {
                if (outerChoiceRect.Count > 0 && outerChoiceRect[index].Count > 0)
                {
                    DrawOuterChoice(node, outerChoiceDrawCounter, index);
                    outerChoiceDrawCounter++;
                }
            }
            GUILayout.Space(node.GetIsRoot() ? GetAllOuterChoicesSize(index) : GetAllOuterChoicesSize(index));
            GUILayout.Label("Effectors:");
            if (GUILayout.Button("Add new effector"))
            {
                node.AddEffector();
                effectorRect.Add(new List<Rect>());
                effectorRect[index].Add(new Rect());
            }
            Rect tempRect2 = GUILayoutUtility.GetLastRect();
            lastEffectorBtnRects.Add(new Rect());
            if (Event.current.type == EventType.Repaint)
            {
                lastEffectorBtnRects[index] = tempRect2;
            }
            int effectorIndex = 0;
            foreach (DialogNode.EffectorsAndEffects effector in node.GetEffectors().ToList())
            {
                DrawEffector(node, effectorIndex, index, lastEffectorBtnRects[index]);
                effectorIndex++;
            }
            if (!node.GetIsRoot())
            {
                GUILayout.Space(node.GetEffectorsCount() * effectorAreaHeight);
                GUILayout.Label("Costs/Benefits:");
                if (GUILayout.Button("Add Cost/Benefit"))
                {
                    node.AddCostOrBenefit();
                }
                int costOrBenefitIndex = 0;

                foreach (Effector effector in node.GetCostsOrBenefits().ToList())
                {
                    DrawCostsAndBenefits(node, costOrBenefitIndex, index);
                    costOrBenefitIndex++;
                }
            }
            GUILayout.EndArea();
        }


        private void DrawNodeMenu(DialogNode node)
        {
            EditorGUILayout.BeginHorizontal();
            if (linkingParentNode != null && !node.GetIsRoot() && linkingParentNode != node)
            {
                if (GUILayout.Button("Connect", GUILayout.ExpandWidth(false)))
                {
                    linkingChildNode = node;
                }
            }
            GUILayout.FlexibleSpace();
            if (!node.GetIsRoot())
            {
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Remove choice", "This action will delete this choice and all children choices, are you sure?", "OK", "CANCEL"))
                    {
                        nodeToDelete = node;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawConnections(DialogNode node, int nodeIndex)
        {
            int outerChoiceCounter = 0;
            foreach (DialogNode.OuterChoice outerChoice in node.GetOuterChoices())
            {
                Vector2 startPosition = new Vector2(node.GetRect().x + outerChoiceRect[nodeIndex][outerChoiceCounter].xMax, node.GetRect().y + outerChoiceRect[nodeIndex][outerChoiceCounter].center.y);
                if (outerChoice.GetChildUniqueID() != null && outerChoice.GetChildUniqueID() != "")
                {
                    DialogNode childNode = selectedDialog.GetNodeFromID(outerChoice.GetChildUniqueID());
                    Vector2 endPosition = new Vector2(childNode.GetRect().x + nodeStyle.border.left, childNode.GetRect().center.y);
                    Vector2 controlPointOffset = endPosition - startPosition;
                    controlPointOffset.y = 0;
                    controlPointOffset.x *= 0.8f;
                    Handles.DrawBezier(startPosition, endPosition,
                        startPosition + controlPointOffset,
                        endPosition - controlPointOffset,
                        Color.blue, null, 2f);
                    Handles.color = Color.blue;
                    Handles.DrawSolidDisc(endPosition - new Vector2(nodeStyle.border.right / 2, 0), Vector3.back, 5);
                }
                Handles.DrawSolidDisc(startPosition + new Vector2(nodeStyle.border.right / 2, 0), Vector3.forward, 5);
                outerChoiceCounter++;
            }
        }
        private void DrawOuterChoice(DialogNode node, int outerChoiceIndex, int nodeIndex)
        {
            float innerChoiceCount = (node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices() == null || !node.GetOuterChoiceVisibility(outerChoiceIndex)) ? 0 : node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices().Count();
            outerChoiceRect[nodeIndex][outerChoiceIndex] = new Rect(nodeStyle.border.left,
                lastRects[outerChoiceIndex].yMax + (node.GetIsRoot() ? 0 : 20) + GetAllPreviousOuterChoicesSize(outerChoiceIndex, nodeIndex),
                node.GetRect().width - 2 * outerChoiceRect[nodeIndex][outerChoiceIndex].x,
                45 + innerChoiceCount * 22);
            float chanceOfShowing = node.GetOuterChoiceAtIndex(outerChoiceIndex).GetChanceOfShowing();
            Color defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(2f * (1 - chanceOfShowing), 2f * chanceOfShowing, 0);
            GUILayout.BeginArea(outerChoiceRect[nodeIndex][outerChoiceIndex], OuterChoicestyle);
            GUI.backgroundColor = defaultColor;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent($"Choice {outerChoiceIndex + 1} ({node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoicesCount()}):", "Clickable, inner choices count in ()"), GetButtonLabelStyle(), GUILayout.ExpandWidth(false)))
            {
                node.InvertOuterChoiceVisability(outerChoiceIndex);
            }

            node.SetChanceOfShowing(outerChoiceIndex, Mathf.Clamp01(EditorGUILayout.FloatField(node.GetOuterChoiceAtIndex(outerChoiceIndex).GetChanceOfShowing(), GUILayout.MinWidth(30), GUILayout.ExpandWidth(true))));
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(lastRect, new GUIContent("", "Defines the chance for this option to be available for the player (0-1)"));
            DrawLinkingButton(node, outerChoiceIndex);
            if (GUILayout.Button("+", GUILayout.Width(20), GUILayout.ExpandWidth(false)))
            {
                node.GetOuterChoiceAtIndex(outerChoiceIndex).AddInnerChoice();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            if (node.GetOuterChoiceVisibility(outerChoiceIndex))
            {
                if (node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices() != null)
                {
                    for (int i = 0; i < node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices().Count(); i++) //UnityEngine.Object @object in node.OuterChoices[outerChoiceIndex].innerChoice.innerChoices)
                    {
                        DrawInnerChoice(node, outerChoiceIndex, nodeIndex, i);
                    }
                }
            }
            GUILayout.EndArea();
        }
        private GUIStyle GetButtonLabelStyle()
        {
            GUIStyle style = new GUIStyle();
            RectOffset border = style.border;
            border = new RectOffset(0, 0, 0, 0);
            style.normal.textColor = Color.white;
            return style;
        }
        private void DrawLinkingButton(DialogNode node, int outerChoiceIndex)
        {
            if (node.GetOuterChoiceAtIndex(outerChoiceIndex).GetChildUniqueID() != null && node.GetOuterChoiceAtIndex(outerChoiceIndex).GetChildUniqueID() != "" && linkingParentNode == null)
            {
                if (GUILayout.Button("UnLink", GUILayout.ExpandWidth(false)))
                {
                    node.GetOuterChoiceAtIndex(outerChoiceIndex).SetChildUniqueId(null);
                }
            }
            else if (linkingParentNode == null)
            {
                if (GUILayout.Button("Link", GUILayout.ExpandWidth(false)))
                {
                    linkingParentNode = node;
                    linkingOuterChoice = node.GetOuterChoiceAtIndex(outerChoiceIndex);
                }
            }
            if (node == linkingParentNode && node.GetOuterChoiceAtIndex(outerChoiceIndex) == linkingOuterChoice)
            {
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(false)))
                {
                    linkingParentNode = null;
                }           
            }

        }
        private void DrawInnerChoice(DialogNode node, int outerChoiceIndex, int nodeIndex, int i)
        {
            EditorGUILayout.BeginHorizontal();
            string choice = node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoiceAtIndex(i);
            string buttonName = choice != null && choice != "" ? choice : "Add Choice";
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.UpperLeft;
            if (GUILayout.Button(buttonName, buttonStyle, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(20),
                GUILayout.MaxWidth(outerChoiceRect[nodeIndex][outerChoiceIndex].width - (OuterChoicestyle.padding.left + OuterChoicestyle.border.left) - 20)))
            {
                ChoiceEditor choiceEditor = (ChoiceEditor)GetWindow(typeof(ChoiceEditor), false, "Choice Editor");
                choiceEditor.maxSize = new Vector2(300, 200);
                choiceEditor.minSize = new Vector2(300, 200);
                choiceEditor.Show();
                choiceEditor.Init(node, outerChoiceIndex, i, choice);
            }
            if (choice == null) node.GetOuterChoiceAtIndex(outerChoiceIndex).SetInnerChoiceAtIndex(i, "");
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                node.GetOuterChoiceAtIndex(outerChoiceIndex).RemoveInnerChoiceAtIndex(i);
            }
            EditorGUILayout.EndHorizontal();
        }
        private void DrawEffector(DialogNode node, int effectorIndex, int nodeIndex, Rect effectorBtnRect)
        {
            float height = node.GetIsRoot() ? GetAllOuterChoicesSize(nodeIndex) + 140 : GetAllOuterChoicesSize(nodeIndex) + 140;
            effectorRect[nodeIndex][effectorIndex] = new Rect(nodeStyle.border.left,
                (node.GetIsRoot() ? 0 : 20) + (effectorAreaHeight * effectorIndex) + height,
                node.GetRect().width - 2 * effectorRect[nodeIndex][effectorIndex].x,
                effectorAreaHeight) ;
            GUILayout.BeginArea(effectorRect[nodeIndex][effectorIndex], OuterChoicestyle);
            EditorGUILayout.BeginHorizontal();
            UnityEngine.Object obj = node.GetEffectorAtIndex(effectorIndex).effector;
            node.AddEffectorAtIndex(EditorGUILayout.ObjectField("", obj, typeof(Effector), true, GUILayout.MaxWidth(168)) as Effector, effectorIndex);
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                effectorToRemove = node.GetEffectorAtIndex(effectorIndex);
            }
            EditorGUILayout.EndHorizontal();
            List<string> displayedOptions = new List<string>();
            for (int i = 0; i < node.GetOuterChoicesCount(); i++)
            {
                displayedOptions.Add("Choice " + (i + 1));
            }
            float defaultWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            node.GetEffectorAtIndex(effectorIndex).choiceIfFalse = EditorGUILayout.Popup("Positive: ", node.GetEffectorAtIndex(effectorIndex).choiceIfFalse, displayedOptions.ToArray());
            node.GetEffectorAtIndex(effectorIndex).choiceIfTrue = EditorGUILayout.Popup("Negative: ", node.GetEffectorAtIndex(effectorIndex).choiceIfTrue, displayedOptions.ToArray());
            EditorGUIUtility.labelWidth = defaultWidth;
            GUILayout.EndArea();
        }
        private void DrawCostsAndBenefits(DialogNode node, int costOrBenefitIndex, int nodeIndex)
        {
            EditorGUILayout.BeginHorizontal();
            UnityEngine.Object obj = node.GetCostOrBenefitAtIndex(costOrBenefitIndex);
            node.AddCostOrBenefitAtIndex(EditorGUILayout.ObjectField("", obj, typeof(Effector), true, GUILayout.MaxWidth(176)) as Effector, costOrBenefitIndex);
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                costOrBenefitToRemove = node.GetCostOrBenefitAtIndex(costOrBenefitIndex);
            }
            EditorGUILayout.EndHorizontal();
        }

        private float GetAllOuterChoicesSize(int nodeIndex)
        {
            float result = 0;
            if (outerChoiceRect == null || outerChoiceRect.Count == 0 || outerChoiceRect[nodeIndex].Count == 0) return 0;
            for (int i = 0; i < outerChoiceRect[nodeIndex].Count; i++)
            {
                if (outerChoiceRect[nodeIndex][i] != null)
                    result += outerChoiceRect[nodeIndex][i].height;
            }
            return result;
        }
        private float GetAllPreviousOuterChoicesSize(int outerChoiceIndex, int nodeIndex)
        {
            float result = 0;
            for (int i = 0; i < outerChoiceIndex; i++)
            {
                result += outerChoiceRect[nodeIndex][i].height;
            }
            if (outerChoiceIndex > 0) result -= 20;
            return result;
        }
        private DialogNode GetNodeAtPoint(Vector2 point)
        {
            DialogNode foundNode = null;
            foreach (DialogNode node in selectedDialog.GetAllNodes())
            {
                if (node.GetRect().Contains(point))
                {
                    foundNode = node;
                }
            }
            return foundNode;
        }
    }
}
