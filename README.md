# ScreenshotCompressor
When an image is copied to the clipboard:
* Compress that image as a png in its original size at various compression levels
* If the image is now <= 8 MB (the discord image past limit), set the clipboard data to the compressed image
* If the image is too big, resize the bitmap form by 2/3 and try again
* Repeat until its small enough
