using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetTransportText : NetTransportDebug{

	[Space(3)]
	[Header("Specific")]
	public int maxPDispName = 10;
	public InputField nameField;
	public InputField ipField;
	public InputField sendField;


	public override void ReceiveDataEvent (PlayerInfo playerInfo, byte[] data, int index = 0){
		base.ReceiveDataEvent (playerInfo, data, index);
		ByteReceiver r = new ByteReceiver (data, index);
		if ((DataType)r.getInt () == DataType.Text){
			string message = r.getString ();
			textReceived (playerInfo, message);
		}
	}

	public virtual void sendText(){
		if (!isConnected ()) {
			logMessage ("Not Connected", Color.red);
			return;
		}
		if (sendField.text == "") {
			Notifier.instance.notify ("Nothing to Send :(", Color.red);
			return;
		}

		string text = sendField.text;
		sendField.text = "";
		string toPrint = String.Format("Me\t\t\t:{0}", text);
		//		for (int i = 0; i < maxPDispName-2; i++) {
		//			toPrint += " ";
		//		}
		logMessage (toPrint, thisPInfo.color);
		Notifier.instance.notify (String.Format("I said: {0}", text), thisPInfo.color);
		byte[] textB = ByteHelper.stringBytes (text);
		byte[] combined = ByteHelper.Combine (System.BitConverter.GetBytes ((int)DataType.Text), textB);
		sendField.ActivateInputField ();

		//SentTo Server/AllClients
		sendBasicAll (combined);
		if (isClient ()) {
			List<int> uniqueIDList = new List<int> ();
			foreach (PlayerInfo pi in playerInfoList) {
				if (pi.synced)
					uniqueIDList.Add (pi.uniqueID);
			}
			sendRelay (combined, uniqueIDList.ToArray ());
		}
	}

	public virtual void textReceived(PlayerInfo pi, string message){
		string toLog1 = pi.name;
		//Normalize player name *********************************
		if (toLog1.Length > maxPDispName) {
			toLog1 = toLog1.Substring (0, maxPDispName - 2);
			toLog1 += "..";
		} else
			while (toLog1.Length < maxPDispName)
				toLog1 += " ";
		//*******************************************************
		toLog1 += "\t:";
		toLog1 += message;
		string toLog2 = String.Format ("{0} said: {1}", pi.name, message);
		textReceivedFormated (toLog1, toLog2, pi.color);
	}

	public virtual void textReceivedFormated(string message1, string message2, Color color){
		logMessage (message1, color);
		Notifier.instance.notify (message2, color);
	}

	public void nameChanged(){
		name = nameField.text;
	}
	public void ipChanged(){
		ipAddress = ipField.text;
	}

}
