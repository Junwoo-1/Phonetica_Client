import asyncio
import json
from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse
import uvicorn
import random

app = FastAPI()

# ⭐️ 진짜 서버처럼 순차적으로 이벤트를 뱉어내는 제너레이터 함수
async def fake_sse_generator():
    # 핵심 데이터: 유니티가 파싱할 JSON 스코어
    random_word = random.choice(["사과", "바나나", "포도", "오렌지"])
    # 기획서(README.md)에 명시된 SSE 이벤트 순서
    events = [
        ("accepted", "요청 수신됨"),
        ("audio_loaded", "오디오 16kHz 변환 완료"),
        ("asr_progress", "텍스트 변환 중 (30%)"),
        ("asr_progress", "텍스트 변환 중 (80%)"),
        ("asr_completed", "텍스트 변환 완료"),
        ("g2p_completed", "표준 발음 변환 완료"),
        ("phoneme_completed", "실제 발음 추출 완료"),
        ("alignment_completed", "발음 비교 정렬 완료"),
        ("score", json.dumps({
            "overall_score": 88.5, 
            "per": 0.16, 
            "weighted_per": 0.118, 
            "low_confidence": False,
            "recognized_word": random_word # 이제 쏠 때마다 무작위 적이 맞습니다!
        })),
        ("done", "스트림 종료")
    ]

    for event_name, event_data in events:
        # SSE 국제 표준 규격에 맞게 문자열 조합 (event: ~ \n data: ~ \n\n)
        yield f"event: {event_name}\ndata: {event_data}\n\n"
        
        # 진짜 분석하는 것처럼 보이도록 0.3초씩 딜레이를 줍니다.
        await asyncio.sleep(0.3)

# ⭐️ 유니티가 파일을 POST 방식으로 쏘게 될 엔드포인트
@app.post("/pronounce")
async def pronounce(file: UploadFile = File(...)):
    print(f"📦 [서버] 유니티로부터 파일 수신 완료: {file.filename} (타입: {file.content_type})")
    
    # 파일을 받은 즉시 SSE 스트림 응답을 시작합니다.
    return StreamingResponse(fake_sse_generator(), media_type="text/event-stream")

# 서버 실행 (포트 8000)
if __name__ == "__main__":
    print("🚀 Phonetica Mock 서버가 8000 포트에서 실행 중입니다...")
    uvicorn.run(app, host="0.0.0.0", port=8000)