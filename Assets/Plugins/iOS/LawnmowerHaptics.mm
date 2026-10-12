#import <UIKit/UIKit.h>
#import "LawnmowerHaptics.h"

// Unity calls these from its player loop, which runs on the main thread, as UIKit requires.

static UIImpactFeedbackGenerator *s_lightImpact;
static UINotificationFeedbackGenerator *s_notification;

static UIImpactFeedbackGenerator *LightImpact(void)
{
    if (s_lightImpact == nil)
        s_lightImpact = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
    return s_lightImpact;
}

static UINotificationFeedbackGenerator *Notification(void)
{
    if (s_notification == nil)
        s_notification = [[UINotificationFeedbackGenerator alloc] init];
    return s_notification;
}

extern "C" void LawnmowerHaptics_LightTap(void)
{
    UIImpactFeedbackGenerator *generator = LightImpact();
    [generator impactOccurred];
    [generator prepare];
}

extern "C" void LawnmowerHaptics_Success(void)
{
    UINotificationFeedbackGenerator *generator = Notification();
    [generator notificationOccurred:UINotificationFeedbackTypeSuccess];
    [generator prepare];
}

extern "C" void LawnmowerHaptics_Error(void)
{
    UINotificationFeedbackGenerator *generator = Notification();
    [generator notificationOccurred:UINotificationFeedbackTypeError];
    [generator prepare];
}
