using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds a pre-authored boss UI to the currently active boss.
/// </summary>
[DisallowMultipleComponent]
public sealed class BossHealthBarHud : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider delayedHealthSlider;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    private BossEnemy activeBoss;
    private CharacterDamReceiver activeReceiver;
    private float targetHealth;
    private float displayedDelayedHealth;

    private void OnEnable()
    {
        BossEnemy.OnBossSpawned += ShowBoss;
        BossEnemy.OnBossDefeated += HideBoss;
        FindAndShowExistingBoss();
    }

    private void OnDisable()
    {
        BossEnemy.OnBossSpawned -= ShowBoss;
        BossEnemy.OnBossDefeated -= HideBoss;
        UnbindReceiver();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
            return;

        if (activeBoss == null || !activeBoss.IsBoss || !activeBoss.gameObject.activeInHierarchy)
        {
            HideBoss(activeBoss);
            return;
        }

        displayedDelayedHealth = Mathf.MoveTowards(displayedDelayedHealth, targetHealth, Time.unscaledDeltaTime * 0.35f);
        if (delayedHealthSlider != null)
            delayedHealthSlider.value = displayedDelayedHealth;
    }

    private void ShowBoss(BossEnemy boss)
    {
        if (boss == null || !boss.IsBoss)
            return;

        if (!HasViewReferences())
        {
            Debug.LogWarning("BossHealthBarHud is missing UI references. Assign the saved BossHealthBar fields in the Inspector.", this);
            return;
        }

        UnbindReceiver();

        activeBoss = boss;
        activeReceiver = boss.DamageReceiver;
        if (activeReceiver == null)
            activeReceiver = boss.GetComponentInChildren<CharacterDamReceiver>();

        if (activeReceiver != null)
            activeReceiver.OnHpChanged += RefreshHealth;

        nameText.text = boss.DisplayName.ToUpperInvariant();
        root.SetActive(true);

        CharacterStat stat = activeReceiver != null ? activeReceiver.CharacterCtrl?.CharacterStat : null;
        RefreshHealth(stat?.CurrentHealth ?? 0f, stat?.MaxHealth?.FinalValue ?? 1f);
    }

    private void HideBoss(BossEnemy boss)
    {
        if (boss != null && boss != activeBoss)
            return;

        UnbindReceiver();
        activeBoss = null;
        if (root != null)
            root.SetActive(false);
    }

    private void RefreshHealth(float current, float maximum)
    {
        float normalized = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
        targetHealth = normalized;
        displayedDelayedHealth = Mathf.Max(displayedDelayedHealth, normalized);

        healthSlider.value = normalized;
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current):N0} / {Mathf.CeilToInt(maximum):N0}";
    }

    private void UnbindReceiver()
    {
        if (activeReceiver != null)
            activeReceiver.OnHpChanged -= RefreshHealth;
        activeReceiver = null;
    }

    private void FindAndShowExistingBoss()
    {
        BossEnemy[] bosses = FindObjectsByType<BossEnemy>(FindObjectsInactive.Exclude);
        foreach (BossEnemy boss in bosses)
        {
            if (boss.IsBoss)
            {
                ShowBoss(boss);
                return;
            }
        }
    }

    private bool HasViewReferences()
    {
        return root != null && healthSlider != null && delayedHealthSlider != null && nameText != null && healthText != null;
    }
}
