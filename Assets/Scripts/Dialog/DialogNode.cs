using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
using System.Linq;

namespace Nomad.Dialog
{
    public class DialogNode : ScriptableObject
    {
        static System.Random random = new System.Random();

        [SerializeField]
        private bool isRoot = false;
        [SerializeField]
        string speaker = "";
        [SerializeField]
        private string header;
        //[SerializeField]
        //private bool isNext;
        [SerializeField]
        private string childUniqueIDIfIsNext;
        [Serializable]
        public class InnerChoices
        {
            public List<string> innerChoices; 
        }
        [Serializable]
        public class CostOrBenefits
        {
            public Effector costOrBenefit;
            public float amount;
        }
        [Serializable]
        public class OuterChoice
        {
            [SerializeField]
            public string childUniqueID;
            [SerializeField]
            public InnerChoices innerChoices = new InnerChoices();
            [SerializeField]
            public int priority = 2;
            public bool visibility = true;
            [SerializeField]
            List<EffectorsAndEffects> effectorsAndEffects = new List<EffectorsAndEffects>();
            [SerializeField]
            private List<CostOrBenefits> costOrBenefits = new List<CostOrBenefits>();
            [SerializeField]
            Rect rect;
            public string GetRandomInnerChoice()
            {
                string result = "no inner choices";
                if (innerChoices.innerChoices != null && innerChoices.innerChoices.Count > 0)
                {
                    foreach(string choice in innerChoices.innerChoices)
                    {
                        if (choice == "")
                        {
                            return "atleset one of your inner choices is empty";
                        }
                    }
                    result = innerChoices.innerChoices[random.Next(innerChoices.innerChoices.Count)];
                }
                return result;
            }
            public void AddInnerChoices()
            {
                innerChoices.innerChoices = new List<string>();
                innerChoices.innerChoices.Add(null);
            }
            public IEnumerable<string> GetInnerChoices()
            {
                return innerChoices.innerChoices;
            }
            public int GetInnerChoicesCount()
            {
                return innerChoices.innerChoices.Count;
            }
            public string GetInnerChoiceAtIndex(int index)
            {
                return innerChoices.innerChoices[index];
            }
            public void SetInnerChoiceAtIndex(int index, string choice)
            {
                innerChoices.innerChoices[index] = choice;
            }
            public void RemoveInnerChoiceAtIndex(int i)
            {
                innerChoices.innerChoices.RemoveAt(i);
            }
            public void AddInnerChoice(string choice)
            {
                innerChoices.innerChoices.Add(choice);
            }
            public int GetPriority()
            {
                return priority;
            }
            public string GetChildUniqueID()
            {
                return childUniqueID;
            }
            public void SetChildUniqueId(string newID)
            {
                childUniqueID = newID;
            }
            public void AddEffector()
            {
                effectorsAndEffects.Add(new EffectorsAndEffects
                {
                    effector = null,
                    amount = 0,
                    relation = 1
                });
            }
            public IEnumerable<EffectorsAndEffects> GetEffectors()
            {
                return effectorsAndEffects;
            }
            public int GetEffectorsCount()
            {
                return effectorsAndEffects.Count;
            }
            public void RemoveEffector(EffectorsAndEffects effector)
            {
                effectorsAndEffects.Remove(effector);
            }
            public void AddEffectorAtIndex(Effector effector, int effectorIndex)
            {
                effectorsAndEffects[effectorIndex].effector = effector;
            }
            public void AddCostOrBenefit()
            {
                costOrBenefits.Add(new CostOrBenefits
                {
                    costOrBenefit = null,
                    amount = 0
                });
            }
            public IEnumerable<CostOrBenefits> GetCostsOrBenefits()
            {
                return costOrBenefits;
            }
            public CostOrBenefits GetCostOrBenefitAtIndex(int costOrBenefitIndex)
            {
                return costOrBenefits[costOrBenefitIndex];
            }
            public void RemoveCostOrBenefit(int index)
            {
                costOrBenefits.RemoveAt(index);
            }
            public int GetCostOrBenefitCount()
            {
                return costOrBenefits.Count;
            }
            public void AddCostOrBenefitAtIndex(Effector effector, int costOrBenefitIndex)
            {
                costOrBenefits[costOrBenefitIndex].costOrBenefit = effector;
            }
            public float GetCostOrBenefitAmountAtIndex(int costOrBenefitIndex)
            {
                return costOrBenefits[costOrBenefitIndex].amount;
            }
            public void SetCostOrBenefitAmountAtIndex(float newAmount, int costOrBenefitIndex)
            {
                costOrBenefits[costOrBenefitIndex].amount = newAmount;
            }
            public Rect GetOuterChoiceRect()
            {
                return rect;
            }
            public void SetOuterChoiceRect(Rect newRect)
            {
                rect = newRect;
            }
            public bool GetIsVisible()
            {
                return visibility;
            }
            public void InvertVisibility()
            {
                visibility = !visibility;
            }

            public void SetPriority(int newPriotiy)
            {
                priority = newPriotiy;
            }
        }

        public string GetNpcAnswer(int npcAnswerIndex)
        {
            return npcAnswers[npcAnswerIndex];
        }

        public void SetNpcAnswer(int npcAnswerIndex, string selectedChoice)
        {
            npcAnswers[npcAnswerIndex] = selectedChoice;
        }

        [SerializeField]
        private List<OuterChoice> outerChoices = new List<OuterChoice>();
        [Serializable]
        public class EffectorsAndEffects
        {
            public Effector effector;
            public float amount;
            public int relation;
            public float GetAmount()
            {
                return amount;
            }
            public void SetAmount(float newAmount)
            {
                amount = newAmount;
            }
            public int GetRelation()
            {
                return relation;
            }
            public void SetRelation(int newRelation)
            {
                relation = newRelation;
            }
            //public int choiceIfTrue;
            //public int choiceIfFalse;
            //public bool isAbsolute;
            //public float effectIfNotAbsolute;
            //public bool GetIsAbsolute()
            //{
            //    return isAbsolute;
            //}
            //public void SetIsAbsolute(bool v)
            //{
            //    isAbsolute = v;
            //}
            //public float GetEffect()
            //{
            //    return effectIfNotAbsolute;
            //}
            //public void SetEffect(float newEffect)
            //{
            //    effectIfNotAbsolute = newEffect;
            //}
        }
        
        [SerializeField]
        private Rect rect = new Rect(0, 0, 240, 800);
        private bool isVisable = true;
        [SerializeField]
        private List<string> npcAnswers = new List<string>();


        public void IsRoot(bool v)
        {
            isRoot = v;
        }
        public IEnumerable<OuterChoice> GetOuterChoices()
        {
            return outerChoices;
        }
        public void AddOuterChoice(string newNodeName)
        {
            outerChoices.Add(new OuterChoice
            {
                childUniqueID = newNodeName,
                innerChoices = new InnerChoices()
            });
        }
        public OuterChoice GetOuterChoiceAtIndex(int index)
        {
            return outerChoices[index];
        }
        public Rect GetRect()
        {
            return rect;
        }
        public void SetRect(Rect newRect)
        {
            Undo.RecordObject(this, "Update Dialog Node position and/or size");
            rect = newRect;
        }
        public string GetHeader()
        {
            return header;
        }
        public void SetHeader(string newHeader)
        {
            if (header != newHeader)
            {
                Undo.RecordObject(this, "Update Dialog Node header");
                header = newHeader;
            }
        }
        public void RemoveOuterChoice(OuterChoice outerChoice)
        {
            Undo.RecordObject(this, "Remove child referance in parent of deleted node");
            outerChoices.Remove(outerChoice);
        }
        public bool GetIsRoot()
        {
            return isRoot;
        }
        //public void InvertOuterChoiceVisability(int outerChoiceIndex)
        //{
        //    outerChoices[outerChoiceIndex].visibility = !outerChoices[outerChoiceIndex].visibility;
        //}
        //public bool GetOuterChoiceVisibility(int outerChoiceIndex)
        //{
        //    return outerChoices[outerChoiceIndex].visibility;
        //}
        public int GetOuterChoicesCount()
        {
            return outerChoices.Count;
        }
        //public void RemoveEffectorAtIndex(int index)
        //{
        //    Undo.RecordObject(this, "Remove effector");
        //    effectorsAndEffects.RemoveAt(index);
        //}
        //public EffectorsAndEffects GetEffectorAtIndex(int index)
        //{
        //    return effectorsAndEffects[index];
        //}
        
        public void SetSpeaker(string newSpeaker)
        {
            speaker = newSpeaker;
        }
        public string GetSpeaker()
        {
            return speaker;
        }

        public void IsVisable()
        {
            isVisable = !isVisable;
        }
        public void IsVisable(bool v)
        {
            isVisable = v;
        }

        public bool GetIsVisable()
        {
            return isVisable;
        }
        //public bool GetIsNext()
        //{
        //    return isNext;
        //}

        public void AddNpcAnswer()
        {
            npcAnswers.Add("");
        }
        public int GetNpcAnswersCount()
        {
            return npcAnswers.Count;
        }
        public IEnumerable<string> GetNpcAnswers()
        {
            return npcAnswers;
        }

        //public void SetIsNext(bool isNext)
        //{
        //    this.isNext = isNext;
        //}
        public string GetChildUniqueIDIfIsNext()
        {
            return childUniqueIDIfIsNext;
        }
        public void SetChildUniqueIDIfIsNext(string uniqueID)
        {
            childUniqueIDIfIsNext = uniqueID;
        }

        public void RemoveNpcAnswer(int npcAnswerIndex)
        {
            npcAnswers.RemoveAt(npcAnswerIndex);
        }
    }
}
