using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Text;
using centrafuse.Plugins;
using PandoraSharp;

namespace Pandora
{
	public class Pandora : CFPlugin
    {
        #region Class Variables

        private const string _pluginName = "Pandora";
        private string _logFilePath = CFTools.AppDataPath + "\\Plugins\\Pandora\\Pandora.log";
        private string _pluginPath = null;
        private CFControls.CFAdvancedList _stationsListView;
        private List<Station> _stations = new List<Station>();
        private Queue<Song> _songs = new Queue<Song>();
        private PandoraClient _pandoraClient = null;
        private DateTime _lastAuthenticated = DateTime.MinValue;
        private bool _clientIsBusy = false;
        private String _currentStationId = null;
        private Song _currentSong = null;
        private string _afterCallAction = null;
        private Image _defaultAlbumArt = null;
        private Image _activeLikeIcon = null;
        private Image _activeGuestIcon = null;
        private System.Net.WebClient _albumArtWebClient = null;
        private System.Timers.Timer _timer;
        private System.Data.DataTable _stationsTable = new System.Data.DataTable();
        private List<string> _favorites = new List<string>();
        private bool _hasControl = false;
        private int _audioStream = 0;
        private int _zone = 0;
        private bool _guest = false;
        private string _userName = null;
        private string _password = null;
        private bool _logEvents = false;
        private bool _clearCache = false;
        private bool _subscriber = false;
        private AudioFormats _audioFormat = AudioFormats.MP3;

        //Lame global variables to pass around data through asynchronous events
        private string _stationIdToRemove = null;
        private Song _songToRate = null;
        private Ratings _ratingToSubmit = Ratings.Like;

        #endregion

		public Pandora()
		{
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            _pluginPath = System.IO.Path.GetDirectoryName(assembly.Location) + "\\Plugins\\Pandora";
        }

        #region CFPlugin methods

        /// <summary>
        /// Initializes the plugin.  This is called from the main application
        /// when the plugin is first loaded.
        /// </summary>
		public override void CF_pluginInit()
		{
			try
			{
                // CF3_initPlugin() Will configure pluginConfig and pluginLang automatically
                // All plugins must call this method once
                this.CF3_initPlugin("Pandora", true);

                // All controls should be created or Setup in CF_localskinsetup.
                // This method is also called when the resolution or skin has changed.
                this.CF_localskinsetup();

                LoadSettings();

                WriteLog("Pandora Plugin Startup");

                string tempDir = System.IO.Path.GetTempPath();
                WriteLog("Copying UI Images to Temp Directory: " + tempDir);

                if (!File.Exists(tempDir + "CF_PandoraPlugin_DefaultAlbumArt.png"))
                    File.Copy(_pluginPath + "\\Skins\\Clean\\DefaultAlbumArt.png", tempDir + "CF_PandoraPlugin_DefaultAlbumArt.png", true);

                if (!File.Exists(tempDir + "CF_PandoraPlugin_Icon_Guest_Active.png"))
                    File.Copy(_pluginPath + "\\Skins\\Clean\\Icon_Guest_Active.png", tempDir + "CF_PandoraPlugin_Icon_Guest_Active.png", true);

                if (!File.Exists(tempDir + "CF_PandoraPlugin_Icon_Like_Active.png"))
                    File.Copy(_pluginPath + "\\Skins\\Clean\\Icon_Like_Active.png", tempDir + "CF_PandoraPlugin_Icon_Like_Active.png", true);

                WriteLog("Loading UI Images");
                _defaultAlbumArt = Image.FromFile(tempDir + "CF_PandoraPlugin_DefaultAlbumArt.png");
                _activeGuestIcon = Image.FromFile(tempDir + "CF_PandoraPlugin_Icon_Guest_Active.png");
                _activeLikeIcon = Image.FromFile(tempDir + "CF_PandoraPlugin_Icon_Like_Active.png");

                WriteLog("Setting default UI values");
                CF_setPictureImage("AlbumArtPicture", _defaultAlbumArt);
                CF_setPictureImage("GuestActive", null);
                CF_setPictureImage("ThumbsUpActive", null);

                WriteLog("Initializing Plugin Variables");
                //Pause other system audio when this plugin is entered
                //Also gives the "current playing application" functionality
                this.CF_params.pauseAudio = true;

                WriteLog("Initializing Album Art WebClient");
                _albumArtWebClient = new System.Net.WebClient();
                _albumArtWebClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(_albumArtWebClient_DownloadFileCompleted);

                if (Directory.Exists(CFTools.AppDataPath + "\\Plugins\\Pandora\\AlbumArt"))
                {
                    if (_clearCache)
                    {
                        WriteLog("Clearing Album Art Cache Directory");
                        try
                        {
                            DirectoryInfo directoryInfo = new DirectoryInfo(CFTools.AppDataPath + "\\Plugins\\Pandora\\AlbumArt");
                            foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                                fileInfo.Delete();
                        }
                        catch (Exception exception)
                        {
                            WriteLog("Unable to clear album art cache directory", exception);
                        }
                    }
                }
                else
                {
                    WriteLog("Creating Album Art Cache Directory");
                    Directory.CreateDirectory(CFTools.AppDataPath + "\\Plugins\\Pandora\\AlbumArt");
                }

                WriteLog("Initializing Timer");
                _timer = new System.Timers.Timer(2000);
                _timer.Elapsed += new System.Timers.ElapsedEventHandler(_timer_Elapsed);

                WriteLog("Wiring Up Plugin Events");
                //HACK: These three lines should work, but they don't...
                //Instead, these events are hijacked in the CF_pluginCMLCommand method
                //this.CF_events.nextTrack += new EventHandler(CF_events_nextTrack);
                //this.CF_events.previousTrack += new EventHandler(CF_events_previousTrack);
                //base.CF_createButtonEvents("PlayPause", new MouseEventHandler(handler), null);
				this.CF_events.powerModeChanged +=new Microsoft.Win32.PowerModeChangedEventHandler(CF_Events_PowerModeChanged);

                WriteLog("Creating Data Table");
                _stationsTable = new System.Data.DataTable();
                _stationsTable.Columns.Add("StationId", typeof(String));
                _stationsTable.Columns.Add("StationName", typeof(String));
                _stationsTable.Columns.Add("Title", typeof(String));
                _stationsTable.Columns.Add("Favorite", typeof(bool));

                InitPandoraClient();
			}
			catch(Exception errmsg)
            {
                CF_displayMessage("Error initializing Pandora plugin: " + errmsg);
                CFTools.writeError(errmsg.ToString());
            }
		}

        /// <summary>
        /// This is called to setup the skin.  This will usually be called in CF_pluginInit.  It will 
        /// also called by the system when the resolution has been changed.
        /// </summary>
        public override void CF_localskinsetup()
        {
            WriteLog();

            // Read the skin file, controls will be automatically created
            // CF_localskinsetup() should always call CF3_initSection() first, with the exception of setting any
            // CF_displayHooks flags, which affect the behaviour of the CF3_initSection() call.
            this.CF3_initSection("Pandora");

            //CF_updateText("StationTitle", this.pluginLang.ReadField("/APPLANG/PANDORA/STATIONLISTTITLE"));

            //Wire up the buttons from the skin XML
            this.CF_createButtonClick("ThumbsUp", new MouseEventHandler(ThumbsUp_Click));
            this.CF_createButtonClick("ThumbsDown", new MouseEventHandler(ThumbsDown_Click));
            this.CF_createButtonClick("PlayStation", new MouseEventHandler(PlayStation_Click));
            this.CF_createButtonClick("CreateStation", new MouseEventHandler(CreateStation_Click));
            this.CF_createButtonClick("RefreshStations", new MouseEventHandler(RefreshStations_Click));
            this.CF_createButtonClick("GuestLogin", new MouseEventHandler(GuestLogin_Click));
            this.CF_createButtonClick("PageUp", new MouseEventHandler(PageUp_Click));
            this.CF_createButtonClick("PageDown", new MouseEventHandler(PageDown_Click));
            
            //Setup the list view selected index changed event
            //_stationsListView = listviewArray[0];
            //_stationsListView = advancedlistArray[0];
            _stationsListView = this.advancedlistArray[CF_getAdvancedListID("StationsList")];
            //_stationsListView.TemplateID = "StationTemplate";
            _stationsListView.AllowDeleter = true;
            _stationsListView.LinkedItemOnEnter = "Play";
            _stationsListView.LinkedItemOnSpace = "Favorite";
            _stationsListView.LinkedItemOnDelete = "Delete";
            _stationsListView.LinkedItemClick += new EventHandler<CFControlsExtender.Listview.LinkedItemArgs>(StationsListView_LinkedItemClick);
        }

        /// <summary>
        /// This is called by the system when it exits or the plugin has been deleted.
        /// </summary>
        public override void CF_pluginClose()
        {
            base.CF_pluginClose(); // calls form Dispose() method
        }

        /// <summary>
        /// This is called by the system when a button with this plugin action has been clicked.
        /// </summary>
        public override void CF_pluginShow()
        {
            base.CF_pluginShow(); // sets form Visible property
            _hasControl = true;
        }

        public override void CF_pluginHide()
        {
            base.CF_pluginHide();
            //_hasControl = false;
        }

        /// <summary>
        /// This is called by the system when the plugin setup is clicked.
        /// </summary>
        /// <returns>Returns the dialog result.</returns>
        public override DialogResult CF_pluginShowSetup()
        {
            // Return DialogResult.OK for the main application to update from plugin changes.
            DialogResult returnvalue = DialogResult.Cancel;

            try
            {
                // Creates a new plugin setup instance. If you create a CFDialog or CFSetup you must
                // set its MainForm property to the main plugins MainForm property.
                Setup setup = new Setup(this.MainForm, this.pluginConfig, this.pluginLang);
                returnvalue = setup.ShowDialog();

                if (returnvalue == DialogResult.OK)
                {
                    #region Check if the credentials have changed

                    //Here we check if the credentials have changed
                    string currentUsername = _userName;
                    string currentPassword = _password;

                    string newUsername = this.pluginConfig.ReadField("/APPCONFIG/USERNAME");
                    string newPassword = null;
                    string newEncryptedPassword = this.pluginConfig.ReadField("/APPCONFIG/PASSWORD");

                    if (!String.IsNullOrEmpty(newEncryptedPassword))
                    {
                        try
                        {
                            newPassword = EncryptionHelper.DecryptString(newEncryptedPassword, Setup.ENCRYPTION_PASSPHRASE);
                        }
                        catch (Exception ex)
                        {
                            WriteLog("Error during password decryption.", ex);
                        }
                    }

                    bool credentialsChanged = currentUsername != newUsername || currentPassword != newPassword;

                    #endregion

                    //If the credentials have changed, we need to stop everything
                    if (credentialsChanged)
                    {
                        WriteLog("Credentials have changed, ending session...");
                        EndSession();
                    }

                    //Load the new settings
                    LoadSettings();

                    //If the credentials have changed, lets try to refresh the stations list
                    if (credentialsChanged)
                        RefreshStations();
                }
                
                    

                setup.Close();
                setup = null;
            }
            catch (Exception errmsg) { CFTools.writeError(errmsg.ToString()); }

            return returnvalue;
        }

        /// <summary>
        /// This method is called by the system when it pauses all audio.
        /// </summary>
        public override void CF_pluginPause()
        {
            WriteLog();

            _timer.Stop();
            _hasControl = false;

            if (CF_getAudioStatus(_audioStream) == CF_AudioStatus.Playing)
            {
                WriteLog("Pausing audio stream");
                CF_controlAudioStream(_audioStream, CF_AudioAction.Pause);
                CF_setPlayPauseButton(true, _zone);
            }
        }

        /// <summary>
        /// This is called by the system when it resumes all audio.
        /// </summary>
        public override void CF_pluginResume()
        {
            WriteLog();

            _hasControl = true;

            if (CF_getAudioStatus(_audioStream) == CF_AudioStatus.Paused)
            {
                WriteLog("Resuming audio stream");
                CF_controlAudioStream(_audioStream, CF_AudioAction.Play);
                CF_setPlayPauseButton(false, _zone);
                _timer.Start();
            }
        }

        /// <summary>
        /// Used for plugin to plugin communication. Parameters can be passed into CF_Main_systemCommands
        /// with CF_Actions.PLUGIN, plugin name, plugin command, and a command parameter.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="param1">The first parameter.</param>
        /// <param name="param2">The second parameter.</param>
        public override void CF_pluginCommand(string command, string param1, string param2)
        {
        }

        /// <summary>
        /// Used for retrieving information from plugins. You can run CF_getPluginData with a plugin name,
        ///	command, and parameter to retrieve information from other plugins running on the system.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="param">The parameter.</param>
        /// <returns>Returns whatever is appropriate.</returns>
        public override string CF_pluginData(string command, string param)
        {
            return String.Empty;
        }

        /// <summary>
        /// Called on control clicks, down events, etc, if the control has a defined CML action parameter in the skin xml.
        /// </summary>
        /// <param name="id">The command to execute.</param>
        /// <param name="state">Button State.</param>
        /// <returns>Returns whatever is appropriate.</returns>
        public override bool CF_pluginCMLCommand(string id, string[] parameters, CF_ButtonState state, int zone)
        {
            if (!_hasControl)
                return false;

            if (state != CF_ButtonState.Click)
                return false;

            _zone = zone;

            switch (id)
            {
                case "Centrafuse.Main.PlayPause":
                    PlayPause();
                    return true;
                case "Centrafuse.Main.Rewind":
                    PreviousTrack();
                    return true;
                case "Centrafuse.Main.FastForward":
                    NextTrack();
                    return true;
            }

            return false;
        }

        public override string CF_pluginCMLData(CF_CMLTextItems textItem)
        {
            switch (textItem)
            {
                case CF_CMLTextItems.MainTitle:
                    return _currentSong == null ? String.Empty : _currentSong.SongTitle;
                case CF_CMLTextItems.MediaArtist:
                    return _currentSong == null ? String.Empty : _currentSong.ArtistSummary;
                case CF_CMLTextItems.MediaTitle:
                    return _currentSong == null ? String.Empty : _currentSong.SongTitle;
                case CF_CMLTextItems.MediaAlbum:
                    return _currentSong == null ? String.Empty : _currentSong.AlbumTitle;
                case CF_CMLTextItems.MediaSource:
                    return "Pandora";
                default:
                    return base.CF_pluginCMLData(textItem);
            }
        }

        #endregion

        #region UI Events

        private void PlayPause()
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            CF_AudioStatus audioStatus = CF_getAudioStatus(_audioStream);

            switch (audioStatus)
            {
                case CF_AudioStatus.Paused:
                    CF_controlAudioStream(_audioStream, CF_AudioAction.Play);
                    CF_setPlayPauseButton(false, _zone);
                    break;
                case CF_AudioStatus.Playing:
                    CF_controlAudioStream(_audioStream, CF_AudioAction.Pause);
                    CF_setPlayPauseButton(true, _zone);
                    break;
                case CF_AudioStatus.Stopped:
                    PlayNextSong();
                    break;
                case CF_AudioStatus.Stalled:
                    break;
            }
        }

        private void PreviousTrack()
        {
            WriteLog();
        }

        private void NextTrack()
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            PlayNextSong();
        }

        private void PageUp_Click(object sender, MouseEventArgs e)
        {
            WriteLog();
            _stationsListView.PageUp();
        }

        private void PageDown_Click(object sender, MouseEventArgs e)
        {
            WriteLog();
            _stationsListView.PageDown();
        }

        private void RefreshStations_Click(object sender, MouseEventArgs e)
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            RefreshStations();
        }

        private void PlayStation_Click(object sender, MouseEventArgs e)
        {
            WriteLog();
            PlayStation();
        }

        private void CreateStation_Click(object sender, MouseEventArgs e)
        {
            WriteLog();
            StationSearch();
        }

        private void ThumbsUp_Click(object sender, MouseEventArgs e)
        {
            WriteLog();

            if (_currentSong == null)
                return;

            if (_currentSong.Like)
                return;

            SubmitFeedback(_currentSong, Ratings.Like);
        }

        private void ThumbsDown_Click(object sender, MouseEventArgs e)
        {
            WriteLog();

            if (_currentSong == null)
                return;

            SubmitFeedback(_currentSong, Ratings.Dislike);
        }

        private void GuestLogin_Click(object sender, MouseEventArgs e)
        {
            WriteLog();
            ToggleGuest();
        }

        private void StationsListView_LinkedItemClick(object sender, CFControlsExtender.Listview.LinkedItemArgs e)
        {
            WriteLog();

            switch (e.LinkId)
            {
                case "Favorite":
                    {
                        if (_guest)
                            CF_displayMessage("Favorites are disabled for guest accounts.");
                        else
                        {
                            ToggleFavorite(_stationsTable.DefaultView[e.ItemId]["StationId"] as String);
                            _stationsListView.Refresh();
                        }
                    }
                    break;
                case "Delete":
                    DeleteStation(_stationsTable.DefaultView[e.ItemId]["StationId"] as String);
                    break;
                case "Play":
                    PlayStation();
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //WriteLog("tick " + DateTime.Now.ToShortTimeString());

            if (!_hasControl)
                _timer.Stop();
            else
            {
                if (CF_getAudioStatus(_audioStream) == CF_AudioStatus.Stopped)
                {
                    _timer.Stop();
                    PlayNextSong();
                }
            }
        }

        private void _pandoraClient_AuthenticateComplete(object sender, EventArgs<AuthenticationResult> e)
        {
            WriteLog();

            _subscriber = e.Value.Subscriber;

            _lastAuthenticated = DateTime.Now;

            _clientIsBusy = false;

            switch (_afterCallAction)
            {
                case "Refresh":
                    RefreshStations();
                    break;
                case "PlayNextSong":
                    PlayNextSong();
                    break;
                case "StationSearch":
                    StationSearch();
                    break;
                case "CreateStation":
                case "SubmitFeedback":
                case "DeleteStation":
                    CF_displayMessage("Unable to complete request; please try again.");
                    break;
            }
        }

        private void _pandoraClient_RetrieveStationsComplete(object sender, EventArgs<List<Station>> e)
        {
            WriteLog();

            _stations = e.Value;
            //_stationsListView.Items.Clear();
            _stationsListView.Clear();

            LoadStationList(e.Value);
            //foreach (Station station in _stations)
            //    _stationsListView.Items.Add(new CFControls.CFListViewItem(station.StationName, station.StationId));

            //HACK to get the list to display
            //_stationsListView.pageUp();

            _clientIsBusy = false;
        }

        private void _pandoraClient_RetrieveSongsComplete(object sender, EventArgs<List<Song>> e)
        {
            WriteLog();

            foreach (Song song in e.Value)
                _songs.Enqueue(song);

            _clientIsBusy = false;

            switch (_afterCallAction)
            {
                case "PlayNextSong":
                    PlayNextSong();
                    break;
            }
        }

        private void _pandoraClient_SearchComplete(object sender, EventArgs<List<SearchResult>> e)
        {
            WriteLog();

            CF_systemCommand(CF_Actions.HIDEINFO);
            _clientIsBusy = false;

            if (e.Value == null || e.Value.Count == 0)
                CF_displayMessage("No results found.");
            else
                ShowSearchResults(e.Value);
        }

        private void _pandoraClient_CreateStationComplete(object sender, EventArgs<Station> e)
        {
            WriteLog();

            _clientIsBusy = false;

            AddStationToList(e.Value);
        }

        private void _pandoraClient_RemoveStationComplete(object sender, EventArgs e)
        {
            WriteLog();

            _clientIsBusy = false;

            if (_currentStationId == _stationIdToRemove)
                StopStation();

            RemoveStationFromList(_stationIdToRemove);
            _stationIdToRemove = null;
        }

        private void _pandoraClient_SubmitFeedbackComplete(object sender, EventArgs e)
        {
            WriteLog();

            _clientIsBusy = false;

            if (_songToRate == _currentSong)
            {
                if (_ratingToSubmit == Ratings.Like)
                    CF_setPictureImage("ThumbsUpActive", _activeLikeIcon);
                else if (_ratingToSubmit == Ratings.Dislike)
                    PlayNextSong();
            }

            _songToRate = null;
        }

        private void _pandoraClient_ExceptionReceived(object sender, EventArgs<Exception> e)
        {
            WriteLog();

            CF_systemCommand(CF_Actions.HIDEINFO);

            _lastAuthenticated = DateTime.MinValue;
            _afterCallAction = null;

            WriteLog("An exception occured while calling the Pandora client.", e.Value);

            if (e.Value.Data != null)
            {
                foreach (DictionaryEntry entry in e.Value.Data)
                    WriteLog(String.Format("Additional Info ({0}): {1}", entry.Key, entry.Value));
            }

            if (e.Value.InnerException != null)
                WriteLog("Inner Exception", e.Value.InnerException);

            CFDialogParams dialogParams = new CFDialogParams();
            dialogParams.displaytext = String.Format("{0} Plugin Error:{1}{2}", _pluginName, Environment.NewLine, e.Value.Message);
            CF_displayDialog(CF_Dialogs.OkBoxBig, dialogParams);

            _clientIsBusy = false;
        }

        private void _albumArtWebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            WriteLog();

            Song song = e.UserState as Song;

            if (_currentSong != song)
            {
                WriteLog("Album art was retrieved, however it is no longer applicable to the currently playing song.");
                return;
            }

            try
            {
                string imagePath = GetLocalAlbumArtFileName(song.ArtRadio);
                CF_setPictureImage("AlbumArtPicture", Image.FromFile(imagePath));
            }
            catch (Exception ex)
            {
                WriteLog("Error displaying album art.", ex);
            }
        }

        private void CF_Events_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            WriteLog();
            //TODO ???
        }

        #endregion

        #region Methods

        private void LoadSettings()
        {
            //Need to check this first because other stuff may need to write logs
            string logEvents = this.pluginConfig.ReadField("/APPCONFIG/LOGEVENTS");
            Boolean.TryParse(logEvents, out _logEvents);

            //Display name for the plugin
            this.CF_params.displayName = this.pluginLang.ReadField("/APPLANG/PANDORA/DISPLAYNAME");

            _userName = this.pluginConfig.ReadField("/APPCONFIG/USERNAME");

            string _encryptedPassword = this.pluginConfig.ReadField("/APPCONFIG/PASSWORD");

            if (!String.IsNullOrEmpty(_encryptedPassword))
            {
                try
                {
                    _password = EncryptionHelper.DecryptString(_encryptedPassword, Setup.ENCRYPTION_PASSPHRASE);
                }
                catch (Exception ex)
                {
                    WriteLog("Error during password decryption.", ex);
                }
            }

            string favoritesString = this.pluginConfig.ReadField("/APPCONFIG/FAVORITES");
            _favorites.Clear();

            if (!String.IsNullOrEmpty(favoritesString))
                _favorites.AddRange(favoritesString.Split(';'));

            try
            {
                _audioFormat = (AudioFormats)Enum.Parse(typeof(AudioFormats), this.pluginConfig.ReadField("/APPCONFIG/AUDIOFORMAT"));
            }
            catch (Exception ex)
            {
                WriteLog("Unable to parse AudioFormat.", ex);
            }

            try
            {
                _clearCache = Boolean.Parse(this.pluginConfig.ReadField("/APPCONFIG/CLEARCACHE"));
            }
            catch (Exception ex)
            {
                WriteLog("Unable to parse ClearCache.", ex);
            }
        }

        private bool InitPandoraClient()
        {
            WriteLog();

            if (_pandoraClient != null)
                return true;

            if (IsNullOrWhiteSpace(_userName) || IsNullOrWhiteSpace(_password))
            {
                if (_hasControl)
                {
                    string message = "User name and/or password are not configured.";
                    WriteLog(message);
                    CF_displayMessage(String.Format("{0} Plugin: {1}", _pluginName, message));
                }
                return false;
            }

            _pandoraClient = new PandoraClient();

            _pandoraClient.AuthenticateComplete += new EventHandler<EventArgs<AuthenticationResult>>(_pandoraClient_AuthenticateComplete);
            _pandoraClient.RetrieveStationsComplete += new EventHandler<EventArgs<List<Station>>>(_pandoraClient_RetrieveStationsComplete);
            _pandoraClient.RetrieveSongsComplete += new EventHandler<EventArgs<List<Song>>>(_pandoraClient_RetrieveSongsComplete);
            _pandoraClient.SearchComplete += new EventHandler<EventArgs<List<SearchResult>>>(_pandoraClient_SearchComplete);
            _pandoraClient.CreateStationComplete += new EventHandler<EventArgs<Station>>(_pandoraClient_CreateStationComplete);
            _pandoraClient.RemoveStationComplete += new EventHandler(_pandoraClient_RemoveStationComplete);
            _pandoraClient.SubmitFeedbackComplete += new EventHandler(_pandoraClient_SubmitFeedbackComplete);
            _pandoraClient.ExceptionReceived += new EventHandler<EventArgs<Exception>>(_pandoraClient_ExceptionReceived);

            return true;
        }

        private void RefreshStations()
        {
            WriteLog();

            if (!EnsureAuthenticated("Refresh"))
                return;

            _clientIsBusy = true;
            _afterCallAction = null;
            _pandoraClient.RetrieveStationsAsync();
        }

        private void PlayStation()
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            _timer.Stop();
            _songs.Clear();

            if (_stationsListView.SelectedIndex == -1)
                _currentStationId = null;
            else
                _currentStationId = _stationsTable.DefaultView[_stationsListView.SelectedIndex]["StationId"] as string;

            Station currentStation = _stations.Find(delegate(Station item)
            {
                return item.StationId == _currentStationId;
            });

            CF_updateText("StationName", currentStation == null ? String.Empty : currentStation.StationName);

            PlayNextSong();
        }

        private void StopStation()
        {
            _timer.Stop();

            CF_controlAudioStream(_audioStream, CF_AudioAction.Stop);
            CF_controlAudioStream(_audioStream, CF_AudioAction.Free);
            CF_setPlayPauseButton(true, _zone);

            _currentStationId = null;
            _songs.Clear();

            CF_updateText("StationName", String.Empty);
            CF_updateText("CurrentSongInfo", String.Empty);
            CF_setPictureImage("AlbumArtPicture", _defaultAlbumArt);
        }

        private void PlayNextSong()
        {
            WriteLog();

            if (String.IsNullOrEmpty(_currentStationId))
                CF_displayMessage("Please select a station first.");
            else if (_songs.Count == 0)
            {
                if (!EnsureAuthenticated("PlayNextSong"))
                    return;

                _clientIsBusy = true;
                _afterCallAction = "PlayNextSong";
                _pandoraClient.RetrieveSongsAsync(_currentStationId, GetAudioFormat());
            }
            else
            {
                _timer.Stop();

                CF_controlAudioStream(_audioStream, CF_AudioAction.Stop);
                CF_controlAudioStream(_audioStream, CF_AudioAction.Free);
                CF_setPlayPauseButton(true, _zone);

                _currentSong = _songs.Dequeue();

                CF_updateText("CurrentSongInfo", String.Format("{0} / {1} / {2}", _currentSong.SongTitle, _currentSong.ArtistSummary, _currentSong.AlbumTitle));

                if (_currentSong.Like)
                    CF_setPictureImage("ThumbsUpActive", _activeLikeIcon);
                else
                    CF_setPictureImage("ThumbsUpActive", null);

                CF_setPictureImage("AlbumArt", _defaultAlbumArt);

                string audioUrl = _currentSong.AudioUrl;

                try
                {
                    //HACK - The audio complete callback handler apparently isn't implemented
                    //so we pass null and use a timer to check manually... so lame!
                    _audioStream = CF_playAudioStream(audioUrl, null);
                    CF_setPlayPauseButton(false, _zone);
                }
                catch (Exception ex)
                {
                    _timer.Stop();
                    WriteLog("Unable to play audio stream.", ex);
                    CFDialogParams dialogParams = new CFDialogParams();
                    dialogParams.displaytext = String.Format("{0} Plugin: Unable to play audio stream ({1})", _pluginName, ex.Message);
                    CF_displayDialog(CF_Dialogs.OkBoxBig, dialogParams);
                    return;
                }

                _timer.Start();

                try
                {
                    if (!String.IsNullOrEmpty(_currentSong.ArtRadio))
                    {
                        string imagePath = GetLocalAlbumArtFileName(_currentSong.ArtRadio);

                        if (File.Exists(imagePath))
                            CF_setPictureImage("AlbumArtPicture", Image.FromFile(imagePath));
                        else
                            _albumArtWebClient.DownloadFileAsync(new Uri(_currentSong.ArtRadio), imagePath, _currentSong);
                    }
                }
                catch (Exception ex)
                {
                    WriteLog("Error retrieving album art.", ex);
                }

                if (_songs.Count < 2)
                {
                    if (!EnsureAuthenticated())
                        return;

                    _clientIsBusy = true;
                    _afterCallAction = null;
                    _pandoraClient.RetrieveSongsAsync(_currentStationId, GetAudioFormat());
                }
            }
        }

        private bool EnsureAuthenticated()
        {
            return EnsureAuthenticated(null);
        }

        private bool EnsureAuthenticated(string afterAuthenticateCallback)
        {
            WriteLog();

            bool initSuccessful = InitPandoraClient();

            if (!initSuccessful)
                return false;

            if (_lastAuthenticated == DateTime.MinValue || _lastAuthenticated < DateTime.Now.AddHours(-1))
            {
                WriteLog(String.Format("Last successful authentication attempt was {0}; attemping to re-authenticate.", _lastAuthenticated.ToString()));
                _clientIsBusy = true;
                _afterCallAction = afterAuthenticateCallback;
                _pandoraClient.AuthenticateAsync(_userName, _password);
                return false;
            }
            else
                return true;
        }

        private void LoadStationList(List<Station> stations)
        {
            WriteLog();

            _stationsTable.Clear();

            BindingSource bindingSource = new BindingSource();

            foreach (Station station in stations)
            {
                System.Data.DataRow dataRow = _stationsTable.NewRow();
                dataRow["StationId"] = station.StationId;
                dataRow["StationName"] = station.StationName;
                dataRow["Favorite"] = _favorites.Contains(station.StationId);
                _stationsTable.Rows.Add(dataRow);
            }
            
            _stationsTable.DefaultView.Sort = "Favorite DESC, StationName ASC";
            bindingSource.DataSource = _stationsTable.DefaultView.Table;
            _stationsListView.DataBinding = bindingSource;
            _stationsListView.Refresh();
        }

        private void ToggleFavorite(string stationId)
        {
            WriteLog();

            bool favorite = false;

            if (_favorites.Contains(stationId))
            {
                _favorites.Remove(stationId);
                favorite = false;
            }
            else
            {
                _favorites.Add(stationId);
                favorite = true;
            }

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < _favorites.Count; i++)
            {
                stringBuilder.Append(_favorites[i]);

                if (i != _favorites.Count - 1)
                    stringBuilder.Append(";");
            }

            //Write to config
            this.pluginConfig.WriteField("/APPCONFIG/FAVORITES", stringBuilder.ToString());
            this.pluginConfig.Save();

            //Update the DataTable
            foreach (System.Data.DataRow dataRow in _stationsTable.Rows)
            {
                if (dataRow["StationId"] as string == stationId)
                {
                    dataRow["Favorite"] = favorite;
                    dataRow.AcceptChanges();
                    break;
                }
            }
        }

        private void ToggleGuest()
        {
            WriteLog();

            if (_guest)
            {
                CFDialogParams dialogParams = new CFDialogParams();
                dialogParams.displaytext = String.Format("Are you sure you want to logoff of the guest account {0} ?", _userName);
                DialogResult dialogResult = CF_displayDialog(CF_Dialogs.YesNo, dialogParams);

                if (dialogResult == DialogResult.OK)
                {
                    EndSession();
                    _guest = false;
                    CF_setPictureImage("GuestActive", null);

                    //Load the stored credentials, favorites, etc back in
                    LoadSettings();

                    RefreshStations();
                }
            }
            else
            {
                string guestUsername = null;
                string guestPassword = null;

                CFDialogParams dialogParams = new CFDialogParams();
                CFDialogResults dialogResults = new CFDialogResults();
                DialogResult dialogResult;

                dialogParams.displaytext = this.pluginLang.ReadField("/APPLANG/PANDORA/USERNAMEPROMPT");
                dialogResult = CF_displayDialog(CF_Dialogs.OSK, dialogParams, dialogResults);

                if (dialogResult == DialogResult.Cancel)
                    return;

                if (IsNullOrWhiteSpace(dialogResults.resultvalue))
                {
                    CF_displayMessage("Invalid user name.");
                    return;
                }

                guestUsername = dialogResults.resultvalue;

                //UGH! More SDK inconsistencies... passing "PASSWORD" as param2 here doesn't invoke the password
                //masking feature, so I have to use the full blown CF_systemDisplayDialog method instead. Lame.
                //dialogParams.displaytext = this.pluginLang.ReadField("/APPLANG/PANDORA/PASSWORDPROMPT");
                //dialogParams.param2 = "PASSWORD";
                //dialogResult = CF_displayDialog(CF_Dialogs.OSK, dialogParams, dialogResults);
                object tempObject;
                string resultValue, resultText;
                dialogResult = this.CF_systemDisplayDialog(CF_Dialogs.OSK, this.pluginLang.ReadField("/APPLANG/PANDORA/PASSWORDPROMPT"), String.Empty, "PASSWORD", out resultValue, out resultText, out tempObject, null, true, true, true, true, false, false, 1);

                if (dialogResult == DialogResult.Cancel)
                    return;

                if (IsNullOrWhiteSpace(resultValue))
                {
                    CF_displayMessage("Invalid password.");
                    return;
                }

                guestPassword = resultValue;

                EndSession();
                _guest = true;
                CF_setPictureImage("GuestActive", _activeGuestIcon);
                _userName = guestUsername;
                _password = guestPassword;
                RefreshStations();
            }
        }

        private void DeleteStation(string stationId)
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            if (!EnsureAuthenticated("DeleteStation"))
                return;

            Station station = _stations.Find(delegate(Station item)
            {
                return item.StationId == stationId;
            });

            if (station == null)
                return;

            CFDialogParams dialogParams = new CFDialogParams();
            dialogParams.displaytext = String.Format("{0}{1}{1}Are you sure you want to delete this station?", station.StationName, Environment.NewLine);
            DialogResult result = CF_displayDialog(CF_Dialogs.YesNo, dialogParams);

            if (result == DialogResult.OK)
            {
                _clientIsBusy = true;
                _stationIdToRemove = station.StationId;
                _pandoraClient.RemoveStationAsync(station.StationId);
            }
        }

        private void StationSearch()
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            if (!EnsureAuthenticated("StationSearch"))
                return;

            CFDialogParams dialogParams = new CFDialogParams();
            dialogParams.displaytext = "Enter the name of an artist or song";
            CFDialogResults dialogResults = new CFDialogResults();

            DialogResult dialogResult = CF_displayDialog(CF_Dialogs.OSK, dialogParams, dialogResults);

            if (dialogResult == DialogResult.OK && !IsNullOrWhiteSpace(dialogResults.resultvalue))
            {
                CF_systemCommand(CF_Actions.SHOWINFO, "Searching...");
                _clientIsBusy = true;
                _pandoraClient.SearchAsync(dialogResults.resultvalue);
            }
        }

        private void ShowSearchResults(List<SearchResult> searchResults)
        {
            WriteLog();

            if (searchResults == null || searchResults.Count == 0)
                return;

            searchResults.Sort(delegate(SearchResult searchResult1, SearchResult searchResult2)
            {
                if (searchResult1.SearchResultType == searchResult2.SearchResultType)
                    return 0;
                else if (searchResult1.SearchResultType == SearchResultTypes.Artist &&
                            searchResult2.SearchResultType == SearchResultTypes.Song)
                    return -1;
                else if (searchResult1.SearchResultType == SearchResultTypes.Song &&
                            searchResult2.SearchResultType == SearchResultTypes.Artist)
                    return 1;
                else
                    return 0; //Shouldn't happen
            });

            CFControls.CFListViewItem[] listItems = new CFControls.CFListViewItem[searchResults.Count];

            for (int i = 0; i < searchResults.Count; i++)
                listItems[i] = new CFControls.CFListViewItem(searchResults[i].ResultText, searchResults[i].MusicId, 0, false, searchResults[i] as object);

            CFDialogParams dialogParams = new CFDialogParams();
            CFDialogResults dialogResults = new CFDialogResults();
            dialogParams.displaytext = "Choose a result from the list to create a station.";
            dialogParams.listitems = listItems;

            DialogResult dialogResult = CF_displayDialog(CF_Dialogs.FileBrowser, dialogParams, dialogResults);

            if (dialogResult != DialogResult.OK)
                return;

            if (!EnsureAuthenticated("CreateStation"))
                return;

            _clientIsBusy = true;
            _pandoraClient.CreateStationAsync(dialogResults.resultvalue);
        }

        private void AddStationToList(Station station)
        {
            WriteLog();

            Station existingStation = _stations.Find(delegate(Station item)
            {
                return item.StationId == station.StationId;
            });

            if (existingStation == null)
            {
                _stations.Add(station);
                LoadStationList(_stations);
                SelectStation(station.StationId);
            }
        }

        private void RemoveStationFromList(string stationId)
        {
            WriteLog();

            if (String.IsNullOrEmpty(stationId))
                return;

            _stations.RemoveAll(delegate(Station item)
            {
                return item.StationId == stationId;
            });

            LoadStationList(_stations);
        }

        private void SelectStation(string stationId)
        {
            if (String.IsNullOrEmpty(stationId))
                return;

            for (int i = 0; i < _stationsTable.DefaultView.Table.Rows.Count; i++)
            {
                if (_stationsTable.DefaultView[i]["StationId"] as string == stationId)
                {
                    _stationsListView.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SubmitFeedback(Song song, Ratings rating)
        {
            WriteLog();

            if (_clientIsBusy)
                return;

            if (!EnsureAuthenticated("SubmitFeedback"))
                return;

            if (song == null)
                return;

            if (rating == Ratings.Dislike)
            {
                CFDialogParams dialogParams = new CFDialogParams();
                dialogParams.displaytext = "Are you sure you want to dislike this song?";
                DialogResult dialogResult = CF_displayDialog(CF_Dialogs.YesNo, dialogParams);

                if (dialogResult != DialogResult.OK)
                    return;
            }

            _clientIsBusy = true;
            _songToRate = song;
            _ratingToSubmit = rating;
            _pandoraClient.SubmitFeedbackAsync(song, rating);
        }

        private void EndSession()
        {
            WriteLog();

            _timer.Stop();
            CF_controlAudioStream(_audioStream, CF_AudioAction.Stop);
            CF_setPlayPauseButton(true, _zone);
            _lastAuthenticated = DateTime.MinValue;
            _currentStationId = null;
            _currentSong = null;
            _stations.Clear();
            _songs.Clear();
            _favorites.Clear();
            _stationsTable.Clear();
            _stationsListView.Clear();
            CF_updateText("StationName", String.Empty);
            CF_updateText("CurrentSongInfo", String.Empty);
            CF_setPictureImage("AlbumArtPicture", _defaultAlbumArt);
        }

        #endregion

        #region Helper Methods

        private void WriteLog()
        {
            if (_logEvents)
                CFTools.writeModuleLog(String.Empty, _logFilePath);
        }

        private void WriteLog(string logMessage)
        {
            #if DEBUG
            Console.WriteLine(logMessage);
            #endif

            if (_logEvents)
                CFTools.writeModuleLog(logMessage, _logFilePath);
        }

        private void WriteLog(string logMessage, Exception ex)
        {
            #if DEBUG
            Console.WriteLine(logMessage);
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            #endif

            if (_logEvents)
            {
                WriteLog(logMessage);
                WriteLog(ex.Message);
                WriteLog(ex.StackTrace);
            }
        }

        private string GetLocalAlbumArtFileName(string albumArtUrl)
        {
            if (String.IsNullOrEmpty(albumArtUrl))
                return null;

            int indexOfLastSlash = albumArtUrl.LastIndexOf("/");
            return CFTools.AppDataPath + "\\Plugins\\Pandora\\AlbumArt\\" + albumArtUrl.Substring(indexOfLastSlash, albumArtUrl.Length - indexOfLastSlash);
        }

        public static bool IsNullOrWhiteSpace(string value)
        {
            if (String.IsNullOrEmpty(value))
                return true;

            return String.IsNullOrEmpty(value.Trim());
        }

        public AudioFormats GetAudioFormat()
        {
            if (_audioFormat == AudioFormats.MP3_HIFI && _subscriber == false)
                return AudioFormats.MP3;
            else
                return _audioFormat;
        }

        #endregion
	}
}
