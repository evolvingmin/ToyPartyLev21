using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToyParty.System;
using ToyParty.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ToyParty
{
    public enum HexDirection
    {
        None = 0,
        LeftTop = 1,
        Top = 2,
        RightTop = 3,
        RightBottom = 4,
        Bottom = 5,
        LeftBottom = 6,
    }

    public static class HexDirectionEx
    {
        /// <summary>
        /// 현재 진행방향의 정 반대를 가져 옵니다.
        /// </summary>
        /// <param name="cur"></param>
        /// <returns></returns>
        public static HexDirection OppositeDirection(this HexDirection cur)
        {
            int oppositeIndex = ((int)cur + 2) % 6 + 1;
            return (HexDirection)oppositeIndex;
        }
    }

    public class Board : MonoBehaviour
    {
        // 최초 레벨 배치만을 위해 활용.
        [SerializeField]
        private Tilemap objectTileMap = null;

        // 보드가 움직일 수 있는 영역으로 활용.
        [SerializeField]
        private Tilemap playAreaMap = null;

        private Dictionary<Vector3Int, BlockBehaviour> blocks = new Dictionary<Vector3Int, BlockBehaviour>();

        public Grid Grid { get => this.grid; }
        public int EmptyCount { get => blocks.Count(x => x.Value == null); }

        [SerializeField]
        private Grid grid = null;

        

        private void Start()
        {
            // 오브젝트 타일에서 사용하는 Sprite들을 단순히 null 세팅 한다고 사라지지 않는다. 
            objectTileMap.RefreshAllTiles();
        }

        public bool ContainsBlock(Vector3Int index)
        {
            if (IsInPlayArea(index) == false)
            {
                return false;
            }

            return blocks.ContainsKey(index);
        }

        public void AddBlockFromLevel(Vector3Int position, string tag, BlockBehaviour blockBehaviour)
        {
            PoolingManager.Instance.AddInstantiatedItem(tag, blockBehaviour.gameObject);
            blocks.Add(position, blockBehaviour);
        }

        /// <summary>
        /// 기준점으로 부터 같은 블록들을 6방향, 직선으로 체크한다.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public Dictionary<HexDirection, Queue<BlockBehaviour>> GetSameTypeBlocksFromPoint(Vector3Int startIndex)
        {
            Dictionary<HexDirection, Queue<BlockBehaviour>> sameBlocks = new Dictionary<HexDirection, Queue<BlockBehaviour>>();

            BlockBehaviour start = GetBlockBehaviour(startIndex);

            for (HexDirection dir = HexDirection.LeftTop; dir <= HexDirection.LeftBottom; dir++)
            {
                Queue<BlockBehaviour> sameBlocksByDir = new Queue<BlockBehaviour>();

                Vector3Int nextIndex = startIndex;

                while(true)
                {
                    nextIndex = GetNextIndexByDirection(nextIndex, dir);
                    BlockBehaviour compare = GetBlockBehaviour(nextIndex);

                    if (compare == null)
                        break;

                    if (start.BlockType != compare.BlockType)
                        break;

                    sameBlocksByDir.Enqueue(compare);
                }

                sameBlocks.Add(dir, sameBlocksByDir);
            }

            return sameBlocks;
        }

        public async Task DropBlock(Vector3Int startIndex, HexDirection suggestDir, BlockBehaviour behaviour, float speed = 10.0f)
        {
            behaviour.transform.SetParent(this.transform);
            behaviour.transform.position = Grid.CellToWorld(startIndex);
            behaviour.Index = startIndex;
            behaviour.gameObject.SetActive(true);
            List<Vector3Int> path = new List<Vector3Int>();
            Vector3Int destIndex = behaviour.GetDestIndex(path, suggestDir);

            await behaviour.TravelByPath(path, speed);

            blocks[destIndex] = behaviour;
            blocks[destIndex].State = BlockState.Placed;
            blocks[destIndex].Index = destIndex;
            blocks[destIndex].name = destIndex.ToString();
        }

        public async Task<IEnumerable<Vector3Int>> RemoveBlocks(IEnumerable<BlockBehaviour> blocksToRemove, float speed = 10.0f)
        {
            HashSet<BlockBehaviour> dirtyBlocks = GetDirtyBlocks(blocksToRemove);

            // 매치된 블록 제거
            foreach (BlockBehaviour item in blocksToRemove)
            {
                blocks[item.Index] = null;
                item.State = BlockState.Matched;
                PoolingManager.Instance.StoreObject(item.BlockType, item.gameObject);
            }

            // 정렬 시작.
            List<Task> tasks = new List<Task>();

            foreach (BlockBehaviour block in dirtyBlocks.OrderBy( x=> x.transform.position.y))
            {
                if (block.State == BlockState.Matched)
                    continue;

                Vector3Int startIndex = block.Index;
                blocks[startIndex] = null;

                List<Vector3Int> path = new List<Vector3Int>();
                Vector3Int newIndex = block.GetDestIndex(path);
                blocks[newIndex] = block;
                blocks[newIndex].State = BlockState.Placed;
                blocks[newIndex].Index = newIndex;

                Task task = block.TravelByPath(path, speed);
                tasks.Add(task);
            }

            // 모든 개별 블록들의 TravelByPath가 끝날 때 까지 기다림.
            foreach (var task in tasks)
            {
                await Awaiters.Until(() => task.IsCompleted);
            }

            return dirtyBlocks.Select(x => x.Index);
        }


        /// <summary>
        /// 주어진 블록들이 제거될 때, 정렬이 필요한 블록들을 체크하고, 해당 블록들을 가져 옵니다.
        /// </summary>
        /// <param name="matched"></param>
        /// <returns></returns>
        private HashSet<BlockBehaviour> GetDirtyBlocks(IEnumerable<BlockBehaviour> matched)
        {
            HashSet<BlockBehaviour> dirtyBlocks = new HashSet<BlockBehaviour>();

            List<HexDirection> checkDir = new List<HexDirection>() { HexDirection.Top, HexDirection.RightTop, HexDirection.LeftTop };

            foreach (var item in matched)
            {
                Vector3Int nextIndex = item.Index;

                while (true)
                {
                    BlockBehaviour nextBlock = null;
                    Vector3Int validNextIndex = nextIndex;

                    foreach (HexDirection dir in checkDir)
                    {
                        validNextIndex = GetNextIndexByDirection(nextIndex, dir);
                        nextBlock = GetBlockBehaviour(validNextIndex);

                        if (nextBlock != null)
                            break;
                    }

                    if (nextBlock == null)
                        break;

                    nextBlock.IsDirty = true;
                    nextIndex = validNextIndex;

                    if (dirtyBlocks.Contains(nextBlock) == false)
                    {
                        dirtyBlocks.Add(nextBlock);
                        nextBlock.State = BlockState.Drop;
                    }
                        
                }
            }

            return dirtyBlocks;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public (bool, Tuple<Vector3Int,Vector3Int>) SwapBlockByDirection(Vector3 worldPosition, Vector3 dir)
        {
            Vector3Int startIndex = Grid.WorldToCell(worldPosition);
            var hexDir = GetHexDirection(dir);

            
            Vector3Int destIndex = GetNextIndexByDirection(startIndex, hexDir);

            if (startIndex == destIndex)
                return (false, new Tuple<Vector3Int, Vector3Int>(startIndex,destIndex));

            bool hasBlock = ContainsBlock(destIndex);
            Debug.Log($"Start Swap to {hexDir}, Index is {destIndex}");
            
            if (hasBlock)
            {
                return (SwapBlock(startIndex, destIndex), new Tuple<Vector3Int, Vector3Int>(startIndex, destIndex));
            }
            else
            {
                return (false, new Tuple<Vector3Int, Vector3Int>(startIndex, destIndex));
            }
        }

        public bool SwapBlock(Vector3Int a, Vector3Int b)
        {
            var aBlock = GetBlockBehaviour(a);
            var bBlock = GetBlockBehaviour(b);
            
            blocks[a] = bBlock;
            blocks[a].Index = a;
            blocks[a].name = a.ToString();

            blocks[b] = aBlock;
            blocks[b].Index = b;
            blocks[b].name = b.ToString();

            Vector3 tempPos = bBlock.transform.position;
            // 이제 여기서 부터 Async 신경써야 합니당.
            bBlock.transform.position = aBlock.transform.position;
            aBlock.transform.position = tempPos;
            return true;
        }

        public BlockBehaviour GetBlockBehaviour(Vector3Int index)
        {
            if (blocks.ContainsKey(index) == false)
                return null;

            return blocks[index];
        }

        public bool IsInPlayArea(Vector3Int index)
        {
            return playAreaMap.HasTile(index);
        }

        public bool IsPassable(Vector3Int index)
        {
            if (IsInPlayArea(index) == false)
                return false;

            if(blocks.ContainsKey(index))
            {
                if (blocks[index] != null && blocks[index].State == BlockState.Placed)
                    return false;
            }
            return true;
        }

        public static Vector3Int GetNextIndexByDirection(Vector3Int startIndex, HexDirection hexDir)
        {
            return startIndex + GetOffsetByDirection(startIndex, hexDir);
        }

        public static Vector3Int GetOffsetByDirection(Vector3Int startIndex, HexDirection hexDir)
        {
            bool isEven = startIndex.y % 2 == 0;

            // Odd Co - ordinate
            switch (hexDir)
            {
                case HexDirection.Top:
                    return new Vector3Int(1, 0, 0);
                case HexDirection.RightTop:
                    return (isEven ? new Vector3Int(0, 1, 0) : new Vector3Int(1, 1, 0));
                case HexDirection.RightBottom:
                    return (isEven ? new Vector3Int(-1, 1, 0) : new Vector3Int(0, 1, 0));
                case HexDirection.Bottom:
                    return new Vector3Int(-1, 0, 0);
                case HexDirection.LeftTop:
                    return (isEven ? new Vector3Int(0, -1, 0) : new Vector3Int(1, -1, 0));
                case HexDirection.LeftBottom:
                    return (isEven ? new Vector3Int(-1, -1, 0) : new Vector3Int(0, -1, 0));
                default:
                    return Vector3Int.zero;
            }
        }

        public static HexDirection GetHexDirection(Vector3 dir)
        {
            // https://www.redblobgames.com/grids/hexagons/implementation.html
            // 디렉션 벡터에서 임의로 정한 이 방향 벡터, 이 방식 계속 활용하려면 정확하게 계산 필요.
            Vector2 rightTop = Vector2.one;
            Vector2 leftTop = new Vector2(-1, 1);
            Vector2 leftBottom = new Vector2(-1, -1);
            Vector2 rightBottom = new Vector2(1, -1);

            HexDirection hexDirection;

            if(dir.magnitude <0.1f)
            {
                return HexDirection.None;
            }

            if (dir.IsWithinAB(Vector2.right, rightTop))
            {
                hexDirection = HexDirection.RightTop;
            }
            else if (dir.IsWithinAB(rightTop, leftTop))
            {
                hexDirection = HexDirection.Top;
            }
            else if (dir.IsWithinAB(leftTop, Vector2.left))
            {
                hexDirection = HexDirection.LeftTop;
            }
            else if (dir.IsWithinAB(Vector2.left, leftBottom))
            {
                hexDirection = HexDirection.LeftBottom;
            }
            else if (dir.IsWithinAB(leftBottom, rightBottom))
            {
                hexDirection = HexDirection.Bottom;
            }
            else
            {
                hexDirection = HexDirection.RightBottom;
            }

            return hexDirection;
        }


    }
}

