using UnityEngine;
using System;

public class NetIdentityCustom : MonoBehaviour{

	//[HideInInspector]
	public int objectID;
	//[HideInInspector]
	public static bool server = false;
	//[HideInInspector]
	public static bool client = false;
	//[HideInInspector]
	//public bool authority = false;
	public bool hasAuthority = false;

	public bool localPlayer = false;

	public int authorityID = 0;
	//[HideInInspector]
	//public bool localPlayer = false;

	public int prefabIndex = 0;

//	public bool isServer(){
//		return server;
//	}
//	public bool isClient(){
//		return client;
//	}
//	public bool hasAuthority(){
//		return authority;
//	}
//	public bool isLocalPlayer(){
//		return localPlayer;
//	}
}