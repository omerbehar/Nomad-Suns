using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nomad.Dialog;
using TMPro;
using UnityEngine.UI;
using System;

namespace Nomad.UI
{
    public class DialogUI : MonoBehaviour
    {
        PlayerConversant playerConversant;
        [SerializeField]
        TextMeshProUGUI speaker;
        [SerializeField]
        TextMeshProUGUI AIText;
        [SerializeField]
        TextMeshProUGUI[] answers = new TextMeshProUGUI[3];
        [SerializeField]
        Button[] answerButtons = new Button[3];
        //List<string> uniqueIDs = new List<string>(3);
        //List<string> choices = new List<string>(3);
        // Start is called before the first frame update
        void Start()
        {
            playerConversant = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerConversant>();
            AIText.text = playerConversant.GetHeader();
            speaker.text = playerConversant.GetSpeaker();
            List<string> choices = playerConversant.GetChoices();
            List<string> uniqueIDs = playerConversant.GetUniqueIDs();
            for (int i = 0; i < choices.Count; i++)
            {
                answers[i].text = choices[i];
            }
            answerButtons[0].onClick.AddListener(() => AnswerSelected(uniqueIDs[0]));
            answerButtons[1].onClick.AddListener(() => AnswerSelected(uniqueIDs[1]));
            answerButtons[2].onClick.AddListener(() => AnswerSelected(uniqueIDs[2]));
        }

        private void AnswerSelected(string uniqueID)
        {
            playerConversant.SetNextNode(uniqueID);
            AIText.text = playerConversant.GetHeader();
            speaker.text = playerConversant.GetSpeaker();
            List<string> choices = playerConversant.GetChoices();
            List<string> uniqueIDs = playerConversant.GetUniqueIDs();

            for (int i = 0; i < choices.Count; i++)
            {
                answers[i].text = choices[i];
            }
            answerButtons[0].onClick.AddListener(() => AnswerSelected(uniqueIDs[0]));
            answerButtons[1].onClick.AddListener(() => AnswerSelected(uniqueIDs[1]));
            answerButtons[2].onClick.AddListener(() => AnswerSelected(uniqueIDs[2]));
        }

    }
}
