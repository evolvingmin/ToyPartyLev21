using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ToyParty.Patterns
{
    // 심플한 게임메시지 수신가능한 모노비헤이비어.
    // 보통 메세지는 리플렉션과 자동 생성으로 작성.
    public class GMReceiveBehaviour : MonoBehaviour
    {
        public virtual void Receive_UpdateTotalScore(int totalScore) { }

        public virtual void Receive_CurrentGoalCount(int currentGoal) { }

        public virtual void Receive_LevelComplete() { }
    }
}
