﻿using System.Configuration;

namespace Game.Machine
{
    public static class GameSettings
    {
        public static string Xml { get { return ConfigurationManager.AppSettings["Game.Round.Index"]; } }
        public static string RoundSpan { get { return ConfigurationManager.AppSettings["Game.Round.Span"]; } }
    }
}
