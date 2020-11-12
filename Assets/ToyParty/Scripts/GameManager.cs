using System;
using System.Collections;
using System.Collections.Generic;
using ToyParty.Utilities;
using UnityEngine;
using ToyParty.Patterns;

namespace ToyParty
{
    // 게임 룰도 여기서 제어 하도록 하자.
    public class GameManager : SingletonMono<GameManager>
    {
        public Board Board { get => this.board; }

        [SerializeField]
        private Board board = null;

        public void PlayBlock(Vector3 worldPosition, Vector3 dir)
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

            //Dictionary<HexDirection, Queue<BlockBehaviour>> matchedFromStart = Board.GetSameTypeBlocksFromPoint(startIndex);
            Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocksFromDest = Board.GetSameTypeBlocksFromPoint(destIndex);

            Dictionary<Vector3Int, BlockBehaviour> matched = GetMatchingBlocks(destIndex, sameBlocksFromDest);

            if (matched.Count == 0)
                return;

            Board.RemoveBlocks(matched.Values);

            Debug.Log("Done");
            /// 블록 합산 로직
            /// 이렇게 3개의 블록이 지워진다고 판단하면, 
            /// 먼저 보드 정리 혹은 타일 정렬 로직을 수행함
            /// 정리 수행은 제거되는 블록들을 시작점으로 하여 (Top 방향으로, 다른 제거되는 블록들도 포함)
            /// 제거 플레그와, 하강 인덱스를 ++ 해 준다.
            /// 그 다음 제거 에니메이션 수행
            /// 그 다음 제거 플레그가 담긴 애들만, 하강 인덱스의 길이만큼 에니메이션을 수행한다.
            /// 여기 레벨에서는 Drop Point가 하나인데, 드랍 포인트 기준으로 빈 공간의 개수를 미리 산정해야 한다.
            /// 항상 Drop Point가 비어 있을 때에만 활성화 되며 
            /// 드랍포인트가 가득차지 않았을 때 왼쪽으로 탐색을 시작하여 탐색 시작과 동시에 Queue 로 Index를 넣는다.
            /// 해당 탐색 로직은 블록 하나당 한번 대응하며, 왼쪽 중간 오른쪽 순으로 탐색이 가능할 때까지 진행하며, 
            /// 더이상 진행이 불가능하면 (3방향 모두 이동불가) 그 곳을 해당 블록이 도달할 위치로 플레그를 세팅하고
            /// 그 블록에게 Queue로 페스를 전달하여, 그 페스대로 블록들이 이동하게 한다.
            /// 
            /// 드랍랍 로직이 종료되면 새로 넣은 블록들을 대상으로 하여 빠른 체크를 수행하고
            /// 있으면 제거, 없으면 
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
