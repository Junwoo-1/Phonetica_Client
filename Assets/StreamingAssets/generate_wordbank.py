import json
import os
from g2pkk import G2p # 팀원 서버에서 사용하는 라이브러리

def generate_word_bank(input_words, output_path):
    g2p = G2p()
    word_list = []

    for word in input_words:
        # 1. 단어에 G2P 규칙을 적용해 표준 발음을 추출합니다.
        # 예: '닭볶이' -> '닥뽀끼'
        pronunciation = g2p(word)
        
        # 2. JSON 구조에 맞게 딕셔너리를 생성합니다.
        word_list.append({
            "word": word,
            "pronunciation": pronunciation
        })
        print(f"✅ 처리 완료: {word} -> {pronunciation}")

    # 3. 유니티가 읽을 수 있는 JSON 파일로 저장합니다.
    output_data = {"wordList": word_list}
    
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, ensure_ascii=False, indent=2)

    print(f"\n🚀 총 {len(word_list)}개의 단어가 {output_path}에 저장되었습니다.")

# --- 사용 예시 ---
if __name__ == "__main__":
    # 추가하고 싶은 단어들만 쭉 적으면 됩니다.
    my_new_words = ["사과", "바나나", "닭볶이", "먹었어", "국물", "같이"]
    
    # 유니티 프로젝트의 StreamingAssets 경로로 지정하세요.
    target_path = "./WordBank.json" 
    
    generate_word_bank(my_new_words, target_path)