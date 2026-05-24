using System;
using System.Collections.Generic;

[Serializable]
public class WordEntry
{
    public string word;          // Ç¥½Ã¿ë (¿¹: ´ßººÀÌ)
    public string pronunciation;  // ÆÇÁ¤¿ë (¿¹: ´Ú»Ç³¢) [cite: 33]
}

[Serializable]
public class WordBankData
{
    public List<WordEntry> wordList;
}