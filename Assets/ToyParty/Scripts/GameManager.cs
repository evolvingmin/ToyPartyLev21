using System;
using System.Collections;
using System.Collections.Generic;
using ToyParty.Utilities;
using UnityEngine;

namespace ToyParty
{
    public class GameManager : MonoBehaviour
    {
        public Board Board { get => this.board; }

        [SerializeField]
        private Board board = null;

        private static GameManager instance = null;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<GameManager>();
                }

                return instance;
            }

        }


        public void PlayBlock(Vector3 worldPosition, Vector3 dir)
        {
            bool isSuccess = Board.TryBlockSwap(worldPosition, dir);
        }
    }

}
