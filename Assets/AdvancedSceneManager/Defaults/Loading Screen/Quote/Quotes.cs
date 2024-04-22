using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedSceneManager.Defaults
{

    /// <summary>A list of quotes for <see cref="QuoteLoadingScreen"/>.</summary>
    public class Quotes : MonoBehaviour
    {

        /// <summary>The list of quotes.</summary>
        public List<Quote> quoteList;

        /// <summary>A quote.</summary>
        [Serializable]
        public class Quote
        {
            /// <summary>The name of this quote.</summary>
            public string name;
            /// <summary>The quote text itself.</summary>
            public string quote;
        }

    }

}
