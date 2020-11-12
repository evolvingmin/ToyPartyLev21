using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToyParty;
using ToyParty.Collections;
using ToyParty.Patterns;
using UnityEngine;

namespace ToyParty.System
{
    // 해당 프로젝트에서 임시적으로 활용하기 위해 작성하는 오브젝트 풀링 시스템
    // 
    public class PoolingManager : SingletonMono<PoolingManager>
    {
        [ShowInInspector]
        private Dictionary<string, Pool<GameObject>> pools = new Dictionary<string, Pool<GameObject>>();

        public void AddInstantiatedItem(string tag, GameObject instanciated)
        {
            if (pools.ContainsKey(tag) == false)
            {
                pools[tag] = new Pool<GameObject>();
            }

            pools[tag].PushNewItem(instanciated);
        }

        public void StoreObject(string tag, GameObject store)
        {
            if (pools.ContainsKey(tag) == false)
            {
                Debug.Log($"{tag} is not pooled ");
                return;
            }
            store.SetActive(false);
            store.transform.SetParent(this.transform);

            pools[tag].Store(store);
        }
        
    }
}
