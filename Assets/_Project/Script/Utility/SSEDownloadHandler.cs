using UnityEngine.Networking;
using System.Text;
using System;
using UnityEngine;

public class SSEDownloadHandler : DownloadHandlerScript
{
    private Action<string> _onEventReceived;
    private StringBuilder _buffer = new StringBuilder();

    public SSEDownloadHandler(Action<string> onEventReceived) : base()
    {
        _onEventReceived = onEventReceived;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return true;

        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
        _buffer.Append(chunk);

        string currentText = _buffer.ToString();

        // ⭐️ 핵심 수정: 서버마다 다른 줄바꿈 방식(\n\n 또는 \r\n\r\n)을 모두 대응합니다.
        string[] separators = new string[] { "\r\n\r\n", "\n\n" };
        int delimiterIndex = -1;
        string foundSeparator = "";

        foreach (var sep in separators)
        {
            int idx = currentText.IndexOf(sep);
            if (idx != -1 && (delimiterIndex == -1 || idx < delimiterIndex))
            {
                delimiterIndex = idx;
                foundSeparator = sep;
            }
        }

        while (delimiterIndex >= 0)
        {
            string eventBlock = currentText.Substring(0, delimiterIndex);

            // ⭐️ 로그가 안 찍힌다면 이 아래 로그가 찍히는지 꼭 확인해야 합니다!
            // Debug.Log($"[RAW SSE DATA] {eventBlock}"); 

            _onEventReceived?.Invoke(eventBlock);

            currentText = currentText.Substring(delimiterIndex + foundSeparator.Length);
            _buffer.Clear();
            _buffer.Append(currentText);

            // 다음 이벤트가 또 있는지 확인
            delimiterIndex = -1;
            foreach (var sep in separators)
            {
                int idx = currentText.IndexOf(sep);
                if (idx != -1 && (delimiterIndex == -1 || idx < delimiterIndex))
                {
                    delimiterIndex = idx;
                    foundSeparator = sep;
                }
            }
        }

        return true;
    }
}