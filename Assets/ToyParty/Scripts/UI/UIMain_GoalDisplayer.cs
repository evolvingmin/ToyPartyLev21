using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ToyParty.Patterns;

namespace ToyParty.UI
{
    public class UIMain_GoalDisplayer : GMReceiveBehaviour
    {
        [SerializeField]
        private Image goalImage = null;

        [SerializeField]
        private TextMeshProUGUI goalCount;

        public override void Receive_CurrentGoalCount(int currentGoal)
        {
            goalCount.text = currentGoal.ToString();
        }
    }

}
