using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private Ray m_Ray;

    private bool m_IsMoving;
    private bool m_AirAttack;

    private int m_attackSpeedStacks = 0;
    private float m_lastAttackTime;
    private float m_lastStackTime;

    private const float k_StackBonus = 0.05f;
    private const int k_MaxStacks = 5;

    private Animator m_Animator;
    private NavMeshAgent m_NavMeshAgent;
    private NavMeshPath m_NavMeshPath;
    private Camera m_Camera;

    private GameObject m_targetEnemy;

    [Header("VFX")]
    [SerializeField] private ParticleSystem m_airAttackEffect;
    [SerializeField] private ParticleSystem m_ClickEffect;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text airAttackText;
    [SerializeField] private TMP_Text cooldownText;

    [Header("Movement Speed")]
    [SerializeField] private float m_speed = 3.5f;

    [Header("Attack")]
    [SerializeField] private float m_attackRange = 2f;
    [SerializeField] private float m_attackCooldown = 1f;

    [Header("Air Attack")]
    [SerializeField] private float m_AirAttackRadius = 3f;
    [SerializeField] private int m_AirAttackDamage = 20;

    [Header("Stack Reset")]
    [SerializeField] private float m_stackResetTime = 5f;

    // Attack speed stack sistemi

    void Start()
    {
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
        m_Animator = GetComponent<Animator>();
        m_NavMeshPath = new NavMeshPath();
        m_Camera = Camera.main;
    }

    void Update()
    {
        m_NavMeshAgent.speed = m_speed;
        #region UI
        if (airAttackText != null)
        {
            airAttackText.text = $"Air Attack: {(m_AirAttack ? "True" : "False")}";
        }

        if (cooldownText != null)
        {
            float timeSinceLastAttack = Time.time - m_lastAttackTime;
           
            cooldownText.text = $"Cooldown: {m_attackCooldown:F2}s";
        }
        #endregion

        #region Movement & location
        if (Input.GetMouseButton(0))
        {

            Vector3 clickPosition = GetWorldPosition();
            if (clickPosition != Vector3.zero)
            {
                Instantiate(m_ClickEffect, clickPosition + Vector3.up * 0.1f, Quaternion.identity).Play();
            }

            if (CanReach())
            {
                m_NavMeshAgent.SetPath(m_NavMeshPath);
                m_Animator.SetBool("Walk",true);
                m_IsMoving = true;
            }
        }

        if (m_IsMoving && m_NavMeshAgent.remainingDistance == 0)
        {
            m_Animator.SetBool("Walk", false);
            m_IsMoving = false;

        }
        #endregion

        #region Find & Attack Enemy

        if (Input.GetMouseButton(0))
        {
            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Enemy"))
            {
                m_Animator.SetBool("Walk", true);
                m_targetEnemy = hit.collider.gameObject;
                m_NavMeshAgent.SetDestination(m_targetEnemy.transform.position);
            }
        }
        if (m_targetEnemy != null)
        {
            float distance = Vector3.Distance(transform.position, m_targetEnemy.transform.position);
            if (distance <= m_attackRange && Time.time - m_lastAttackTime >= m_attackCooldown)
            {
                Attack();
            }
        }
        // Air Attack
        if (Input.GetKeyDown(KeyCode.Q))
        {
            m_AirAttack = true;
            Debug.Log("hedefleme modu aktif.");
        }
        if (m_AirAttack && Input.GetMouseButtonDown(0))
        {
            Vector3 targetPoint = GetWorldPosition();
            DoAirAttack(targetPoint);
            m_AirAttack = false;
        }
        #endregion

        #region Attack Speed
        if (Time.time - m_lastAttackTime > m_stackResetTime && m_attackSpeedStacks > 0)
        {
            m_attackSpeedStacks = 0;
            m_attackCooldown = 1f;
        }
        #endregion
    }
    private Vector3 GetWorldPosition()
    {
        m_Ray = m_Camera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(m_Ray, out RaycastHit hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    private bool CanReach()
    {
        return m_NavMeshAgent.CalculatePath(GetWorldPosition(), m_NavMeshPath) && m_NavMeshPath.status == NavMeshPathStatus.PathComplete;
    }

    private void Attack()
    {
        m_lastAttackTime = Time.time;
        m_lastStackTime = Time.time;

        transform.LookAt(m_targetEnemy.transform);

        var health = m_targetEnemy.GetComponent<EnemyHealth>();
        if (health != null)
        {
            m_Animator.SetTrigger("Attack");
            m_Animator.SetBool("Walk", false);


            health.TakeDamage(10);
        }

        if (m_attackSpeedStacks < k_MaxStacks)
        {
            m_attackSpeedStacks++;
        }

        
        m_attackCooldown = 1f * (1f - m_attackSpeedStacks * k_StackBonus);

    }

    private void DoAirAttack(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, m_AirAttackRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {

                m_airAttackEffect.Play();
                Instantiate(m_airAttackEffect, center, Quaternion.identity).Play();


                var health = hit.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(m_AirAttackDamage);
                    Debug.Log("Havadan hasar verildi: " + hit.name);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (m_AirAttack)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetWorldPosition(), m_AirAttackRadius);
        }
    }
}
