# three realities

![](/img/stretchy-hand.gif)

_three realities_ is a mixed reality experience that explores the boundary between the virtual and the real.

## Installation

- [Oculus Rift Setup](https://www.oculus.com/setup/)
- [Leap Motion Orion Beta](https://developer.leapmotion.com/get-started/)
- [NVIDIA Graphics Drivers](https://www.nvidia.com/Download/Find.aspx?lang=en-us)

An NVIDIA Graphics Card (GTX 1060 or higher) is required to use our software.

It is recommended to do a “clean” install when installing graphics drivers. From the installer, choose the Custom install option, and uncheck all options except for NVIDIA Graphics Drivers.

- [CUDA SDK](https://developer.nvidia.com/cuda-10.0-download-archive)
CUDA 10.0 is required to use the ZED SDK 2.7. The ZED installer (below) should include installing CUDA as part of its installation process now, but this is the required version here.

As with the Graphics Drivers, it is recommended to select the Custom install option, and to uncheck all options that is not CUDA.

- [ZED SDK](https://www.stereolabs.com/developers/release/2.7/#sdkdownloads_anchor)

We support ZED SDK version 2.7. Please download the installer for CUDA 10.0, and Windows.
If asked to install CUDA during the install step, quit the installation and install CUDA separately. Then continue with the ZED SDK installation.

## Headset Setup

The experience requires a hybrid setup, using either an Oculus Rift, with a ZED Mini stereo camera and Leap Motion hand tracker mounted on the front. Please refer to this reference image to set up your headset.

![](/img/oculus-zed-leap.png)

Note that while the ZED mini is mounted in the center of the headset, the Leap Motion is mounted directly below, by flipping the mount upside down to attach to the bottom of the headset.

## Hand Calibration

On startup of the executable, the virtual hand positions may not be perfectly aligned to the user’s hands. This is because we had to place the Leap Motion towards the bottom of the VR headset in order to fit it with a ZED camera as well when it is designed to be in the center of the headset. Keyboard controls are provided to move the alignment of the virtual Leap hands so that someone running the prototype can manually calibrate their hands. On the number pad, these controls are:

- Keypad 4 - Move left
- Keypad 6 - Move right
- Keypad 7 - Move up
- Keypad 9 - Move down
- Keypad 5 - Move away from player
- Keypad 8 - Move towards player

Note: Once hand positions are calibrated for a particular executable, those settings will be saved across sessions. However, they will need to be calibrated for each demo!

## Debug GUI / Hotkeys

- H - Toggle FPS counter
