using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Diagnostics;
using ToyParty.System;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ToyParty
{
    public enum MatchType
    {
        Default, // 일반적인 게임룰, 3개 이상의 같은 블록이 모였을 때 메치됨.
        Special, // 이 게임의 특수 블록, 기본적인 매치 조건과 마찬가지로, 특수블록 타입끼리 매치될 수 있음 
        Obstacle // 자신은 매치조건에 포함되지 않고, 주변부 블록이 매치되어 파괴되면, 영향을 받는 타입.
    }

    [Serializable]
    public struct BlockData
    {
        public Color color;
        public Sprite sprite;
        public GameObject prefab;
        public MatchType matchType;
        [ShowIf("@this.matchType == MatchType.Obstacle")]
        public int durability;
    }

    public static class BlockEx
    {
        public static bool IsSpawnable(this Block block)
        {
            return block.Data.matchType == MatchType.Default;
        }
    }

    public class Block : TileBase
    {
        [SerializeField]
        private BlockData blockData;
        public BlockData Data { get => blockData; }

        public GameObject Instanciate()
        {
            GameObject instanciate = GameObject.Instantiate(Data.prefab);
            instanciate.gameObject.SetActive(false);
            instanciate.transform.SetParent(PoolingManager.Instance.transform);

            var behaviour = instanciate.GetComponent<BlockBehaviour>();
            behaviour.Init(name, GameManager.Instance.DropPoint, Data);
            
            return instanciate;
        }

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if(go != null)
            {
                var behaviour = go.GetComponent<BlockBehaviour>();
                behaviour.Init(name, position, Data);
                GameManager.Instance.Board.AddBlockFromLevel(position, name, behaviour);
            }

            return base.StartUp(position, tilemap, go);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.gameObject = Data.prefab;
            tileData.color = Data.color;
            
            if(Application.isPlaying)
            {
                tileData.sprite = null;
            }
            else
            {
                tileData.sprite = Data.sprite;
            }

            tileData.flags = TileFlags.LockColor | TileFlags.InstantiateGameObjectRuntimeOnly;
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/ToyParty/Block")]
        public static void CreateBlock()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save ToyParty Block", "ToyParty Block", "Asset", "Save ToyParty Block", "Assets");
            if (path == "")
                return;
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Block>(), path);
        }
#endif
    }

}

