using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class SfxPostprocessor : AssetPostprocessor {
	
	void OnPreprocessAudio() {
		var imp = assetImporter as AudioImporter;
		if (imp.assetPath.ToLower().EndsWith(".wav")) {
			imp.threeD = false;
		}
	}
	
}
