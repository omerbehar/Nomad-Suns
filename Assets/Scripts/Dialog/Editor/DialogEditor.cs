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
        List<Rect> lastRects = new List<Rect>();
        [NonSerialized]
        GUIStyle nodeStyle, OuterChoicestyle;
        [NonSerialized]
        DialogNode draggedNode = null, creatingNode = null, nodeToDelete = null, linkingParentNode = null, linkingChildNode = null;
        [NonSerialized]
        Vector2 dragOffset;
        [NonSerialized]
        private bool isScrolling;
        private Vector2 scrollPosition;
        [NonSerialized]
        private Vector2 scrollOffset;
        [NonSerialized]
        private bool isLinked;
        [NonSerialized]
        private DialogNode.OuterChoice linkingOuterChoice = null;
        [NonSerialized]
        List<List<Rect>> outerChoiceRect = new List<List<Rect>>();
        [NonSerialized]
        List<List<Rect>> effectorRect = new List<List<Rect>>();
        [NonSerialized]
        private DialogNode.EffectorsAndEffects effectorToRemove = null;
        [NonSerialized]
        private int costToRemoveIndex;
        [NonSerialized]
        private DialogNode.OuterChoice outerChoiceToRemoveCostFrom = null;

        const int effectorAreaHeight = 105;
        const float nodeHeightOffset = 180;
        const int costOrBenefitsHeight = 22;
        const float dialogWindowSize = 4000;
        const float backgroundSize = 50;

        [MenuItem("Window/Dialog Editor")]
        public static void ShowEditorWindow()
        {
            GetWindow(typeof(DialogEditor), false, "Dialog Editor");
        }

        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line) //when openning an asset, if its of type Dialog, open dialog editor window
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
            if (selectedDialog) Init();
        }
        private void StyleNode() //style dialog node area
        {
            nodeStyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            nodeStyle.padding = new RectOffset(20, 20, 20, 20);
            nodeStyle.border = new RectOffset(12, 12, 12, 12);
        }
        private void StyleOuterChoice() //style inner areas
        {
            OuterChoicestyle.normal.background = EditorGUIUtility.Load("node0") as Texture2D;
            OuterChoicestyle.padding = new RectOffset(12, 12, 10, 10);
            OuterChoicestyle.border = new RectOffset(12, 12, 12, 12);
        }
        private void OnSelectionChange() //when you change asset, if its of type Dialog, open it in the dialog editor window, otherwise leave the last one
        {
            Dialog newDialog = Selection.activeObject as Dialog;
            if (newDialog != null)
            {
                selectedDialog = newDialog;
                Init();
                Repaint();
            }
        }
        private void Init() //initialize all lists that holds the rects of this dialog
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
                DrawBackground();
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
                EditorGUILayout.EndScrollView();
                CreateAddedNode();
                DeleteRemovedNode();
                LinkNodes();
                RemoveEffector();
                RemoveCostOrBenefit();
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
        private static void DrawBackground()
        {
            Rect canvas = GUILayoutUtility.GetRect(dialogWindowSize, dialogWindowSize);
            Rect textCoords = new Rect(0, 0, dialogWindowSize / backgroundSize, dialogWindowSize / backgroundSize);
            Texture2D background = Resources.Load("background") as Texture2D;
            GUI.DrawTextureWithTexCoords(canvas, background, textCoords);
        }
        private void DrawConnections(DialogNode node, int nodeIndex)
        {
            int outerChoiceCounter = 0;
            foreach (DialogNode.OuterChoice outerChoice in node.GetOuterChoices())
            {
                Vector2 startPosition = new Vector2(node.GetRect().x + outerChoiceRect[nodeIndex][outerChoiceCounter].xMax, node.GetRect().y + outerChoiceRect[nodeIndex][outerChoiceCounter].center.y);
                if (!node.GetIsVisable()) startPosition.y = node.GetRect().y + 50;
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
        private void DrawNode(DialogNode node, int index)
        {
            float height = node.GetIsVisable() ? nodeHeightOffset + GetAllOuterChoicesSize(index) + effectorAreaHeight * node.GetEffectorsCount() : 100;
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
            if (GUILayout.Button(buttonName, buttonStyle, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(20)))
            {
                ChoiceEditor choiceEditor = (ChoiceEditor)GetWindow(typeof(ChoiceEditor), false, "Choice Editor");
                choiceEditor.maxSize = new Vector2(300, 200);
                choiceEditor.minSize = new Vector2(300, 200);
                choiceEditor.Show();
                choiceEditor.Init(node, newHeader);
            }
            if (node.GetIsVisable())
            {
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
                GUILayout.Space(GetAllOuterChoicesSize(index));
                GUILayout.Label("Effectors:");
                if (GUILayout.Button("Add new effector"))
                {
                    node.AddEffector();
                    effectorRect.Add(new List<Rect>());
                    effectorRect[index].Add(new Rect());
                }

                int effectorIndex = 0;
                foreach (DialogNode.EffectorsAndEffects effector in node.GetEffectors().ToList())
                {
                    DrawEffector(node, effectorIndex, index);
                    effectorIndex++;
                }
            }
            GUILayout.EndArea();
        }
        private void CreateAddedNode()
        {
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
        }
        private void DeleteRemovedNode()
        {
            if (nodeToDelete != null)
            {
                selectedDialog.DeleteNode(nodeToDelete);
                nodeToDelete = null;
            }
        }
        private void LinkNodes()
        {
            if (linkingParentNode != null && linkingChildNode != null)
            {
                Undo.RecordObject(selectedDialog, "Linked node");
                selectedDialog.LinkNodes(linkingParentNode, linkingChildNode, linkingOuterChoice);
                linkingParentNode = null;
                linkingChildNode = null;
                linkingOuterChoice = null;
            }
        }
        private void RemoveEffector()
        {
            if (effectorToRemove != null)
            {
                foreach (DialogNode node in selectedDialog.GetAllNodes())
                {
                    node.RemoveEffector(effectorToRemove);
                }
                effectorToRemove = null;
            }
        }
        private void RemoveCostOrBenefit()
        {
            if (outerChoiceToRemoveCostFrom != null)
            {
                outerChoiceToRemoveCostFrom.RemoveCostOrBenefit(costToRemoveIndex);
                //foreach (DialogNode node in selectedDialog.GetAllNodes())
                //{
                //    node.GetOuterChoiceAtIndex(costToRemoveOuterChoiceIndex).RemoveCostOrBenefit(costOrBenefitToRemove);
                //}
                outerChoiceToRemoveCostFrom = null;
            }
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
            if (GUILayout.Button("__", GUILayout.Width(20)))
            {
                node.IsVisable();
            }
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
        private void DrawOuterChoice(DialogNode node, int outerChoiceIndex, int nodeIndex)
        {
            float innerChoiceCount = (node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices() == null || !node.GetOuterChoiceVisibility(outerChoiceIndex)) ? 0 : node.GetOuterChoiceAtIndex(outerChoiceIndex).GetInnerChoices().Count();
            outerChoiceRect[nodeIndex][outerChoiceIndex] = new Rect(nodeStyle.border.left,
                lastRects[outerChoiceIndex].yMax + GetAllPreviousOuterChoicesSize(outerChoiceIndex, nodeIndex),
                node.GetRect().width - 2 * outerChoiceRect[nodeIndex][outerChoiceIndex].x,
                45 + innerChoiceCount * 22 + (!node.GetOuterChoiceVisibility(outerChoiceIndex) ? 0 : 40 + costOrBenefitsHeight * node.GetOuterChoiceAtIndex(outerChoiceIndex).GetCostOrBenefitCount()));
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
                GUILayout.Label("Costs/Benefits:");
                if (GUILayout.Button("Add Cost/Benefit"))
                {
                    node.GetOuterChoiceAtIndex(outerChoiceIndex).AddCostOrBenefit();
                }
                int costOrBenefitIndex = 0;
                foreach (DialogNode.CostOrBenefits costOrBenefit in node.GetOuterChoiceAtIndex(outerChoiceIndex).GetCostsOrBenefits().ToList())
                {
                    DrawCostsAndBenefits(node, costOrBenefitIndex, outerChoiceIndex);
                    costOrBenefitIndex++;
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
        private void DrawEffector(DialogNode node, int effectorIndex, int nodeIndex)
        {
            float height = GetAllOuterChoicesSize(nodeIndex) + 160;
            effectorRect[nodeIndex][effectorIndex] = new Rect(nodeStyle.border.left,
                (effectorAreaHeight * effectorIndex) + height,
                node.GetRect().width - 2 * effectorRect[nodeIndex][effectorIndex].x,
                effectorAreaHeight);
            GUILayout.BeginArea(effectorRect[nodeIndex][effectorIndex], OuterChoicestyle);
            EditorGUILayout.BeginHorizontal();
            UnityEngine.Object obj = node.GetEffectorAtIndex(effectorIndex).effector;
            node.AddEffectorAtIndex(EditorGUILayout.ObjectField("", obj, typeof(Effector), true, GUILayout.MaxWidth(168)) as Effector, effectorIndex);
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                effectorToRemove = node.GetEffectorAtIndex(effectorIndex);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            float defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 120;
            node.GetEffectorAtIndex(effectorIndex).SetIsAbsolute(EditorGUILayout.Toggle("Is Effect absolut: ", node.GetEffectorAtIndex(effectorIndex).GetIsAbsolute(), GUILayout.MaxWidth(160)));
            EditorGUIUtility.labelWidth = defaultLabelWidth;
            GUI.enabled = !node.GetEffectorAtIndex(effectorIndex).GetIsAbsolute();
            node.GetEffectorAtIndex(effectorIndex).SetEffect(Mathf.Clamp01(EditorGUILayout.FloatField(node.GetEffectorAtIndex(effectorIndex).GetEffect())));
            GUI.enabled = true;
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(lastRect, new GUIContent("", "Effect on the chance of showing (0-1)"));

            EditorGUILayout.EndHorizontal();
            List<string> displayedOptions = new List<string>();
            for (int i = 0; i < node.GetOuterChoicesCount(); i++)
            {
                displayedOptions.Add("Choice " + (i + 1));
            }
            displayedOptions.Add("None");
            float defaultWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 60;
            node.GetEffectorAtIndex(effectorIndex).choiceIfFalse = EditorGUILayout.Popup("Positive: ", node.GetEffectorAtIndex(effectorIndex).choiceIfFalse, displayedOptions.ToArray());
            node.GetEffectorAtIndex(effectorIndex).choiceIfTrue = EditorGUILayout.Popup("Negative: ", node.GetEffectorAtIndex(effectorIndex).choiceIfTrue, displayedOptions.ToArray());
            EditorGUIUtility.labelWidth = defaultWidth;
            GUILayout.EndArea();
        }
        private void DrawCostsAndBenefits(DialogNode node, int costOrBenefitIndex, int outerChoiceIndex)
        {
            EditorGUILayout.BeginHorizontal();
            UnityEngine.Object obj = node.GetOuterChoiceAtIndex(outerChoiceIndex).GetCostOrBenefitAtIndex(costOrBenefitIndex).costOrBenefit;
            node.GetOuterChoiceAtIndex(outerChoiceIndex).AddCostOrBenefitAtIndex(EditorGUILayout.ObjectField("", obj, typeof(Effector), true, GUILayout.MaxWidth(119)) as Effector, costOrBenefitIndex);
            node.GetOuterChoiceAtIndex(outerChoiceIndex).SetCostOrBenefitAmountAtIndex(EditorGUILayout.FloatField(node.GetOuterChoiceAtIndex(outerChoiceIndex).GetCostOrBenefitAmountAtIndex(costOrBenefitIndex), GUILayout.Width(46)), costOrBenefitIndex);
            Rect lastRect = GUILayoutUtility.GetLastRect();
            GUI.Label(lastRect, new GUIContent("", "Amount of Cost or Benefit needed"));
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                costToRemoveIndex = costOrBenefitIndex;
                outerChoiceToRemoveCostFrom = node.GetOuterChoiceAtIndex(outerChoiceIndex);
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
