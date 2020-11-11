using System;
using System.Collections;
using System.Collections.Generic;
using ToyParty.Utilities;
using UnityEngine;
using ToyParty.Patterns;

namespace ToyParty
{
    public class GameManager : SingletonMono<GameManager>
    {
        public Board Board { get => this.board; }

        [SerializeField]
        private Board board = null;

        public void PlayBlock(Vector3 worldPosition, Vector3 dir)
        {
            bool isSuccess = Board.TryBlockSwap(worldPosition, dir);
        }
    }

}
