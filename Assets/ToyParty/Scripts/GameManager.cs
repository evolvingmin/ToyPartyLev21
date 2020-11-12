using System;
using System.Collections;
using System.Collections.Generic;
using ToyParty.Utilities;
using UnityEngine;
using ToyParty.Patterns;
using System.Threading.Tasks;
using ToyParty.System;

namespace ToyParty
{
    // 게임 룰도 여기서 제어 하도록 하자.
    public class GameManager : SingletonMono<GameManager>
    {
        public Board Board { get => this.board; }

        [SerializeField]
        private Board board = null;

        [SerializeField]
        private List<Block> blockData = new List<Block>();

        public Vector3Int DropPoint { get => dropPoint; }
        [SerializeField]
        private Vector3Int dropPoint = Vector3Int.zero;

        private void Start()
        {
            foreach (var item in blockData)
            {
                PoolingManager.Instance.Subscribe(item.name, item.Instanciate);
            }
        }

        public async Task PlayBlock(Vector3 worldPosition, Vector3 dir)
        {
            var result = Board.SwapBlockByDirection(worldPosition, dir);

            if (result.Item1 == false)
                return;

            /// 계략적으로 생각한 검사 로직
            /// 스왑이 가능하다고 할 때, start/dest 와 해당 스왑이 유효하다고 판단하면
            /// 그 두점 모두 검사 대상으로 잡는다.
            /// 또한 dir 방향 검사 별로라고 생각하는데, 그냥 콜라이더 충돌처리가 더 나을지도 모른다. 이건 
            /// 막판에 수정가능하니 후순위로 잡자.
            /// 일단 dest를 기준으로 잡아서 HexDirection을 top 부터 오른쪽으로 돌아서 
            /// 똑같은 방향으로 계속 나아가서, dest에 있는 블록과 같지않으면 스킵, 같으면 넣어서
            /// 최종적으로 Dictionary<HexDirection, Queue<BlockBehaviour>> 형태로 6개를 가져오게 만들고
            /// 이 자료구조를 바탕으로 진짜로 스왑이 가능한지 아닌지 판단한다
            /// 여기서 일단 저 자료형에 아무것도 없으면 기본적으로 아예 스왑이 되지 않는 것이고
            /// 가령 Top에 한개, Bottom에 한개 있으면 dest까지 포함하여 총 3개가 될 것이고
            /// 이걸로 유효하니 해당 3개의 블록을 지우고 점수 로직에 합산하는 것이다

            Vector3Int startIndex = result.Item2.Item1;
            Vector3Int destIndex = result.Item2.Item2;

            // 일단 destIndex 체크만 시작.

            //Dictionary<HexDirection, Queue<BlockBehaviour>> matchedFromStart = Board.GetSameTypeBlocksFromPoint(startIndex);
            Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocksFromDest = Board.GetSameTypeBlocksFromPoint(destIndex);

            Dictionary<Vector3Int, BlockBehaviour> matched = GetMatchingBlocks(destIndex, sameBlocksFromDest);

            if (matched.Count == 0)
                return;

            await Board.RemoveBlocks(matched.Values);

            int emptyCount = Board.EmptyCount;

            Debug.Log($"Empty Count : {emptyCount}");

            //PoolingManager.Instance.FetchObject()

            List<HexDirection> dropDir = new List<HexDirection>() { HexDirection.LeftTop, HexDirection.Top, HexDirection.RightTop };
            int dirIndex = 0;

            while (Board.EmptyCount > 0)
            {
                Block dropBlock = blockData.RandomSelect();
                GameObject instantiated = PoolingManager.Instance.FetchObject(dropBlock.name);
                BlockBehaviour behaviour = instantiated.GetComponent<BlockBehaviour>();

                HexDirection suggestDir = (HexDirection)(dirIndex % dropDir.Count);

                await Board.DropBlock(dropPoint, suggestDir, behaviour);
                dirIndex++;
            }

        }

        public enum MatchType
        {
            ShortLine, // 3줄짜리
            LongLine,  // 4줄이상, 이 게임에서는 특수 블록이 만들어질 수 있음.
        }

        /// <summary>
        /// 같은 타입의 블록들이 실제로 매칭되어 제거 될 수 있는지 검사하고, 그 블록들을 돌려준다.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="sameBlocks"></param>
        /// <returns></returns>
        public Dictionary<Vector3Int,BlockBehaviour> GetMatchingBlocks(Vector3Int startIndex, Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocks)
        {
            BlockBehaviour start = Board.GetBlockBehaviour(startIndex);

            Dictionary<Vector3Int, BlockBehaviour> matched = new Dictionary<Vector3Int, BlockBehaviour>();
            

            bool hasMatch = false;

            // 3방향 검사, 여기의 검사 방식이 정교해 질 수록 (GetSameTypeBlocksFromPoint 도) 다양한 메치조건을 만들 수 있다.
            // 일단 라인 체크를 목표로 진행한다.
            for (HexDirection dir = HexDirection.LeftTop; dir <= HexDirection.LeftBottom; dir++)
            {
                HexDirection oppositeDir = dir.OppositeDirection();

                int sameBlockCount = sameBlocks[dir].Count + sameBlocks[oppositeDir].Count + 1;

                // 여기서 3개 이상이면 매치 체크를 한다. 
                if (sameBlockCount < 3)
                    continue;

                hasMatch = true;

                foreach (var item in sameBlocks[dir])
                {
                    if (matched.ContainsKey(item.Index))
                        continue;

                    matched.Add(item.Index, item);
                }
            }

            if(hasMatch)
            {
                matched.Add(startIndex, start);
            }

            return matched;
        }

        
    }

}
