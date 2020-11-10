using UnityEngine;
using System.Collections;

namespace SeawispHunter.MinibufferConsole {

public class AutoDestroyParticles : MonoBehaviour {

  private void Start() {
#if UNITY_5_5_OR_NEWER
    Destroy(gameObject, GetComponent<ParticleSystem>().main.duration);
#else
    Destroy(gameObject, GetComponent<ParticleSystem>().duration);
#endif
  }

}

}
