using System.Collections.Generic;

public static class JamoSplitter
{
    // 이중모음 매핑 테이블 (한글 복구 버전)
    private static readonly Dictionary<string, string[]> DoubleVowelMap = new Dictionary<string, string[]>
    {
        { "ㅐ", new[] { "ㅏ", "ㅣ" } },
        { "ㅒ", new[] { "ㅑ", "ㅣ" } },
        { "ㅔ", new[] { "ㅓ", "ㅣ" } },
        { "ㅖ", new[] { "ㅕ", "ㅣ" } },
        { "ㅘ", new[] { "ㅗ", "ㅏ" } },
        { "ㅙ", new[] { "ㅗ", "ㅐ" } },
        { "ㅚ", new[] { "ㅗ", "ㅣ" } },
        { "ㅝ", new[] { "ㅜ", "ㅓ" } },
        { "ㅞ", new[] { "ㅜ", "ㅔ" } },
        { "ㅟ", new[] { "ㅜ", "ㅣ" } },
        { "ㅢ", new[] { "ㅡ", "ㅣ" } }
    };

    public static string[] Split(string jamo)
    {
        if (DoubleVowelMap.ContainsKey(jamo))
        {
            return DoubleVowelMap[jamo];
        }
        return new[] { jamo };
    }
}