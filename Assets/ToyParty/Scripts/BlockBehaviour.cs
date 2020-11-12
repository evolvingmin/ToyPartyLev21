using DG.Tweening;
using JetBrains.Annotations;
using Lean.Touch;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
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
    public int DropCount { get; set; } = 0;

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
        GameManager.Instance.PlayBlock(worldPosition, dir);
        current = null;
        
    }

    public void Init(string name, Vector3Int position, Sprite sprite, Color color)
    {
        this.name = position.ToString();
        BlockType = name;
        Index = position;
        State = BlockState.Placed;
        DropCount = 0;
        var renderer = GetComponent<SpriteRenderer>();
        renderer.color = color;
        renderer.sprite = sprite;

        
    }

    public Vector3Int GetDestIndex()
    {
        Board board = GameManager.Instance.Board;

        Vector3Int currentIndex = Index;
        int dropCount = DropCount;

        List<HexDirection> checkDir = new List<HexDirection>() { HexDirection.Bottom, HexDirection.LeftBottom, HexDirection.RightBottom };

        while (dropCount > 0)
        {
            Vector3Int nextIndex = currentIndex;
            bool isPassable = false;
            foreach (HexDirection dir in checkDir)
            {
                nextIndex = Board.GetNextIndexByDirection(currentIndex, dir);

                isPassable = board.IsPassable(nextIndex);

                if (isPassable)
                    break;
            }

            if (isPassable == false)
                break;

            currentIndex = nextIndex;
            dropCount--;
        }


        return currentIndex;
    }
}
