using UnityEngine;
public abstract class CharacterCtrl : BaseMonoBehaviour
{
    [SerializeField] protected Faction faction;
    public Faction Faction => faction;
    [SerializeField] protected Transform model;
    public Transform Model => model;
    [SerializeField] private Animator animator;
    public Animator Animator => animator;
    [SerializeField] private Rigidbody2D rb;
    public Rigidbody2D Rb => rb;
    [SerializeField] private Collider2D _collider2D;
    public Collider2D Collider2D => _collider2D;
    [SerializeField] private CharacterMovement characterMovement;
    public CharacterMovement CharacterMovement => characterMovement;
    [SerializeField] private CharacterAnimation characterAnimation;
    public CharacterAnimation CharacterAnimation => characterAnimation;


    [SerializeField] private CharacterStat charaterStat;
    public CharacterStat CharacterStat => charaterStat;
    [SerializeField] private CharacterDamSender characterDamSender;
    public CharacterDamSender CharacterDamSender => characterDamSender;
    [SerializeField] private CharacterDamReceiver characterDamReceiver;
    public CharacterDamReceiver CharacterDamReceiver => characterDamReceiver;
    [SerializeField] private CharacterCombatController characterCombatController;
    public CharacterCombatController CharacterCombatController => characterCombatController;
    [SerializeField] private CharacterTargetFinder characterTargetFinder;
    public CharacterTargetFinder CharacterTargetFinder => characterTargetFinder;
    [SerializeField] private CharacterVFXController characterVFXController;
    public CharacterVFXController CharacterVFXController => characterVFXController;
    //[SerializeField] private CharacterLevel characterLevel;
    //public CharacterLevel CharacterLevel => characterLevel;
    protected override void LoadComponents()
    {
        base.LoadComponents();

        this.LoadModel();
        this.LoadAnimator();
        this.LoadRigidBody();
        this.LoadCollider2D();
        this.LoadCharacterMovement();
        this.LoadCharacterAnimation();
        this.LoadCharaterStat();
        this.LoadCharacterDamReceiver();
        this.LoadCharacterDamSender();
        this.LoadCharacterCombatController();
        this.LoadTargetFinder();
        this.LoadCharacterVFXController();
        //this.LoadCharacterLevel();
    }
    protected virtual void LoadModel()
    {
        this.model = transform.Find("Model");
    }
    protected virtual void LoadAnimator()
    {
        this.animator = GetComponentInChildren<Animator>();
    }
    protected virtual void LoadRigidBody()
    {
        if (this.rb != null) return;
        this.rb = GetComponent<Rigidbody2D>();
        Debug.Log(transform.name + ": LoadRigidBody", gameObject);
    }
    protected virtual void LoadCharacterMovement()
    {
        if (this.characterMovement != null) return;
        this.characterMovement = GetComponentInChildren<CharacterMovement>();
        Debug.Log(transform.name + ": LoadCharacterMovement", gameObject);
    }
    protected virtual void LoadCollider2D()
    {
        if (this._collider2D != null) return;
        this._collider2D = GetComponent<Collider2D>();
        Debug.Log(transform.name + ": LoadCollider2D", gameObject);
    }
    protected virtual void LoadCharacterAnimation()
    {
        if (this.characterAnimation != null) return;
        this.characterAnimation = GetComponentInChildren<CharacterAnimation>();
        Debug.Log(transform.name + ": LoadChracterAnimation", gameObject);
    }
    protected virtual void LoadCharaterStat()
    {
        if (this.charaterStat != null) return;
        this.charaterStat = GetComponentInChildren<CharacterStat>();
        Debug.Log(transform.name + ": LoadCharaterStat", gameObject);
    }
    protected virtual void LoadCharacterDamReceiver()
    {
        if (this.characterDamReceiver != null) return;
        this.characterDamReceiver = GetComponentInChildren<CharacterDamReceiver>();
        Debug.Log(transform.name + ": LoadCharacterDamReceiver", gameObject);
    }
    protected virtual void LoadCharacterDamSender()
    {
        if (this.characterDamSender != null) return;
        this.characterDamSender = GetComponentInChildren<CharacterDamSender>();
        Debug.Log(transform.name + ": LoadCharacterDamSender", gameObject);
    }
    protected virtual void LoadCharacterCombatController()
    {
        if (this.characterCombatController != null) return;
        this.characterCombatController = GetComponentInChildren<CharacterCombatController>();
        Debug.Log(transform.name + ": LoadCharacterCombatController", gameObject);
    }
    protected virtual void LoadTargetFinder()
    {
        if (this.characterTargetFinder != null) return;
        this.characterTargetFinder = GetComponentInChildren<CharacterTargetFinder>();
        Debug.Log(transform.name + ": LoadTargetFinder", gameObject);
    }
    protected virtual void LoadCharacterVFXController()
    {
        if (this.characterVFXController != null) return;
        this.characterVFXController = GetComponentInChildren<CharacterVFXController>();
    }
    //protected virtual void LoadCharacterLevel()
    //{
    //    if (this.characterLevel != null) return;
    //    this.characterLevel = GetComponentInChildren<CharacterLevel>();
    //    //Debug.Log(transform.name + ": LoadCharacterLevel", gameObject);
    //}
}
