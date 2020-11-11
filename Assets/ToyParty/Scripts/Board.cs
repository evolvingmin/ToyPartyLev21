using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using ToyParty.System;
using ToyParty.Utilities;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ToyParty
{
    public enum HexDirection
    {
        Top,
        RightTop,
        RightBottom,
        Bottom,
        LeftBottom,
        LeftTop,
        None,
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
        [SerializeField]
        private Grid grid = null;

        private void Start()
        {
            // 오브젝트 타일에서 사용하는 Sprite들을 단순히 null 세팅 한다고 사라지지 않는다. 
            objectTileMap.RefreshAllTiles();
        }

        public bool HasObject(Vector3Int index)
        {
            if (playAreaMap.HasTile(index) == false)
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

        public bool TryBlockSwap(Vector3 worldPosition, Vector3 dir)
        {
            Vector3Int startIndex = Grid.WorldToCell(worldPosition);
            var hexDir = GetHexDirection(dir);

            
            Vector3Int destIndex = startIndex + GetCellDirIndex(startIndex.y %2 == 0, hexDir);

            if (startIndex == destIndex)
                return false;

            bool hasObject = HasObject(destIndex);
            Debug.Log($"Start Swap to {hexDir}, Index is {destIndex}");
            
            if (hasObject)
            {
                
                return SwapBlock(startIndex, destIndex);
            }
            else
            {
                return false;
            }
        }

        public bool SwapBlock(Vector3Int a, Vector3Int b)
        {
            var aBlock = GetBlockBehaviour(a);
            var bBlock = GetBlockBehaviour(b);
            Vector3 temp = bBlock.transform.position;
            blocks[a] = bBlock;
            blocks[b] = aBlock;

            // 이제 여기서 부터 Async 신경써야 합니당.
            bBlock.transform.position = aBlock.transform.position;
            aBlock.transform.position = temp;
            return true;
        }

        public BlockBehaviour GetBlockBehaviour(Vector3Int index)
        {
            if (blocks.ContainsKey(index) == false)
                return null;

            return blocks[index];
        }

        public static Vector3Int GetCellDirIndex(bool isEven, HexDirection hexDir)
        {
            // Odd Co - ordinate
            // https://www.redblobgames.com/grids/hexagons/
            switch (hexDir)
            {
                case HexDirection.Top:
                    return new Vector3Int(1, 0, 0);
                case HexDirection.RightTop:
                    return isEven ? new Vector3Int(0, 1, 0) : new Vector3Int(1, 1, 0);
                case HexDirection.RightBottom:
                    return isEven ? new Vector3Int(-1, 1, 0) : new Vector3Int(0, 1, 0);
                case HexDirection.Bottom:
                    return new Vector3Int(-1, 0, 0);
                case HexDirection.LeftTop:
                    return isEven ? new Vector3Int(0, -1, 0) : new Vector3Int(1, -1, 0);
                case HexDirection.LeftBottom:
                    return isEven ? new Vector3Int(-1, -1, 0) : new Vector3Int(0, -1, 0);
                default:
                    return Vector3Int.zero;
            }
        }

        public static HexDirection GetHexDirection(Vector3 dir)
        {
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

