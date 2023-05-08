
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : MonoBehaviour
{
    public NavMeshAgent agent;
    private bool enemyDead;

    public Transform player;

    public LayerMask whatIsGround, whatIsPlayer;

    public float health;

    private BulletScript bullet;

    //Patroling
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    //Animation
    public float movementThreshold;
    public Animator animator;
    bool isWalking;
    private Vector3 previousPosition;

    //Attacking
    public float timeBetweenAttacks;
    bool alreadyAttacked;
    public GameObject projectile;
    public Transform arrowSpawn;

    //States
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        previousPosition = transform.position;
    }

    private void Update()
    {
        if (!enemyDead)
        {
            //Check for sight and attack range
            playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
            playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

            if (!playerInSightRange && !playerInAttackRange) Patroling();
            if (playerInSightRange && !playerInAttackRange) ChasePlayer();
            if (playerInAttackRange && playerInSightRange) AttackPlayer();

            if (Vector3.Distance(transform.position, previousPosition) > movementThreshold)
            {
                isWalking = true;
            }
            else
            {
                isWalking = false;
            }

            // Store the current position for the next frame
            previousPosition = transform.position;

            // Trigger the animation if the Rigidbody is moving
            animator.SetBool("isWalking", isWalking);
        }
        
    }

    private void Patroling()
    {
        if (!walkPointSet) SearchWalkPoint();

        if (walkPointSet)
            agent.SetDestination(walkPoint);

        Vector3 distanceToWalkPoint = transform.position - walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
            walkPointSet = false;
    }
    private void SearchWalkPoint()
    {
        //Calculate random point in range
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
            walkPointSet = true;
    }

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        //Make sure enemy doesn't move
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            ///Attack code here
            Rigidbody rb = Instantiate(projectile, arrowSpawn.transform.position, Quaternion.identity).GetComponent<Rigidbody>();
            animator.SetBool("isAttacking", true);
            
            rb.transform.LookAt(player);
            rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
            ///End of attack code

            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
        else
        {
            animator.SetBool("isAttacking", false);
        }
    }

    IEnumerator Example()
    {
        Debug.Log("Hello");
        //wait 3 seconds
        yield return new WaitForSeconds(15);
        Debug.Log("Goodbye");
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    public void TakeDamage(int damage)
    {
        //if (health <= 0) Invoke(nameof(), 0.5f);
        if (health > 0)
        {
            //animator.SetBool("isHit", false);
            health -= damage;
        }
        else
        {
            DestroyEnemy();
        }
        
        
    }
    private void DestroyEnemy()
    {

        animator.SetBool("isDying", true);
        animator.SetBool("isHit", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isAttacking", false);
        enemyDead = true;

        //Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Bullet") // Check if the hit object is an enemy
        {
            bullet = other.gameObject.GetComponent<BulletScript>();
            //animator.SetBool("isHit", true);
            /*
            if (enemyHP.getSetEnenmyHitPoints > 0)
            {
                enemyHP.getSetEnenmyHitPoints -= bullet.DMG;
            }
            else
            {
                Destroy(gameObject); // Destroy the bullet object
            }
            */
            Debug.Log(health);
            TakeDamage(bullet.DMG);

            Destroy(other.gameObject); // Destroy the enemy object
        }
    }
}
