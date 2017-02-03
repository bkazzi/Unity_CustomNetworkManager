﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// # 実装された機能
// - 実行内容などのステータスを記録する機能。
// - 開始済みかどうかを検証してから安全に開始する機能。
// - 定期的に自動接続する機能。
// -- 例えば他のスクリプトなどの制御なしに、実行時に自動でホストとして起動することができます。
// -- 例えばクライアントが接続に失敗したとき、自動的に接続させたりすることができます。

// # 実装方針
// base.client.RegisterHandler(MsgType.Error, OnError);
// 形式でエラーメッセージを取得しようとしても具体的なメッセージは得られず、
// 主にエラーコードだけが取得できるので、これを使ったエラーのハンドリングは取り止めました。

// # エラーコードについて
// エラー発生時に取得することができるエラーコードは、
// transport layer NetworkError code: に等しいようです。
// https://docs.unity3d.com/ScriptReference/Networking.NetworkError.html

/// <summary>
/// 自動接続機能などを持った NetworkManager です。
/// </summary>
public class CustomNetworkManager : NetworkManager
{
    #region Struct

    /// <summary>
    /// 実行された内容や現在の状態を表すメッセージ。
    /// </summary>
    public struct UNETStatusMessage
    {
        public DateTime time;
        public string   message;
    }

    #endregion Struct

    #region Enum

    /// <summary>
    /// 接続の種類。
    /// </summary>
    public enum UNETConnectionType
    {
        /// <summary>
        /// サーバーとして接続。
        /// </summary>
        Server = 0,

        /// <summary>
        /// ホストとして接続。
        /// </summary>
        Host = 1,

        /// <summary>
        /// クライアントとして接続。
        /// </summary>
        Client = 2,

        /// <summary>
        /// 接続されていません。
        /// </summary>
        None = 3,
    }

    #endregion Enum

    #region const Field

    protected const string MessageDefault = "…";

    #endregion const Field

    #region Field

    /// <summary>
    /// 自動接続するかどうか。true のとき自動接続します。
    /// </summary>
    public bool autoConnect;

    /// <summary>
    /// 自動接続の種類。
    /// </summary>
    public UNETConnectionType autoConnectionType;

    /// <summary>
    /// 自動接続を試みる場合の試行間隔。
    /// </summary>
    public float autoConnectIntervalTimeSec = 15;

    /// <summary>
    /// 最後に自動接続を試みた時間。
    /// </summary>
    protected float autoConnectPreviousTryTimeSec = 0;

    /// <summary>
    /// 現在の接続の種類。
    /// </summary>
    private UNETConnectionType connectionType;

    /// <summary>
    /// ステータスメッセージの最大保存数。
    /// </summary>
    public int statusMessagesCount = 10;

    /// <summary>
    /// ステータスメッセージ。
    /// 先頭が最新のメッセージになります。
    /// </summary>
    protected List<UNETStatusMessage> statusMessages;

    #endregion Field

    #region Property

    /// <summary>
    /// 現在の接続の種類を取得します。
    /// </summary>
    public UNETConnectionType ConnectionType
    {
        get { return this.connectionType; }
    }

    /// <summary>
    /// ステータスメッセージのリストを取得します。
    /// </summary>
    public List<UNETStatusMessage> StatusMessages
    {
        get { return this.statusMessages; }
    }

    #endregion Propery

    #region Method

    /// <summary>
    /// 初期化時に呼び出されます。
    /// </summary>
    protected virtual void Awake()
    {
        this.connectionType = UNETConnectionType.None;

        this.statusMessages = new List<UNETStatusMessage>();

        AddStatusMessage(CustomNetworkManager.MessageDefault);
    }

    /// <summary>
    /// 開始時に呼び出されます。
    /// </summary>
    protected virtual void Start()
    {
        TryToAutoStart();
    }

    /// <summary>
    /// 更新時に呼び出されます。
    /// </summary>
    protected virtual void Update()
    {
        TryToAutoStart();
    }

    /// <summary>
    /// ステータスメッセージを追加します。
    /// </summary>
    /// <param name="statusMessage">
    /// 追加するメッセージ。
    /// </param>
    protected void AddStatusMessage(string statusMessage)
    {
        AddStatusMessage(new UNETStatusMessage()
        {
            time = DateTime.Now,
            message = statusMessage
        });
    }

    /// <summary>
    /// ステータスメッセージを追加します。
    /// </summary>
    /// <param name="statusMessage">
    /// 追加するメッセージ。
    /// </param>
    protected void AddStatusMessage(UNETStatusMessage statusMessage)
    {
        this.statusMessages.Insert(0, statusMessage);
        TrimStatusMessages();
    }

    /// <summary>
    /// ステータスメッセージをトリムして最大数に合わせます。
    /// </summary>
    protected void TrimStatusMessages()
    {
        int count = this.statusMessages.Count;

        while (count > this.statusMessagesCount)
        {
            this.statusMessages.RemoveAt(count - 1);

            count = count - 1;
        }
    }

    /// <summary>
    /// すべてのステータスメッセージを削除します。
    /// </summary>
    public void ClearStatusMessages()
    {
        this.statusMessages.Clear();
    }

    #region Start Stop

    /// <summary>
    /// 自動接続を試みます。自動接続モードでないときは何も処理されません。
    /// </summary>
    protected virtual void TryToAutoStart()
    {
        // 自動接続が無効であったり、既に接続されているときは処理を抜けます。

        if (!this.autoConnect || this.connectionType != UNETConnectionType.None)
        {
            return;
        }

        float currentTime = Time.timeSinceLevelLoad;

        // 再接続を試みる時間が経過していなかったら処理を抜けます。

        if (this.autoConnectIntervalTimeSec > currentTime - this.autoConnectPreviousTryTimeSec)
        {
            return;
        }

        // 情報を更新して接続を試みます。

        this.autoConnectPreviousTryTimeSec = currentTime;

        AddStatusMessage("Try to Auto Connect.");

        switch (this.autoConnectionType)
        {
            case UNETConnectionType.Server:
                {
                    base.StartServer();
                    break;
                }
            case UNETConnectionType.Host:
                {
                    base.StartHost();
                    break;
                }
            case UNETConnectionType.Client:
                {
                    base.StartClient();
                    break;
                }
        }
    }

    /// <summary>
    /// 既にいずれかの形式で開始されていないかを確認してからサーバーとして開始します。
    /// 既にいずれかの形式で開始されているとき、サーバーとして開始しません。
    /// </summary>
    public virtual void StartServerSafe()
    {
        if (this.connectionType != UNETConnectionType.None)
        {
            AddStatusMessage("Faild to Start Server. : Already Started as " + this.connectionType.ToString());
            return;
        }

        base.StartServer();
    }

    /// <summary>
    /// 既にいずれかの形式で開始されていないかを確認してからホストとして開始します。
    /// 既にいずれかの形式で開始されているとき、ホストとして開始しません。
    /// </summary>
    public virtual void StartHostSafe()
    {
        if (this.connectionType != UNETConnectionType.None)
        {
            AddStatusMessage("Faild to Start Host. : Already Started as " + this.connectionType.ToString());
            return;
        }

        base.StartHost();
    }

    /// <summary>
    /// 既にいずれかの形式で開始されていないかを確認してからクライアントとして開始します。
    /// 既にいずれかの形式で開始されているとき、クライアントとして開始しません。
    /// </summary>
    public virtual void StartClientSafe()
    {
        if (this.connectionType != UNETConnectionType.None)
        {
            AddStatusMessage("Faild to Start Client. : Already Started as " + this.connectionType.ToString());
            return;
        }

        base.StartClient();
    }

    /// <summary>
    /// サーバー、ホスト、クライアントにかかわらず停止処理を実行します。
    /// </summary>
    public virtual void Stop()
    {
        switch (this.connectionType)
        {
            case UNETConnectionType.Server:
                {
                    base.StopServer();
                    break;
                }
            case UNETConnectionType.Host:
                {
                    base.StopHost();
                    break;
                }
            case UNETConnectionType.Client:
                {
                    base.StopClient();
                    break;
                }
            case UNETConnectionType.None:
                {
                    AddStatusMessage("Failed to Stop : Nothing is Started.");
                    break;
                }
        }
    }

    #endregion Start Stop

    // # 実装上の注意 1
    // 以下のメソッドで実行される処理は、base メソッドよりも先に実行されています。
    // base メソッドの中で、引数となる接続情報を破棄する処理などが実行されるためです。

    // # 実装上の注意 2
    // Host として起動する場合、OnStartHost が実行された後に、
    // OnStartServer, OnStartClient が実行される点に注意してください。

    // # 実装上の注意 3
    // 停止するタイミングで this.autoConnectPreviousTryTimeSec をリセットしています。
    // 停止した直後に自動接続が実行されないようにするための措置です。

    #region Override Server

    /// <summary>
    /// サーバーで開始されたときに呼び出されます。
    /// </summary>
    public override void OnStartServer()
    {
        AddStatusMessage("Start Server.");

        if (this.connectionType != UNETConnectionType.Host)
        {
            this.connectionType = UNETConnectionType.Server;
        }

        base.OnStartServer();
    }

    /// <summary>
    /// サーバーで停止されたときに呼び出されます。
    /// </summary>
    public override void OnStopServer()
    {
        AddStatusMessage("Stop Server.");

        this.connectionType = UNETConnectionType.None;

        this.autoConnectPreviousTryTimeSec = Time.timeSinceLevelLoad;

        base.OnStopServer();
    }

    /// <summary>
    /// サーバーでクライアントが接続されたときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    public override void OnServerConnect(NetworkConnection conn)
    {
        AddStatusMessage("Client Connected : " + conn.address);

        base.OnServerConnect(conn);
    }

    /// <summary>
    /// サーバーでクライアントとの接続が切断されたときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        AddStatusMessage("Client Disconnected : " + conn.address);

        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// サーバーでエラーが起きたときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    /// <param name="errorCode">
    /// エラーコード。
    /// </param>
    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        AddStatusMessage("Server Get Error : " + (NetworkError)errorCode);

        base.OnServerError(conn, errorCode);
    }

    #endregion Override Server

    #region Override Host

    /// <summary>
    /// ホストで開始されたときに呼び出されます。
    /// </summary>
    public override void OnStartHost()
    {
        AddStatusMessage("Start Host.");

        this.connectionType = UNETConnectionType.Host;

        base.OnStartHost();
    }

    /// <summary>
    /// ホストで停止したときに呼び出されます。
    /// </summary>
    public override void OnStopHost()
    {
        AddStatusMessage("Stop Host.");

        this.connectionType = UNETConnectionType.None;

        this.autoConnectPreviousTryTimeSec = Time.timeSinceLevelLoad;

        base.OnStopHost();
    }

    #endregion Override Host

    #region Override Client

    /// <summary>
    /// クライアントで開始されたときに呼び出されます。
    /// </summary>
    /// <param name="client">
    /// 該当するクライアント。
    /// </param>
    public override void OnStartClient(NetworkClient client)
    {
        AddStatusMessage("Start Client.");

        if (this.connectionType != UNETConnectionType.Host)
        {
            this.connectionType = UNETConnectionType.Client;
        }

        base.OnStartClient(client);
    }

    /// <summary>
    /// クライアントで停止したときに呼び出されます。
    /// </summary>
    public override void OnStopClient()
    {
        AddStatusMessage("Stop Client.");

        this.connectionType = UNETConnectionType.None;

        this.autoConnectPreviousTryTimeSec = Time.timeSinceLevelLoad;

        base.OnStopClient();
    }

    /// <summary>
    /// クライアントでサーバーに接続したときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    public override void OnClientConnect(NetworkConnection conn)
    {
        AddStatusMessage("Connected to Server. : " + conn.address);

        base.OnClientConnect(conn);
    }

    /// <summary>
    /// クライアントでサーバーとの接続が切断されたときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        AddStatusMessage("Disconnected from Server. : " + conn.address);

        base.OnClientDisconnect(conn);
    }

    /// <summary>
    /// クライアントでエラーが起きたときに呼び出されます。
    /// </summary>
    /// <param name="conn">
    /// 該当する接続情報。
    /// </param>
    /// <param name="errorCode">
    /// エラーコード。
    /// </param>
    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        AddStatusMessage("Client Get Error : " + (NetworkError)errorCode);

        base.OnClientError(conn, errorCode);
    }

    #endregion Override Client

    #endregion Method
}