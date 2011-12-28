using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;

namespace PandoraSharp
{
    public class PandoraClient
    {
        private const string BASE_URL = "http://www.pandora.com/radio/xmlrpc/v29?";
        private const string BASE_URL_RID = BASE_URL + "rid={0:D7}P&method={1}";
        private const string BASE_URL_LID = BASE_URL + "rid={0:D7}P&lid={1}&method={2}";

        private int _rid;
        private string _lid;
        private string _authenticationToken;
        private string _username;
        private string _password;

        public PandoraClient()
        {
            _rid = GetTimestamp() % 10000000;
        }

        #region Events

        public event EventHandler<EventArgs<AuthenticationResult>> AuthenticateComplete;
        public event EventHandler<EventArgs<List<Station>>> RetrieveStationsComplete;
        public event EventHandler<EventArgs<List<Song>>> RetrieveSongsComplete;
        public event EventHandler<EventArgs<Exception>> ExceptionReceived;
        public event EventHandler<EventArgs<List<SearchResult>>> SearchComplete;
        public event EventHandler<EventArgs<Station>> CreateStationComplete;
        public event EventHandler RemoveStationComplete;
        public event EventHandler SubmitFeedbackComplete;

        #endregion

        #region Public Methods

        public void AuthenticateAsync(string username, string password)
        {
            _username = username;
            _password = password;

            SyncAsync();
        }

        public void RetrieveStationsAsync()
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(RetrieveStations_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "getStations"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);

                string xml = GetXml("station.getStations", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        public void RetrieveSongsAsync(string stationId, AudioFormats audioFormat)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(RetrieveSongs_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "getFragment"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);
                parameters.Add(stationId);
                parameters.Add("0"); //total listening time
                parameters.Add(String.Empty); //time since last session
                parameters.Add(String.Empty); //tracking code
                
                switch (audioFormat)
                {
                    case AudioFormats.AAC_PLUS:
                        parameters.Add("aacplus");
                        break;
                    case AudioFormats.MP3:
                        parameters.Add("mp3");
                        break;
                    case AudioFormats.MP3_HIFI:
                        parameters.Add("mp3-hifi");
                        break;
                }

                parameters.Add("0"); //delta listening time
                parameters.Add("0"); //listening timestamp

                string xml = GetXml("playlist.getFragment", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        public void SearchAsync(string query)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(Search_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "search"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);
                parameters.Add(query);

                string xml = GetXml("music.search", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        public void CreateStationAsync(string musicId)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(CreateStation_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "createStation"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);
                parameters.Add("mi" + musicId);

                string xml = GetXml("station.createStation", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        public void RemoveStationAsync(string stationId)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(RemoveStation_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "removeStation"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);
                parameters.Add(stationId);

                string xml = GetXml("station.removeStation", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        public void SubmitFeedbackAsync(Song song, Ratings rating)
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(SubmitFeedback_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_LID, _rid, _lid, "addFeedback"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_authenticationToken);
                parameters.Add(song.StationId);
                parameters.Add(song.MusicId);
                parameters.Add(song.UserSeed);
                parameters.Add(String.Empty);//TestStrategy--wtf?
                parameters.Add(rating == Ratings.Like);
                parameters.Add(false); //IsCreatorQuickMix
                parameters.Add(song.SongType);

                string xml = GetXml("station.addFeedback", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        #endregion

        #region Private Methods

        private void SyncAsync()
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(Sync_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_RID, _rid, "sync"));
                string xml = GetXml("misc.sync", null);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        private void Sync_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            AuthenticateListenerAsync();
        }

        private void AuthenticateListenerAsync()
        {
            try
            {
                WebClient client = new WebClient();
                client.Headers.Add("content-type", "text-xml");
                client.UploadDataCompleted += new UploadDataCompletedEventHandler(AuthenticateListener_UploadDataCompleted);
                Uri uri = new Uri(String.Format(BASE_URL_RID, _rid, "authenticateListener"));

                List<object> parameters = new List<object>();
                parameters.Add(GetTimestamp());
                parameters.Add(_username);
                parameters.Add(_password);

                string xml = GetXml("listener.authenticateListener", parameters);
                string encryptedXml = EncryptionHelper.EncryptString(xml);
                client.UploadDataAsync(uri, "POST", System.Text.Encoding.ASCII.GetBytes(encryptedXml));
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
            }
        }

        #endregion

        #region Event Handlers

        private void AuthenticateListener_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            AuthenticationResult authenticationResult = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }

                Dictionary<string, string> dict = ParseAuthListenerXml(response);

                _lid = dict["listenerId"];
                _authenticationToken = dict["authToken"];

                authenticationResult = new AuthenticationResult();

                int daysLeft = 0;
                int.TryParse(dict["subscriptionDaysLeft"], out daysLeft);

                authenticationResult.SubscriptionDaysLeft = daysLeft;
                authenticationResult.Subscriber = dict["listenerState"] == "SUBSCRIBER" && daysLeft > 0;
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (AuthenticateComplete != null)
                AuthenticateComplete(this, new EventArgs<AuthenticationResult>(authenticationResult));
        }

        private void RetrieveStations_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            List<Station> stations = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }

                stations = ParseStationXml(response);
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (RetrieveStationsComplete != null)
                RetrieveStationsComplete(this, new EventArgs<List<Station>>(stations));
        }

        private void RetrieveSongs_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            List<Song> songs = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }
                
                songs = ParseSongXml(response);

                //Decrypt last 48 characters of URL
                foreach (Song song in songs)
                {
                    string decryptedString = EncryptionHelper.DecryptUrlHex(song.AudioUrl.Substring(song.AudioUrl.Length - 48, 48));
                    song.AudioUrl = song.AudioUrl.Substring(0, song.AudioUrl.Length - 48) + decryptedString;
                }
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (RetrieveStationsComplete != null)
                RetrieveSongsComplete(this, new EventArgs<List<Song>>(songs));
        }

        private void Search_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            List<SearchResult> searchResults = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }

                searchResults = ParseSearchResultsXml(response);
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (SearchComplete != null)
                SearchComplete(this, new EventArgs<List<SearchResult>>(searchResults));
        }

        private void CreateStation_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            Station station = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }

                station = ParseCreateStationXml(response);
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (CreateStationComplete != null)
                CreateStationComplete(this, new EventArgs<Station>(station));
        }

        private void RemoveStation_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (RemoveStationComplete != null)
                RemoveStationComplete(this, new EventArgs());
        }

        private void SubmitFeedback_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                if (ExceptionReceived != null)
                {
                    Exception exception = new Exception("Request was cancelled.");
                    ExceptionReceived(this, new EventArgs<Exception>(exception));
                }

                return;
            }

            if (e.Error != null)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(e.Error));

                return;
            }

            String xml = null;

            try
            {
                String response = System.Text.Encoding.ASCII.GetString(e.Result);

                string faultString = GetFaultString(response);

                if (!String.IsNullOrEmpty(faultString))
                {
                    if (ExceptionReceived != null)
                        ExceptionReceived(this, new EventArgs<Exception>(new Exception(faultString)));

                    return;
                }

                xml = response;
            }
            catch (Exception exception)
            {
                if (ExceptionReceived != null)
                    ExceptionReceived(this, new EventArgs<Exception>(exception));

                return;
            }

            if (SubmitFeedbackComplete != null)
                SubmitFeedbackComplete(this, new EventArgs());
        }

        #endregion

        #region Helper Methods

        public int GetTimestamp()
        {
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            return (int)t.TotalSeconds;
        }

        public string GetXml(string fullMethodName, List<object> parameters)
        {
            StringBuilder xml = new StringBuilder();

            xml.Append("<?xml version='1.0'?>");
            xml.Append("<methodCall>");
            xml.AppendFormat("<methodName>{0}</methodName>", fullMethodName);

            if (parameters != null)
            {
                xml.Append("<params>");

                foreach (object parameter in parameters)
                {
                    string type = "string";
                    string value = String.Empty;

                    if (parameter is String)
                    {
                        type = "string";
                        value = parameter == null ? String.Empty : parameter as string;
                    }
                    else if (parameter is int)
                    {
                        type = "int";
                        value = parameter.ToString();
                    }
                    else if (parameter is bool)
                    {
                        type = "boolean";
                        value = (bool)parameter ? "1" : "0";
                    }

                    xml.AppendFormat("<param><value><{0}>{1}</{0}></value></param>", type, value);
                }

                xml.Append("</params>");
            }

            xml.Append("</methodCall>");

            return xml.ToString();
        }

        public Dictionary<string, string> ParseAuthListenerXml(string xml)
        {
            try
            {
                Dictionary<string, string> retObj = new Dictionary<string, string>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNode listenerIdNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='listenerId']/value");
                XmlNode authTokenNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='authToken']/value");
                XmlNode listenerStateNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='listenerState']/value");
                XmlNode subscriptionDaysLeftNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='subscriptionDaysLeft']/value/int");

                retObj.Add("listenerId", listenerIdNode.InnerText);
                retObj.Add("authToken", authTokenNode.InnerText);
                retObj.Add("listenerState", listenerStateNode.InnerText);
                retObj.Add("subscriptionDaysLeft", subscriptionDaysLeftNode.InnerText);

                return retObj;
            }
            catch (Exception exception)
            {
                throw new Exception("Unable to parse AuthListener XML.", exception);
            }
        }

        public List<Station> ParseStationXml(string xml)
        {
            try
            {
                List<Station> stations = new List<Station>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNodeList stationNodes = xmlDoc.SelectNodes("/methodResponse/params/param/value/array/data/value");

                foreach (XmlNode stationNode in stationNodes)
                {
                    XmlNode stationIdNode = stationNode.SelectSingleNode("struct/member[name='stationId']/value");
                    XmlNode stationNameNode = stationNode.SelectSingleNode("struct/member[name='stationName']/value");

                    Station station = new Station();
                    station.StationId = stationIdNode.InnerText;
                    station.StationName = stationNameNode.InnerText;
                    stations.Add(station);
                }

                return stations;
            }
            catch (Exception exception)
            {
                throw new Exception("Unable to parse Station XML.", exception);
            }
        }

        public List<Song> ParseSongXml(string xml)
        {
            try
            {
                List<Song> songs = new List<Song>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNodeList songNodes = xmlDoc.SelectNodes("/methodResponse/params/param/value/array/data/value");

                foreach (XmlNode songNode in songNodes)
                {
                    XmlNode albumTitleNode = songNode.SelectSingleNode("struct/member[name='albumTitle']/value");
                    XmlNode artistSummaryNode = songNode.SelectSingleNode("struct/member[name='artistSummary']/value");
                    XmlNode artRadioNode = songNode.SelectSingleNode("struct/member[name='artRadio']/value");
                    XmlNode audioUrlNode = songNode.SelectSingleNode("struct/member[name='audioURL']/value");
                    XmlNode songTitleNode = songNode.SelectSingleNode("struct/member[name='songTitle']/value");
                    XmlNode ratingNode = songNode.SelectSingleNode("struct/member[name='rating']/value");
                    XmlNode stationIdNode = songNode.SelectSingleNode("struct/member[name='stationId']/value");
                    XmlNode musicIdNode = songNode.SelectSingleNode("struct/member[name='musicId']/value");
                    XmlNode userSeedNode = songNode.SelectSingleNode("struct/member[name='userSeed']/value");
                    XmlNode songTypeNode = songNode.SelectSingleNode("struct/member[name='songType']/value/int");

                    Song song = new Song();
                    song.AlbumTitle = albumTitleNode.InnerText;
                    song.ArtistSummary = artistSummaryNode.InnerText;
                    song.ArtRadio = artRadioNode.InnerText;
                    song.AudioUrl = audioUrlNode.InnerText;
                    song.SongTitle = songTitleNode.InnerText;
                    song.Like = ratingNode.InnerText == "1" ? true : false;
                    song.StationId = stationIdNode.InnerText;
                    song.MusicId = musicIdNode.InnerText;
                    song.UserSeed = userSeedNode.InnerText;
                    song.SongType = int.Parse(songTypeNode.InnerText);
                    songs.Add(song);
                }

                return songs;
            }
            catch (Exception exception)
            {
                throw new Exception("Unable to parse Song XML.", exception);
            }
        }

        public List<SearchResult> ParseSearchResultsXml(string xml)
        {
            try
            {
                List<SearchResult> searchResults = new List<SearchResult>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNodeList songNodes = xmlDoc.SelectNodes("/methodResponse/params/param/value/struct/member[name='songs']/value/array/data/value");

                foreach (XmlNode songNode in songNodes)
                {
                    XmlNode musicIdNode = songNode.SelectSingleNode("struct/member[name='musicId']/value");
                    XmlNode artistSummaryNode = songNode.SelectSingleNode("struct/member[name='artistSummary']/value");
                    XmlNode songTitleNode = songNode.SelectSingleNode("struct/member[name='songTitle']/value");

                    SearchResult searchResult = new SearchResult();
                    searchResult.SearchResultType = SearchResultTypes.Song;
                    searchResult.MusicId = musicIdNode.InnerText;
                    searchResult.ResultText = String.Format("{0} by {1}", songTitleNode.InnerText, artistSummaryNode.InnerText);
                    searchResults.Add(searchResult);
                }

                XmlNodeList artistNodes = xmlDoc.SelectNodes("/methodResponse/params/param/value/struct/member[name='artists']/value/array/data/value");

                foreach (XmlNode artistNode in artistNodes)
                {
                    XmlNode musicIdNode = artistNode.SelectSingleNode("struct/member[name='musicId']/value");
                    XmlNode artistNameNode = artistNode.SelectSingleNode("struct/member[name='artistName']/value");

                    SearchResult searchResult = new SearchResult();
                    searchResult.SearchResultType = SearchResultTypes.Artist;
                    searchResult.MusicId = musicIdNode.InnerText;
                    searchResult.ResultText = artistNameNode.InnerText;
                    searchResults.Add(searchResult);
                }

                return searchResults;
            }
            catch (Exception exception)
            {
                throw new Exception("Unable to parse Song XML.", exception);
            }
        }

        public Station ParseCreateStationXml(string xml)
        {
            try
            {
                List<Station> stations = new List<Station>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNode stationIdNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='stationId']/value");
                XmlNode stationNameNode = xmlDoc.SelectSingleNode("/methodResponse/params/param/value/struct/member[name='stationName']/value");

                Station station = new Station();
                station.StationId = stationIdNode.InnerText;
                station.StationName = stationNameNode.InnerText;

                return station;
            }
            catch (Exception exception)
            {
                throw new Exception("Unable to parse CreateStation XML.", exception);
            }
        }

        public string GetFaultString(string xml)
        {
            if (String.IsNullOrEmpty(xml))
                return null;

            if (!xml.Contains("<fault>"))
                return null;

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNode faultStringNode = xmlDoc.SelectSingleNode("/methodResponse/fault/value/struct/member[name='faultString']/value");

                if (faultStringNode == null)
                    return null;

                if (String.IsNullOrEmpty(faultStringNode.InnerText))
                    return null;

                if (faultStringNode.InnerText.Contains("AUTH_INVALID_USERNAME_PASSWORD"))
                    return "Invalid username and/or password.";
                else
                    return faultStringNode.InnerText;
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
