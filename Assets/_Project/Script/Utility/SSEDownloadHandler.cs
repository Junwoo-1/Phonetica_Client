using UnityEngine.Networking;
using System.Text;
using System;
using UnityEngine;

// UnityWebRequest의 다운로드 방식을 내 입맛대로 개조하는 클래스
public class SSEDownloadHandler : DownloadHandlerScript
{
    private Action<string> _onEventReceived;
    private StringBuilder _buffer = new StringBuilder();

    // 생성자: 이벤트가 완성될 때마다 호출할 콜백 함수를 받습니다.
    public SSEDownloadHandler(Action<string> onEventReceived) : base()
    {
        _onEventReceived = onEventReceived;
    }

    // 핵심: 서버에서 데이터 조각(Chunk)이 네트워크를 타고 도착할 때마다 즉시!! 호출됩니다.
    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0) return true;

        // 들어온 바이트 배열을 즉시 텍스트로 변환하여 버퍼에 넣습니다.
        string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
        _buffer.Append(chunk);

        // "\n\n" (SSE 이벤트의 끝)을 찾아냅니다.
        string currentText = _buffer.ToString();
        int delimiterIndex;
        
        while ((delimiterIndex = currentText.IndexOf("\n\n")) >= 0)
        {
            // 완벽한 이벤트 블록 1개를 뽑아냅니다.
            string eventBlock = currentText.Substring(0, delimiterIndex);
            
            // 완성된 이벤트를 밖으로 던져줍니다. (PronunciationClient가 받음)
            _onEventReceived?.Invoke(eventBlock);

            // 처리한 부분은 버퍼에서 날려버립니다. (메모리 최적화)
            currentText = currentText.Substring(delimiterIndex + 2);
            _buffer.Clear();
            _buffer.Append(currentText);
        }

        return true; // 계속 다운로드 하겠다는 뜻
    }
}