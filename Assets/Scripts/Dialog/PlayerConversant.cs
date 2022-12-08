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
        [SerializeField]
        private int npcAnswerIndex = 0;

        public event Action onConversationUpdate;
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
        public IEnumerable<string[]> GetChoices()
        {
            foreach (DialogNode.OuterChoice outerChoice in currentNode.GetOuterChoices())
            {
                yield return new string[2] { outerChoice.GetRandomInnerChoice(), outerChoice.GetChildUniqueID() }; 
            }
        }

        public void AdvanceNext()
        {
            npcAnswerIndex++;
            onConversationUpdate();
        }

        public string GetNextID()
        {
            return currentNode.GetChildUniqueIDIfIsNext();
        }

        public string GetCurrentNodeID()
        {
            return currentNode.name;
        }

        public string GetNpcAnswer()
        {
            return currentNode.GetNpcAnswer(npcAnswerIndex);
        }

        public bool GetIsNext()
        {
            if (currentNode.GetNpcAnswersCount() - 1 > npcAnswerIndex)
            {
                return true;
            }
            return false;
        }
        public void SetNextNode(string uniqueID)
        {
            currentNode = currentDialog.GetNodeFromID(uniqueID);
            npcAnswerIndex = 0;
            onConversationUpdate();
        }
    }
}
