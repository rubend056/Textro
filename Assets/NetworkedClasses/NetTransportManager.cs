using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class NetTransportManager : MonoBehaviour{


	//NetworkIdentity something;

	//GeneralVariables
	public static int unreliableChannel;
	public static int reliableChannel;
	private bool connected = false;
	private int thisSocket = 0;
	private Coroutine connWaitCoroutine;

	//ServerVariables
	int serverSocket;
	public List<PlayerInfo> playerInfoList;
	private static int uniqueIDIndex = 1;

	//Clientvariables
	int clientSocket;
	public string ipAddress = "127.0.0.1";
	int clientConnectionID;

	//Additional Variables
	public static PlayerInfo thisPInfo;
	public static bool client = false;
	public static bool server = false;
	public int localPort = 2222;
	public int remotePort = 2222;

	public enum DataType {ConnAffirmation, Disconnect, Text, PlayerListUpdate, Relay, RelayS};

	//For a global NetworkTransportManager instance
	public static NetTransportManager instance;


	void Awake(){
		instance = this;
	}

	public virtual void Start(){
		
		Application.runInBackground = true;
		InitNetwork ();
		thisPInfo = new PlayerInfo (true, "Me");
		playerInfoList = new List<PlayerInfo> ();
	}

	#region Update, StartServer and StartClient Functions
	//run every frame
	public virtual void Update(){
		if (client || server)
			updateNetPackages ();

		if (closeConnection) {
			if (client) {
				NetworkTransport.RemoveHost (clientSocket);
			} else if (server) {
				NetworkTransport.RemoveHost (serverSocket);
			}
			clientConnectionID = 0;
			playerInfoList.Clear ();
//			StopAllCoroutines ();
			server = false;
			client = false;
			NetIdentityCustom.server = false;
			NetIdentityCustom.client = false;
//			playerUniqueID = 0;
			uniqueIDIndex = 1;
			setConnected (false);
			closeConnection = false;
		}
	}
	public virtual void startAsServer(){
		//Check that neither one started
		if (server || client)
			return;

		//Setting up server
		server = true;
		NetIdentityCustom.server = true;
		SocketSetup ();

		setConnected (true);
	}
		
	public virtual void startAsClient(int offset = 1){
		//Check that neither one started
		if (server || client)
			return;
		
		//Settting up the Client
		client = true;
		NetIdentityCustom.client = true;
		SocketSetup (offset);
		Connect ();
	}
	#endregion

	private void setConnected(bool state){
		if(connected == !state)
			ConnStateChangeEvent (state);
		connected = state;
	}


	// Disabled the coroutine to enable updating per frame
//	private IEnumerator receiverCoroutine(){
//		while (client || server) {
//			yield return new WaitForSeconds (0.1f);
//			updateMessages ();
//		}
//	}

	void updateNetPackages(){
		int outHostId;
		int outConnectionId;
		int outChannelId;
		byte[] buffer = new byte[1024];
		int bufferSize = 1024;
		int receiveSize;
		byte error;

		NetworkEventType evnt = NetworkTransport.Receive(out outHostId, out outConnectionId, out outChannelId, buffer, bufferSize, out receiveSize, out error);
		while (evnt != NetworkEventType.Nothing) {

			if (((server && outHostId == serverSocket) || (client && outHostId == clientSocket))  && (NetworkError)error == NetworkError.Ok) {	//Just making sure packet came through our socket and no errors were thrown

				switch (evnt) {
				case NetworkEventType.ConnectEvent:

					if (client) {
						ServerRequestConnect (outConnectionId);
					} else {
						ClientRequestConnect (outConnectionId);
					}
					break;



				case NetworkEventType.DisconnectEvent:
					if (client && outConnectionId == clientConnectionID) {
						ServerRequestDisconnect ();
					} else if (findWithConnID (outConnectionId) != -1) {
						ClientRequestDisconnect (outConnectionId);
					}
					break;
			
				case NetworkEventType.DataEvent:
					ReceiveDataBasicEvent (outHostId, outConnectionId, outChannelId, buffer, receiveSize);
					break;
				}
			}
			evnt = NetworkTransport.Receive (out outHostId, out outConnectionId, out outChannelId, buffer, bufferSize, out receiveSize, out error);
		}
	}

	#region SendFunctions

	public void sendRelay(byte[] toSend, int channel, int[] uniqueIDs){
		if (!isClient ())
			return;
		send (ByteHelper.Combine (
				System.BitConverter.GetBytes ((int)DataType.Relay), 
				ByteHelper.intArrayBytes (uniqueIDs), 
				toSend),
			clientConnectionID
		);
	}
	public void sendRelay(byte[] toSend, int[] uniqueIDs){
		sendRelay (toSend, reliableChannel, uniqueIDs);
	}

	public void sendRelayS(byte[] toSend, int channel, int SUniqueID, int[] DUniqueIDs){
		if (!isServer ())
			return;
		for (int i = 0; i < DUniqueIDs.Length; i++) {
			PlayerInfo pi = playerInfoList [findWithUniqueID (DUniqueIDs [i])];
			send (
				ByteHelper.Combine (
					System.BitConverter.GetBytes ((int)DataType.RelayS), 
					System.BitConverter.GetBytes  (SUniqueID), 
					toSend),
				pi.connID
			);
		}

	}
	public void sendRelayS(byte[] toSend, int SUniqueID, int[] DUniqueIDs){
		sendRelayS(toSend, reliableChannel,SUniqueID,DUniqueIDs);
	}

	public void sendBasicAllBut (byte[] toSend, int channel, int connIDExeption){
		if (client)
			return;
		if (isConnected()) {
			foreach (PlayerInfo playerInfo in playerInfoList) {
				if (playerInfo.connID != connIDExeption) {
					send (toSend, channel, playerInfo.connID);
				}
			}
		}
	}

	public void sendBasicAllBut(byte[] toSend, int connIDExeption){
		sendBasicAllBut (toSend, reliableChannel, connIDExeption);
	}

	public void sendBasicAll (byte[] toSend, int channel){
		if (isConnected()) {
			if (client) {
				send (toSend, channel, clientConnectionID);
			} else if (server) {
				foreach (PlayerInfo playerInfo in playerInfoList) {
					send(toSend, channel, playerInfo.connID);
				}
			}
		}
	}

	public void sendBasicAll (byte[] toSend){
		sendBasicAll (toSend, reliableChannel);
	}

	public void send(byte[] toSend, int channel, int connID ){
		byte error;
		NetworkTransport.Send (thisSocket, connID, reliableChannel, toSend, toSend.Length, out error);
		NetErrorEvent (error);
	}
	public void send(byte[] toSend, int connID){
		send (toSend, reliableChannel, connID);
	}

	#endregion

	#region Connect/Disconnect

	public virtual void ClientRequestConnect(int outConnectionId){
		setConnected (true);

		ByteContructor bc = new ByteContructor();
		bc.add (System.BitConverter.GetBytes ((int)DataType.ConnAffirmation));
		send (bc.bytesList.ToArray(), outConnectionId);
	}
	public virtual void ServerRequestConnect(int outConnectionId){
		setConnected (true);
	}

	public virtual void ServerRequestDisconnect(){
		StopAllConnections (true);
		//Something to do when server disconnected;
		playerInfoList.Clear ();
	}
	public virtual void ClientRequestDisconnect(int outConnectionId){
		int index = findWithConnID (outConnectionId);
		playerInfoList.RemoveAt (index);

//		if (playerInfoList.Count > 0)
//			setConnected (true);
//		else
//			setConnected (false);

		//		int uniqueIDL = findWithConnID (outConnectionId);
		//		updatePlayerList (NetRequestHandler.ListRequestType.Remove, uniqueIDL, "");
	}

	#endregion

	#region FindFunctions

	public int findWithConnID (int connID)
	{
		for (int i = 0; i < playerInfoList.Count; i++) {
			if (playerInfoList [i].connID == connID)
				return i;
		}
		return -1;
	}

	public int findWithUniqueID (int uniqueID)
	{
		for (int i = 0; i < playerInfoList.Count; i++) {
			if (playerInfoList [i].uniqueID == uniqueID)
				return i;
		}
		return -1;
	}

	#endregion

	#region Setup/EndAll connecttions

	void SocketSetup (int offset = 1){

		// Create a connection_config and add a Channel.
		ConnectionConfig connection_config = new ConnectionConfig ();
		reliableChannel = connection_config.AddChannel (QosType.Reliable);
		unreliableChannel = connection_config.AddChannel (QosType.Unreliable);

		// Create a topology based on the connection config.
		HostTopology topology = new HostTopology (connection_config, 10);

		// Create a host based on the topology we just created, bound to a specific port
		if (client) {
			clientSocket = NetworkTransport.AddHost (topology, localPort + offset);
			thisSocket = clientSocket;
		} else {
			serverSocket = NetworkTransport.AddHost (topology, localPort);
			thisSocket = serverSocket;
		}
	}

	public virtual void InitNetwork (){
		GlobalConfig config = new GlobalConfig ();
		config.MaxPacketSize = 2048;
		NetworkTransport.Init (config);
	}

	public virtual void Connect (){
		byte error;
		clientConnectionID = NetworkTransport.Connect (clientSocket, ipAddress, remotePort, 0, out error);
		NetErrorEvent (error);
		connWaitCoroutine = StartCoroutine (connWaitFunc ());
	}

	private bool closeConnection = false;
	public virtual void StopAllConnections (bool serverRequested)
	{
		if (!serverRequested)
			Disconnect ();

		closeConnection = true;
	}

	private void Disconnect(){


		if (isConnected()) {
//			ByteContructor bc = new ByteContructor();
//			bc.add (System.BitConverter.GetBytes ((int)DataType.Disconnect));
//			if (isClient ())
//				send (bc.bytes.ToArray (), clientConnectionID);
//			else
//				foreach (PlayerInfo playerInfo in playerInfoList) {
//					send (bc.bytes.ToArray (), playerInfo.connID);
//				}
			byte error;
			if (isClient ()) {
				NetworkTransport.Disconnect (thisSocket, clientConnectionID, out error);
				NetErrorEvent (error);
			} else if (isServer ()) {
				foreach (PlayerInfo playerInfo in playerInfoList) {
					NetworkTransport.Disconnect (thisSocket, playerInfo.connID, out error);
					NetErrorEvent (error);
				}
			}
		}
	}

	#endregion

	#region Events

	public virtual void ConnStateChangeEvent(bool state){

	}
	public virtual void ReceiveDataEvent(PlayerInfo playerInfo, byte[] data, int index = 0){
		ByteReceiver r = new ByteReceiver (data, index);
		switch ((DataType)r.getInt ()) {
		case DataType.ConnAffirmation: 
			ReceiveConnAffirmation ();
			break;
		case DataType.Relay:
			int[] intA = r.getIntArray ();
			sendRelayS (ByteHelper.RemoveBefore (r.data, r.index), playerInfo.uniqueID, intA);
			break;
		case DataType.RelayS:
			int uniqueID = r.getInt();
			ReceiveDataEvent(playerInfoList[findWithUniqueID(uniqueID)], data, r.index);
			break;
		}
	}
	public virtual void ReceiveDataBasicEvent (int hostId, int connectionID, int channelId, byte[] data, int size){
		ReceiveDataEvent (playerInfoList [findWithConnID (connectionID)], data);
	}
	public virtual void ReceiveConnAffirmation(){
		StopCoroutine (connWaitCoroutine);
	}
	public virtual void NetErrorEvent(byte error){
		
	}
	public virtual void CouldNConnect(){
		StopAllConnections (true);
	}


	#endregion

	public bool isConnected(){
		return connected;
	}

	public bool isClient(){
		if (!connected)
			return false;
		else
			return client;
	}
	public bool isServer(){
		if (!connected)
			return false;
		else
			return server;
	}

	public int generateUniqueID(){
		return uniqueIDIndex++;
	}

	void OnApplicationQuit(){
		StopAllConnections (false);
		NetworkTransport.Shutdown ();
	}

	IEnumerator connWaitFunc(){
		yield return new WaitForSeconds (1.5f);
		CouldNConnect ();
	}

	//	public void updatePlayerList (NetRequestHandler.ListRequestType type, int uniqueID, string name){
	//		switch (type) {
	//		case NetRequestHandler.ListRequestType.Add:	
	//			if (findWithUniqueID (uniqueID) == -1) {
	//				playerInfoList.Add (new PlayerInfo (uniqueID, name));
	//			}
	//			break;
	//		case NetRequestHandler.ListRequestType.Remove:
	//			int playerIndex = findWithUniqueID (uniqueID);
	//			if (playerIndex != -1) {
	//				playerInfoList.RemoveAt (playerIndex);
	//			}
	//			break;
	//		case NetRequestHandler.ListRequestType.Clear:
	//			playerInfoList.Clear ();
	//			break;
	//		}
	//	}
}

