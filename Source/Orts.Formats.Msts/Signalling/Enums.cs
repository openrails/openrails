namespace Orts.Formats.Msts.Signalling
{
    internal enum TokenType
    {
        Value = 0x00,
        Operator,               // ! & | ^ + - * / % #
        Tab = 0x09,             // \t
        LineEnd = 0x0a,         // \n
        Separator = 0x20,       // blank
        BracketOpen = 0x28,     // (
        BracketClose = 0x29,    // )
        Comma = 0x2c,           // ,
        StatementEnd = 0x3b,    // ;
        BlockOpen = 0x7b,       // {
        BlockClose = 0x7d,      // }
    }
    internal enum CommentParserState
    {
        None,
        OpenComment,
        EndComment,
        Operator,
    }

}
