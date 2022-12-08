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
        [SerializeField] TextMeshProUGUI speaker;
        [SerializeField] TextMeshProUGUI AIText;
        [SerializeField] Transform answersRoot;
        [SerializeField] GameObject answerPrefab;
        [SerializeField] Button nextBtn;
        [SerializeField] Button endBtn;
        void Start()
        {
            playerConversant = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerConversant>();
            playerConversant.onConversationUpdate += UpadteUI;
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
            answersRoot.gameObject.SetActive(!playerConversant.GetIsNext());
            nextBtn.gameObject.SetActive(playerConversant.GetIsNext());
            endBtn.gameObject.SetActive(playerConversant.GetChoices().Count() == 0 && !playerConversant.GetIsNext());
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
        private void ExitDialog()
        {
            this.gameObject.SetActive(false);
        }
    }
}
