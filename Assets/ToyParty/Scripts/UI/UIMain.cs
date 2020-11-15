using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ToyParty.Patterns;
using SeawispHunter.MinibufferConsole;

using Doozy.Engine.UI;


namespace ToyParty.UI
{
    public class UIMain : GMReceiveBehaviour
    {
        [SerializeField]
        private UIPopup levelClearPopup;

        private void Awake()
        {
            Minibuffer.Register(this);
        }

        public void OnClickButton_PlayGain()
        {
            ReloadMainScene();
        }

        private async void ReloadMainScene()
        {
            await SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
        }

        public override void Receive_LevelComplete()
        {
            levelClearPopup.Show();
        }

        [Command]
        public void InvokeLevelComplete()
        {
            Messenger.Instance.Send_LevelComplete();
        }
    }

}
