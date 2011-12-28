using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    public class Station
    {
        private string _stationId;

        public string StationId
        {
            get { return _stationId; }
            set { _stationId = value; }
        }

        private string _stationName;

        public string StationName
        {
            get { return _stationName; }
            set { _stationName = value; }
        }
    }
}
