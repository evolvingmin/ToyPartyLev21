using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace ToyParty.Patterns
{
    // 심플한 메신저. 보통 Send 함수는 하나로 처리하고, enum 을 리플렉션으로 함수화 하여 제작한다.
    public class Messenger : SingletonMono<Messenger>
    {
        private List<GMReceiveBehaviour> receivers = null;

        private void Awake()
        {
            receivers = new List<GMReceiveBehaviour>(FindObjectsOfType<GMReceiveBehaviour>());
        }

        public void Send_UpdateTotalScore(int totalScore)
        {
            foreach (var receiver in receivers)
            {
                receiver.Receive_UpdateTotalScore(totalScore);
            }
        }

        public void Send_CurrentGoalCount(int currentGoal) 
        {
            foreach (var receiver in receivers)
            {
                receiver.Receive_CurrentGoalCount(currentGoal);
            }
        }
              
        public void Send_LevelComplete() 
        {
            foreach (var receiver in receivers)
            {
                receiver.Receive_LevelComplete();
            }
        }
    }
}
