using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using ToyParty.System;
using ToyParty.Utilities;
using ToyParty.Patterns;

namespace ToyParty
{
    public enum ClearCondition
    {
        Collect_Top,
        Collect_Paper,
        Collect_MovingTube,
        Damage_ToyBox,
        //etc...
    }

    public enum GoalCheckType
    {
        None,
        OnCollect,
        OnDamage,
    }

    // 이 부분은 자료형으로 보관되어야 할 것.
    public static class LevelDataEx
    {
        public static GoalCheckType GetGoalCheckType(this ClearCondition clearCondition)
        {
            switch (clearCondition)
            {
                case ClearCondition.Collect_Top:
                    return GoalCheckType.OnCollect;
                default:
                    return GoalCheckType.None;
            }
        }

        public static string GetTargetBlockType(this ClearCondition clearCondition)
        {
            switch (clearCondition)
            {
                case ClearCondition.Collect_Top:
                    return "Block_Top";
                default:
                    return "Error:: Target BlockType is not Assigned";
            }
        }
        
    }

    // 게임 룰도 여기서 제어 하도록 하자.
    public class GameManager : SingletonMono<GameManager>
    {
        public Board Board { get => this.board; }

        [SerializeField]
        private Board board = null;

        [SerializeField]
        private List<Block> blocks = new List<Block>();
        private List<Block> spawnableBlocks = new List<Block>();
        private Dictionary<string, BlockData> blockDataByTag = new Dictionary<string, BlockData>();

        public BlockData GetBlockData(string blockTag) => blockDataByTag.ContainsKey(blockTag) ? blockDataByTag[blockTag] : default;

        public Vector3Int DropPoint { get => dropPoint; }
        [SerializeField]
        private Vector3Int dropPoint = Vector3Int.zero;

        public ClearCondition ClearCondition { get; private set; } = ClearCondition.Collect_Top;
        public bool IsLevelClear { get; private set; } = false;
        public int GoalCount { get; private set; } = 0;

        private void Start()
        {
            foreach (Block block in blocks)
            {
                PoolingManager.Instance.Subscribe(block.name, block.Instanciate);
                blockDataByTag.Add(block.name, block.Data);

                if(block.IsSpawnable())
                {
                    spawnableBlocks.Add(block);
                }
            }

            //레벨 관련 세팅
            IsLevelClear = false;
            GoalCount = Board.GetLevelInitialCount(ClearCondition.GetTargetBlockType());
            Messenger.Instance.Send_CurrentGoalCount(GoalCount);
            Debug.Log($"{GoalCount}");

            InputManager.Instance.PlayState = InputState.Idle;
        }

        public async Task PlayBlock(Vector3 worldPosition, Vector3 dir)
        {
            if (InputManager.Instance.PlayState == InputState.Lock)
                return;

            if (IsLevelClear)
                return;

            InputManager.Instance.PlayState = InputState.Lock;
            Debug.Log($"InputState: {InputManager.Instance.PlayState}");

            var result = await Board.SwapBlockByDirection(worldPosition, dir);

            if (result.Item1 == false)
            {
                InputManager.Instance.PlayState = InputState.Idle;
                Debug.Log($"InputState: {InputManager.Instance.PlayState}");
                return;
            }

            bool anyMatched = await PlayCoreCycle(new List<Vector3Int>() { result.Item2.Item1, result.Item2.Item2 });

            if(anyMatched == false)
            {
                // 아무것도 매치된 것이 없다면 다시 위치를 복구 시킨다.
                await Board.SwapBlock(result.Item2.Item1, result.Item2.Item2);
            }

            if(IsLevelClear)
            {
                Messenger.Instance.Send_LevelComplete();
            }
            else
            {
                InputManager.Instance.PlayState = InputState.Idle;
            }


            Debug.Log($"InputState: {InputManager.Instance.PlayState}");
        }

        /// <summary>
        /// [매칭 블록 찾기 -> 매칭된 블록 제거 -> 블록들 정렬 -> 사라진 블록만큼 추가.] 게임의 핵심루프를 더 이상 매칭되는 블록이 없을 때 까지 반복합니다.
        /// </summary>
        /// <param name="candidateIndexes">매칭 조건을 탐색할 후보자 인덱스들이 담긴 열거형입니다.</param>
        /// <param name="cycleCount"></param>
        /// <returns>단 한번이라도 메인 사이클이 동작하였다면 true, 아니면 false를 반환합니다.</returns>
        private async Task<bool> PlayCoreCycle(IEnumerable<Vector3Int> candidateIndexes, int cycleCount = 1)
        {
            Dictionary<Vector3Int, BlockBehaviour> matched = GetMatchingBlocks(candidateIndexes);

            if (matched.Count == 0)
            {
                return cycleCount != 1;
            }

            Debug.Log($"Core Cycle Count: {cycleCount}");

            List<BlockBehaviour> blocksToRemove = new List<BlockBehaviour>(GetBrokenObstacles(matched.Keys));
            blocksToRemove.AddRange(matched.Values);

            List <Vector3Int> candidates = new List<Vector3Int>(await Board.RemoveBlocks(blocksToRemove, 10));

            List<HexDirection> dropDir = new List<HexDirection>() { HexDirection.LeftTop, HexDirection.Top, HexDirection.RightTop };
            int dirIndex = 0;

            while (Board.EmptyCount > 0)
            {
                Block newBlock = spawnableBlocks.RandomSelect();
 
                GameObject instantiated = PoolingManager.Instance.FetchObject(newBlock.name);
                BlockBehaviour behaviour = instantiated.GetComponent<BlockBehaviour>();
                behaviour.ResetBehaviour(dropPoint, newBlock.Data);

                HexDirection suggestDir = (HexDirection)(dirIndex % dropDir.Count);

                await Board.DropBlock(suggestDir, behaviour);
                candidates.Add(behaviour.Index);
                dirIndex++;
            }

            // 매칭 검사를 진행할 블록들은 정렬로 인해 위치가 옮겨진 블록들과, 새로 추가된 블록들을 대상으로 합니다.
            return await PlayCoreCycle(candidates, cycleCount + 1);
        }

        public void CheckGoalProgress(string blockTag, GoalCheckType goalCheckType)
        {
            if (goalCheckType != ClearCondition.GetGoalCheckType())
                return;

            if (blockTag != ClearCondition.GetTargetBlockType())
                return;

            GoalCount--;
            Messenger.Instance.Send_CurrentGoalCount(GoalCount);

            if (GoalCount <= 0)
            {
                IsLevelClear = true;
            }
                

        }

        private IEnumerable<BlockBehaviour> GetBrokenObstacles(IEnumerable<Vector3Int> matched)
        {
            List<BlockBehaviour> brokenObstacles = new List<BlockBehaviour>();

            foreach (var neighbour in Board.GetNeighbours(matched))
            {
                bool isBroken = neighbour.ReduceDurability();

                if (isBroken == false)
                    continue;

                brokenObstacles.Add(neighbour);
            }

            return brokenObstacles;
        }

        private Dictionary<Vector3Int, BlockBehaviour> GetMatchingBlocks(IEnumerable<Vector3Int> checkIndexes)
        {
            Dictionary<Vector3Int, BlockBehaviour> matched = new Dictionary<Vector3Int, BlockBehaviour>();

            foreach (var index in checkIndexes)
            {
                Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocksFromDest = Board.GetSameTypeBlocksFromPoint(index);
                Dictionary<Vector3Int, BlockBehaviour> candidates = CheckMatchingConditions(index, sameBlocksFromDest);

                foreach (var candidate in candidates)
                {
                    if (matched.ContainsKey(candidate.Key))
                        continue;
                    
                    matched.Add(candidate.Key, candidate.Value);
                }
            }

            return matched;
        }

        /// <summary>
        /// 같은 타입의 블록들이 실제로 매칭되어 제거 될 수 있는지 검사하고, 그 블록들을 돌려준다.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="sameBlocks"></param>
        /// <returns></returns>
        private Dictionary<Vector3Int,BlockBehaviour> CheckMatchingConditions(Vector3Int startIndex, Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocks)
        {
            BlockBehaviour start = Board.GetBlockBehaviour(startIndex);

            Dictionary<Vector3Int, BlockBehaviour> matched = new Dictionary<Vector3Int, BlockBehaviour>();
            
            bool hasMatch = false;
            
            if(GetBlockData(start.BlockTag).matchType == MatchType.Obstacle)
            {
                return matched;
            }

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
