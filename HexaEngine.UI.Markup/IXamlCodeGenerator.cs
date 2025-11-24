using System.Xml;
using HexaEngine.UI.XamlGen;

namespace HexaEngine.UI.Markup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IXamlCodeGenerator
    {
        public void GenerateCode(CodeWriter writer, XmlReader reader);
    }
}
