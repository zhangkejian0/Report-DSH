using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>规格校验失败：调用方给出的 JSON 不满足工具契约。</summary>
internal class SpecException : Exception
{
    public SpecException(string message) : base(message) { }
}


/// <summary>
/// 最小 JSON 解析器（对象/数组/字符串/数字/true/false/null），供 MarkImage 读取标记规格。
/// 数字统一解析为 double；字符串去转义后返回。
/// </summary>
sealed class JsonParser
{
    readonly string text;
    int pos;

    public JsonParser(string text) { this.text = text; }

    public object Parse()
    {
        object value = ParseValue();
        SkipSpace();
        if (pos != text.Length) throw new SpecException("trailing characters at " + pos);
        return value;
    }

    object ParseValue()
    {
        SkipSpace();
        if (pos >= text.Length) throw new SpecException("unexpected end of spec");
        char c = text[pos];
        switch (c)
        {
            case '{': return ParseObject();
            case '[': return ParseArray();
            case '"': return ParseString();
            case 't': Expect("true"); return true;
            case 'f': Expect("false"); return false;
            case 'n': Expect("null"); return null;
            default: return ParseNumber();
        }
    }

    Dictionary<string, object> ParseObject()
    {
        pos++; // '{'
        var result = new Dictionary<string, object>();
        SkipSpace();
        if (pos < text.Length && text[pos] == '}') { pos++; return result; }
        for (; ; )
        {
            SkipSpace();
            string key = ParseString();
            SkipSpace();
            if (pos >= text.Length || text[pos] != ':') throw new SpecException("expected ':' at " + pos);
            pos++;
            result[key] = ParseValue();
            SkipSpace();
            if (pos >= text.Length) throw new SpecException("unterminated object");
            if (text[pos] == ',') { pos++; continue; }
            if (text[pos] == '}') { pos++; return result; }
            throw new SpecException("expected ',' or '}' at " + pos);
        }
    }

    List<object> ParseArray()
    {
        pos++; // '['
        var result = new List<object>();
        SkipSpace();
        if (pos < text.Length && text[pos] == ']') { pos++; return result; }
        for (; ; )
        {
            result.Add(ParseValue());
            SkipSpace();
            if (pos >= text.Length) throw new SpecException("unterminated array");
            if (text[pos] == ',') { pos++; continue; }
            if (text[pos] == ']') { pos++; return result; }
            throw new SpecException("expected ',' or ']' at " + pos);
        }
    }

    string ParseString()
    {
        SkipSpace();
        if (pos >= text.Length || text[pos] != '"') throw new SpecException("expected string at " + pos);
        pos++;
        var sb = new StringBuilder();
        while (pos < text.Length && text[pos] != '"')
        {
            char c = text[pos];
            if (c == '\\')
            {
                pos++;
                if (pos >= text.Length) throw new SpecException("unterminated escape");
                char e = text[pos];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (pos + 4 >= text.Length) throw new SpecException("short \\u escape");
                        sb.Append((char)int.Parse(text.Substring(pos + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        pos += 4;
                        break;
                    default: throw new SpecException("bad escape \\" + e);
                }
                pos++;
            }
            else { sb.Append(c); pos++; }
        }
        if (pos >= text.Length) throw new SpecException("unterminated string");
        pos++; // closing '"'
        return sb.ToString();
    }

    double ParseNumber()
    {
        int start = pos;
        while (pos < text.Length && "+-0123456789.eE".IndexOf(text[pos]) >= 0) pos++;
        double value;
        if (!double.TryParse(text.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            throw new SpecException("bad number at " + start);
        return value;
    }

    void Expect(string word)
    {
        if (string.CompareOrdinal(text, pos, word, 0, word.Length) != 0)
            throw new SpecException("expected " + word + " at " + pos);
        pos += word.Length;
    }

    void SkipSpace()
    {
        while (pos < text.Length && char.IsWhiteSpace(text[pos])) pos++;
    }
}
