using System;
using System.Collections.Generic;

[Serializable]
public class QuizCacheEntry
{
    public string Topic;
    public List<QuizQuestion> Questions;
}

[Serializable]
public class QuizCacheData
{
    public List<QuizCacheEntry> Entries = new List<QuizCacheEntry>();
}

