using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using PrintManagement.Services;
using System;

namespace PrintManagement.Helpers.ControlHelpers;

public static class AnimationHelpers
{
    public static void PlayEntranceAnimation(UIElement target)
    {
        if (Application.Current.Resources["ShowControlAnimation"] is Storyboard anim)
        {
            try
            {
                // חובה לעצור את האנימציה לפני שינוי ה-Target או הפעלה מחדש אם היא כבר הייתה בשימוש
                anim.Stop();

                Storyboard.SetTarget(anim, target);
                anim.Begin();
            }
            catch (Exception ex)
            {
                string errorMessage = $"Animation error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorMessage);
                Logger.Error(ex);
            }
        }
    }
}