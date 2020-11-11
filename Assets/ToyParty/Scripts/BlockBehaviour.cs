using DG.Tweening;
using Lean.Touch;
using System.Collections;
using System.Collections.Generic;
using ToyParty;
using UnityEngine;

public class BlockBehaviour : LeanSelectableBehaviour
{
    private LeanFinger current = null;
    private float touchedTime = 0.0f;
    private Vector3 worldPosition;

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

}
