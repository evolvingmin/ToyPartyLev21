using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Diagnostics;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ToyParty
{
    // 이리저리 굴려본 결과 레벨 배치 전용으로만 활용하는게 좋다.
    public class Block : TileBase
    {
        [SerializeField]
        private Color color;

        [SerializeField]
        private Sprite sprite;

        [SerializeField]
        private GameObject prefab = null;

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if(go != null)
            {
                var behaviour = go.GetComponent<BlockBehaviour>();
                behaviour.Init(name, position, sprite, color);
                GameManager.Instance.Board.AddBlockFromLevel(position, name, behaviour);
            }

            return base.StartUp(position, tilemap, go);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.gameObject = prefab;
            tileData.color = color;
            
            if(Application.isPlaying)
            {
                tileData.sprite = null;
            }
            else
            {
                tileData.sprite = sprite;
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

