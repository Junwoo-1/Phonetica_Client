using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("이동 설정")]
    public float speed = 15f;
    public GameObject hitEffectPrefab;

    private Enemy _target;
    private float _damage;

    // 투사체 발사 시 플레이어가 정보를 넘겨줍니다.
    public void Initialize(Enemy target, float damage)
    {
        _target = target;
        _damage = damage;
    }

    void Update()
    {
        // 날아가는 도중 다른 공격에 의해 적이 이미 죽어 없어졌다면 투사체도 소멸
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // 타겟을 향해 이동
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // 적과의 거리가 충분히 가까워지면(충돌 판정) 타격 처리
        if (Vector3.Distance(transform.position, _target.transform.position) < 0.5f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        // 1. 적에게 데미지를 입히고 생존 여부 확인
        bool isAlive = _target.TakeDamage(_damage);

        // 2. 적이 살았을 때만 단어 갱신 (Player.cs에 있던 로직을 이쪽으로 이동!)
        if (isAlive)
        {
            EnemySpawner.Instance.ChangeEnemyWord(_target, _target.displayText);
        }

        // 3. 타격 이펙트 생성 후 투사체 파괴
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}