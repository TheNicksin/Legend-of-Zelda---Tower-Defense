using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public enum TargettingMode
{
    First,
    Last,
    Stronger,
    Weaker
}
public class Unit : NetworkBehaviour
{
    [SerializeField] protected  UnitSO unitSO;

    [Space]

    protected bool isAttacking = true;

    [Space]

    [SyncVar]
    [SerializeField] TargettingMode targetMode;
    [SerializeField] GameObject projectile;
    [SerializeField] Transform projectileOutput;

    [Tooltip("Value ^ 2")]
    public int unitGridSize;

    [Space]

    public List<EnemyUnit> enemiesInRange = new List<EnemyUnit>();
    PlayerUnitManager attachedPlayer;
    [SerializeField] protected SphereCollider rangeCollider;
    [SerializeField] protected Transform rangeVisualSprite;

    [Header("WorldSpace UI")]
    [SerializeField] UIUnitHoverStats hoverStats;
    [SerializeField] GameObject hoverStatsObject;
    [SerializeField] MissedAttackUI missedAttackUI;
    [SerializeField] GameObject missedAttackUIObject;

    [Space]
    [SerializeField] UnitAnimationManager animations;
    [SerializeField] GridDisplayFade gridDisplayFade;

    [Space]

    [SyncVar]
    string unitName;
    [SyncVar]
    string ownedPlayerName;

    [Header("Runtime Stats")]

    [SyncVar]
    [SerializeField] int cost;

    [SyncVar]
    [SerializeField] int sellCost;

    [SyncVar]
    [SerializeField] protected float cooldown;

    [SyncVar]
    [SerializeField] protected float attack;

    [SyncVar]
    [SerializeField] protected float range;

    [SyncVar]
    [SerializeField] int level = 1;

    [Space]
    public List<int> gridCells = new List<int>();

    #region Get Unit Stats methods

    public string GetUnitName() { return unitName; }
    public string GetOwnedPlayerName() { return ownedPlayerName; }
    public int GetLevel() { return level; }
    public float GetCooldown() { return cooldown; }
    public float GetAttack() { return attack; }
    public float GetRange() { return range; }
    public int GetCost() { return cost; }
    public int GetSellCost() { return sellCost; }
    public UnitSO GetUnitSO() { return unitSO; }
    public TargettingMode GetTargetMode() { return targetMode; }

    bool attacking = false;

    #endregion


    public bool isPlaced = false;
    protected bool isSelected = false;
    int loadoutIndex;

    

    #region Sound Effects

    
    AudioSource audioSource;
    [Space]
    [SerializeField] AudioClip attackSound;

    #endregion

    protected virtual void Start()
    {
        audioSource = GetComponent<AudioSource>();
        missedAttackUIObject.SetActive(false);
        hoverStatsObject.SetActive(false);

        if (!isClient)
        {
            range = unitSO.CurrentRange(level);
            rangeVisualSprite.gameObject.SetActive(true);
            rangeCollider.radius = range / 5;
            rangeVisualSprite.localScale = new Vector3(1,1) * (rangeCollider.radius * 0.2f);
            return;
        }

        if (!isOwned)
        {
            rangeVisualSprite.gameObject.SetActive(false);
        }

        if (!isServer)
        {
            return;
        }
        

        currentCooldown = Time.time + cooldown;
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();

        
        range = unitSO.CurrentRange(level);

        rangeCollider.radius = range / 5;
        rangeVisualSprite.localScale = new Vector3(1,1) * (rangeCollider.radius * 0.2f);
    }

    public override void OnStopServer()
    {
        GameManager.instance.SyncGridCellOccupence(false, gridCells);
    }

    protected virtual void Update()
    {
        if (!isPlaced) return;

        if (!isServer) return; 
        
            
        if (attacking)
            animations.AttackingAnim(0.2f);
    
        else
            animations.IdleAnim(0.3f);
            
        

        if (enemiesInRange.Count == 0) return;
        if (enemiesInRange[0] == null) 
        {
            enemiesInRange.RemoveAt(0);
            return;
        }
        AttackEnemy();
    }

    private void FixedUpdate()
    {
        Physics.IgnoreLayerCollision(6, 9);
    }

    [Server]
    public void PlacedUnit(List<int> cellIndexes, int loadoutIndex)
    {
        isPlaced = true;
        attachedPlayer = netIdentity.connectionToClient.identity.GetComponent<PlayerUnitManager>();

        gridCells = cellIndexes;
        this.loadoutIndex = loadoutIndex;

        // This sets the grid cells states for EVERYONE \\
        foreach (int cellIndex in cellIndexes)
        {
            GameManager.instance.GetGridCell(cellIndex).SetOccupence(true);
        }       
        GameManager.instance.SyncGridCellOccupence(true, cellIndexes);
        // -/////////////////////\\\\\\\\\\\\\\\\\\\\\- \\

        level = 1;
        sellCost += unitSO.NextCost(level) / 4;
        cost = unitSO.NextCost(level + 1);

        unitName = unitSO.name;
        ownedPlayerName = attachedPlayer.GetComponent<PlayerNetworkInfo>().name;
        attack = unitSO.CurrentAttack(level);
        cooldown = unitSO.CurrentCooldown(level);
        range = unitSO.CurrentRange(level);

        targetMode = TargettingMode.First;

        rangeCollider.radius = range / 5;
        rangeVisualSprite.gameObject.SetActive(false);

        ClientPlacedUnit();
    }

    [TargetRpc]
    void ClientPlacedUnit()
    {
        isPlaced = true;
    }

    public int CostToUpgrade(int level)
    {
        return unitSO.NextCost(level);
    }

    public int SellingCost(int level)
    {
        int costHolder = 0;

        for (int i = 1; i < level + 1; i++)
        {
            costHolder += unitSO.NextCost(i) / 4;
        }

        return costHolder;
    }

    #region Targetting Modes

    protected void CurrentTargettingMode()
    {
        if (enemiesInRange.Count <= 1) return;

        switch(targetMode)
        {
            case TargettingMode.First:
                ListSortByMostDistance();
                break;
            
            case TargettingMode.Last:
                ListSortByLeastDistance();
                break;

            case TargettingMode.Stronger:
                ListSortByMostHealth();
                break;

            case TargettingMode.Weaker:
                ListSortByLeastHealth();
                break;
        }
    }

    void ListSortByMostDistance()
    {
        enemiesInRange.Sort(delegate(EnemyUnit x, EnemyUnit y) 
        {
            if (x.distanceCovered == y.distanceCovered) { return 0; }
            if (x.distanceCovered > y.distanceCovered) { return -1; }
            return 1;
        });
    }

    void ListSortByLeastDistance()
    {
        enemiesInRange.Sort(delegate(EnemyUnit x, EnemyUnit y) 
        {
            if (x.distanceCovered == y.distanceCovered) { return 0; }
            if (x.distanceCovered < y.distanceCovered) { return -1; }
            return 1;
        });
    }

    void ListSortByMostHealth()
    {
        enemiesInRange.Sort(delegate(EnemyUnit x, EnemyUnit y) 
        {
            if (x.GetHealthPoints() == y.GetHealthPoints()) { return 0; }
            if (x.GetHealthPoints() > y.GetHealthPoints()) { return -1; }
            return 1;
        });
    }

    void ListSortByLeastHealth()
    {
        enemiesInRange.Sort(delegate(EnemyUnit x, EnemyUnit y) 
        {
            if (x.GetHealthPoints() == y.GetHealthPoints()) { return 0; }
            if (x.GetHealthPoints() < y.GetHealthPoints()) { return -1; }
            return 1;
        });
    }

    [Command]
    public void ChangeTargetMode(int newTargetMode)
    {
        targetMode = (TargettingMode)newTargetMode;
    } 

    #endregion

    [Command]
    public void UpgradeUnit(NetworkIdentity conn)
    {
        if (conn.netId != attachedPlayer.netId) return;

        if (level >= 5) return;

        if (attachedPlayer.money < unitSO.NextCost(GetLevel() + 1)) return;

        level++;

        attachedPlayer.SetMoney(-unitSO.NextCost(GetLevel()));

        sellCost += unitSO.NextCost(level) / 4;
        cost = unitSO.NextCost(level + 1);

        attack = unitSO.CurrentAttack(level);
        cooldown = unitSO.CurrentCooldown(level);
        range = unitSO.CurrentRange(level);

        rangeCollider.radius = range / 5;
        rangeVisualSprite.localScale = new Vector3(1,1) * (rangeCollider.radius * 0.2f);
        
        UpdateLocalClient(rangeVisualSprite.localScale, true);
    }

    [Command]
    public void SellUnit(NetworkIdentity conn)
    {
        if (conn.netId != attachedPlayer.netId) return;

        GameManager.instance.SyncGridCellOccupence(false, gridCells);
        attachedPlayer.SetMoney(sellCost);
        attachedPlayer.ChangeLoadoutCount(loadoutIndex, -1);
        NetworkServer.Destroy(gameObject);
    }

    [TargetRpc]
    protected virtual void UpdateLocalClient(Vector3 newScale, bool activeUI)
    {
        rangeVisualSprite.localScale = newScale;
        UIUnitStats.instance.UpdateUnitStats(this, activeUI);
    }

    protected float currentCooldown;

    [Server]
    void AttackEnemy()
    {
        if (isAttacking == false) return;

        if (enemiesInRange[0].isDead)
        {
            enemiesInRange.RemoveAt(0);
            return;   
        }

        if (Time.time < currentCooldown)
        {
            return;
        }

        CurrentTargettingMode();

        if (enemiesInRange[0] == null)
        {
            enemiesInRange.RemoveAt(0);
            return;
        }
        else
        {
            StartCoroutine(AttackingAnimationLength());
            RpcClientUnitActions(enemiesInRange[0].transform.position);

            int rand = Random.Range(0, 100);
            if (rand < unitSO.chanceToMiss)
            {
                MissedAttack();
            }
            else DealDamageToEnemy();
        }

        currentCooldown = Time.time + cooldown;
    }

    [Server]
    protected virtual void DealDamageToEnemy()
    {
        enemiesInRange[0].DealDamage(attack);
    }

    [ClientRpc]
    void MissedAttack()
    {
        Debug.Log($"missed attack");
        missedAttackUIObject.SetActive(true);
        missedAttackUI.StartAnimation();
    }


    protected IEnumerator AttackingAnimationLength()
    {
        attacking = true;

        yield return new WaitForSeconds(animations.GetAttackAnimLength());

        attacking = false;
    }

    [ClientRpc]
    protected virtual void RpcClientUnitActions(Vector3 firstEnemyPos)
    {
        // CODE IS BROKEN | TEMPORARILY DISABLED
        //StartCoroutine(UnitLookAtEnemyAnimation(firstEnemyPos));

        Vector3 lookEnemyRot = new Vector3(firstEnemyPos.x, 0, firstEnemyPos.z);
        Vector3 modelUnitRot = new Vector3(transform.GetChild(0).position.x, 0, transform.GetChild(0).position.z);
        Quaternion lookOnLook = Quaternion.LookRotation(lookEnemyRot - modelUnitRot);
        transform.GetChild(0).rotation = lookOnLook;

        StartCoroutine(UnitShootProjectile(firstEnemyPos));
    }

    IEnumerator UnitLookAtEnemyAnimation(Vector3 firstEnemyPos)
    {
        Vector3 lookEnemyRot = new Vector3(firstEnemyPos.x, 0, firstEnemyPos.z);
        Vector3 modelUnitRot = new Vector3(transform.GetChild(0).position.x, 0, transform.GetChild(0).position.z);

        Quaternion lookOnLook = Quaternion.LookRotation(lookEnemyRot - modelUnitRot);

        float timer = 0;
        while (timer < 1)
        {
            timer += Time.deltaTime;
            transform.GetChild(0).rotation = Quaternion.Slerp(transform.GetChild(0).rotation, lookOnLook, 4);
            yield return null;
        }

    }

    IEnumerator UnitShootProjectile(Vector3 firstEnemyPos)
    {
        if (projectile == null) yield break;

        Transform newProjectile = Instantiate(projectile, projectileOutput.position, Quaternion.identity).transform;
        Vector3 newEnemyPos = new Vector3(firstEnemyPos.x, firstEnemyPos.y + 0.5f, firstEnemyPos.z);
        //                  destination  -  origin
        Vector3 direction = newEnemyPos - newProjectile.position;

        while (Vector3.Distance(newEnemyPos, newProjectile.position) > 0.2f)
        {
            newProjectile.Translate(direction * (.1f * .4f));
            yield return null;
        }

        Destroy(newProjectile.gameObject);
    }

    public void ChangeVisualRangeSprite(bool red)
    {
        if (red)
            rangeVisualSprite.GetComponent<SpriteRenderer>().color = new Color (1, 0, 0, 1f);
        else
            rangeVisualSprite.GetComponent<SpriteRenderer>().color = new Color (1, 1, 1, 1f);
    }
    
    public virtual void SelectUnit()
    {
        isSelected = true;
        rangeVisualSprite.gameObject.SetActive(true);
        rangeCollider.radius = range / 5;
        rangeVisualSprite.localScale = new Vector3(1,1) * (rangeCollider.radius * 0.2f);
        gridDisplayFade.ToggleCellDisplay(true);

        UIUnitStats.instance.UpdateUnitStats(this, true);
    }

    public void DeSelectUnit()
    {
        rangeVisualSprite.gameObject.SetActive(false);
        isSelected = false;
        gridDisplayFade.ToggleCellDisplay(false);

        UIUnitStats.instance.UpdateUnitStats(this, false);
    }

    private void OnMouseOver()
    {
        if (!isClient) return;

        if (!isSelected)
            gridDisplayFade.ToggleCellDisplay(true);
        
        hoverStatsObject.SetActive(true);
        hoverStats.UpdateHoverStats();
    }

    private void OnMouseExit()
    {
        if (!isSelected)
        {
            if (isPlaced)
                gridDisplayFade.ToggleCellDisplay(false);
        }

        hoverStatsObject.SetActive(false);
    }

    [Server]
    public IEnumerator StunnedEffect(float seconds)
    {
        isAttacking = false;
        RpcStunEffects(true);

        yield return new WaitForSeconds(seconds);

        RpcStunEffects(false);
        isAttacking = true;
    }

    [ClientRpc]
    void RpcStunEffects(bool stunned)
    {
        if (stunned)
        {
            animations.PauseAnimation(true);
        }
        else
        {
            animations.PauseAnimation(false);
        }
    }

    public bool CheckGridCellAvailability()
    {
        if (gridCells.Count == 0) return false;

        foreach (int cellIndex in gridCells)
        {
            if (gridCells.Count != Mathf.Pow(unitGridSize, 2)) return false;

            if (!GameManager.instance.GetGridCell(cellIndex).CheckAvailability(unitSO.gridType))
            {
                return false;
            }
            else continue;
        }

        return true;
    }

    public void PlayAttackSoundClip()
    {
        audioSource.PlayOneShot(attackSound, 1);
    }

    public void DoNothingMethod() {}
}
