using System;
using UnityEngine;

public class NetTransportC : NetTransportText{

	public GameObject loginMenu;
	public GameObject mainMenu;

	public override void Update (){
		base.Update ();

		if (Input.GetKey (KeyCode.RightShift) && Input.GetKeyDown (KeyCode.P)) {
			StopAllConnections (false);
		}
		if (Input.GetKeyDown (KeyCode.Return)) {
			sendText ();
		}

	}

	public override void ConnStateChangeEvent (bool state){
		base.ConnStateChangeEvent (state);
//		mainMenu.SetActive (state);
//		DebugConsole.isVisible = state;
		loginMenu.SetActive (!state);

	}

	public override void textReceivedFormated (string message1, string message2, Color color){
		base.textReceivedFormated (message1, message2, color);
	}

	//	public void changeName(){
	//		if (isClient ()) {
	//			
	//		}
	//	}
}
