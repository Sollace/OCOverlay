# OCOverlay

A project I started roughly three years ago; it started because I wanted to have a little companion to sit on my desktop as I worked.

Thus I created OC Overlay. The original design was simple, render an image on the desktop that I can resize and move around. The UI was simple and out of the way, and can be shown or hidden by right-clicking the window.

Since then it's only grown, with support for rotating, flipping, adjusting the opacity of the image. You can lock the window to keep from moving, pin it to keep it in front of other windows.

It also supports basic animating through blink frames. Each blink frame is simply a group of images you select that will be played at random times.

OC Overlay will also make its best attempt to render anything you give it.

Images? Well, duh.
Animated gifs and apngs? Sure.
SVG? Why not?

A link to mozilla firefox? I did say _anything_ didn't I?

Anything that isn't already an image it will try to convert, often just resulting in a picture of it's icon as provided by windows.

The latest addition includes support for playing video and audio files with a button to play/pause and a to toggle loopinh, making more of a general media app.

# Startup Parameters

There's a long list of parameters you can use to control how OC Overlay behaves when it starts up.

First parameter is always a file to load. After that you can mix and match any of the following.

* --w { window width }
* --h { window height }
* --x { horizontal position }
* --y { vertical position }
* --o { opacity }
* --scale {scale}
* --rot {rotation}
* --sector { TOP_LEFT | TOP_CENTER | TOP_RIGHT }
          { MIDDLE_LEFT | MIDDLE_CENTER | MIDDLE_RIGHT }
          { BOTTOM_LEFT | BOTTOM_CENTER | BOTTOM_RIGHT }
* -nofocus clicks pass through window (only if window is shown in taskbar)
* -notask hides window in taskbar
* -a autostart animation
* -t turn transparent when hovered
* -cover start in canvas mode
* -lock locks window position
* -flipX
* -flipY
* -loop Loop playback of audio/video
