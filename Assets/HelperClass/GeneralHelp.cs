using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralHelp : MonoBehaviour {

	public delegate void GODelegate(GameObject gO);

	public static void searchHierarchy(GODelegate toCall, GameObject gO){
		toCall (gO);
		int i = 0;
		while (gO.transform.parent != null && i<10) {
			gO = gO.transform.parent.gameObject;
			toCall (gO);
			i++;
		}
	}
}
