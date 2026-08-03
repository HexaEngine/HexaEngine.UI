using HexaEngine.UI.XamlGen;

namespace HexaEngine.UI.Markup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class XamlCodeGenContext
    {
        private readonly Stack<ElementContext> elementStack = new();
        private ElementContext currentElement;

        public Stack<ElementContext> ElementStack => elementStack;

        public ElementContext ParentContext => ElementStack.Peek();

        public ref ElementContext CurrentElement => ref currentElement;

        public void PushElement(ElementContext context)
        {
            ElementStack.Push(CurrentElement);
            CurrentElement = context;
        }
    }
}
