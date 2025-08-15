using UnityEngine;

namespace DungeonGunner.WeaponPooling
{
    // 从对象池取出的弹药：蓄力停留、按方向飞行、命中或超出射程后自己禁用回到池里。
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class Ammo : MonoBehaviour, IFireable
    {
        // 近距离判定阈值（Unity 单位）：小于这个距离时用玩家到鼠标的角度而不是武器朝向角度。
        private const float CloseRangeThreshold = 3.5f;

        [Tooltip("拖尾特效，只有配置了拖尾的弹药类型才会用到")]
        [SerializeField] private TrailRenderer _trailRenderer;

        private SpriteRenderer _spriteRenderer;
        private AmmoDetailsSO _details;
        private Vector3 _fireDirection;
        private float _speed;
        private float _remainingRange;
        private float _chargeTimer;
        private bool _hasAppliedChargedMaterial;
        private bool _hasDealtDamage;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // 蓄力期间原地停留，只播放蓄力材质，蓄力结束才真正开始移动。
            if (_chargeTimer > 0f)
            {
                _chargeTimer -= Time.deltaTime;
                return;
            }

            if (!_hasAppliedChargedMaterial)
            {
                _spriteRenderer.material = _details.Material;
                _hasAppliedChargedMaterial = true;
            }

            Vector3 step = _fireDirection * _speed * Time.deltaTime;
            transform.position += step;
            _remainingRange -= step.magnitude;

            if (_remainingRange < 0f)
            {
                Deactivate();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasDealtDamage)
            {
                return;
            }

            if (collision.TryGetComponent(out Health health))
            {
                _hasDealtDamage = true;
                health.TakeDamage(_details.Damage);
            }

            Deactivate();
        }

        // weaponAimDirection 太短（枪口几乎贴着目标）时用玩家到鼠标的角度，否则用武器朝向角度，
        // 避免近距离时因为枪口与瞄准点距离太近导致角度抖动。
        public void Initialize(AmmoDetailsSO details, float aimAngle, float weaponAimAngle, float speed, Vector3 weaponAimDirection)
        {
            _details = details != null ? details : throw new System.ArgumentNullException(nameof(details));
            _speed = speed;
            _remainingRange = details.Range;
            _hasDealtDamage = false;

            float spreadMagnitude = Random.Range(details.SpreadMin, details.SpreadMax);
            float spreadSign = Random.value < 0.5f ? -1f : 1f;
            float directionAngle = (weaponAimDirection.magnitude < CloseRangeThreshold ? aimAngle : weaponAimAngle) + spreadMagnitude * spreadSign;

            transform.eulerAngles = new Vector3(0f, 0f, directionAngle);
            _fireDirection = DirectionFromAngle(directionAngle);
            _spriteRenderer.sprite = details.AmmoSprite;

            if (details.ChargeTime > 0f)
            {
                _chargeTimer = details.ChargeTime;
                _spriteRenderer.material = details.ChargeMaterial;
                _hasAppliedChargedMaterial = false;
            }
            else
            {
                _chargeTimer = 0f;
                _spriteRenderer.material = details.Material;
                _hasAppliedChargedMaterial = true;
            }

            ApplyTrailSettings(details);
            gameObject.SetActive(true);
        }

        public GameObject GetGameObject() => gameObject;

        private void ApplyTrailSettings(AmmoDetailsSO details)
        {
            if (_trailRenderer == null)
            {
                return;
            }

            _trailRenderer.emitting = details.HasTrail;
            _trailRenderer.gameObject.SetActive(details.HasTrail);

            if (!details.HasTrail)
            {
                return;
            }

            _trailRenderer.material = details.TrailMaterial;
            _trailRenderer.time = details.TrailTime;
            _trailRenderer.startWidth = details.TrailStartWidth;
            _trailRenderer.endWidth = details.TrailEndWidth;
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private static Vector3 DirectionFromAngle(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        }
    }
}
