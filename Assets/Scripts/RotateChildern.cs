﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateChildern : MonoBehaviour {
	public int axis = 1;  // which axis to rotate around 
	// Update is called once per frame
	void Update () {
		// RotationManager から回転角を取得して，(ModelController下の)子オブジェクトに適用する．
		Vector3 rot_euler = Vector3.zero;
		rot_euler [axis] = RotationManager.RotationAngle;
		foreach (Transform child in transform)
			child.localEulerAngles = rot_euler;
	}
}
