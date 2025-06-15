using System;
using AAYInvisionaryTTSPlayer.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace AAYInvisionaryTTSPlayer
{
    public class ViewLocator : IDataTemplate
    {
        public bool SupportsRecycling => false;

        public Control Build(object data)
        {
            var fullName = data.GetType().FullName;
            if (fullName != null)
            {
                var name = fullName.Replace("ViewModel", "View");
                var type = Type.GetType(name);

                if (type != null)
                {
                    return (Control)Activator.CreateInstance(type);
                }
                else
                {
                    return new TextBlock { Text = "Not Found: " + name };
                }
            }
            throw new Exception("Object name was null");
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}