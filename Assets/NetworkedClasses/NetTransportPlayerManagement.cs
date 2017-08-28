using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetTransportPlayerManagement : NetTransportManager{

	public enum ListUpdateType {UpdateYourself, SyncOthers};
	
	public override void ReceiveDataEvent (PlayerInfo playerInfo, byte[] data, int index = 0){
		base.ReceiveDataEvent (playerInfo, data, index);

		ByteReceiver br = new ByteReceiver (data, index);
		if ((DataType)br.getInt () == DataType.PlayerListUpdate) {
			switch ((ListUpdateType)br.getInt()) {
			case ListUpdateType.UpdateYourself:
				thisPInfo.deserialize (data, br.index);
				break;
			case ListUpdateType.SyncOthers:
				receiveSyncOthers (br);
				break;
			}
		}
	}

	public override void startAsClient (int offset){
		base.startAsClient (offset);
		thisPInfo.name = "PlayerMe";
		thisPInfo.randColor ();
//		playerInfoList.Add(thisPInfo);
	}
	public override void startAsServer (){
		base.startAsServer ();
		thisPInfo.name = "Server";
		thisPInfo.color = Color.white;
		thisPInfo.uniqueID = 0;
//		playerInfoList.Add (thisPInfo);
	}

	public override void ClientRequestConnect (int outConnectionId){
		base.ClientRequestConnect (outConnectionId);
		PlayerInfo pi = new PlayerInfo (outConnectionId);
		pi.uniqueID = generateUniqueID ();
		pi.name = "Player" + pi.uniqueID;
		playerInfoList.Add (pi);

		ByteContructor bc = new ByteContructor ();
		send(bc.addLevels (new int[] { (int)DataType.PlayerListUpdate, (int)ListUpdateType.UpdateYourself }, pi.serialize()), outConnectionId);
		sendSyncOthers ();
	}

	public override void ServerRequestConnect (int outConnectionId){
		base.ServerRequestConnect (outConnectionId);
		playerInfoList.Add (new PlayerInfo (outConnectionId, "Server", 0, Color.white));   //Since it's the server the unique ID will always be 0
	}

	public void sendSyncOthers(){
		ByteContructor bc = new ByteContructor ();
		bc.addLevels (new int[] { (int)DataType.PlayerListUpdate, (int)ListUpdateType.SyncOthers });	//add types
		bc.add(System.BitConverter.GetBytes(playerInfoList.Count-1)); 		//add ammount of Players int
		byte[] standardBytes = bc.bytesList.ToArray();

		for (int q = 0; q < playerInfoList.Count; q++) {					//loop for all Players
			ByteContructor bcL = new ByteContructor (standardBytes);

			for (int i = 0; i < playerInfoList.Count; i++)					//add all players
				if(playerInfoList[i].connID != playerInfoList[q].connID)
					bcL.add (playerInfoList [i].serialize ());

			send (bcL.bytesList.ToArray (), playerInfoList[q].connID);					//send to player
		}
	}

	public virtual void receiveSyncOthers(ByteReceiver br){
		if (isServer ())
			return;
		//Remove all synced PlayerInfo's
		int count = 0;
		for (int i = 0; i < playerInfoList.Count; i++) {
			if (playerInfoList [count].synced) 
				playerInfoList.RemoveAt (count);
			else
				count++;
		}

		int max = br.getInt ();
		for (int i = 0; i < max; i++) {
			var pi = new PlayerInfo ();
			br.index = pi.deserialize (br.data, br.index);
			pi.synced = true;
			playerInfoList.Add (pi);
		}
	}

}

[System.Serializable]
public class PlayerInfo{
	//General
	public string name;
	public int uniqueID;
	public Color color;
	public bool me = false;
	public bool synced = false;
	
	//Other
	public int connID;
	
	public PlayerInfo(){
	
	}
	public PlayerInfo(bool meL, string nameL){
		me = meL;
		name = nameL;
		randColor();
	}
	public PlayerInfo(bool meL, string nameL, Color colorL){
		me = meL;
		name = nameL;
		color = colorL;
	}
	public PlayerInfo(int connIDL){
		connID = connIDL;
		name = "Player";
		uniqueID = 60000;
		color = Color.black;
		randColor ();
	}
	public PlayerInfo(int connIDL, string nameL){
		connID = connIDL;
		name = nameL;
		uniqueID = 60000;
		color = Color.black;
		randColor ();
	}
	public PlayerInfo(int connIDL, string nameL, int uniqueIDL){
		connID = connIDL;
		name = nameL;
		uniqueID = uniqueIDL;
		color = Color.black;
		randColor ();
	}
	public PlayerInfo(int connIDL, string nameL, int uniqueIDL, Color colorL){
		connID = connIDL;
		name = nameL;
		uniqueID = uniqueIDL;
		color = colorL;
	}
	public PlayerInfo(byte[] bytes, int index){
		deserialize (bytes, index);
	}

	public void randColor(){
		float r = 1f * Random.Range (0f,1f);
		float g = (1.5f - r) * Random.Range (0f, 1f);
		float b = 1.5f - r - g;
		if (r + g + b < 1) {
			float u = (1 - r - g - b)/3;
			r += u;
			g += u;
			b += u;
		}
		if (r > 1)
			r = 1;
		if (g > 1)
			g = 1;
		if (b > 1)
			b = 1;
		color = new Color (r, g, b, 1);
	}
	public byte[] serialize(){
		ByteContructor bc = new ByteContructor ();
		bc.add(System.BitConverter.GetBytes (uniqueID));
//		bc.add(System.BitConverter.GetBytes (connID));
		bc.add(ByteHelper.colorBytes (color));
		bc.add(ByteHelper.stringBytes(name));
		return bc.bytesList.ToArray ();
	}
	public int deserialize(byte[] bytes, int index){
		ByteReceiver br = new ByteReceiver (bytes, index);
		uniqueID = (int)br.getInt ();
//		connID = br.getInt ();
		color = br.getColor ();
		name = br.getString();
		return br.index;
	}
}