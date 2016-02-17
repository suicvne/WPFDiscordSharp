using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#if WIN10NOTIF
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
#endif

namespace CustomDiscordClient
{
    public class ToastManager
    {
        private string APP_ID;
        public string GetAppID => APP_ID;
        public ToastManager(string appid)
        {
            APP_ID = appid;
        }
        public ToastNotification CreateToast(string title)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText01);
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            for (int i = 0; i < stringElements.Length; i++)
            {
                stringElements[i].AppendChild(toastXml.CreateTextNode(title));
            }
            return new ToastNotification(toastXml);
        }

        public ToastNotification CreateToast(string title, string message)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            for (int i = 0; i < stringElements.Length; i++)
            {
                if (i == 0)
                    stringElements[i].AppendChild(toastXml.CreateTextNode(title));
                else
                    stringElements[i].AppendChild(toastXml.CreateTextNode(message));
            }
            return new ToastNotification(toastXml);
        }

        public ToastNotification CreateToast(string imagePath, string title, string messageLine1, string messageLine2)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            for (int i = 0; i < stringElements.Length; i++)
            {
                if (i == 0)
                    stringElements[i].AppendChild(toastXml.CreateTextNode(title));
                else if(i == 1)
                    stringElements[i].AppendChild(toastXml.CreateTextNode(messageLine1));
                else if(i == 2)
                    stringElements[i].AppendChild(toastXml.CreateTextNode(messageLine2));
            }
            string ImagePath = "file:////" + imagePath;
            XmlNodeList imageElemtns = toastXml.GetElementsByTagName("image");
            imageElemtns[0].Attributes.GetNamedItem("src").NodeValue = ImagePath;
            return new ToastNotification(toastXml);
        }
    }
}
