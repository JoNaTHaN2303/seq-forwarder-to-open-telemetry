namespace Seq.Forwarder.SerilogJsonConverter
{
    public class MessageTemplateSyntax
    {
        public static string Escape(string text)
        {
            return text.Replace("{", "{{").Replace("}", "}}");
        }
    }
}
