using System.Collections;
using UnityEngine.UI;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A default loading screen script. Displays quotes.</summary>
    public class QuoteLoadingScreen : FadeLoadingScreen
    {

        public Quotes quotes;
        public Image Background;
        public Text Quote;
        public Text Name;

        public override IEnumerator OnOpen()
        {

            var quote = quotes.quoteList[UnityEngine.Random.Range(0, quotes.quoteList.Count - 1)];
            Quote.text = quote.quote;
            Name.text = quote.name;

            yield return FadeIn();

        }

        public override IEnumerator OnClose()
        {
            yield return FadeOut();
            Background.enabled = false;
            Quote.enabled = false;
            Name.enabled = false;
        }

    }

}