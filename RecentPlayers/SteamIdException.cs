// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;

namespace RecentPlayers
{
    internal class SteamIdException : Exception
    {
        public SteamIdException(string message) : base(message)
        {
        }

        public SteamIdException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
