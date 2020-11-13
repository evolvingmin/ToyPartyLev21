using DG.Tweening;
using JetBrains.Annotations;
using Lean.Touch;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToyParty;
using UnityEngine;

public enum BlockState
{
    Placed,
    Drop,
    Matched
}

public class BlockBehaviour : LeanSelectableBehaviour
{
    private LeanFinger current = null;
    private float touchedTime = 0.0f;
    private Vector3 worldPosition;

    [ShowInInspector]
    public BlockState State { get; set; } = BlockState.Placed;

    [ShowInInspector]
    public string BlockType { get; private set; }
    [ShowInInspector]
    public Vector3Int Index { get; set; }

    [ShowInInspector]
    public bool IsDirty { get; set; } = false;

    protected override void OnSelect(LeanFinger finger)
    {
        var cellIndex = GameManager.Instance.Board.Grid.WorldToCell(this.transform.position);
        Debug.Log($"Selected, cellIndex is  :{cellIndex})");

        current = finger;
        touchedTime = Time.realtimeSinceStartup;
        worldPosition = this.transform.position;
    }

    protected override void OnDeselect()
    {
        if (current == null)
            return;

        var dir = current.GetSnapshotScaledDelta(Time.realtimeSinceStartup - touchedTime);
        _= GameManager.Instance.PlayBlock(worldPosition, dir);
        current = null;
    }

    public void Init(string name, Vector3Int position, Sprite sprite, Color color)
    {
        this.name = position.ToString();
        BlockType = name;
        Index = position;
        State = BlockState.Placed;
        IsDirty = false;
        var renderer = GetComponent<SpriteRenderer>();
        renderer.color = color;
        renderer.sprite = sprite;
    }

    /// <summary>
    /// 현재 인덱스부터 떨어지는 것을 가정하여, 최종 도착점을 가져 옵니다.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="suggestDir"></param>
    /// <returns></returns>
    public Vector3Int GetDestIndex(List<Vector3Int> path = null, HexDirection suggestDir = HexDirection.None)
    {
        Board board = GameManager.Instance.Board;

        Vector3Int currentIndex = Index;
        List<HexDirection> checkDir = new List<HexDirection>() { HexDirection.Bottom, HexDirection.LeftBottom, HexDirection.RightBottom };

        bool isFirst = true;
        while (true)
        {
            Vector3Int nextIndex = currentIndex;
            bool isPassable = false;

            // 위에서 충원하는 방식으로 부를때는, 처음에 가는 방향을 임의로 제어할 수 있도록 만듬.
            if(isFirst && suggestDir != HexDirection.None)
            {
                nextIndex = Board.GetNextIndexByDirection(currentIndex, suggestDir);
                isPassable = board.IsPassable(nextIndex);

                if (isPassable)
                    break;
            }
            
            if(isPassable == false)
            {
                foreach (HexDirection dir in checkDir)
                {
                    nextIndex = Board.GetNextIndexByDirection(currentIndex, dir);

                    isPassable = board.IsPassable(nextIndex);

                    if (isPassable)
                        break;
                }
            }

            if (isPassable == false)
                break;

            currentIndex = nextIndex;

            if (path != null)
            {
                path.Add(currentIndex);
            }

            isFirst = false;
        }


        return currentIndex;
    }

    /// <summary>
    /// 현재 position에서 제공된 path(cellIndex)로 순서대로 이동합니다.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task TravelByPath(List<Vector3Int> path, float speed = 10.0f)
    {
        foreach (var item in path)
        {
            var nextPosition = GameManager.Instance.Board.Grid.CellToWorld(item);

            while(Vector3.Distance(transform.position, nextPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, nextPosition, speed * Time.deltaTime);
                await Awaiters.Seconds(Time.deltaTime);
            }
            transform.position = nextPosition;
        }
    }
}
