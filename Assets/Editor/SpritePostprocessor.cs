using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class SpriteImporterExtensions {
	
	// BLACK MAGIC
	public static void GetImageSize(this TextureImporter importer, out int width, out int height) {
		object[] args = new object[2] { 0, 0 };
		MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
		mi.Invoke(importer, args);
		width = (int) args[0];
		height = (int) args[1];
	}
	
}

public class SpritePostprocessor : AssetPostprocessor {
	
	void OnPreprocessTexture() {
		
		// look for assets within the sprites directory
		if (assetPath.Contains("/Sprites/")) {
			
			// basic sprite settings
			var importer = assetImporter as TextureImporter;
			importer.textureFormat = TextureImporterFormat.ARGB32;
			importer.textureType = TextureImporterType.Sprite;
			importer.spritePixelsToUnits = 32;
			importer.filterMode = FilterMode.Point;
			
			
			// images group'd in a subdirectory should get backed together
			var pathTokens = assetPath.Split('/');
			var dirToken = pathTokens[pathTokens.Length-2];
			if (dirToken != "Sprites") {
				importer.spritePackingTag = dirToken;
			}
			
			// sprites with "#" in their names are vertical-strip animations
			var fileName = Path.GetFileNameWithoutExtension(assetPath);
			var hashIdx = fileName.IndexOf('#');
			int frameCount;
			if (hashIdx > 0 && int.TryParse(fileName.Substring(hashIdx+1), out frameCount)) {
				importer.spriteImportMode = SpriteImportMode.Multiple;
				var metaData = new SpriteMetaData[frameCount];
				int width, height;
				importer.GetImageSize(out width, out height);
				int frameHeight = Mathf.FloorToInt( height / frameCount );
				var name = fileName.Substring(0, hashIdx);
				for(int i=0; i<frameCount; ++i) {
					metaData[i].name = string.Format("{0}_frame{1}", name, i);
					metaData[i].rect = new Rect(
						0, i * frameHeight, width, frameHeight
					);
					metaData[i].pivot = new Vector2(
						0.5f * width, 0.5f * frameHeight
					);
				}
				importer.spritesheet = metaData;
			} else {
				importer.spriteImportMode = SpriteImportMode.Single;
			}
			
			
		}
	}	
	
}
