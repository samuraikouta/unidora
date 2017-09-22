using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour {
	// ゲームタイプ名
	const string GameTypeName = "SenKuSya";

	// ローカルIPアドレスとポート
	const string LocalServerIP = "127.0.0.1"; // 開発用
	const int ServerPort = 25000;

	string playerName;
	string gameServerName;

	// Use this for initialization
	void Start () {
		playerName = "Player" + Random.Range(0, 99999999).ToString();
		gameServerName = "Server" + Random.Range(0, 99999999).ToString();
		UpdateHostList();
	}

	// 状態
	public enum Status
	{
		NoError, // エラーなし

		LaunchingServer, // サーバー起動中
		ServerLaunched, // サーバーが起動に成功
		LaunchServerFailed, // サーバーの起動に失敗

		ConnectingToServer, // サーバーに接続中
		ConnectedToServer, // サーバーに接続成功
		ConnectToServerFailed, // サーバーへの接続に失敗

		DisconnectedFromServer, // サーバーから切断された
	};

	Status _status = Status.NoError;
	public Status status { get { return _status;} private set { _status = value;}}

	// サーバーを起動する
	public void LaunchServer(string roomName)
	{
		status = Status.LaunchingServer;
		StartCoroutine(LaunchServerCoroutine(gameServerName));
	}


	bool useNat = false; // natパンチスルーを使用するか
	IEnumerator CheckNat()
	{
		bool doneTesting = false; // 接続テストが終わったか
		bool probingPublicIP = false;
		float timer = 0;
		useNat = false;

		// 接続テストをしてNATパンチスルーが必要か調べる
		while (!doneTesting){
			ConnectionTesterStatus connectionTestResult = Network.TestConnection();
			switch (connectionTestResult){
				case ConnectionTesterStatus.Error:
					// 問題が発生した
					doneTesting = true;
					break;

				case ConnectionTesterStatus.Undetermined:
					// 調査中
					doneTesting = false;
					break;

				case ConnectionTesterStatus.PublicIPIsConnectable:
					// パブリックIPアドレスを持っているのでNATパンチスルーは使わなくていい
					useNat = false;
					doneTesting = true;
					break;

				case ConnectionTesterStatus.PublicIPPortBlocked:
					// パブリックIPアドレスを持っているようだがポートがブロックされていて接続できない
					useNat = false;
					if (!probingPublicIP){
						connectionTestResult = Network.TestConnectionNAT();
						probingPublicIP = true;
						timer = Time.time + 10;
					}

					else if (Time.time > timer){
						probingPublicIP = false; // reset
						useNat = true;
						doneTesting = true;
					}
					break;

				case ConnectionTesterStatus.PublicIPNoServerStarted:
					// パブリックIPアドレスを持っているがサーバーが起動していない
					break;

				case ConnectionTesterStatus.LimitedNATPunchthroughPortRestricted:
				case ConnectionTesterStatus.LimitedNATPunchthroughSymmetric:
					// NATパンチスルーに制限がある
					// サーバーに接続できないクライアントがあるかもしれない
					useNat = true;
					doneTesting = true;
					break;

				case ConnectionTesterStatus.NATpunchthroughAddressRestrictedCone:
				case ConnectionTesterStatus.NATpunchthroughFullCone:
					// NATパンチスルーによりサーバーとクライアントは問題なく接続できる
					useNat = true;
					doneTesting = true;
					break;

				default:
					Debug.Log("Error in test routine, got" + connectionTestResult);
					break;
			}
			yield return null;
		}
	}

	// サーバーを起動するコルーチン
	IEnumerator LaunchServerCoroutine(string roomName)
	{
		yield return StartCoroutine(CheckNat());

		// サーバーを起動する
		NetworkConnectionError error = Network.InitializeServer(32, ServerPort, useNat);
		if (error != NetworkConnectionError.NoError){
			Debug.Log("Can't Launch Server");
			status = Status.LaunchServerFailed;
		} else {
			// マスターサーバーにゲームサーバーを登録する
			MasterServer.RegisterHost(GameTypeName, gameServerName);
		}
	}

	// サーバーに接続する
	public void ConnectToServer(string serverGuid, bool connectLocalServer)
	{
		status = Status.ConnectingToServer;
		if (connectLocalServer){
			Network.Connect(LocalServerIP, ServerPort);
		} else {
			Network.Connect(serverGuid);
		}
	}

	// サーバーが起動した
	void OnServerInitialized()
	{
		status = Status.ServerLaunched;
	}

	// サーバーに接続した
	void OnConnectedToServer()
	{
		status = Status.ConnectedToServer;
	}

	// サーバーへの接続に失敗した
	void OnFailedToConnect(NetworkConnectionError error){
		Debug.Log("FailedToConnect: " + error.ToString());
		status = Status.ConnectToServerFailed;
	}

	// プレイヤーが切断した
	// (サーバーが動作しているコンピュータで呼び出される)
	void OnPlayerDisconnected(NetworkPlayer player){
		Network.RemoveRPCs(player);
		Network.DestroyPlayerObjects(player);
	}

	// サーバーから切断された
	void OnDisconnectedFromServer(NetworkDisconnection info){
		Debug.Log("DisconnectedFromServer:" + info.ToString());
		status = Status.DisconnectedFromServer;
		Application.LoadLevel(0);
	}

	// ステータスを得る
	public Status GetStatus()
	{
		return status;
	}

	// プレイヤーネームを得る
	public string GetPlayerName()
	{
		return playerName;
	}

	void OnDestroy()
	{
		if (Network.isServer){
			MasterServer.UnregisterHost();
			Network.Disconnect();
		}
	}

	// ----------- ロビー関連 -----------
	// マスターサーバーに登録されているゲームサーバーのリストを更新する
	public void UpdateHostList()
	{
		MasterServer.ClearHostList();
		MasterServer.RequestHostList(GameTypeName);
	}

	// マスターサーバーに登録されているゲームサーバーのリストを取得する
	public HostData[] GetHostList()
	{
		return MasterServer.PollHostList();
	}

	// マスターサーバーとNATファシリテーターのIPアドレスを設定する
	void SetMasterServerAndNatFacilitatorIP(string masterServerAddress, string facilitatorAddress)
	{
		MasterServer.ipAddress = masterServerAddress;
		Network.natFacilitatorIP = facilitatorAddress;
	}

	// マスターサーバーへの登録を削除する
	public void UnregisterHost()
	{
		MasterServer.UnregisterHost();
	}

	// ----------- 設定GUI -----------
	void OnGUI()
	{
		if ((Network.isServer || Network.isClient))
			return;

		// 高さ480の(0,0)中心を基準にする
		float scale = Screen.height / 480.0f;
		GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0), Quaternion.identity, new Vector3(scale, scale, 1.0f));

		GUI.Window(0, new Rect(-200, -200, 400, 400), NetworkSettingWindow, "Network Setting");
	}

	Vector2 scrollPosition;

	void NetworkSettingWindow(int windowID){
		// プレイヤー名の設定
		GUILayout.BeginHorizontal();
		GUILayout.Label("Player Name: ");
		playerName = GUILayout.TextField(playerName, 32);
		GUILayout.EndHorizontal();

		// ゲームサーバー名の設定
		GUILayout.BeginHorizontal();
		GUILayout.Label("Game Server Name: ");
		gameServerName = GUILayout.TextField(gameServerName, 32);
		GUILayout.EndHorizontal();

		// ゲームサーバーを起動する
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Launch"))
			LaunchServer(gameServerName);
		GUILayout.EndHorizontal();
		GUILayout.Space(20);

		// ゲームサーバーリスト
		GUILayout.BeginHorizontal();
		GUILayout.Label("Game Server List (Click To Connect):");
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Refresh"))
			UpdateHostList();
		GUILayout.EndHorizontal();

		scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(380), GUILayout.Height(200));
		HostData[] hosts = GetHostList(); // サーバ一覧を取得
		if (hosts.Length > 0){
			foreach (HostData host in hosts)
				if (GUILayout.Button(host.gameName, GUI.skin.box, GUILayout.Width(360)))
					ConnectToServer(host.guid, false);
		} else {
			GUILayout.Label("No Server");
		}
		GUILayout.EndScrollView();

		// ローカルサーバーに接続
		if (GUILayout.Button("Connect Local Server")){
			ConnectToServer("", true);
		}

		// ステータスの表示
		GUILayout.Label("Status: " + status.ToString());
			
	}
}
