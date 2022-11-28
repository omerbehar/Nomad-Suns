using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

namespace Nomad.Dialog
{
    [CreateAssetMenu(fileName = "New Dialog", menuName = "Dialog/Dialog", order = 50)]
    public class Dialog : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<DialogNode> nodes = new List<DialogNode>();
        Dictionary<string, DialogNode> nodeLookup = new Dictionary<string, DialogNode>();   
        private void Awake()
        {
            OnValidate();
        }
        private void OnValidate() 
        {
            nodeLookup.Clear();
            foreach (DialogNode node in GetAllNodes())
            {
                nodeLookup[node.name] = node;
            }
        }
        public IEnumerable<DialogNode> GetAllNodes()
        {
            return nodes;
        }

        public DialogNode GetRootNode()
        {
            foreach (DialogNode node in GetAllNodes())
            {
                if (node.GetIsRoot()) return node;
            }
            return null;
        }

        public IEnumerable<DialogNode> GetChildNodes(DialogNode parentNode)
        {
            foreach (DialogNode.OuterChoice outerChoice in parentNode.GetOuterChoices())
            {
                if (nodeLookup.ContainsKey(outerChoice.GetChildUniqueID()))
                {
                    yield return nodeLookup[outerChoice.GetChildUniqueID()];
                }
            }
        }

        public void CreateNode(DialogNode parentNode)
        {
            DialogNode newNode = CreateInstance<DialogNode>();
            newNode.name = Guid.NewGuid().ToString();
            Undo.RegisterCreatedObjectUndo(newNode, "Created dialog node");
            newNode.SetRect(new Rect(parentNode.GetRect().xMax + 50, parentNode.GetRect().y, newNode.GetRect().size.x, newNode.GetRect().size.y));
            Undo.RecordObject(parentNode, "Adding new node as a child to parent node");
            parentNode.AddOuterChoice(newNode.name);
            parentNode.GetOuterChoices().Last().AddInnerChoices();
            Undo.RecordObject(this, "Added dialog node");
            nodes.Add(newNode);
            OnValidate();
        }
        public void DeleteNode(DialogNode nodeToDelete)
        {
            foreach (DialogNode.OuterChoice outerchoice in nodeToDelete.GetOuterChoices().ToList())
            {
                DeleteNode(GetNodeFromID(outerchoice.GetChildUniqueID()));
            }
            foreach(DialogNode node in nodes)
            {
                foreach (DialogNode.OuterChoice outerChoice in node.GetOuterChoices().ToList())
                {
                    if (nodeToDelete.name == outerChoice.GetChildUniqueID())
                    {
                        node.RemoveOuterChoice(outerChoice);
                    }
                }
            }
            Undo.RecordObject(this, "Removed dialog node");
            nodes.Remove(nodeToDelete);
            AssetDatabase.RemoveObjectFromAsset(nodeToDelete);
            Undo.DestroyObjectImmediate(nodeToDelete);
            OnValidate();
        }
        public DialogNode GetNodeFromID(string uniqueID)
        {
            foreach(DialogNode node in nodes)
            {
                if (node.name == uniqueID)
                {
                    return node;
                }
            }
            return null; 
        }

        public void LinkNodes(DialogNode linkingParentNode, DialogNode linkingChildNode, DialogNode.OuterChoice outerChoice)
        {
            outerChoice.SetChildUniqueId(linkingChildNode.name);
        }

        public void OnBeforeSerialize()
        {
            if (nodes.Count == 0)
            {
                DialogNode rootNode = CreateInstance<DialogNode>();
                rootNode.name = Guid.NewGuid().ToString();
                rootNode.IsRoot(true);
                nodes.Add(rootNode);
            }
            if (AssetDatabase.GetAssetPath(this) != "")
            {
                foreach (DialogNode node in GetAllNodes())
                {
                    if (AssetDatabase.GetAssetPath(node) == "")
                    {
                        AssetDatabase.AddObjectToAsset(node, this);
                    }
                }
            }
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}

