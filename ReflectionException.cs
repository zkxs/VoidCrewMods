// This file is part of RecentPlayers and is licensed under the MIT License.
// See LICENSE file for full text.
// Copyright © 2024 Michael Ripley

using System;

namespace RecentPlayers
{
    [Serializable]
    internal class ReflectionException : Exception
    {
        public ReflectionException(string message) : base(message)
        {
        }
    }
}
