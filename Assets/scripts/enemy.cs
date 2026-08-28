using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 15f;
    public float attackRange = 1.8f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Attack")]
    public int damage = 10;
    public float attackCooldown = 1.5f;

    private Transform player;
    private NavMeshAgent agent;
    private float attackTimer;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError("EnemyAI: No object with the Player tag was found!");
        }

        agent.speed = moveSpeed;
        attackTimer = 0f;
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        // Player is close enough to chase
        if (distance <= detectionRange && distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        // Player is close enough to attack
        else if (distance <= attackRange)
        {
            agent.isStopped = true;

            // Face the player
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
        // Player is too far away
        else
        {
            agent.isStopped = true;
        }
    }

    private void Attack()
    {
        Debug.Log("MONSTER ATTACKED! Damage: " + damage);

        // Your teammate can connect their health system here.
        // Example:
        // player.GetComponent<PlayerHealth>().TakeDamage(damage);
    }
}



