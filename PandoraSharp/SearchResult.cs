using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    public class SearchResult
    {
        private SearchResultTypes _searchResultType;

        public SearchResultTypes SearchResultType
        {
            get { return _searchResultType; }
            set { _searchResultType = value; }
        }

        private string _musicId;

        public string MusicId
        {
            get { return _musicId; }
            set { _musicId = value; }
        }

        private string _resultText;

        public string ResultText
        {
            get { return _resultText; }
            set { _resultText = value; }
        }
    }
}
