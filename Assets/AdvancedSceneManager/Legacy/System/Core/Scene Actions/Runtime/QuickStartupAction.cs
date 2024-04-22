namespace AdvancedSceneManager.Core.Actions
{

    /// <summary>
    /// Performs startup sequence, but without splash screen and loading screens.
    /// <para><see cref="CloseAllUnityScenesAction"/>.</para>
    /// <para><see cref="OpenStartupCollections"/>.</para>
    /// </summary>
    public class QuickStartupAction : StartupAction
    {

        ///<inheritdoc cref="QuickStartupAction"/>
        public QuickStartupAction()
            : base(skipSplashScreen: true)
        { }

    }

}
