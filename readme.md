
# Hands Tracking (Oculus Quest) directly in Unity Editor

Currently Oculus Link -> Unity integration is not supporting hand tracking. This makes quick iteration for hands related interactions more difficult.

You can use this package to bridge that gap till Link supports that completely. It transmits hand related data directly to Unity via network and then feeds that into the scripts.

You can see hands in the Scene View and access hand related data quickly.
**That should make iterations faster without the need to deploy all changes to Quest.**

![Quest Hand Tracking in Unity Editor](/_github/QuestHandsTrackedInUnityEditor.gif)


## Setup
1) Import [Oculus Integration package](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022)
2) Run `HandsIntegrationTrainScene` from `/Assets/Oculus/SampleFramework/Usage`
3) Make sure you follow Oculus guidelines to set up, also to enable hands in the scene `Hand Tracking Support` on `OVRCameraRig` game object needs to be set to either `Hands` or `Controller and hands`.
4) [Download](https://github.com/handzlikchris/Unity.QuestRemoteHandTracking/raw/master/UnityPackage/QuestRemoteHandTracking.unitypackage) and import  `Unity.QuestRemoteHandTracking` package
5) Go to `/Assets/RemoteHandsTracking/Prefabs` and add `HandsDataTransmission` to the scene

## Configuring Network
Data will be sent over your network, best if your PC and Quest are on the same wi-fi network.
1) In `HandsDataSender` game object specify IP address that your PC is on
    - you can get that by running `ipconfig` in console
2) Make sure your firewall is allowing connections on that ip/port
3) In `HandsDataReceiver` specify the same IP and port (you should be able to use loopback IP `127.0.0.1`, if you're running into troubles use same IP as for `HandsDataSender`)
4) You'll need to add `Hands` reference to `Feeder` objects 
- `HandsDataFeeder`
- `SkeletonDataFeeder`
- `MeshDataFeeder`

## Running
That's it. Now run the application on Quest and hit play in the editor. Once Quest starts sending your hand data into Unity you'll be able to see them directly in Scene View.

## Recording and Replaying
If you're iterating over specific gesture for a while it may be easier to record it and replay, this way you won't need to do it over and over in Quest.

![Quest Hand Tracking in Unity Editor - Custom Replay](/_github/QuestHandsTrackedInUnityEditor_Custom_Replay.gif)

### Recording
1) Play the scene (in editor and in Quest)
	- make sure you can see hand movement on screen before recording
2) Go to `HandsDataRecorder` and fill `New Recording Name`
	- you can have multiple recordings and they'll be locally persisted under that name
3) In editor on `HandsDataRecorder` click `StartRecording`
4) Perform gestures as needed
5) In editor on `HandsDataRecorder` click `StopRecording`
	- you'll now have a child under `HandsDataRecorder` named as defined in `New Recording Name`, this is locally persisted and **can be re-run later without needing to even connecting Quest to PC**

### Replaying
1) Run the scene (you don't need to have Ouest running)
1) In editor go to `HandsDataRecorder` - it'll have children with your custom recordings
2) Pick one that you want to replay and click on `Play`
	- You can also remove it from drive via `Delete` button
3) Inspector will focus on `HandsDataPlayer` and will start playing your pre-recorded gestures

**Controlling Replay**
With recording assigned to `HandDataPlayer` you can also choose specific frame that you'd like to view via `ManuallyMoveToFrame` 


## Customising to run in your project
You can easily use the package in your project. To do so you need to adjust feeder scripts that set hand / mesh / skeleton data. Look at classes in `Customisation` to see how that's done. Assign new handler to corresponding event on `HandsDataReceiver` object.

Remember to change event calls on `HandDataRecorder` to use your custom feeder objects.


## Known issues
- If your hands are bit oddly shaped make sure your actual hands are not visible to Quest cameras for a second or two when app starts up. There seems to be some timing / initialization issue with skeleton/mesh if they are visible from very first moments.

- If hands on screen are flickering go to `OculusSampleFramework.Hand.ScaledAlpha` in `\Assets\Oculus\SampleFramework\Core\CustomHands\Scripts\Hand.cs` property and set getter to always return '1f'.
```
// Calculated Alpha vlaue of the hand according to OVRPlugin.TrackingConfidence
public float ScaledAlpha
{
    get
       {
           return 1f; //HACK: always visible
       }
    set
    {
        _scaledAlpha = value;
    }
}
```
That value is constantly overridden by default scripts. Even though it's set every there's some timing issue somewhere which could cause hands to flicker.