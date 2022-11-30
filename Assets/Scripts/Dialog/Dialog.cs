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
        [SerializeField]
        List<string> speakers = new List<string>();
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
#if UNITY_EDITOR
        public void CreateNode(DialogNode parentNode)
        {
            DialogNode newNode = CreateInstance<DialogNode>();
            newNode.name = Guid.NewGuid().ToString();
            Undo.RegisterCreatedObjectUndo(newNode, "Created dialog node");
            newNode.SetSpeaker(speakers[0]);
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


        public void LinkNodes(DialogNode linkingParentNode, DialogNode linkingChildNode, DialogNode.OuterChoice outerChoice)
        {
            outerChoice.SetChildUniqueId(linkingChildNode.name);
        }
#endif
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            if (nodes.Count == 0)
            {
                DialogNode rootNode = CreateInstance<DialogNode>();
                speakers.Add("");
                rootNode.SetSpeaker("");
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
#endif
        }

        public void OnAfterDeserialize()
        {
            
        }
        public DialogNode GetNodeFromID(string uniqueID)
        {
            foreach (DialogNode node in nodes)
            {
                if (node.name == uniqueID)
                {
                    return node;
                }
            }
            return null;
        }
        public string GetFirstParticipant()
        {
            return speakers[0];
        }

        public void SetFirstParticipant(string newFirstParticipant)
        {
            speakers[0] = newFirstParticipant;
        }

        public List<string> GetSpeakers()
        {
            return speakers;
        }

        public int GetSpeakerIndex(string speaker)
        {
            int result = speakers.IndexOf(speaker);
            result = result == -1 ? 0 : result;
            return result;
        }

        public string GetSpeakerAtIndex(int index)
        {
            return speakers[index];
        }
    }
}

