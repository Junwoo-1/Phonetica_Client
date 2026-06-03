using System;
using System.Collections.Generic;

[Serializable]
public class WordEntry
{
    public string word;           // 표시용 (예: 일하다)
    public string pronunciation;  // 판정용 (예: 일하다)
}

// [NEW] 카테고리 하나를 통째로 담는 새로운 클래스
[Serializable]
public class CategoryEntry
{
    public string categoryName;   // 카테고리 이름 (예: 필수 동사)
    public List<WordEntry> words; // 해당 카테고리에 속한 단어 리스트
}

[Serializable]
public class WordBankData
{
    // 기존의 wordList 대신 CategoryEntry의 리스트를 받습니다.
    public List<CategoryEntry> categories; 
}