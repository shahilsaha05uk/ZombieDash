#pragma warning disable CS0414
using System.Threading.Tasks;

namespace AdvancedSceneManager.Editor.UI
{

    partial class SceneManagerWindow
    {

        class EditorCoroutinesNotification : INotificationPopup
        {

            bool hasBeenDismissed;
            public async void ReloadNotification()
            {

                while (!window)
                    await Task.Delay(10);

                ClearNotification();

#if !COROUTINES
                if (!hasBeenDismissed)
                    DisplayNotification();
#endif

            }

            const string notificationID = nameof(EditorCoroutinesNotification);
            void DisplayNotification()
            {
                ClearNotification();
                window.notifications.Notify("Editor coroutines is not installed, this may cause some features to behave unexpectedly outside of play-mode.", notificationID, Install, Dismiss);
            }

            void ClearNotification() =>
                window.notifications.Remove(notificationID);

            void Install()
            {
                try
                {
                    UnityEditor.PackageManager.UI.Window.Open("com.unity.editorcoroutines");
                }
                catch
                {
                    //Internal null ref sometimes happen, no clue why
                }
            }

            void Dismiss() =>
                hasBeenDismissed = true;

        }

    }

}
