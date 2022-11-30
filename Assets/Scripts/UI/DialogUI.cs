using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nomad.Dialog;
using TMPro;
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
        // Start is called before the first frame update
        void Start()
        {
            playerConversant = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerConversant>();
            AIText.text = playerConversant.GetHeader();
            speaker.text = playerConversant.GetSpeaker();
            List<string> choices = playerConversant.GetChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                answers[i].text = choices[i];
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
