#!/usr/bin/python
from psd_tools import PSDImage
from PIL import Image
import math, os, sys

# open a PSD file and create a PNG that turns the
# layers into frames in a vertical-strip animation
# and baked the frame count into the output filename

def export(path):
	psd = PSDImage.load(path)

	# determine the bounds of all the layers
	x1,y1,x2,y2 = psd.layers[0].bbox
	for layer in psd.layers:
		a,b,c,d = layer.bbox
		x1 = min(a, x1)
		y1 = min(b, y1)
		x2 = max(c, x2)
		y2 = max(d, y2)

	w, h = x2 - x1, y2 - y1

	result = Image.new('RGBA', (w,len(psd.layers)*h), (0,0,0,0))

	for i,layer in enumerate(psd.layers):
		layerImg = layer.as_PIL()
		x, y, _, _ = layer.bbox
		result.paste(layerImg, (x - x1, y + i * h - y1))

	d,f = os.path.split(path)
	fn,fe = os.path.splitext(f)
	result.save(os.path.join('../Assets/Sprites/game', "%s#%d.png" % (fn, len(psd.layers))))

if __name__ == '__main__' and sys.argv > 1:
	export( sys.argv[1] )

