using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nomad.Dialog;
using TMPro;
using UnityEngine.UI;
using System.Linq;

namespace Nomad.UI
{
    public class DialogUI : MonoBehaviour
    {
        PlayerConversant playerConversant;
        AIENG aIENG;
        [SerializeField] TextMeshProUGUI speaker;
        [SerializeField] TextMeshProUGUI AIText;
        [SerializeField] Transform answersRoot;
        [SerializeField] GameObject answerPrefab;
        [SerializeField] Button nextBtn;
        [SerializeField] Button endBtn;
        private bool isAI = true;
        private string dialog;
        private int dialogCounter;

        void Start()
        {
            playerConversant = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerConversant>();
            aIENG = GameObject.FindGameObjectWithTag("Player").GetComponent<AIENG>();
            playerConversant.onConversationUpdate += UpadteUI;
            aIENG.onOpenAIRequestDone += UpadteUI;
            UpadteUI();
            endBtn.gameObject.SetActive(false);
            endBtn.onClick.AddListener(() => ExitDialog());
        }
        void Next()
        {
            playerConversant.AdvanceNext();
        }
        private void UpadteUI()
        {
            if (isAI)
            {
                answersRoot.gameObject.SetActive(true);
                nextBtn.gameObject.SetActive(false);
                endBtn.gameObject.SetActive(playerConversant.GetAIChoices().Count() == 0);
                AIText.text = aIENG.npcLine;
                speaker.text = playerConversant.GetSpeaker();
                foreach (Transform item in answersRoot)
                {
                    Destroy(item.gameObject);
                }
                foreach (string choice in playerConversant.GetAIChoices())
                {
                    GameObject newAnswerObject = Instantiate(answerPrefab, answersRoot);
                    newAnswerObject.GetComponentInChildren<TextMeshProUGUI>().text = choice;
                    newAnswerObject.GetComponentInChildren<Button>().onClick.AddListener(() =>
                    {
                        dialog += aIENG.npcLine + choice;
                        if (dialogCounter < 3)
                        {
                            print(dialogCounter);
                            aIENG.Complete($"this is a beginning of a dialog: {dialog} give the next line of dialog and three possible answers");
                        }
                        else
                        {
                            print(dialogCounter);
                            aIENG.Complete($"this is a beginning of a dialog: {dialog} give the last line of dialog");
                        }
                        dialogCounter++;
                    });
                }
            }
            else
            {
                answersRoot.gameObject.SetActive(!playerConversant.GetIsNext());
                nextBtn.gameObject.SetActive(playerConversant.GetIsNext());
                endBtn.gameObject.SetActive(isAI ? (playerConversant.GetAIChoices().Count() == 0) : playerConversant.GetChoices().Count() == 0 && !playerConversant.GetIsNext());
                AIText.text = playerConversant.GetNpcAnswer();
                speaker.text = playerConversant.GetSpeaker();
                if (playerConversant.GetIsNext())
                {
                    nextBtn.onClick.RemoveAllListeners();
                    nextBtn.onClick.AddListener(() => Next());
                    AIText.text = playerConversant.GetNpcAnswer();
                }
                else
                {
                    foreach (Transform item in answersRoot)
                    {
                        Destroy(item.gameObject);
                    }
                    foreach (string[] answerAndID in playerConversant.GetChoices())
                    {
                        GameObject newAnswerObject = Instantiate(answerPrefab, answersRoot);
                        newAnswerObject.GetComponentInChildren<TextMeshProUGUI>().text = answerAndID[0];
                        newAnswerObject.GetComponentInChildren<Button>().onClick.AddListener(() =>
                        {
                            playerConversant.SetNextNode(answerAndID[1]);
                        });
                    }
                }
            }
        }
        private void ExitDialog()
        {
            this.gameObject.SetActive(false);
        }
    }
}
