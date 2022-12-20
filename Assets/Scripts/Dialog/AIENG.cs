using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI_API;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using UnityEditor;

namespace Nomad.Dialog
{
    public class AIENG : MonoBehaviour
    {
        OpenAIAPI api = new OpenAIAPI("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");
       
        private string control;
        public string npcLine = "";
        public static List<string> choices = new List<string>();

        //Dialog currentDialog = null;

        public event Action onOpenAIRequestDone;

        

        void Start()
        {
            api.UsingEngine = "text-davinci-003";
            Complete("write the first of a dialog between a farmer that lost his sheep and a teenager and three possible answers");
        }

      

        public async void Complete(string query)
        {
            choices.Clear();
            control = "";
            npcLine = "";
            await foreach (CompletionResult token in api.Completions.StreamCompletionEnumerableAsync(new CompletionRequest(query, 300, temperature: .1, top_p: 1, frequencyPenalty: 0.0, presencePenalty: 0.6)))
            {
                control += token;
            }
            string[] result = control.Split(new[] { '\r', '\n' });
            if (result[0] == "") npcLine = result[2];
            else npcLine = result[0];
            if (result.Length > 5) choices.Add(result[5]);
            if (result.Length > 6) choices.Add(result[6]);
            if (result.Length > 7) choices.Add(result[7]);
            onOpenAIRequestDone();

        }
    }
}
