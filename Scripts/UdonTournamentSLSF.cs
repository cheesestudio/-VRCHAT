
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class UdonTournamentSLSF : UdonSharpBehaviour
{
    [UdonSynced, TextArea] public string syncPlayers;
    public Animator controlAnimator;
    public string animatorIntParameterName;
    public TextMeshProUGUI[] playerTexts = new TextMeshProUGUI[64];
    public TextMeshProUGUI ownerText;
    public string creatorName;
    //Filter
    private DataList allPlayersData = new DataList();
    private DataList joinedPlayersData = new DataList();
    public TextMeshProUGUI[] showDisallowPlayer = new TextMeshProUGUI[64];
    public UnityEngine.UI.Button[] showDisallowButton = new UnityEngine.UI.Button[64];
    public TextMeshProUGUI addPlayerText;
    public TextMeshProUGUI[] showAllowPlayer = new TextMeshProUGUI[64];
    public UnityEngine.UI.Button[] showAllowButton = new UnityEngine.UI.Button[64];
    public TextMeshProUGUI delPlayerText;
    private DataList missPlayersData = new DataList();
    //Owner
    public TextMeshProUGUI requestText;
    [TextArea] public string preSelentText;
    public string inSelentText;
    private bool startBool = false;
    private bool selentMode = false;

    void Start()
    {
        SetOwnerText();
        requestText.text = preSelentText;
    }
    public void AddPlayer()
    {
        if (Networking.IsOwner(gameObject))
        {
            var player = showDisallowPlayer[Convert.ToInt32(addPlayerText.text)].text;
            if (!selentMode)
            {
                var index = joinedPlayersData.IndexOf(player);
                var indexOfEmpty = joinedPlayersData.IndexOf("");
                if (indexOfEmpty != -1)
                {
                    joinedPlayersData[indexOfEmpty] = player;
                }
                else
                {
                    joinedPlayersData.Add(player);
                }
                allPlayersData.Remove(player);
                PlayerDataRead();
            }
            else if (!string.IsNullOrEmpty(player))
            {
                requestText.text = player;
                selentMode = false;
            }
            else
            {
                requestText.text = preSelentText;
                selentMode = false;
            }
        }
    }
    public void DelPlayer()
    {
        if (Networking.IsOwner(gameObject))
        {
            var player = showAllowPlayer[Convert.ToInt32(delPlayerText.text)].text;
            if (!selentMode)
            {
                if (showAllowPlayer[Convert.ToInt32(delPlayerText.text)].color != Color.red && !string.IsNullOrEmpty(player))
                {
                    allPlayersData.Add(player);
                }
                else
                {
                    missPlayersData.Remove(player);
                }
                var index = joinedPlayersData.IndexOf(player);
                joinedPlayersData[index] = "";
                showAllowPlayer[Convert.ToInt32(delPlayerText.text)].color = Color.white;
                PlayerDataRead();
            }
            else if (showAllowPlayer[Convert.ToInt32(delPlayerText.text)].color != Color.red)
            {
                requestText.text = player;
                selentMode = false;
            }
            else
            {
                requestText.text = preSelentText;
                selentMode = false;
            }
        }
    }
    public void ClearEmpty()
    {
        if (Networking.IsOwner(gameObject))
        {
            while (joinedPlayersData.IndexOf("") != -1)
            {
                joinedPlayersData.RemoveAt(joinedPlayersData.IndexOf(""));
            }
            PlayerDataRead();
        }
    }
    public void ClearJoined()
    {
        if (Networking.IsOwner(gameObject))
        {
            for (int m = 0; m < joinedPlayersData.Count; m++)
            {
                if (showAllowPlayer[m].color == Color.red)
                {
                    showAllowPlayer[m].color = Color.white;
                    continue;
                }
                else if (string.IsNullOrEmpty(showAllowPlayer[m].text)) continue;
                allPlayersData.Add(joinedPlayersData[m]);
            }
            joinedPlayersData = new DataList();
            PlayerDataRead();
        }
    }
    public void ClearMiss()
    {
        if (Networking.IsOwner(gameObject))
        {
            for (int m = 0; m < joinedPlayersData.Count; m++)
            {
                for (int n = 0; n < missPlayersData.Count; n++)
                {
                    if (joinedPlayersData[m] == missPlayersData[n])
                    {
                        joinedPlayersData.RemoveAt(m);
                        m--;
                        break;
                    }
                }
            }
            PlayerDataRead();
        }
    }
    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            var index = joinedPlayersData.IndexOf(player.displayName);
            if (index != -1)
            {
                missPlayersData.Remove(player.displayName);
            }
            else if (allPlayersData.Count < playerTexts.Length)
            {
                var p = allPlayersData.IndexOf(player.displayName);
                if (p == -1)
                {
                    allPlayersData.Add(player.displayName);
                }
            }
            PlayerDataRead();
        }
    }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Networking.IsOwner(gameObject))
        {
            allPlayersData.Remove(player.displayName);
            var index = joinedPlayersData.IndexOf(player.displayName);
            if (index != -1)
            {
                missPlayersData.Add(player.displayName);
            }
            PlayerDataRead();
            SetOwnerText();
        }
    }
    public void AcceptOwner()
    {
        if (Networking.IsOwner(gameObject) && !string.IsNullOrEmpty(requestText.text) && requestText.text != preSelentText && requestText.text != inSelentText)
        {
            VRCPlayerApi[] allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
            VRCPlayerApi.GetPlayers(allPlayers);
            foreach (var player in allPlayers)
            {
                if (player.displayName == requestText.text)
                {
                    Networking.SetOwner(player, gameObject);
                    break;
                }
            }
        }
    }
    public override bool OnOwnershipRequest(VRCPlayerApi requester, VRCPlayerApi newOwner)
    {
        if (Networking.IsOwner(requester, gameObject))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        SetOwnerText();
    }
    public void SetOwnerText()
    {
        var player = Networking.GetOwner(gameObject);
        ownerText.text = "[" + player.playerId + "] " + player.displayName;
    }
    void PlayerDataRead()
    {
        //JoinedPlayer
        foreach (var show in showAllowPlayer)
        {
            show.enabled = false;
        }
        foreach (var show in showAllowButton)
        {
            show.enabled = false;
        }
        for (int m = 0; m < joinedPlayersData.Count; m++)
        {
            showAllowPlayer[m].enabled = true;
            showAllowPlayer[m].text = joinedPlayersData[m].ToString();
            showAllowButton[m].enabled = true;
            bool isMiss = false;
            for (int n = 0; n < missPlayersData.Count; n++)
            {
                if (joinedPlayersData[m] == missPlayersData[n]) isMiss = true;
            }
            if (isMiss)
            {
                showAllowPlayer[m].color = Color.red;
            }
            else
            {
                showAllowPlayer[m].color = Color.white;
            }
        }
        //AllPlayer
        foreach (var show in showDisallowPlayer)
        {
            show.enabled = false;
        }
        foreach (var show in showDisallowButton)
        {
            show.enabled = false;
        }
        if (joinedPlayersData.Count != 0)
        {
            int o = 0;
            for (int m = 0; m < allPlayersData.Count; m++)
            {
                bool equal = false;
                for (int n = 0; n < joinedPlayersData.Count; n++)
                {
                    if (allPlayersData[m].ToString() == joinedPlayersData[n].ToString())
                    {
                        equal = true;
                    }
                }
                if (!equal)
                {
                    showDisallowPlayer[o].enabled = true;
                    showDisallowButton[o].enabled = true;
                    showDisallowPlayer[o].text = allPlayersData[m].ToString();
                    o++;
                }
            }
        }
        else
        {
            for (int m = 0; m < allPlayersData.Count; m++)
            {
                showDisallowPlayer[m].enabled = true;
                showDisallowButton[m].enabled = true;
                showDisallowPlayer[m].text = allPlayersData[m].ToString();
            }
        }
        if (Networking.IsOwner(gameObject))
        {
            DataList allData = new DataList();
            allData.Add(allPlayersData);
            allData.Add(joinedPlayersData);
            allData.Add(missPlayersData);
            if (VRCJson.TrySerializeToJson(allData, JsonExportType.Minify, out DataToken exportToken))
            {
                syncPlayers = exportToken.ToString();
                RequestSerialization();
            }
        }
        //Place
        foreach (var uiText in playerTexts)
        {
            uiText.text = "";
        }
        //在最小二叉树+1的基础上补
        int result, num;
        if (joinedPlayersData.Count <= 1)
        {
            result = 1;
            num = 2;
        }
        else
        {
            result = (int)Math.Ceiling(Math.Log(joinedPlayersData.Count, 2));  // 计算以2为底的对数并向上取整
            num = (int)Math.Pow(2, result);
        }
        int temp = 0;
        int index = 0;
        bool revert = false;
        if (joinedPlayersData.Count <= 2)// || joinedPlayersData.Count == num)
        {
            for (int p = 0; p < joinedPlayersData.Count; p++)
            {
                playerTexts[p].text = joinedPlayersData[p].ToString();
                if (showAllowPlayer[p].color == Color.red)
                {
                    playerTexts[p].color = Color.red;
                }
                else
                {
                    playerTexts[p].color = Color.white;
                }
            }
        }
        else
        {
            while (temp < joinedPlayersData.Count)
            {
                if (index >= num  && revert == false)
                {
                    index = 1;
                    revert = true;
                }
                //Debug.Log("temp = " + temp + "index = " + index + "num = " + num + "revert = " + revert);
                playerTexts[index].text = joinedPlayersData[temp].ToString();
                if (showAllowPlayer[temp].color == Color.red)
                {
                    playerTexts[index].color = Color.red;
                }
                else
                {
                    playerTexts[index].color = Color.white;
                }
                index += 2;
                temp++;
            }
        }
        //for (int p = 0; p < joinedPlayersData.Count; p++)
        //{
        //    playerTexts[p].text = joinedPlayersData[p].ToString();
        //    if (showAllowPlayer[p].color == Color.red)
        //    {
        //        playerTexts[p].color = Color.red;
        //    }
        //    else
        //    {
        //        playerTexts[p].color = Color.white;
        //    }
        //}
        controlAnimator.SetInteger(animatorIntParameterName, joinedPlayersData.Count);
    }
    public override void OnDeserialization()
    {
        if (VRCJson.TryDeserializeFromJson(syncPlayers, out DataToken syncToken))
        {
            allPlayersData = syncToken.DataList[0].DataList;
            joinedPlayersData = syncToken.DataList[1].DataList;
            missPlayersData = syncToken.DataList[2].DataList;
            PlayerDataRead();
            if (Networking.GetOwner(gameObject).displayName != creatorName)
            {
                if (!startBool && Networking.LocalPlayer.displayName == creatorName)
                {
                    startBool = true;
                    SendCustomNetworkEvent(NetworkEventTarget.Owner, "SetCreatorToOwner");
                }
            }
        }
    }
    public void SetCreatorToOwner()
    {
        VRCPlayerApi[] allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(allPlayers);
        foreach (var player in allPlayers)
        {
            if (player.displayName == creatorName)
            {
                Networking.SetOwner(player, gameObject);
                break;
            }
        }
    }
    public void SelentPlayer()
    {
        if (Networking.IsOwner(gameObject))
        {
            selentMode = !selentMode;
            if (selentMode)
            {
                requestText.text = inSelentText;
            }
            else
            {
                requestText.text = preSelentText;
            }
        }
    }

    public void Shuffle()
    {
        if (Networking.IsOwner(gameObject))
        {
            System.Random rng = new System.Random();
            int n = joinedPlayersData.Count;

            // 使用 Fisher-Yates 洗牌算法
            for (int i = n - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);  // 生成一个随机索引
                                             // 交换 i 和 j 元素
                var temp = joinedPlayersData[i];
                joinedPlayersData[i] = joinedPlayersData[j];
                joinedPlayersData[j] = temp;
            }
            PlayerDataRead();
        }
    }
}
