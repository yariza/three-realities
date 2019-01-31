using UnityEngine;
using Leap.Unity;
using Leap.Unity.Interaction;

public static class LeapUtils
{
    // public static bool IsPinching(this InteractionController controller, float threshold = 0.8f)
    // {
    //     return controller.intHand.leapHand.PinchStrength > threshold &&
    //            !controller.intHand.leapHand.GetIndex().IsExtended;
    // }

    public static Vector3 GetPinchPosition(this InteractionController controller)
    {
        return controller.intHand.leapHand.GetPredictedPinchPosition();
    }

    public static Vector3 GetPinchNormal(this InteractionController controller)
    {
        var hand = controller.intHand.leapHand;
        var direction = (-hand.GetIndex().Direction + hand.GetThumb().Direction) * 0.5f;
        return direction.ToVector3();
    }

    public static bool IsPointing(this InteractionController controller)
    {
        // only check two fingers because the latter two are more unstable
        var hand = controller.intHand.leapHand;
        return hand.GetIndex().IsExtended &&
              !hand.GetMiddle().IsExtended;
    }

    public static Vector3 GetPointPosition(this InteractionController controller)
    {
        var finger = controller.intHand.leapHand.GetIndex();
        // a bit longer from the tip
        return finger.TipPosition.ToVector3() + 0.01f * finger.Direction.ToVector3();
    }
}
