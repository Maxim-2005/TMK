using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class PeacefulAnimal : MonoBehaviour
{
    [Header("Animal Settings")]
    public AnimalConfig config;
    
    [Header("Debug")]
    public NPCState currentState = NPCState.Wandering;
    public bool showGizmos = true;
    
    // Компоненты
    protected NavMeshAgent _agent;
    protected Animator _animator;
    
    // Переменные состояния
    protected Vector3 _wanderTarget;
    protected Vector3 _startPosition;
    protected int _currentPatternIndex = 0;
    
    // Ссылки для анимаций
    protected readonly int _animSpeed = Animator.StringToHash("Speed");
    protected readonly int _animIsWaiting = Animator.StringToHash("IsWaiting");
    protected readonly int _animIsSleeping = Animator.StringToHash("IsSleeping");
    
    public enum NPCState
    {
        Wandering,
        Waiting,
        Sleeping
    }

    protected virtual void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        _startPosition = transform.position;
        
        // ДОБАВЬ СМЕЩЕНИЕ ДЛЯ КОРРЕКТНОЙ ВЫСОТЫ
        if (_agent != null)
        {
            _agent.baseOffset = 0.43f; // Поднимаем агента над землей
        }
        
        SetupAgent();
        StartCoroutine(StateMachine());
    }

    protected virtual void Update()
    {
        UpdateAnimations();
    }

    private void SetupAgent()
    {
        if (_agent == null || config == null) return;
        
        _agent.speed = config.wanderSpeed;
        _agent.angularSpeed = 0f; // Отключаем встроенный поворот NavMeshAgent
        _agent.acceleration = config.acceleration;
        _agent.stoppingDistance = config.stoppingDistance;
        _agent.autoBraking = true;
    }

    protected virtual IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case NPCState.Wandering:
                    yield return StartCoroutine(WanderRoutine());
                    break;
                case NPCState.Waiting:
                    yield return StartCoroutine(WaitRoutine());
                    break;
                case NPCState.Sleeping:
                    yield return StartCoroutine(SleepRoutine());
                    break;
            }
            
            yield return StartCoroutine(ChooseNextState());
        }
    }

    private IEnumerator WanderRoutine()
    {
        if (!_agent.isOnNavMesh) yield break;
        
        if (FindRandomWanderPoint(out _wanderTarget))
        {
            _agent.isStopped = false;
            _agent.SetDestination(_wanderTarget);
            
            // ПОВОРОТ ПЕРЕД ДВИЖЕНИЕМ
            Vector3 directionToTarget = (_wanderTarget - transform.position).normalized;
            directionToTarget.y = 0;
            
            if (directionToTarget != Vector3.zero)
            {
                // КОРРЕКЦИЯ НАПРАВЛЕНИЯ - поворачиваем на 180 градусов
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget) * Quaternion.Euler(0, 180, 0);
                
                float rotationTime = 0f;
                float maxRotationTime = config.maxRotationTime;
                
                while (rotationTime < maxRotationTime && 
                    Quaternion.Angle(transform.rotation, targetRotation) > 5f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                        rotationTime / maxRotationTime);
                    rotationTime += Time.deltaTime;
                    yield return null;
                }
                transform.rotation = targetRotation;
            }
            
            // ДВИЖЕНИЕ
            float timeout = config.wanderTimeout;
            float timer = 0f;
            
            while ((_agent.pathPending || 
                _agent.remainingDistance > _agent.stoppingDistance) && 
                timer < timeout && _agent.isOnNavMesh)
            {
                timer += Time.deltaTime;
                
                // ПЛАВНЫЙ ПОВОРОТ ВО ВРЕМЯ ДВИЖЕНИЯ С КОРРЕКЦИЕЙ
                if (_agent.velocity.magnitude > 0.1f)
                {
                    Vector3 moveDirection = _agent.velocity.normalized;
                    moveDirection.y = 0;
                    if (moveDirection != Vector3.zero)
                    {
                        // КОРРЕКЦИЯ НАПРАВЛЕНИЯ ДВИЖЕНИЯ
                        Quaternion moveRotation = Quaternion.LookRotation(moveDirection) * Quaternion.Euler(0, 180, 0);
                        transform.rotation = Quaternion.Slerp(transform.rotation, moveRotation, 
                            config.rotationSpeed * Time.deltaTime);
                    }
                }
                
                yield return null;
            }
            
            yield return new WaitForSeconds(config.pauseBetweenActions);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }
    }

    protected virtual IEnumerator WaitRoutine()
    {
        _agent.isStopped = true;
        float waitTime = Random.Range(config.minWaitTime, config.maxWaitTime);
        yield return new WaitForSeconds(waitTime);
        _agent.isStopped = false;
    }

    protected virtual IEnumerator SleepRoutine()
    {
        _agent.isStopped = true;
        yield return new WaitForSeconds(config.sleepDuration);
        _agent.isStopped = false;
    }

    protected virtual IEnumerator ChooseNextState()
    {
        if (config.useRandomPattern)
        {
            float randomValue = Random.value;
            
            if (randomValue < config.sleepChance)
            {
                currentState = NPCState.Sleeping;
            }
            else if (randomValue < config.wanderChance)
            {
                currentState = NPCState.Wandering;
            }
            else
            {
                currentState = NPCState.Waiting;
            }
        }
        else
        {
            if (_currentPatternIndex >= config.behaviorPattern.Length)
                _currentPatternIndex = 0;
            
            char nextStateChar = config.behaviorPattern[_currentPatternIndex];
            _currentPatternIndex++;
            
            switch (nextStateChar)
            {
                case '1': currentState = NPCState.Wandering; break;
                case '2': currentState = NPCState.Waiting; break;
                case '3': currentState = NPCState.Sleeping; break;
                default: currentState = NPCState.Wandering; break;
            }
        }
        
        yield return null;
    }

    protected virtual bool FindRandomWanderPoint(out Vector3 result)
    {
        Vector3 center = config.useStartPosition ? _startPosition : transform.position;
        
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * config.wanderRadius;
            randomDirection.y = 0;
            Vector3 randomPoint = center + randomDirection;
            
            if (Vector3.Distance(transform.position, randomPoint) < config.minWanderDistance)
                continue;
            
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, config.wanderRadius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        
        result = transform.position;
        return false;
    }

    protected virtual void UpdateAnimations()
    {
        if (_animator == null) return;
        
        switch (currentState)
        {
            case NPCState.Wandering:
                _animator.SetFloat(_animSpeed, _agent.velocity.magnitude);
                _animator.SetBool(_animIsWaiting, false);
                _animator.SetBool(_animIsSleeping, false);
                break;
                
            case NPCState.Waiting:
                _animator.SetFloat(_animSpeed, 0f);
                _animator.SetBool(_animIsWaiting, true);
                _animator.SetBool(_animIsSleeping, false);
                break;
                
            case NPCState.Sleeping:
                _animator.SetFloat(_animSpeed, 0f);
                _animator.SetBool(_animIsWaiting, false);
                _animator.SetBool(_animIsSleeping, true);
                break;
        }
    }
    
    // Методы для внешнего управления
    public virtual void ForceState(NPCState newState)
    {
        StopAllCoroutines();
        currentState = newState;
        StartCoroutine(StateMachine());
    }
    
    public virtual void RunAwayFromPlayer()
    {
        // Будет реализовано позже
        Debug.Log($"{gameObject.name}: Убегает от игрока!");
    }
    
    // Визуализация в редакторе
    protected virtual void OnDrawGizmosSelected()
    {
        if (!showGizmos || config == null) return;
        
        Vector3 center = config.useStartPosition && Application.isPlaying ? _startPosition : transform.position;
        
        // Радиус блуждания
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(center, config.wanderRadius);
        
        // Минимальная дистанция блуждания
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, config.minWanderDistance);
        
        // Текущая цель
        if (Application.isPlaying && currentState == NPCState.Wandering)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, _wanderTarget);
            Gizmos.DrawWireSphere(_wanderTarget, 0.3f);
        }
    }
}