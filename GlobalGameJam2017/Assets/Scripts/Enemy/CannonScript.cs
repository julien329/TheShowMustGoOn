﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class CannonScript : IEnemy {

    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// VARIABLES
    ////////////////////////////////////////////////////////////////////////////////////////////////

    private Animator anim;
    [SerializeField]
    private GameObject CannonBall;
    [SerializeField]
    private GameObject CannonPosition;
    [SerializeField]
    private GameObject DeathExplosion;
    private Quaternion targetRotation;
    private bool isReadyToShoot;

    public float rotationSpeed = 4.0f;
    public float shootDelay = 2.0f;


    ////////////////////////////////////////////////////////////////////////////////////////////////
    /// UNITY
    ////////////////////////////////////////////////////////////////////////////////////////////////

    void Start() {
        base.Init();
        anim = GetComponent<Animator>();
        isCooldown = false;
        isReadyToShoot = false;
    }


    void Update() {
        if (humanPlayer) {
            EvaluateState();
            action();
        }
    }


    /////////////////////////////////////////////////////////////////////////////////////////////////
    /// METHODS
    ////////////////////////////////////////////////////////////////////////////////////////////////

    protected override void EvaluateState() {
        switch (state) {
            case State.IDLE:
                if (Vector3.Distance(humanPlayer.position, transform.position) < attackRange) {
                    navMeshAgent.ResetPath();
                    state = State.ATTACKING;
                    action = AggressiveAction;
                    //anim.SetBool("isRun", false);
                    //anim.SetBool("LowKick", true);
                }
                else if (Vector3.Distance(humanPlayer.position, transform.position) < chaseRange) {
                    state = State.FOLLOWING;
                    action = FollowingAction;
                    //anim.SetBool("isRun", true);
                    navMeshAgent.speed = chaseSpeed;
                }
                break;
            case State.FOLLOWING:
                if (Vector3.Distance(humanPlayer.position, transform.position) < attackRange) {
                    navMeshAgent.ResetPath();
                    state = State.ATTACKING;
                    action = AggressiveAction;
                    //LaunchAttack();
                }
                break;
            case State.ROAMING:
                if (Vector3.Distance(humanPlayer.position, transform.position) < attackRange) {
                    navMeshAgent.ResetPath();
                    state = State.ATTACKING;
                    action = AggressiveAction;
                    //anim.SetBool("isRun", false);
                    //anim.SetBool("LowKick", true);
                    isCooldown = false;
                }
                else if (Vector3.Distance(humanPlayer.position, transform.position) < chaseRange) {
                    state = State.FOLLOWING;
                    action = FollowingAction;
                    //anim.SetBool("isRun", true);
                    navMeshAgent.speed = chaseSpeed;
                    isCooldown = false;
                }
                break;
            case State.ATTACKING:
                break;
            case State.COOLDOWN:
                break;
            case State.DYING:
                break;
            default:
                break;
        }
    }


    public override void TakeDamage(int damage) {
        anim.SetBool("isDamage", true);
        HP -= damage;
        gameObject.GetComponentInChildren<HealthBar>().UpdateBar(maxHP, HP);

        //Floating text
        hitController.createHitText(damage, transform);

        if (HP < 1 && state != State.DYING) {
            EnemyDie();
        }
    }

    protected override void AggressiveAction() {
        targetRotation.SetLookRotation(Vector3.Normalize(humanPlayer.transform.position - transform.position));

        float angleToTurn = Quaternion.Angle(transform.rotation, targetRotation);
        if (!isReadyToShoot && angleToTurn < 10.0f) {
            Debug.Log("Locked on");
            isReadyToShoot = true;
            StartCoroutine("ShootDelay", shootDelay);
        }

        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }


    public override void EnemyDie() {
        if (state != State.DYING) {
            spawnManager.EnnemyDied();
            state = State.DYING;
            action = DyingAction;
            DropItems();
            ScoreHandler.totalScore += pointWorth;
            ScoreHandler.UpdateScore();
            clearAnimParameters();
            anim.SetBool("isDeath", true);
            Destroy(gameObject, 0.5f);
            Destroy(Instantiate(DeathExplosion, transform.position, Quaternion.identity), 5.0f);

        }
    }


    protected override void CooldownAction() {
        if (!isCooldown) {
            StartCoroutine("Cooldown", attackCooldown);
        }
    }


    protected override void DyingAction() {

    }


    protected override void RoamingAction() {
        if (!navMeshAgent.hasPath && !isCooldown) {
            StartCoroutine("Cooldown", idleWait);
        }
    }


    protected override void FollowingAction() {
        navMeshAgent.SetDestination(humanPlayer.position);
    }


    protected override void IdleAction() {
        //Find a random place to roam to
        if (!navMeshAgent.hasPath && !isCooldown) {
            Vector3 randomDirection = UnityEngine.Random.insideUnitCircle * idleRoamRange;
            randomDirection += transform.position;
            NavMeshHit hit;
            NavMesh.SamplePosition(randomDirection, out hit, idleRoamRange, 1);
            Vector3 targetPosition = hit.position;

            navMeshAgent.SetDestination(targetPosition);

            //anim.SetBool("isWalk", true);
            state = State.ROAMING;
            action = RoamingAction;
        }
    }


    protected override void LaunchAttack() {
        audioSource.clip = attackClips[Random.Range(0, attackClips.Length)];
        audioSource.Play();

        var news = Instantiate(CannonBall, CannonPosition.transform.position, targetRotation);
        AttackIsOver();
    }


    //Animation for attack is over
    protected void AttackIsOver() {
        state = State.COOLDOWN;
        action = CooldownAction;
        navMeshAgent.speed = idleSpeed;
        isCooldown = false;
        isReadyToShoot = false;
    }


    //Animation for death is over
    protected void DeathIsOver() {
        StartCoroutine("DeathDelay");
    }


    //Delay until cannon shoots
    IEnumerator ShootDelay(float time) {
        yield return new WaitForSeconds(time);
        if (gameObject != null && state != State.DYING && !isCooldown) {
            LaunchAttack();
        }
    }


    //Cooldown between attacks
    IEnumerator Cooldown(float time) {
        isCooldown = true;
        //anim.SetBool("isRun", false);
        //anim.SetBool("isWalk", false);
        navMeshAgent.ResetPath();
        yield return new WaitForSeconds(time);
        //Cooldown over
        if (gameObject != null && state != State.DYING && isCooldown) {
            state = State.IDLE;
            action = IdleAction;
            isCooldown = false;
        }
    }


    //Delay where the corpse is on the ground
    IEnumerator DeathDelay() {
        Collider collider = GetComponent<Collider>();
        NavMeshAgent nv = GetComponent<NavMeshAgent>();
        nv.enabled = false;
        collider.enabled = false;

        float waitTime = 3f;
        int increment = 10;

        for (int i = 0; i < increment; i++) {
            transform.Translate(0.1f * Vector3.down, Space.World);
            yield return new WaitForSeconds(waitTime / increment);
        }
        Destroy(gameObject);
    }

   
    void clearAnimParameters() {
        anim.SetBool("isWalk", false);
        anim.SetBool("isRun", false);
        anim.SetBool("isAnother", false);
        anim.SetBool("Attack", false);
        anim.SetBool("LowKick", false);
        anim.SetBool("isDeath", false);
        anim.SetBool("isDeath2", false);
        anim.SetBool("HitStrike", false);
        anim.SetBool("isDamage", false);
    }


    public override void ShockwaveHit(float distance) {
        AttackIsOver();
        StartCoroutine("Cooldown", (2 / distance));
    }
}
