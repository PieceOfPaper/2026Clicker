using System.Collections.Generic;
using UnityEngine;

public class CoinParticleAttractor : MonoBehaviour
{
    [SerializeField] private ParticleSystem _particleSystem;
    [SerializeField] private Sprite[] _coinSprites;
    [SerializeField, Min(1)] private int _normalCoinCount = 10;
    [SerializeField, Min(1)] private int _bossCoinCount = 25;
    [Header("Floor Bounce")]
    [SerializeField, Min(0f)] private float _dropHeight = 2.2f;
    [SerializeField, Min(0f)] private float _dropSpeed = 2.5f;
    [SerializeField, Min(0f)] private float _gravity = 18f;
    [SerializeField, Min(0f)] private float _bounceSpeed = 5.5f;
    [SerializeField, Min(0f)] private float _horizontalScatterSpeed = 4.5f;
    [SerializeField, Min(0f)] private float _scatterDuration = 0.35f;
    [SerializeField, Min(0.1f)] private float _attractionSpeed = 12f;
    [SerializeField, Min(0.1f)] private float _attractionAcceleration = 35f;
    [SerializeField, Min(0.01f)] private float _collectionDistance = 0.25f;

    private readonly Dictionary<uint, CoinReward> _rewards = new();
    private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[128];
    private uint _nextSeed = 1;

    private struct CoinReward
    {
        public BigNumber Amount;
        public Transform Target;
        public Vector3 TargetPosition;
        public float GroundY;
        public float BounceTime;
        public bool HasBounced;
    }

    private void Awake()
    {
        if (_particleSystem == null)
            _particleSystem = GetComponent<ParticleSystem>();

        ConfigureParticleSystem();
    }

    private void Update()
    {
        if (_particleSystem == null || _rewards.Count == 0)
            return;

        var particleCount = _particleSystem.GetParticles(_particles);
        var deltaTime = Time.deltaTime;

        for (var i = 0; i < particleCount; i++)
        {
            ref var particle = ref _particles[i];
            if (!_rewards.TryGetValue(particle.randomSeed, out var reward))
                continue;

            var aliveTime = particle.startLifetime - particle.remainingLifetime;

            if (!reward.HasBounced)
            {
                particle.velocity += Vector3.down * (_gravity * deltaTime);

                if (particle.position.y <= reward.GroundY && particle.velocity.y <= 0f)
                {
                    var position = particle.position;
                    position.y = reward.GroundY;
                    particle.position = position;
                    particle.velocity = new Vector3(
                        Random.Range(-_horizontalScatterSpeed, _horizontalScatterSpeed),
                        _bounceSpeed * Random.Range(0.85f, 1.15f),
                        0f);

                    reward.HasBounced = true;
                    reward.BounceTime = aliveTime;
                    _rewards[particle.randomSeed] = reward;
                }

                continue;
            }

            if (aliveTime - reward.BounceTime < _scatterDuration)
            {
                particle.velocity += Vector3.down * (_gravity * deltaTime);
                continue;
            }

            var targetPosition = reward.Target != null ? reward.Target.position : reward.TargetPosition;
            var toTarget = targetPosition - particle.position;
            if (toTarget.sqrMagnitude <= _collectionDistance * _collectionDistance)
            {
                GameMain.Instance?.AddGold(reward.Amount);
                _rewards.Remove(particle.randomSeed);
                particle.remainingLifetime = -1f;
                continue;
            }

            var desiredVelocity = toTarget.normalized * _attractionSpeed;
            particle.velocity = Vector3.MoveTowards(
                particle.velocity,
                desiredVelocity,
                _attractionAcceleration * deltaTime);
        }

        _particleSystem.SetParticles(_particles, particleCount);
    }

    public bool PlayReward(Vector3 origin, Transform target, BigNumber totalReward, bool isBoss)
    {
        if (_particleSystem == null || target == null || totalReward <= BigNumber.Zero)
            return false;

        var coinCount = isBoss ? _bossCoinCount : _normalCoinCount;
        var rewardPerCoin = totalReward / coinCount;
        var distributedReward = BigNumber.Zero;

        for (var i = 0; i < coinCount; i++)
        {
            var reward = i == coinCount - 1
                ? totalReward - distributedReward
                : rewardPerCoin;
            distributedReward += reward;

            var seed = GetNextSeed();
            var emitParams = new ParticleSystem.EmitParams
            {
                position = origin + Vector3.up * _dropHeight,
                velocity = new Vector3(
                    Random.Range(-_horizontalScatterSpeed * 0.5f, _horizontalScatterSpeed * 0.5f),
                    -_dropSpeed * Random.Range(0.85f, 1.15f),
                    0f),
                startLifetime = 3f,
                startSize = Random.Range(0.65f, 0.9f),
                randomSeed = seed,
            };

            _rewards[seed] = new CoinReward
            {
                Amount = reward,
                Target = target,
                TargetPosition = target.position,
                GroundY = origin.y,
                BounceTime = 0f,
                HasBounced = false,
            };
            _particleSystem.Emit(emitParams, 1);
        }

        return true;
    }

    private uint GetNextSeed()
    {
        do
        {
            _nextSeed++;
            if (_nextSeed == 0)
                _nextSeed = 1;
        }
        while (_rewards.ContainsKey(_nextSeed));

        return _nextSeed;
    }

    private void ConfigureParticleSystem()
    {
        if (_particleSystem == null)
            return;

        var main = _particleSystem.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(128, _bossCoinCount * 4);
        main.startLifetime = 3f;
        main.startSpeed = 0f;
        main.startSize = 0.8f;
        // Gravity is applied per reward so it can stop when attraction begins.
        main.gravityModifier = 0f;

        var emission = _particleSystem.emission;
        emission.enabled = false;

        var shape = _particleSystem.shape;
        shape.enabled = false;

        var textureSheet = _particleSystem.textureSheetAnimation;
        textureSheet.enabled = _coinSprites != null && _coinSprites.Length > 0;
        if (!textureSheet.enabled)
            return;

        textureSheet.mode = ParticleSystemAnimationMode.Sprites;
        while (textureSheet.spriteCount > 0)
            textureSheet.RemoveSprite(0);
        foreach (var sprite in _coinSprites)
        {
            if (sprite != null)
                textureSheet.AddSprite(sprite);
        }
        textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        textureSheet.cycleCount = 3;
    }
}
