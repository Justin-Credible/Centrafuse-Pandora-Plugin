using System;
using System.Collections.Generic;
using System.Text;

namespace PandoraSharp
{
    public class AuthenticationResult
    {
        private bool _subscriber;

        public bool Subscriber
        {
            get { return _subscriber; }
            set { _subscriber = value; }
        }

        private int _subscriptionDaysLeft;

        public int SubscriptionDaysLeft
        {
            get { return _subscriptionDaysLeft; }
            set { _subscriptionDaysLeft = value; }
        }
    }
}
