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
        [SerializeField]
        private bool isRoot = false;
        [SerializeField]
        private string header;
        [Serializable]
        public class InnerChoices
        {
            public List<Choice> innerChoices; 
        }
        [Serializable]
        public class OuterChoice
        {
            [SerializeField]
            public string childUniqueID;
            [SerializeField]
            public InnerChoices innerChoices = new InnerChoices();
            [SerializeField]
            public float chanceOfShowing = 1f;
            public bool visibility = true;

            public float GetChanceOfShowing()
            {
                return chanceOfShowing;
            }
            
            public void AddInnerChoices()
            {
                innerChoices.innerChoices = new List<Choice>();
                innerChoices.innerChoices.Add(null);
            }

            public string GetChildUniqueID()
            {
                return childUniqueID;
            }

            public IEnumerable<Choice> GetInnerChoices()
            {
                return innerChoices.innerChoices;
            }
            public int GetInnerChoicesCount()
            {
                return innerChoices.innerChoices.Count;
            }

            public void SetChildUniqueId(string newID)
            {
                childUniqueID = newID;
            }

            public Choice GetInnerChoiceAtIndex(int index)
            {
                return innerChoices.innerChoices[index];
            }

            public void SetInnerChoiceAtIndex(int index, Choice choice)
            {
                innerChoices.innerChoices[index] = choice;
            }

            public void RemoveInnerChoiceAtIndex(int i)
            {
                innerChoices.innerChoices.RemoveAt(i);
            }

            public void AddInnerChoice()
            {
                innerChoices.innerChoices.Add(null);
            }
        }
        public void SetChanceOfShowing(int index, float chance)
        {
            Undo.RecordObject(this, "Update chance of outer choice showing");
            outerChoices[index].chanceOfShowing = chance;
        }
        public void IsRoot(bool v)
        {
            isRoot = v;
        }

        [SerializeField]
        private List<OuterChoice> outerChoices = new List<OuterChoice>();
        [Serializable]
        public class EffectorsAndEffects
        {
            public Effector effector;
            public int choiceIfTrue;
            public int choiceIfFalse;
        }
        [SerializeField]
        List<EffectorsAndEffects> effectorsAndEffects = new List<EffectorsAndEffects>();
        [SerializeField]
        private Rect rect = new Rect(0, 0, 240, 800);
        [SerializeField]
        private List<Effector> costOrBenefits = new List<Effector>();

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

        public void InvertOuterChoiceVisability(int outerChoiceIndex)
        {
            outerChoices[outerChoiceIndex].visibility = !outerChoices[outerChoiceIndex].visibility;
        }

        public bool GetOuterChoiceVisibility(int outerChoiceIndex)
        {
            return outerChoices[outerChoiceIndex].visibility;
        }

        public int GetOuterChoicesCount()
        {
            return outerChoices.Count;
        }

        public void AddEffector()
        {
            effectorsAndEffects.Add(new EffectorsAndEffects
            {
                effector = null,
                choiceIfFalse = 0,
                choiceIfTrue = 0
            });
        }
        public void AddCostOrBenefit()
        {
            costOrBenefits.Add(null);
        }

        public IEnumerable<EffectorsAndEffects> GetEffectors()
        {
            return effectorsAndEffects;
        }

        public void RemoveEffectorAtIndex(int index)
        {
            Undo.RecordObject(this, "Remove effector");
            effectorsAndEffects.RemoveAt(index);
        }

        public EffectorsAndEffects GetEffectorAtIndex(int index)
        {
            return effectorsAndEffects[index];
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

        public IEnumerable<Effector> GetCostsOrBenefits()
        {
            return costOrBenefits;
        }

        public Effector GetCostOrBenefitAtIndex(int costOrBenefitIndex)
        {
            return costOrBenefits[costOrBenefitIndex];
        }

        public void RemoveCostOrBenefit(Effector costOrBenefitToRemove)
        {
            costOrBenefits.Remove(costOrBenefitToRemove);
        }

        public int GetCostOrBenefitCount()
        {
            return costOrBenefits.Count;
        }

        public void AddCostOrBenefitAtIndex(Effector effector, int costOrBenefitIndex)
        {
            costOrBenefits[costOrBenefitIndex] = effector;
        }
    }
}
