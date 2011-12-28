using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    public class Song
    {
        private string _audioUrl;

        public string AudioUrl
        {
            get { return _audioUrl; }
            set { _audioUrl = value; }
        }

        private string _artistSummary;

        public string ArtistSummary
        {
            get { return _artistSummary; }
            set { _artistSummary = value; }
        }

        private string _albumTitle;

        public string AlbumTitle
        {
            get { return _albumTitle; }
            set { _albumTitle = value; }
        }

        private string _songTitle;

        public string SongTitle
        {
            get { return _songTitle; }
            set { _songTitle = value; }
        }

        private string _artRadio;

        public string ArtRadio
        {
            get { return _artRadio; }
            set { _artRadio = value; }
        }

        private bool _like;

        public bool Like
        {
            get { return _like; }
            set { _like = value; }
        }

        private string _musicId;

        public string MusicId
        {
            get { return _musicId; }
            set { _musicId = value; }
        }

        private string _stationId;

        public string StationId
        {
            get { return _stationId; }
            set { _stationId = value; }
        }

        private string _userSeed;

        public string UserSeed
        {
            get { return _userSeed; }
            set { _userSeed = value; }
        }

        private int songType;

        public int SongType
        {
            get { return songType; }
            set { songType = value; }
        }
    }
}
