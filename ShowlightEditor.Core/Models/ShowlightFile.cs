using System.Collections.Generic;
using System.Xml.Serialization;
using XmlUtils;

namespace ShowlightEditor.Core.Models
{
    [XmlRoot("showlights", Namespace = "")]
    public sealed class ShowlightFile : XmlCountList<Showlight>
    {
        public static void Save(string filename, IEnumerable<Showlight> showlights)
        {
            ShowlightFile fileToSave = new ShowlightFile();
            fileToSave.AddRange(showlights);

            XmlHelper.Serialize(filename, fileToSave);
        }
    }
}
