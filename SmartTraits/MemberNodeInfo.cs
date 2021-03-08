using Microsoft.CodeAnalysis.CSharp;

namespace SmartTraits
{
    public class MemberNodeInfo
    {
        public string Name;
        public SyntaxKind Kind;
        public string Arguments = "";
        public string ReturnType = "";
        public int GenericsCount;
    }
}
