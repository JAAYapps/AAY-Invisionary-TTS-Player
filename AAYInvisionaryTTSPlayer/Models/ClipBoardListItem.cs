using System;
using System.Collections.Generic;
using System.Text;

namespace AAYInvisionaryTTSPlayer.Models
{
    public class ClipBoardListItem
    {
        public ClipBoardListItem(string preview, string text)
        {
            Preview = preview;
            Text = text;
        }

        public string Preview { get; set; }
        public string Text { get; set; }
        public bool IsChecked { get; set; }
    }
}
