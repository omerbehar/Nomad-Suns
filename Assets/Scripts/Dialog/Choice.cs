using UnityEngine;
using System;
namespace Nomad.Dialog
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Choice", menuName = "Dialog/Choice", order = 50)]
    public class Choice : ScriptableObject
    {
        public string choiceText = "";
    }
}
