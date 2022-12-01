using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nomad.Dialog
{
    public class PlayerConversant : MonoBehaviour
    {
        [SerializeField]
        Dialog currentDialog;
        DialogNode currentNode;
        private void Awake()
        {
            currentNode = currentDialog.GetRootNode();
        }
        public string GetHeader()
        {
            if (currentDialog == null)
            {
                return "";
            }
            return currentNode.GetHeader();
        }
        public string GetSpeaker()
        {
            if (currentDialog == null)
            {
                return "";
            }
            return currentNode.GetSpeaker();
        }
        public List<string> GetChoices()
        {
            List<string> result = new List<string>();
            foreach (DialogNode.OuterChoice outerChoice in currentNode.GetOuterChoices())
            {
                result.Add(outerChoice.GetRandomInnerChoice());
            }
            return result;
        }
        public List<string> GetUniqueIDs()
        {
            List<string> result = new List<string>();
            foreach (DialogNode.OuterChoice outerChoice in currentNode.GetOuterChoices())
            {
                result.Add(outerChoice.GetChildUniqueID());
            }
            return result;
        }

        public void SetNextNode(string uniqueID)
        {
            currentNode = currentDialog.GetNodeFromID(uniqueID);
        }
    }
}
